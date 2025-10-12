using System;

namespace Scm.Web.Areas.Admin.Models.Users;

public sealed class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
