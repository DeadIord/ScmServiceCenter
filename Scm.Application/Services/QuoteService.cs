using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class QuoteService(ScmDbContext dbContext) : IQuoteService
{
    private readonly ScmDbContext _dbContext = dbContext;

    public async Task AddLineAsync(AddQuoteLineDto in_dto, CancellationToken cancellationToken = default)
    {
        var orderExists = await _dbContext.Orders.AnyAsync(o => o.Id == in_dto.OrderId, cancellationToken);
        if (!orderExists)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        var line = new QuoteLine
        {
            OrderId = in_dto.OrderId,
            Kind = in_dto.Kind,
            Title = in_dto.Title.Trim(),
            Qty = in_dto.Qty,
            Price = in_dto.Price,
            Status = QuoteLineStatus.Draft
        };

        _dbContext.QuoteLines.Add(line);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitForApprovalAsync(Guid in_orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders.Include(o => o.QuoteLines).FirstOrDefaultAsync(o => o.Id == in_orderId, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        var hasQuoteItems = order.QuoteLines.Any(l => l.Kind == QuoteLineKind.Labor || l.Kind == QuoteLineKind.Part);
        if (!hasQuoteItems)
        {
            throw new InvalidOperationException("Нельзя отправить заказ без указанных работ или запчастей");
        }

        foreach (var line in order.QuoteLines)
        {
            if (line.Status == QuoteLineStatus.Draft)
            {
                line.Status = QuoteLineStatus.Proposed;
            }
        }

        order.Status = OrderStatus.WaitingApproval;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessClientApprovalAsync(
        Guid in_orderId,
        IReadOnlyCollection<Guid> in_approvedLineIds,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.QuoteLines)
            .FirstOrDefaultAsync(o => o.Id == in_orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        var proposedLines = order.QuoteLines
            .Where(l => l.Status == QuoteLineStatus.Proposed)
            .ToList();

        if (!proposedLines.Any())
        {
            throw new InvalidOperationException("Нет строк для подтверждения");
        }

        var approvedLineIds = new HashSet<Guid>(in_approvedLineIds);

        foreach (var line in proposedLines)
        {
            if (approvedLineIds.Contains(line.Id))
            {
                line.Status = QuoteLineStatus.Approved;
            }
            else
            {
                line.Status = QuoteLineStatus.Rejected;
            }
        }

        if (order.Status == OrderStatus.WaitingApproval)
        {
            var hasApproved = order.QuoteLines.Any(l => l.Status == QuoteLineStatus.Approved);
            if (hasApproved)
            {
                order.Status = OrderStatus.InRepair;
            }
            else
            {
                order.Status = OrderStatus.Diagnosing;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAsync(Guid in_orderId, CancellationToken cancellationToken = default)
    {
        decimal ret = await _dbContext.QuoteLines
            .Where(l => l.OrderId == in_orderId && l.Status == QuoteLineStatus.Approved)
            .SumAsync(l => l.Price * l.Qty, cancellationToken);

        return ret;
    }
}
