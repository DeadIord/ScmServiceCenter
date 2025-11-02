using System.Collections.Generic;

namespace Scm.Application.DTOs;

public sealed class TicketComposeDto
{
    public string ClientEmail { get; set; }
        = string.Empty;

    public string? ClientName { get; set; }
        = null;

    public string Subject { get; set; }
        = string.Empty;

    public string BodyHtml { get; set; }
        = string.Empty;

    public string? SenderName { get; set; }
        = null;

    public IList<TicketAttachmentInputDto> Attachments { get; set; }
        = new List<TicketAttachmentInputDto>();
}
