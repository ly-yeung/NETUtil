using System;
using System.Text;

namespace CSharpUtil.FileUtil
{
    /// <summary>
    /// Validation and sanitising for file names that come from untrusted input (an upload form field,
    /// a client header, a queue message). A name is a plain string; it never contains a directory.
    /// </summary>
    public static class FileNameUtil
    {
        /// <summary>
        /// Longest name NTFS and most other file systems accept for a single segment.
        /// </summary>
        public const int MaxFileNameLength = 255;

        private const string DefaultFallback = "file";

        private static readonly char[] InvalidChars = { '"', '<', '>', '|', ':', '*', '?', '\\', '/' };

        // Windows still refuses these names even with an extension: "CON.txt" behaves like "CON".
        private static readonly string[] ReservedDeviceNames =
        {
            "CON", "PRN", "AUX", "NUL", "CLOCK$",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        /// <summary>
        /// Reports whether a string can be used as a single file name.
        /// Rejects path separators, characters the file systems refuse, control characters, bidirectional
        /// formatting characters (U+202E turns "invoice\u202Etxt.exe" into something that displays as
        /// "invoice exp.txt"), trailing dots and spaces (Windows strips them silently, so two different
        /// names end up writing to the same file), and Windows reserved device names.
        /// </summary>
        /// <param name="name">Candidate file name; may be null.</param>
        /// <returns>True when the name is safe to use as a single path segment.</returns>
        public static bool IsSafeFileName(this string name)
        {
            string reason;
            return IsSafeFileName(name, out reason);
        }

        /// <summary>
        /// Reports whether a string can be used as a single file name and explains the first rule it broke.
        /// </summary>
        /// <param name="name">Candidate file name; may be null.</param>
        /// <param name="reason">Description of the failed rule, or null when the name is safe.</param>
        /// <returns>True when the name is safe to use as a single path segment.</returns>
        public static bool IsSafeFileName(this string name, out string reason)
        {
            reason = null;

            if (string.IsNullOrEmpty(name))
            {
                reason = "The name is empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                reason = "The name is whitespace only.";
                return false;
            }

            if (name.Length > MaxFileNameLength)
            {
                reason = "The name is longer than " + MaxFileNameLength + " characters.";
                return false;
            }

            if (name == "." || name == "..")
            {
                reason = "The name refers to the current or parent directory.";
                return false;
            }

            for (int i = 0; i < name.Length; i++)
            {
                char value = name[i];

                if (value == '\\' || value == '/')
                {
                    reason = "The name contains a path separator.";
                    return false;
                }

                if (Array.IndexOf(InvalidChars, value) >= 0)
                {
                    reason = "The name contains the character '" + value + "'.";
                    return false;
                }

                if (char.IsControl(value))
                {
                    reason = "The name contains a control character.";
                    return false;
                }

                if (IsInvisibleFormattingChar(value))
                {
                    reason = "The name contains an invisible formatting character.";
                    return false;
                }
            }

            char last = name[name.Length - 1];
            if (last == '.' || last == ' ')
            {
                reason = "The name ends with a dot or a space, which Windows strips.";
                return false;
            }

            if (IsReservedDeviceName(name))
            {
                reason = "The name is a reserved Windows device name.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reports whether a name works on both Windows and Linux without being altered.
        /// On top of <see cref="IsSafeFileName(string)"/> it rejects leading and trailing whitespace and names
        /// whose UTF-8 form exceeds 255 bytes, so the name survives a transfer to a file system that
        /// limits names by bytes rather than characters.
        /// </summary>
        /// <param name="name">Candidate file name; may be null.</param>
        /// <returns>True when the name is safe on both platforms.</returns>
        public static bool IsPortableFileName(this string name)
        {
            if (!IsSafeFileName(name))
            {
                return false;
            }

            if (!string.Equals(name, name.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            return Encoding.UTF8.GetByteCount(name) <= MaxFileNameLength;
        }

        /// <summary>
        /// Turns an untrusted name into a usable one: strips any directory part, normalises the text to
        /// NFC so visually identical names compare equal, replaces characters the file system refuses,
        /// drops control and invisible formatting characters, removes trailing dots and spaces, prefixes a
        /// reserved device name with "_", and truncates the name while keeping its extension.
        /// </summary>
        /// <param name="name">Untrusted name, possibly a full path; may be null.</param>
        /// <param name="fallback">Name to use when nothing usable is left. Defaults to "file".</param>
        /// <param name="maxLength">Maximum length of the result, clamped to 1 or more. Defaults to 100.</param>
        /// <returns>A name that passes <see cref="IsSafeFileName(string)"/> for every input that has at least one usable character.</returns>
        public static string GetSafeFileName(this string name, string fallback = DefaultFallback, int maxLength = 100)
        {
            if (string.IsNullOrEmpty(fallback))
            {
                fallback = DefaultFallback;
            }

            if (maxLength < 1)
            {
                maxLength = 1;
            }

            if (name == null)
            {
                return TruncatePreservingExtension(fallback, maxLength);
            }

            // Drop the directory part first so "..\..\etc\passwd" contributes only its last segment.
            string value = FilePathUtil.GetFileName(name);
            if (value.Length == 0)
            {
                value = name;
            }

            // IsNormalized scans without allocating; most names already are NFC, so the copy is skipped.
            if (!value.IsNormalized(NormalizationForm.FormC))
            {
                value = value.Normalize(NormalizationForm.FormC);
            }

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];

                if (current == '\\' || current == '/' || Array.IndexOf(InvalidChars, current) >= 0)
                {
                    builder.Append('_');
                }
                else if (char.IsControl(current) || IsInvisibleFormattingChar(current))
                {
                    // Drop these instead of replacing them: a visible replacement would still hide
                    // the fact that the client sent a control character.
                }
                else
                {
                    builder.Append(current);
                }
            }

            // Trim removes the trailing spaces that Windows would strip anyway.
            string cleaned = builder.ToString().Trim();

            // Find the last kept character first: trimming one dot per iteration would allocate a
            // string for every dot in "name.....".
            int end = cleaned.Length;
            while (end > 0 && cleaned[end - 1] == '.')
            {
                end--;
            }

            if (end != cleaned.Length)
            {
                cleaned = cleaned.Substring(0, end);
            }

            if (cleaned.Length == 0 || cleaned == "." || cleaned == "..")
            {
                return TruncatePreservingExtension(fallback, maxLength);
            }

            if (IsReservedDeviceName(cleaned))
            {
                cleaned = "_" + cleaned;
            }

            return TruncatePreservingExtension(cleaned, maxLength);
        }

        // C0/C1 controls are already covered by char.IsControl; these are the invisible ones that survive.
        private static bool IsInvisibleFormattingChar(char value)
        {
            return value == '\u200B' || value == '\u200C' || value == '\u200D' || value == '\u200E'
                || value == '\u200F' || value == '\u202A' || value == '\u202B' || value == '\u202C'
                || value == '\u202D' || value == '\u202E' || value == '\u2066' || value == '\u2067'
                || value == '\u2068' || value == '\u2069' || value == '\u061C' || value == '\uFEFF';
        }

        private static bool IsReservedDeviceName(string name)
        {
            // Compare in place: sub-stringing the stem and upper-casing it would allocate twice on
            // every name that reaches this check, and this runs for every validated upload.
            int end = name.IndexOf('.');
            if (end < 0)
            {
                end = name.Length;
            }

            while (end > 0 && name[end - 1] == ' ')
            {
                end--;
            }

            for (int i = 0; i < ReservedDeviceNames.Length; i++)
            {
                string reserved = ReservedDeviceNames[i];
                if (reserved.Length != end)
                {
                    continue;
                }

                if (string.Compare(name, 0, reserved, 0, end, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string TruncatePreservingExtension(string name, int maxLength)
        {
            if (name.Length <= maxLength)
            {
                return name;
            }

            int dot = FilePathUtil.GetExtensionDotIndex(name);
            int extensionLength = dot < 0 ? 0 : name.Length - dot;

            // Keep the extension only when it leaves room for a meaningful name part.
            if (extensionLength > 0 && extensionLength <= maxLength - 8)
            {
                int keep = maxLength - extensionLength;
                string stem = name.Substring(0, dot);
                if (stem.Length > keep)
                {
                    stem = stem.Substring(0, keep);
                }

                // Slice the extension out of the original name so its case survives the truncation.
                return TrimTrailingDotsAndSpaces(stem) + name.Substring(dot);
            }

            return TrimTrailingDotsAndSpaces(name.Substring(0, maxLength));
        }

        private static string TrimTrailingDotsAndSpaces(string value)
        {
            int end = value.Length;
            while (end > 0 && (value[end - 1] == '.' || value[end - 1] == ' '))
            {
                end--;
            }

            return value.Substring(0, end);
        }
    }
}
