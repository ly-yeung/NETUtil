using System.Text;
using CSharpUtil.StringUtil;

namespace CSharpUtil.Tests;

public class JsonDetectionTests
{
    // ---------------- 形状判定：IsLikeJsonObject / IsLikeJsonArray ----------------

    [Theory]
    [InlineData("{\"a\":1}", true)]
    [InlineData(" {\"a\":1}", true)]
    [InlineData("\r\n\t{\"a\":1}", true)]
    [InlineData("\uFEFF{\"a\":1}", true)]              // BOM 开头也认
    [InlineData("{\"a\":1} extra", true)]             // 形状判定不看后半段
    [InlineData("{bad json", true)]                   // 已知局限：残缺文本也判 true
    [InlineData("[1,2,3]", false)]
    [InlineData("[\"a\"]", false)]
    [InlineData("null", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsLikeJsonObject_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsLikeJsonObject());
    }

    [Theory]
    [InlineData("[1,2,3]", true)]
    [InlineData("  [ ]", true)]
    [InlineData("{\"a\":1}", false)]
    [InlineData("[1,2,3] tail", true)]                 // 同上的形状语义
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLikeJsonArray_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsLikeJsonArray());
    }

    [Fact]
    public void IsLikeJson_IsStableAcrossManyCalls()
    {
        // 形状判定必须与调用次数无关（无状态、零分配）
        for (int i = 0; i < 1000; i++)
        {
            Assert.True("{\"a\":1}".IsLikeJsonObject());
            Assert.False("[1,2,3]".IsLikeJsonObject());
        }
    }

    // ---------------- 合法性校验：IsValidJsonObject / IsValidJsonArray ----------------

    [Theory]
    [InlineData("{\"a\":1}", true)]
    [InlineData("{ \"a\" : { \"b\" : [1, 2, 3] } }", true)]
    [InlineData("   {\"a\":\"含中文与转义\\\"引号\"}   ", true)]
    [InlineData("\uFEFF{\"a\":1}", true)]              // BOM 被容忍
    [InlineData("{'a':1}", false)]                     // 单引号：Newtonsoft 接受，STJ 拒绝
    [InlineData("{a:1}", false)]                       // 无引号 key
    [InlineData("{\"a\":1,}", false)]                  // 尾逗号
    [InlineData("{\"a\":NaN}", false)]                 // NaN
    [InlineData("{\"a\":1} extra", false)]             // 顶层尾随内容
    [InlineData("{bad json", false)]
    [InlineData("[1,2,3]", false)]                     // 是合法 JSON，但不是对象
    [InlineData("null", false)]
    [InlineData("\"text\"", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsValidJsonObject_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsValidJsonObject());
    }

    [Theory]
    [InlineData("[1,2,3]", true)]
    [InlineData("[ {\"a\":1}, [true, false, null] ]", true)]
    [InlineData("[1,2,]", false)]
    [InlineData("{\"a\":1}", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidJsonArray_Cases(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsValidJsonArray());
    }

    [Fact]
    public void IsValidJsonObject_ToleratesWhitespaceOnlyAroundDocument()
    {
        Assert.True(" \t\r\n {\"a\":1} \r\n\t ".IsValidJsonObject());
    }

    [Fact]
    public void IsValidJsonObject_EnforcesDepthLimit()
    {
        // 64 层以内通过
        string ok = "{\"v\":" + new string('[', 60) + new string(']', 60) + "}";
        Assert.True(ok.IsValidJsonObject());

        // 超过 64 层被拒绝（防止嵌套炸弹）
        string tooDeep = "{\"v\":" + new string('[', 80) + new string(']', 80) + "}";
        Assert.False(tooDeep.IsValidJsonObject());
    }

    [Fact]
    public void IsValidJson_RejectsUnbalancedBraces()
    {
        Assert.False("{\"a\":1".IsValidJsonObject());
        Assert.False("{\"a\":1}}".IsValidJsonObject());
    }

    // ---------------- UTF-8 字节重载（零分配路径） --------------

    [Fact]
    public void IsValidJsonObject_Utf8Bytes()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("{\"名\":\"值\",\"n\":1}");
        Assert.True(utf8.AsSpan().IsValidJsonObject());
        Assert.False(utf8.AsSpan().IsValidJsonArray());
    }

    [Fact]
    public void IsValidJsonObject_Utf8Bytes_RejectsInvalid()
    {
        byte[] singleQuote = Encoding.UTF8.GetBytes("{'a':1}");
        Assert.False(singleQuote.AsSpan().IsValidJsonObject());

        byte[] trailing = Encoding.UTF8.GetBytes("{\"a\":1} garbage");
        Assert.False(trailing.AsSpan().IsValidJsonObject());
    }

    [Fact]
    public void IsValidJsonObject_Utf8Bytes_EmptyOrWhitespace()
    {
        Assert.False(ReadOnlySpan<byte>.Empty.IsValidJsonObject());
        byte[] whitespace = Encoding.UTF8.GetBytes("   ");
        Assert.False(whitespace.AsSpan().IsValidJsonObject());
    }

    // ---------------- 两层语义的关系 --------------

    [Theory]
    [InlineData("{\"a\":1}")]
    [InlineData(" { \"a\" : [1] } ")]
    [InlineData("\uFEFF{\"a\":1}")]
    public void ValidObjectAlwaysLooksLikeObject(string input)
    {
        Assert.True(input.IsValidJsonObject());
        Assert.True(input.IsLikeJsonObject());
    }

    [Fact]
    public void LikeDoesNotImplyValid()
    {
        // 形状判定通过但校验失败，这是设计上的预期差异
        Assert.True("{bad json".IsLikeJsonObject());
        Assert.False("{bad json".IsValidJsonObject());
    }
}
