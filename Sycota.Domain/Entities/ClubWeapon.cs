namespace Sycota.Domain.Entities;

public class ClubWeapon
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? AssignedShooterId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Club Club { get; set; } = null!;
    public ClubMember? AssignedShooter { get; set; }
}
