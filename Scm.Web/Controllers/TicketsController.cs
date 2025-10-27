using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Web.Authorization;
using Scm.Web.Models.Tickets;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.CrmAccess)]
public sealed class TicketsController : Controller
{
    private readonly ILogger<TicketsController> m_logger;
    private readonly ITicketService m_ticketService;

    public TicketsController(ITicketService in_ticketService, ILogger<TicketsController> in_logger)
    {
        m_ticketService = in_ticketService;
        m_logger = in_logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "id")] Guid? in_ticketId,
        [FromQuery(Name = "status")] TicketStatus? in_status,
        [FromQuery(Name = "q")] string? in_term,
        CancellationToken in_cancellationToken)
    {
        m_logger.LogInformation("Открыт раздел тикетов.");

        var allTickets = await m_ticketService.GetListAsync(null, null, in_cancellationToken);

        var counters = Enum
            .GetValues<TicketStatus>()
            .ToDictionary(status => status, status => allTickets.Count(t => t.Status == status));

        IEnumerable<Ticket> filtered = allTickets;
        if (!string.IsNullOrWhiteSpace(in_term))
        {
            var term = in_term.Trim();
            filtered = filtered.Where(t =>
                t.Subject.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.ClientEmail.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(t.ClientName) && t.ClientName!.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (in_status.HasValue)
        {
            var status = in_status.Value;
            filtered = filtered.Where(t => t.Status == status);
        }

        var ticketItems = filtered
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Select(t => new TicketListItemViewModel
            {
                Id = t.Id,
                Subject = t.Subject,
                ClientEmail = t.ClientEmail,
                ClientName = t.ClientName,
                Status = t.Status,
                UpdatedAtLocal = t.UpdatedAtUtc.ToLocalTime(),
                AwaitingReply = t.Status == TicketStatus.Open
            })
            .ToList();

        TicketDetailsViewModel? selectedTicket = null;
        var selectedId = in_ticketId ?? ticketItems.FirstOrDefault()?.Id;

        if (selectedId.HasValue)
        {
            var ticket = await m_ticketService.GetAsync(selectedId.Value, in_cancellationToken);
            if (ticket is not null)
            {
                selectedTicket = BuildTicketDetails(ticket);
            }
            else
            {
                TempData["Tickets.Error"] = "Тикет не найден или был удалён.";
            }
        }

        var model = new TicketsIndexViewModel
        {
            Tickets = ticketItems,
            SelectedTicket = selectedTicket,
            CurrentFilter = in_status,
            SearchTerm = in_term,
            Counters = counters
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(TicketReplyInputModel in_model, CancellationToken in_cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Tickets.Error"] = "Введите текст ответа";
            return RedirectToAction(nameof(Index), new { id = in_model.TicketId });
        }

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var senderName = User?.Identity?.Name;

        var attachments = new List<TicketAttachmentInputDto>();
        foreach (var file in in_model.Attachments)
        {
            if (file.Length > 0)
            {
                await using var memory = new MemoryStream();
                await file.CopyToAsync(memory, in_cancellationToken);
                attachments.Add(new TicketAttachmentInputDto
                {
                    FileName = file.FileName,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    Content = memory.ToArray()
                });
            }
        }

        var reply = new TicketReplyDto
        {
            Subject = in_model.Subject ?? string.Empty,
            BodyHtml = in_model.Body,
            BodyText = null,
            SenderName = senderName,
            ReplyToExternalId = in_model.ReplyToExternalId,
            Attachments = attachments
        };

        try
        {
            await m_ticketService.AddAgentReplyAsync(in_model.TicketId, reply, userId, in_cancellationToken);
            TempData["Tickets.Success"] = "Ответ отправлен клиенту";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Tickets.Error"] = ex.Message;
            m_logger.LogWarning(ex, "Не удалось отправить ответ по тикету {TicketId}", in_model.TicketId);
        }
        catch (Exception ex)
        {
            TempData["Tickets.Error"] = "Не удалось отправить ответ";
            m_logger.LogError(ex, "Ошибка при отправке ответа по тикету {TicketId}", in_model.TicketId);
        }

        return RedirectToAction(nameof(Index), new { id = in_model.TicketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        [FromForm(Name = "ticketId")] Guid in_ticketId,
        [FromForm(Name = "status")] TicketStatus in_status,
        CancellationToken in_cancellationToken)
    {
        try
        {
            await m_ticketService.UpdateStatusAsync(in_ticketId, in_status, in_cancellationToken);
            TempData["Tickets.Success"] = "Статус тикета обновлён";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Tickets.Error"] = ex.Message;
            m_logger.LogWarning(ex, "Не удалось обновить статус тикета {TicketId}", in_ticketId);
        }
        catch (Exception ex)
        {
            TempData["Tickets.Error"] = "Не удалось обновить статус тикета";
            m_logger.LogError(ex, "Ошибка при обновлении статуса тикета {TicketId}", in_ticketId);
        }

        return RedirectToAction(nameof(Index), new { id = in_ticketId });
    }

    [HttpGet]
    public async Task<IActionResult> Attachment(
        [FromQuery(Name = "ticketId")] Guid in_ticketId,
        [FromQuery(Name = "messageId")] Guid in_messageId,
        [FromQuery(Name = "attachmentId")] Guid in_attachmentId,
        [FromQuery(Name = "inline")] bool in_inline = false,
        CancellationToken in_cancellationToken = default)
    {
        var attachment = await m_ticketService.GetAttachmentAsync(in_ticketId, in_messageId, in_attachmentId, in_cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        var downloadName = in_inline ? null : attachment.FileName;
        if (in_inline)
        {
            return File(attachment.Content, attachment.ContentType);
        }

        return File(attachment.Content, attachment.ContentType, downloadName);
    }

    private TicketDetailsViewModel BuildTicketDetails(Ticket in_ticket)
    {
        var messages = in_ticket.Messages
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new TicketMessageViewModel
            {
                Id = m.Id,
                FromClient = m.FromClient,
                Author = GetAuthorName(in_ticket, m),
                SentAtLocal = m.SentAtUtc.ToLocalTime(),
                BodyHtml = m.BodyHtml,
                Attachments = m.Attachments
                    .Select(a => BuildAttachmentViewModel(in_ticket.Id, m.Id, a))
                    .ToList()
            })
            .ToList();

        var lastClientMessage = in_ticket.Messages
            .Where(m => m.FromClient)
            .OrderByDescending(m => m.SentAtUtc)
            .FirstOrDefault();

        var replyModel = new TicketReplyInputModel
        {
            TicketId = in_ticket.Id,
            Subject = in_ticket.Subject,
            ReplyToExternalId = lastClientMessage?.ExternalId
        };

        return new TicketDetailsViewModel
        {
            Id = in_ticket.Id,
            Subject = in_ticket.Subject,
            ClientEmail = in_ticket.ClientEmail,
            ClientName = in_ticket.ClientName,
            Status = in_ticket.Status,
            CreatedAtLocal = in_ticket.CreatedAtUtc.ToLocalTime(),
            UpdatedAtLocal = in_ticket.UpdatedAtUtc.ToLocalTime(),
            Messages = messages,
            Reply = replyModel
        };
    }

    private static string GetAuthorName(Ticket in_ticket, TicketMessage in_message)
    {
        if (in_message.FromClient)
        {
            if (!string.IsNullOrWhiteSpace(in_message.SenderName))
            {
                return in_message.SenderName;
            }

            if (!string.IsNullOrWhiteSpace(in_ticket.ClientName))
            {
                return in_ticket.ClientName!;
            }

            return in_ticket.ClientEmail;
        }

        return string.IsNullOrWhiteSpace(in_message.SenderName) ? "Команда сервиса" : in_message.SenderName!;
    }

    private TicketAttachmentViewModel BuildAttachmentViewModel(Guid in_ticketId, Guid in_messageId, TicketAttachment in_attachment)
    {
        var preview = IsPreviewSupported(in_attachment.ContentType);
        var previewUrl = Url.Action(
            nameof(Attachment),
            new
            {
                ticketId = in_ticketId,
                messageId = in_messageId,
                attachmentId = in_attachment.Id,
                inline = true
            }) ?? string.Empty;

        var downloadUrl = Url.Action(
            nameof(Attachment),
            new
            {
                ticketId = in_ticketId,
                messageId = in_messageId,
                attachmentId = in_attachment.Id,
                inline = false
            }) ?? string.Empty;

        return new TicketAttachmentViewModel
        {
            TicketId = in_ticketId,
            MessageId = in_messageId,
            AttachmentId = in_attachment.Id,
            FileName = in_attachment.FileName,
            ContentType = in_attachment.ContentType,
            Length = in_attachment.Length,
            PreviewInline = preview,
            PreviewUrl = previewUrl,
            DownloadUrl = downloadUrl
        };
    }

    private static bool IsPreviewSupported(string in_contentType)
    {
        if (string.IsNullOrWhiteSpace(in_contentType))
        {
            return false;
        }

        if (in_contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (in_contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
