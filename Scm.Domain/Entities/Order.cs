namespace Scm.Domain.Entities;

public enum OrderPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum OrderStatus
{
    Received = 0,
    Diagnosing = 1,
    WaitingApproval = 2,
    InRepair = 3,
    Ready = 4,
    Closed = 5
}

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Number { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string ClientPhone { get; set; } = string.Empty;

    public Guid? AccountId { get; set; }
        = null;

    public Guid? ContactId { get; set; }
        = null;

    public string Device { get; set; } = string.Empty;

    public string? Serial { get; set; }

    public string Defect { get; set; } = string.Empty;

    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    public DateTime? SLAUntil { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Received;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string ClientAccessToken { get; set; } = Guid.NewGuid().ToString("N");

    public ICollection<QuoteLine> QuoteLines { get; set; } = new List<QuoteLine>();

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public Account? Account { get; set; }
        = null;

    public Contact? Contact { get; set; }
        = null;
}
