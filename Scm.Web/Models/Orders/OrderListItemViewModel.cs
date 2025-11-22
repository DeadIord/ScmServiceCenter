using Scm.Domain.Entities;

namespace Scm.Web.Models.Orders;

public sealed class OrderListItemViewModel
{
    public Guid Id { get; init; }

    public string Number { get; init; } = string.Empty;

    public string ClientName { get; init; } = string.Empty;

    public string ClientPhone { get; init; } = string.Empty;

    public string ClientEmail { get; init; } = string.Empty;

    public string Device { get; init; } = string.Empty;

    public OrderStatus Status { get; init; }

    public OrderPriority Priority { get; init; }

    public DateTime? SLAUntil { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string AssignedUserName { get; init; } = string.Empty;
}
