using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Models.Users;

public sealed class UserCreateViewModel
{
    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    [EmailAddress(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Email")]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_PasswordsNotMatch")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IList<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
}
