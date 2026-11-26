using System;
using System.Globalization;
using CSharpUtil.NumberUtil;
using CSharpUtil.StringUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Test cases for the second batch of helpers: enum/guid/timespan conversion, blank handling,
/// masking, splitting, truncation, the non-throwing JSON conversion and the culture invariant
/// parsing rule, plus the NumberUtil formatting helpers.
/// </summary>
public class UtilityExtensionsTests
{
    private enum Color
    {
        Red,
        Green,
        Blue
    }

    [Flags]
    private enum Permission
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    private class SampleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    // ------------------------------------------------------------------
    // ToEnum<T> / IsEnum<T>
    // ------------------------------------------------------------------

    [Fact]
    public void ToEnum_ParsesMemberNameIgnoringCaseAndWhitespace()
    {
        Assert.Equal(Color.Green, "green".ToEnum<Color>());
        // Enum.TryParse does not trim on its own, so the helper trims first.
        Assert.Equal(Color.Green, "  GREEN  ".ToEnum(Color.Red));
    }

    [Fact]
    public void ToEnum_UnknownText_ReturnsDefault()
    {
        Assert.Equal(default(Color), "purple".ToEnum<Color>());
        Assert.Equal(Color.Blue, "purple".ToEnum(Color.Blue));
        Assert.Equal(Color.Blue, "".ToEnum(Color.Blue));
        Assert.Equal(Color.Blue, ((string?)null).ToEnum(Color.Blue));
    }

    [Fact]
    public void ToEnum_NumericText_IsAcceptedOnlyForDefinedMembers()
    {
        // "1" maps to Green, which the enum defines.
        Assert.Equal(Color.Green, "1".ToEnum<Color>());
        // "99" is a valid int literal but no member has that value, so it is treated as corrupt input.
        Assert.Equal(Color.Red, "99".ToEnum(Color.Red));
    }

    [Fact]
    public void ToEnum_FlagsEnum_AcceptsCombinations()
    {
        Assert.Equal(Permission.Read | Permission.Write, "3".ToEnum<Permission>());
        Assert.Equal(Permission.Read | Permission.Write, "Read, Write".ToEnum<Permission>());
    }

    [Fact]
    public void IsEnum_AppliesTheSameRuleAsToEnum()
    {
        Assert.True("Blue".IsEnum<Color>());
        Assert.False("99".IsEnum<Color>());
        Assert.True("99".IsEnum<Permission>());     // flags enums allow arbitrary bit patterns
        Assert.False("".IsEnum<Color>());
    }

    // ------------------------------------------------------------------
    // ToGuid / IsGuid
    // ------------------------------------------------------------------

    [Fact]
    public void ToGuid_ParsesCanonicalAndCompactForms()
    {
        Guid expected = Guid.Parse("8f8f5a4e-1c2b-4f6a-9d3e-7a1b2c3d4e5f");

        Assert.Equal(expected, "8f8f5a4e-1c2b-4f6a-9d3e-7a1b2c3d4e5f".ToGuid(Guid.Empty));
        // Guid.TryParse also accepts the 32-char "N" format without dashes.
        Assert.Equal(expected, "8f8f5a4e1c2b4f6a9d3e7a1b2c3d4e5f".ToGuid(Guid.Empty));
    }

    [Fact]
    public void ToGuid_InvalidText_ReturnsSuppliedDefault()
    {
        Guid fallback = Guid.NewGuid();

        Assert.Equal(fallback, "not-a-guid".ToGuid(fallback));
        Assert.Null("not-a-guid".ToGuid((Guid?)null));
        Assert.Equal(Guid.Empty, "not-a-guid".ToGuid((Guid?)null) ?? Guid.Empty);
    }

    [Fact]
    public void IsGuid_ReflectsParseability()
    {
        Assert.True("8f8f5a4e-1c2b-4f6a-9d3e-7a1b2c3d4e5f".IsGuid());
        Assert.False("not-a-guid".IsGuid());
        Assert.False(((string?)null).IsGuid());
    }

    // ------------------------------------------------------------------
    // ToTimeSpan
    // ------------------------------------------------------------------

    [Fact]
    public void ToTimeSpan_ParsesInvariantForms()
    {
        Assert.Equal(new TimeSpan(0, 0, 5, 30), "00:05:30".ToTimeSpan(TimeSpan.Zero));
        Assert.Equal(TimeSpan.FromMinutes(90), "01:30:00".ToTimeSpan(TimeSpan.Zero));
    }

    [Fact]
    public void ToTimeSpan_InvalidText_ReturnsSuppliedDefault()
    {
        TimeSpan fallback = TimeSpan.FromSeconds(1);

        Assert.Equal(fallback, "abc".ToTimeSpan(fallback));
        Assert.Null("abc".ToTimeSpan((TimeSpan?)null));
    }

    // ------------------------------------------------------------------
    // TrimToNull / DefaultIfBlank
    // ------------------------------------------------------------------

    [Fact]
    public void TrimToNull_TurnsBlankIntoNull()
    {
        Assert.Null(((string?)null).TrimToNull());
        Assert.Null("".TrimToNull());
        Assert.Null("   \t ".TrimToNull());
        Assert.Equal("abc", "  abc  ".TrimToNull());
    }

    [Fact]
    public void DefaultIfBlank_KeepsOriginalTextWhenNotBlank()
    {
        Assert.Equal("fallback", ((string?)null).DefaultIfBlank("fallback"));
        Assert.Equal("fallback", "   ".DefaultIfBlank("fallback"));
        // The value is returned as-is, without trimming, so formatting is preserved.
        Assert.Equal(" abc ", " abc ".DefaultIfBlank("fallback"));
    }

    // ------------------------------------------------------------------
    // Mask / MaskMobile / MaskIdCard / MaskEmail
    // ------------------------------------------------------------------

    [Fact]
    public void Mask_KeepsEdgesAndHidesTheMiddle()
    {
        Assert.Equal("138****8000", "13800138000".Mask(3, 4));
        // 11 characters minus 3 kept leading and 4 kept trailing leaves 4 masked characters.
        Assert.Equal("138####8000", "13800138000".Mask(3, 4, '#'));

        // Nothing would be hidden, so the whole value is masked instead of leaking it.
        Assert.Equal("*****", "abcde".Mask(3, 4));
    }

    [Fact]
    public void Mask_NullOrEmpty_IsReturnedUnchanged()
    {
        Assert.Null(((string?)null).Mask(3, 4));
        Assert.Equal(string.Empty, "".Mask(3, 4));
    }

    [Fact]
    public void MaskMobile_KeepsThreeLeadingAndFourTrailingDigits()
    {
        Assert.Equal("138****8000", "13800138000".MaskMobile());
    }

    [Fact]
    public void MaskIdCard_KeepsRegionCodeAndLastFour()
    {
        Assert.Equal("110101********1234", "110101199001011234".MaskIdCard());
    }

    [Fact]
    public void MaskEmail_MasksLocalPartAndKeepsDomain()
    {
        Assert.Equal("zh******@example.com", "zhangsan@example.com".MaskEmail());
        Assert.Equal("*@b.com", "a@b.com".MaskEmail());
        Assert.Equal("ab*@b.com", "abc@b.com".MaskEmail());

        // Text without a usable local/domain split falls back to positional masking.
        Assert.Equal("n*******", "noatsign".MaskEmail());
        Assert.Equal("@*****", "@b.com".MaskEmail());
    }

    // ------------------------------------------------------------------
    // SplitTrim
    // ------------------------------------------------------------------

    [Fact]
    public void SplitTrim_TrimsPartsAndDropsEmptyOnes()
    {
        Assert.Equal(new[] { "a", "b", "c" }, "a, b,, c ".SplitTrim());
        Assert.Equal(new[] { "1", "2", "3" }, "1;2;3".SplitTrim(';'));
        Assert.Equal(new[] { "x", "y" }, "x|y|".SplitTrim('|'));
    }

    [Fact]
    public void SplitTrim_BlankInput_ReturnsEmptyArray()
    {
        Assert.Empty(((string?)null).SplitTrim());
        Assert.Empty("".SplitTrim());
        Assert.Empty(" , , ".SplitTrim());
    }

    // ------------------------------------------------------------------
    // Truncate
    // ------------------------------------------------------------------

    [Fact]
    public void Truncate_CutsToMaxLength()
    {
        Assert.Equal("abc", "abcdef".Truncate(3));
        Assert.Equal("abc...", "abcdef".Truncate(3, "..."));
        Assert.Equal("abcdef", "abcdef".Truncate(6));
        Assert.Equal("abcdef", "abcdef".Truncate(10));
    }

    [Fact]
    public void Truncate_NeverSplitsASurrogatePair()
    {
        // Each emoji is two UTF-16 chars; cutting at 3 would split the second emoji, so the cut backs off to 2.
        Assert.Equal("\U0001F600", "\U0001F600\U0001F600\U0001F600".Truncate(3));
    }

    [Fact]
    public void Truncate_NonPositiveLength_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, "abc".Truncate(0));
        Assert.Equal(string.Empty, "abc".Truncate(-1));
        Assert.Null(((string?)null).Truncate(3));
    }

    // ------------------------------------------------------------------
    // TryConvertTo<T>
    // ------------------------------------------------------------------

    [Fact]
    public void TryConvertTo_ValidJson_ReturnsTrueAndInstance()
    {
        Assert.True("{\"Id\":7,\"Name\":\"x\"}".TryConvertTo<SampleDto>(out SampleDto? dto));
        Assert.NotNull(dto);
        Assert.Equal(7, dto!.Id);
        Assert.Equal("x", dto.Name);
    }

    [Fact]
    public void TryConvertTo_InvalidJson_ReturnsFalseInsteadOfThrowing()
    {
        // JsonConvert would throw JsonReaderException here; this overload reports failure instead.
        Assert.False("{bad json".TryConvertTo<SampleDto>(out SampleDto? dto));
        Assert.Null(dto);
    }

    [Fact]
    public void TryConvertTo_BlankInput_ReturnsFalse()
    {
        Assert.False(((string?)null).TryConvertTo<SampleDto>(out _));
        Assert.False("   ".TryConvertTo<SampleDto>(out _));
    }

    // ------------------------------------------------------------------
    // Overload resolution for the To* families
    // ------------------------------------------------------------------

    [Fact]
    public void CallsWithoutArguments_BindToTheNonNullableOverloads()
    {
        // The nullable overloads take their default parameter without a default value, so these calls are
        // unambiguous and return the non-nullable type. Before that change they failed with CS0121.
        Assert.Equal(123, "123".ToInt());
        Assert.Equal(123L, "123".ToLong());
        Assert.Equal((short)123, "123".ToShort());
        Assert.Equal(1.5, "1.5".ToDouble());
        Assert.Equal(1.5f, "1.5".ToFloat());
        Assert.Equal(1.5m, "1.5".ToDecimal());
        Assert.False("abc".ToBool());

        // Requesting null-on-failure still works, and now needs an explicit argument.
        Assert.Null("abc".ToInt((int?)null));
        Assert.Null("abc".ToDecimal((decimal?)null));
    }

    // ------------------------------------------------------------------
    // Culture invariant parsing
    // ------------------------------------------------------------------

    [Fact]
    public void NumericAndDateParsing_IsCultureInvariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // de-DE uses '.' as the group separator, so the ambient-culture parse of "1.5" would return 15.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            Assert.Equal(1.5, "1.5".ToDouble(0));
            Assert.True("1.5".IsDouble());
            Assert.Equal(1.5f, "1.5".ToFloat(0f));
            Assert.Equal(1234.5m, "1,234.5".ToDecimal(0m));

            DateTime fallback = new DateTime(2000, 1, 1);
            DateTime parsed = "2026-11-07 10:00:00".ToDateTime(fallback);
            Assert.Equal(new DateTime(2026, 11, 7, 10, 0, 0), parsed);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void IntegerParsing_IsCultureInvariant()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            Assert.Equal(1234, "1234".ToInt(0));
            Assert.True("1234".IsInteger());
            // Integer style never accepts group separators, so "1.234" is a parse failure, not 1234.
            Assert.Equal(0, "1.234".ToInt(0));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ------------------------------------------------------------------
    // NumberUtil.FormatFileSize
    // ------------------------------------------------------------------

    [Fact]
    public void FormatFileSize_UsesBinaryUnits()
    {
        Assert.Equal("0 B", 0L.FormatFileSize());
        Assert.Equal("1023 B", 1023L.FormatFileSize());
        Assert.Equal("1.00 KB", 1024L.FormatFileSize());
        Assert.Equal("1.50 KB", 1536L.FormatFileSize());
        Assert.Equal("1.00 MB", 1048576L.FormatFileSize());
        Assert.Equal("1.00 GB", 1073741824L.FormatFileSize());
    }

    [Fact]
    public void FormatFileSize_RoundsAndPromotesAtBoundary()
    {
        // 1023.999 KB rounds to 1024.00 KB, which must be shown as 1.00 MB instead.
        Assert.Equal("1.00 MB", 1048575L.FormatFileSize());
        Assert.Equal("2 KB", 1536L.FormatFileSize(0));
        Assert.Equal("1.5000 KB", 1536L.FormatFileSize(4));
    }

    [Fact]
    public void FormatFileSize_NegativeInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (-1L).FormatFileSize());
    }

    [Fact]
    public void FormatFileSize_MaxUnitCapsThePromotion()
    {
        // Default (PB) keeps the automatic promotion chain untouched.
        Assert.Equal("1.00 GB", 1073741824L.FormatFileSize());
        Assert.Equal("1.00 TB", 1099511627776L.FormatFileSize());

        // Capped at KB: no promotion to MB/GB, the value stays in KB.
        Assert.Equal("1.50 KB", 1536L.FormatFileSize(2, SizeUnit.KB));
        Assert.Equal("1048576.00 KB", 1073741824L.FormatFileSize(2, SizeUnit.KB));

        // Capped at MB: stops at MB even though GB would be more readable.
        Assert.Equal("1024.00 MB", 1073741824L.FormatFileSize(2, SizeUnit.MB));

        // Capped at bytes: prints the raw count for every magnitude.
        Assert.Equal("1536 B", 1536L.FormatFileSize(2, SizeUnit.B));
        Assert.Equal("1073741824 B", 1073741824L.FormatFileSize(2, SizeUnit.B));
    }

    [Fact]
    public void FormatFileSize_AtCapTheBoundaryRoundOverflowsInsteadOfPromoting()
    {
        // 1048575 is 1023.999 KB. With a KB cap the unit may not move to MB, so the rounded value is shown
        // as "1024.00 KB" - the promotion is blocked by the cap, not by the rounding rule.
        Assert.Equal("1.00 MB", 1048575L.FormatFileSize(2, SizeUnit.MB));
        Assert.Equal("1024.00 KB", 1048575L.FormatFileSize(2, SizeUnit.KB));

        // Same overflow, no decimals: 1024 KB.
        Assert.Equal("1024 KB", 1048575L.FormatFileSize(0, SizeUnit.KB));
    }

    [Fact]
    public void FormatFileSize_PetabyteAndLongMaxValue()
    {
        // 1 PB = 1024^5; the default cap allows the top unit.
        Assert.Equal("1.00 PB", 1125899906842624L.FormatFileSize());

        // long.MaxValue is 2^63 - 1, a hair under 8192 PB, so two decimals round it up to "8192.00 PB".
        // PB is the last unit, so the cap blocks the promotion this rounding would otherwise trigger -
        // the same accepted overflow as "1024.00 KB" under a KB cap.
        Assert.Equal("8192.00 PB", long.MaxValue.FormatFileSize());
    }

    [Fact]
    public void FormatFileSize_UndefinedMaxUnit_Throws()
    {
        // The enum is public, so an out-of-range cast must be rejected instead of indexing the symbol array.
        Assert.Throws<ArgumentOutOfRangeException>(() => 1536L.FormatFileSize(2, (SizeUnit)6));
        Assert.Throws<ArgumentOutOfRangeException>(() => 1536L.FormatFileSize(2, (SizeUnit)(-1)));
    }

    [Fact]
    public void FormatFileSize_KeepsDecimalClampWithMaxUnit()
    {
        Assert.Equal("2 KB", 1536L.FormatFileSize(-1, SizeUnit.KB));
        Assert.Equal("1.5000 KB", 1536L.FormatFileSize(9, SizeUnit.KB));
    }

    [Fact]
    public void FormatFileSize_RoundsTiesAwayFromZeroLikeTheDocSays()
    {
        // 1.125 MB is an exact tie at two decimals. The printed value must follow the same rule that
        // decides promotion (away from zero), not the "F2" default of ties-to-even, which would print 1.12.
        Assert.Equal("1.13 MB", 1179648L.FormatFileSize());
        Assert.Equal("1.13 MB", 1179648L.FormatFileSize(2, SizeUnit.MB));
    }

    [Fact]
    public void FormatFileSize_RendersEveryDeclaredUnitSymbol()
    {
        // Guards the UnitSymbols array against drifting away from the public enum: feeding exactly one of
        // each unit must print that unit's own symbol as the suffix.
        foreach (SizeUnit unit in Enum.GetValues(typeof(SizeUnit)))
        {
            string name = Enum.GetName(typeof(SizeUnit), unit)!;
            long oneUnit = 1L << (10 * (int)unit);
            Assert.EndsWith(" " + name, oneUnit.FormatFileSize(2, unit), StringComparison.Ordinal);
        }
    }

    // ------------------------------------------------------------------
    // NumberUtil.ToChineseRmb
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(0, "零元整")]
    [InlineData(1, "壹元整")]
    [InlineData(10, "壹拾元整")]
    [InlineData(1001, "壹仟零壹元整")]
    [InlineData(10000, "壹万元整")]
    [InlineData(10001, "壹万零壹元整")]
    [InlineData(1000000, "壹佰万元整")]
    [InlineData(100000000, "壹亿元整")]
    [InlineData(100000001, "壹亿零壹元整")]
    [InlineData(12345678, "壹仟贰佰叁拾肆万伍仟陆佰柒拾捌元整")]
    public void ToChineseRmb_IntegerAmounts(decimal amount, string expected)
    {
        Assert.Equal(expected, amount.ToChineseRmb());
    }

    [Theory]
    [InlineData(1234.56, "壹仟贰佰叁拾肆元伍角陆分")]
    [InlineData(1234.5, "壹仟贰佰叁拾肆元伍角整")]
    [InlineData(1234.05, "壹仟贰佰叁拾肆元零伍分")]
    [InlineData(0.5, "零元伍角整")]
    [InlineData(0.05, "零元零伍分")]
    [InlineData(-1234.56, "负壹仟贰佰叁拾肆元伍角陆分")]
    [InlineData(100.999, "壹佰零壹元整")]   // 100.999 rounds to 101.00 away from zero
    public void ToChineseRmb_DecimalAmounts(decimal amount, string expected)
    {
        Assert.Equal(expected, amount.ToChineseRmb());
    }

    [Fact]
    public void ToChineseRmb_UnsupportedMagnitude_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 10000000000000000m.ToChineseRmb());
    }

    // ------------------------------------------------------------------
    // NumberUtil.RoundTo
    // ------------------------------------------------------------------

    [Fact]
    public void RoundTo_DefaultsToAwayFromZero()
    {
        Assert.Equal(2.35m, 2.345m.RoundTo(2));
        Assert.Equal(-2.35m, (-2.345m).RoundTo(2));
        Assert.Equal(2m, 1.5m.RoundTo(0));
    }

    [Fact]
    public void RoundTo_ExplicitModeIsHonoured()
    {
        // Banker's rounding sends 2.345 to the even digit.
        Assert.Equal(2.34m, 2.345m.RoundTo(2, MidpointRounding.ToEven));
    }

    [Fact]
    public void RoundTo_DigitsOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 1.5m.RoundTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => 1.5m.RoundTo(29));
    }
}
