using Scm.Application.DTOs;
using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IAccountService
{
    Task<List<Account>> GetListAsync(string? query, CancellationToken cancellationToken = default);

    Task<List<Account>> SearchAsync(string? query, int limit = 20, CancellationToken cancellationToken = default);

    Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Account> CreateAsync(AccountInputDto dto, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, AccountInputDto dto, CancellationToken cancellationToken = default);
}
