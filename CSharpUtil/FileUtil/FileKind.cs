using System;

namespace CSharpUtil.FileUtil
{
    /// <summary>
    /// Broad family a file kind belongs to, for allowlist policies ("images only") without matching
    /// every format by hand.
    /// </summary>
    public enum FileCategory
    {
        /// <summary>Not classified.</summary>
        Unknown = 0,

        /// <summary>Plain or structured text.</summary>
        Text = 1,

        /// <summary>Office, PDF and e-book documents.</summary>
        Document = 2,

        /// <summary>Raster and vector images.</summary>
        Image = 3,

        /// <summary>Audio files.</summary>
        Audio = 4,

        /// <summary>Video files.</summary>
        Video = 5,

        /// <summary>Compressed archives.</summary>
        Archive = 6,

        /// <summary>Executables, libraries and scripts.</summary>
        Executable = 7,

        /// <summary>Fonts, databases, mail and calendar data.</summary>
        Data = 8
    }

    /// <summary>
    /// File type inferred from the extension only. It says nothing about the actual bytes: an uploaded
    /// "invoice.pdf" can be a renamed executable. Content based detection needs a stream and is a
    /// separate layer.
    /// </summary>
    public enum FileKind
    {
        /// <summary>Extension missing or unmapped.</summary>
        Unknown = 0,

        /// <summary>Plain text (.txt, .log).</summary>
        Text,

        /// <summary>Markdown (.md, .markdown).</summary>
        Markdown,

        /// <summary>HTML page (.html, .htm).</summary>
        Html,

        /// <summary>JSON (.json).</summary>
        Json,

        /// <summary>XML (.xml).</summary>
        Xml,

        /// <summary>YAML (.yaml, .yml).</summary>
        Yaml,

        /// <summary>Comma separated values (.csv).</summary>
        Csv,

        /// <summary>Portable Document Format (.pdf).</summary>
        Pdf,

        /// <summary>Word document (.doc, .docx).</summary>
        Word,

        /// <summary>Excel workbook (.xls, .xlsx, .xlsm).</summary>
        Excel,

        /// <summary>PowerPoint presentation (.ppt, .pptx).</summary>
        PowerPoint,

        /// <summary>Rich text format (.rtf).</summary>
        Rtf,

        /// <summary>OpenDocument text (.odt).</summary>
        OpenDocumentText,

        /// <summary>OpenDocument spreadsheet (.ods).</summary>
        OpenDocumentSpreadsheet,

        /// <summary>OpenDocument presentation (.odp).</summary>
        OpenDocumentPresentation,

        /// <summary>EPUB e-book (.epub).</summary>
        Epub,

        /// <summary>Portable Network Graphics (.png).</summary>
        Png,

        /// <summary>JPEG image (.jpg, .jpeg, .jfif).</summary>
        Jpeg,

        /// <summary>GIF image (.gif).</summary>
        Gif,

        /// <summary>Bitmap image (.bmp).</summary>
        Bmp,

        /// <summary>WebP image (.webp).</summary>
        Webp,

        /// <summary>TIFF image (.tif, .tiff).</summary>
        Tiff,

        /// <summary>Scalable Vector Graphics (.svg); script capable, see IsDangerousUploadExtension.</summary>
        Svg,

        /// <summary>Icon (.ico).</summary>
        Icon,

        /// <summary>MPEG audio (.mp3).</summary>
        Mp3,

        /// <summary>Wave audio (.wav).</summary>
        Wav,

        /// <summary>FLAC audio (.flac).</summary>
        Flac,

        /// <summary>AAC audio (.aac).</summary>
        Aac,

        /// <summary>Ogg audio (.ogg).</summary>
        Ogg,

        /// <summary>MPEG-4 video (.mp4).</summary>
        Mp4,

        /// <summary>Matroska video (.mkv).</summary>
        Mkv,

        /// <summary>Audio Video Interleave (.avi).</summary>
        Avi,

        /// <summary>QuickTime movie (.mov).</summary>
        Mov,

        /// <summary>WebM video (.webm).</summary>
        Webm,

        /// <summary>Windows Media Video (.wmv).</summary>
        Wmv,

        /// <summary>ZIP archive (.zip).</summary>
        Zip,

        /// <summary>Gzip archive (.gz, .gzip).</summary>
        Gzip,

        /// <summary>TAR archive (.tar).</summary>
        Tar,

        /// <summary>7-Zip archive (.7z).</summary>
        SevenZip,

        /// <summary>RAR archive (.rar).</summary>
        Rar,

        /// <summary>Bzip2 archive (.bz2).</summary>
        Bzip2,

        /// <summary>XZ archive (.xz).</summary>
        Xz,

        /// <summary>Windows executable (.exe).</summary>
        Exe,

        /// <summary>Dynamic link library (.dll).</summary>
        Dll,

        /// <summary>SQLite database (.db, .sqlite, .sqlite3).</summary>
        Sqlite,

        /// <summary>Shell or PowerShell script (.ps1, .sh, .bat, .cmd).</summary>
        Script,

        /// <summary>Font file (.ttf, .otf, .woff, .woff2).</summary>
        Font,

        /// <summary>E-mail message (.eml).</summary>
        Email,

        /// <summary>Calendar entry (.ics).</summary>
        Calendar,

        /// <summary>Contact card (.vcf).</summary>
        VCard
    }

    /// <summary>
    /// Extension driven file type helpers. Every lookup uses the last segment of a path only, so
    /// "C:\a\b.pdf" and "a/b.pdf" report the same kind.
    /// </summary>
    public static class FileKindUtil
    {
        /// <summary>
        /// Infers the file kind from the extension of a path.
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <returns>The matching kind, or <see cref="FileKind.Unknown"/> when the extension is missing or unmapped.</returns>
        public static FileKind GetFileKind(this string path)
        {
            FileExtensionTable.Entry entry;
            return FileExtensionTable.TryGetEntry(FilePathUtil.GetExtensionName(path), out entry)
                ? entry.Kind
                : FileKind.Unknown;
        }

        /// <summary>
        /// Maps a file kind to its broad family, so an allowlist can say "images only" without
        /// enumerating every format.
        /// </summary>
        /// <param name="kind">Kind to classify.</param>
        /// <returns>The matching category, or <see cref="FileCategory.Unknown"/>.</returns>
        public static FileCategory GetFileCategory(this FileKind kind)
        {
            switch (kind)
            {
                case FileKind.Text:
                case FileKind.Markdown:
                case FileKind.Html:
                case FileKind.Json:
                case FileKind.Xml:
                case FileKind.Yaml:
                case FileKind.Csv:
                    return FileCategory.Text;

                case FileKind.Pdf:
                case FileKind.Word:
                case FileKind.Excel:
                case FileKind.PowerPoint:
                case FileKind.Rtf:
                case FileKind.OpenDocumentText:
                case FileKind.OpenDocumentSpreadsheet:
                case FileKind.OpenDocumentPresentation:
                case FileKind.Epub:
                    return FileCategory.Document;

                case FileKind.Png:
                case FileKind.Jpeg:
                case FileKind.Gif:
                case FileKind.Bmp:
                case FileKind.Webp:
                case FileKind.Tiff:
                case FileKind.Svg:
                case FileKind.Icon:
                    return FileCategory.Image;

                case FileKind.Mp3:
                case FileKind.Wav:
                case FileKind.Flac:
                case FileKind.Aac:
                case FileKind.Ogg:
                    return FileCategory.Audio;

                case FileKind.Mp4:
                case FileKind.Mkv:
                case FileKind.Avi:
                case FileKind.Mov:
                case FileKind.Webm:
                case FileKind.Wmv:
                    return FileCategory.Video;

                case FileKind.Zip:
                case FileKind.Gzip:
                case FileKind.Tar:
                case FileKind.SevenZip:
                case FileKind.Rar:
                case FileKind.Bzip2:
                case FileKind.Xz:
                    return FileCategory.Archive;

                case FileKind.Exe:
                case FileKind.Dll:
                case FileKind.Script:
                    return FileCategory.Executable;

                case FileKind.Sqlite:
                case FileKind.Font:
                case FileKind.Email:
                case FileKind.Calendar:
                case FileKind.VCard:
                    return FileCategory.Data;

                default:
                    return FileCategory.Unknown;
            }
        }

        /// <summary>
        /// Reports whether the extension of a name belongs to the executable, script or active content
        /// family. This is a policy helper, not a security guarantee: it reads the name only.
        /// </summary>
        /// <param name="name">File name or path; may be null.</param>
        /// <returns>True when the extension is on the list of dangerous upload extensions.</returns>
        public static bool IsDangerousUploadExtension(this string name)
        {
            FileExtensionTable.Entry entry;
            return FileExtensionTable.TryGetEntry(FilePathUtil.GetExtensionName(name), out entry)
                && entry.Dangerous;
        }
    }
}
