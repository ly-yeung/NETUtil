using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace CSharpUtil.IntegrationTests;

/// <summary>
/// Verifies what actually ships inside the .nupkg: per-TFM assemblies, XML documentation
/// and the dependency groups a consumer will resolve.
/// </summary>
public class PackageContentTests
{
    [Fact]
    public void Package_ContainsAssembliesAndDocsForBothTargetFrameworks()
    {
        using ZipArchive package = ZipFile.OpenRead(PackageUnderTest.Instance.PackagePath);
        string[] entries = package.Entries.Select(entry => entry.FullName).ToArray();

        Assert.Contains("lib/net8.0/CSharpUtil.dll", entries);
        Assert.Contains("lib/netstandard2.0/CSharpUtil.dll", entries);

        // <GenerateDocumentationFile>true</GenerateDocumentationFile> must reach the package,
        // otherwise consumers lose IntelliSense for every helper.
        Assert.Contains("lib/net8.0/CSharpUtil.xml", entries);
        Assert.Contains("lib/netstandard2.0/CSharpUtil.xml", entries);

        Assert.Contains(entries, name => name.EndsWith(".nuspec"));
    }

    [Fact]
    public void Package_DeclaresDependenciesPerTargetFramework()
    {
        using ZipArchive package = ZipFile.OpenRead(PackageUnderTest.Instance.PackagePath);
        ZipArchiveEntry nuspecEntry = package.Entries.Single(entry => entry.FullName.EndsWith(".nuspec"));

        using Stream stream = nuspecEntry.Open();
        XDocument nuspec = XDocument.Load(stream);

        var groups = nuspec.Descendants()
            .Where(element => element.Name.LocalName == "group" && element.Attribute("targetFramework") != null)
            .ToDictionary(
                group => group.Attribute("targetFramework")!.Value,
                group => group.Elements()
                    .Where(dependency => dependency.Name.LocalName == "dependency")
                    .Select(dependency => dependency.Attribute("id")!.Value)
                    .ToList());

        string net8Key = groups.Keys.Single(key => key.Replace(".", string.Empty).Contains("net8", System.StringComparison.OrdinalIgnoreCase));
        string netStandardKey = groups.Keys.Single(key => key.Contains("netstandard", System.StringComparison.OrdinalIgnoreCase));

        // net8.0 already ships System.Text.Json in the shared framework: a package dependency would be redundant.
        Assert.Contains("Newtonsoft.Json", groups[net8Key]);
        Assert.DoesNotContain("System.Text.Json", groups[net8Key]);

        // netstandard2.0 has no System.Text.Json of its own: without the dependency the JSON validation APIs
        // would not compile for consumers on that target.
        Assert.Contains("Newtonsoft.Json", groups[netStandardKey]);
        Assert.Contains("System.Text.Json", groups[netStandardKey]);
    }

    [Fact]
    public void Package_CarriesPublishMetadataForNuGetOrg()
    {
        using ZipArchive package = ZipFile.OpenRead(PackageUnderTest.Instance.PackagePath);
        string[] entries = package.Entries.Select(entry => entry.FullName).ToArray();

        // nuget.org renders the README and license on the package page, but only when the files
        // referenced by <PackageReadmeFile> / the license are actually packed.
        Assert.Contains("README.md", entries);
        Assert.Contains("LICENSE", entries);

        ZipArchiveEntry nuspecEntry = package.Entries.Single(entry => entry.FullName.EndsWith(".nuspec"));
        using Stream stream = nuspecEntry.Open();
        XDocument nuspec = XDocument.Load(stream);
        XNamespace ns = nuspec.Root!.Name.Namespace;
        XElement metadata = nuspec.Root!.Element(ns + "metadata")!;

        // The id and version here are what nuget.org records permanently, so they must match the
        // values the integration tests resolve the package from.
        Assert.Equal(PackageUnderTest.Instance.PackageId, metadata.Element(ns + "id")!.Value);
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
