using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sycota.Application.Interfaces;
using Sycota.Application.Interfaces.Options;
using Sycota.Application.Services;
using Sycota.Domain.Entities;
using Sycota.Domain.Classes;
using Xunit;

namespace Sycota.Tests.Unit.Services;

internal class FakeSessionResultRepository : ISessionResultRepository
{
    private readonly List<SessionResult> _store = new();

    public void Seed(SessionResult sr) => _store.Add(sr);

    public Task AddSessionResultAsync(SessionResult sessionResult)
    {
        _store.Add(sessionResult);
        return Task.CompletedTask;
    }

    public Task UpdateSessionResultAsync(SessionResult sessionResult)
    {
        var existing = _store.FirstOrDefault(s => s.Id == sessionResult.Id);
        if (existing != null)
        {
            _store.Remove(existing);
            _store.Add(sessionResult);
        }
        return Task.CompletedTask;
    }

    public Task DeleteSessionResultAsync(SessionResult sessionResult)
    {
        _store.Remove(sessionResult);
        return Task.CompletedTask;
    }

    public Task DeleteSessionResultByIdAsync(int sessionResultId)
    {
        var e = _store.FirstOrDefault(s => s.Id == sessionResultId);
        if (e != null) _store.Remove(e);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<SessionResult>> GetAllSessionResultsAsync(SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
        => Task.FromResult<IEnumerable<SessionResult>>(_store);

    public Task<IEnumerable<SessionResult>> GetAllSessionResultsByClubMemberIdAsync(int clubMemberId, DateTime? from = null, DateTime? to = null, SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
    {
        var q = _store.Where(s => s.ClubMemberId == clubMemberId);
        if (from.HasValue) q = q.Where(s => s.SessionDate >= from.Value);
        if (to.HasValue) q = q.Where(s => s.SessionDate <= to.Value);
        return Task.FromResult<IEnumerable<SessionResult>>(q);
    }

    public Task<IEnumerable<SessionResult>> GetAllSessionResultsByTrainingSessionIdAsync(int trainingSessionId, SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
        => Task.FromResult<IEnumerable<SessionResult>>(_store.Where(s => s.TrainingSessionId == trainingSessionId));

    public Task<SessionResult?> GetSessionResultByIdAsync(int sessionResultId, SessionResultIncludeOptions include = SessionResultIncludeOptions.None)
    {
        var sr = _store.FirstOrDefault(s => s.Id == sessionResultId);
        return Task.FromResult<SessionResult?>(sr);
    }
}

internal class FakeShotRepository : IShotRepository
{
    private readonly List<Shot> _shots = new();

    public void Seed(IEnumerable<Shot> shots) => _shots.AddRange(shots);

    public Task AddShotAsync(Shot shot)
    {
        _shots.Add(shot);
        return Task.CompletedTask;
    }

    public Task AddShotsAsync(IEnumerable<Shot> shots)
    {
        _shots.AddRange(shots);
        return Task.CompletedTask;
    }

    public Task DeleteShotAsync(Shot shot)
    {
        _shots.Remove(shot);
        return Task.CompletedTask;
    }

    public Task DeleteShotsBySessionResultIdAsync(int sessionResultId)
    {
        _shots.RemoveAll(s => s.SessionResultId == sessionResultId);
        return Task.CompletedTask;
    }

    public Task<Shot?> GetShotByIdAsync(int shotId, Application.Interfaces.Options.ShotIncludeOptions include = Application.Interfaces.Options.ShotIncludeOptions.None)
    {
        return Task.FromResult(_shots.FirstOrDefault(s => s.Id == shotId));
    }

    public Task<IEnumerable<Shot>> GetAllShotsByClubMemberIdAsync(int clubMemberId, DateTime? from = null, DateTime? to = null, Application.Interfaces.Options.ShotIncludeOptions include = Application.Interfaces.Options.ShotIncludeOptions.None)
    {
        // rely on session id linkage in seeded session results
        return Task.FromResult<IEnumerable<Shot>>(_shots);
    }

    public Task<IEnumerable<Shot>> GetAllShotsBySessionResultIdAsync(int sessionResultId, Application.Interfaces.Options.ShotIncludeOptions include = Application.Interfaces.Options.ShotIncludeOptions.None)
    {
        return Task.FromResult<IEnumerable<Shot>>(_shots.Where(s => s.SessionResultId == sessionResultId));
    }

    public Task UpdateShotAsync(Shot shot)
    {
        // no-op for fake
        return Task.CompletedTask;
    }
}

public class StatisticsServiceTests
{
    [Fact]
    public async Task ComputeSessionStatistics_ReturnsExpectedAggregates()
    {
        var sessionRepo = new FakeSessionResultRepository();
        var shotRepo = new FakeShotRepository();

        var session = new SessionResult { Id = 1, SeriesCount = 1, SessionDate = DateTime.UtcNow };
        session.Shots.Add(new Shot { Id = 1, SessionResultId = 1, SeriesIndex = 1, ShotIndex = 1, Xmm = 0, Ymm = 0 }); // center
        session.Shots.Add(new Shot { Id = 2, SessionResultId = 1, SeriesIndex = 1, ShotIndex = 2, Xmm = 5, Ymm = 0 });

        sessionRepo.Seed(session);

        var service = new StatisticsService(sessionRepo, shotRepo);
        var stats = await service.ComputeSessionStatisticsAsync(1);

        Assert.NotNull(stats);
        Assert.Equal(2, stats!.ShotsCount);
        Assert.True(stats.TotalScore > 0);
        Assert.Equal(session.SessionDate, stats.SessionDate);
    }

    [Fact]
    public async Task ComputeShooterStatistics_AggregatesAcrossSessions()
    {
        var sessionRepo = new FakeSessionResultRepository();
        var shotRepo = new FakeShotRepository();

        var s1 = new SessionResult { Id = 1, ClubMemberId = 10, SessionDate = DateTime.UtcNow };
        s1.Shots.Add(new Shot { Id = 1, SessionResultId = 1, SeriesIndex = 1, ShotIndex = 1, Xmm = 0, Ymm = 0 });
        s1.Shots.Add(new Shot { Id = 2, SessionResultId = 1, SeriesIndex = 1, ShotIndex = 2, Xmm = 5, Ymm = 0 });

        var s2 = new SessionResult { Id = 2, ClubMemberId = 10, SessionDate = DateTime.UtcNow.AddDays(-1) };
        s2.Shots.Add(new Shot { Id = 3, SessionResultId = 2, SeriesIndex = 1, ShotIndex = 1, Xmm = 2, Ymm = 0 });

        sessionRepo.Seed(s1);
        sessionRepo.Seed(s2);

        var service = new StatisticsService(sessionRepo, shotRepo);
        var shooterStats = await service.ComputeShooterStatisticsAsync(10);

        Assert.Equal(2, shooterStats.SessionsCount);
        Assert.Equal(3, shooterStats.TotalShots);
        Assert.True(shooterStats.AverageShot >= 0m);
        Assert.True(shooterStats.BestSessionScore >= 0m);
    }
}