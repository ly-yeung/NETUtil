using System;

namespace CSharpUtil.FileUtil
{
    /// <summary>
    /// Maps a file extension to a media type without taking a dependency on ASP.NET Core.
    /// The value is meant for setting a response header or filling a database column; it is not a
    /// validation result, because the extension is chosen by whoever named the file.
    /// </summary>
    public static class FileContentTypeUtil
    {
        /// <summary>
        /// Media type reported when the extension is missing or unmapped.
        /// </summary>
        public const string DefaultContentType = "application/octet-stream";

        /// <summary>
        /// Returns the media type of a path's extension, falling back to
        /// <see cref="DefaultContentType"/> when the extension is missing or unmapped.
        /// No charset parameter is appended: the real encoding of the content is not knowable from
        /// the name, and a wrong "charset=utf-8" overrides the bytes' own BOM.
        /// </summary>
        /// <param name="path">File name or path; may be null.</param>
        /// <returns>A media type such as "image/png", or "application/octet-stream".</returns>
        public static string GetContentType(this string path)
        {
            string contentType;
            return TryGetContentType(path, out contentType) ? contentType : DefaultContentType;
        }

        /// <summary>
        /// Looks up the media type of a path's extension and reports whether it was found.
        /// Use this when "unknown" must stay distinguishable from a real octet-stream.
        /// </summary>
        /// <param name="path">File name or path; may be null.</param>
        /// <param name="contentType">The media type, or null when the extension is missing or unmapped.</param>
        /// <returns>True when the extension maps to a known media type.</returns>
        public static bool TryGetContentType(this string path, out string contentType)
        {
            contentType = null;

            FileExtensionTable.Entry entry;
            if (!FileExtensionTable.TryGetEntry(FilePathUtil.GetExtensionName(path), out entry)
                || entry.ContentType == null)
            {
                return false;
            }

            contentType = entry.ContentType;
            return true;
        }
    }
}
