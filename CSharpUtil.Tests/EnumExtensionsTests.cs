using System.ComponentModel;
using CSharpUtil.EnumUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Cached [Description] lookup added in 0.3.0.
/// </summary>
public class EnumExtensionsTests
{
    private enum Status
    {
        [Description("待处理")]
        Pending,

        [Description("已完成")]
        Done,

        Plain
    }

    [Flags]
    private enum Access
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    [Fact]
    public void GetDescription_ReturnsAttributeText()
    {
        Assert.Equal("待处理", Status.Pending.GetDescription());
        Assert.Equal("已完成", Status.Done.GetDescription());
    }

    [Fact]
    public void GetDescription_FallsBackToMemberName()
    {
        // No attribute on the member, so the name itself is the display text.
        Assert.Equal("Plain", Status.Plain.GetDescription());
    }

    [Fact]
    public void GetDescription_FallsBackToValueForUndefinedCombinations()
    {
        // A flags combination has no member and therefore no attribute to read.
        Assert.Equal("Read, Write", (Access.Read | Access.Write).GetDescription());
        Assert.Equal("None", Access.None.GetDescription());
    }

    [Fact]
    public void GetDescription_ReturnsTheSameStringOnRepeatedCalls()
    {
        // The cache must hand back the identical instance instead of re-reading the attribute every time.
        string first = Status.Pending.GetDescription();
        string second = Status.Pending.GetDescription();
        Assert.Same(first, second);
    }
}
