using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

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

        // The nullable overloads' default parameter carries no default value on purpose: if both overloads had
        // one, a call like "value.ToInt()" would be ambiguous (CS0121). With this arrangement a call without
        // arguments binds to the non-nullable overload, and null-on-failure must be requested explicitly.
        /// <summary>
        /// Converts a JSON string to an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="createNew">If to create a default instance of T if the string is null or empty."></param>
        /// <returns></returns>
        public static T ConvertTo<T>(this string value, bool createNew = false) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return createNew? new T() : default;
            }
            return JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// Attempts to convert a JSON string to an object of type T and reports failure instead of throwing.
        /// Use this for text from untrusted sources (request body, config file, queue message); the throwing
        /// <c>ConvertTo&lt;T&gt;</c> stays the right choice where invalid input should fail fast.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="result">The deserialized instance, or null when conversion failed.</param>
        /// <returns>True when the text is valid JSON that produced an instance.</returns>
        public static bool TryConvertTo<T>(this string value, out T result) where T : class, new()
        {
            result = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(value);
                return result != null;
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a string to an integer. If the string is null or empty, returns the specified default value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string value) where T : struct, Enum
        {
            return TryParseEnum<T>(value, out T result) ? result : default;
        }

        /// <summary>
        /// Converts a string to an enum value, returning <paramref name="defaultValue"/> when the text does not
        /// name a member of T. Numeric input is accepted only for members the enum defines, or for any
        /// bit combination when T is a [Flags] enum.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string value, T defaultValue) where T : struct, Enum
        {
            return TryParseEnum<T>(value, out T result) ? result : defaultValue;
        }

        /// <summary>
        /// Determines whether the string names a member of T (same rule as the ToEnum overloads:
        /// case-insensitive; numeric text must match a defined member unless T is [Flags]).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsEnum<T>(this string value) where T : struct, Enum
        {
            return TryParseEnum<T>(value, out _);
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

            if (!Enum.TryParse(value.Trim(), true, out T parsed))
            {
                return false;
            }

            // Flags enums legitimately hold combinations, so any parsed bit pattern is accepted there.
            if (Enum.IsDefined(typeof(T), parsed) || typeof(T).IsDefined(typeof(FlagsAttribute), false))
            {
                result = parsed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a string to a Guid. Returns <paramref name="defaultValue"/> when the text is not a Guid.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static Guid ToGuid(this string value, Guid defaultValue)
        {
            return Guid.TryParse(value, out Guid result) ? result : defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable Guid. Pass null explicitly to get null on failure; the parameter
        /// deliberately has no default so a call without arguments binds to the non-nullable overload.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static Guid? ToGuid(this string value, Guid? defaultValue)
        {
            return Guid.TryParse(value, out Guid result) ? result : defaultValue;
        }

        /// <summary>
        /// Determines whether the string is a parseable Guid. Acronym-style input without dashes ("N" format)
        /// is accepted, matching Guid.TryParse.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsGuid(this string value)
        {
            return Guid.TryParse(value, out _);
        }

        /// <summary>
        /// Converts a string in the invariant "[-][d.]hh:mm:ss[.fffffff]" form to a TimeSpan.
        /// Returns <paramref name="defaultValue"/> on failure.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TimeSpan ToTimeSpan(this string value, TimeSpan defaultValue)
        {
            return TimeSpan.TryParse(value, ParsingCulture, out TimeSpan result) ? result : defaultValue;
        }

        /// <summary>
        /// Converts a string to a nullable TimeSpan. Pass null explicitly to get null on failure; the parameter
        /// deliberately has no default so a call without arguments binds to the non-nullable overload.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TimeSpan? ToTimeSpan(this string value, TimeSpan? defaultValue)
        {
            return TimeSpan.TryParse(value, ParsingCulture, out TimeSpan result) ? result : defaultValue;
        }

        /// <summary>
        /// Trims the string and turns blank text (null, empty or whitespace only) into null.
        /// Useful for normalizing optional input before persisting or comparing it.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// <param name="value"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static string DefaultIfBlank(this string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        /// <summary>
        /// Masks the middle of the string, keeping <paramref name="keepStart"/> leading characters and
        /// <paramref name="keepEnd"/> trailing characters. When the string is too short to leave anything
        /// hidden, the whole value is masked so nothing leaks. Null and empty input are returned unchanged.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="keepStart"></param>
        /// <param name="keepEnd"></param>
        /// <param name="maskChar"></param>
        /// <returns></returns>
        public static string Mask(this string value, int keepStart, int keepEnd, char maskChar = '*')
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (keepStart < 0)
            {
                keepStart = 0;
            }

            if (keepEnd < 0)
            {
                keepEnd = 0;
            }

            if (keepStart + keepEnd >= value.Length)
            {
                return new string(maskChar, value.Length);
            }

            return value.Substring(0, keepStart)
                + new string(maskChar, value.Length - keepStart - keepEnd)
                + value.Substring(value.Length - keepEnd);
        }

        /// <summary>
        /// Masks a mobile number: 13800138000 becomes 138****8000 (3 leading and 4 trailing characters kept).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MaskMobile(this string value)
        {
            return value.Mask(3, 4);
        }

        /// <summary>
        /// Masks an 18-digit ID card number: keeps the first 6 (region code) and last 4 characters.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MaskIdCard(this string value)
        {
            return value.Mask(6, 4);
        }

        /// <summary>
        /// Masks the local part of an email address and keeps the domain readable:
        /// "zhangsan@example.com" becomes "zh******@example.com". Text without a valid local/domain split
        /// (no '@', empty part) is masked with <see cref="Mask"/> instead so it never leaks.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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

            string local = value.Substring(0, at);
            string domain = value.Substring(at);

            string maskedLocal;
            if (local.Length <= 1)
            {
                maskedLocal = "*";
            }
            else if (local.Length == 2)
            {
                maskedLocal = local[0] + "*";
            }
            else
            {
                maskedLocal = local.Substring(0, 2) + new string('*', local.Length - 2);
            }

            return maskedLocal + domain;
        }

        /// <summary>
        /// Splits the string on the given separators (comma by default), trims every part and drops the
        /// empty ones: "a, b,, c " becomes ["a", "b", "c"]. Null or empty input yields an empty array.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separators">Separator characters; defaults to a single comma.</param>
        /// <returns></returns>
        public static string[] SplitTrim(this string value, params char[] separators)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new string[0];
            }

            char[] actual = separators == null || separators.Length == 0 ? new[] { ',' } : separators;

            // RemoveEmptyEntries only drops truly empty parts, so whitespace-only parts are filtered below.
            string[] parts = value.Split(actual, StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>(parts.Length);
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Cuts the string to at most <paramref name="maxLength"/> characters and appends
        /// <paramref name="suffix"/> (not counted in maxLength, so the result can be longer).
        /// A surrogate pair at the cut point is never split, so emoji stay intact.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <param name="suffix">Appended only when the string was actually cut; null by default.</param>
        /// <returns></returns>
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
        /// Determines whether a string looks like a JSON object, i.e. its first non-whitespace character is '{'.
        /// This is a shape check only: "{bad json" also returns true. Use <see cref="IsValidJsonObject(string)"/>
        /// when you need to know whether the text is really valid JSON. Costs O(1) for typical input and allocates nothing.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsLikeJsonObject(this string value)
        {
            return FirstMeaningfulChar(value) == '{';
        }

        /// <summary>
        /// Determines whether a string looks like a JSON array, i.e. its first non-whitespace character is '['.
        /// Shape check only; see <see cref="IsValidJsonArray(string)"/> for real validation.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsLikeJsonArray(this string value)
        {
            return FirstMeaningfulChar(value) == '[';
        }

        /// <summary>
        /// Returns the first character that is neither a BOM nor ASCII whitespace, or '\0' when there is none.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static char FirstMeaningfulChar(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return '\0';
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                // 只跳过 BOM 与 ASCII 空白：与 JSON 语法一致，也避免 char.IsWhiteSpace 按文化判定的开销
                if (c == '\uFEFF' || c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\v' || c == '\f')
                {
                    continue;
                }
                return c;
            }

            return '\0';
        }

        /// <summary>
        /// Determines whether a string is a syntactically valid JSON object, using the strict System.Text.Json rules
        /// (RFC 8259): single-quoted strings, unquoted property names, trailing commas and NaN are all rejected.
        /// Note this is stricter than Newtonsoft.Json, so only use it for input that follows the same rules.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsValidJsonObject(this string value)
        {
            return IsValidJsonDocument(value, JsonTokenType.StartObject);
        }

        /// <summary>
        /// Determines whether a string is a syntactically valid JSON array. See <see cref="IsValidJsonObject(string)"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsValidJsonArray(this string value)
        {
            return IsValidJsonDocument(value, JsonTokenType.StartArray);
        }

        /// <summary>
        /// Determines whether the UTF-8 bytes are a syntactically valid JSON object.
        /// Use this overload when the input is already bytes (request body, file, BLOB): it validates without allocating.
        /// </summary>
        /// <param name="utf8Json"></param>
        /// <returns></returns>
        public static bool IsValidJsonObject(this ReadOnlySpan<byte> utf8Json)
        {
            return IsValidJsonDocument(utf8Json, JsonTokenType.StartObject);
        }

        /// <summary>
        /// Determines whether the UTF-8 bytes are a syntactically valid JSON array.
        /// </summary>
        /// <param name="utf8Json"></param>
        /// <returns></returns>
        public static bool IsValidJsonArray(this ReadOnlySpan<byte> utf8Json)
        {
            return IsValidJsonDocument(utf8Json, JsonTokenType.StartArray);
        }

        /// <summary>
        /// Transcodes the text to UTF-8 and validates it. The transcode is the only allocation on this path.
        /// </summary>
        private static bool IsValidJsonDocument(string value, JsonTokenType expectedRoot)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // U+FEFF（UTF-8 BOM 解码后的字符）不被 JSON 语法允许，先去掉再校验
            if (value[0] == '\uFEFF')
            {
                value = value.Substring(1);
            }

            return IsValidJsonDocument(Encoding.UTF8.GetBytes(value), expectedRoot);
        }

        /// <summary>
        /// Walks every token with <see cref="Utf8JsonReader"/> and confirms the root token type.
        /// Allocates nothing; any structural error, bad escape or trailing content surfaces as JsonException.
        /// </summary>
        private static bool IsValidJsonDocument(ReadOnlySpan<byte> utf8Json, JsonTokenType expectedRoot)
        {
            try
            {
                Utf8JsonReader reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
                {
                    MaxDepth = 64,
                    CommentHandling = JsonCommentHandling.Disallow,
                    AllowTrailingCommas = false
                });

                if (!reader.Read() || reader.TokenType != expectedRoot)
                {
                    return false;
                }

                while (reader.Read())
                {
                    // 遍历全部 token，不构建任何对象模型
                }

                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }
    }
}
