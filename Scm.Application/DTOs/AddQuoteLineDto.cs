using Scm.Domain.Entities;

namespace Scm.Application.DTOs;

public sealed class AddQuoteLineDto
{
    public Guid OrderId { get; set; }

    public QuoteLineKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public decimal Price { get; set; }
}
