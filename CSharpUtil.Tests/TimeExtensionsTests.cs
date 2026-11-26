using System;
using CSharpUtil.DateTimeUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Time helpers added in 0.3.0: Unix conversion both ways, timestamp text parsing, day boundaries and the
/// Version-style encoding used for ordering.
/// </summary>
public class TimeExtensionsTests
{
    private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UnixSeconds_ZeroAtEpochAndRoundTrips()
    {
        Assert.Equal(0L, Epoch.ToUnixSeconds());
        Assert.Equal(Epoch, 0L.FromUnixSeconds());
        Assert.Equal(DateTimeKind.Utc, 0L.FromUnixSeconds().Kind);

        DateTime sample = new(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc);
        Assert.Equal(sample, sample.ToUnixSeconds().FromUnixSeconds());
    }

    [Fact]
    public void UnixMilliseconds_KeepsMillisecondResolution()
    {
        DateTime sample = new(2024, 3, 5, 6, 7, 8, 123, DateTimeKind.Utc);
        Assert.Equal(sample, sample.ToUnixMilliseconds().FromUnixMilliseconds());
        Assert.Equal(sample.ToUnixSeconds(), sample.ToUnixMilliseconds() / 1000);
    }

    [Fact]
    public void UnixConversions_NormalizeLocalAndUnspecifiedToUtc()
    {
        DateTime unspecified = new(2024, 3, 5, 0, 0, 0, DateTimeKind.Unspecified);
        // Unspecified is documented as local time, so the result must match the local-time conversion.
        Assert.Equal(unspecified.ToUniversalTime().ToUnixSeconds(), unspecified.ToUnixSeconds());
    }

    [Fact]
    public void FromUnix_FarOutOfRangeValueThrowsInsteadOfWrapping()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => long.MaxValue.FromUnixSeconds());
        Assert.Throws<ArgumentOutOfRangeException>(() => long.MinValue.FromUnixMilliseconds());
    }

    [Fact]
    public void IsUnixTimestamp_AcceptsTenAndThirteenDigitsOnly()
    {
        Assert.True("1712345678".IsUnixTimestamp());
        Assert.True("1712345678901".IsUnixTimestamp());

        Assert.False("171234567".IsUnixTimestamp());   // 9 digits: too short, likely a legacy id
        Assert.False("2024".IsUnixTimestamp());        // a year must not become a date
        Assert.False("17123456789012".IsUnixTimestamp());
        Assert.False("17123456789a".IsUnixTimestamp());
        Assert.False(((string?)null).IsUnixTimestamp());
    }

    [Fact]
    public void ToTimestamp_PicksUnitFromTheDigitCount()
    {
        Assert.Equal(1712345678L.FromUnixSeconds(), "1712345678".ToTimestamp());
        Assert.Equal(1712345678901L.FromUnixMilliseconds(), "1712345678901".ToTimestamp());

        Assert.Null("2024".ToTimestamp());
        Assert.Null("".ToTimestamp());
        Assert.Null(((string?)null).ToTimestamp());
    }

    [Fact]
    public void StartOfDay_And_EndOfDay_BracketTheWholeDay()
    {
        DateTime sample = new(2024, 3, 5, 13, 45, 30, 500, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc), sample.StartOfDay());
        Assert.Equal(new DateTime(2024, 3, 5, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), sample.EndOfDay());
        Assert.Equal(DateTimeKind.Utc, sample.StartOfDay().Kind);
        Assert.Equal(DateTimeKind.Utc, sample.EndOfDay().Kind);

        // The last tick of the day is inside the range; the next day's first tick is not.
        Assert.True(sample.EndOfDay().IsBetween(sample.StartOfDay(), sample.EndOfDay()));
        Assert.False(sample.EndOfDay().AddTicks(1).IsBetween(sample.StartOfDay(), sample.EndOfDay()));
    }

    [Fact]
    public void EndOfDay_DoesNotOverflowAtMaxValue()
    {
        Assert.Equal(DateTime.MaxValue, DateTime.MaxValue.EndOfDay());
    }

    [Fact]
    public void IsBetween_IncludesBothEnds()
    {
        DateTime min = new(2024, 1, 1);
        DateTime max = new(2024, 12, 31);

        Assert.True(min.IsBetween(min, max));
        Assert.True(max.IsBetween(min, max));
        Assert.True(new DateTime(2024, 6, 1).IsBetween(min, max));
        Assert.False(min.AddTicks(-1).IsBetween(min, max));
    }

    [Fact]
    public void ToVersion_EncodesYearMonthDayAndSeconds()
    {
        DateTime sample = new(2026, 11, 26, 13, 45, 59, DateTimeKind.Unspecified);
        Version version = sample.ToVersion();

        Assert.Equal(2026, version.Major);
        Assert.Equal(11, version.Minor);
        Assert.Equal(26, version.Build);
        Assert.Equal(13 * 3600 + 45 * 60 + 59, version.Revision);
    }

    [Fact]
    public void VersionOrdering_MatchesDateTimeOrdering()
    {
        DateTime earlier = new(2026, 11, 26, 13, 45, 59);
        DateTime later = new(2026, 11, 26, 13, 46, 0);

        Assert.True(earlier.ToVersion().CompareTo(later.ToVersion()) < 0);
        Assert.True(new DateTime(2025, 12, 31, 23, 59, 59).ToVersion()
            .CompareTo(new DateTime(2026, 1, 1, 0, 0, 0).ToVersion()) < 0);
    }

    [Fact]
    public void ToDateTime_ReversesToVersionWithinOneSecond()
    {
        DateTime sample = new(2026, 11, 26, 13, 45, 59, DateTimeKind.Unspecified);
        Assert.Equal(sample, sample.ToVersion().ToDateTime());
    }

    [Fact]
    public void ToDateTime_ReturnsNullForImpossibleVersions()
    {
        Assert.Null(((Version?)null).ToDateTime());
        // Month 13 cannot form a date.
        Assert.Null(new Version(2026, 13, 1, 0).ToDateTime());
        // A Version with two parts has Build == -1, which the DateTime constructor rejects.
        Assert.Null(new Version(1, 2).ToDateTime());
        // 86400 seconds does not exist inside a day.
        Assert.Null(new Version(2026, 1, 1, 86400).ToDateTime());
    }

    [Fact]
    public void ToVersionStamp_IsTicksAndKeepsFullResolution()
    {
        DateTime a = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime b = a.AddTicks(1);

        Assert.Equal(a.Ticks, a.ToVersionStamp());
        // One tick apart is still ordered correctly, unlike a Version which stops at whole seconds.
        Assert.True(a.ToVersionStamp() < b.ToVersionStamp());
    }

    [Theory]
    [InlineData(VersionPrecision.Hour, 13)]
    [InlineData(VersionPrecision.Minute, 825)]
    [InlineData(VersionPrecision.Second, 49559)]
    [InlineData(VersionPrecision.Millisecond, 49559123)]
    public void ToVersion_HonoursPrecision(VersionPrecision precision, int expectedRevision)
    {
        DateTime sample = new(2026, 11, 26, 13, 45, 59, 123);
        Version version = sample.ToVersion(precision);

        Assert.Equal(2026, version.Major);
        Assert.Equal(11, version.Minor);
        Assert.Equal(26, version.Build);
        Assert.Equal(expectedRevision, version.Revision);
    }

    [Theory]
    [InlineData(VersionPrecision.Hour)]
    [InlineData(VersionPrecision.Minute)]
    [InlineData(VersionPrecision.Second)]
    [InlineData(VersionPrecision.Millisecond)]
    public void ToVersion_RoundTripsAtEveryPrecision(VersionPrecision precision)
    {
        DateTime sample = new(2026, 11, 26, 13, 45, 59, 123);
        DateTime expected = TruncateTo(sample, precision);

        Assert.Equal(expected, sample.ToVersion(precision).ToDateTime(precision));
    }

    [Fact]
    public void ToVersion_CoarsePrecisionCollapsesNearbyInstants()
    {
        DateTime first = new(2026, 11, 26, 13, 45, 1);
        DateTime second = new(2026, 11, 26, 13, 45, 31);

        // 30 seconds apart: equal at minute and hour precision, ordered at second precision.
        Assert.Equal(first.ToVersion(VersionPrecision.Minute), second.ToVersion(VersionPrecision.Minute));
        Assert.Equal(first.ToVersion(VersionPrecision.Hour), second.ToVersion(VersionPrecision.Hour));
        Assert.True(first.ToVersion(VersionPrecision.Second).CompareTo(second.ToVersion(VersionPrecision.Second)) < 0);
    }

    [Fact]
    public void ToDateTime_RejectsRevisionOutsidePrecisionRange()
    {
        Assert.Null(new Version(2026, 1, 1, 24).ToDateTime(VersionPrecision.Hour));
        Assert.Null(new Version(2026, 1, 1, 1440).ToDateTime(VersionPrecision.Minute));
        Assert.Null(new Version(2026, 1, 1, 86400).ToDateTime(VersionPrecision.Second));
        Assert.Null(new Version(2026, 1, 1, 86400000).ToDateTime(VersionPrecision.Millisecond));

        Assert.Equal(new DateTime(2026, 1, 1, 23, 0, 0), new Version(2026, 1, 1, 23).ToDateTime(VersionPrecision.Hour));
    }

    [Fact]
    public void ToVersion_RejectsUndefinedPrecision()
    {
        DateTime sample = new(2026, 11, 26);
        Assert.Throws<ArgumentOutOfRangeException>(() => sample.ToVersion((VersionPrecision)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Version(2026, 11, 26, 0).ToDateTime((VersionPrecision)99));
    }

    private static DateTime TruncateTo(DateTime value, VersionPrecision precision)
    {
        long unit = precision switch
        {
            VersionPrecision.Hour => TimeSpan.TicksPerHour,
            VersionPrecision.Minute => TimeSpan.TicksPerMinute,
            VersionPrecision.Millisecond => TimeSpan.TicksPerMillisecond,
            _ => TimeSpan.TicksPerSecond
        };

        return new DateTime(value.Ticks - value.Ticks % unit, value.Kind);
    }
}
