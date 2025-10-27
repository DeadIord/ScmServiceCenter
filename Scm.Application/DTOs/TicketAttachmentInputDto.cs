namespace Scm.Application.DTOs;

public sealed class TicketAttachmentInputDto
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Content { get; set; } = Array.Empty<byte>();
}
