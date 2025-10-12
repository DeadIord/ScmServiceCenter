using System.ComponentModel.DataAnnotations;
using Scm.Domain.Entities;

namespace Scm.Application.DTOs;

public sealed class AccountInputDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AccountType Type { get; set; } = AccountType.Company;

    [StringLength(12)]
    [RegularExpression(@"^$|^[0-9]{10}([0-9]{2})?$", ErrorMessage = "ИНН должен содержать 10 или 12 цифр")]
    public string? Inn { get; set; }
        = null;

    [StringLength(500)]
    public string? Address { get; set; }
        = null;

    [StringLength(256)]
    public string? Tags { get; set; }
        = null;

    [StringLength(450)]
    public string? ManagerUserId { get; set; }
        = null;
}
