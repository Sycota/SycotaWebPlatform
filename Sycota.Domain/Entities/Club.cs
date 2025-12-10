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
    
    // Navigation properties
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
    public ICollection<TrainingSession> TrainingSessions { get; set; } = new List<TrainingSession>();
}

