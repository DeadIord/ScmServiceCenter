namespace Scm.Domain.Entities;

public enum QuoteLineKind
{
    Labor = 0,
    Part = 1
}

public enum QuoteLineStatus
{
    Draft = 0,
    Proposed = 1,
    Approved = 2,
    Rejected = 3
}

public sealed class QuoteLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public QuoteLineKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Qty { get; set; }

    public decimal Price { get; set; }

    public QuoteLineStatus Status { get; set; } = QuoteLineStatus.Draft;

    public Order? Order { get; set; }
}
