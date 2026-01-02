using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sycota.Infrastructure.Data;
using Sycota.Infrastructure.Repository;
using Sycota.Domain.Entities;
using Sycota.Domain.Enums;
using Xunit;

namespace Sycota.Tests.Integration.Repositories;

public class SessionResultRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public SessionResultRepositoryTests()
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
    public async Task AddSessionResultAsync_PersistsSessionWithShots()
    {
        await ResetDatabaseAsync();
        // seed: user, club, member
        await using var context = new ApplicationDbContext(_options);
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "u@test", Email = "u@test" };
        await context.Users.AddAsync(user);
        var club = new Club { Name = "C", CreatedById = user.Id, ContactEmail = "c@test.com" };
        await context.Clubs.AddAsync(club);
        await context.SaveChangesAsync();
        var member = new ClubMember { ClubId = club.Id, UserId = user.Id, Role = ClubRole.Competitor, JoinedAt = DateTime.UtcNow };
        await context.ClubMembers.AddAsync(member);
        await context.SaveChangesAsync();

        var repository = new SessionResultRepository(context);

        var session = new SessionResult
        {
            ClubMemberId = member.Id,
            SessionDate = DateTime.UtcNow,
            SeriesCount = 2,
            ShotsCount = 3,
            Shots = { new Shot { SeriesIndex = 1, ShotIndex = 1, Xmm = 0, Ymm = 0 },
                      new Shot { SeriesIndex = 1, ShotIndex = 2, Xmm = 5, Ymm = 0 } }
        };

        await repository.AddSessionResultAsync(session);

        await using var assertContext = new ApplicationDbContext(_options);
        var stored = await assertContext.SessionResults.Include(sr => sr.Shots).FirstOrDefaultAsync(sr => sr.Id == session.Id);
        Assert.NotNull(stored);
        Assert.Equal(2, stored!.Shots.Count);
    }

    [Fact]
    public async Task GetSessionResultByIdAsync_WithShotsFlag_ReturnsShots()
    {
        await ResetDatabaseAsync();
        var seed = await SeedSessionAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new SessionResultRepository(context);

        var session = await repository.GetSessionResultByIdAsync(seed.SessionId, Sycota.Application.Interfaces.Options.SessionResultIncludeOptions.Shots);
        Assert.NotNull(session);
        Assert.NotEmpty(session!.Shots);
    }

    [Fact]
    public async Task DeleteSessionResultByIdAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedSessionAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new SessionResultRepository(context);

        await repository.DeleteSessionResultByIdAsync(seed.SessionId);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.SessionResults.FindAsync(seed.SessionId);
        Assert.Null(deleted);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private async Task<(int SessionId, int MemberId)> SeedSessionAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "u@test", Email = "u@test" };
        await context.Users.AddAsync(user);
        var club = new Club { Name = "C", CreatedById = user.Id, ContactEmail = "c@test.com" };
        await context.Clubs.AddAsync(club);
        await context.SaveChangesAsync();
        var member = new ClubMember { ClubId = club.Id, UserId = user.Id, Role = ClubRole.Competitor, JoinedAt = DateTime.UtcNow };
        await context.ClubMembers.AddAsync(member);
        await context.SaveChangesAsync();

        var session = new SessionResult
        {
            ClubMemberId = member.Id,
            SessionDate = DateTime.UtcNow,
            SeriesCount = 1,
            ShotsCount = 1
        };

        await context.SessionResults.AddAsync(session);
        await context.SaveChangesAsync();

        var shot = new Shot { SessionResultId = session.Id, SeriesIndex = 1, ShotIndex = 1, Xmm = 0, Ymm = 0 };
        await context.Shots.AddAsync(shot);
        await context.SaveChangesAsync();

        return (session.Id, member.Id);
    }
}