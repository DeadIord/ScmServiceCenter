using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class OrderService(ScmDbContext dbContext) : IOrderService
{
    private readonly ScmDbContext _dbContext = dbContext;

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
            Device = dto.Device.Trim(),
            Serial = string.IsNullOrWhiteSpace(dto.Serial) ? null : dto.Serial.Trim(),
            Defect = dto.Defect.Trim(),
            Priority = dto.Priority,
            Status = OrderStatus.Received,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<List<Order>> GetQueueAsync(string? q, OrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(o => o.Number.Contains(term) || o.ClientName.Contains(term) || o.ClientPhone.Contains(term) || o.ClientEmail.Contains(term) || o.Device.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderBy(o => o.Status)
            .ThenBy(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.QuoteLines)
            .Include(o => o.Messages)
            .Include(o => o.Invoices)
            .Include(o => o.Account)
            .Include(o => o.Contact)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task ChangeStatusAsync(Guid id, OrderStatus to, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        order.Status = to;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Invoice> CreateInvoiceAsync(Guid in_orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.QuoteLines)
            .FirstOrDefaultAsync(o => o.Id == in_orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        var approvedLines = order.QuoteLines
            .Where(l => l.Status == QuoteLineStatus.Approved)
            .ToList();

        if (!approvedLines.Any())
        {
            throw new InvalidOperationException("Нет утверждённых работ или запчастей для формирования счёта");
        }

        var amount = approvedLines.Sum(l => l.Price * l.Qty);

        var invoice = new Invoice
        {
            OrderId = order.Id,
            Amount = amount,
            Currency = "RUB",
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public string GenerateNumber()
    {
        var prefix = $"SRV-{DateTime.UtcNow:yyyy}-";
        var existing = _dbContext.Orders
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
