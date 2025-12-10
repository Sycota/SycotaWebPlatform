using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces.Options;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Sycota.Infrastructure.Data;
using Sycota.Infrastructure.Repository;
using Xunit;

namespace Sycota.Tests.Integration.Repositories;

public class TrainingSessionRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TrainingSessionRepositoryTests()
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
    public async Task AddTrainingSessionAsync_PersistsSession()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new TrainingSessionRepository(context);

        var session = new TrainingSession
        {
            ClubId = seed.Club.Id,
            Name = "Evening Session",
            Description = "New training session",
            SessionDate = DateTime.UtcNow,
            WeaponType = ISSFWeaponType.AirPistol,
            Shots = "{}",
            CreatedById = seed.Creator.Id,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddTrainingSessionAsync(session);

        await using var assertContext = new ApplicationDbContext(_options);
        var stored = await assertContext.TrainingSessions.FirstOrDefaultAsync(ts => ts.Id == session.Id);
        Assert.NotNull(stored);
        Assert.Equal(seed.Club.Id, stored!.ClubId);
        Assert.Equal(seed.Creator.Id, stored.CreatedById);
    }

    [Fact]
    public async Task UpdateTrainingSessionAsync_PersistsChanges()
    {
        await ResetDatabaseAsync();
        var seed = await SeedTrainingSessionAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new TrainingSessionRepository(context);

        var session = await context.TrainingSessions.FirstAsync(ts => ts.Id == seed.SessionId);
        session.Description = "Updated description";

        await repository.UpdateTrainingSessionAsync(session);

        await using var assertContext = new ApplicationDbContext(_options);
        var updated = await assertContext.TrainingSessions.FirstAsync(ts => ts.Id == seed.SessionId);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task DeleteTrainingSessionAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedTrainingSessionAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new TrainingSessionRepository(context);

        var session = await context.TrainingSessions.FirstAsync(ts => ts.Id == seed.SessionId);
        await repository.DeleteTrainingSessionAsync(session);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.TrainingSessions.FirstOrDefaultAsync(ts => ts.Id == seed.SessionId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteTrainingSessionByIdAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedTrainingSessionAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new TrainingSessionRepository(context);

        await repository.DeleteTrainingSessionByIdAsync(seed.SessionId);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.TrainingSessions.FirstOrDefaultAsync(ts => ts.Id == seed.SessionId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetTrainingSessionByIdAsync_WithAllFlag_LoadsNavigations()
    {
        await ResetDatabaseAsync();
        var seed = await SeedTrainingSessionAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new TrainingSessionRepository(context);

        var session = await repository.GetTrainingSessionByIdAsync(seed.SessionId, TrainingSessionIncludeOptions.All);

        Assert.NotNull(session);
        Assert.Equal(seed.ClubName, session!.Club.Name);
        Assert.Equal(seed.CreatorEmail, session.CreatedBy.Email);
    }

    [Fact]
    public async Task GetAllTrainingSessionsByClubIdAsync_FiltersByClub()
    {
        await ResetDatabaseAsync();
        var seed = await SeedMultipleClubsWithSessionsAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new TrainingSessionRepository(context);

        var sessions = await repository.GetAllTrainingSessionsByClubIdAsync(seed.TargetClubId, TrainingSessionIncludeOptions.None);

        Assert.All(sessions, s => Assert.Equal(seed.TargetClubId, s.ClubId));
        Assert.Single(sessions);
    }

    [Fact]
    public async Task GetAllTrainingSessionsAsync_ReturnsAllSessions()
    {
        await ResetDatabaseAsync();
        var seed = await SeedMultipleClubsWithSessionsAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new TrainingSessionRepository(context);

        var sessions = (await repository.GetAllTrainingSessionsAsync()).ToList();

        Assert.Equal(seed.TotalSessions, sessions.Count);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private async Task<ApplicationUser> CreateUserAsync(string email)
    {
        await using var context = new ApplicationDbContext(_options);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<ClubSeedResult> SeedClubAsync()
    {
        var creator = await CreateUserAsync("creator@test.com");

        await using var context = new ApplicationDbContext(_options);
        var club = new Club
        {
            Name = "Seed Club",
            Description = "Seed club",
            Address = "1 Test Lane",
            ContactEmail = "seed@test.com",
            CreatedById = creator.Id
        };

        await context.Clubs.AddAsync(club);
        await context.SaveChangesAsync();

        return new ClubSeedResult(club, creator);
    }

    private async Task<TrainingSessionSeedResult> SeedTrainingSessionAsync()
    {
        var seed = await SeedClubAsync();

        await using var context = new ApplicationDbContext(_options);
        var session = new TrainingSession
        {
            ClubId = seed.Club.Id,
            Name = "Morning Session",
            Description = "Seed session",
            SessionDate = DateTime.UtcNow,
            WeaponType = ISSFWeaponType.AirRifle,
            Shots = "{}",
            CreatedById = seed.Creator.Id,
            CreatedAt = DateTime.UtcNow
        };

        await context.TrainingSessions.AddAsync(session);
        await context.SaveChangesAsync();

        return new TrainingSessionSeedResult(session.Id, seed.Club.Id, seed.Club.Name, seed.Creator.Email!, 1);
    }

    private async Task<TrainingSessionSeedResult> SeedMultipleClubsWithSessionsAsync()
    {
        var creator = await CreateUserAsync("creator@test.com");
        var otherCreator = await CreateUserAsync("other@test.com");

        await using var context = new ApplicationDbContext(_options);

        var clubA = new Club
        {
            Name = "Club A",
            Description = "Club A description",
            Address = "1 A Street",
            ContactEmail = "a@test.com",
            CreatedById = creator.Id
        };

        var clubB = new Club
        {
            Name = "Club B",
            Description = "Club B description",
            Address = "2 B Street",
            ContactEmail = "b@test.com",
            CreatedById = otherCreator.Id
        };

        await context.Clubs.AddRangeAsync(clubA, clubB);
        await context.SaveChangesAsync();

        var sessionA = new TrainingSession
        {
            ClubId = clubA.Id,
            Name = "Session A1",
            Description = "Club A session",
            SessionDate = DateTime.UtcNow,
            WeaponType = ISSFWeaponType.AirPistol,
            Shots = "{}",
            CreatedById = creator.Id,
            CreatedAt = DateTime.UtcNow
        };

        var sessionB = new TrainingSession
        {
            ClubId = clubB.Id,
            Name = "Session B1",
            Description = "Club B session",
            SessionDate = DateTime.UtcNow,
            WeaponType = ISSFWeaponType.AirRifle,
            Shots = "{}",
            CreatedById = otherCreator.Id,
            CreatedAt = DateTime.UtcNow
        };

        await context.TrainingSessions.AddRangeAsync(sessionA, sessionB);
        await context.SaveChangesAsync();

        return new TrainingSessionSeedResult(sessionA.Id, clubA.Id, clubA.Name, creator.Email!, 2);
    }

    private sealed record ClubSeedResult(Club Club, ApplicationUser Creator);

    private sealed record TrainingSessionSeedResult(int SessionId, int TargetClubId, string ClubName, string CreatorEmail, int TotalSessions);
}

