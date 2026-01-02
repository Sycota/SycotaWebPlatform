using System;
using Sycota.Domain.Entities;

namespace Sycota.Domain.Classes;

public static class ShotScoring
{
    // Compute target radius (mm)
    public static double TargetRadiusMm(TargetSpecification spec)
    {
        return spec.TargetDiameterMm / 2.0;
    }

    // Determine ring width (mm). If RingWidthMm is set use it, otherwise compute equal rings.
    public static double RingWidthMm(TargetSpecification spec)
    {
        if (spec.RingWidthMm.HasValue && spec.RingWidthMm.Value > 0)
            return spec.RingWidthMm.Value;

        var radius = TargetRadiusMm(spec);
        return radius / Math.Max(1, spec.RingsCount);
    }

    // Calculate integer (ring) score from coordinates
    // Returns 0..RingsCount (0 = miss/outside)
    public static int CalculateIntegerScore(int xmm, int ymm, TargetSpecification spec)
    {
        var dist = Math.Sqrt((double)xmm * xmm + (double)ymm * ymm);
        var radius = TargetRadiusMm(spec);

        if (dist > radius) return 0;

        var ringWidth = RingWidthMm(spec);
        // ringIndex: 10 (center) down to 1 (outermost), compute as:
        // rings from center out: 10 => dist < ringWidth, 9 => ringWidth <= dist < 2*ringWidth, etc.
        // So ring = RingsCount - floor(dist / ringWidth)
        var ring = spec.RingsCount - (int)Math.Floor(dist / ringWidth);
        if (ring < 0) ring = 0;
        if (ring > spec.RingsCount) ring = spec.RingsCount;
        return ring;
    }

    // Calculate decimal score (e.g., 10.7) from coordinates
    // Maps position inside the ring to 0.0..0.9 fraction and adds to integer ring.
    public static decimal CalculateDecimalScore(int xmm, int ymm, TargetSpecification spec)
    {
        var intScore = CalculateIntegerScore(xmm, ymm, spec);
        if (intScore <= 0) return 0m;

        var dist = Math.Sqrt((double)xmm * xmm + (double)ymm * ymm);
        var ringWidth = RingWidthMm(spec);

        // distance to inner edge of this ring:
        var innerEdgeRadius = (spec.RingsCount - intScore) * ringWidth;
        var offsetInRing = dist - innerEdgeRadius;

        // fraction inside ring (0 => at inner edge / closer to center, 1 => at outer edge)
        var frac = 1.0 - Math.Min(Math.Max(offsetInRing / ringWidth, 0.0), 1.0);

        var addition = frac * 0.9; // map to 0.0 .. 0.9
        var score = intScore + addition;

        if (score > spec.RingsCount + 0.9) score = spec.RingsCount + 0.9; // cap (e.g., 10.9)
        return Math.Round((decimal)score, 2);
    }

    // Inner ten (X) detection
    public static bool IsInnerTen(int xmm, int ymm, TargetSpecification spec)
    {
        var dist = Math.Sqrt((double)xmm * xmm + (double)ymm * ymm);
        return dist <= spec.InnerTenRadiusMm;
    }

    // Overloads for Shot
    public static int CalculateIntegerScore(Shot shot, TargetSpecification spec) =>
        CalculateIntegerScore(shot.Xmm, shot.Ymm, spec);

    public static decimal CalculateDecimalScore(Shot shot, TargetSpecification spec) =>
        CalculateDecimalScore(shot.Xmm, shot.Ymm, spec);

    public static bool IsInnerTen(Shot shot, TargetSpecification spec) =>
        IsInnerTen(shot.Xmm, shot.Ymm, spec);
}