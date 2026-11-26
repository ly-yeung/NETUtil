using System;
using System.Globalization;

namespace CSharpUtil.DateTimeUtil
{
    /// <summary>
    /// Timestamp conversion, day boundaries and a version-style encoding of an instant.
    /// </summary>
    public static class DateTimeUtil
    {
        private const long TicksPerSecond = 10000000L;
        private const long TicksPerMillisecond = 10000L;

        // 1970-01-01T00:00:00Z in DateTime ticks. Kept as a constant so no DateTimeOffset is constructed per call.
        private const long UnixEpochTicks = 621355968000000000L;

        private const int SecondsTimestampLength = 10;
        private const int MillisecondsTimestampLength = 13;

        /// <summary>
        /// Converts to Unix seconds (1970-01-01T00:00:00Z). Unspecified kinds are treated as local time,
        /// which is what ToUniversalTime already assumes.
        /// </summary>
        public static long ToUnixSeconds(this DateTime value)
        {
            return (value.ToUniversalTime().Ticks - UnixEpochTicks) / TicksPerSecond;
        }

        /// <summary>
        /// Converts to Unix milliseconds.
        /// </summary>
        public static long ToUnixMilliseconds(this DateTime value)
        {
            return (value.ToUniversalTime().Ticks - UnixEpochTicks) / TicksPerMillisecond;
        }

        /// <summary>
        /// Builds a UTC DateTime from Unix seconds; throws ArgumentOutOfRangeException instead of wrapping around
        /// when the value cannot be represented.
        /// </summary>
        public static DateTime FromUnixSeconds(this long value)
        {
            return FromUnixValue(value, TicksPerSecond, "seconds");
        }

        /// <summary>
        /// Builds a UTC DateTime from Unix milliseconds.
        /// </summary>
        public static DateTime FromUnixMilliseconds(this long value)
        {
            return FromUnixValue(value, TicksPerMillisecond, "milliseconds");
        }

        /// <summary>
        /// True when the text is exactly 10 digits (seconds) or 13 digits (milliseconds). Shorter or longer
        /// values are rejected on purpose: "2024" and 8-digit legacy ids would otherwise silently become dates.
        /// </summary>
        public static bool IsUnixTimestamp(this string value)
        {
            if (value == null || (value.Length != SecondsTimestampLength && value.Length != MillisecondsTimestampLength))
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] < '0' || value[index] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts a 10-digit (seconds) or 13-digit (milliseconds) string to a UTC DateTime; null when the text
        /// is not a timestamp per <see cref="IsUnixTimestamp"/>.
        /// </summary>
        public static DateTime? ToTimestamp(this string value)
        {
            if (!value.IsUnixTimestamp())
            {
                return null;
            }

            long number = long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
            return value.Length == SecondsTimestampLength ? FromUnixSeconds(number) : FromUnixMilliseconds(number);
        }

        /// <summary>
        /// Midnight of the same day, keeping the original DateTimeKind.
        /// </summary>
        public static DateTime StartOfDay(this DateTime value)
        {
            return value.Date;
        }

        /// <summary>
        /// The last tick of the same day (23:59:59.9999999), so a BETWEEN-style filter includes the whole day.
        /// </summary>
        public static DateTime EndOfDay(this DateTime value)
        {
            DateTime start = value.Date;
            return start == DateTime.MaxValue.Date ? DateTime.MaxValue : start.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// True when the value falls inside [min, max], both ends inclusive.
        /// </summary>
        public static bool IsBetween(this DateTime value, DateTime min, DateTime max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Encodes the instant as a four-part Version (year, month, day, seconds within the day). Version.CompareTo
        /// orders these the same way the instants are ordered, which makes the value handy for cache keys, build
        /// numbers and audit labels. Resolution is one second. For concurrency comparisons use ToVersionStamp.
        /// </summary>
        public static Version ToVersion(this DateTime value)
        {
            return new Version(value.Year, value.Month, value.Day, (int)(value.TimeOfDay.Ticks / TicksPerSecond));
        }

        /// <summary>
        /// Reverses <see cref="ToVersion"/>; null when the parts cannot form a valid date (month 13, or a Version
        /// that carries fewer than four parts).
        /// </summary>
        public static DateTime? ToDateTime(this Version version)
        {
            if (version == null || version.Revision < 0 || version.Revision >= 86400)
            {
                return null;
            }

            try
            {
                return new DateTime(version.Major, version.Minor, version.Build)
                    .AddSeconds(version.Revision);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        /// <summary>
        /// The instant as 100-nanosecond ticks: a single long that compares with one operator and keeps full
        /// resolution, which is what an optimistic-lock "newer than mine" check needs. It is still a wall clock,
        /// so keep the comparison on one server - two machines with drifting clocks can order stamps wrongly.
        /// </summary>
        public static long ToVersionStamp(this DateTime value)
        {
            return value.Ticks;
        }

        private static DateTime FromUnixValue(long value, long ticksPerUnit, string unitName)
        {
            // Guard the multiplication first: an out-of-range stamp must fail loudly, not wrap into a valid date.
            if (value < long.MinValue / ticksPerUnit || value > long.MaxValue / ticksPerUnit)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The Unix " + unitName + " value is out of range.");
            }

            long ticks = UnixEpochTicks + value * ticksPerUnit;
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The Unix " + unitName + " value is out of range.");
            }

            return new DateTime(ticks, DateTimeKind.Utc);
        }
    }
}
