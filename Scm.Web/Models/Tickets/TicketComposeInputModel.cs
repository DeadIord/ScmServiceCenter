using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Scm.Web.Models.Tickets;

public sealed class TicketComposeInputModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
        = string.Empty;

    public string? ClientName { get; set; }
        = null;

    [Required]
    [Display(Name = "Subject")]
    public string Subject { get; set; }
        = string.Empty;

    [Required]
    [DataType(DataType.MultilineText)]
    public string Body { get; set; }
        = string.Empty;

    public IList<IFormFile> Attachments { get; set; }
        = new List<IFormFile>();
}
