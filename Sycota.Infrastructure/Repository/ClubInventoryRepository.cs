using Microsoft.EntityFrameworkCore;
using Sycota.Application.Interfaces;
using Sycota.Domain.Entities;
using Sycota.Infrastructure.Data;

namespace Sycota.Infrastructure.Repository;

public class ClubInventoryRepository : IClubInventoryRepository
{
    private readonly ApplicationDbContext _context;

    public ClubInventoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClubWeapon>> GetWeaponsByClubIdAsync(int clubId)
    {
        return await _context.ClubWeapons
            .Include(w => w.AssignedShooter)
                .ThenInclude(s => s!.User)
            .Where(w => w.ClubId == clubId)
            .OrderBy(w => w.SerialNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<ClubAmmo>> GetAmmoByClubIdAsync(int clubId)
    {
        return await _context.ClubAmmo
            .Where(a => a.ClubId == clubId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryIssue>> GetIssuesByClubIdAsync(int clubId)
    {
        return await _context.InventoryIssues
            .Include(i => i.Shooter).ThenInclude(s => s.User)
            .Include(i => i.IssuedBy).ThenInclude(s => s.User)
            .Include(i => i.Weapon)
            .Include(i => i.Ammo)
            .Where(i => i.ClubId == clubId)
            .OrderByDescending(i => i.IssuedAt)
            .Take(100)
            .ToListAsync();
    }

    public Task<ClubWeapon?> GetWeaponByIdAsync(int weaponId)
        => _context.ClubWeapons.FirstOrDefaultAsync(w => w.Id == weaponId);

    public Task<ClubAmmo?> GetAmmoByIdAsync(int ammoId)
        => _context.ClubAmmo.FirstOrDefaultAsync(a => a.Id == ammoId);

    public async Task AddWeaponAsync(ClubWeapon weapon)
    {
        await _context.ClubWeapons.AddAsync(weapon);
        await _context.SaveChangesAsync();
    }

    public async Task AddAmmoAsync(ClubAmmo ammo)
    {
        await _context.ClubAmmo.AddAsync(ammo);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateWeaponAsync(ClubWeapon weapon)
    {
        _context.ClubWeapons.Update(weapon);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAmmoAsync(ClubAmmo ammo)
    {
        _context.ClubAmmo.Update(ammo);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteWeaponAsync(ClubWeapon weapon)
    {
        _context.ClubWeapons.Remove(weapon);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAmmoAsync(ClubAmmo ammo)
    {
        _context.ClubAmmo.Remove(ammo);
        await _context.SaveChangesAsync();
    }

    public async Task AddIssueAsync(InventoryIssue issue)
    {
        await _context.InventoryIssues.AddAsync(issue);
        await _context.SaveChangesAsync();
    }
}
