namespace Scm.Application.Services;

public sealed class MailOptions
{
    public string From { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }
        = 25;

    public bool UseStartTls { get; set; }
        = true;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableHealthCheckProbe { get; set; }
        = false;

    public string ProbeRecipient { get; set; } = string.Empty;

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
