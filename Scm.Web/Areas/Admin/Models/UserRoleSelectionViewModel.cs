using System.ComponentModel.DataAnnotations;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Models;

public class UserRoleSelectionViewModel
{
    [Display(Name = "Role_Name", ResourceType = typeof(SharedResource))]
    public string RoleName { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    [Display(Name = "Role_Selected", ResourceType = typeof(SharedResource))]
    public bool Selected { get; set; }
}
