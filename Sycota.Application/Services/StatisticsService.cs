using System;
using System.Linq;
using System.Threading.Tasks;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Classes;

namespace Sycota.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ISessionResultRepository _sessionRepo;
    private readonly IShotRepository _shotRepo;

    public StatisticsService(ISessionResultRepository sessionRepo, IShotRepository shotRepo)
    {
        _sessionRepo = sessionRepo;
        _shotRepo = shotRepo;
    }

    public async Task<SessionStatistics?> ComputeSessionStatisticsAsync(int sessionResultId)
    {
        var session = await _sessionRepo.GetSessionResultByIdAsync(sessionResultId, SessionResultIncludeOptions.Shots);
        if (session is null) return null;

        var shots = session.Shots.OrderBy(s => s.SeriesIndex).ThenBy(s => s.ShotIndex).ToList();
        if (!shots.Any())
        {
            return new SessionStatistics
            {
                SessionResultId = session.Id,
                TotalScore = 0,
                AverageShot = 0,
                StandardDeviation = 0,
                ShotsCount = 0,
                SeriesCount = session.SeriesCount,
                BestSeriesIndex = 0,
                BestSeriesScore = 0,
                SessionDate = session.SessionDate
            };
        }

        var values = shots.Select(s => (double)s.DistanceMm).ToArray();
        var total = shots.Sum(s => (double)s.DistanceMm);
        var avg = shots.Average(s => (double)s.DistanceMm);
        var variance = shots.Count > 1 ? shots.Select(v => Math.Pow((double)v.DistanceMm - avg, 2)).Sum() / (shots.Count - 1) : 0.0;
        var stddev = Math.Sqrt(variance);

        var seriesGroups = shots.GroupBy(s => s.SeriesIndex)
                               .Select(g => new { Series = g.Key, Sum = g.Sum(s => (double)s.DistanceMm) })
                               .OrderByDescending(x => x.Sum)
                               .ToList();

        var bestSeries = seriesGroups.First();

        return new SessionStatistics
        {
            SessionResultId = session.Id,
            TotalScore = (decimal)total,
            AverageShot = (decimal)avg,
            StandardDeviation = (decimal)stddev,
            ShotsCount = shots.Count,
            SeriesCount = session.SeriesCount,
            BestSeriesIndex = bestSeries.Series,
            BestSeriesScore = (decimal)bestSeries.Sum,
            SessionDate = session.SessionDate
        };
    }

    public async Task<ShooterStatistics> ComputeShooterStatisticsAsync(int clubMemberId, DateTime? from = null, DateTime? to = null)
    {
        var sessions = (await _sessionRepo.GetAllSessionResultsByClubMemberIdAsync(clubMemberId, from, to, SessionResultIncludeOptions.Shots))
                       .OrderBy(s => s.SessionDate)
                       .ToList();

        var allShots = sessions.SelectMany(s => s.Shots).ToList();

        var totalShots = allShots.Count;
        var averageShot = totalShots == 0 ? 0m : Math.Round((decimal)allShots.Average(s => s.DistanceMm), 2);
        var sessionTotals = sessions.Select(s => s.Shots.Sum(sh => (double)sh.DistanceMm)).ToList();
        var averageSessionScore = sessionTotals.Count == 0 ? 0m : Math.Round((decimal)sessionTotals.Average(), 2);
        var bestSessionScore = sessionTotals.Count == 0 ? 0m : Math.Round((decimal)sessionTotals.Max(), 2);
        var bestSessionDate = sessions.Count == 0 ? (DateTime?)null : sessions.OrderByDescending(s => s.Shots.Sum(sh => (double)sh.DistanceMm)).First().SessionDate;

        return new ShooterStatistics
        {
            ClubMemberId = clubMemberId,
            SessionsCount = sessions.Count,
            TotalShots = totalShots,
            AverageShot = averageShot,
            AverageSessionScore = averageSessionScore,
            BestSessionScore = bestSessionScore,
            BestSessionDate = bestSessionDate
        };
    }
}