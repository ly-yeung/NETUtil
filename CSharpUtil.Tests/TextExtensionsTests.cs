using System;
using System.Collections.Generic;
using CSharpUtil.StringUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Text helpers added in 0.3.0: blank checks, white-space normalization, non-blank joining, delimiter cutting
/// and the UTF-8/Base64 family.
/// </summary>
public class TextExtensionsTests
{
    // ------------------------------------------------------------------
    // IsNullOrWhiteSpace / IsNullOrEmpty / EmptyIfNull
    // ------------------------------------------------------------------

    [Fact]
    public void IsNullOrWhiteSpace_AcceptsNullEmptyAndBlank()
    {
        Assert.True(((string?)null).IsNullOrWhiteSpace());
        Assert.True(string.Empty.IsNullOrWhiteSpace());
        Assert.True(" \t\r\n ".IsNullOrWhiteSpace());
        Assert.False(" a ".IsNullOrWhiteSpace());
    }

    [Fact]
    public void IsNullOrEmpty_TreatsWhiteSpaceAsContent()
    {
        Assert.True(((string?)null).IsNullOrEmpty());
        Assert.True(string.Empty.IsNullOrEmpty());
        Assert.False(" ".IsNullOrEmpty());
        Assert.False("a".IsNullOrEmpty());
    }

    [Fact]
    public void EmptyIfNull_ReplacesNullWithEmptyAndLeavesOtherValuesAlone()
    {
        Assert.Equal(string.Empty, ((string?)null).EmptyIfNull());
        Assert.Equal(" ", " ".EmptyIfNull());
        string value = "abc";
        // Non-null input must be handed back untouched, not copied.
        Assert.Same(value, value.EmptyIfNull());
    }

    // ------------------------------------------------------------------
    // NormalizeSpace
    // ------------------------------------------------------------------

    [Fact]
    public void NormalizeSpace_TrimsAndCollapsesRuns()
    {
        Assert.Equal("a b", "  a \t b ".NormalizeSpace());
        Assert.Equal("a b c", "a\r\n\r\nb   c".NormalizeSpace());
        Assert.Equal(string.Empty, "   \t\n".NormalizeSpace());
        Assert.Equal(string.Empty, ((string?)null).NormalizeSpace());
    }

    [Fact]
    public void NormalizeSpace_LeavesCleanValuesUnchangedWithoutAllocating()
    {
        string clean = "alpha beta";
        Assert.Same(clean, clean.NormalizeSpace());

        string single = "a";
        Assert.Same(single, single.NormalizeSpace());
    }

    [Fact]
    public void NormalizeSpace_SingleSpaceBetweenWordsIsAlreadyClean()
    {
        // Exactly one space on each side of the word is the normalized shape, so it must not be rewritten.
        string already = "a b c d";
        Assert.Same(already, already.NormalizeSpace());
    }

    // ------------------------------------------------------------------
    // JoinNonEmpty
    // ------------------------------------------------------------------

    [Fact]
    public void JoinNonEmpty_DropsNullsAndBlanks()
    {
        Assert.Equal("a,b", CSharpUtil.StringUtil.StringUtil.JoinNonEmpty(",", new List<string?> { "a", null, "", " ", "b" }));
        Assert.Equal(" a -b", CSharpUtil.StringUtil.StringUtil.JoinNonEmpty("-", new[] { " a ", " ", "b" }));
        Assert.Equal(string.Empty, CSharpUtil.StringUtil.StringUtil.JoinNonEmpty(",", new List<string?> { null, " " }));
        Assert.Equal(string.Empty, CSharpUtil.StringUtil.StringUtil.JoinNonEmpty(",", (IEnumerable<string?>?)null));
    }

    [Fact]
    public void JoinNonEmpty_ParamsOverloadWorksForLiterals()
    {
        Assert.Equal("x,y", CSharpUtil.StringUtil.StringUtil.JoinNonEmpty(",", "x", "", "y"));
    }

    [Fact]
    public void JoinNonEmpty_KeepsOriginalSpacingOfTheValues()
    {
        // Joining is not normalizing: the caller's text is preserved as given.
        Assert.Equal(" a , b ", CSharpUtil.StringUtil.StringUtil.JoinNonEmpty(",", " a ", " b "));
    }

    // ------------------------------------------------------------------
    // CutBefore / CutAfter
    // ------------------------------------------------------------------

    [Fact]
    public void CutBefore_ReturnsHeadOrWholeValue()
    {
        Assert.Equal("a", "a=b".CutBefore("="));
        Assert.Equal("a", "a=b=c".CutBefore('='));
        Assert.Equal("a=b", "a=b".CutBefore(":"));
        Assert.Equal("a=b", "a=b".CutBefore((string?)null));
        Assert.Equal("a=b", "a=b".CutBefore(string.Empty));
        Assert.Null(((string?)null).CutBefore("="));
    }

    [Fact]
    public void CutAfter_ReturnsTailOrWholeValue()
    {
        Assert.Equal("b", "a=b".CutAfter("="));
        Assert.Equal("b=c", "a=b=c".CutAfter('='));
        Assert.Equal("a=b", "a=b".CutAfter(":"));
        Assert.Equal("a=b", "a=b".CutAfter(string.Empty));
        Assert.Equal(string.Empty, "a=".CutAfter("="));
        Assert.Null(((string?)null).CutAfter("="));
    }

    // ------------------------------------------------------------------
    // UTF-8
    // ------------------------------------------------------------------

    [Fact]
    public void Utf8Bytes_RoundTripsNonAsciiText()
    {
        const string text = "中文 abc";
        byte[] bytes = text.ToUtf8Bytes();
        Assert.Equal(text, bytes.FromUtf8Bytes());
        Assert.Empty(((string?)null).ToUtf8Bytes());
        Assert.Empty(string.Empty.ToUtf8Bytes());
        Assert.Equal(string.Empty, ((byte[]?)null).FromUtf8Bytes());
    }

    // ------------------------------------------------------------------
    // Base64 / Base64Url
    // ------------------------------------------------------------------

    [Fact]
    public void Base64_RoundTrips()
    {
        byte[] data = { 1, 2, 3, 250, 251 };
        string encoded = data.ToBase64();
        Assert.Equal(data, encoded.FromBase64());
        Assert.Equal(string.Empty, ((byte[]?)null).ToBase64());
        Assert.Null(((string?)null).FromBase64());
        Assert.Null("not base64!".FromBase64());
    }

    [Fact]
    public void Base64Url_UsesUrlSafeAlphabetAndNoPadding()
    {
        // 0xFF 0xFE produces '+' and '/' in standard Base64, which is exactly the case being replaced.
        byte[] data = { 0xFB, 0xFF };
        string standard = data.ToBase64();
        Assert.Contains("+", standard);

        string urlSafe = data.ToBase64Url();
        Assert.DoesNotContain("+", urlSafe);
        Assert.DoesNotContain("/", urlSafe);
        Assert.DoesNotContain("=", urlSafe);
        Assert.Equal(data, urlSafe.FromBase64Url());
    }

    [Fact]
    public void Base64Url_RoundTripsEveryResidueLength()
    {
        for (int length = 1; length <= 7; length++)
        {
            byte[] data = new byte[length];
            for (int index = 0; index < length; index++)
            {
                data[index] = (byte)(index + 1);
            }

            Assert.Equal(data, data.ToBase64Url().FromBase64Url());
        }
    }

    [Fact]
    public void Base64Url_RejectsMalformedInput()
    {
        Assert.Null(((string?)null).FromBase64Url());
        Assert.Null(string.Empty.FromBase64Url());
        // A length of 4n+1 is impossible for Base64, so it is refused before padding is attempted.
        Assert.Null("abcde".FromBase64Url());
        Assert.Null("!!!!".FromBase64Url());
    }

    // ------------------------------------------------------------------
    // EnsureMaxLength / EnsureNotEmpty
    // ------------------------------------------------------------------

    [Fact]
    public void EnsureMaxLength_ReturnsTheValueWhenItFits()
    {
        Assert.Equal("abcd", "abcd".EnsureMaxLength(4));
        Assert.Equal("abc", "abc".EnsureMaxLength(10));
        Assert.Null(((string?)null).EnsureMaxLength(3));
        Assert.Equal(string.Empty, string.Empty.EnsureMaxLength(0));
    }

    [Fact]
    public void EnsureMaxLength_ThrowsWithTheLengthInTheMessage()
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => "abcdef".EnsureMaxLength(3, nameof(error)));
        Assert.Contains("6", error.Message);
        Assert.Contains("3", error.Message);
        Assert.Null(error.ActualValue);
    }

    [Fact]
    public void EnsureMaxLength_RejectsNegativeLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".EnsureMaxLength(-1));
    }

    [Fact]
    public void EnsureNotEmpty_ReturnsTheValueWhenItCarriesContent()
    {
        Assert.Equal("a", "a".EnsureNotEmpty());
        Assert.Equal(" a ", " a ".EnsureNotEmpty());
    }

    [Fact]
    public void EnsureNotEmpty_RejectsNullEmptyAndBlank()
    {
        Assert.Throws<ArgumentException>(() => ((string?)null).EnsureNotEmpty());
        Assert.Throws<ArgumentException>(() => string.Empty.EnsureNotEmpty("title"));
        Assert.Throws<ArgumentException>(() => "  ".EnsureNotEmpty("title"));
    }
}
