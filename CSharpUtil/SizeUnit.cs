namespace CSharpUtil.NumberUtil
{
    /// <summary>
    /// Binary size units (1 KB = 1024 bytes), ordered by magnitude. The numeric value is the power of 1024,
    /// so the divisor for a unit is <c>1L &lt;&lt; (10 * (int)unit)</c>.
    /// The symbols follow the Windows convention: "KB" means 1024 bytes, not 1000 as in strict SI / IEC naming.
    /// </summary>
    public enum SizeUnit
    {
        /// <summary>Bytes (1024^0).</summary>
        B = 0,

        /// <summary>Kilobytes (1024^1).</summary>
        KB = 1,

        /// <summary>Megabytes (1024^2).</summary>
        MB = 2,

        /// <summary>Gigabytes (1024^3).</summary>
        GB = 3,

        /// <summary>Terabytes (1024^4).</summary>
        TB = 4,

        /// <summary>Petabytes (1024^5).</summary>
        PB = 5
    }
}
