namespace Scm.Application.DTOs;

public sealed class TicketReplyDto
{
    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public string? BodyText { get; set; }
        = null;

    public string? SenderName { get; set; }
        = null;

    public string? ReplyToExternalId { get; set; }
        = null;

    public IList<TicketAttachmentInputDto> Attachments { get; set; }
        = new List<TicketAttachmentInputDto>();
}
