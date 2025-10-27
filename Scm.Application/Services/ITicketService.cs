using Scm.Application.DTOs;
using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface ITicketService
{
    Task<List<Ticket>> GetListAsync(
        string? in_term = null,
        TicketStatus? in_status = null,
        CancellationToken in_cancellationToken = default);

    Task<Ticket?> GetAsync(
        Guid in_ticketId,
        CancellationToken in_cancellationToken = default);

    Task<TicketMessage> AddAgentReplyAsync(
        Guid in_ticketId,
        TicketReplyDto in_reply,
        string in_userId,
        CancellationToken in_cancellationToken = default);

    Task<TicketMessage?> IngestEmailAsync(
        InboundTicketMessageDto in_message,
        CancellationToken in_cancellationToken = default);

    Task<TicketAttachment?> GetAttachmentAsync(
        Guid in_ticketId,
        Guid in_messageId,
        Guid in_attachmentId,
        CancellationToken in_cancellationToken = default);

    Task UpdateStatusAsync(
        Guid in_ticketId,
        TicketStatus in_status,
        CancellationToken in_cancellationToken = default);
}
