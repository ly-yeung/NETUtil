using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CSharpUtil.NumberUtil
{
    /// <summary>
    /// Number helpers: display formatting (file size), Chinese RMB capitalisation and decimal rounding.
    /// </summary>
    public static class NumberUtil
    {
        private const int Kilo = 1024;

        private static readonly string[] RmbDigits =
        {
            "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖"
        };

        // Units inside one four-digit group: thousands, hundreds, tens, ones.
        private static readonly string[] RmbSmallUnits = { "", "拾", "佰", "仟" };

        // Units for groups of four digits, low to high.
        private static readonly string[] RmbGroupUnits = { "", "万", "亿", "万亿" };

        /// <summary>
        /// Formats a byte count with binary units (1 KB = 1024 bytes) as "1.50 KB", "3.00 MB" etc.
        /// The unit is promoted when rounding reaches the next boundary (1023.999 KB becomes 1.00 MB).
        /// </summary>
        /// <param name="bytes">Byte count; must not be negative.</param>
        /// <param name="decimals">Digits after the decimal point, clamped to 0..4. Defaults to 2.</param>
        /// <returns></returns>
        public static string FormatFileSize(this long bytes, int decimals = 2)
        {
            if (bytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes, "File size cannot be negative.");
            }

            if (decimals < 0)
            {
                decimals = 0;
            }
            else if (decimals > 4)
            {
                decimals = 4;
            }

            string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };

            if (bytes < Kilo)
            {
                return bytes.ToString(CultureInfo.InvariantCulture) + " B";
            }

            double size = bytes;
            int unit = 0;
            while (size >= Kilo && unit < units.Length - 1)
            {
                size /= Kilo;
                unit++;
            }

            // 1048575 bytes is 1023.999 KB: rounding to 2 decimals would print "1024.00 KB", so promote the unit first.
            if (unit < units.Length - 1
                && Math.Round(size, decimals, MidpointRounding.AwayFromZero) >= Kilo)
            {
                size /= Kilo;
                unit++;
            }

            return size.ToString("F" + decimals, CultureInfo.InvariantCulture) + " " + units[unit];
        }

        /// <summary>
        /// Converts an amount to Chinese RMB capitalisation, e.g. 1234.56 becomes 壹仟贰佰叁拾肆元伍角陆分.
        /// Values are rounded to 2 decimals (away from zero) first; negative amounts get a 负 prefix.
        /// Supported range is below 10^16 (10,000 万亿); larger values throw ArgumentOutOfRangeException.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static string ToChineseRmb(this decimal amount)
        {
            bool negative = amount < 0;
            decimal absolute = amount < 0 ? -amount : amount;
            decimal rounded = Math.Round(absolute, 2, MidpointRounding.AwayFromZero);

            if (rounded >= 10000000000000000m)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amounts of 10^16 or more are not supported.");
            }

            long integerPart = (long)Math.Truncate(rounded);
            int fraction = (int)((rounded - integerPart) * 100m);
            int jiao = fraction / 10;
            int fen = fraction % 10;

            StringBuilder builder = new StringBuilder();
            if (negative)
            {
                builder.Append('负');
            }

            builder.Append(integerPart == 0 ? "零元" : ConvertIntegerPart(integerPart) + "元");

            if (jiao == 0 && fen == 0)
            {
                builder.Append("整");
                return builder.ToString();
            }

            if (jiao > 0)
            {
                builder.Append(RmbDigits[jiao]).Append('角');
            }

            if (fen > 0)
            {
                // "零" is required when the jiao digit is empty: 0.05 reads 零元零伍分, not 零元伍分.
                if (jiao == 0)
                {
                    builder.Append('零');
                }
                builder.Append(RmbDigits[fen]).Append('分');
            }

            if (fen == 0)
            {
                builder.Append("整");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Rounds to the given number of decimal digits. Defaults to AwayFromZero (financial convention),
        /// which differs from Math.Round's default of ToEven (banker's rounding).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="digits">Decimal digits, 0..28.</param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static decimal RoundTo(this decimal value, int digits, MidpointRounding mode = MidpointRounding.AwayFromZero)
        {
            if (digits < 0 || digits > 28)
            {
                throw new ArgumentOutOfRangeException(nameof(digits), digits, "Digits must be between 0 and 28.");
            }

            return Math.Round(value, digits, mode);
        }

        /// <summary>
        /// Converts the integer part (already positive and truncated) into capitalised Chinese digits,
        /// collapsing zero runs the way Chinese numerals require: 10001 reads 壹万零壹, not 壹万零仟零佰零拾壹.
        /// </summary>
        private static string ConvertIntegerPart(long value)
        {
            List<int> groups = new List<int>();
            long remaining = value;
            do
            {
                groups.Add((int)(remaining % 10000));
                remaining /= 10000;
            }
            while (remaining > 0);

            StringBuilder builder = new StringBuilder();
            bool zeroBetween = false;

            for (int index = groups.Count - 1; index >= 0; index--)
            {
                int group = groups[index];
                if (group == 0)
                {
                    // Only meaningful when something was already emitted and a higher group follows.
                    if (builder.Length > 0)
                    {
                        zeroBetween = true;
                    }
                    continue;
                }

                // A group shorter than four digits needs one 零 between it and the previous group:
                // 1000001 reads 壹佰万零壹.
                if (builder.Length > 0 && (zeroBetween || group < 1000))
                {
                    builder.Append('零');
                }

                builder.Append(ConvertGroup(group)).Append(RmbGroupUnits[index]);
                zeroBetween = false;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts one group of at most four digits (1..9999) into capitalised Chinese digits,
        /// using 仟/佰/拾 and a single 零 for any run of zero digits inside the group.
        /// </summary>
        private static string ConvertGroup(int group)
        {
            StringBuilder builder = new StringBuilder();
            bool zeroPending = false;

            int weight = 1000;
            int unitIndex = 3;
            for (int i = 0; i < 4; i++)
            {
                int digit = (group / weight) % 10;
                if (digit == 0)
                {
                    if (builder.Length > 0)
                    {
                        zeroPending = true;
                    }
                }
                else
                {
                    if (zeroPending)
                    {
                        builder.Append('零');
                        zeroPending = false;
                    }
                    builder.Append(RmbDigits[digit]).Append(RmbSmallUnits[unitIndex]);
                }

                weight /= 10;
                unitIndex--;
            }

            return builder.ToString();
        }
    }
}
