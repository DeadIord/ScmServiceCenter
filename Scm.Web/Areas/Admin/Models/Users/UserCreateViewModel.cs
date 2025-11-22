using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Models.Users;

public class UserCreateViewModel
{
    [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
    [EmailAddress(ErrorMessageResourceName = "Validation_Email", ErrorMessageResourceType = typeof(SharedResource))]
    [Display(Name = "User_Email", ResourceType = typeof(SharedResource))]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "User_DisplayName", ResourceType = typeof(SharedResource))]
    [StringLength(128)]
    public string? DisplayName { get; set; }

    [Phone(ErrorMessageResourceName = "Validation_Phone", ErrorMessageResourceType = typeof(SharedResource))]
    [Display(Name = "User_Phone", ResourceType = typeof(SharedResource))]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
    [DataType(DataType.Password)]
    [StringLength(64, MinimumLength = 6, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(SharedResource))]
    [Display(Name = "User_Password", ResourceType = typeof(SharedResource))]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessageResourceName = "Validation_PasswordMismatch", ErrorMessageResourceType = typeof(SharedResource))]
    [Display(Name = "User_ConfirmPassword", ResourceType = typeof(SharedResource))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "User_IsLocked", ResourceType = typeof(SharedResource))]
    public bool IsLocked { get; set; }

    public IList<UserRoleOptionViewModel> Roles { get; set; } = new List<UserRoleOptionViewModel>();
}
