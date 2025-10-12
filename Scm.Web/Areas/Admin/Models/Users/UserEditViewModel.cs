using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Models.Users;

public sealed class UserEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
    [EmailAddress(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Email")]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public IList<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
}
