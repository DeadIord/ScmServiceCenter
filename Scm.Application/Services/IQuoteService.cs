using System;
using System.Collections.Generic;
using Scm.Application.DTOs;

namespace Scm.Application.Services;

public interface IQuoteService
{
    Task AddLineAsync(AddQuoteLineDto in_dto, CancellationToken cancellationToken = default);

    Task SubmitForApprovalAsync(Guid in_orderId, CancellationToken cancellationToken = default);

    Task ProcessClientApprovalAsync(Guid in_orderId, IReadOnlyCollection<Guid> in_approvedLineIds, CancellationToken cancellationToken = default);

    Task<decimal> GetTotalAsync(Guid in_orderId, CancellationToken cancellationToken = default);
}
