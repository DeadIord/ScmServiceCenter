using Scm.Application.DTOs;

namespace Scm.Application.Services;

public interface IQuoteService
{
    Task AddLineAsync(AddQuoteLineDto dto, CancellationToken cancellationToken = default);

    Task SubmitForApprovalAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task ApproveLineAsync(Guid lineId, CancellationToken cancellationToken = default);

    Task RejectLineAsync(Guid lineId, CancellationToken cancellationToken = default);

    Task<decimal> GetTotalAsync(Guid orderId, CancellationToken cancellationToken = default);
}
