namespace Sycota.Domain.Entities;

public class BadgeNotification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
    public string BadgeTitle { get; set; } = string.Empty;
    public string BadgeDescription { get; set; } = string.Empty;
    public DateTime UnlockedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
