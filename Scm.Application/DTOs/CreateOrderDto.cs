using System.ComponentModel.DataAnnotations;
using Scm.Domain.Entities;

namespace Scm.Application.DTOs;

public sealed class CreateOrderDto
{
    [Required]
    [StringLength(128)]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(32)]
    public string ClientPhone { get; set; } = string.Empty;

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
