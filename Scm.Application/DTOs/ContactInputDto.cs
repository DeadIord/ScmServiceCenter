using System.ComponentModel.DataAnnotations;

namespace Scm.Application.DTOs;

public sealed class ContactInputDto
{
    [Required]
    public Guid AccountId { get; set; }
        = Guid.Empty;

    [Required]
    [StringLength(256)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(128)]
    public string? Position { get; set; }
        = null;

    [Required]
    [StringLength(64)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(128)]
    public string Email { get; set; } = string.Empty;
}
