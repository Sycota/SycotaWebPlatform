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

public class ShooterProfileRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ShooterProfileRepositoryTests()
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
    public async Task AddShooterProfileAsync_PersistsProfile()
    {
        await ResetDatabaseAsync();
        var seed = await SeedClubWithCompetitorAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShooterProfileRepository(context);

        var profile = new ShooterProfile
        {
            ClubMemberId = seed.CompetitorMember.Id,
            PrimaryWeapon = ISSFWeaponType.AirRifle,
            Category = ISSFCategory.Men,
            ISSFLicenseNumber = "ISSF-001",
            NationalLicenseNumber = "NAT-001",
            MedicalCertificateNumber = "MED-001",
            AdditionalNotes = "New profile"
        };

        await repository.AddShooterProfileAsync(profile);

        await using var assertContext = new ApplicationDbContext(_options);
        var stored = await assertContext.ShooterProfiles.FirstOrDefaultAsync(sp => sp.Id == profile.Id);
        Assert.NotNull(stored);
        Assert.Equal(seed.CompetitorMember.Id, stored!.ClubMemberId);
    }

    [Fact]
    public async Task UpdateShooterProfileAsync_PersistsChanges()
    {
        await ResetDatabaseAsync();
        var seed = await SeedShooterProfileAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShooterProfileRepository(context);

        var profile = await context.ShooterProfiles.FirstAsync(sp => sp.Id == seed.ProfileId);
        profile.AdditionalNotes = "Updated notes";

        await repository.UpdateShooterProfileAsync(profile);

        await using var assertContext = new ApplicationDbContext(_options);
        var updated = await assertContext.ShooterProfiles.FirstAsync(sp => sp.Id == seed.ProfileId);
        Assert.Equal("Updated notes", updated.AdditionalNotes);
    }

    [Fact]
    public async Task DeleteShooterProfileAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedShooterProfileAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShooterProfileRepository(context);

        var profile = await context.ShooterProfiles.FirstAsync(sp => sp.Id == seed.ProfileId);
        await repository.DeleteShooterProfileAsync(profile);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.ShooterProfiles.FirstOrDefaultAsync(sp => sp.Id == seed.ProfileId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteShooterProfileByIdAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        var seed = await SeedShooterProfileAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShooterProfileRepository(context);

        await repository.DeleteShooterProfileByIdAsync(seed.ProfileId);

        await using var assertContext = new ApplicationDbContext(_options);
        var deleted = await assertContext.ShooterProfiles.FirstOrDefaultAsync(sp => sp.Id == seed.ProfileId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetShooterProfileByIdAsync_WithAllFlag_LoadsNavigations()
    {
        await ResetDatabaseAsync();
        var seed = await SeedShooterProfileAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShooterProfileRepository(context);

        var profile = await repository.GetShooterProfileByIdAsync(seed.ProfileId, ShooterProfileIncludeOptions.All);

        Assert.NotNull(profile);
        Assert.Equal(seed.CompetitorEmail, profile!.ClubMember.User.Email);
        Assert.Equal(seed.TrainerEmail, profile.ClubMember.Trainer!.User.Email);
    }

    [Fact]
    public async Task GetShooterProfileByClubMemberIdAsync_ReturnsProfile()
    {
        await ResetDatabaseAsync();
        var seed = await SeedShooterProfileAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShooterProfileRepository(context);

        var profile = await repository.GetShooterProfileByClubMemberIdAsync(seed.CompetitorMemberId, ShooterProfileIncludeOptions.None);

        Assert.NotNull(profile);
        Assert.Equal(seed.CompetitorMemberId, profile!.ClubMemberId);
    }

    [Fact]
    public async Task GetAllShooterProfilesAsync_ReturnsAllProfiles()
    {
        await ResetDatabaseAsync();
        await SeedMultipleShooterProfilesAsync();

        await using var context = new ApplicationDbContext(_options);
        var repository = new ShooterProfileRepository(context);

        var profiles = (await repository.GetAllShooterProfilesAsync()).ToList();

        Assert.Equal(2, profiles.Count);
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

    private async Task<ClubWithMembers> SeedClubWithCompetitorAsync()
    {
        await using var context = new ApplicationDbContext(_options);

        var trainerUser = await CreateUserAsync("trainer@test.com");
        var competitorUser = await CreateUserAsync("competitor@test.com");
        var adminUser = await CreateUserAsync("admin@test.com");

        var club = new Club
        {
            Name = "Precision Club",
            Description = "Integration test club",
            Address = "1 Test Lane",
            ContactEmail = "info@test.com",
            CreatedById = adminUser.Id
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

        await context.ClubMembers.AddAsync(competitorMember);
        await context.SaveChangesAsync();

        return new ClubWithMembers(club, trainerMember, competitorMember, trainerUser.Email!, competitorUser.Email!);
    }

    private async Task<ShooterProfileSeedResult> SeedShooterProfileAsync()
    {
        var seed = await SeedClubWithCompetitorAsync();

        await using var context = new ApplicationDbContext(_options);
        var profile = new ShooterProfile
        {
            ClubMemberId = seed.CompetitorMember.Id,
            PrimaryWeapon = ISSFWeaponType.AirRifle,
            Category = ISSFCategory.Men,
            ISSFLicenseNumber = "ISSF-123",
            NationalLicenseNumber = "NAT-456",
            MedicalCertificateNumber = "MED-789",
            AdditionalNotes = "Seed profile"
        };

        await context.ShooterProfiles.AddAsync(profile);
        await context.SaveChangesAsync();

        return new ShooterProfileSeedResult(profile.Id, seed.CompetitorMember.Id, seed.TrainerEmail, seed.CompetitorEmail);
    }

    private async Task SeedMultipleShooterProfilesAsync()
    {
        await using var context = new ApplicationDbContext(_options);

        var clubSeed = await SeedClubWithCompetitorAsync();
        var extraUser = await CreateUserAsync("extra@test.com");

        var extraMember = new ClubMember
        {
            ClubId = clubSeed.Club.Id,
            UserId = extraUser.Id,
            Role = ClubRole.Competitor,
            JoinedAt = DateTime.UtcNow,
            TrainerId = clubSeed.TrainerMember.Id
        };

        await context.ClubMembers.AddAsync(extraMember);
        await context.SaveChangesAsync();

        var profileA = new ShooterProfile
        {
            ClubMemberId = clubSeed.CompetitorMember.Id,
            PrimaryWeapon = ISSFWeaponType.AirRifle,
            Category = ISSFCategory.Men,
            ISSFLicenseNumber = "ISSF-A",
            NationalLicenseNumber = "NAT-A",
            MedicalCertificateNumber = "MED-A"
        };

        var profileB = new ShooterProfile
        {
            ClubMemberId = extraMember.Id,
            PrimaryWeapon = ISSFWeaponType.AirPistol,
            Category = ISSFCategory.Women,
            ISSFLicenseNumber = "ISSF-B",
            NationalLicenseNumber = "NAT-B",
            MedicalCertificateNumber = "MED-B"
        };

        await context.ShooterProfiles.AddRangeAsync(profileA, profileB);
        await context.SaveChangesAsync();
    }

    private sealed record ClubWithMembers(Club Club, ClubMember TrainerMember, ClubMember CompetitorMember, string TrainerEmail, string CompetitorEmail);

    private sealed record ShooterProfileSeedResult(int ProfileId, int CompetitorMemberId, string TrainerEmail, string CompetitorEmail);
}

