namespace Sycota.Domain.Entities;

public class InventoryIssue
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public int ShooterId { get; set; }
    public int IssuedById { get; set; }
    public int? WeaponId { get; set; }
    public int? AmmoId { get; set; }
    public int? AmmoQuantity { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public Club Club { get; set; } = null!;
    public ClubMember Shooter { get; set; } = null!;
    public ClubMember IssuedBy { get; set; } = null!;
    public ClubWeapon? Weapon { get; set; }
    public ClubAmmo? Ammo { get; set; }
}
