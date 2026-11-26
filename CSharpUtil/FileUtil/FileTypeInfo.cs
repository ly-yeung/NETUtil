using System;

namespace CSharpUtil.FileUtil
{
    /// <summary>
    /// Everything the library can tell about a file name in one value: kind, category, media type and
    /// whether the extension is dangerous to accept on upload. It is a struct, so one call does not
    /// allocate anything beyond the extension string the lookup itself needs.
    /// </summary>
    public readonly struct FileTypeInfo
    {
        internal FileTypeInfo(FileExtensionTable.Entry entry)
        {
            _contentType = entry.ContentType;
            Kind = entry.Kind;
            Category = entry.Kind.GetFileCategory();
            IsKnown = entry.Kind != FileKind.Unknown || entry.ContentType != null;
            IsDangerousUploadExtension = entry.Dangerous;
        }

        private readonly string _contentType;

        /// <summary>Inferred file kind, or <see cref="FileKind.Unknown"/>.</summary>
        public FileKind Kind { get; }

        /// <summary>Broad family of <see cref="Kind"/>, or <see cref="FileCategory.Unknown"/>.</summary>
        public FileCategory Category { get; }

        /// <summary>
        /// Media type of the extension, or <see cref="FileContentTypeUtil.DefaultContentType"/> when the
        /// extension has no mapping. Never null, including for the default value.
        /// </summary>
        public string ContentType => _contentType ?? FileContentTypeUtil.DefaultContentType;

        /// <summary>
        /// False when the extension is missing or unknown, i.e. when the value carries only defaults.
        /// </summary>
        public bool IsKnown { get; }

        /// <summary>
        /// True when the extension is on the dangerous upload list (executables, scripts, active content).
        /// A policy hint based on the name only, not a security guarantee.
        /// </summary>
        public bool IsDangerousUploadExtension { get; }
    }

    /// <summary>
    /// Reads the whole extension table in a single pass. Use this when more than one of kind, category,
    /// media type and the dangerous flag is needed for the same name: the separate helpers each resolve
    /// the extension again, which is one extra scan and one extra temporary string per call.
    /// </summary>
    public static class FileTypeInfoUtil
    {
        /// <summary>
        /// Resolves kind, category, media type and the dangerous flag from the extension of a path.
        /// Unknown extensions come back as an unknown value with the default media type.
        /// </summary>
        /// <param name="path">File name or path; may be null.</param>
        /// <returns>A value describing the extension; never null.</returns>
        public static FileTypeInfo GetFileInfo(this string path)
        {
            FileExtensionTable.Entry entry;
            return FileExtensionTable.TryGetEntry(FilePathUtil.GetExtensionName(path), out entry)
                ? new FileTypeInfo(entry)
                : default(FileTypeInfo);
        }

        /// <summary>
        /// Resolves the same four facts as <see cref="GetFileInfo"/> and reports whether the extension
        /// carried any information at all.
        /// </summary>
        /// <param name="path">File name or path; may be null.</param>
        /// <param name="info">The resolved value, or the default value when nothing matched.</param>
        /// <returns>True when the extension is present in the table.</returns>
        public static bool TryGetFileInfo(this string path, out FileTypeInfo info)
        {
            FileExtensionTable.Entry entry;
            if (!FileExtensionTable.TryGetEntry(FilePathUtil.GetExtensionName(path), out entry))
            {
                info = default(FileTypeInfo);
                return false;
            }

            info = new FileTypeInfo(entry);
            return info.IsKnown;
        }
    }
}
