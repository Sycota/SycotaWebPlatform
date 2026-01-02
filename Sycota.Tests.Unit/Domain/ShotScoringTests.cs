using System;
using Sycota.Domain.Entities;
using Sycota.Domain.Classes;
using Xunit;

namespace Sycota.Tests.Unit.Domain;

public class ShotScoringTests
{
    private static TargetSpecification CreateTestSpec()
    {
        // Small deterministic spec: diameter 100mm, 10 rings => ring width 5mm
        return new TargetSpecification
        {
            TargetDiameterMm = 100,
            RingsCount = 10,
            RingWidthMm = null,
            InnerTenRadiusMm = 2
        };
    }

    [Fact]
    public void CalculateIntegerScore_Center_IsMax()
    {
        var spec = CreateTestSpec();
        var ring = ShotScoring.CalculateIntegerScore(0, 0, spec);
        Assert.Equal(spec.RingsCount, ring);
    }

    [Fact]
    public void CalculateDecimalScore_Center_IsMaxDecimal()
    {
        var spec = CreateTestSpec();
        var score = ShotScoring.CalculateDecimalScore(0, 0, spec);
        // center should map to ringsCount + 0.9 (e.g., 10.9)
        Assert.Equal((decimal)spec.RingsCount + 0.9m, score);
    }

    [Fact]
    public void CalculateIntegerScore_OutsideTarget_IsZero()
    {
        var spec = CreateTestSpec();
        // distance > radius (radius = 50)
        var ring = ShotScoring.CalculateIntegerScore(1000, 0, spec);
        Assert.Equal(0, ring);
    }

    [Fact]
    public void CalculateDecimalScore_MapsFractionInsideRing()
    {
        var spec = CreateTestSpec();
        // pick a point in outermost ring: radius=50, ringWidth=5 -> outermost innerEdge = (10-1)*5 =45
        // pick dist = 46 -> inside ring 1, near inner edge => fractional close to 0.9
        var x = 46;
        var y = 0;
        var intScore = ShotScoring.CalculateIntegerScore(x, y, spec);
        var decimalScore = ShotScoring.CalculateDecimalScore(x, y, spec);

        Assert.Equal(1, intScore);
        Assert.InRange(decimalScore, 1.0m, 1.9m);
    }

    [Fact]
    public void IsInnerTen_ReturnsTrue_ForCloseToCenter()
    {
        var spec = CreateTestSpec();
        // inner ten radius default 2 -> point at (1,1) distance ~1.414 < 2
        Assert.True(ShotScoring.IsInnerTen(1, 1, spec));
        Assert.False(ShotScoring.IsInnerTen(3, 0, spec));
    }
}