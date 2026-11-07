using CSharpUtil.ObjectUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Test cases for every public helper in <see cref="CSharpUtil.ObjectUtil.ObjectUtil"/>.
/// The class works through extension methods, so the null cases verify that the compiler
/// binds to the static method rather than throwing a NullReferenceException on the receiver.
/// </summary>
public class ObjectUtilTests
{
    // ------------------------------------------------------------------
    // IsNull
    // ------------------------------------------------------------------

    [Fact]
    public void IsNull_ReturnsTrueForNullReference()
    {
        object? value = null;

        // Extension methods are static calls, so invoking one on a null receiver is legal.
        Assert.True(value.IsNull());
    }

    [Fact]
    public void IsNull_ReturnsFalseForInstance()
    {
        object value = new object();
        Assert.False(value.IsNull());
        Assert.False("text".IsNull());
        Assert.False(0.IsNull());            // boxed int is still a value, not null
    }

    // ------------------------------------------------------------------
    // IsNotNull
    // ------------------------------------------------------------------

    [Fact]
    public void IsNotNull_ReturnsFalseForNullReference()
    {
        object? value = null;
        Assert.False(value.IsNotNull());
    }

    [Fact]
    public void IsNotNull_ReturnsTrueForInstance()
    {
        object value = new object();
        Assert.True(value.IsNotNull());
        Assert.True("text".IsNotNull());
    }

    [Fact]
    public void IsNull_And_IsNotNull_AreMutuallyExclusive()
    {
        object? nullValue = null;
        object instance = new object();

        Assert.True(nullValue.IsNull());
        Assert.False(nullValue.IsNotNull());

        Assert.False(instance.IsNull());
        Assert.True(instance.IsNotNull());
    }

    // ------------------------------------------------------------------
    // CopyTo<T>
    // ------------------------------------------------------------------

    [Fact]
    public void CopyTo_CastsReferenceType()
    {
        object value = "abc";
        string text = value.CopyTo<string>();

        Assert.Equal("abc", text);
    }

    [Fact]
    public void CopyTo_CastsBoxedValueType()
    {
        object value = 42;
        int number = value.CopyTo<int>();

        Assert.Equal(42, number);
    }

    [Fact]
    public void CopyTo_NullSource_ToReferenceType_ReturnsNull()
    {
        object? value = null;

        // (string)null succeeds and yields null; no exception is thrown.
        Assert.Null(value.CopyTo<string>());
    }

    [Fact]
    public void CopyTo_NullSource_ToValueType_ThrowsNullReferenceException()
    {
        object? value = null;

        // Unboxing a null reference is a NullReferenceException in C#.
        Assert.Throws<NullReferenceException>(() => value.CopyTo<int>());
    }

    [Fact]
    public void CopyTo_IncompatibleType_ThrowsInvalidCastException()
    {
        object value = 42;

        Assert.Throws<InvalidCastException>(() => value.CopyTo<string>());
    }

    // ------------------------------------------------------------------
    // As<T>
    // ------------------------------------------------------------------

    [Fact]
    public void As_ReturnsSameInstanceWhenTypeMatches()
    {
        object value = "abc";
        string? text = value.As<string>();

        // 'as' performs the same cast but yields the original reference, not a copy.
        Assert.Same(value, text);
    }

    [Fact]
    public void As_ReturnsNullWhenTypeDoesNotMatch()
    {
        object value = 42;

        // Unlike CopyTo, an incompatible runtime type is not an error here.
        Assert.Null(value.As<string>());
    }

    [Fact]
    public void As_NullReceiver_ReturnsNull()
    {
        object? value = null;
        Assert.Null(value.As<string>());
    }

    // ------------------------------------------------------------------
    // IsDefault<T>
    // ------------------------------------------------------------------

    [Fact]
    public void IsDefault_RecognisesDefaultValueTypes()
    {
        Assert.True(0.IsDefault());
        Assert.True(default(int).IsDefault());
        Assert.True(default(bool).IsDefault());
        Assert.True(default(Guid).IsDefault());     // Guid.Empty
        Assert.False(42.IsDefault());
    }

    [Fact]
    public void IsDefault_RecognisesNullReferences()
    {
        string? text = null;
        Assert.True(text.IsDefault());
        Assert.False("abc".IsDefault());
        Assert.False(string.Empty.IsDefault());     // empty string is not the default for string (null is)
    }

    // ------------------------------------------------------------------
    // Coalesce<T>
    // ------------------------------------------------------------------

    [Fact]
    public void Coalesce_ReturnsValueWhenNotNull()
    {
        Assert.Equal("abc", "abc".Coalesce("fallback"));
    }

    [Fact]
    public void Coalesce_ReturnsFallbackWhenNull()
    {
        string? value = null;
        Assert.Equal("fallback", value.Coalesce("fallback"));
    }
}
