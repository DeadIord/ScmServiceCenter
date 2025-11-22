using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface ITechnicianTaskService
{
    Task<TechnicianTask?> CreateForOrderAsync(Order in_order, CancellationToken in_cancellationToken = default);

    Task<List<TechnicianTask>> GetAsync(string? in_q, TechnicianTaskStatus? in_status, CancellationToken in_cancellationToken = default);
}
