using Microsoft.EntityFrameworkCore;
using Sycota.Domain.Classes;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Web.Services;

public class DbGamificationNotificationService : IGamificationNotificationService
{
    private readonly ApplicationDbContext _dbContext;

    public DbGamificationNotificationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<GamificationNotificationItem> RegisterUnlockedBadges(string userId, int clubId, string? clubName, IEnumerable<GamificationBadge> badges)
    {
        var badgeTitles = badges.Select(b => b.Title).Distinct().ToList();
        if (badgeTitles.Count == 0)
        {
            return [];
        }

        var existingTitles = _dbContext.BadgeNotifications
            .Where(n => n.UserId == userId && n.ClubId == clubId && badgeTitles.Contains(n.BadgeTitle))
            .Select(n => n.BadgeTitle)
            .ToList();

        var existingSet = existingTitles.ToHashSet(StringComparer.Ordinal);
        var created = new List<BadgeNotification>();

        foreach (var badge in badges)
        {
            if (!existingSet.Add(badge.Title))
            {
                continue;
            }

            created.Add(new BadgeNotification
            {
                UserId = userId,
                ClubId = clubId,
                ClubName = clubName,
                BadgeTitle = badge.Title,
                BadgeDescription = badge.Description,
                UnlockedAtUtc = DateTime.UtcNow,
                IsRead = false
            });
        }

        if (created.Count > 0)
        {
            _dbContext.BadgeNotifications.AddRange(created);
            _dbContext.SaveChanges();
        }

        return created.Select(Map).ToList();
    }

    public IReadOnlyList<GamificationNotificationItem> GetNotifications(string userId, bool unreadOnly = false)
    {
        var query = _dbContext.BadgeNotifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var items = query
            .OrderByDescending(n => n.UnlockedAtUtc)
            .ToList();

        return items.Select(Map).ToList();
    }

    public GamificationNotificationItem? MarkAsRead(string userId, string notificationId)
    {
        if (!int.TryParse(notificationId, out var id))
        {
            return null;
        }

        var notification = _dbContext.BadgeNotifications
            .FirstOrDefault(n => n.Id == id && n.UserId == userId);

        if (notification == null)
        {
            return null;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            _dbContext.SaveChanges();
        }

        return Map(notification);
    }

    public int GetUnreadCount(string userId)
    {
        return _dbContext.BadgeNotifications
            .Count(n => n.UserId == userId && !n.IsRead);
    }

    private static GamificationNotificationItem Map(BadgeNotification notification)
    {
        return new GamificationNotificationItem
        {
            Id = notification.Id.ToString(),
            ClubId = notification.ClubId,
            ClubName = notification.ClubName,
            BadgeTitle = notification.BadgeTitle,
            BadgeDescription = notification.BadgeDescription,
            UnlockedAtUtc = notification.UnlockedAtUtc,
            IsRead = notification.IsRead
        };
    }
}
