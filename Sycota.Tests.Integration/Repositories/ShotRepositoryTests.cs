using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sycota.Infrastructure.Data;
using Sycota.Infrastructure.Repository;
using Sycota.Domain.Entities;
using Xunit;

namespace Sycota.Tests.Integration.Repositories;

public class ShotRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ShotRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task AddShotAsync_PersistsShot()
    {
        await ResetDatabaseAsync();
        var seed = await SeedSessionAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShotRepository(context);

        var shot = new Shot
        {
            SessionResultId = seed.SessionId,
            SeriesIndex = 1,
            ShotIndex = 1,
            Xmm = 0,
            Ymm = 0
        };

        await repository.AddShotAsync(shot);

        await using var assertContext = new ApplicationDbContext(_options);
        var stored = await assertContext.Shots.FirstOrDefaultAsync(s => s.Id == shot.Id);
        Assert.NotNull(stored);
        Assert.Equal(seed.SessionId, stored!.SessionResultId);
    }

    [Fact]
    public async Task GetAllShotsBySessionResultIdAsync_ReturnsShots()
    {
        await ResetDatabaseAsync();
        var seed = await SeedSessionWithShotsAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShotRepository(context);

        var shots = await repository.GetAllShotsBySessionResultIdAsync(seed.SessionId);
        Assert.NotEmpty(shots);
        Assert.Equal(2, shots.Count());
    }

    [Fact]
    public async Task DeleteShotsBySessionResultIdAsync_RemovesShots()
    {
        await ResetDatabaseAsync();
        var seed = await SeedSessionWithShotsAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShotRepository(context);

        await repository.DeleteShotsBySessionResultIdAsync(seed.SessionId);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.Shots.Where(s => s.SessionResultId == seed.SessionId).ToListAsync();
        Assert.Empty(deleted);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private async Task<(int SessionId, object Dummy)> SeedSessionAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "u@test", Email = "u@test" };
        await context.Users.AddAsync(user);
        var club = new Club { Name = "C", CreatedById = user.Id, ContactEmail = "c@test.com" };
        await context.Clubs.AddAsync(club);
        await context.SaveChangesAsync();
        var member = new ClubMember { ClubId = club.Id, UserId = user.Id, Role = Domain.Enums.ClubRole.Competitor, JoinedAt = DateTime.UtcNow };
        await context.ClubMembers.AddAsync(member);
        await context.SaveChangesAsync();

        var session = new SessionResult
        {
            ClubMemberId = member.Id,
            SessionDate = DateTime.UtcNow,
            SeriesCount = 1,
            ShotsCount = 0
        };

        await context.SessionResults.AddAsync(session);
        await context.SaveChangesAsync();

        return (session.Id, null!);
    }

    private async Task<(int SessionId, object Dummy)> SeedSessionWithShotsAsync()
    {
        var s = await SeedSessionAsync();
        await using var context = new ApplicationDbContext(_options);
        var shotA = new Shot { SessionResultId = s.SessionId, SeriesIndex = 1, ShotIndex = 1, Xmm = 0, Ymm = 0 };
        var shotB = new Shot { SessionResultId = s.SessionId, SeriesIndex = 1, ShotIndex = 2, Xmm = 10, Ymm = 0 };
        await context.Shots.AddRangeAsync(shotA, shotB);
        await context.SaveChangesAsync();
        return (s.SessionId, null!);
    }
}