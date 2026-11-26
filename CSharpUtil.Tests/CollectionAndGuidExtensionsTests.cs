using System;
using System.Collections.Generic;
using System.Linq;
using CSharpUtil.CollectionUtil;
using CSharpUtil.GuidUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Collection null-safety and the compact GUID encoding added in 0.3.0.
/// </summary>
public class CollectionAndGuidExtensionsTests
{
    // ------------------------------------------------------------------
    // OrEmpty / IsNullOrEmpty
    // ------------------------------------------------------------------

    [Fact]
    public void OrEmpty_ReturnsTheSameInstanceWhenNotNull()
    {
        List<int> list = new() { 1, 2 };
        // No wrapper, no copy: LINQ chaining afterwards must see the original object.
        Assert.Same(list, list.OrEmpty());
    }

    [Fact]
    public void OrEmpty_ReturnsASharedEmptySequenceWhenNull()
    {
        IEnumerable<int>? missing = null;
        Assert.Empty(missing.OrEmpty());
        // Enumerable.Empty is a singleton, so two calls must not allocate two different sequences.
        Assert.Same(missing.OrEmpty(), missing.OrEmpty());
    }

    [Fact]
    public void OrEmpty_MakesNullChainingSafe()
    {
        IEnumerable<int>? missing = null;
        Assert.Empty(missing.OrEmpty());
        Assert.Equal(6, ((IEnumerable<int>?)new[] { 1, 2, 3 }).OrEmpty().Sum());
    }

    [Fact]
    public void IsNullOrEmpty_WorksForArraysListsAndNull()
    {
        Assert.True(((int[]?)null).IsNullOrEmpty());
        Assert.True(Array.Empty<int>().IsNullOrEmpty());
        Assert.False(new[] { 1 }.IsNullOrEmpty());
        Assert.True(((List<string>?)null).IsNullOrEmpty());
        Assert.False(new List<string> { "" }.IsNullOrEmpty());
    }

    // ------------------------------------------------------------------
    // Guid short string
    // ------------------------------------------------------------------

    [Fact]
    public void ToShortString_Produces22UrlSafeCharacters()
    {
        string shortText = Guid.NewGuid().ToShortString();

        Assert.Equal(22, shortText.Length);
        Assert.DoesNotContain("+", shortText);
        Assert.DoesNotContain("/", shortText);
        Assert.DoesNotContain("=", shortText);
    }

    [Fact]
    public void ShortString_RoundTrips()
    {
        for (int index = 0; index < 200; index++)
        {
            Guid original = Guid.NewGuid();
            Assert.Equal(original, original.ToShortString().ParseShortString());
        }

        Assert.Equal(Guid.Empty, Guid.Empty.ToShortString().ParseShortString());
    }

    [Fact]
    public void ParseShortString_ReturnsNullForAnythingUnexpected()
    {
        Assert.Null(((string?)null).ParseShortString());
        Assert.Null(string.Empty.ParseShortString());
        Assert.Null("too-short".ParseShortString());
        Assert.Null(new string('=', 22).ParseShortString());
        Assert.Null("!!!!!!!!!!!!!!!!!!!!!!".ParseShortString());
    }
}
