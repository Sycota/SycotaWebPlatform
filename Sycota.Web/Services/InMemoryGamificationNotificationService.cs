using System.Collections.Concurrent;
using Sycota.Domain.Classes;

namespace Sycota.Web.Services;

public class InMemoryGamificationNotificationService : IGamificationNotificationService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, GamificationNotificationItem>> _store = new();

    public IReadOnlyList<GamificationNotificationItem> RegisterUnlockedBadges(string userId, int clubId, string? clubName, IEnumerable<GamificationBadge> badges)
    {
        var userNotifications = _store.GetOrAdd(userId, _ => new ConcurrentDictionary<string, GamificationNotificationItem>());
        var created = new List<GamificationNotificationItem>();

        foreach (var badge in badges)
        {
            var key = $"{clubId}:{badge.Title}";
            if (userNotifications.ContainsKey(key))
            {
                continue;
            }

            var item = new GamificationNotificationItem
            {
                Id = key,
                ClubId = clubId,
                ClubName = clubName,
                BadgeTitle = badge.Title,
                BadgeDescription = badge.Description,
                UnlockedAtUtc = DateTime.UtcNow,
                IsRead = false
            };

            if (userNotifications.TryAdd(key, item))
            {
                created.Add(item);
            }
        }

        return created;
    }

    public IReadOnlyList<GamificationNotificationItem> GetNotifications(string userId, bool unreadOnly = false)
    {
        if (!_store.TryGetValue(userId, out var userNotifications))
        {
            return [];
        }

        var items = userNotifications.Values.AsEnumerable();
        if (unreadOnly)
        {
            items = items.Where(n => !n.IsRead);
        }

        return items.OrderByDescending(n => n.UnlockedAtUtc).ToList();
    }

    public GamificationNotificationItem? MarkAsRead(string userId, string notificationId)
    {
        if (!_store.TryGetValue(userId, out var userNotifications) ||
            !userNotifications.TryGetValue(notificationId, out var item))
        {
            return null;
        }

        item.IsRead = true;
        return item;
    }

    public int GetUnreadCount(string userId)
    {
        if (!_store.TryGetValue(userId, out var userNotifications))
        {
            return 0;
        }

        return userNotifications.Values.Count(n => !n.IsRead);
    }
}
