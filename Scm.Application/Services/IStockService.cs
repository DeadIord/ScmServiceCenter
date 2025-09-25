using Scm.Application.DTOs;
using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IStockService
{
    Task ReceiveAsync(ReceivePartDto dto, CancellationToken cancellationToken = default);

    Task<List<Part>> GetLowStockAsync(CancellationToken cancellationToken = default);
}
