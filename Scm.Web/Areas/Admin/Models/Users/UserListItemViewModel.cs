using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Areas.Admin.Models.Users;

public class UserListItemViewModel
{
    [Display(Name = "User_Id")]
    public string Id { get; set; } = string.Empty;

    [Display(Name = "User_DisplayName")]
    public string? DisplayName { get; set; }

    [Display(Name = "User_Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "User_Phone")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "User_IsLocked")]
    public bool IsLocked { get; set; }

    [Display(Name = "User_Roles")]
    public IList<string> Roles { get; set; } = new List<string>();
}
