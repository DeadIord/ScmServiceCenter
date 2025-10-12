using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Areas.Admin.Models.Users;

public class UserRoleOptionViewModel
{
    [Display(Name = "User_RoleName")]
    public string RoleName { get; set; } = string.Empty;

    [Display(Name = "User_RoleDisplay")]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
