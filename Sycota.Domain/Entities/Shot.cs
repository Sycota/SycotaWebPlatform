namespace Sycota.Domain.Entities;

public class Shot
{
    public int Id { get; set; }
    public int TrainingScoreId { get; set; }
    public int SeriesNumber { get; set; } // Which series (1-6 for qualification, 1-2 for final)
    public int ShotNumber { get; set; } // Shot number within the series (1-10 for qualification, 1-12 for final)
    public decimal Score { get; set; } // ISSF score: 0.0 to 10.9 (increments of 0.1)
    public int? ShotOrder { get; set; } // Overall shot order in the training session (1-60 for qualification, 1-24 for final)
    
    // Navigation properties
    public TrainingScore TrainingScore { get; set; } = null!;
}
