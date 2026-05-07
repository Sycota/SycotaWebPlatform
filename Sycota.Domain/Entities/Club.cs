namespace Sycota.Domain.Entities;

public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedById { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; } = true; // If true, join requests need admin approval
    
    // Navigation properties
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
    public ICollection<TrainingSession> TrainingSessions { get; set; } = new List<TrainingSession>();
    public ICollection<ClubAnnouncement> Announcements { get; set; } = new List<ClubAnnouncement>();
    public ICollection<ClubWeapon> Weapons { get; set; } = new List<ClubWeapon>();
    public ICollection<ClubAmmo> AmmoBatches { get; set; } = new List<ClubAmmo>();
    public ICollection<InventoryIssue> InventoryIssues { get; set; } = new List<InventoryIssue>();
    public ICollection<ClubJoinRequest> JoinRequests { get; set; } = new List<ClubJoinRequest>();
    public ICollection<ClubInvitation> Invitations { get; set; } = new List<ClubInvitation>();
}

