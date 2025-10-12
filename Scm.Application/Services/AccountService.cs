using System.Linq;
using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class AccountService(ScmDbContext dbContext) : IAccountService
{
    private readonly ScmDbContext _dbContext = dbContext;

    public async Task<List<Account>> GetListAsync(string? query, CancellationToken cancellationToken = default)
    {
        var accounts = _dbContext.Accounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var pattern = $"%{term}%";
            accounts = accounts.Where(a => EF.Functions.ILike(a.Name, pattern)
                || (a.Inn != null && EF.Functions.ILike(a.Inn, pattern)));
        }

        return await accounts
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Account>> SearchAsync(string? query, int limit = 20, CancellationToken cancellationToken = default)
    {
        var accounts = _dbContext.Accounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var pattern = $"%{term}%";
            accounts = accounts.Where(a => EF.Functions.ILike(a.Name, pattern)
                || (a.Inn != null && EF.Functions.ILike(a.Inn, pattern)));
        }

        return await accounts
            .OrderBy(a => a.Name)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .Include(a => a.Contacts)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account> CreateAsync(AccountInputDto dto, CancellationToken cancellationToken = default)
    {
        var account = new Account
        {
            Name = dto.Name.Trim(),
            Type = dto.Type,
            Inn = string.IsNullOrWhiteSpace(dto.Inn) ? null : dto.Inn.Trim(),
            Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
            Tags = string.IsNullOrWhiteSpace(dto.Tags) ? null : dto.Tags.Trim(),
            ManagerUserId = string.IsNullOrWhiteSpace(dto.ManagerUserId) ? null : dto.ManagerUserId.Trim()
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task UpdateAsync(Guid id, AccountInputDto dto, CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (account is null)
        {
            throw new InvalidOperationException("Контрагент не найден");
        }

        account.Name = dto.Name.Trim();
        account.Type = dto.Type;
        account.Inn = string.IsNullOrWhiteSpace(dto.Inn) ? null : dto.Inn.Trim();
        account.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
        account.Tags = string.IsNullOrWhiteSpace(dto.Tags) ? null : dto.Tags.Trim();
        account.ManagerUserId = string.IsNullOrWhiteSpace(dto.ManagerUserId) ? null : dto.ManagerUserId.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
