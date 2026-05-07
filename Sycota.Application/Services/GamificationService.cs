using System.Text.Json;
using Sycota.Application.Interfaces;
using Sycota.Domain.Classes;
using Sycota.Domain.Entities;

namespace Sycota.Application.Services;

public class GamificationService : IGamificationService
{
    public GamificationProgress Calculate(IEnumerable<TrainingSession> sessions, ShooterProfile? shooterProfile, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var sessionList = sessions.OrderByDescending(s => s.SessionDate).ToList();
        var badges = new List<GamificationBadge>();

        var sessionsCount = sessionList.Count;
        var last30DaysCount = sessionList.Count(s => s.SessionDate >= now.AddDays(-30));
        var uniqueDays = sessionList.Select(s => s.SessionDate.Date).Distinct().OrderBy(d => d).ToList();
        var weekendSessions = sessionList.Count(s => s.SessionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        var earlyBirdSessions = sessionList.Count(s => s.SessionDate.Hour < 9);
        var eveningSessions = sessionList.Count(s => s.SessionDate.Hour >= 20);
        var weaponVariety = sessionList.Select(s => s.WeaponType).Distinct().Count();
        var currentMonthSessions = sessionList.Count(s => s.SessionDate.Year == now.Year && s.SessionDate.Month == now.Month);
        var weeklyStart = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        var weeklyProgress = sessionList.Count(s => s.SessionDate.Date >= weeklyStart);
        var totalShots = sessionList.Sum(s => CountSeriesShots(s.Shots));

        var currentStreak = CalculateCurrentStreak(uniqueDays);
        var bestStreak = CalculateBestStreak(uniqueDays);

        AddBadgeIf(badges, sessionsCount >= 1, "Първа стъпка", "Записахте първа тренировъчна сесия.", "success");
        AddBadgeIf(badges, sessionsCount >= 5, "Постоянство", "Имате поне 5 записани сесии.", "primary");
        AddBadgeIf(badges, sessionsCount >= 20, "Напреднал стрелец", "Имате поне 20 тренировъчни сесии.", "warning text-dark");
        AddBadgeIf(badges, sessionsCount >= 50, "Желязна дисциплина", "Имате поне 50 тренировъчни сесии.", "danger");
        AddBadgeIf(badges, last30DaysCount >= 8, "Активен този месец", "Поне 8 сесии през последните 30 дни.", "info");
        AddBadgeIf(badges, !string.IsNullOrWhiteSpace(shooterProfile?.ISSFLicenseNumber), "Лицензиран", "Добавен ISSF лиценз в профила.", "secondary");
        AddBadgeIf(badges, bestStreak >= 7, "7-дневна серия", "Тренирахте 7 поредни дни.", "dark");
        AddBadgeIf(badges, bestStreak >= 14, "14-дневна серия", "Тренирахте 14 поредни дни.", "warning text-dark");
        AddBadgeIf(badges, weekendSessions >= 4, "Уикенд войн", "Поне 4 уикенд тренировки.", "secondary");
        AddBadgeIf(badges, earlyBirdSessions >= 5, "Ранобуден", "Поне 5 сутрешни тренировки преди 09:00.", "info");
        AddBadgeIf(badges, eveningSessions >= 5, "Вечерна смяна", "Поне 5 вечерни тренировки след 20:00.", "primary");
        AddBadgeIf(badges, weaponVariety >= 2, "Универсален стрелец", "Тренирате с повече от един тип оръжие.", "success");
        AddBadgeIf(badges, totalShots >= 100, "100 изстрела", "Записани поне 100 изстрела.", "primary");
        AddBadgeIf(badges, totalShots >= 500, "500 изстрела", "Записани поне 500 изстрела.", "danger");
        AddBadgeIf(badges, currentMonthSessions >= 12, "Месечен маратон", "Поне 12 тренировки в текущия месец.", "warning text-dark");

        var milestoneTargets = new[] { 1, 5, 10, 25, 50, 100, 200 };
        var nextTarget = milestoneTargets.FirstOrDefault(t => t > sessionsCount);
        var nextMilestone = nextTarget > 0
            ? $"Следваща цел: {nextTarget} сесии ({nextTarget - sessionsCount} оставащи)."
            : "Постигнахте всички текущи етапи!";

        var totalXp = sessionsCount * 40
                      + last30DaysCount * 10
                      + totalShots / 5
                      + currentStreak * 20
                      + badges.Count * 25;
        var level = Math.Max(1, (totalXp / 250) + 1);
        var nextLevelXp = level * 250;

        var rankTitle = level switch
        {
            <= 2 => "Новобранец",
            <= 4 => "Кадет",
            <= 7 => "Състезател",
            <= 10 => "Експерт",
            _ => "Елитен стрелец"
        };

        return new GamificationProgress
        {
            Badges = badges,
            NextMilestone = nextMilestone,
            TotalXp = totalXp,
            Level = level,
            NextLevelXp = nextLevelXp,
            RankTitle = rankTitle,
            CurrentStreakDays = currentStreak,
            BestStreakDays = bestStreak,
            WeeklyChallengeTarget = 3,
            WeeklyChallengeProgress = weeklyProgress
        };
    }

    private static void AddBadgeIf(List<GamificationBadge> badges, bool condition, string title, string description, string colorClass)
    {
        if (!condition)
        {
            return;
        }

        badges.Add(new GamificationBadge
        {
            Title = title,
            Description = description,
            ColorClass = colorClass
        });
    }

    private static int CalculateCurrentStreak(IReadOnlyList<DateTime> uniqueDays)
    {
        if (uniqueDays.Count == 0)
        {
            return 0;
        }

        var cursor = uniqueDays[^1];
        var streak = 1;

        for (var i = uniqueDays.Count - 2; i >= 0; i--)
        {
            if ((cursor - uniqueDays[i]).TotalDays == 1)
            {
                streak++;
                cursor = uniqueDays[i];
            }
            else if ((cursor - uniqueDays[i]).TotalDays > 1)
            {
                break;
            }
        }

        return streak;
    }

    private static int CalculateBestStreak(IReadOnlyList<DateTime> uniqueDays)
    {
        if (uniqueDays.Count == 0)
        {
            return 0;
        }

        var run = 1;
        var best = 1;

        for (var i = 1; i < uniqueDays.Count; i++)
        {
            if ((uniqueDays[i] - uniqueDays[i - 1]).TotalDays == 1)
            {
                run++;
                best = Math.Max(best, run);
            }
            else
            {
                run = 1;
            }
        }

        return best;
    }

    private static int CountSeriesShots(string? shotsJson)
    {
        if (string.IsNullOrWhiteSpace(shotsJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(shotsJson);
            if (!document.RootElement.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array)
            {
                return 0;
            }

            var count = 0;
            foreach (var group in groups.EnumerateArray())
            {
                if (!group.TryGetProperty("valueType", out var valueType) ||
                    !string.Equals(valueType.GetString(), "10-shot-series", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (group.TryGetProperty("shots", out var shots) && shots.ValueKind == JsonValueKind.Array)
                {
                    count += shots.GetArrayLength();
                }
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }
}
