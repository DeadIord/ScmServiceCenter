using System.ComponentModel.DataAnnotations;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Models;

public class UserCreateViewModel : UserEditViewModel
{
    [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
    [StringLength(100, MinimumLength = 6, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(SharedResource))]
    [DataType(DataType.Password)]
    [Display(Name = "User_Password", ResourceType = typeof(SharedResource))]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
    [Compare("Password", ErrorMessageResourceName = "Validation_PasswordMismatch", ErrorMessageResourceType = typeof(SharedResource))]
    [DataType(DataType.Password)]
    [Display(Name = "User_ConfirmPassword", ResourceType = typeof(SharedResource))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
