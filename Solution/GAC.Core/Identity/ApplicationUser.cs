using Microsoft.AspNetCore.Identity;

namespace GAC.Core.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
