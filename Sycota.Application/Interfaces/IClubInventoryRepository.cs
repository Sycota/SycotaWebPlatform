using Sycota.Domain.Entities;

namespace Sycota.Application.Interfaces;

public interface IClubInventoryRepository
{
    Task<IEnumerable<ClubWeapon>> GetWeaponsByClubIdAsync(int clubId);
    Task<IEnumerable<ClubAmmo>> GetAmmoByClubIdAsync(int clubId);
    Task<IEnumerable<InventoryIssue>> GetIssuesByClubIdAsync(int clubId);
    Task<ClubWeapon?> GetWeaponByIdAsync(int weaponId);
    Task<ClubAmmo?> GetAmmoByIdAsync(int ammoId);
    Task AddWeaponAsync(ClubWeapon weapon);
    Task AddAmmoAsync(ClubAmmo ammo);
    Task UpdateWeaponAsync(ClubWeapon weapon);
    Task UpdateAmmoAsync(ClubAmmo ammo);
    Task DeleteWeaponAsync(ClubWeapon weapon);
    Task DeleteAmmoAsync(ClubAmmo ammo);
    Task AddIssueAsync(InventoryIssue issue);
}
