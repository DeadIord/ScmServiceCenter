using System.ComponentModel.DataAnnotations;
namespace Scm.Web.Areas.Admin.Models;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "User_DisplayName")]
    public string? DisplayName { get; set; }

    [Display(Name = "User_UserName")]
    public string? UserName { get; set; }

    [Display(Name = "User_Email")]
    public string? Email { get; set; }

    [Display(Name = "User_Phone")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Role_List")]
    public IList<string> Roles { get; set; } = new List<string>();

    [Display(Name = "User_IsActive")]
    public bool IsActive { get; set; }
}
