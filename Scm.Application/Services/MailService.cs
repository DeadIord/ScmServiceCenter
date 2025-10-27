using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public Task SendAsync(
        string in_to,
        string in_subject,
        string in_body,
        bool in_isHtml = false,
        CancellationToken in_cancellationToken = default)
    {
        var request = new MailSendRequest
        {
            To = in_to,
            Subject = in_subject,
            Body = in_body,
            IsHtml = in_isHtml
        };

        return SendAsync(request, in_cancellationToken);
    }

    public async Task<string> SendAsync(
        MailSendRequest in_request,
        CancellationToken in_cancellationToken = default)
    {
        if (in_request is null)
        {
            throw new ArgumentNullException(nameof(in_request));
        }

        if (string.IsNullOrWhiteSpace(in_request.To))
        {
            throw new ArgumentException("Не указан адрес получателя", nameof(in_request));
        }

        if (string.IsNullOrWhiteSpace(m_options.From) || string.IsNullOrWhiteSpace(m_options.Host))
        {
            throw new InvalidOperationException("Почтовый сервис не настроен");
        }

        var fromAddress = m_options.From.Trim();
        var hostAddress = m_options.Host.Trim();

        var subject = string.IsNullOrWhiteSpace(in_request.Subject) ? string.Empty : in_request.Subject.Trim();
        var body = in_request.Body ?? string.Empty;

        var mailAddressFrom = new MailAddress(fromAddress, null, Encoding.UTF8);
        var mailAddressTo = new MailAddress(in_request.To.Trim(), null, Encoding.UTF8);

        using var mailMessage = new MailMessage
        {
            From = mailAddressFrom,
            Subject = subject,
            Body = body,
            IsBodyHtml = in_request.IsHtml,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };

        mailMessage.To.Add(mailAddressTo);

        var messageIdHeader = EnsureMessageId(mailMessage, in_request.MessageId, fromAddress);
        ApplyThreadHeaders(mailMessage, in_request.InReplyTo, in_request.References);
        AddAttachments(mailMessage, in_request.Attachments);

        var smtpUser = m_options.User.Trim();
        var smtpPassword = m_options.GetSanitizedPassword();

        if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword))
        {
            throw new InvalidOperationException("Учетные данные SMTP не заданы");
        }

        if (smtpPassword.Length != 16 || smtpPassword.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            const string invalidPasswordMessage = "Пароль SMTP должен содержать 16 латинских символов без пробелов";
            m_logger.LogError(invalidPasswordMessage);
            throw new InvalidOperationException(invalidPasswordMessage);
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
            m_logger.LogInformation("Письмо отправлено на {Recipient}", in_request.To);
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Не удалось отправить письмо на {Recipient}", in_request.To);
            throw new InvalidOperationException("Не удалось отправить письмо клиенту", ex);
        }

        return TrimMessageId(messageIdHeader);
    }

    private static string EnsureMessageId(MailMessage in_mailMessage, string? in_messageId, string in_fromAddress)
    {
        var normalized = NormalizeMessageId(in_messageId);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = GenerateMessageId(in_fromAddress);
        }

        var headerValue = normalized.StartsWith('<') && normalized.EndsWith('>')
            ? normalized
            : $"<{normalized}>";

        in_mailMessage.Headers.Remove("Message-ID");
        in_mailMessage.Headers.Add("Message-ID", headerValue);

        return headerValue;
    }

    private static void ApplyThreadHeaders(MailMessage in_mailMessage, string? in_inReplyTo, IList<string>? in_references)
    {
        var references = new List<string>();

        var inReplyTo = NormalizeMessageId(in_inReplyTo);
        if (!string.IsNullOrWhiteSpace(inReplyTo))
        {
            var value = $"<{inReplyTo}>";
            in_mailMessage.Headers.Add("In-Reply-To", value);
            references.Add(value);
        }

        if (in_references is not null)
        {
            foreach (var reference in in_references)
            {
                var normalized = NormalizeMessageId(reference);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    var value = $"<{normalized}>";
                    if (!references.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
                    {
                        references.Add(value);
                    }
                }
            }
        }

        if (references.Count > 0)
        {
            in_mailMessage.Headers.Add("References", string.Join(' ', references));
        }
    }

    private static void AddAttachments(MailMessage in_mailMessage, IList<MailSendAttachment>? in_attachments)
    {
        if (in_attachments is null || in_attachments.Count == 0)
        {
            return;
        }

        foreach (var attachment in in_attachments)
        {
            if (attachment.Content.Length != 0)
            {
                var fileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "attachment" : attachment.FileName;
                var contentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType;

                var stream = new MemoryStream(attachment.Content);
                var mailAttachment = new Attachment(stream, fileName, contentType);
                in_mailMessage.Attachments.Add(mailAttachment);
            }
        }
    }

    private static string NormalizeMessageId(string? in_messageId)
    {
        if (string.IsNullOrWhiteSpace(in_messageId))
        {
            return string.Empty;
        }

        var trimmed = in_messageId.Trim();
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            trimmed = trimmed[1..^1];
        }

        return trimmed;
    }

    private static string GenerateMessageId(string in_fromAddress)
    {
        var domain = "localhost";
        var atIndex = in_fromAddress.IndexOf('@');
        if (atIndex >= 0 && atIndex < in_fromAddress.Length - 1)
        {
            domain = in_fromAddress[(atIndex + 1)..];
        }

        return $"{Guid.NewGuid():N}@{domain}";
    }

    private static string TrimMessageId(string in_messageId)
    {
        var trimmed = in_messageId?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        return trimmed.Trim('<', '>');
    }
}
