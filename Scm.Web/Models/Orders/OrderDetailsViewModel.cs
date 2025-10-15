using Scm.Domain.Entities;

namespace Scm.Web.Models.Orders;

public sealed class OrderDetailsViewModel
{
    public Order Order { get; init; } = null!;

    public decimal ApprovedTotal { get; init; }

    public IReadOnlyCollection<Message> Messages { get; init; } = Array.Empty<Message>();

    public string ClientTrackingLink { get; init; } = string.Empty;
}
