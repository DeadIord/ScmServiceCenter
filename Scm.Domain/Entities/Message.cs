namespace Scm.Domain.Entities;

public sealed class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public string? FromUserId { get; set; }

    public bool FromClient { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime AtUtc { get; set; } = DateTime.UtcNow;

    public Order? Order { get; set; }
}
