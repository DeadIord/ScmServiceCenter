using Scm.Domain.Entities;

namespace Scm.Web.Models.Orders;

public sealed class OrdersIndexViewModel
{
    public string? Query { get; init; }

    public OrderStatus? Status { get; init; }

    public IReadOnlyCollection<OrderListItemViewModel> Orders { get; init; } = Array.Empty<OrderListItemViewModel>();
}
