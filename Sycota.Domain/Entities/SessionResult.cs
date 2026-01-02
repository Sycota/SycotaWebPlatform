using System;
using System.Collections.Generic;

namespace Sycota.Domain.Entities;

public class SessionResult
{
    public int Id { get; set; }
    public int ClubMemberId { get; set; }
    public int? TrainingSessionId { get; set; }
    public DateTime SessionDate { get; set; } = DateTime.UtcNow;
    public decimal? TotalScore { get; set; }
    public int ShotsCount { get; set; }
    public int SeriesCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ClubMember ClubMember { get; set; } = null!;
    public TrainingSession? TrainingSession { get; set; }
    public ICollection<Shot> Shots { get; set; } = new List<Shot>();
}