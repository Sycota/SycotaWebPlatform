using System;
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

public class ClubRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ClubRepositoryTests()
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
    public async Task AddClubAsync_PersistsClub()
    {
        await ResetDatabaseAsync();

        var creator = await CreateUserAsync("creator@test.com");

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubRepository(context);

        var club = new Club
        {
            Name = "Created Club",
            Description = "New club description",
            Address = "123 Main St",
            ContactEmail = "contact@test.com",
            CreatedById = creator.Id,
        };

        await repository.AddClubAsync(club);

        await using var assertContext = new ApplicationDbContext(_options);
        var stored = await assertContext.Clubs.FirstOrDefaultAsync(c => c.Id == club.Id);
        Assert.NotNull(stored);
        Assert.Equal("Created Club", stored!.Name);
    }

    [Fact]
    public async Task UpdateClubAsync_PersistsChanges()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubAggregateAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubRepository(context);

        var club = await context.Clubs.FirstAsync(c => c.Id == seed.ClubId);
        club.Description = "Updated description";

        await repository.UpdateClubAsync(club);

        await using var assertContext = new ApplicationDbContext(_options);
        var updated = await assertContext.Clubs.FirstAsync(c => c.Id == seed.ClubId);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task DeleteClubAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubAggregateAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubRepository(context);

        var club = await context.Clubs.FirstAsync(c => c.Id == seed.ClubId);
        await repository.DeleteClubAsync(club);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.Clubs.FirstOrDefaultAsync(c => c.Id == seed.ClubId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteClubByIdAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubAggregateAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubRepository(context);

        await repository.DeleteClubByIdAsync(seed.ClubId);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.Clubs.FirstOrDefaultAsync(c => c.Id == seed.ClubId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetClubByIdAsync_WithMembersFlag_ReturnsMembers()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubAggregateAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubRepository(context);

        var club = await repository.GetClubByIdAsync(seed.ClubId, ClubIncludeOptions.Members);

        Assert.NotNull(club);
        var member = Assert.Single(club!.Members);
        Assert.Equal(seed.MemberUserId, member.UserId);
    }

    [Fact]
    public async Task GetClubByIdAsync_WithAllFlag_LoadsAllNavigations()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubAggregateAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubRepository(context);

        var club = await repository.GetClubByIdAsync(seed.ClubId, ClubIncludeOptions.All);

        Assert.NotNull(club);
        Assert.Equal(seed.CreatorEmail, club!.CreatedBy.Email);
        Assert.Single(club.Members);
        Assert.Single(club.TrainingSessions);
    }

    [Fact]
    public async Task GetAllClubsAsync_WithCreatedByFlag_PopulatesCreator()
    {
        await ResetDatabaseAsync();
        await SeedClubAggregateAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubRepository(context);

        var clubs = await repository.GetAllClubsAsync(ClubIncludeOptions.CreatedBy);

        var club = Assert.Single(clubs);
        Assert.NotNull(club.CreatedBy);
        Assert.False(string.IsNullOrWhiteSpace(club.CreatedBy.Email));
        Assert.Empty(club.Members);
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

    private async Task<ClubSeedResult> SeedClubAggregateAsync()
    {
        await using var context = new ApplicationDbContext(_options);

        var creator = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "creator@test.com",
            Email = "creator@test.com"
        };

        var memberUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "member@test.com",
            Email = "member@test.com"
        };

        await context.Users.AddRangeAsync(creator, memberUser);

        var club = new Club
        {
            Name = "Precision Club",
            Description = "Integration test club",
            Address = "1 Test Lane",
            ContactEmail = "info@test.com",
            CreatedById = creator.Id,
            CreatedBy = creator
        };

        await context.Clubs.AddAsync(club);
        await context.SaveChangesAsync();

        var member = new ClubMember
        {
            ClubId = club.Id,
            UserId = memberUser.Id,
            Role = ClubRole.Competitor,
            JoinedAt = DateTime.UtcNow
        };

        await context.ClubMembers.AddAsync(member);

        var session = new TrainingSession
        {
            ClubId = club.Id,
            Name = "Morning Session",
            Description = "Integration test session",
            SessionDate = DateTime.UtcNow,
            WeaponType = ISSFWeaponType.AirRifle,
            Shots = "{}",
            CreatedById = creator.Id,
            CreatedAt = DateTime.UtcNow
        };

        await context.TrainingSessions.AddAsync(session);
        await context.SaveChangesAsync();

        return new ClubSeedResult(club.Id, creator.Email!, member.Id, memberUser.Id);
    }

    private sealed record ClubSeedResult(int ClubId, string CreatorEmail, int MemberId, string MemberUserId);
}

