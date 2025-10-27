namespace Scm.Domain.Entities;

public sealed class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Subject { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public string? ClientName { get; set; }
        = null;

    public TicketStatus Status { get; set; }
        = TicketStatus.Open;

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public string? ExternalThreadId { get; set; }
        = null;

    public ICollection<TicketMessage> Messages { get; set; }
        = new List<TicketMessage>();
}
