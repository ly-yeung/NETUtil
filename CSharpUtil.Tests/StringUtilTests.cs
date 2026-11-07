using System;
using System.Collections.Generic;
using CSharpUtil.StringUtil;
using Newtonsoft.Json;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Test cases for every public helper in <see cref="CSharpUtil.StringUtil.StringUtil"/>.
/// Each case documents the observed behavior of the underlying BCL call (TryParse / JsonConvert),
/// so a failure here means the library contract or the BCL behavior changed.
/// </summary>
public class StringUtilTests
{
    private class SampleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool Active { get; set; }
        public List<int>? Tags { get; set; }
    }

    // ------------------------------------------------------------------
    // ConvertTo<T>
    // ------------------------------------------------------------------

    [Fact]
    public void ConvertTo_ValidJson_ReturnsDeserializedInstance()
    {
        var dto = "{\"Id\":42,\"Name\":\"doubao\",\"Active\":true,\"Tags\":[1,2,3]}".ConvertTo<SampleDto>();

        Assert.NotNull(dto);
        Assert.Equal(42, dto!.Id);
        Assert.Equal("doubao", dto.Name);
        Assert.True(dto.Active);
        Assert.Equal(new[] { 1, 2, 3 }, dto.Tags);
    }

    [Fact]
    public void ConvertTo_UnknownProperties_AreIgnored()
    {
        // Newtonsoft.Json ignores members that do not exist on the target type.
        var dto = "{\"Id\":1,\"Unknown\":\"ignored\"}".ConvertTo<SampleDto>();

        Assert.NotNull(dto);
        Assert.Equal(1, dto!.Id);
    }

    [Fact]
    public void ConvertTo_MissingProperties_KeepDefaultValues()
    {
        var dto = "{\"Id\":7}".ConvertTo<SampleDto>();

        Assert.NotNull(dto);
        Assert.Equal(7, dto!.Id);
        Assert.Null(dto.Name);
        Assert.False(dto.Active);
        Assert.Null(dto.Tags);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void ConvertTo_BlankInput_ReturnsDefault(string? input)
    {
        // Blank input is short-circuited before JsonConvert, so no exception is raised.
        Assert.Null(input.ConvertTo<SampleDto>());

        // createNew = true turns the blank case into an empty instance instead of null.
        var created = input.ConvertTo<SampleDto>(true);
        Assert.NotNull(created);
        Assert.Equal(0, created!.Id);
    }

    [Fact]
    public void ConvertTo_EmptyObject_ReturnsInstanceWithDefaults()
    {
        // "{}" is not blank, so it goes through JsonConvert and yields a non-null instance.
        var dto = "{}".ConvertTo<SampleDto>();

        Assert.NotNull(dto);
        Assert.Equal(0, dto!.Id);
        Assert.Null(dto.Name);
    }

    [Fact]
    public void ConvertTo_JsonNull_ReturnsNull()
    {
        Assert.Null("null".ConvertTo<SampleDto>());
    }

    [Theory]
    [InlineData("{bad json")]
    [InlineData("{\"Id\":}")]
    public void ConvertTo_MalformedJson_ThrowsJsonException(string input)
    {
        // Newtonsoft throws JsonReaderException, a subclass of JsonException, so use ThrowsAny.
        Assert.ThrowsAny<JsonException>(() => input.ConvertTo<SampleDto>());
    }

    [Fact]
    public void ConvertTo_ArrayForObjectTarget_ThrowsJsonException()
    {
        // A JSON array cannot be materialized into a class instance: JsonSerializationException, a subclass of JsonException.
        Assert.ThrowsAny<JsonException>(() => "[1,2,3]".ConvertTo<SampleDto>());
    }

    // ------------------------------------------------------------------
    // ToInt (both overloads)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("123", 123)]
    [InlineData("-45", -45)]
    [InlineData("+7", 7)]
    [InlineData(" 42 ", 42)]           // leading/trailing whitespace is allowed by NumberStyles.Integer
    [InlineData("0007", 7)]            // leading zeros are skipped
    [InlineData("1.5", 0)]             // not an integer -> default
    [InlineData("2147483648", 0)]      // overflow -> default, no exception
    [InlineData("abc", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void ToInt_ReturnsParsedValueOrDefault(string? input, int expected)
    {
        Assert.Equal(expected, input.ToInt(0));
    }

    [Fact]
    public void ToInt_CustomDefault_IsReturnedOnFailure()
    {
        Assert.Equal(7, "abc".ToInt(7));
        Assert.Equal(123, "123".ToInt(7));
    }

    [Fact]
    public void ToInt_NullableOverload_ReturnsNullOnFailure()
    {
        Assert.Null("abc".ToInt((int?)null));
        Assert.Equal(9, "abc".ToInt(9));
        Assert.Equal(123, "123".ToInt((int?)null));
    }

    // ------------------------------------------------------------------
    // ToShort (both overloads)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("123", 123)]
    [InlineData("-32768", -32768)]
    [InlineData("32767", 32767)]
    [InlineData("32768", 0)]           // one past the upper bound
    [InlineData("1.5", 0)]
    [InlineData("abc", 0)]
    [InlineData(null, 0)]
    public void ToShort_ReturnsParsedValueOrDefault(string? input, int expected)
    {
        Assert.Equal(expected, (int)input.ToShort((short)0));
    }

    [Fact]
    public void ToShort_NullableOverload_ReturnsNullOnFailure()
    {
        Assert.Null("abc".ToShort((short?)null));
        Assert.Equal((short)123, "123".ToShort((short?)9));
    }

    // ------------------------------------------------------------------
    // ToLong (both overloads)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("9223372036854775807", 9223372036854775807L)]
    [InlineData("-9223372036854775808", -9223372036854775808L)]
    [InlineData("9223372036854775808", 0L)]   // one past long.MaxValue
    [InlineData("abc", 0L)]
    [InlineData(null, 0L)]
    public void ToLong_ReturnsParsedValueOrDefault(string? input, long expected)
    {
        Assert.Equal(expected, input.ToLong(0L));
    }

    [Fact]
    public void ToLong_NullableOverload_ReturnsNullOnFailure()
    {
        Assert.Null("abc".ToLong((long?)null));
        Assert.Equal(123L, "123".ToLong((long?)0L));
    }

    // ------------------------------------------------------------------
    // ToDouble (both overloads)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("-0.25", -0.25)]
    [InlineData("1e5", 100000)]        // exponent notation is accepted by NumberStyles.Float
    [InlineData("abc", 0)]
    [InlineData(null, 0)]
    public void ToDouble_ReturnsParsedValueOrDefault(string? input, double expected)
    {
        Assert.Equal(expected, input.ToDouble(0));
    }

    [Fact]
    public void ToDouble_NonFiniteResults_FallBackToDefault()
    {
        // "1e400" parses to Infinity and "NaN" parses to NaN, but both are treated as "no usable value"
        // so the caller receives the supplied default instead of a sentinel.
        Assert.Equal(-1.0, "1e400".ToDouble(-1));
        Assert.Equal(-1.0, "-1e400".ToDouble(-1));
        Assert.Equal(-1.0, "NaN".ToDouble(-1));
    }

    [Fact]
    public void ToDouble_NullableOverload_AlsoRejectsNonFinite()
    {
        Assert.Null("1e400".ToDouble((double?)null));
        Assert.Null("NaN".ToDouble((double?)null));
        Assert.Equal(1.5, "1.5".ToDouble((double?)null));
    }

    [Fact]
    public void ToDouble_NullableOverload_ReturnsNullOnFailure()
    {
        Assert.Null("abc".ToDouble((double?)null));
    }

    // ------------------------------------------------------------------
    // ToFloat (both overloads)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("1e5", 100000)]
    [InlineData("abc", 0)]
    [InlineData(null, 0)]
    public void ToFloat_ReturnsParsedValueOrDefault(string? input, double expected)
    {
        Assert.Equal(expected, (double)input.ToFloat(0f), 6);
    }

    [Fact]
    public void ToFloat_NullableOverload_ReturnsNullOnFailure()
    {
        Assert.Null("abc".ToFloat((float?)null));
    }

    [Fact]
    public void ToFloat_NonFiniteResults_FallBackToDefault()
    {
        // Same overflow-to-Infinity semantics as double, with a much smaller range ("1e40" -> Infinity).
        Assert.Equal(-1f, "1e40".ToFloat(-1f));
        Assert.Equal(-1f, "NaN".ToFloat(-1f));
        Assert.Null("1e40".ToFloat((float?)null));
    }

    // ------------------------------------------------------------------
    // ToDecimal (both overloads)
    // ------------------------------------------------------------------

    [Fact]
    public void ToDecimal_ParsesPlainAndGroupedNumbers()
    {
        Assert.Equal(1234.5m, "1,234.5".ToDecimal(0m));   // NumberStyles.Number allows group separators
        Assert.Equal(-0.125m, "-0.125".ToDecimal(0m));
        Assert.Equal(decimal.MaxValue, "79228162514264337593543950335".ToDecimal(0m));
    }

    [Fact]
    public void ToDecimal_ExponentNotation_FallsBackToDefault()
    {
        // decimal uses NumberStyles.Number, which has no AllowExponent: "1e5" is not a valid decimal.
        Assert.Equal(0m, "1e5".ToDecimal(0m));
    }

    [Fact]
    public void ToDecimal_InvalidInput_ReturnsDefault()
    {
        Assert.Equal(-1m, "abc".ToDecimal(-1m));
        Assert.Null("abc".ToDecimal((decimal?)null));
    }

    // ------------------------------------------------------------------
    // ToBool (both overloads)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]         // bool.TryParse is case-insensitive
    [InlineData(" false ", false)]
    [InlineData("1", false)]           // "1" is NOT a bool for bool.TryParse
    [InlineData("yes", false)]
    [InlineData(null, false)]
    public void ToBool_ReturnsParsedValueOrDefaultValue(string? input, bool expected)
    {
        Assert.Equal(expected, input.ToBool(false));
    }

    [Fact]
    public void ToBool_CustomDefaultAndNullableOverload()
    {
        Assert.True("abc".ToBool(true));
        Assert.Null("abc".ToBool((bool?)null));
        Assert.True("true".ToBool((bool?)false));
    }

    // ------------------------------------------------------------------
    // ToDateTime (both overloads)
    // ------------------------------------------------------------------

    [Fact]
    public void ToDateTime_ParsesIsoLikeText()
    {
        var fallback = new DateTime(2000, 1, 1);
        var parsed = "2026-11-07 10:00:00".ToDateTime(fallback);

        Assert.Equal(new DateTime(2026, 11, 7, 10, 0, 0), parsed);
    }

    [Fact]
    public void ToDateTime_InvalidInput_ReturnsSuppliedDefault()
    {
        var fallback = new DateTime(2000, 1, 1);
        Assert.Equal(fallback, "abc".ToDateTime(fallback));
    }

    [Fact]
    public void ToDateTime_NullableOverload_ReturnsNullOnFailure()
    {
        Assert.Null("abc".ToDateTime((DateTime?)null));
        Assert.Equal(new DateTime(2026, 11, 7), "2026-11-07".ToDateTime((DateTime?)null));
    }

    // ------------------------------------------------------------------
    // IsNumber / IsInteger / IsLong / IsShort / IsFloat / IsDecimal / IsBool / IsDouble / IsDateTime
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("1.5", true)]
    [InlineData("1e5", true)]
    [InlineData("123", true)]
    [InlineData("1e400", false)]       // parses to Infinity -> not a usable number
    [InlineData("NaN", false)]         // NaN literal -> not a usable number
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsNumber_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsNumber());
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("-2147483648", true)]
    [InlineData("2147483648", false)]  // int range is enforced
    [InlineData("1.5", false)]
    [InlineData(null, false)]
    public void IsInteger_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsInteger());
    }

    [Theory]
    [InlineData("9223372036854775807", true)]
    [InlineData("9223372036854775808", false)]
    [InlineData("1.5", false)]
    [InlineData(null, false)]
    public void IsLong_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsLong());
    }

    [Theory]
    [InlineData("32767", true)]
    [InlineData("32768", false)]
    [InlineData("1.5", false)]
    [InlineData(null, false)]
    public void IsShort_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsShort());
    }

    [Theory]
    [InlineData("1.5", true)]
    [InlineData("1e5", true)]
    [InlineData("1e40", false)]        // float overflow -> Infinity -> rejected
    [InlineData("NaN", false)]
    [InlineData("abc", false)]
    [InlineData(null, false)]
    public void IsFloat_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsFloat());
    }

    [Theory]
    [InlineData("1,234.5", true)]      // group separators are allowed
    [InlineData("1e5", false)]         // decimal itself is the backing type: no exponent support
    [InlineData("abc", false)]
    [InlineData(null, false)]
    public void IsDecimal_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsDecimal());
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", false)]
    [InlineData("abc", false)]
    [InlineData(null, false)]
    public void IsBool_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsBool());
    }

    [Theory]
    [InlineData("1e5", true)]          // double accepts exponent notation
    [InlineData("1e400", false)]       // parses to Infinity, but non-finite values are rejected
    [InlineData("NaN", false)]
    [InlineData("abc", false)]
    [InlineData(null, false)]
    public void IsDouble_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsDouble());
    }

    [Theory]
    [InlineData("2026-11-07", true)]
    [InlineData("2026-11-07 10:00:00", true)]
    [InlineData("abc", false)]
    [InlineData(null, false)]
    public void IsDateTime_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsDateTime());
    }

    // ------------------------------------------------------------------
    // Cross-check: IsDecimal and IsDouble intentionally differ
    // ------------------------------------------------------------------

    [Fact]
    public void IsDecimal_And_IsDouble_DifferOnExponentNotation()
    {
        // Documents the asymmetry between the two predicates: decimal rejects "1e5", double accepts it.
        Assert.False("1e5".IsDecimal());
        Assert.True("1e5".IsDouble());
    }

    [Fact]
    public void IsDouble_And_ToDouble_AgreeOnNonFiniteInput()
    {
        // Option A: predicates and conversions share the same rule. A successful check now guarantees
        // ToDouble returns the parsed value rather than the fallback.
        Assert.False("1e400".IsDouble());
        Assert.Equal(0, "1e400".ToDouble(0));
        Assert.False("NaN".IsDouble());
        Assert.Equal(0, "NaN".ToDouble(0));

        Assert.True("1.5".IsDouble());
        Assert.Equal(1.5, "1.5".ToDouble(0));
    }

    [Fact]
    public void IsFloat_And_ToFloat_AgreeOnNonFiniteInput()
    {
        Assert.False("1e40".IsFloat());
        Assert.Equal(0f, "1e40".ToFloat(0f));
        Assert.True("1.5".IsFloat());
        Assert.Equal(1.5f, "1.5".ToFloat(0f));
    }
}
