using System.IO;
using Xunit;

namespace CSharpUtil.IntegrationTests;

/// <summary>
/// End-to-end consumption tests: a throw-away project restores the packed .nupkg, compiles against
/// the selected TFM asset and, for the runnable one, executes the helpers to prove the package works
/// outside its own build tree.
/// </summary>
public class ConsumptionTests
{
    [Fact]
    public void NetStandard20Consumer_CompilesAgainstNetStandardAsset()
    {
        string directory = PackageUnderTest.Instance.CreateConsumer(
            "ns20",
            "netstandard2.0",
            "Api.cs",
            """
            using CSharpUtil.NumberUtil;
            using CSharpUtil.StringUtil;

            namespace Consumer
            {
                public static class Api
                {
                    public static bool IsJsonObject(string value) => value.IsValidJsonObject();
                    public static bool LooksLikeJsonObject(string value) => value.IsLikeJsonObject();
                    public static int AsInt(string value) => value.ToInt(0);
                    public static double AsDouble(string value) => value.ToDouble(0);
                    public static double? AsNullableDouble(string value) => value.ToDouble((double?)null);
                    public static string MaskMobile(string value) => value.MaskMobile();
                    public static string AsRmb(decimal amount) => amount.ToChineseRmb();
                    public static string AsFileSize(long bytes) => bytes.FormatFileSize();
                }
            }
            """,
            executable: false);

        DotnetCli.Run(directory, "build", Path.Combine(directory, "Consumer.csproj"), "-c", "Release", "--nologo");

        // The build only proves assets resolved; also confirm the package (and not a project reference) was used.
        string assets = File.ReadAllText(Path.Combine(directory, "obj", "project.assets.json"));
        Assert.Contains("CSharpUtil", assets);
        Assert.Contains("netstandard2.0", assets);
    }

    [Fact]
    public void Net10Consumer_RunsAndReturnsExpectedValues()
    {
        string directory = PackageUnderTest.Instance.CreateConsumer(
            "net10",
            "net10.0",
            "Program.cs",
            """
            using CSharpUtil.NumberUtil;
            using CSharpUtil.StringUtil;

            // Emit UTF-8 explicitly: redirected console output otherwise depends on the machine code page.
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            string json = "{\"a\":1}";
            System.Console.WriteLine("RESULT:isValid=" + json.IsValidJsonObject());
            System.Console.WriteLine("RESULT:isLike=" + json.IsLikeJsonObject());
            System.Console.WriteLine("RESULT:toDouble=" + "1e400".ToDouble(0));
            System.Console.WriteLine("RESULT:toInt=" + "123".ToInt(0));
            System.Console.WriteLine("RESULT:isDouble=" + "1e400".IsDouble());
            System.Console.WriteLine("RESULT:mask=" + "13800138000".MaskMobile());
            System.Console.WriteLine("RESULT:rmb=" + 1234.56m.ToChineseRmb());
            System.Console.WriteLine("RESULT:size=" + 1536L.FormatFileSize());
            """,
            executable: true);

        string output = DotnetCli.Run(
            directory,
            "run",
            "--project", Path.Combine(directory, "Consumer.csproj"),
            "-c", "Release",
            "--nologo",
            "--verbosity", "quiet");

        // net10.0 consumer resolves the net8.0 asset, so this also proves forward compatibility of the package.
        Assert.Contains("RESULT:isValid=True", output);
        Assert.Contains("RESULT:isLike=True", output);
        Assert.Contains("RESULT:toDouble=0", output);
        Assert.Contains("RESULT:toInt=123", output);
        Assert.Contains("RESULT:isDouble=False", output);
        Assert.Contains("RESULT:mask=138****8000", output);
        Assert.Contains("RESULT:rmb=壹仟贰佰叁拾肆元伍角陆分", output);
        Assert.Contains("RESULT:size=1.50 KB", output);
    }
}
