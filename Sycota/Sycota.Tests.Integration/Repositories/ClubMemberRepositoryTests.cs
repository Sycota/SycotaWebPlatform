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

public class ClubMemberRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ClubMemberRepositoryTests()
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
    public async Task AddClubMemberAsync_PersistsMember()
    {
        await ResetDatabaseAsync();

        var user = await CreateUserAsync("member@test.com");
        var club = await CreateClubAsync("Test Club", user);

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var member = new ClubMember
        {
            ClubId = club.Id,
            UserId = user.Id,
            Role = ClubRole.Competitor,
            JoinedAt = DateTime.UtcNow
        };

        await repository.AddClubMemberAsync(member);

        await using var assertContext = new ApplicationDbContext(_options);
        var stored = await assertContext.ClubMembers.FirstOrDefaultAsync(cm => cm.Id == member.Id);
        Assert.NotNull(stored);
        Assert.Equal(club.Id, stored!.ClubId);
        Assert.Equal(user.Id, stored.UserId);
    }

    [Fact]
    public async Task UpdateClubMemberAsync_PersistsChanges()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var member = await context.ClubMembers.FirstAsync(cm => cm.Id == seed.CompetitorMemberId);
        member.Role = ClubRole.Admin;

        await repository.UpdateClubMemberAsync(member);

        await using var assertContext = new ApplicationDbContext(_options);
        var updated = await assertContext.ClubMembers.FirstAsync(cm => cm.Id == seed.CompetitorMemberId);
        Assert.Equal(ClubRole.Admin, updated.Role);
    }

    [Fact]
    public async Task DeleteClubMemberAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var member = await context.ClubMembers.FirstAsync(cm => cm.Id == seed.CompetitorMemberId);
        await repository.DeleteClubMemberAsync(member);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.ClubMembers.FirstOrDefaultAsync(cm => cm.Id == seed.CompetitorMemberId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteClubMemberByIdAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        await repository.DeleteClubMemberByIdAsync(seed.CompetitorMemberId);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.ClubMembers.FirstOrDefaultAsync(cm => cm.Id == seed.CompetitorMemberId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetClubMemberByIdAsync_WithAllFlag_LoadsNavigations()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var member = await repository.GetClubMemberByIdAsync(seed.CompetitorMemberId, ClubMemberIncludeOptions.All);

        Assert.NotNull(member);
        Assert.NotNull(member!.User);
        Assert.Equal(seed.CompetitorEmail, member.User.Email);
        Assert.NotNull(member.Club);
        Assert.Equal(seed.ClubId, member.Club.Id);
        Assert.NotNull(member.Trainer);
        Assert.Equal(ClubRole.Trainer, member.Trainer.Role);
        Assert.NotNull(member.ShooterProfile);
    }

    [Fact]
    public async Task GetAllClubMembersByClubIdAsync_FiltersByClub()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var members = await repository.GetAllClubMembersByClubIdAsync(seed.ClubId, ClubMemberIncludeOptions.None);

        Assert.NotEmpty(members);
        Assert.All(members, m => Assert.Equal(seed.ClubId, m.ClubId));
    }

    [Fact]
    public async Task GetAllTrainersByClubIdAsync_ReturnsOnlyTrainers()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var trainers = await repository.GetAllTrainersByClubIdAsync(seed.ClubId, ClubMemberIncludeOptions.User);

        var trainer = Assert.Single(trainers);
        Assert.Equal(ClubRole.Trainer, trainer.Role);
        Assert.Equal(seed.TrainerEmail, trainer.User.Email);
    }

    [Fact]
    public async Task GetAllCompetitorsByClubIdAsync_ReturnsOnlyCompetitors()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var competitors = await repository.GetAllCompetitorsByClubIdAsync(seed.ClubId, ClubMemberIncludeOptions.All);

        var competitor = Assert.Single(competitors);
        Assert.Equal(ClubRole.Competitor, competitor.Role);
        Assert.Equal(seed.CompetitorEmail, competitor.User.Email);
        Assert.NotNull(competitor.Trainer);
        Assert.Equal(seed.TrainerMemberId, competitor.Trainer.Id);
    }

    [Fact]
    public async Task GetAllAdminsByClubIdAsync_ReturnsOnlyAdmins()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var admins = await repository.GetAllAdminsByClubIdAsync(seed.ClubId, ClubMemberIncludeOptions.User);

        var admin = Assert.Single(admins);
        Assert.Equal(ClubRole.Admin, admin.Role);
        Assert.Equal(seed.AdminEmail, admin.User.Email);
    }

    [Fact]
    public async Task GetAllClubMembersAsync_ReturnsAllMembers()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithMembersAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ClubMemberRepository(context);

        var members = (await repository.GetAllClubMembersAsync()).ToList();

        Assert.Equal(3, members.Count);
        Assert.Contains(members, m => m.Id == seed.TrainerMemberId);
        Assert.Contains(members, m => m.Id == seed.CompetitorMemberId);
        Assert.Contains(members, m => m.Id == seed.AdminMemberId);
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

    private async Task<Club> CreateClubAsync(string name, ApplicationUser creator)
    {
        await using var context = new ApplicationDbContext(_options);
        var club = new Club
        {
            Name = name,
            Description = "Integration test club",
            Address = "1 Test Lane",
            ContactEmail = "info@test.com",
            CreatedById = creator.Id
        };

        await context.Clubs.AddAsync(club);
        await context.SaveChangesAsync();

        return club;
    }

    private async Task<ClubMembersSeedResult> SeedClubWithMembersAsync()
    {
        await using var context = new ApplicationDbContext(_options);

        var trainerUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "trainer@test.com",
            Email = "trainer@test.com"
        };

        var competitorUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "competitor@test.com",
            Email = "competitor@test.com"
        };

        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin@test.com",
            Email = "admin@test.com"
        };

        await context.Users.AddRangeAsync(trainerUser, competitorUser, adminUser);

        var club = new Club
        {
            Name = "Precision Club",
            Description = "Integration test club",
            Address = "1 Test Lane",
            ContactEmail = "info@test.com",
            CreatedById = adminUser.Id,
            CreatedBy = adminUser
        };

        await context.Clubs.AddAsync(club);
        await context.SaveChangesAsync();

        var trainerMember = new ClubMember
        {
            ClubId = club.Id,
            UserId = trainerUser.Id,
            Role = ClubRole.Trainer,
            JoinedAt = DateTime.UtcNow
        };

        await context.ClubMembers.AddAsync(trainerMember);
        await context.SaveChangesAsync();

        var competitorMember = new ClubMember
        {
            ClubId = club.Id,
            UserId = competitorUser.Id,
            Role = ClubRole.Competitor,
            JoinedAt = DateTime.UtcNow,
            TrainerId = trainerMember.Id
        };

        var adminMember = new ClubMember
        {
            ClubId = club.Id,
            UserId = adminUser.Id,
            Role = ClubRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        await context.ClubMembers.AddRangeAsync(competitorMember, adminMember);
        await context.SaveChangesAsync();

        var shooterProfile = new ShooterProfile
        {
            ClubMemberId = competitorMember.Id,
            PrimaryWeapon = ISSFWeaponType.AirRifle,
            Category = ISSFCategory.Men,
            ISSFLicenseNumber = "ISSF-123",
            NationalLicenseNumber = "NAT-456",
            MedicalCertificateNumber = "MED-789",
            AdditionalNotes = "Integration test profile"
        };

        await context.ShooterProfiles.AddAsync(shooterProfile);
        await context.SaveChangesAsync();

        return new ClubMembersSeedResult(
            club.Id,
            trainerMember.Id,
            competitorMember.Id,
            adminMember.Id,
            trainerUser.Email!,
            competitorUser.Email!,
            adminUser.Email!
        );
    }

    private sealed record ClubMembersSeedResult(
        int ClubId,
        int TrainerMemberId,
        int CompetitorMemberId,
        int AdminMemberId,
        string TrainerEmail,
        string CompetitorEmail,
        string AdminEmail
    );
}


