using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class OrderService(
    ScmDbContext in_dbContext,
    ITechnicianTaskService in_technicianTaskService) : IOrderService
{
    private readonly ScmDbContext m_dbContext = in_dbContext;
    private readonly ITechnicianTaskService m_technicianTaskService = in_technicianTaskService;

    public async Task<Order> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var number = GenerateNumber();

        var order = new Order
        {
            Number = number,
            ClientName = dto.ClientName.Trim(),
            ClientPhone = dto.ClientPhone.Trim(),
            ClientEmail = dto.ClientEmail.Trim(),
            AccountId = dto.AccountId,
            ContactId = dto.ContactId,
            AssignedUserId = string.IsNullOrWhiteSpace(dto.AssignedUserId) ? null : dto.AssignedUserId.Trim(),
            Device = dto.Device.Trim(),
            Serial = string.IsNullOrWhiteSpace(dto.Serial) ? null : dto.Serial.Trim(),
            Defect = dto.Defect.Trim(),
            Priority = dto.Priority,
            Status = OrderStatus.Received,
            CreatedAtUtc = DateTime.UtcNow
        };

        m_dbContext.Orders.Add(order);
        await m_dbContext.SaveChangesAsync(cancellationToken);

        await m_technicianTaskService.CreateForOrderAsync(order, cancellationToken);
        return order;
    }

    public async Task<List<Order>> GetQueueAsync(string? q, OrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = BuildQueueQuery(q, status, null);

        return await query
            .OrderBy(o => o.Status)
            .ThenBy(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Order>> GetQueuePageAsync(
        string? in_q,
        OrderStatus? in_status,
        int in_pageNumber,
        int in_pageSize,
        ClientOrderAccessFilter? in_clientFilter = null,
        CancellationToken in_cancellationToken = default)
    {
        int pageNumber = Math.Max(1, in_pageNumber);
        int pageSize = Math.Clamp(in_pageSize, 1, 100);

        var query = BuildQueueQuery(in_q, in_status, in_clientFilter);

        var totalCount = await query.CountAsync(in_cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResult<Order>
            {
                Items = Array.Empty<Order>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        int skip = (pageNumber - 1) * pageSize;

        var items = await query
            .OrderBy(o => o.Status)
            .ThenBy(o => o.CreatedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(in_cancellationToken);

        return new PagedResult<Order>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private IQueryable<Order> BuildQueueQuery(
        string? in_q,
        OrderStatus? in_status,
        ClientOrderAccessFilter? in_clientFilter)
    {
        var query = m_dbContext.Orders
            .Include(o => o.AssignedUser)
            .Include(o => o.Contact)
            .AsNoTracking();

        if (in_clientFilter is not null && in_clientFilter.HasAny)
        {
            var contactId = in_clientFilter.ContactId;
            var email = string.IsNullOrWhiteSpace(in_clientFilter.Email)
                ? null
                : in_clientFilter.Email.Trim().ToLowerInvariant();
            var phone = string.IsNullOrWhiteSpace(in_clientFilter.Phone)
                ? null
                : in_clientFilter.Phone.Trim();

            query = query.Where(o =>
                (contactId.HasValue && contactId.Value != Guid.Empty && o.ContactId == contactId.Value)
                || (email != null && (
                    (!string.IsNullOrWhiteSpace(o.ClientEmail) && o.ClientEmail.ToLower() == email)
                    || (o.Contact != null && o.Contact.Email.ToLower() == email)))
                || (phone != null && (
                    (!string.IsNullOrWhiteSpace(o.ClientPhone) && o.ClientPhone == phone)
                    || (o.Contact != null && o.Contact.Phone == phone))));
        }

        if (!string.IsNullOrWhiteSpace(in_q))
        {
            var term = in_q.Trim();
            query = query.Where(o =>
                o.Number.Contains(term) ||
                o.ClientName.Contains(term) ||
                o.ClientPhone.Contains(term) ||
                o.ClientEmail.Contains(term) ||
                o.Device.Contains(term));
        }

        if (in_status.HasValue)
        {
            query = query.Where(o => o.Status == in_status.Value);
        }

        return query;
    }

    public async Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await m_dbContext.Orders
            .Include(o => o.QuoteLines)
            .Include(o => o.Messages)
            .Include(o => o.Invoices)
            .Include(o => o.Account)
            .Include(o => o.Contact)
            .Include(o => o.AssignedUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task ChangeStatusAsync(Guid id, OrderStatus to, CancellationToken cancellationToken = default)
    {
        var order = await m_dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        order.Status = to;
        await m_dbContext.SaveChangesAsync(cancellationToken);

        await m_technicianTaskService.SyncStatusFromOrderAsync(order, cancellationToken);
    }

    public async Task<Invoice> CreateInvoiceAsync(Guid in_orderId, CancellationToken cancellationToken = default)
    {
        var order = await m_dbContext.Orders
            .Include(o => o.QuoteLines)
            .FirstOrDefaultAsync(o => o.Id == in_orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        var invoiceLines = order.QuoteLines
             .Where(l => l.Status == QuoteLineStatus.Approved || l.Status == QuoteLineStatus.Proposed)
             .ToList();

        if (!invoiceLines.Any())
        {
            throw new InvalidOperationException("Нет работ или запчастей для формирования счёта");
        }

        var amount = invoiceLines.Sum(l => l.Price * l.Qty);

        var invoice = new Invoice
        {
            OrderId = order.Id,
            Amount = amount,
            Currency = "RUB",
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        m_dbContext.Invoices.Add(invoice);
        await m_dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public string GenerateNumber()
    {
        var prefix = $"SRV-{DateTime.UtcNow:yyyy}-";
        var existing = m_dbContext.Orders
            .Where(o => o.Number.StartsWith(prefix))
            .Select(o => o.Number)
            .ToList();

        var max = 0;
        foreach (var number in existing)
        {
            var parts = number.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var seq))
            {
                if (seq > max)
                {
                    max = seq;
                }
            }
        }

        return $"{prefix}{(max + 1).ToString("D4")}";
    }
}
