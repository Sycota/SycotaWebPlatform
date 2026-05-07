using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

public class ClubAmmo
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public AmmoType Type { get; set; }
    public int Quantity { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Club Club { get; set; } = null!;
}
