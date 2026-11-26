using System;
using System.IO;
using System.Linq;
using Xunit;

namespace CSharpUtil.IntegrationTests;

/// <summary>
/// Packs the two shipped projects once per run and creates throw-away consumer projects that restore
/// the produced .nupkg files. This covers the gap unit tests cannot: real NuGet restore, per-TFM asset
/// selection and the dependency metadata a consumer actually sees.
/// </summary>
internal sealed class PackageUnderTest
{
    private static readonly Lazy<PackageUnderTest> InstanceFactory = new(() => new PackageUnderTest());

    public static PackageUnderTest Instance => InstanceFactory.Value;

    public string RepoRoot { get; }

    public string WorkRoot { get; }

    public string CorePackageId { get; } = "CSharpUtil.Extensions";

    public string JsonPackageId { get; } = "CSharpUtil.Extensions.Json";

    public string CorePackageDirectory { get; }

    public string JsonPackageDirectory { get; }

    public string CorePackagePath { get; }

    public string JsonPackagePath { get; }

    public string Version { get; }

    private PackageUnderTest()
    {
        RepoRoot = LocateRepoRoot();
        WorkRoot = Path.Combine(Path.GetTempPath(), "netutil-it-" + Guid.NewGuid().ToString("N"));
        CorePackageDirectory = Path.Combine(WorkRoot, "pkg", "core");
        JsonPackageDirectory = Path.Combine(WorkRoot, "pkg", "json");
        Directory.CreateDirectory(CorePackageDirectory);
        Directory.CreateDirectory(JsonPackageDirectory);

        Pack(Path.Combine(RepoRoot, "CSharpUtil", "CSharpUtil.csproj"), CorePackageDirectory);
        Pack(Path.Combine(RepoRoot, "CSharpUtil.Json", "CSharpUtil.Json.csproj"), JsonPackageDirectory);

        CorePackagePath = SinglePackage(CorePackageDirectory);
        JsonPackagePath = SinglePackage(JsonPackageDirectory);

        Version = Path.GetFileNameWithoutExtension(CorePackagePath).Substring(CorePackageId.Length + 1);
        Assert.Equal(Version, Path.GetFileNameWithoutExtension(JsonPackagePath).Substring(JsonPackageId.Length + 1));

        // NuGet resolves a package id/version from the global packages folder before looking at any source,
        // so re-packing the same version leaves consumers with the previously cached copy. Evict both so the
        // consumers below always restore the bits produced by this run.
        EvictCachedPackage(CorePackageId, Version);
        EvictCachedPackage(JsonPackageId, Version);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => TryDelete(WorkRoot);
    }

    /// <summary>
    /// Writes a consumer project that restores the packed packages from local sources.
    /// </summary>
    public string CreateConsumer(
        string name,
        string targetFramework,
        string sourceFileName,
        string sourceCode,
        bool executable,
        bool includeJsonPackage)
    {
        string directory = Path.Combine(WorkRoot, "consumers", name);
        Directory.CreateDirectory(directory);

        string outputType = executable ? "<OutputType>Exe</OutputType>" : string.Empty;
        string jsonReference = includeJsonPackage
            ? $"""<PackageReference Include="{JsonPackageId}" Version="{Version}" />"""
            : string.Empty;

        File.WriteAllText(
            Path.Combine(directory, "Consumer.csproj"),
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
                <Nullable>enable</Nullable>
                <LangVersion>latest</LangVersion>
                <!-- Restore is offline for the dependencies (already cached); auditing would add a network
                     round-trip to nuget.org and make these tests hang when the network is slow. -->
                <NuGetAudit>false</NuGetAudit>
                {outputType}
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{CorePackageId}" Version="{Version}" />
                {jsonReference}
              </ItemGroup>
            </Project>
            """);

        // Local folders first, then whatever sources the machine already has (nuget.org for the transitive deps).
        File.WriteAllText(
            Path.Combine(directory, "NuGet.config"),
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="local-it-core" value="{CorePackageDirectory}" />
                <add key="local-it-json" value="{JsonPackageDirectory}" />
              </packageSources>
            </configuration>
            """);

        File.WriteAllText(Path.Combine(directory, sourceFileName), sourceCode);
        return directory;
    }

    private void Pack(string project, string outputDirectory)
    {
        DotnetCli.Run(RepoRoot, "pack", project, "-c", "Release", "-o", outputDirectory, "--nologo");
    }

    private static string SinglePackage(string directory)
    {
        string[] packages = Directory.GetFiles(directory, "*.nupkg");
        Assert.Single(packages);
        return packages[0];
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CSharpUtil", "CSharpUtil.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root (expected a CSharpUtil/CSharpUtil.csproj above '{AppContext.BaseDirectory}').");
    }

    /// <summary>
    /// Deletes the global-packages-folder copy of this package id/version so a restore cannot silently
    /// reuse an older build that happens to carry the same version number.
    /// </summary>
    private static void EvictCachedPackage(string packageId, string version)
    {
        string? packagesRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(packagesRoot))
        {
            packagesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        }

        string cached = Path.Combine(
            packagesRoot, packageId.ToLowerInvariant(), version.ToLowerInvariant());

        TryDelete(cached);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Cleanup is best effort: a locked temp folder must never fail a test run.
        }
    }
}
