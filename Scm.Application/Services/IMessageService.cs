using Scm.Application.DTOs;
using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IMessageService
{
    Task<Message> AddAsync(MessageDto dto, string? userId = null, CancellationToken cancellationToken = default);

    Task<List<Message>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
