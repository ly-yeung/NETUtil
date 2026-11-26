using System;
using CSharpUtil.NumberUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Number helpers added in 0.3.0: range clamping, division that cannot throw and invariant percentage formatting.
/// </summary>
public class NumberExtensionsTests
{
    [Fact]
    public void Clamp_ConstrainsEachSupportedType()
    {
        Assert.Equal(5, 5.Clamp(1, 10));
        Assert.Equal(1, (-3).Clamp(1, 10));
        Assert.Equal(10, 99.Clamp(1, 10));

        Assert.Equal(5L, 5L.Clamp(1L, 10L));
        Assert.Equal(10L, long.MaxValue.Clamp(1L, 10L));

        Assert.Equal(5m, 5m.Clamp(1m, 10m));
        Assert.Equal(1m, decimal.MinValue.Clamp(1m, 10m));

        Assert.Equal(5d, 5d.Clamp(1d, 10d));
        Assert.Equal(10d, double.MaxValue.Clamp(1d, 10d));
    }

    [Fact]
    public void Clamp_InvertedRangeIsRejected()
    {
        Assert.Throws<ArgumentException>(() => 5.Clamp(10, 1));
        Assert.Throws<ArgumentException>(() => 5L.Clamp(10L, 1L));
        Assert.Throws<ArgumentException>(() => 5m.Clamp(10m, 1m));
        Assert.Throws<ArgumentException>(() => 5d.Clamp(10d, 1d));
    }

    [Fact]
    public void SafeDivide_ReturnsFallbackOnZeroDivisor()
    {
        Assert.Equal(2.5m, 5m.SafeDivide(2m));
        Assert.Equal(0m, 5m.SafeDivide(0m));
        Assert.Equal(-1m, 5m.SafeDivide(0m, -1m));
        // Negative divisors are ordinary input, not a special case.
        Assert.Equal(-2.5m, 5m.SafeDivide(-2m));
    }

    [Fact]
    public void ToPercent_UsesInvariantDecimalMark()
    {
        Assert.Equal("12.3%", 0.1234m.ToPercent());
        Assert.Equal("12%", 0.1234m.ToPercent(0));
        Assert.Equal("0.0%", 0m.ToPercent());
        Assert.Equal("-5.0%", (-0.05m).ToPercent());
        // A comma here would mean the ambient culture leaked into formatting.
        Assert.DoesNotContain(",", 0.1234m.ToPercent(3));
    }

    [Fact]
    public void ToPercent_ClampsDigitCountInsteadOfThrowing()
    {
        // 0..4 are the formats the formatter carries; anything outside is clamped rather than rejected.
        Assert.Equal("12.3400%", 0.1234m.ToPercent(9));
        Assert.Equal("12%", 0.1234m.ToPercent(-3));
    }
}
