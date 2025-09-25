namespace Scm.Application.DTOs;

public sealed class MessageDto
{
    public Guid OrderId { get; set; }

    public bool FromClient { get; set; }

    public string Text { get; set; } = string.Empty;
}
