using System.ComponentModel.DataAnnotations;
using Scm.Domain.Entities;

namespace Scm.Application.DTOs;

public sealed class CreateOrderDto
{
    [Required]
    [StringLength(128)]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    [StringLength(11, MinimumLength = 11)]
    [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "Телефон должен содержать 11 цифр")]
    public string ClientPhone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(128)]
    public string ClientEmail { get; set; } = string.Empty;

    public Guid? AccountId { get; set; }
        = null;

    public Guid? ContactId { get; set; }
        = null;

    [Required]
    [StringLength(128)]
    public string Device { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Serial { get; set; }

    [Required]
    [StringLength(500)]
    public string Defect { get; set; } = string.Empty;

    [Required]
    public OrderPriority Priority { get; set; } = OrderPriority.Normal;
}
