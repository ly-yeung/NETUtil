using System;
using System.IO;
using CSharpUtil.FileUtil;
using Xunit;

namespace CSharpUtil.Tests;

/// <summary>
/// Test cases for the path, file name, kind and content type helpers.
/// Inputs marked "hostile" are the strings a client can actually send; the assertions pin the
/// behaviour that keeps them from escaping a directory or overwriting a neighbouring file.
/// </summary>
public class FileUtilTests
{
    // ------------------------------------------------------------------
    // FilePathUtil.GetFileName
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(@"C:\docs\report.pdf", "report.pdf")]
    [InlineData("C:/docs/report.pdf", "report.pdf")]
    [InlineData(@"..\..\etc\passwd", "passwd")]
    [InlineData("/etc/passwd", "passwd")]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData(@"C:\docs\", "")]
    [InlineData("", "")]
    public void GetFileName_ReturnsLastSegment(string path, string expected)
    {
        Assert.Equal(expected, path.GetFileName());
    }

    [Fact]
    public void GetFileName_ReturnsEmptyForNull()
    {
        string? path = null;
        Assert.Equal(string.Empty, path.GetFileName());
    }

    // ------------------------------------------------------------------
    // FilePathUtil.GetFileNameWithoutExtension
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(@"C:\docs\report.PDF", "report")]
    [InlineData(@".gitignore", ".gitignore")]        // Path.GetFileNameWithoutExtension returns "" here
    [InlineData("notes.txt.", "notes.txt.")]         // a trailing dot is not an extension
    [InlineData("archive.tar.gz", "archive.tar")]
    [InlineData("noext", "noext")]
    [InlineData("a..b", "a.")]
    public void GetFileNameWithoutExtension_HandlesDotFilesAndTrailingDots(string path, string expected)
    {
        Assert.Equal(expected, path.GetFileNameWithoutExtension());
    }

    // ------------------------------------------------------------------
    // FilePathUtil.GetExtension and GetExtensionName
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(@"C:\docs\Report.PDF", ".pdf")]
    [InlineData("photo.JPG", ".jpg")]
    [InlineData(@"folder.with.dot\x.txt", ".txt")]
    [InlineData(@"a.b\c", "")]
    [InlineData(@"/srv/data.dir/", "")]
    [InlineData(".gitignore", "")]
    [InlineData("notes.txt.", "")]
    [InlineData("noext", "")]
    [InlineData("", "")]
    public void GetExtension_ReturnsLowercaseExtensionWithDot(string path, string expected)
    {
        Assert.Equal(expected, path.GetExtension());
    }

    [Theory]
    [InlineData(@"C:\docs\Report.PDF", "pdf")]
    [InlineData(@"/srv/data/.hidden", "")]
    [InlineData(".gitignore", "")]
    public void GetExtensionName_ReturnsExtensionWithoutDot(string path, string expected)
    {
        Assert.Equal(expected, path.GetExtensionName());
    }

    [Theory]
    [InlineData("archive.tar.gz", 2, ".tar.gz")]
    [InlineData("archive.tar.gz", 1, ".gz")]
    [InlineData("a.pdf", 2, ".pdf")]
    [InlineData(".gitignore", 2, "")]
    [InlineData("noext", 2, "")]
    // Out-of-range maxSegments is clamped instead of rejected: below 1 behaves like 1.
    [InlineData("archive.tar.gz", 0, ".gz")]
    [InlineData("a.b.c.d", 4, ".b.c.d")]
    [InlineData("a.b.c.d", 99, ".b.c.d")]
    public void GetCompoundExtension_CombinesTrailingSegments(string path, int maxSegments, string expected)
    {
        Assert.Equal(expected, path.GetCompoundExtension(maxSegments));
    }

    // ------------------------------------------------------------------
    // FilePathUtil.IsExtensionOf
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("a.PDF", "pdf")]
    [InlineData("a.PDF", ".PDF")]
    [InlineData("a.jpeg", "jpg", "jpeg")]
    public void IsExtensionOf_IgnoresCaseAndLeadingDot(string path, params string[] extensions)
    {
        Assert.True(path.IsExtensionOf(extensions));
    }

    [Theory]
    [InlineData("a.txt", "pdf")]
    [InlineData("noext", "txt")]
    [InlineData(".gitignore", "gitignore")]
    public void IsExtensionOf_ReturnsFalseWhenExtensionDiffers(string path, string extension)
    {
        Assert.False(path.IsExtensionOf(extension));
    }

    // ------------------------------------------------------------------
    // FileNameUtil.IsSafeFileName
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("notes.txt")]
    [InlineData(".gitignore")]
    [InlineData("报告(终版).pdf")]
    [InlineData("a b.txt")]
    public void IsSafeFileName_AcceptsOrdinaryNames(string name)
    {
        Assert.True(name.IsSafeFileName());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"..\..\etc\passwd")]
    [InlineData(@"C:\docs\report.pdf")]
    [InlineData("/etc/passwd")]
    [InlineData("..")]
    [InlineData("a:b.txt")]
    [InlineData("a*b.txt")]
    [InlineData("CON")]
    [InlineData("CON.txt")]
    [InlineData("con.pdf")]
    [InlineData("LPT9.log")]
    [InlineData("notes.txt.")]
    [InlineData("notes.txt ")]
    public void IsSafeFileName_RejectsHostileNames(string? name)
    {
        Assert.False(name.IsSafeFileName());
    }

    [Fact]
    public void IsSafeFileName_RejectsControlAndFormattingCharacters()
    {
        Assert.False("a\u0000b.txt".IsSafeFileName());
        Assert.False("a\tb.txt".IsSafeFileName());
        // Right-to-left override: displays as "invoice exp.txt" while the real name ends with .exe.
        Assert.False("invoice\u202Etxt.exe".IsSafeFileName());
        Assert.False("a\u200Bb.txt".IsSafeFileName());
    }

    [Fact]
    public void IsSafeFileName_RejectsNamesLongerThanLimit()
    {
        Assert.False((new string('a', 256) + ".txt").IsSafeFileName());
    }

    [Fact]
    public void IsSafeFileName_ExplainsTheFailedRule()
    {
        string? reason = null;
        Assert.False(@"docs\report.pdf".IsSafeFileName(out reason));
        Assert.NotNull(reason);
        Assert.Contains("separator", reason);

        // An absolute path fails on the colon first, which is reported as the offending character.
        Assert.False(@"C:\docs\report.pdf".IsSafeFileName(out reason));
        Assert.Contains("':'", reason);

        Assert.True("notes.txt".IsSafeFileName(out reason));
        Assert.Null(reason);
    }

    // ------------------------------------------------------------------
    // FileNameUtil.GetSafeFileName
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(@"..\..\etc\passwd", "passwd")]
    [InlineData(@"C:\docs\report.pdf", "report.pdf")]
    [InlineData("a|b.txt", "a_b.txt")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("notes.txt.", "notes.txt")]
    [InlineData("  spaced.txt  ", "spaced.txt")]
    [InlineData("invoice\u202Etxt.exe", "invoicetxt.exe")]
    public void GetSafeFileName_CleansHostileNames(string input, string expected)
    {
        Assert.Equal(expected, input.GetSafeFileName());
    }

    [Fact]
    public void GetSafeFileName_FallsBackWhenNothingUsableIsLeft()
    {
        Assert.Equal("file", ((string?)null).GetSafeFileName());
        Assert.Equal("file", "".GetSafeFileName());
        Assert.Equal("file", "\u0000\u202E".GetSafeFileName());
        Assert.Equal("unnamed", "..".GetSafeFileName("unnamed"));
    }

    [Fact]
    public void GetSafeFileName_NormalisesToNfc()
    {
        // NFD (e + combining acute) and NFC (single code point) look identical but are different strings.
        string nfd = "cafe\u0301.txt";
        string nfc = "caf\u00E9.txt";

        Assert.Equal(9, nfd.Length);
        Assert.Equal(8, nfc.Length);
        Assert.NotEqual(nfc, nfd);
        Assert.Equal(nfc, nfd.GetSafeFileName());
    }

    [Fact]
    public void GetSafeFileName_TruncatesButKeepsTheExtension()
    {
        string input = new string('a', 200) + ".pdf";
        string result = input.GetSafeFileName("file", 100);

        Assert.Equal(100, result.Length);
        Assert.EndsWith(".pdf", result);
        Assert.True(result.IsSafeFileName());

        // The extension keeps its original case; truncation must not silently rename "REPORT.PDF" to "REPORT.pdf".
        string upper = new string('a', 200) + ".PDF";
        Assert.EndsWith(".PDF", upper.GetSafeFileName("file", 100));
    }

    [Fact]
    public void GetSafeFileName_AlwaysProducesASafeName()
    {
        string[] hostile =
        {
            @"..\..\etc\passwd", "C:\\docs\\report.pdf", "CON", "CON.txt", "notes.txt.", "a\u0000b.txt",
            "invoice\u202Etxt.exe", "a/b/c.txt", "..", ".", " ", new string('x', 300)
        };

        foreach (string input in hostile)
        {
            Assert.True(input.GetSafeFileName().IsSafeFileName(), "Cleaned name is still unsafe for: " + input);
        }
    }

    // ------------------------------------------------------------------
    // FileNameUtil.IsPortableFileName
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("report-2026_final.pdf")]
    public void IsPortableFileName_AcceptsOrdinaryNames(string name)
    {
        Assert.True(name.IsPortableFileName());
    }

    [Theory]
    [InlineData(" notes.txt")]
    [InlineData("notes.txt ")]
    [InlineData("CON.txt")]
    [InlineData(@"dir\notes.txt")]
    public void IsPortableFileName_RejectsNamesThatShiftAcrossPlatforms(string name)
    {
        Assert.False(name.IsPortableFileName());
    }

    // ------------------------------------------------------------------
    // FilePathUtil root containment
    // ------------------------------------------------------------------

    [Fact]
    public void TryResolveUnderRoot_AcceptsRelativePathInsideRoot()
    {
        string root = CreateTempRoot();

        Assert.True(root.TryResolveUnderRoot("sub\\a.txt", out string resolved));
        Assert.True(Path.IsPathRooted(resolved));
        Assert.True(resolved.IsUnderRoot(root));
    }

    [Fact]
    public void TryResolveUnderRoot_RejectsParentTraversal()
    {
        string root = CreateTempRoot();

        Assert.False(root.TryResolveUnderRoot("..\\..\\evil.txt", out string? resolved));
        Assert.Null(resolved);
    }

    [Fact]
    public void TryResolveUnderRoot_RejectsAbsolutePathOutsideRoot()
    {
        string root = CreateTempRoot();
        string outside = Path.Combine(Path.GetTempPath(), "netutil-outside.txt");

        Assert.False(root.TryResolveUnderRoot(outside, out string? resolved));
        Assert.Null(resolved);
    }

    [Fact]
    public void IsUnderRoot_RespectsSegmentBoundaries()
    {
        string root = Path.Combine(Path.GetTempPath(), "netutil-root");
        string sibling = Path.Combine(Path.GetTempPath(), "netutil-root2", "a.txt");

        Assert.False(sibling.IsUnderRoot(root));
        Assert.True(Path.Combine(root, "a.txt").IsUnderRoot(root));
        Assert.True(root.IsUnderRoot(root));
    }

    [Fact]
    public void CombineUnderRoot_NormalisesSafeSegments()
    {
        string root = CreateTempRoot();
        string combined = root.CombineUnderRoot("sub", "a.txt");

        Assert.Equal(Path.Combine(root, "sub", "a.txt"), combined);
    }

    [Fact]
    public void CombineUnderRoot_RejectsRootedAndTraversalSegments()
    {
        string root = CreateTempRoot();
        string absolute = Path.Combine(root, "x.txt");

        Assert.Throws<ArgumentException>(() => root.CombineUnderRoot(absolute));
        Assert.Throws<ArgumentException>(() => root.CombineUnderRoot(".."));
        Assert.Throws<ArgumentException>(() => root.CombineUnderRoot("sub", "..\\..\\evil.txt"));
        Assert.Throws<ArgumentException>(() => root.CombineUnderRoot(""));
    }

    [Fact]
    public void IsPathLengthWithin_UsesCharacterBudget()
    {
        Assert.True("short.txt".IsPathLengthWithin());
        Assert.False((new string('a', 261) + ".txt").IsPathLengthWithin());
        Assert.True((new string('a', 260)).IsPathLengthWithin());
        Assert.True(((string?)null).IsPathLengthWithin());
    }

    // ------------------------------------------------------------------
    // FileKindUtil
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(@"C:\docs\a.PDF", FileKind.Pdf)]
    [InlineData("photo.JPG", FileKind.Jpeg)]
    [InlineData("book.xlsx", FileKind.Excel)]
    [InlineData("archive.tar.gz", FileKind.Gzip)]
    [InlineData("page.html", FileKind.Html)]
    [InlineData("setup.ps1", FileKind.Script)]
    [InlineData("noext", FileKind.Unknown)]
    [InlineData(".gitignore", FileKind.Unknown)]
    [InlineData("data.unknownext", FileKind.Unknown)]
    public void GetFileKind_UsesTheExtension(string path, FileKind expected)
    {
        Assert.Equal(expected, path.GetFileKind());
    }

    [Theory]
    [InlineData(FileKind.Pdf, FileCategory.Document)]
    [InlineData(FileKind.Jpeg, FileCategory.Image)]
    [InlineData(FileKind.Mp4, FileCategory.Video)]
    [InlineData(FileKind.Zip, FileCategory.Archive)]
    [InlineData(FileKind.Script, FileCategory.Executable)]
    [InlineData(FileKind.Json, FileCategory.Text)]
    [InlineData(FileKind.Unknown, FileCategory.Unknown)]
    public void GetFileCategory_GroupsKinds(FileKind kind, FileCategory expected)
    {
        Assert.Equal(expected, kind.GetFileCategory());
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("page.html")]
    [InlineData("logo.svg")]
    [InlineData("run.ps1")]
    [InlineData("archive.jar")]
    public void IsDangerousUploadExtension_FlagsActiveContent(string name)
    {
        Assert.True(name.IsDangerousUploadExtension());
    }

    [Theory]
    [InlineData("a.pdf")]
    [InlineData("photo.jpg")]
    [InlineData("noext")]
    public void IsDangerousUploadExtension_AcceptsPassiveFiles(string name)
    {
        Assert.False(name.IsDangerousUploadExtension());
    }

    // ------------------------------------------------------------------
    // FileContentTypeUtil
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("a.PDF", "application/pdf")]
    [InlineData("a.txt", "text/plain")]
    [InlineData("a.PNG", "image/png")]
    [InlineData("a.mp4", "video/mp4")]
    [InlineData("archive.tar.gz", "application/gzip")]
    [InlineData("book.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void GetContentType_MapsKnownExtensions(string path, string expected)
    {
        Assert.Equal(expected, path.GetContentType());
    }

    [Fact]
    public void GetContentType_FallsBackToOctetStream()
    {
        Assert.Equal(FileContentTypeUtil.DefaultContentType, "data.unknownext".GetContentType());
        Assert.Equal(FileContentTypeUtil.DefaultContentType, "noext".GetContentType());
        Assert.Equal(FileContentTypeUtil.DefaultContentType, ((string?)null).GetContentType());
    }

    [Fact]
    public void TryGetContentType_ReportsUnknownSeparately()
    {
        Assert.True("a.pdf".TryGetContentType(out string? contentType));
        Assert.Equal("application/pdf", contentType);

        Assert.False("data.unknownext".TryGetContentType(out contentType));
        Assert.Null(contentType);
    }

    // ------------------------------------------------------------------
    // FileTypeInfoUtil
    // ------------------------------------------------------------------

    [Fact]
    public void GetFileInfo_ResolvesKindCategoryAndMediaTypeInOneCall()
    {
        FileTypeInfo info = @"C:\incoming\Invoice.PDF".GetFileInfo();

        Assert.Equal(FileKind.Pdf, info.Kind);
        Assert.Equal(FileCategory.Document, info.Category);
        Assert.Equal("application/pdf", info.ContentType);
        Assert.True(info.IsKnown);
        Assert.False(info.IsDangerousUploadExtension);
    }

    [Fact]
    public void GetFileInfo_FlagsActiveContent()
    {
        FileTypeInfo info = "setup.EXE".GetFileInfo();

        Assert.Equal(FileKind.Exe, info.Kind);
        Assert.Equal(FileCategory.Executable, info.Category);
        Assert.Equal("application/x-msdownload", info.ContentType);
        Assert.True(info.IsDangerousUploadExtension);
    }

    [Fact]
    public void GetFileInfo_ReturnsDefaultsForUnknownInput()
    {
        FileTypeInfo unknown = "blob.unknownext".GetFileInfo();
        Assert.Equal(FileKind.Unknown, unknown.Kind);
        Assert.Equal(FileCategory.Unknown, unknown.Category);
        Assert.Equal(FileContentTypeUtil.DefaultContentType, unknown.ContentType);
        Assert.False(unknown.IsKnown);
        Assert.False(unknown.IsDangerousUploadExtension);

        FileTypeInfo missing = ((string?)null).GetFileInfo();
        Assert.Equal(FileKind.Unknown, missing.Kind);
        Assert.False(missing.IsKnown);
    }

    [Fact]
    public void TryGetFileInfo_ReportsKnownSeparately()
    {
        Assert.True("photo.png".TryGetFileInfo(out FileTypeInfo known));
        Assert.Equal(FileKind.Png, known.Kind);

        Assert.False("blob.unknownext".TryGetFileInfo(out _));
        Assert.False("noext".TryGetFileInfo(out _));
    }

    [Fact]
    public void TryGetFileInfo_TreatsMediaTypeOnlyExtensionsAsKnown()
    {
        // "heic" has a media type but no FileKind member, so the value is known but unclassified.
        Assert.True("photo.heic".TryGetFileInfo(out FileTypeInfo info));
        Assert.Equal(FileKind.Unknown, info.Kind);
        Assert.Equal("image/heic", info.ContentType);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "netutil-fileroot");
        Directory.CreateDirectory(root);
        return root;
    }
}
