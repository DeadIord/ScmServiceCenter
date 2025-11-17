using Scm.Domain.Entities;

namespace Scm.Web.Models.Stock;

public sealed class StockIndexViewModel
{
    public string? Query { get; init; }

    public bool OnlyLowStock { get; init; }

    public IReadOnlyCollection<Part> Parts { get; init; } = Array.Empty<Part>();

    public int TotalParts { get; init; }

    public int ActiveParts { get; init; }

    public int LowStockParts { get; init; }

    public decimal TotalStockValue { get; init; }
}
