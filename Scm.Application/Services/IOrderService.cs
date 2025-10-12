using Scm.Application.DTOs;
using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IOrderService
{
    Task<Order> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);

    Task<List<Order>> GetQueueAsync(string? q, OrderStatus? status, CancellationToken cancellationToken = default);

    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(Guid id, OrderStatus to, CancellationToken cancellationToken = default);

    Task<Invoice> CreateInvoiceAsync(Guid in_orderId, CancellationToken cancellationToken = default);

    string GenerateNumber();
}
