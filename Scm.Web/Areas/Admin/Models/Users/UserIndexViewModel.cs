using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Areas.Admin.Models.Users;

public class UserIndexViewModel
{
    [Display(Name = "User_Search")]
    public string? Query { get; set; }

    public IList<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
}
