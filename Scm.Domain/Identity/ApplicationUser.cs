using Microsoft.AspNetCore.Identity;

namespace Scm.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
