using Sycota.Domain.Entities;
using Sycota.Domain.Enums;

namespace Sycota.Web.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalClubs { get; set; }
    public int TotalSessions { get; set; }
    public int TotalMembers { get; set; }
    public IEnumerable<ApplicationUser> RecentUsers { get; set; } = [];
    public IEnumerable<Club> RecentClubs { get; set; } = [];
}

public class AdminUsersViewModel
{
    public IEnumerable<ApplicationUser> Users { get; set; } = [];
    public string? SearchTerm { get; set; }
}

public class AdminEditUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DOB { get; set; }
    public string? Nationality { get; set; }
    public bool IsAdmin { get; set; }
    public bool EmailConfirmed { get; set; }
}

public class AdminClubsViewModel
{
    public IEnumerable<Club> Clubs { get; set; } = [];
    public string? SearchTerm { get; set; }
}

public class AdminEditClubViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool RequiresApproval { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

public class AdminSessionsViewModel
{
    public IEnumerable<TrainingSession> Sessions { get; set; } = [];
    public string? SearchTerm { get; set; }
}

public class AdminEditSessionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime SessionDate { get; set; }
    public ISSFWeaponType WeaponType { get; set; }
    public string? Notes { get; set; }
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? CreatedByName { get; set; }
}

public class AdminMembersViewModel
{
    public IEnumerable<ClubMember> Members { get; set; } = [];
    public string? SearchTerm { get; set; }
}
