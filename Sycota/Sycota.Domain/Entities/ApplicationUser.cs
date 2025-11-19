using Microsoft.AspNetCore.Identity;

namespace Sycota.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DOB { get; set; }
    public string? Nationality { get; set; }
}


