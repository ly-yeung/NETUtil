using System;
using System.Globalization;
using System.Text;

namespace CSharpUtil.NumberUtil
{
    /// <summary>
    /// Number helpers: display formatting (file size), Chinese RMB capitalisation and decimal rounding.
    /// </summary>
    public static class NumberUtil
    {
        // 1024, not 1000: the units are binary even though the symbols say KB/MB. Named after what it is
        // rather than "Kilo", which normally means 1000 and misled every reader of this file.
        private const int BytesPerKilo = 1024;

        // Digits in one RMB group (万): used both as the group modulus and as the second group power.
        private const int RmbGroupSize = 10000;

        // Largest amount ToChineseRmb accepts: 10^16 would need a fifth group, which the tables do not have.
        private const decimal MaxSupportedAmount = 10000000000000000m;

        // decimal carries at most 28 fractional digits; Math.Round rejects anything above it.
        private const int MaxDecimalDigits = 28;

        // Kept static: building the unit array and the "F{decimals}" format string per call allocated
        // two objects on every FormatFileSize call. Indexed by (int)SizeUnit, so no lookup happens.
        // This array also defines which precisions are renderable - ClampDecimals follows its length.
        private static readonly string[] UnitSymbols = { "B", "KB", "MB", "GB", "TB", "PB" };

        private static readonly string[] FixedFormats = { "F0", "F1", "F2", "F3", "F4" };

        // One builder sized for the longest realistic output (negative, four groups, jiao and fen).
        private const int RmbBuilderCapacity = 48;

        private static readonly long[] RmbGroupPowers = { 1L, 10000L, 100000000L, 1000000000000L };

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
        /// Values below 1 KB, and calls capped at <see cref="SizeUnit.B"/>, print the raw byte count without
        /// decimals ("999 B"). Rounding is half away from zero, and the unit is promoted when rounding reaches
        /// the next boundary (1023.999 KB becomes 1.00 MB), but never past <paramref name="maxUnit"/>.
        /// </summary>
        /// <param name="bytes">Byte count; must not be negative.</param>
        /// <param name="decimals">Digits after the decimal point, clamped to 0..4. Defaults to 2.</param>
        /// <param name="maxUnit">
        /// Largest unit that may be used. Defaults to <see cref="SizeUnit.PB"/>, which promotes all the way up
        /// and matches the behaviour before this parameter existed. Pass <see cref="SizeUnit.KB"/> to keep
        /// large values in KB ("5242880.00 KB") instead of switching to MB/GB/TB. At the cap the value is
        /// allowed to overflow its unit ("1024.00 KB"), because moving to the next unit is not permitted.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytes"/> is negative, or <paramref name="maxUnit"/> is not one of the defined units.
        /// </exception>
        public static string FormatFileSize(this long bytes, int decimals = 2, SizeUnit maxUnit = SizeUnit.PB)
        {
            if (bytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes, "File size cannot be negative.");
            }

            // A public enum can carry any int through a cast, so an undefined value must be rejected rather
            // than silently indexed into the symbol array.
            int maxExponent = (int)maxUnit;
            if (maxExponent < 0 || maxExponent >= UnitSymbols.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(maxUnit), maxUnit, "maxUnit must be one of the defined SizeUnit values.");
            }

            // A fraction of a byte has no meaning, so the decimals setting does not apply to the raw form.
            if (bytes < BytesPerKilo || maxExponent == 0)
            {
                return $"{bytes.ToString(CultureInfo.InvariantCulture)} B";
            }

            int clamped = ClampDecimals(decimals);
            int exponent = ResolveUnit(bytes, clamped, maxExponent, out double size);

            return $"{size.ToString(FixedFormats[clamped], CultureInfo.InvariantCulture)} {UnitSymbols[exponent]}";
        }

        /// <summary>
        /// Picks the unit and the value to display, already rounded with the same rule that decides promotion.
        /// Keeping both decisions here avoids the unit being chosen with one rounding rule while the printed
        /// number uses another ("F2" rounds ties to even, <c>Math.Round</c> rounds them away from zero).
        /// </summary>
        private static int ResolveUnit(long bytes, int decimals, int maxExponent, out double size)
        {
            // Integer walk: one comparison per unit, at most five steps, and the loop bound is exactly the
            // caller's cap. The chosen exponent is the largest unit that still fits, so the value below is
            // always below 1024 - which is why the promotion below needs no second rounding.
            int exponent = 0;
            while (exponent < maxExponent && bytes >= UnitDivisor(exponent + 1))
            {
                exponent++;
            }

            double rounded = Math.Round((double)bytes / UnitDivisor(exponent), decimals, MidpointRounding.AwayFromZero);

            // 1048575 bytes is 1023.999 KB: rounding to 2 decimals would print "1024.00 KB", so promote the
            // unit first - but only when the promotion stays within the caller's cap. Capped calls keep the
            // overflowed value on purpose (a KB cap on 1048575 gives "1024.00 KB", never "1.00 MB").
            if (exponent < maxExponent && rounded >= BytesPerKilo)
            {
                exponent++;
                rounded /= BytesPerKilo;
            }

            size = rounded;
            return exponent;
        }

        /// <summary>
        /// Clamps the requested precision to what <c>FixedFormats</c> can render. Reading the length instead of
        /// a literal keeps the two in sync: adding "F5" there immediately allows decimals = 5 here.
        /// </summary>
        private static int ClampDecimals(int decimals)
        {
            int max = FixedFormats.Length - 1;
            if (decimals < 0)
            {
                return 0;
            }

            return decimals > max ? max : decimals;
        }

        /// <summary>
        /// 1024^exponent, the divisor for a unit. Single definition so the "power of 1024" rule is not
        /// restated in the loop bound and in the division.
        /// </summary>
        private static long UnitDivisor(int exponent)
        {
            return 1L << (10 * exponent);
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

            if (rounded >= MaxSupportedAmount)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amounts of 10^16 or more are not supported.");
            }

            long integerPart = (long)Math.Truncate(rounded);
            int fraction = (int)((rounded - integerPart) * 100m);
            int jiao = fraction / 10;
            int fen = fraction % 10;

            StringBuilder builder = new StringBuilder(RmbBuilderCapacity);
            if (negative)
            {
                builder.Append('负');
            }

            if (integerPart == 0)
            {
                builder.Append("零元");
            }
            else
            {
                AppendIntegerPart(builder, integerPart);
                builder.Append('元');
            }

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
            if (digits < 0 || digits > MaxDecimalDigits)
            {
                throw new ArgumentOutOfRangeException(nameof(digits), digits, "Digits must be between 0 and 28.");
            }

            return Math.Round(value, digits, mode);
        }

        /// <summary>
        /// Appends the integer part (already positive and truncated) into the caller's builder, collapsing
        /// zero runs the way Chinese numerals require: 10001 reads 壹万零壹, not 壹万零仟零佰零拾壹.
        /// Appending directly avoids a List per call plus a builder and a string per group.
        /// </summary>
        private static void AppendIntegerPart(StringBuilder builder, long value)
        {
            int groupCount = 1;
            while (groupCount < RmbGroupPowers.Length && value >= RmbGroupPowers[groupCount])
            {
                groupCount++;
            }

            bool emitted = false;
            bool zeroBetween = false;

            for (int index = groupCount - 1; index >= 0; index--)
            {
                int group = (int)((value / RmbGroupPowers[index]) % RmbGroupSize);
                if (group == 0)
                {
                    // Only meaningful when something was already emitted and a higher group follows.
                    if (emitted)
                    {
                        zeroBetween = true;
                    }
                    continue;
                }

                // A group shorter than four digits needs one 零 between it and the previous group:
                // 1000001 reads 壹佰万零壹.
                if (emitted && (zeroBetween || group < 1000))
                {
                    builder.Append('零');
                }

                AppendGroup(builder, group);
                builder.Append(RmbGroupUnits[index]);
                emitted = true;
                zeroBetween = false;
            }
        }

        /// <summary>
        /// Appends one group of at most four digits (1..9999) with 仟/佰/拾 and a single 零 for any run of
        /// zero digits inside the group.
        /// </summary>
        private static void AppendGroup(StringBuilder builder, int group)
        {
            bool emitted = false;
            bool zeroPending = false;

            int weight = 1000;
            int unitIndex = 3;
            for (int i = 0; i < 4; i++)
            {
                int digit = (group / weight) % 10;
                if (digit == 0)
                {
                    if (emitted)
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
                    emitted = true;
                }

                weight /= 10;
                unitIndex--;
            }
        }
    }
}
