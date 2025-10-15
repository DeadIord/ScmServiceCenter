using Scm.Domain.Entities;

namespace Scm.Web.Models.Orders;

public sealed class OrderKanbanColumnViewModel
{
    public OrderStatus Status { get; init; }

    public string Title { get; init; } = string.Empty;

    public IReadOnlyCollection<Order> Orders { get; init; } = Array.Empty<Order>();
}

public sealed class OrderKanbanViewModel
{
    public IReadOnlyCollection<OrderKanbanColumnViewModel> Columns { get; init; } = Array.Empty<OrderKanbanColumnViewModel>();
}
