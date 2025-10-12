using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Areas.Admin.Models.Users;

public sealed class UserStatusInputModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
