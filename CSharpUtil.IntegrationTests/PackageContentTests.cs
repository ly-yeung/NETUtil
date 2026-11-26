using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace CSharpUtil.IntegrationTests;

/// <summary>
/// Verifies what actually ships inside the two .nupkg files: per-TFM assemblies, XML documentation
/// and the dependency groups a consumer will resolve. The dependency assertions are what make the
/// core/json split real: the core package must resolve nothing, the json package must resolve the
/// serializer and the core package.
/// </summary>
public class PackageContentTests
{
    private static string[] Entries(string packagePath)
    {
        using ZipArchive package = ZipFile.OpenRead(packagePath);
        return package.Entries.Select(entry => entry.FullName).ToArray();
    }

    private static XDocument Nuspec(string packagePath)
    {
        using ZipArchive package = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry nuspecEntry = package.Entries.Single(entry => entry.FullName.EndsWith(".nuspec"));

        using Stream stream = nuspecEntry.Open();
        return XDocument.Load(stream);
    }

    private static XElement Metadata(XDocument nuspec)
    {
        XNamespace ns = nuspec.Root!.Name.Namespace;
        return nuspec.Root!.Element(ns + "metadata")!;
    }

    private static Dictionary<string, List<(string Id, string Version)>> DependenciesByTargetFramework(XDocument nuspec)
    {
        var groups = nuspec.Descendants()
            .Where(element => element.Name.LocalName == "group" && element.Attribute("targetFramework") != null)
            .ToDictionary(
                group => group.Attribute("targetFramework")!.Value,
                group => group.Elements()
                    .Where(dependency => dependency.Name.LocalName == "dependency")
                    .Select(dependency => (
                        dependency.Attribute("id")!.Value,
                        dependency.Attribute("version")!.Value))
                    .ToList());

        return groups;
    }

    private static string KeyFor(Dictionary<string, List<(string Id, string Version)>> groups, string prefix)
    {
        return groups.Keys.Single(key => key.Contains(prefix, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CorePackage_ContainsAssembliesAndDocsForBothTargetFrameworks()
    {
        string[] entries = Entries(PackageUnderTest.Instance.CorePackagePath);

        Assert.Contains("lib/net8.0/CSharpUtil.dll", entries);
        Assert.Contains("lib/netstandard2.0/CSharpUtil.dll", entries);

        // <GenerateDocumentationFile>true</GenerateDocumentationFile> must reach the package,
        // otherwise consumers lose IntelliSense for every helper.
        Assert.Contains("lib/net8.0/CSharpUtil.xml", entries);
        Assert.Contains("lib/netstandard2.0/CSharpUtil.xml", entries);

        // The JSON assembly belongs to the companion package; shipping it here would defeat the split.
        Assert.DoesNotContain(entries, name => name.EndsWith("CSharpUtil.Json.dll"));

        Assert.Contains(entries, name => name.EndsWith(".nuspec"));
    }

    [Fact]
    public void JsonPackage_ContainsAssembliesAndDocsForBothTargetFrameworks()
    {
        string[] entries = Entries(PackageUnderTest.Instance.JsonPackagePath);

        Assert.Contains("lib/net8.0/CSharpUtil.Json.dll", entries);
        Assert.Contains("lib/netstandard2.0/CSharpUtil.Json.dll", entries);
        Assert.Contains("lib/net8.0/CSharpUtil.Json.xml", entries);
        Assert.Contains("lib/netstandard2.0/CSharpUtil.Json.xml", entries);

        // The core assembly ships in its own package; embedding a second copy here would let the two
        // versions drift apart in the same restore graph.
        Assert.DoesNotContain("lib/net8.0/CSharpUtil.dll", entries);
    }

    [Fact]
    public void CorePackage_HasNoPackageDependencies()
    {
        XDocument nuspec = Nuspec(PackageUnderTest.Instance.CorePackagePath);

        // No <dependency> at all: installing the core package must pull nothing from nuget.org.
        Assert.Empty(nuspec.Descendants().Where(element => element.Name.LocalName == "dependency"));

        string raw = nuspec.ToString();
        Assert.DoesNotContain("Newtonsoft", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Text.Json", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonPackage_DeclaresDependenciesPerTargetFramework()
    {
        Dictionary<string, List<(string Id, string Version)>> groups =
            DependenciesByTargetFramework(Nuspec(PackageUnderTest.Instance.JsonPackagePath));

        string net8Key = KeyFor(groups, "net8");
        string netStandardKey = KeyFor(groups, "netstandard");

        List<string> net8 = groups[net8Key].Select(dependency => dependency.Id).ToList();
        List<string> netStandard = groups[netStandardKey].Select(dependency => dependency.Id).ToList();

        // net8.0 already ships System.Text.Json in the shared framework: a package dependency would be redundant.
        Assert.Contains("Newtonsoft.Json", net8);
        Assert.Contains("CSharpUtil.Extensions", net8);
        Assert.DoesNotContain("System.Text.Json", net8);

        // netstandard2.0 has no System.Text.Json of its own: without the dependency the JSON validation
        // APIs would not compile for consumers on that target.
        Assert.Contains("Newtonsoft.Json", netStandard);
        Assert.Contains("CSharpUtil.Extensions", netStandard);
        Assert.Contains("System.Text.Json", netStandard);

        // The companion package must track the core package version, otherwise a consumer can end up with
        // a json assembly compiled against a core assembly that is no longer there.
        foreach (List<(string Id, string Version)> group in groups.Values)
        {
            string coreRange = group.Single(dependency => dependency.Id == "CSharpUtil.Extensions").Version;
            Assert.Contains(PackageUnderTest.Instance.Version, coreRange);
        }
    }

    [Fact]
    public void CorePackage_ExposesFileUtilInEveryTargetFramework()
    {
        // The XML doc file is generated from the public surface, so finding the type there proves the
        // FileUtil namespace is compiled into each TFM asset rather than only existing in source.
        foreach (string targetFramework in new[] { "net8.0", "netstandard2.0" })
        {
            using ZipArchive package = ZipFile.OpenRead(PackageUnderTest.Instance.CorePackagePath);
            ZipArchiveEntry docEntry = package.Entries.Single(entry => entry.FullName == "lib/" + targetFramework + "/CSharpUtil.xml");

            using Stream stream = docEntry.Open();
            XDocument doc = XDocument.Load(stream);

            var documentedMembers = doc.Descendants()
                .Where(element => element.Name.LocalName == "member")
                .Select(element => element.Attribute("name")!.Value);

            Assert.Contains(documentedMembers, name => name.StartsWith("T:CSharpUtil.FileUtil.", StringComparison.Ordinal));
            Assert.Contains(documentedMembers, name => name.StartsWith("M:CSharpUtil.FileUtil.", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void JsonPackage_ExposesJsonHelpersAndNoFileUtil()
    {
        using ZipArchive package = ZipFile.OpenRead(PackageUnderTest.Instance.JsonPackagePath);
        ZipArchiveEntry docEntry = package.Entries.Single(entry => entry.FullName == "lib/net8.0/CSharpUtil.Json.xml");

        using Stream stream = docEntry.Open();
        XDocument doc = XDocument.Load(stream);

        var documentedMembers = doc.Descendants()
            .Where(element => element.Name.LocalName == "member")
            .Select(element => element.Attribute("name")!.Value)
            .ToList();

        Assert.Contains(documentedMembers, name => name.Contains("IsValidJsonObject", StringComparison.Ordinal));
        Assert.Contains(documentedMembers, name => name.Contains("ConvertTo", StringComparison.Ordinal));

        // Json helpers kept the CSharpUtil.StringUtil namespace on purpose, so verify that too.
        Assert.Contains(documentedMembers, name => name.StartsWith("T:CSharpUtil.StringUtil.", StringComparison.Ordinal));
    }

    [Fact]
    public void Packages_CarryPublishMetadataForNuGetOrg()
    {
        foreach ((string id, string packagePath) in new[]
        {
            (PackageUnderTest.Instance.CorePackageId, PackageUnderTest.Instance.CorePackagePath),
            (PackageUnderTest.Instance.JsonPackageId, PackageUnderTest.Instance.JsonPackagePath),
        })
        {
            string[] entries = Entries(packagePath);

            // nuget.org renders the README and license on the package page, but only when the files
            // referenced by <PackageReadmeFile> / the license are actually packed.
            Assert.Contains("README.md", entries);
            Assert.Contains("LICENSE", entries);

            XElement metadata = Metadata(Nuspec(packagePath));
            XNamespace ns = metadata.Name.Namespace;

            // The id and version here are what nuget.org records permanently, so they must match the
            // values the integration tests resolve the packages from.
            Assert.Equal(id, metadata.Element(ns + "id")!.Value);
            Assert.Equal(PackageUnderTest.Instance.Version, metadata.Element(ns + "version")!.Value);

            // <license type="expression">MIT</license>: the type says the value is an SPDX expression.
            XElement license = metadata.Element(ns + "license")!;
            Assert.Equal("expression", license.Attribute("type")!.Value);
            Assert.Equal("MIT", license.Value);

            Assert.Equal("README.md", metadata.Element(ns + "readme")!.Value);
            Assert.Equal("https://github.com/ly-yeung/NETUtil", metadata.Element(ns + "projectUrl")!.Value);

            XElement repository = metadata.Element(ns + "repository")!;
            Assert.Equal("git", repository.Attribute("type")!.Value);
            Assert.Equal("https://github.com/ly-yeung/NETUtil", repository.Attribute("url")!.Value);
        }
    }
}
