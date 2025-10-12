using System.ComponentModel.DataAnnotations;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Models;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "User_DisplayName", ResourceType = typeof(SharedResource))]
    public string? DisplayName { get; set; }

    [Display(Name = "User_UserName", ResourceType = typeof(SharedResource))]
    public string? UserName { get; set; }

    [Display(Name = "User_Email", ResourceType = typeof(SharedResource))]
    public string? Email { get; set; }

    [Display(Name = "User_Phone", ResourceType = typeof(SharedResource))]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Role_List", ResourceType = typeof(SharedResource))]
    public IList<string> Roles { get; set; } = new List<string>();

    [Display(Name = "User_IsActive", ResourceType = typeof(SharedResource))]
    public bool IsActive { get; set; }
}
