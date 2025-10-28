using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Scm.Web.Models.Tickets;

public sealed class TicketReplyInputModel
{
    public const string FormFieldPrefix = "Reply";

    [Required]
    public Guid TicketId { get; set; }
        = Guid.Empty;

    [Display(Name = "Subject")]
    public string? Subject { get; set; }
        = null;

    [Required]
    [DataType(DataType.MultilineText)]
    public string Body { get; set; } = string.Empty;

    public string? ReplyToExternalId { get; set; }
        = null;

    public IList<IFormFile> Attachments { get; set; }
        = new List<IFormFile>();
}
