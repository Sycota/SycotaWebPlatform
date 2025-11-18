using Microsoft.AspNetCore.Identity;

namespace Sycota.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    // Username and Email are already in IdentityUser
    // We'll make them required through configuration
}

