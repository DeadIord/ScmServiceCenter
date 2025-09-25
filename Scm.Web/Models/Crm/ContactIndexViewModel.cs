using Scm.Domain.Entities;

namespace Scm.Web.Models.Crm;

public sealed class ContactIndexViewModel
{
    public Guid? AccountId { get; init; }
        = null;

    public string? Query { get; init; }
        = null;

    public IReadOnlyCollection<Contact> Contacts { get; init; }
        = Array.Empty<Contact>();

    public IReadOnlyCollection<Account> Accounts { get; init; }
        = Array.Empty<Account>();
}
