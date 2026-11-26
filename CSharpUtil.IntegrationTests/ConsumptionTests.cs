using System.IO;
using Xunit;

namespace CSharpUtil.IntegrationTests;

/// <summary>
/// End-to-end consumption tests: throw-away projects restore the packed .nupkg files, compile against
/// the selected TFM asset and, for the runnable ones, execute the helpers to prove the packages work
/// outside their own build tree.
/// </summary>
public class ConsumptionTests
{
    [Fact]
    public void CoreOnlyConsumer_CompilesOnNetStandard20AndPullsNoThirdPartyPackages()
    {
        string directory = PackageUnderTest.Instance.CreateConsumer(
            "ns20-core",
            "netstandard2.0",
            "Api.cs",
            """
            using CSharpUtil.FileUtil;
            using CSharpUtil.NumberUtil;
            using CSharpUtil.ObjectUtil;
            using CSharpUtil.StringUtil;

            namespace Consumer
            {
                public static class Api
                {
                    public static int AsInt(string value) => value.ToInt(0);
                    public static double? AsNullableDouble(string value) => value.ToDouble((double?)null);
                    public static string MaskMobile(string value) => value.MaskMobile();
                    public static string Normalized(string value) => value.NormalizeSpace();
                    public static decimal Clamped(decimal value) => value.Clamp(0m, 10m);
                    public static string Percent(decimal value) => value.ToPercent(1);
                    public static string AsRmb(decimal amount) => amount.ToChineseRmb();
                    public static string AsFileSize(long bytes) => bytes.FormatFileSize();

                    public static string Extension(string value) => value.GetExtension();
                    public static string CleanName(string value) => value.GetSafeFileName();
                    public static string MediaType(string value) => value.GetContentType();
                    public static FileKind KindOf(string value) => value.GetFileKind();
                    public static FileTypeInfo InfoOf(string value) => value.GetFileInfo();

                    public static T Cast<T>(object value) => value.CopyTo<T>();
                }
            }
            """,
            executable: false,
            includeJsonPackage: false);

        DotnetCli.Run(directory, "build", Path.Combine(directory, "Consumer.csproj"), "-c", "Release", "--nologo");

        // The build only proves assets resolved; also confirm the package (and not a project reference) was used.
        string assets = File.ReadAllText(Path.Combine(directory, "obj", "project.assets.json"));
        Assert.Contains("CSharpUtil.Extensions", assets);
        Assert.Contains("netstandard2.0", assets);

        // The whole point of the split: a consumer of the core package must not drag a JSON serializer in.
        Assert.DoesNotContain("newtonsoft.json", assets, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("system.text.json", assets, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonConsumer_CompilesOnNetStandard20()
    {
        string directory = PackageUnderTest.Instance.CreateConsumer(
            "ns20-json",
            "netstandard2.0",
            "Api.cs",
            """
            using CSharpUtil.StringUtil;

            namespace Consumer
            {
                public sealed class Payload
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = string.Empty;
                }

                public static class Api
                {
                    public static bool IsJsonObject(string value) => value.IsValidJsonObject();
                    public static bool LooksLikeJsonObject(string value) => value.IsLikeJsonObject();
                    public static Payload Deserialize(string value) => value.ConvertTo<Payload>();
                    public static bool TryDeserialize(string value, out Payload result) => value.TryConvertTo(out result);
                }
            }
            """,
            executable: false,
            includeJsonPackage: true);

        DotnetCli.Run(directory, "build", Path.Combine(directory, "Consumer.csproj"), "-c", "Release", "--nologo");

        string assets = File.ReadAllText(Path.Combine(directory, "obj", "project.assets.json"));
        Assert.Contains("CSharpUtil.Extensions.Json", assets);
        Assert.Contains("newtonsoft.json", assets, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Net10Consumer_RunsAndReturnsExpectedValues()
    {
        string directory = PackageUnderTest.Instance.CreateConsumer(
            "net10",
            "net10.0",
            "Program.cs",
            """
            using CSharpUtil.DateTimeUtil;
            using CSharpUtil.FileUtil;
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
            System.Console.WriteLine("RESULT:capSize=" + 1073741824L.FormatFileSize(2, SizeUnit.KB));
            System.Console.WriteLine("RESULT:ext=" + "C:\\docs\\Report.PDF".GetExtension());
            System.Console.WriteLine("RESULT:kind=" + "a.PNG".GetFileKind());
            System.Console.WriteLine("RESULT:ctype=" + "a.png".GetContentType());
            System.Console.WriteLine("RESULT:safe=" + "CON.txt".IsSafeFileName());
            FileTypeInfo info = "a.png".GetFileInfo();
            System.Console.WriteLine("RESULT:info=" + info.Kind + "/" + info.Category + "/" + info.ContentType);
            System.Console.WriteLine("RESULT:normalized=" + "  a  b ".NormalizeSpace());
            System.Console.WriteLine("RESULT:clamped=" + 42m.Clamp(0m, 10m));
            System.Console.WriteLine("RESULT:percent=" + 0.125m.ToPercent(1));
            System.Console.WriteLine("RESULT:unix=" + new System.DateTime(2024, 3, 5, 6, 7, 8, System.DateTimeKind.Utc).ToUnixSeconds());
            """,
            executable: true,
            includeJsonPackage: true);

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
        // Exercises the public SizeUnit enum through the packaged assembly, not just via a project reference.
        Assert.Contains("RESULT:capSize=1048576.00 KB", output);
        Assert.Contains("RESULT:ext=.pdf", output);
        Assert.Contains("RESULT:kind=Png", output);
        Assert.Contains("RESULT:ctype=image/png", output);
        Assert.Contains("RESULT:safe=False", output);
        Assert.Contains("RESULT:info=Png/Image/image/png", output);
        Assert.Contains("RESULT:normalized=a b", output);
        Assert.Contains("RESULT:clamped=10", output);
        Assert.Contains("RESULT:percent=12.5%", output);
        Assert.Contains("RESULT:unix=1709618828", output);
    }
}
