using Scm.Application.DTOs;
using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IContactService
{
    Task<List<Contact>> GetListAsync(Guid? accountId, string? query, CancellationToken cancellationToken = default);

    Task<List<Contact>> GetForAccountAsync(Guid accountId, string? query, int limit = 20, CancellationToken cancellationToken = default);

    Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Contact?> FindByIdentityAsync(string? in_email, string? in_phone, CancellationToken in_cancellationToken = default);

    Task<Contact> CreateAsync(ContactInputDto dto, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, ContactInputDto dto, CancellationToken cancellationToken = default);
}
