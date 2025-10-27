namespace Scm.Web.Models.Tickets;

public sealed class TicketAttachmentViewModel
{
    public Guid TicketId { get; set; }
        = Guid.Empty;

    public Guid MessageId { get; set; }
        = Guid.Empty;

    public Guid AttachmentId { get; set; }
        = Guid.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Length { get; set; }
        = 0;

    public bool PreviewInline { get; set; }
        = false;

    public string PreviewUrl { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;
}
