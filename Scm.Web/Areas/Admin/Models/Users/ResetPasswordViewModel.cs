using System.ComponentModel.DataAnnotations;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Models.Users;

public sealed class ResetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_PasswordsNotMatch")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
