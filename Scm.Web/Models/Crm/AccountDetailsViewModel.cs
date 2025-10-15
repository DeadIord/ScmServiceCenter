using Scm.Domain.Entities;

namespace Scm.Web.Models.Crm;

public sealed class AccountDetailsViewModel
{
    public Account Account { get; init; } = null!;

    public IReadOnlyCollection<Contact> Contacts { get; init; }
        = Array.Empty<Contact>();
}
