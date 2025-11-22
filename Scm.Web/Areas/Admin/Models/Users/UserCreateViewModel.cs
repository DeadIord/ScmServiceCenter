using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace Scm.Web.Areas.Admin.Models.Users;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    [Display(Name = "User_Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "User_DisplayName")]
    [StringLength(128)]
    public string? DisplayName { get; set; }

    [Phone(ErrorMessage = "Validation_Phone")]
    [Display(Name = "User_Phone")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [DataType(DataType.Password)]
    [StringLength(64, MinimumLength = 6, ErrorMessage = "Validation_StringLength")]
    [Display(Name = "User_Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Validation_PasswordMismatch")]
    [Display(Name = "User_ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "User_IsLocked")]
    public bool IsLocked { get; set; }

    public IList<UserRoleOptionViewModel> Roles { get; set; } = new List<UserRoleOptionViewModel>();
}
