using System;

namespace CSharpUtil.GuidUtil
{
    /// <summary>
    /// Compact GUID representation: 36 characters down to 22, for logs, short links and request ids.
    /// </summary>
    public static class GuidUtil
    {
        private const int ShortLength = 22;

        /// <summary>
        /// Encodes the GUID as 22 URL-safe Base64 characters (RFC 4648 alphabet with '-' and '_', padding removed).
        /// The bytes come from Guid.ToByteArray, so only ParseShortString reads it back - other tools see gibberish.
        /// </summary>
        public static string ToShortString(this Guid value)
        {
            // 16 bytes encode to 24 Base64 characters that always end with "==", so 22 is the usable length.
            string base64 = Convert.ToBase64String(value.ToByteArray());
            char[] buffer = new char[ShortLength];

            for (int index = 0; index < ShortLength; index++)
            {
                char current = base64[index];
                buffer[index] = current == '+' ? '-' : current == '/' ? '_' : current;
            }

            return new string(buffer);
        }

        /// <summary>
        /// Reads a value produced by ToShortString. Returns null instead of throwing when the input is null,
        /// has the wrong length or contains characters outside the URL-safe alphabet.
        /// </summary>
        public static Guid? ParseShortString(this string value)
        {
            if (value == null || value.Length != ShortLength)
            {
                return null;
            }

            char[] buffer = new char[ShortLength + 2];
            for (int index = 0; index < ShortLength; index++)
            {
                char current = value[index];
                if (current == '-')
                {
                    current = '+';
                }
                else if (current == '_')
                {
                    current = '/';
                }

                buffer[index] = current;
            }

            buffer[ShortLength] = '=';
            buffer[ShortLength + 1] = '=';

            try
            {
                byte[] bytes = Convert.FromBase64String(new string(buffer));
                return bytes.Length == 16 ? new Guid(bytes) : (Guid?)null;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
