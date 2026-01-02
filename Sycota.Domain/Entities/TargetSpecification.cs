using System;
using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

public class TargetSpecification
{
    public int Id { get; set; }

    // Weapon type this target spec applies to (optional)
    public ISSFWeaponType? WeaponType { get; set; }

    // Full target diameter in millimeters (outermost circle)
    public int TargetDiameterMm { get; set; }

    // Number of scoring rings (typically 10 for ISSF)
    public int RingsCount { get; set; } = 10;

    // Optional explicit ring width in mm. If null, computed as TargetRadius / RingsCount.
    public int? RingWidthMm { get; set; }

    // Radius in mm considered "inner ten" (X-count)
    public int InnerTenRadiusMm { get; set; } = 2;

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}