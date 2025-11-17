using System.ComponentModel.DataAnnotations;
using Scm.Domain.Entities;

namespace Scm.Application.DTOs;

public sealed class AddQuoteLineDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public QuoteLineKind Kind { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "99999999")]
    public decimal Qty { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    public decimal Price { get; set; }
}
