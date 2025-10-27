using Scm.Domain.Entities;

namespace Scm.Web.Models.Tickets;

public sealed class TicketDetailsViewModel
{
    public Guid Id { get; set; }
        = Guid.Empty;

    public string Subject { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public string? ClientName { get; set; }
        = null;

    public TicketStatus Status { get; set; }
        = TicketStatus.Open;

    public DateTime CreatedAtLocal { get; set; }
        = DateTime.Now;

    public DateTime UpdatedAtLocal { get; set; }
        = DateTime.Now;

    public IReadOnlyList<TicketMessageViewModel> Messages { get; set; }
        = Array.Empty<TicketMessageViewModel>();

    public TicketReplyInputModel Reply { get; set; } = new();
}
