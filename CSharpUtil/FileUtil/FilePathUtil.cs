using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CSharpUtil.FileUtil
{
    /// <summary>
    /// Path helpers for file names and path validation.
    /// Separators are treated the same way on every OS: both '\' and '/' split a path, because paths
    /// often arrive from mixed clients (a Windows client sends "a\b.txt" while the server runs Linux,
    /// and <see cref="Path.GetFileName(string)"/> would return the whole string there).
    /// </summary>
    public static class FilePathUtil
    {
        private const int DefaultMaxPathLength = 260;

        private static readonly char[] Separators = { '\\', '/' };

        // Windows compares paths case-insensitively; Linux does not. Choose once so every method agrees.
        private static readonly StringComparison PathComparison =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        /// <summary>
        /// Returns the last segment of a path, keeping its extension ("a\b\report.PDF" -> "report.PDF").
        /// A path that ends with a separator has no file name and returns an empty string, which matches
        /// <see cref="Path.GetFileName(string)"/> and keeps "C:\a\b\" from turning into the directory name "b".
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <returns>The last segment, or an empty string when there is none.</returns>
        public static string GetFileName(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            int end = path.Length - 1;
            if (IsSeparator(path[end]))
            {
                return string.Empty;
            }

            int start = end;
            while (start >= 0 && !IsSeparator(path[start]))
            {
                start--;
            }

            return path.Substring(start + 1, end - start);
        }

        /// <summary>
        /// Returns the last segment without its extension ("a\b\report.PDF" -> "report").
        /// Unlike <see cref="Path.GetFileNameWithoutExtension(string)"/>, a dot file keeps its name:
        /// ".gitignore" returns ".gitignore" instead of an empty string, and "notes.txt." returns
        /// "notes.txt." because a trailing dot is not an extension.
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <returns>The name without extension, or an empty string when there is no file name.</returns>
        public static string GetFileNameWithoutExtension(this string path)
        {
            string name = GetFileName(path);
            if (name.Length == 0)
            {
                return name;
            }

            int dot = GetExtensionDotIndex(name);
            return dot < 0 ? name : name.Substring(0, dot);
        }

        /// <summary>
        /// Returns the extension of the last segment in lower case, including the dot
        /// ("a\b\Report.PDF" -> ".pdf").
        /// A dot file has no extension (".gitignore" -> an empty string), and a trailing dot is not an
        /// extension either ("notes.txt." -> an empty string); both cases differ from
        /// <see cref="Path.GetExtension(string)"/>, which returns ".gitignore" and "" respectively and therefore
        /// leaves the caller guessing which one it received.
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <returns>The lower-case extension with its dot, or an empty string when there is none.</returns>
        public static string GetExtension(this string path)
        {
            int dot = GetExtensionDotIndexOfPath(path);
            return dot < 0 ? string.Empty : ToLowerInvariantIfNeeded(path.Substring(dot));
        }

        /// <summary>
        /// Returns the extension without its dot in lower case ("a\b\Report.PDF" -> "pdf").
        /// Useful for table lookups and comparisons; <see cref="GetExtension"/> keeps the dot for display.
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <returns>The lower-case extension without a dot, or an empty string when there is none.</returns>
        public static string GetExtensionName(this string path)
        {
            int dot = GetExtensionDotIndexOfPath(path);
            return dot < 0 ? string.Empty : ToLowerInvariantIfNeeded(path.Substring(dot + 1));
        }

        /// <summary>
        /// Returns the trailing extension segments in lower case, including the dots
        /// ("archive.tar.gz" -> ".tar.gz").
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <param name="maxSegments">How many trailing segments to combine, clamped to 1..4. Defaults to 2.</param>
        /// <returns>The combined extension, or an empty string when there is none.</returns>
        public static string GetCompoundExtension(this string path, int maxSegments = 2)
        {
            if (maxSegments < 1)
            {
                maxSegments = 1;
            }
            else if (maxSegments > 4)
            {
                maxSegments = 4;
            }

            string name = GetFileName(path);
            if (name.Length == 0)
            {
                return string.Empty;
            }

            // Walk left once, remembering where the outermost accepted segment starts, then copy the
            // whole tail in a single Substring instead of concatenating a piece per segment.
            int end = name.Length;
            int start = -1;
            int found = 0;

            while (found < maxSegments)
            {
                if (end <= 1)
                {
                    break;
                }

                int dot = name.LastIndexOf('.', end - 1);

                // dot == 0 means the leading dot of a dot file, which is a name character, not an extension.
                if (dot <= 0)
                {
                    break;
                }

                start = dot;
                end = dot;
                found++;
            }

            return start < 0 ? string.Empty : ToLowerInvariantIfNeeded(name.Substring(start));
        }

        /// <summary>
        /// Reports whether the last segment ends with one of the given extensions.
        /// The comparison ignores case and accepts extensions written with or without a leading dot,
        /// because ".JPG" only equals ".jpg" when case is ignored.
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <param name="extensions">Extensions to test, with or without a leading dot.</param>
        /// <returns>True when the extension matches one of the candidates.</returns>
        public static bool IsExtensionOf(this string path, params string[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
            {
                return false;
            }

            string extension = GetExtension(path);
            if (extension.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < extensions.Length; i++)
            {
                string candidate = extensions[i];
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                if (ExtensionEquals(extension, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reports whether a resolved path stays inside a root directory.
        /// The comparison respects segment boundaries, so "/srv/data2" is not inside "/srv/data",
        /// and it applies the platform's case rules (case-insensitive on Windows only).
        /// </summary>
        /// <param name="path">Path to test.</param>
        /// <param name="root">Directory that must contain the path.</param>
        /// <returns>True when the path is inside the root.</returns>
        public static bool IsUnderRoot(this string path, string root)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            try
            {
                string normalizedPath = NormalizeFullPath(path);
                string normalizedRoot = NormalizeFullPath(root);
                return IsUnderNormalizedRoot(normalizedPath, normalizedRoot);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (PathTooLongException)
            {
                return false;
            }
        }

        /// <summary>
        /// Resolves a candidate path and reports whether it stays inside the root directory.
        /// This is the check that prevents a user supplied relative path from escaping with "..".
        /// </summary>
        /// <param name="root">Directory that must contain the candidate.</param>
        /// <param name="candidate">Relative or absolute path to resolve; absolute paths are resolved as given.</param>
        /// <param name="resolvedPath">The normalized absolute path, or null when the check fails.</param>
        /// <returns>True when the candidate resolves inside the root.</returns>
        public static bool TryResolveUnderRoot(this string root, string candidate, out string resolvedPath)
        {
            resolvedPath = null;

            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            try
            {
                string normalizedRoot = NormalizeFullPath(root);
                string normalizedCandidate = Path.IsPathRooted(candidate)
                    ? NormalizeFullPath(candidate)
                    : NormalizeFullPath(Path.Combine(normalizedRoot, candidate));

                if (!IsUnderNormalizedRoot(normalizedCandidate, normalizedRoot))
                {
                    return false;
                }

                resolvedPath = normalizedCandidate;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (PathTooLongException)
            {
                return false;
            }
        }

        /// <summary>
        /// Combines path segments under an absolute root and returns the normalized path.
        /// Rooted segments are rejected instead of silently replacing the root: Path.Combine("root", "C:\evil.txt")
        /// returns "C:\evil.txt", and Path.Combine("root", "..\..\evil") keeps the ".." untouched.
        /// </summary>
        /// <param name="root">Absolute directory that must contain the result.</param>
        /// <param name="segments">Relative segments to append; null and empty segments are rejected.</param>
        /// <returns>The normalized absolute path.</returns>
        public static string CombineUnderRoot(this string root, params string[] segments)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("Root directory must not be empty.", nameof(root));
            }

            if (!Path.IsPathRooted(root))
            {
                throw new ArgumentException("Root directory must be absolute.", nameof(root));
            }

            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            string combined = NormalizeFullPath(root);

            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (string.IsNullOrEmpty(segment))
                {
                    throw new ArgumentException("Path segments must not be null or empty.", nameof(segments));
                }

                if (Path.IsPathRooted(segment) || segment.IndexOf(':') >= 0)
                {
                    throw new ArgumentException("Path segments must be relative and must not contain a volume separator.", nameof(segments));
                }

                if (HasParentSegment(segment))
                {
                    throw new ArgumentException("Path segments must not contain a parent directory reference (..).", nameof(segments));
                }

                combined = Path.Combine(combined, segment);
            }

            string resolved;
            if (!TryResolveUnderRoot(root, combined, out resolved))
            {
                throw new ArgumentException("The combined path escapes the root directory.", nameof(segments));
            }

            return resolved;
        }

        /// <summary>
        /// Reports whether a path fits in a character budget.
        /// The default 260 matches the classic Windows MAX_PATH limit, which still applies unless long paths
        /// are enabled; pass a larger value on systems where they are.
        /// </summary>
        /// <param name="path">Path to measure; a null path counts as 0 characters.</param>
        /// <param name="maxChars">Maximum number of characters, clamped to 1 or more. Defaults to 260.</param>
        /// <returns>True when the path is short enough.</returns>
        public static bool IsPathLengthWithin(this string path, int maxChars = DefaultMaxPathLength)
        {
            if (maxChars < 1)
            {
                maxChars = 1;
            }

            return (path == null ? 0 : path.Length) <= maxChars;
        }

        /// <summary>
        /// Returns the index of the dot that starts the extension of the last segment of a path, or -1.
        /// Works on the whole path so callers do not have to materialise the file name first.
        /// </summary>
        /// <param name="path">Full or partial path; may be null.</param>
        /// <returns>The dot index, or -1.</returns>
        internal static int GetExtensionDotIndexOfPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return -1;
            }

            int end = path.Length - 1;
            if (IsSeparator(path[end]))
            {
                // A path that ends with a separator has no file name, so it has no extension.
                return -1;
            }

            int dot = path.LastIndexOf('.');
            if (dot <= 0 || dot == end)
            {
                return -1;
            }

            int separator = path.LastIndexOfAny(Separators, end);
            if (dot <= separator || dot == separator + 1)
            {
                // The last dot sits in a directory segment, or it is the leading dot of a dot file.
                return -1;
            }

            return dot;
        }

        /// <summary>
        /// Returns the index of the dot that starts the extension of a file name, or -1 when the name
        /// has no extension. A dot at index 0 belongs to the name (".gitignore") and a trailing dot is
        /// not an extension ("notes.txt."); both report -1.
        /// </summary>
        /// <param name="fileName">A file name without any directory part.</param>
        /// <returns>The dot index, or -1.</returns>
        internal static int GetExtensionDotIndex(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return -1;
            }

            int dot = fileName.LastIndexOf('.');
            if (dot <= 0 || dot == fileName.Length - 1)
            {
                return -1;
            }

            return dot;
        }

        private static bool IsSeparator(char value)
        {
            return value == '\\' || value == '/';
        }

        // Lower casing is only needed when the name actually carries upper case; the common case
        // ("report.pdf") then costs a single Substring instead of a substring plus a converted copy.
        private static string ToLowerInvariantIfNeeded(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsUpper(value[i]))
                {
                    return value.ToLowerInvariant();
                }
            }

            return value;
        }

        // Compares ".pdf" with "pdf" or ".PDF" without building the ".<candidate>" string first.
        private static bool ExtensionEquals(string extension, string candidate)
        {
            // Skip the leading dot of whichever string has one, then compare the bare extension text.
            int offset = candidate[0] == '.' ? 1 : 0;
            int length = candidate.Length - offset;

            if (extension.Length != length + 1)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (char.ToLowerInvariant(extension[i + 1]) != char.ToLowerInvariant(candidate[i + offset]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeFullPath(string path)
        {
            string full = Path.GetFullPath(path);
            string root = Path.GetPathRoot(full) ?? string.Empty;

            // Keep a trailing separator only when it is the volume root itself ("C:\", "/").
            int end = full.Length;
            while (end > root.Length && IsSeparator(full[end - 1]))
            {
                end--;
            }

            return end == full.Length ? full : full.Substring(0, end);
        }

        private static bool IsUnderNormalizedRoot(string normalizedPath, string normalizedRoot)
        {
            if (string.Equals(normalizedPath, normalizedRoot, PathComparison))
            {
                return true;
            }

            if (normalizedPath.Length <= normalizedRoot.Length)
            {
                return false;
            }

            // Compare in place instead of slicing the prefix into a new string.
            if (string.Compare(normalizedPath, 0, normalizedRoot, 0, normalizedRoot.Length, PathComparison) != 0)
            {
                return false;
            }

            // A root that already ends with a separator ("C:\", "/") is complete; otherwise the next
            // character must be a separator too, so "/srv/data2" does not count as inside "/srv/data".
            if (normalizedRoot.Length > 0 && IsSeparator(normalizedRoot[normalizedRoot.Length - 1]))
            {
                return true;
            }

            return IsSeparator(normalizedPath[normalizedRoot.Length]);
        }

        private static bool HasParentSegment(string segment)
        {
            int index = 0;

            while (index < segment.Length)
            {
                int separator = segment.IndexOfAny(Separators, index);
                int length = (separator < 0 ? segment.Length : separator) - index;

                if (length == 2
                    && segment[index] == '.'
                    && segment[index + 1] == '.')
                {
                    return true;
                }

                if (separator < 0)
                {
                    break;
                }

                index = separator + 1;
            }

            return false;
        }
    }
}
