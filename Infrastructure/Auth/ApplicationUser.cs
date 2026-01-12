using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Auth;

public class ApplicationUser : IdentityUser<Guid>
{
}

public class ApplicationRole : IdentityRole<Guid>
{
}
