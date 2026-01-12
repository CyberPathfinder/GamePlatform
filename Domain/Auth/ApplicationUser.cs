using Microsoft.AspNetCore.Identity;

namespace Domain.Auth;

public class ApplicationUser : IdentityUser<Guid>
{
}

public class ApplicationRole : IdentityRole<Guid>
{
}
