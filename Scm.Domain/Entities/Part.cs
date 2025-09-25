namespace Scm.Domain.Entities;

public sealed class Part
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Sku { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal StockQty { get; set; }

    public decimal ReorderPoint { get; set; }

    public decimal PriceIn { get; set; }

    public decimal PriceOut { get; set; }

    public string Unit { get; set; } = "шт";

    public bool IsActive { get; set; } = true;
}
