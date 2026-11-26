using System;
using System.Collections.Generic;

namespace CSharpUtil.FileUtil
{
    /// <summary>
    /// Single source of truth for what an extension means: its kind, its media type and whether it
    /// sits on the dangerous upload list. Keeping the three facts in one row is what stops them from
    /// drifting apart; the public helpers are thin readers of this table.
    /// </summary>
    internal static class FileExtensionTable
    {
        /// <summary>A single row. Immutable, and free to hand out once the table is built.</summary>
        internal readonly struct Entry
        {
            /// <summary>File kind, or <see cref="FileKind.Unknown"/> when only a media type is known.</summary>
            internal readonly FileKind Kind;

            /// <summary>Media type, or null when this extension has no media type mapping.</summary>
            internal readonly string ContentType;

            /// <summary>True when the extension belongs to the executable, script or active content family.</summary>
            internal readonly bool Dangerous;

            internal Entry(FileKind kind, string contentType, bool dangerous)
            {
                Kind = kind;
                ContentType = contentType;
                Dangerous = dangerous;
            }
        }

        // Keyed by the lowercased extension without the dot, which is what FilePathUtil.GetExtensionName
        // returns; Ordinal is therefore enough and avoids the culture-aware comparers.
        private static readonly Dictionary<string, Entry> Extensions =
            new Dictionary<string, Entry>(StringComparer.Ordinal)
            {
                { "7z", new Entry(FileKind.SevenZip, "application/x-7z-compressed", false) },
                { "aac", new Entry(FileKind.Aac, "audio/aac", false) },
                { "apk", new Entry(FileKind.Unknown, "application/vnd.android.package-archive", false) },
                { "avi", new Entry(FileKind.Avi, "video/x-msvideo", false) },
                { "avif", new Entry(FileKind.Unknown, "image/avif", false) },
                { "bat", new Entry(FileKind.Script, null, true) },
                { "bmp", new Entry(FileKind.Bmp, "image/bmp", false) },
                { "bz2", new Entry(FileKind.Bzip2, "application/x-bzip2", false) },
                { "cmd", new Entry(FileKind.Script, null, true) },
                { "com", new Entry(FileKind.Unknown, null, true) },
                { "cpl", new Entry(FileKind.Unknown, null, true) },
                { "crt", new Entry(FileKind.Unknown, null, true) },
                { "css", new Entry(FileKind.Unknown, "text/css", false) },
                { "csv", new Entry(FileKind.Csv, "text/csv", false) },
                { "db", new Entry(FileKind.Sqlite, "application/vnd.sqlite3", false) },
                { "deb", new Entry(FileKind.Unknown, "application/vnd.debian.binary-package", false) },
                { "dll", new Entry(FileKind.Dll, "application/x-msdownload", true) },
                { "doc", new Entry(FileKind.Word, "application/msword", false) },
                { "docx", new Entry(FileKind.Word, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", false) },
                { "eml", new Entry(FileKind.Email, "message/rfc822", false) },
                { "epub", new Entry(FileKind.Epub, "application/epub+zip", false) },
                { "exe", new Entry(FileKind.Exe, "application/x-msdownload", true) },
                { "flac", new Entry(FileKind.Flac, "audio/flac", false) },
                { "flv", new Entry(FileKind.Unknown, "video/x-flv", false) },
                { "gif", new Entry(FileKind.Gif, "image/gif", false) },
                { "gz", new Entry(FileKind.Gzip, "application/gzip", false) },
                { "gzip", new Entry(FileKind.Gzip, "application/gzip", false) },
                { "heic", new Entry(FileKind.Unknown, "image/heic", false) },
                { "hta", new Entry(FileKind.Unknown, null, true) },
                { "htm", new Entry(FileKind.Html, "text/html", true) },
                { "html", new Entry(FileKind.Html, "text/html", true) },
                { "ico", new Entry(FileKind.Icon, "image/x-icon", false) },
                { "ics", new Entry(FileKind.Calendar, "text/calendar", false) },
                { "inf", new Entry(FileKind.Unknown, null, true) },
                { "jar", new Entry(FileKind.Unknown, "application/java-archive", true) },
                { "jfif", new Entry(FileKind.Jpeg, "image/jpeg", false) },
                { "jpeg", new Entry(FileKind.Jpeg, "image/jpeg", false) },
                { "jpg", new Entry(FileKind.Jpeg, "image/jpeg", false) },
                { "js", new Entry(FileKind.Unknown, null, true) },
                { "jse", new Entry(FileKind.Unknown, null, true) },
                { "json", new Entry(FileKind.Json, "application/json", false) },
                { "lnk", new Entry(FileKind.Unknown, null, true) },
                { "log", new Entry(FileKind.Text, "text/plain", false) },
                { "m4a", new Entry(FileKind.Unknown, "audio/mp4", false) },
                { "m4v", new Entry(FileKind.Unknown, "video/x-m4v", false) },
                { "markdown", new Entry(FileKind.Markdown, "text/markdown", false) },
                { "md", new Entry(FileKind.Markdown, "text/markdown", false) },
                { "mkv", new Entry(FileKind.Mkv, "video/x-matroska", false) },
                { "mov", new Entry(FileKind.Mov, "video/quicktime", false) },
                { "mp3", new Entry(FileKind.Mp3, "audio/mpeg", false) },
                { "mp4", new Entry(FileKind.Mp4, "video/mp4", false) },
                { "msi", new Entry(FileKind.Unknown, "application/x-msi", true) },
                { "msp", new Entry(FileKind.Unknown, null, true) },
                { "odp", new Entry(FileKind.OpenDocumentPresentation, "application/vnd.oasis.opendocument.presentation", false) },
                { "ods", new Entry(FileKind.OpenDocumentSpreadsheet, "application/vnd.oasis.opendocument.spreadsheet", false) },
                { "odt", new Entry(FileKind.OpenDocumentText, "application/vnd.oasis.opendocument.text", false) },
                { "oga", new Entry(FileKind.Unknown, "audio/ogg", false) },
                { "ogg", new Entry(FileKind.Ogg, "audio/ogg", false) },
                { "otf", new Entry(FileKind.Font, "font/otf", false) },
                { "pdf", new Entry(FileKind.Pdf, "application/pdf", false) },
                { "pif", new Entry(FileKind.Unknown, null, true) },
                { "png", new Entry(FileKind.Png, "image/png", false) },
                { "ppt", new Entry(FileKind.PowerPoint, "application/vnd.ms-powerpoint", false) },
                { "pptx", new Entry(FileKind.PowerPoint, "application/vnd.openxmlformats-officedocument.presentationml.presentation", false) },
                { "ps1", new Entry(FileKind.Script, null, true) },
                { "psd", new Entry(FileKind.Unknown, "image/vnd.adobe.photoshop", false) },
                { "psm1", new Entry(FileKind.Unknown, null, true) },
                { "rar", new Entry(FileKind.Rar, "application/vnd.rar", false) },
                { "reg", new Entry(FileKind.Unknown, null, true) },
                { "rpm", new Entry(FileKind.Unknown, "application/x-rpm", false) },
                { "rtf", new Entry(FileKind.Rtf, "application/rtf", false) },
                { "scf", new Entry(FileKind.Unknown, null, true) },
                { "scr", new Entry(FileKind.Unknown, null, true) },
                { "sh", new Entry(FileKind.Script, null, false) },
                { "sqlite", new Entry(FileKind.Sqlite, "application/vnd.sqlite3", false) },
                { "sqlite3", new Entry(FileKind.Sqlite, "application/vnd.sqlite3", false) },
                { "svg", new Entry(FileKind.Svg, "image/svg+xml", true) },
                { "tar", new Entry(FileKind.Tar, "application/x-tar", false) },
                { "tif", new Entry(FileKind.Tiff, "image/tiff", false) },
                { "tiff", new Entry(FileKind.Tiff, "image/tiff", false) },
                { "ttf", new Entry(FileKind.Font, "font/ttf", false) },
                { "txt", new Entry(FileKind.Text, "text/plain", false) },
                { "url", new Entry(FileKind.Unknown, null, true) },
                { "vbe", new Entry(FileKind.Unknown, null, true) },
                { "vbs", new Entry(FileKind.Unknown, null, true) },
                { "vcf", new Entry(FileKind.VCard, "text/vcard", false) },
                { "wav", new Entry(FileKind.Wav, "audio/wav", false) },
                { "webm", new Entry(FileKind.Webm, "video/webm", false) },
                { "webp", new Entry(FileKind.Webp, "image/webp", false) },
                { "wma", new Entry(FileKind.Unknown, "audio/x-ms-wma", false) },
                { "wmv", new Entry(FileKind.Wmv, "video/x-ms-wmv", false) },
                { "woff", new Entry(FileKind.Font, "font/woff", false) },
                { "woff2", new Entry(FileKind.Font, "font/woff2", false) },
                { "wsf", new Entry(FileKind.Unknown, null, true) },
                { "wsh", new Entry(FileKind.Unknown, null, true) },
                { "xhtml", new Entry(FileKind.Unknown, null, true) },
                { "xls", new Entry(FileKind.Excel, "application/vnd.ms-excel", false) },
                { "xlsm", new Entry(FileKind.Excel, "application/vnd.ms-excel.sheet.macroEnabled.12", false) },
                { "xlsx", new Entry(FileKind.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false) },
                { "xml", new Entry(FileKind.Xml, "application/xml", false) },
                { "xz", new Entry(FileKind.Xz, "application/x-xz", false) },
                { "yaml", new Entry(FileKind.Yaml, "application/yaml", false) },
                { "yml", new Entry(FileKind.Yaml, "application/yaml", false) },
                { "zip", new Entry(FileKind.Zip, "application/zip", false) },
            };

        /// <summary>
        /// Looks up an extension that is already lowercased and has no leading dot.
        /// </summary>
        /// <param name="extension">Extension such as "png"; may be null or empty.</param>
        /// <param name="entry">The matching row when the extension is mapped.</param>
        /// <returns>True when the extension has a row.</returns>
        internal static bool TryGetEntry(string extension, out Entry entry)
        {
            if (extension == null || extension.Length == 0)
            {
                entry = default(Entry);
                return false;
            }

            return Extensions.TryGetValue(extension, out entry);
        }
    }
}
