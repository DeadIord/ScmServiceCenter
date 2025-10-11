using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class QuoteService(ScmDbContext dbContext) : IQuoteService
{
    private readonly ScmDbContext _dbContext = dbContext;

    public async Task AddLineAsync(AddQuoteLineDto dto, CancellationToken cancellationToken = default)
    {
        var orderExists = await _dbContext.Orders.AnyAsync(o => o.Id == dto.OrderId, cancellationToken);
        if (!orderExists)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        var line = new QuoteLine
        {
            OrderId = dto.OrderId,
            Kind = dto.Kind,
            Title = dto.Title.Trim(),
            Qty = dto.Qty,
            Price = dto.Price,
            Status = QuoteLineStatus.Draft
        };

        _dbContext.QuoteLines.Add(line);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitForApprovalAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders.Include(o => o.QuoteLines).FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
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

    public async Task ApproveLineAsync(Guid lineId, CancellationToken cancellationToken = default)
    {
        var line = await _dbContext.QuoteLines.FirstOrDefaultAsync(l => l.Id == lineId, cancellationToken);
        if (line is null)
        {
            throw new InvalidOperationException("Строка сметы не найдена");
        }

        line.Status = QuoteLineStatus.Approved;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectLineAsync(Guid lineId, CancellationToken cancellationToken = default)
    {
        var line = await _dbContext.QuoteLines.FirstOrDefaultAsync(l => l.Id == lineId, cancellationToken);
        if (line is null)
        {
            throw new InvalidOperationException("Строка сметы не найдена");
        }

        line.Status = QuoteLineStatus.Rejected;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.QuoteLines
            .Where(l => l.OrderId == orderId && l.Status == QuoteLineStatus.Approved)
            .SumAsync(l => l.Price * l.Qty, cancellationToken);
    }
}
