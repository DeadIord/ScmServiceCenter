namespace Scm.Application.DTOs;

public sealed class InboundTicketMessageDto
{
    public string MessageId { get; set; } = string.Empty;

    public IList<string> References { get; set; }
        = new List<string>();

    public string Subject { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string? FromName { get; set; }
        = null;

    public string? HtmlBody { get; set; }
        = null;

    public string? TextBody { get; set; }
        = null;

    public DateTime ReceivedAtUtc { get; set; }
        = DateTime.UtcNow;

    public IList<TicketAttachmentInputDto> Attachments { get; set; }
        = new List<TicketAttachmentInputDto>();
}
