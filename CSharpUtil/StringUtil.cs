using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CSharpUtil.StringUtil
{
    /// <summary>
    /// Utility methods for string operations.
    /// </summary>
    public static class StringUtil
    {
        // Every numeric and date conversion uses the invariant culture on purpose: tool input comes from
        // JSON, CSV, config files and APIs, all of which are culture-neutral. Parsing with the ambient
        // culture silently changes the value instead of failing ("1.5" becomes 15 on a de-DE machine,
        // because '.' is that culture's group separator).
        private static readonly CultureInfo ParsingCulture = CultureInfo.InvariantCulture;

        private const NumberStyles IntegerStyle = NumberStyles.Integer;
        private const NumberStyles FloatStyle = NumberStyles.Float | NumberStyles.AllowThousands;
        private const NumberStyles DecimalStyle = NumberStyles.Number;

        // Reused by SplitTrim: allocating a fresh { ',' } array per call was pure per-call garbage.
        private static readonly char[] DefaultSeparators = { ',' };

        // The nullable overloads' default parameter carries no default value on purpose: if both overloads had
        // one, a call like "value.ToInt()" would be ambiguous (CS0121). With this arrangement a call without
        // arguments binds to the non-nullable overload, and null-on-failure must be requested explicitly.
        /// <summary>
        /// Converts a string to an integer. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static int ToInt(this string value, int defaultValue = 0)
        {
            if (int.TryParse(value, IntegerStyle, ParsingCulture, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to an nullable integer. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static int? ToInt(this string value, int? defaultValue)
        {
            if (int.TryParse(value, IntegerStyle, ParsingCulture, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a short integer. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static short ToShort(this string value, short defaultValue = 0)
        {
            if (short.TryParse(value, IntegerStyle, ParsingCulture, out short result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        ///  Converts a string to a nullable short integer. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static short? ToShort(this string value, short? defaultValue)
        {
            if (short.TryParse(value, IntegerStyle, ParsingCulture, out short result))
            {
                return result;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Converts a string to a long integer. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static long ToLong(this string value, long defaultValue = 0)
        {
            if (long.TryParse(value, IntegerStyle, ParsingCulture, out long result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable long integer. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static long? ToLong(this string value, long? defaultValue)
        {
            if (long.TryParse(value, IntegerStyle, ParsingCulture, out long result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a double. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static double ToDouble(this string value, double defaultValue = 0)
        {
            // Since .NET Core 3.0 out-of-range overflow is no longer a parse failure, and "NaN" is a valid
            // literal: "1e400" yields Infinity and "NaN" yields NaN. Neither is a usable number here,
            // so both fall back to the supplied default instead of leaking Infinity/NaN to the caller.
            if (double.TryParse(value, FloatStyle, ParsingCulture, out double result) && !double.IsInfinity(result) && !double.IsNaN(result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable double. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static double? ToDouble(this string value, double? defaultValue)
        {
            // Same rule as the non-nullable overload: Infinity and NaN are treated as "no usable value".
            if (double.TryParse(value, FloatStyle, ParsingCulture, out double result) && !double.IsInfinity(result) && !double.IsNaN(result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a float. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static float ToFloat(this string value, float defaultValue = 0)
        {
            // float has the same overflow-to-Infinity semantics as double ("1e40" -> Infinity).
            if (float.TryParse(value, FloatStyle, ParsingCulture, out float result) && !float.IsInfinity(result) && !float.IsNaN(result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable float. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static float? ToFloat(this string value, float? defaultValue)
        {
            // Same rule as the non-nullable overload.
            if (float.TryParse(value, FloatStyle, ParsingCulture, out float result) && !float.IsInfinity(result) && !float.IsNaN(result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a decimal. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static decimal ToDecimal(this string value, decimal defaultValue = 0)
        {
            if (decimal.TryParse(value, DecimalStyle, ParsingCulture, out decimal result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable decimal. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static decimal? ToDecimal(this string value, decimal? defaultValue)
        {
            if (decimal.TryParse(value, DecimalStyle, ParsingCulture, out decimal result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a bool. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static bool ToBool(this string value, bool defaultValue = false)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable bool. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static bool? ToBool(this string value, bool? defaultValue)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts a string to a DateTime. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static DateTime ToDateTime(this string value, DateTime defaultValue)
        {
            if (DateTime.TryParse(value, ParsingCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Converts a string to a nullable DateTime. If the string is null or empty, returns the specified default value.
        /// </summary>
        public static DateTime? ToDateTime(this string value, DateTime? defaultValue)
        {
            if (DateTime.TryParse(value, ParsingCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Determines whether the specified string is a valid number.
        /// Non-finite results (Infinity/NaN) are rejected so that a successful check guarantees
        /// <see cref="ToDouble(string, double)"/> returns a real value instead of the fallback.
        /// </summary>
        public static bool IsNumber(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return double.TryParse(value, FloatStyle, ParsingCulture, out double result) && !double.IsInfinity(result) && !double.IsNaN(result);
        }
        
        /// <summary>
        /// Determines whether the specified string is a valid integer.
        /// </summary>
        public static bool IsInteger(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return int.TryParse(value, IntegerStyle, ParsingCulture, out _);
        }

        /// <summary>
        /// Determines whether the specified string is a valid long integer.
        /// </summary>
        public static bool IsLong(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return long.TryParse(value, IntegerStyle, ParsingCulture, out _);
        }
        
        /// <summary>
        /// Determines whether the specified string is a valid short integer.
        /// </summary>
        public static bool IsShort(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return short.TryParse(value, IntegerStyle, ParsingCulture, out _);
        }
        
        /// <summary>
        /// Determines whether the specified string is a valid float.
        /// Non-finite results (Infinity/NaN) are rejected, matching <see cref="ToFloat(string, float)"/>.
        /// </summary>
        public static bool IsFloat(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return float.TryParse(value, FloatStyle, ParsingCulture, out float result) && !float.IsInfinity(result) && !float.IsNaN(result);
        }
        
        /// <summary>
        /// Determines whether the specified string is a valid decimal number.
        /// Backed by decimal.TryParse, which uses NumberStyles.Number: group separators ("1,234.5") are accepted,
        /// but exponent notation ("1e5") is not. This is why IsDecimal and IsDouble can disagree.
        /// </summary>
        public static bool IsDecimal(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return decimal.TryParse(value, DecimalStyle, ParsingCulture, out _);
        }

        /// <summary>
        /// Determines whether the specified string is a valid boolean.
        /// </summary>
        public static bool IsBool(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return bool.TryParse(value, out _);
        }

        /// <summary>
        /// Determines whether the specified string is a valid double.
        /// Non-finite results (Infinity/NaN) are rejected, matching <see cref="ToDouble(string, double)"/>.
        /// </summary>
        public static bool IsDouble(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return double.TryParse(value, FloatStyle, ParsingCulture, out double result) && !double.IsInfinity(result) && !double.IsNaN(result);
        }

        /// <summary>
        /// Determines whether the specified string is a valid DateTime.
        /// </summary>
        public static bool IsDateTime(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return DateTime.TryParse(value, ParsingCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Converts a string to an enum value using its member name, ignoring case. Blank text, unknown names
        /// and numeric text that the enum does not define all return the enum's default value.
        /// </summary>
        public static T ToEnum<T>(this string value) where T : struct, Enum
        {
            return TryParseEnum<T>(value, out T result) ? result : default;
        }

        /// <summary>
        /// Converts a string to an enum value, returning <paramref name="defaultValue"/> when the text does not
        /// name a member of T. Numeric input is accepted only for members the enum defines, or for any
        /// bit combination when T is a [Flags] enum.
        /// </summary>
        public static T ToEnum<T>(this string value, T defaultValue) where T : struct, Enum
        {
            return TryParseEnum<T>(value, out T result) ? result : defaultValue;
        }

        /// <summary>
        /// Determines whether the string names a member of T (same rule as the ToEnum overloads:
        /// case-insensitive; numeric text must match a defined member unless T is [Flags]).
        /// </summary>
        public static bool IsEnum<T>(this string value) where T : struct, Enum
        {
            return TryParseEnum<T>(value, out _);
        }

        /// <summary>
        /// Per-type facts about T, computed once. Type.IsDefined is a reflection call, and asking for the
        /// FlagsAttribute on every conversion showed up as measurable overhead in a tight parse loop.
        /// </summary>
        private static class EnumMetadata<T> where T : struct, Enum
        {
            internal static readonly bool IsFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);
        }

        /// <summary>
        /// Shared enum parsing rule. Enum.TryParse alone is not enough: it happily turns "99" into a value
        /// that no member defines, which usually means the input is corrupt rather than intentional.
        /// </summary>
        private static bool TryParseEnum<T>(string value, out T result) where T : struct, Enum
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = value.Trim();
            if (!Enum.TryParse(trimmed, true, out T parsed))
            {
                return false;
            }

            // Flags enums legitimately hold combinations, so any parsed bit pattern is accepted there.
            if (EnumMetadata<T>.IsFlags)
            {
                result = parsed;
                return true;
            }

#if NET8_0_OR_GREATER
            // The generic overload checks the value without boxing it.
            if (Enum.IsDefined<T>(parsed))
            {
                result = parsed;
                return true;
            }
#else
            if (Enum.IsDefined(typeof(T), parsed))
            {
                result = parsed;
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// Converts a string to a Guid. Returns <paramref name="defaultValue"/> when the text is not a Guid.
        /// </summary>
        public static Guid ToGuid(this string value, Guid defaultValue)
        {
            return Guid.TryParse(value, out Guid result) ? result : defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable Guid. Pass null explicitly to get null on failure; the parameter
        /// deliberately has no default so a call without arguments binds to the non-nullable overload.
        /// </summary>
        public static Guid? ToGuid(this string value, Guid? defaultValue)
        {
            return Guid.TryParse(value, out Guid result) ? result : defaultValue;
        }

        /// <summary>
        /// Determines whether the string is a parseable Guid. Acronym-style input without dashes ("N" format)
        /// is accepted, matching Guid.TryParse.
        /// </summary>
        public static bool IsGuid(this string value)
        {
            return Guid.TryParse(value, out _);
        }

        /// <summary>
        /// Converts a string in the invariant "[-][d.]hh:mm:ss[.fffffff]" form to a TimeSpan.
        /// Returns <paramref name="defaultValue"/> on failure.
        /// </summary>
        public static TimeSpan ToTimeSpan(this string value, TimeSpan defaultValue)
        {
            return TimeSpan.TryParse(value, ParsingCulture, out TimeSpan result) ? result : defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable TimeSpan. Pass null explicitly to get null on failure; the parameter
        /// deliberately has no default so a call without arguments binds to the non-nullable overload.
        /// </summary>
        public static TimeSpan? ToTimeSpan(this string value, TimeSpan? defaultValue)
        {
            return TimeSpan.TryParse(value, ParsingCulture, out TimeSpan result) ? result : defaultValue;
        }

        /// <summary>
        /// Trims the string and turns blank text (null, empty or whitespace only) into null.
        /// Useful for normalizing optional input before persisting or comparing it.
        /// </summary>
        public static string TrimToNull(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        /// <summary>
        /// Returns <paramref name="fallback"/> when the string is blank, otherwise the original string
        /// (untrimmed, so callers keep the exact formatting).
        /// </summary>
        public static string DefaultIfBlank(this string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        /// <summary>
        /// Masks the middle of the string, keeping <paramref name="keepStart"/> leading characters and
        /// <paramref name="keepEnd"/> trailing characters. When the string is too short to leave anything
        /// hidden, the whole value is masked so nothing leaks. Null and empty input are returned unchanged.
        /// </summary>
        public static string Mask(this string value, int keepStart, int keepEnd, char maskChar = '*')
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Clamping before the sum also keeps "keepStart + keepEnd" from overflowing into a negative
            // value, which would skip the full-mask branch and leak the original text.
            if (keepStart < 0)
            {
                keepStart = 0;
            }
            else if (keepStart > value.Length)
            {
                keepStart = value.Length;
            }

            if (keepEnd < 0)
            {
                keepEnd = 0;
            }
            else if (keepEnd > value.Length)
            {
                keepEnd = value.Length;
            }

            if (keepStart + keepEnd >= value.Length)
            {
                return new string(maskChar, value.Length);
            }

            // Mask in a single buffer: three Substrings plus a concatenation produced four strings
            // for one masked value.
            char[] chars = value.ToCharArray();
            int end = value.Length - keepEnd;
            for (int i = keepStart; i < end; i++)
            {
                chars[i] = maskChar;
            }

            return new string(chars);
        }

        /// <summary>
        /// Masks a mobile number: 13800138000 becomes 138****8000 (3 leading and 4 trailing characters kept).
        /// </summary>
        public static string MaskMobile(this string value)
        {
            return value.Mask(3, 4);
        }

        /// <summary>
        /// Masks an 18-digit ID card number: keeps the first 6 (region code) and last 4 characters.
        /// </summary>
        public static string MaskIdCard(this string value)
        {
            return value.Mask(6, 4);
        }

        /// <summary>
        /// Masks the local part of an email address and keeps the domain readable:
        /// "zhangsan@example.com" becomes "zh******@example.com". Text without a valid local/domain split
        /// (no '@', empty part) is masked with <see cref="Mask"/> instead so it never leaks.
        /// </summary>
        public static string MaskEmail(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            int at = value.IndexOf('@');
            if (at <= 0 || at == value.Length - 1)
            {
                return value.Mask(1, 0);
            }

            // Mask the local part in place. The previous version built two Substrings, a masked local
            // part and two concatenations, i.e. six strings for one masked address.
            char[] chars = value.ToCharArray();
            int keep = at <= 1 ? 0 : (at == 2 ? 1 : 2);
            for (int i = keep; i < at; i++)
            {
                chars[i] = '*';
            }

            return new string(chars);
        }

        /// <summary>
        /// Splits the string on the given separators (comma by default), trims every part and drops the
        /// empty ones: "a, b,, c " becomes ["a", "b", "c"]. Null or empty input yields an empty array.
        /// </summary>
        /// <param name="value">Text to split.</param>
        /// <param name="separators">Separator characters; defaults to a single comma.</param>
        public static string[] SplitTrim(this string value, params char[] separators)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new string[0];
            }

            char[] actual = separators == null || separators.Length == 0 ? DefaultSeparators : separators;
            List<string> result = new List<string>();

            // One scan instead of Split + List + a second Trim per part: string.Split materialises every
            // raw part first, and each part then cost a Trim allocation before being copied again.
            // Here each kept part is trimmed by index and copied exactly once.
            int index = 0;
            while (index <= value.Length)
            {
                int end = index;
                while (end < value.Length && Array.IndexOf(actual, value[end]) < 0)
                {
                    end++;
                }

                int start = index;
                int stop = end;
                while (start < stop && char.IsWhiteSpace(value[start]))
                {
                    start++;
                }

                while (stop > start && char.IsWhiteSpace(value[stop - 1]))
                {
                    stop--;
                }

                if (stop > start)
                {
                    result.Add(value.Substring(start, stop - start));
                }

                index = end + 1;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Cuts the string to at most <paramref name="maxLength"/> characters and appends
        /// <paramref name="suffix"/> (not counted in maxLength, so the result can be longer).
        /// A surrogate pair at the cut point is never split, so emoji stay intact.
        /// </summary>
        /// <param name="value">Text to cut.</param>
        /// <param name="maxLength">Maximum characters to keep.</param>
        /// <param name="suffix">Appended only when the string was actually cut; null by default.</param>
        public static string Truncate(this string value, int maxLength, string suffix = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (maxLength <= 0)
            {
                return string.Empty;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            int cut = maxLength;
            if (char.IsHighSurrogate(value[cut - 1]))
            {
                cut--;
            }

            if (cut <= 0)
            {
                return string.Empty;
            }

            string head = value.Substring(0, cut);
            return suffix == null ? head : head + suffix;
        }

        /// <summary>
        /// Returns the string unchanged when it is at most <paramref name="maxLength"/> characters, otherwise
        /// throws ArgumentOutOfRangeException. Null passes, so an optional field keeps one responsibility per
        /// method: presence is checked by <see cref="EnsureNotEmpty"/> or <see cref="IsNullOrWhiteSpace"/>.
        /// </summary>
        /// <param name="value">Text to check.</param>
        /// <param name="maxLength">Inclusive upper bound.</param>
        /// <param name="paramName">Argument name reported in the exception; set it with nameof at the call site.</param>
        public static string EnsureMaxLength(this string value, int maxLength, string paramName = "value")
        {
            if (maxLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "The length limit must not be negative.");
            }

            if (value == null || value.Length <= maxLength)
            {
                return value;
            }

            throw new ArgumentOutOfRangeException(paramName, "Length " + value.Length + " exceeds the limit of " + maxLength + ".");
        }

        /// <summary>
        /// Returns the string unchanged when it carries content, otherwise throws ArgumentException. White space
        /// counts as missing, so a required field never ends up holding "   ".
        /// </summary>
        /// <param name="value">Text to check.</param>
        /// <param name="paramName">Argument name reported in the exception; set it with nameof at the call site.</param>
        public static string EnsureNotEmpty(this string value, string paramName = "value")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The value must not be null, empty or white space.", paramName);
            }

            return value;
        }

        /// <summary>
        /// True when the string is null, empty or only white space.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// True when the string is null or empty. White space counts as content here, unlike IsNullOrWhiteSpace.
        /// </summary>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Returns an empty string instead of null; non-null values are returned unchanged.
        /// </summary>
        public static string EmptyIfNull(this string value)
        {
            return value ?? string.Empty;
        }

        /// <summary>
        /// Trims the value and collapses each internal white-space run into one space: "  a \t b " becomes "a b".
        /// Null and all-white-space input becomes string.Empty. Values already in that shape are returned as-is,
        /// so the common case costs one scan and no allocation.
        /// </summary>
        public static string NormalizeSpace(this string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (!NeedsSpaceNormalization(value))
            {
                return value;
            }

            char[] buffer = new char[value.Length];
            int written = 0;
            bool pendingSpace = false;

            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (char.IsWhiteSpace(current))
                {
                    // A run at the very start produces no leading space; every later run produces exactly one.
                    pendingSpace = written > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    buffer[written++] = ' ';
                    pendingSpace = false;
                }

                buffer[written++] = current;
            }

            return new string(buffer, 0, written);
        }

        /// <summary>
        /// Detects whether NormalizeSpace has any work to do, so an already-clean value escapes without a copy.
        /// </summary>
        private static bool NeedsSpaceNormalization(string value)
        {
            bool pendingSpace = false;

            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (char.IsWhiteSpace(current))
                {
                    // Edges always need trimming, and any tab/newline inside must become a plain space.
                    if (index == 0 || index == value.Length - 1 || current != ' ')
                    {
                        return true;
                    }

                    if (pendingSpace)
                    {
                        return true;
                    }

                    pendingSpace = true;
                    continue;
                }

                pendingSpace = false;
            }

            return false;
        }

        /// <summary>
        /// Joins non-blank values, dropping null and white-space entries instead of leaving empty slots
        /// ("a", "", "b" gives "a,b"). Returns string.Empty when nothing remains.
        /// </summary>
        public static string JoinNonEmpty(string separator, IEnumerable<string> values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            if (separator == null)
            {
                separator = string.Empty;
            }

            StringBuilder builder = null;
            foreach (string item in values)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                if (builder == null)
                {
                    builder = new StringBuilder(item.Length);
                }
                else
                {
                    builder.Append(separator);
                }

                builder.Append(item);
            }

            return builder == null ? string.Empty : builder.ToString();
        }

        /// <summary>
        /// Literal-value overload of <see cref="JoinNonEmpty(string, IEnumerable{string})"/>: no array is created
        /// at the call site. Passing a null literal as the last argument is ambiguous between the two overloads.
        /// </summary>
        public static string JoinNonEmpty(string separator, params string[] values)
        {
            return JoinNonEmpty(separator, (IEnumerable<string>)values);
        }

        /// <summary>
        /// The part before the first occurrence of the delimiter ("a=b".CutBefore("=") gives "a"). When the
        /// delimiter is absent, empty or null, the whole value is returned unchanged.
        /// </summary>
        public static string CutBefore(this string value, string delimiter)
        {
            if (value == null || string.IsNullOrEmpty(delimiter))
            {
                return value;
            }

            int index = value.IndexOf(delimiter, StringComparison.Ordinal);
            return index < 0 ? value : value.Substring(0, index);
        }

        /// <summary>
        /// Character overload of <see cref="CutBefore(string, string)"/> for single-character delimiters.
        /// </summary>
        public static string CutBefore(this string value, char delimiter)
        {
            if (value == null)
            {
                return value;
            }

            int index = value.IndexOf(delimiter);
            return index < 0 ? value : value.Substring(0, index);
        }

        /// <summary>
        /// The part after the first occurrence of the delimiter ("a=b".CutAfter("=") gives "b"). When the
        /// delimiter is absent, empty or null, the whole value is returned unchanged.
        /// </summary>
        public static string CutAfter(this string value, string delimiter)
        {
            if (value == null || string.IsNullOrEmpty(delimiter))
            {
                return value;
            }

            int index = value.IndexOf(delimiter, StringComparison.Ordinal);
            return index < 0 ? value : value.Substring(index + delimiter.Length);
        }

        /// <summary>
        /// Character overload of <see cref="CutAfter(string, string)"/> for single-character delimiters.
        /// </summary>
        public static string CutAfter(this string value, char delimiter)
        {
            if (value == null)
            {
                return value;
            }

            int index = value.IndexOf(delimiter);
            return index < 0 ? value : value.Substring(index + 1);
        }

        /// <summary>
        /// UTF-8 bytes of the text; null becomes an empty array.
        /// </summary>
        public static byte[] ToUtf8Bytes(this string value)
        {
            return value == null || value.Length == 0 ? new byte[0] : Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Decodes UTF-8 bytes back to text; null or empty input becomes string.Empty.
        /// </summary>
        public static string FromUtf8Bytes(this byte[] data)
        {
            return data == null || data.Length == 0 ? string.Empty : Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Standard Base64 (RFC 4648, '+' and '/' kept, padding kept).
        /// </summary>
        public static string ToBase64(this byte[] data)
        {
            return data == null || data.Length == 0 ? string.Empty : Convert.ToBase64String(data);
        }

        /// <summary>
        /// URL-safe Base64: '-' and '_' instead of '+' and '/', padding removed. Safe in query strings,
        /// JWT segments and file names.
        /// </summary>
        public static string ToBase64Url(this byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            string base64 = Convert.ToBase64String(data);
            char[] buffer = new char[base64.Length];
            int written = 0;

            for (int index = 0; index < base64.Length; index++)
            {
                char current = base64[index];
                if (current == '=')
                {
                    continue;
                }

                buffer[written++] = current == '+' ? '-' : current == '/' ? '_' : current;
            }

            return new string(buffer, 0, written);
        }

        /// <summary>
        /// Decodes standard Base64. Null, empty or malformed input returns null rather than throwing.
        /// </summary>
        public static byte[] FromBase64(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes URL-safe Base64 written by <see cref="ToBase64Url"/>. Null, empty or malformed input returns null.
        /// </summary>
        public static byte[] FromBase64Url(this string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length % 4 == 1)
            {
                // A length of 4n+1 cannot be produced by Base64, so it is rejected before any padding attempt.
                return null;
            }

            int padding = (4 - value.Length % 4) % 4;
            char[] buffer = new char[value.Length + padding];

            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                buffer[index] = current == '-' ? '+' : current == '_' ? '/' : current;
            }

            for (int index = 0; index < padding; index++)
            {
                buffer[value.Length + index] = '=';
            }

            try
            {
                return Convert.FromBase64String(new string(buffer));
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
