using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Areas.Admin.Models.Users;

public sealed class RoleAssignmentInputModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public IList<string> SelectedRoles { get; set; } = new List<string>();
}
