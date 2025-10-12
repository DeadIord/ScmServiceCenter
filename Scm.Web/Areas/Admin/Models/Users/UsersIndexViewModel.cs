using System;

namespace Scm.Web.Areas.Admin.Models.Users;

public sealed class UsersIndexViewModel
{
    public IReadOnlyCollection<UserListItemViewModel> Users { get; set; } = Array.Empty<UserListItemViewModel>();

    public IReadOnlyCollection<string> RoleNames { get; set; } = Array.Empty<string>();

    public string RoleRulesDescription { get; set; } = string.Empty;
}
