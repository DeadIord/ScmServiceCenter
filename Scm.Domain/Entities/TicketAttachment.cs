namespace Scm.Domain.Entities;

public sealed class TicketAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketMessageId { get; set; }
        = Guid.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Length { get; set; }
        = 0;

    public byte[] Content { get; set; } = Array.Empty<byte>();

    public TicketMessage TicketMessage { get; set; } = null!;
}
