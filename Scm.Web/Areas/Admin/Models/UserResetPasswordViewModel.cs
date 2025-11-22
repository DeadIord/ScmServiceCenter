using System.ComponentModel.DataAnnotations;
namespace Scm.Web.Areas.Admin.Models;

public class UserResetPasswordViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "User_DisplayName")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Validation_StringLength")]
    [DataType(DataType.Password)]
    [Display(Name = "User_Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [Compare("Password", ErrorMessage = "Validation_PasswordMismatch")]
    [DataType(DataType.Password)]
    [Display(Name = "User_ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
