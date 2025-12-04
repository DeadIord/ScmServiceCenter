using System.Linq;
using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class ContactService(ScmDbContext dbContext) : IContactService
{
    private readonly ScmDbContext _dbContext = dbContext;

    public async Task<List<Contact>> GetListAsync(Guid? accountId, string? query, CancellationToken cancellationToken = default)
    {
        var contacts = _dbContext.Contacts
            .Include(c => c.Account)
            .AsNoTracking();

        if (accountId.HasValue && accountId.Value != Guid.Empty)
        {
            contacts = contacts.Where(c => c.AccountId == accountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            contacts = contacts.Where(c => c.FullName.Contains(term) || c.Email.Contains(term) || c.Phone.Contains(term));
        }

        return await contacts
            .OrderBy(c => c.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Contact>> GetForAccountAsync(Guid accountId, string? query, int limit = 20, CancellationToken cancellationToken = default)
    {
        var contacts = _dbContext.Contacts
            .Where(c => c.AccountId == accountId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            contacts = contacts.Where(c => c.FullName.Contains(term) || c.Email.Contains(term) || c.Phone.Contains(term));
        }

        return await contacts
            .OrderBy(c => c.FullName)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contacts
            .Include(c => c.Account)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Contact?> FindByIdentityAsync(
        string? in_email,
        string? in_phone,
        CancellationToken in_cancellationToken = default)
    {
        var email = string.IsNullOrWhiteSpace(in_email) ? null : in_email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(in_phone) ? null : in_phone.Trim();

        if (email is null && phone is null)
        {
            return null;
        }

        return await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => (email != null && c.Email.ToLower() == email)
                    || (phone != null && c.Phone == phone),
                in_cancellationToken);
    }

    public async Task<Contact> CreateAsync(ContactInputDto dto, CancellationToken cancellationToken = default)
    {
        var contact = new Contact
        {
            AccountId = dto.AccountId,
            FullName = dto.FullName.Trim(),
            Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position.Trim(),
            Phone = dto.Phone.Trim(),
            Email = dto.Email.Trim()
        };

        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return contact;
    }

    public async Task UpdateAsync(Guid id, ContactInputDto dto, CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (contact is null)
        {
            throw new InvalidOperationException("Контакт не найден");
        }

        contact.AccountId = dto.AccountId;
        contact.FullName = dto.FullName.Trim();
        contact.Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position.Trim();
        contact.Phone = dto.Phone.Trim();
        contact.Email = dto.Email.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
