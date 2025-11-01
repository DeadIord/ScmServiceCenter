using Scm.Domain.Entities;

namespace Scm.Web.Models.Orders;

public sealed class OrdersIndexViewModel
{
    public string? Query { get; init; }

    public OrderStatus? Status { get; init; }

    public IReadOnlyCollection<OrderListItemViewModel> Orders { get; init; } = Array.Empty<OrderListItemViewModel>();

    public int PageNumber { get; init; }
        = 1;

    public int TotalPages { get; init; }
        = 1;

    public int PageSize { get; init; }
        = 1;

    public int TotalCount { get; init; }
        = 0;

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    public int StartRecord { get; init; }
        = 0;

    public int EndRecord { get; init; }
        = 0;
}
