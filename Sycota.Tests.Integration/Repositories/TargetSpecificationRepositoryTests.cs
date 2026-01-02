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

public class TargetSpecificationRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TargetSpecificationRepositoryTests()
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
    public async Task AddAndGetByWeaponType_PersistsSpec()
    {
        await ResetDatabaseAsync();
        await using var context = new ApplicationDbContext(_options);
        var repo = new TargetSpecificationRepository(context);

        var spec = new TargetSpecification
        {
            WeaponType = ISSFWeaponType.AirRifle,
            TargetDiameterMm = 100,
            RingsCount = 10,
            InnerTenRadiusMm = 2
        };

        await repo.AddAsync(spec);

        await using var assertContext = new ApplicationDbContext(_options);
        var stored = await assertContext.TargetSpecifications.FirstOrDefaultAsync();
        Assert.NotNull(stored);
        Assert.Equal(ISSFWeaponType.AirRifle, stored!.WeaponType);

        var byWeapon = await repo.GetByWeaponTypeAsync(ISSFWeaponType.AirRifle);
        Assert.NotNull(byWeapon);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        await ResetDatabaseAsync();
        await using var context = new ApplicationDbContext(_options);
        var repo = new TargetSpecificationRepository(context);

        var spec = new TargetSpecification
        {
            WeaponType = ISSFWeaponType.AirPistol,
            TargetDiameterMm = 120
        };

        await repo.AddAsync(spec);

        spec.TargetDiameterMm = 140;
        await repo.UpdateAsync(spec);

        var updated = await repo.GetByIdAsync(spec.Id);
        Assert.Equal(140, updated!.TargetDiameterMm);
    }

    [Fact]
    public async Task DeleteByIdAsync_RemovesEntity()
    {
        await ResetDatabaseAsync();
        await using var context = new ApplicationDbContext(_options);
        var repo = new TargetSpecificationRepository(context);

        var spec = new TargetSpecification { WeaponType = ISSFWeaponType.AirRifle, TargetDiameterMm = 100 };
        await repo.AddAsync(spec);

        await repo.DeleteByIdAsync(spec.Id);

        var deleted = await repo.GetByIdAsync(spec.Id);
        Assert.Null(deleted);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = new ApplicationDbContext(_options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}