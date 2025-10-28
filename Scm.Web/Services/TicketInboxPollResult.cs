namespace Scm.Web.Services;

public sealed class TicketInboxPollResult
{
    public bool Enabled { get; set; }

    public int TotalMessages { get; set; }

    public int ProcessedMessages { get; set; }

    public int ImportedMessages { get; set; }

    public int SkippedMessages { get; set; }

    public int FailedMessages { get; set; }

    public string StatusMessage { get; set; } = string.Empty;
}
