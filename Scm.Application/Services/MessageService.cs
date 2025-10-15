using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class MessageService(ScmDbContext dbContext) : IMessageService
{
    private readonly ScmDbContext _dbContext = dbContext;

    public async Task<Message> AddAsync(MessageDto dto, string? userId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Orders.AnyAsync(o => o.Id == dto.OrderId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Заказ не найден");
        }

        var message = new Message
        {
            OrderId = dto.OrderId,
            FromClient = dto.FromClient,
            Text = dto.Text.Trim(),
            FromUserId = userId,
            AtUtc = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task<List<Message>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Messages
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.AtUtc)
            .ToListAsync(cancellationToken);
    }
}
