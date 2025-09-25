using Scm.Domain.Entities;

namespace Scm.Web.Areas.Client.Models;

public sealed class ClientOrderViewModel
{
    public Order? Order { get; init; }

    public IReadOnlyCollection<QuoteLine> QuoteLines { get; init; } = Array.Empty<QuoteLine>();

    public IReadOnlyCollection<Message> Messages { get; init; } = Array.Empty<Message>();
}
