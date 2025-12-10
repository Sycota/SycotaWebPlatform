using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

public class TrainingScore
{
    public int Id { get; set; }
    public int TrainingSessionId { get; set; }
    public int ClubMemberId { get; set; } 
    public ISSFWeaponType WeaponType { get; set; }

    public decimal TotalScore { get; set; } // Sum of all shot scores
    public int ShotsCount { get; set; } // Number of shots fired
    public decimal AverageScore { get; set; } // Average score per shot
    
    public int SeriesCount { get; set; } // Number of series
    public string? SeriesScores { get; set; } // JSON array of series totals
    
    public string? Notes { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? SubmittedById { get; set; } // Who submitted
    
    // Navigation properties
    public TrainingSession TrainingSession { get; set; } = null!;
    public ClubMember ClubMember { get; set; } = null!;
    public ApplicationUser? SubmittedBy { get; set; }
    public ICollection<Shot> Shots { get; set; } = new List<Shot>(); // Individual shot details
}

