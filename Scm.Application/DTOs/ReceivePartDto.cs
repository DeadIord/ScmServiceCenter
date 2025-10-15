namespace Scm.Application.DTOs;

public sealed class ReceivePartDto
{
    public string Sku { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public decimal PriceIn { get; set; }

    public decimal PriceOut { get; set; }

    public string Unit { get; set; } = "шт";
}
