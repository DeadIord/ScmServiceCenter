namespace Scm.Web.Models.Tickets;

public sealed class TicketMessageViewModel
{
    public Guid Id { get; set; }
        = Guid.Empty;

    public bool FromClient { get; set; }
        = true;

    public string Author { get; set; } = string.Empty;

    public DateTime SentAtLocal { get; set; }
        = DateTime.Now;

    public string BodyHtml { get; set; } = string.Empty;

    public IReadOnlyList<TicketAttachmentViewModel> Attachments { get; set; }
        = Array.Empty<TicketAttachmentViewModel>();
}
