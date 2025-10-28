namespace Scm.Domain.Entities;

public sealed class TicketMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
        = Guid.Empty;

    public bool FromClient { get; set; }
        = true;

    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public string? BodyText { get; set; }
        = null;

    public string? SenderName { get; set; }
        = null;

    public string ExternalId { get; set; } = string.Empty;

    public string? ExternalReferences { get; set; }
        = null;

    public string? CreatedByUserId { get; set; }
        = null;

    public DateTime SentAtUtc { get; set; }
        = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;

    public ICollection<TicketAttachment> Attachments { get; set; }
        = new List<TicketAttachment>();
}
