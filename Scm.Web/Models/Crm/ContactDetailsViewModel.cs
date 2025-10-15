using Scm.Domain.Entities;

namespace Scm.Web.Models.Crm;

public sealed class ContactDetailsViewModel
{
    public Contact Contact { get; init; } = null!;
}
