using Sycota.Domain.Classes;

namespace Sycota.Web.Services;

public interface IGamificationNotificationService
{
    IReadOnlyList<GamificationNotificationItem> RegisterUnlockedBadges(string userId, int clubId, string? clubName, IEnumerable<GamificationBadge> badges);
    IReadOnlyList<GamificationNotificationItem> GetNotifications(string userId, bool unreadOnly = false);
    GamificationNotificationItem? MarkAsRead(string userId, string notificationId);
    int GetUnreadCount(string userId);
}

public class GamificationNotificationItem
{
    public string Id { get; set; } = string.Empty;
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
    public string BadgeTitle { get; set; } = string.Empty;
    public string BadgeDescription { get; set; } = string.Empty;
    public DateTime UnlockedAtUtc { get; set; }
    public bool IsRead { get; set; }
}
