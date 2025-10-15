namespace Scm.Domain.Entities;

public enum InvoiceStatus
{
    Draft = 0,
    Paid = 1,
    Refunded = 2
}

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "RUB";

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public Order? Order { get; set; }
}
