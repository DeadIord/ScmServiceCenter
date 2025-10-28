using Scm.Domain.Entities;

namespace Scm.Web.Models.Tickets;

public sealed class TicketsIndexViewModel
{
    public IReadOnlyList<TicketListItemViewModel> Tickets { get; set; }
        = Array.Empty<TicketListItemViewModel>();

    public TicketDetailsViewModel? SelectedTicket { get; set; }
        = null;

    public TicketStatus? CurrentFilter { get; set; }
        = null;

    public string? SearchTerm { get; set; }
        = null;

    public IReadOnlyDictionary<TicketStatus, int> Counters { get; set; }
        = new Dictionary<TicketStatus, int>();
}
