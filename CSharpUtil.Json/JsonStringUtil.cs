using System;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

namespace CSharpUtil.StringUtil
{
    /// <summary>
    /// JSON helpers that need a third-party serializer: JSON shape and validity checks plus object
    /// deserialization. They live in the CSharpUtil.Extensions.Json assembly so that the core package
    /// carries no dependency; the namespace stays <c>CSharpUtil.StringUtil</c>, so existing
    /// <c>using</c> directives keep working unchanged.
    /// </summary>
    public static class JsonStringUtil
    {
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
                return createNew ? new T() : default;
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
