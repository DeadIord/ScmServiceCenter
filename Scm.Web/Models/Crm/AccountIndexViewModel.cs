using Scm.Domain.Entities;

namespace Scm.Web.Models.Crm;

public sealed class AccountIndexViewModel
{
    public string? Query { get; init; }
        = null;

    public IReadOnlyCollection<Account> Accounts { get; init; }
        = Array.Empty<Account>();
}
