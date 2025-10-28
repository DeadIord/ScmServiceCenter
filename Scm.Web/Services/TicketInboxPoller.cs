using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Scm.Application.DTOs;
using Scm.Application.Services;

namespace Scm.Web.Services;

public sealed class TicketInboxPoller : ITicketInboxPoller
{
    private readonly TicketInboxOptions m_options;
    private readonly IServiceScopeFactory m_scopeFactory;
    private readonly ILogger<TicketInboxPoller> m_logger;

    public TicketInboxPoller(
        IOptions<TicketInboxOptions> in_options,
        IServiceScopeFactory in_scopeFactory,
        ILogger<TicketInboxPoller> in_logger)
    {
        m_options = in_options.Value;
        m_scopeFactory = in_scopeFactory;
        m_logger = in_logger;
    }

    public async Task<TicketInboxPollResult> PollAsync(CancellationToken in_cancellationToken)
    {
        var ret = new TicketInboxPollResult
        {
            Enabled = m_options.Enabled,
            StatusMessage = "Импорт почты отключён"
        };

        if (!m_options.Enabled)
        {
            m_logger.LogInformation("Импорт почтовых тикетов отключён в настройках.");
        }
        else
        {
            using var client = new ImapClient();

            if (m_options.IgnoreInvalidCertificates)
            {
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }

            var secureOption = m_options.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(m_options.Host, m_options.Port, secureOption, in_cancellationToken);
            await client.AuthenticateAsync(m_options.User, m_options.GetSanitizedPassword(), in_cancellationToken);

            try
            {
                var mailbox = string.IsNullOrWhiteSpace(m_options.Mailbox) ? "INBOX" : m_options.Mailbox;
                var folder = await client.GetFolderAsync(mailbox, in_cancellationToken);
                await folder.OpenAsync(FolderAccess.ReadWrite, in_cancellationToken);

                try
                {
                    var uids = await folder.SearchAsync(SearchQuery.NotSeen, in_cancellationToken);
                    ret.TotalMessages = uids.Count;

                    if (uids.Count == 0)
                    {
                        ret.StatusMessage = "Новых писем не найдено";
                    }
                    else
                    {
                        var batchSize = Math.Min(m_options.BatchSize, uids.Count);
                        var selectedUids = uids
                            .OrderByDescending(uid => uid.Id)
                            .Take(batchSize)
                            .OrderBy(uid => uid.Id)
                            .ToList();

                        using var scope = m_scopeFactory.CreateScope();
                        var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();

                        var processed = 0;
                        var imported = 0;
                        var skipped = 0;
                        var failed = 0;

                        var processingSequence = selectedUids
                            .TakeWhile(_ => !in_cancellationToken.IsCancellationRequested);

                        foreach (var uid in processingSequence)
                        {
                            try
                            {
                                var message = await folder.GetMessageAsync(uid, in_cancellationToken);
                                var dto = await ConvertAsync(message, in_cancellationToken);
                                var result = await ticketService.IngestEmailAsync(dto, in_cancellationToken);
                                await folder.AddFlagsAsync(uid, MessageFlags.Seen, true, in_cancellationToken);

                                processed++;

                                if (result is not null)
                                {
                                    imported++;
                                    m_logger.LogInformation(
                                        "Получено письмо {MessageId} для тикета {TicketId}",
                                        dto.MessageId,
                                        result.TicketId);
                                }
                                else
                                {
                                    skipped++;
                                    m_logger.LogDebug("Письмо {MessageId} уже обработано ранее", dto.MessageId);
                                }
                            }
                            catch (Exception ex)
                            {
                                failed++;
                                m_logger.LogError(ex, "Не удалось обработать письмо UID {Uid}", uid.Id);

                                try
                                {
                                    await folder.AddFlagsAsync(uid, MessageFlags.Seen, true, in_cancellationToken);
                                }
                                catch (Exception flagEx)
                                {
                                    m_logger.LogWarning(flagEx, "Не удалось пометить письмо UID {Uid} как прочитанное", uid.Id);
                                }
                            }
                        }

                        ret.ProcessedMessages = processed;
                        ret.ImportedMessages = imported;
                        ret.SkippedMessages = skipped;
                        ret.FailedMessages = failed;

                        ret.StatusMessage = failed == 0
                            ? "Импорт писем завершён"
                            : "Импорт писем завершён с ошибками";
                    }
                }
                finally
                {
                    await folder.CloseAsync(false, in_cancellationToken);
                }
            }
            finally
            {
                await client.DisconnectAsync(true, in_cancellationToken);
            }
        }

        return ret;
    }

    private static async Task<InboundTicketMessageDto> ConvertAsync(MimeMessage in_message, CancellationToken in_cancellationToken)
    {
        var attachments = new List<TicketAttachmentInputDto>();
        foreach (var entity in in_message.Attachments)
        {
            if (entity is MimePart mimePart)
            {
                using var memory = new MemoryStream();
                await mimePart.Content.DecodeToAsync(memory, in_cancellationToken);
                attachments.Add(new TicketAttachmentInputDto
                {
                    FileName = string.IsNullOrWhiteSpace(mimePart.FileName) ? "attachment" : mimePart.FileName!,
                    ContentType = mimePart.ContentType?.MimeType ?? "application/octet-stream",
                    Content = memory.ToArray()
                });
            }
            else if (entity is MessagePart messagePart)
            {
                using var memory = new MemoryStream();
                await messagePart.Message.WriteToAsync(memory, in_cancellationToken);
                var messageName = messagePart.ContentDisposition?.FileName ?? messagePart.ContentType?.Name;
                attachments.Add(new TicketAttachmentInputDto
                {
                    FileName = string.IsNullOrWhiteSpace(messageName) ? "message.eml" : messageName!,
                    ContentType = "message/rfc822",
                    Content = memory.ToArray()
                });
            }
        }

        var references = new List<string>();
        if (!string.IsNullOrWhiteSpace(in_message.InReplyTo))
        {
            references.Add(in_message.InReplyTo);
        }

        foreach (var reference in in_message.References)
        {
            references.Add(reference);
        }

        var mailbox = in_message.From?.Mailboxes.FirstOrDefault();

        var dto = new InboundTicketMessageDto
        {
            MessageId = string.IsNullOrWhiteSpace(in_message.MessageId) ? Guid.NewGuid().ToString("N") : in_message.MessageId!,
            Subject = in_message.Subject ?? string.Empty,
            FromAddress = mailbox?.Address ?? string.Empty,
            FromName = mailbox?.Name,
            HtmlBody = in_message.HtmlBody,
            TextBody = in_message.TextBody,
            ReceivedAtUtc = in_message.Date.UtcDateTime,
            References = references,
            Attachments = attachments
        };

        return dto;
    }
}
