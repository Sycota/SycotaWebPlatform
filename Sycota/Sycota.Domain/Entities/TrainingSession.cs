namespace Sycota.Domain.Entities;

public class TrainingSession
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime SessionDate { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Club Club { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<TrainingScore> Scores { get; set; } = new List<TrainingScore>();
}

