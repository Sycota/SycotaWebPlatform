namespace Sycota.Domain.Classes;

public class GamificationProgress
{
    public List<GamificationBadge> Badges { get; set; } = [];
    public string? NextMilestone { get; set; }
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public int NextLevelXp { get; set; }
    public string RankTitle { get; set; } = string.Empty;
    public int CurrentStreakDays { get; set; }
    public int BestStreakDays { get; set; }
    public int WeeklyChallengeTarget { get; set; } = 3;
    public int WeeklyChallengeProgress { get; set; }
}
