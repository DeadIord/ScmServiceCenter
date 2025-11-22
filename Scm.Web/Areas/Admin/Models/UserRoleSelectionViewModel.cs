using System.ComponentModel.DataAnnotations;
namespace Scm.Web.Areas.Admin.Models;

public class UserRoleSelectionViewModel
{
    [Display(Name = "Role_Name")]
    public string RoleName { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    [Display(Name = "Role_Selected")]
    public bool Selected { get; set; }
}
