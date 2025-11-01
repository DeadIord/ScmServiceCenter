using System;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Scm.Application.DTOs;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class TicketService : ITicketService
{
    private static readonly Regex s_subjectPrefixRegex = new(@"^(re|fw|fwd)\s*[:\-]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex s_whitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex s_htmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);

    private readonly ScmDbContext m_dbContext;
    private readonly IMailService m_mailService;
    private readonly HtmlSanitizer m_htmlSanitizer;

    public TicketService(ScmDbContext in_dbContext, IMailService in_mailService)
    {
        m_dbContext = in_dbContext;
        m_mailService = in_mailService;
        m_htmlSanitizer = CreateSanitizer();
    }

    public async Task<List<Ticket>> GetListAsync(string? in_term = null, TicketStatus? in_status = null, CancellationToken in_cancellationToken = default)
    {
        var query = m_dbContext.Tickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(in_term))
        {
            var term = in_term.Trim();
            query = query.Where(t => t.Subject.Contains(term) || t.ClientEmail.Contains(term) || (t.ClientName != null && t.ClientName.Contains(term)));
        }

        if (in_status.HasValue)
        {
            var status = in_status.Value;
            query = query.Where(t => t.Status == status);
        }

        var ret = await query
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Take(200)
            .ToListAsync(in_cancellationToken);

        return ret;
    }

    public async Task<Ticket?> GetAsync(Guid in_ticketId, CancellationToken in_cancellationToken = default)
    {
        var ret = await m_dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.Messages)
            .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(t => t.Id == in_ticketId, in_cancellationToken);

        if (ret is not null)
        {
            ret.Messages = ret.Messages
                .OrderBy(m => m.SentAtUtc)
                .ToList();
        }

        return ret;
    }

    public async Task<TicketMessage> AddAgentReplyAsync(Guid in_ticketId, TicketReplyDto in_reply, string in_userId, CancellationToken in_cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(in_userId))
        {
            throw new InvalidOperationException("Не удалось определить пользователя");
        }

        var ticket = await m_dbContext.Tickets
            .Include(t => t.Messages)
            .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(t => t.Id == in_ticketId, in_cancellationToken);

        if (ticket is null)
        {
            throw new InvalidOperationException("Тикет не найден");
        }

        var sanitizedHtml = SanitizeHtml(in_reply.BodyHtml);
        if (string.IsNullOrWhiteSpace(sanitizedHtml))
        {
            throw new InvalidOperationException("Текст ответа не может быть пустым");
        }

        var bodyText = !string.IsNullOrWhiteSpace(in_reply.BodyText)
            ? NormalizeWhitespace(in_reply.BodyText)
            : BuildPlainText(sanitizedHtml);

        var subject = string.IsNullOrWhiteSpace(in_reply.Subject)
            ? ticket.Subject
            : NormalizeSubject(in_reply.Subject);

        var senderName = string.IsNullOrWhiteSpace(in_reply.SenderName)
            ? null
            : NormalizeWhitespace(in_reply.SenderName);

        var ticketMessages = ticket.Messages
            .OrderByDescending(m => m.SentAtUtc)
            .ToList();

        var referencesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(in_reply.ReplyToExternalId))
        {
            referencesSet.Add(NormalizeMessageId(in_reply.ReplyToExternalId));
        }

        var latestClientMessage = ticketMessages
            .FirstOrDefault(message => message.FromClient);

        if (latestClientMessage is not null)
        {
            referencesSet.Add(latestClientMessage.ExternalId);
            if (!string.IsNullOrWhiteSpace(latestClientMessage.ExternalReferences))
            {
                foreach (var reference in latestClientMessage.ExternalReferences.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    referencesSet.Add(reference);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(ticket.ExternalThreadId))
        {
            referencesSet.Add(ticket.ExternalThreadId);
        }

        var referenceList = referencesSet.ToList();
        var inReplyTo = referenceList.FirstOrDefault();

        var messageId = GenerateAgentMessageId();

        var agentMessage = new TicketMessage
        {
            TicketId = ticket.Id,
            FromClient = false,
            Subject = subject,
            BodyHtml = sanitizedHtml,
            BodyText = bodyText,
            SenderName = senderName,
            SentAtUtc = DateTime.UtcNow,
            CreatedByUserId = in_userId,
            ExternalReferences = referenceList.Any() ? string.Join(' ', referenceList) : null,
            ExternalId = messageId
        };

        foreach (var attachment in in_reply.Attachments)
        {
            if (attachment.Content.Length != 0)
            {
                var ticketAttachment = new TicketAttachment
                {
                    FileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "attachment" : attachment.FileName,
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType,
                    Content = attachment.Content,
                    Length = attachment.Content.LongLength
                };

                agentMessage.Attachments.Add(ticketAttachment);
            }
        }

        m_dbContext.TicketMessages.Add(agentMessage);
        ticket.Messages.Add(agentMessage);
        ticket.UpdatedAtUtc = agentMessage.SentAtUtc;

        if (string.IsNullOrWhiteSpace(ticket.ExternalThreadId))
        {
            ticket.ExternalThreadId = agentMessage.ExternalId;
        }

        if (ticket.Status != TicketStatus.Closed)
        {
            ticket.Status = TicketStatus.Pending;
        }

        var startedTransaction = false;
        IDbContextTransaction? transaction = null;

        try
        {
            if (m_dbContext.Database.CurrentTransaction is null)
            {
                transaction = await m_dbContext.Database.BeginTransactionAsync(in_cancellationToken);
                startedTransaction = true;
            }

            await m_dbContext.SaveChangesAsync(in_cancellationToken);

            var mailRequest = new MailSendRequest
            {
                To = ticket.ClientEmail,
                Subject = subject,
                Body = sanitizedHtml,
                IsHtml = true,
                InReplyTo = inReplyTo,
                References = referenceList,
                MessageId = agentMessage.ExternalId,
                Attachments = agentMessage.Attachments
                    .Select(a => new MailSendAttachment
                    {
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        Content = a.Content
                    })
                    .ToList()
            };

            var actualMessageId = await m_mailService.SendAsync(mailRequest, in_cancellationToken);
            if (!string.IsNullOrWhiteSpace(actualMessageId) &&
                !string.Equals(actualMessageId, agentMessage.ExternalId, StringComparison.OrdinalIgnoreCase))
            {
                agentMessage.ExternalId = actualMessageId;
                if (string.Equals(ticket.ExternalThreadId, messageId, StringComparison.OrdinalIgnoreCase))
                {
                    ticket.ExternalThreadId = actualMessageId;
                }

                await m_dbContext.SaveChangesAsync(in_cancellationToken);
            }

            if (startedTransaction && transaction is not null)
            {
                await transaction.CommitAsync(in_cancellationToken);
            }
        }
        catch
        {
            if (startedTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(in_cancellationToken);
            }

            throw;
        }
        finally
        {
            if (startedTransaction && transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        return agentMessage;
    }

    public async Task<TicketMessage> CreateTicketAsync(TicketComposeDto in_message, string in_userId, CancellationToken in_cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(in_userId))
        {
            throw new InvalidOperationException("Не удалось определить пользователя");
        }

        var normalizedEmail = NormalizeEmail(in_message.ClientEmail);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new InvalidOperationException("Не указан email клиента");
        }

        var sanitizedHtml = SanitizeHtml(in_message.BodyHtml);
        if (string.IsNullOrWhiteSpace(sanitizedHtml))
        {
            throw new InvalidOperationException("Текст сообщения не может быть пустым");
        }

        var subject = NormalizeSubject(in_message.Subject);
        var bodyText = BuildPlainText(sanitizedHtml);
        var senderName = string.IsNullOrWhiteSpace(in_message.SenderName)
            ? null
            : NormalizeWhitespace(in_message.SenderName);
        var clientName = string.IsNullOrWhiteSpace(in_message.ClientName)
            ? null
            : NormalizeWhitespace(in_message.ClientName);

        var sentAtUtc = DateTime.UtcNow;
        var messageId = GenerateAgentMessageId();

        var ticket = new Ticket
        {
            Subject = subject,
            ClientEmail = normalizedEmail,
            ClientName = clientName,
            Status = TicketStatus.Pending,
            CreatedAtUtc = sentAtUtc,
            UpdatedAtUtc = sentAtUtc,
            ExternalThreadId = messageId
        };

        var ticketMessage = new TicketMessage
        {
            Ticket = ticket,
            TicketId = ticket.Id,
            FromClient = false,
            Subject = subject,
            BodyHtml = sanitizedHtml,
            BodyText = bodyText,
            SenderName = senderName,
            SentAtUtc = sentAtUtc,
            CreatedByUserId = in_userId,
            ExternalId = messageId
        };

        foreach (var attachment in in_message.Attachments)
        {
            if (attachment.Content.Length != 0)
            {
                var ticketAttachment = new TicketAttachment
                {
                    FileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "attachment" : attachment.FileName,
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType,
                    Content = attachment.Content,
                    Length = attachment.Content.LongLength
                };

                ticketMessage.Attachments.Add(ticketAttachment);
            }
        }

        ticket.Messages.Add(ticketMessage);
        m_dbContext.Tickets.Add(ticket);

        var startedTransaction = false;
        IDbContextTransaction? transaction = null;

        try
        {
            if (m_dbContext.Database.CurrentTransaction is null)
            {
                transaction = await m_dbContext.Database.BeginTransactionAsync(in_cancellationToken);
                startedTransaction = true;
            }

            await m_dbContext.SaveChangesAsync(in_cancellationToken);

            var mailRequest = new MailSendRequest
            {
                To = ticket.ClientEmail,
                Subject = subject,
                Body = sanitizedHtml,
                IsHtml = true,
                MessageId = ticketMessage.ExternalId,
                Attachments = ticketMessage.Attachments
                    .Select(a => new MailSendAttachment
                    {
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        Content = a.Content
                    })
                    .ToList()
            };

            var actualMessageId = await m_mailService.SendAsync(mailRequest, in_cancellationToken);
            if (!string.IsNullOrWhiteSpace(actualMessageId) &&
                !string.Equals(actualMessageId, ticketMessage.ExternalId, StringComparison.OrdinalIgnoreCase))
            {
                ticketMessage.ExternalId = actualMessageId;
                ticket.ExternalThreadId = actualMessageId;

                await m_dbContext.SaveChangesAsync(in_cancellationToken);
            }

            if (startedTransaction && transaction is not null)
            {
                await transaction.CommitAsync(in_cancellationToken);
            }
        }
        catch
        {
            if (startedTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(in_cancellationToken);
            }

            throw;
        }
        finally
        {
            if (startedTransaction && transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        return ticketMessage;
    }

    public async Task<TicketMessage?> IngestEmailAsync(InboundTicketMessageDto in_message, CancellationToken in_cancellationToken = default)
    {
        var messageId = NormalizeMessageId(in_message.MessageId);
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return null;
        }

        var exists = await m_dbContext.TicketMessages.AnyAsync(m => m.ExternalId == messageId, in_cancellationToken);
        if (exists)
        {
            return null;
        }

        var references = in_message.References
            .Select(NormalizeMessageId)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Ticket? ticket = null;
        if (references.Count > 0)
        {
            ticket = await m_dbContext.TicketMessages
                .Include(m => m.Ticket)
                .Where(m => references.Contains(m.ExternalId))
                .Select(m => m.Ticket)
                .FirstOrDefaultAsync(in_cancellationToken);
        }

        var normalizedEmail = NormalizeEmail(in_message.FromAddress);
        var subject = NormalizeSubject(in_message.Subject);
        var receivedAtUtc = in_message.ReceivedAtUtc == default ? DateTime.UtcNow : in_message.ReceivedAtUtc;

        if (ticket is null)
        {
            ticket = await m_dbContext.Tickets
                .Include(t => t.Messages)
                .Where(t => t.ClientEmail == normalizedEmail)
                .OrderByDescending(t => t.UpdatedAtUtc)
                .FirstOrDefaultAsync(t => t.Subject == subject, in_cancellationToken);
        }

        if (ticket is null)
        {
            ticket = new Ticket
            {
                Subject = subject,
                ClientEmail = normalizedEmail,
                ClientName = string.IsNullOrWhiteSpace(in_message.FromName) ? null : NormalizeWhitespace(in_message.FromName!),
                Status = TicketStatus.Open,
                CreatedAtUtc = receivedAtUtc,
                UpdatedAtUtc = receivedAtUtc,
                ExternalThreadId = messageId
            };

            m_dbContext.Tickets.Add(ticket);
        }
        else
        {
            ticket.Status = TicketStatus.Open;
            ticket.UpdatedAtUtc = receivedAtUtc;
            if (string.IsNullOrWhiteSpace(ticket.ClientName) && !string.IsNullOrWhiteSpace(in_message.FromName))
            {
                ticket.ClientName = NormalizeWhitespace(in_message.FromName!);
            }

            if (string.IsNullOrWhiteSpace(ticket.ExternalThreadId))
            {
                ticket.ExternalThreadId = messageId;
            }
        }

        var rawHtml = !string.IsNullOrWhiteSpace(in_message.HtmlBody)
            ? in_message.HtmlBody!
            : in_message.TextBody ?? string.Empty;
        var bodyHtml = SanitizeHtml(rawHtml);
        var bodyText = !string.IsNullOrWhiteSpace(in_message.TextBody)
            ? NormalizeWhitespace(in_message.TextBody)
            : BuildPlainText(bodyHtml);

        var ticketMessage = new TicketMessage
        {
            Ticket = ticket,
            FromClient = true,
            Subject = subject,
            BodyHtml = bodyHtml,
            BodyText = bodyText,
            SenderName = string.IsNullOrWhiteSpace(in_message.FromName) ? null : NormalizeWhitespace(in_message.FromName!),
            SentAtUtc = receivedAtUtc,
            ExternalId = messageId,
            ExternalReferences = references.Count > 0 ? string.Join(' ', references) : null
        };

        foreach (var attachment in in_message.Attachments)
        {
            if (attachment.Content.Length != 0)
            {
                var ticketAttachment = new TicketAttachment
                {
                    FileName = string.IsNullOrWhiteSpace(attachment.FileName) ? "attachment" : attachment.FileName,
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType,
                    Content = attachment.Content,
                    Length = attachment.Content.LongLength
                };

                ticketMessage.Attachments.Add(ticketAttachment);
            }
        }

        m_dbContext.TicketMessages.Add(ticketMessage);
        await m_dbContext.SaveChangesAsync(in_cancellationToken);

        return ticketMessage;
    }

    public async Task<TicketAttachment?> GetAttachmentAsync(Guid in_ticketId, Guid in_messageId, Guid in_attachmentId, CancellationToken in_cancellationToken = default)
    {
        var ret = await m_dbContext.TicketAttachments
            .AsNoTracking()
            .Include(a => a.TicketMessage)
            .Where(a => a.Id == in_attachmentId && a.TicketMessageId == in_messageId && a.TicketMessage.TicketId == in_ticketId)
            .FirstOrDefaultAsync(in_cancellationToken);

        return ret;
    }

    public async Task UpdateStatusAsync(Guid in_ticketId, TicketStatus in_status, CancellationToken in_cancellationToken = default)
    {
        var ticket = await m_dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == in_ticketId, in_cancellationToken);
        if (ticket is null)
        {
            throw new InvalidOperationException("Тикет не найден");
        }

        ticket.Status = in_status;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await m_dbContext.SaveChangesAsync(in_cancellationToken);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var ret = new HtmlSanitizer();
        ret.AllowedSchemes.Add("data");
        ret.AllowedSchemes.Add("cid");
        ret.AllowedTags.Add("figure");
        ret.AllowedTags.Add("figcaption");
        ret.AllowedTags.Add("video");
        ret.AllowedTags.Add("source");
        ret.AllowedAttributes.Add("controls");
        ret.AllowedAttributes.Add("poster");
        ret.AllowedAttributes.Add("preload");
        return ret;
    }

    private static string SanitizeHtml(string in_html)
    {
        var ret = in_html ?? string.Empty;
        return string.IsNullOrWhiteSpace(ret) ? string.Empty : ret;
    }

    private string BuildPlainText(string in_html)
    {
        var sanitized = m_htmlSanitizer.Sanitize(in_html);
        var noTags = s_htmlTagRegex.Replace(sanitized, string.Empty);
        return NormalizeWhitespace(noTags);
    }

    private string NormalizeSubject(string in_subject)
    {
        var ret = string.IsNullOrWhiteSpace(in_subject) ? "Без темы" : in_subject.Trim();
        var cleaned = s_subjectPrefixRegex.Replace(ret, string.Empty).Trim();
        while (cleaned != ret)
        {
            ret = cleaned;
            cleaned = s_subjectPrefixRegex.Replace(ret, string.Empty).Trim();
        }

        return ret;
    }

    private static string NormalizeEmail(string in_email)
    {
        return string.IsNullOrWhiteSpace(in_email) ? string.Empty : in_email.Trim().ToLowerInvariant();
    }

    private static string NormalizeWhitespace(string in_value)
    {
        var trimmed = in_value?.Trim() ?? string.Empty;
        return s_whitespaceRegex.Replace(trimmed, " ");
    }

    private static string NormalizeMessageId(string? in_messageId)
    {
        if (string.IsNullOrWhiteSpace(in_messageId))
        {
            return string.Empty;
        }

        var ret = in_messageId.Trim();
        if (ret.StartsWith('<') && ret.EndsWith('>'))
        {
            ret = ret[1..^1];
        }

        return ret;
    }

    private static string GenerateAgentMessageId()
    {
        return $"{Guid.NewGuid():N}@tickets.scm.local";
    }
}
