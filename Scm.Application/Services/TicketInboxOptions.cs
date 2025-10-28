namespace Scm.Application.Services;

public sealed class TicketInboxOptions
{
    public bool Enabled { get; set; }
        = false;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }
        = 993;

    public bool UseSsl { get; set; }
        = true;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Mailbox { get; set; } = "INBOX";

    public int PollIntervalSeconds { get; set; }
        = 60;

    public int BatchSize { get; set; }
        = 10;

    public bool IgnoreInvalidCertificates { get; set; }
        = false;

    public string GetSanitizedPassword()
    {
        var ret = string.Empty;

        if (!string.IsNullOrWhiteSpace(Password))
        {
            ret = Password.Replace(" ", string.Empty);
        }

        return ret;
    }
}
