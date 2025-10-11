using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Scm.Application.Services;

public sealed class MailService : IMailService
{
    private readonly MailOptions m_options;
    private readonly ILogger<MailService> m_logger;

    public MailService(IOptions<MailOptions> in_options, ILogger<MailService> in_logger)
    {
        m_options = in_options.Value;
        m_logger = in_logger;
    }

    public async Task SendAsync(
        string in_to,
        string in_subject,
        string in_body,
        bool in_isHtml = false,
        CancellationToken in_cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(in_to))
        {
            throw new ArgumentException("Не указан адрес получателя", nameof(in_to));
        }

        if (string.IsNullOrWhiteSpace(m_options.From) || string.IsNullOrWhiteSpace(m_options.Host))
        {
            throw new InvalidOperationException("Почтовый сервис не настроен");
        }

        var fromAddress = m_options.From.Trim();
        var hostAddress = m_options.Host.Trim();

        var subject = string.IsNullOrWhiteSpace(in_subject) ? string.Empty : in_subject.Trim();
        var body = in_body ?? string.Empty;

        var mailAddressFrom = new MailAddress(fromAddress, null, Encoding.UTF8);
        var mailAddressTo = new MailAddress(in_to.Trim(), null, Encoding.UTF8);

        using var mailMessage = new MailMessage
        {
            From = mailAddressFrom,
            Subject = subject,
            Body = body,
            IsBodyHtml = in_isHtml,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };

        mailMessage.To.Add(mailAddressTo);

        var smtpUser = m_options.User.Trim();
        var smtpPassword = m_options.Password;

        if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword))
        {
            throw new InvalidOperationException("Учетные данные SMTP не заданы");
        }

        using var smtpClient = new SmtpClient(hostAddress, m_options.Port)
        {
            EnableSsl = m_options.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtpUser, smtpPassword)
        };

        try
        {
            await smtpClient.SendMailAsync(mailMessage, in_cancellationToken);
            m_logger.LogInformation("Письмо отправлено на {Recipient}", in_to);
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Не удалось отправить письмо на {Recipient}", in_to);
            throw new InvalidOperationException("Не удалось отправить письмо клиенту", ex);
        }
    }
}
