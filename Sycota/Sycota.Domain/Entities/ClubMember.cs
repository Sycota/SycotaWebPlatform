using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

public class ClubMember
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ClubId { get; set; }
    public ClubRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // For trainers: which competitors they train
    public int? TrainerId { get; set; } // If this member is a competitor, this links to their trainer (ClubMember.Id)
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public ClubMember? Trainer { get; set; } // Trainer navigation property
    public ICollection<ClubMember> Competitors { get; set; } = new List<ClubMember>(); // Competitors trained by this trainer
    public ShooterProfile? ShooterProfile { get; set; } // Additional profile info for competitors
}

