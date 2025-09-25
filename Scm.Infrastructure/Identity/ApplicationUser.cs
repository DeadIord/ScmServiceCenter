using Microsoft.AspNetCore.Identity;

namespace Scm.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
