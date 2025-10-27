namespace Scm.Application.Services;

public sealed class MailSendRequest
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; }
        = false;

    public string? MessageId { get; set; }
        = null;

    public string? InReplyTo { get; set; }
        = null;

    public IList<string> References { get; set; }
        = new List<string>();

    public IList<MailSendAttachment> Attachments { get; set; }
        = new List<MailSendAttachment>();
}

public sealed class MailSendAttachment
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Content { get; set; } = Array.Empty<byte>();
}
