using System;
using Sycota.Domain.Enums;

namespace Sycota.Domain.Entities;

public class Shot
{
    public int Id { get; set; }
    public int SessionResultId { get; set; }
    public int SeriesIndex { get; set; } // 1..N
    public int ShotIndex { get; set; }   // 1..M

    // Cartesian coordinates in millimeters relative to target center
    // - Xmm: horizontal offset (positive = right)
    // - Ymm: vertical offset (positive = up)
    public int Xmm { get; set; }
    public int Ymm { get; set; }

    public ShootingPosition Position { get; set; } = ShootingPosition.Unknown;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation
    public SessionResult SessionResult { get; set; } = null!;

    // Convenience computed properties (not mapped explicitly)
    public double DistanceMm => Math.Sqrt((double)Xmm * Xmm + (double)Ymm * Ymm);
    public double AngleDegrees => Math.Atan2(Ymm, Xmm) * (180.0 / Math.PI);
}