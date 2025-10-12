using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Areas.Admin.Models.Users;

public class UserCreateViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "User_Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "User_DisplayName")]
    [StringLength(128)]
    public string? DisplayName { get; set; }

    [Phone]
    [Display(Name = "User_Phone")]
    public string? PhoneNumber { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(64, MinimumLength = 6)]
    [Display(Name = "User_Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password")]
    [Display(Name = "User_ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "User_IsLocked")]
    public bool IsLocked { get; set; }

    public IList<UserRoleOptionViewModel> Roles { get; set; } = new List<UserRoleOptionViewModel>();
}
