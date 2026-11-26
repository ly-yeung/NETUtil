using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace CSharpUtil.EnumUtil
{
    /// <summary>
    /// Enum presentation helpers. The reflection cost is paid once per (enum type, value) pair.
    /// </summary>
    public static class EnumUtil
    {
        /// <summary>
        /// Returns the [Description] text of the matching member, or the member name when the attribute is
        /// missing or the value is not a defined member. Results are cached per enum type, so the field lookup
        /// and attribute read happen only on the first call for each value.
        /// </summary>
        public static string GetDescription<T>(this T value) where T : struct, Enum
        {
            return Descriptions<T>.Values.GetOrAdd(value, Resolve);
        }

        private static string Resolve<T>(T value) where T : struct, Enum
        {
            string name = Enum.GetName(typeof(T), value);
            if (name == null)
            {
                // Flags combinations and out-of-range values have no member, so there is no attribute to read.
                return value.ToString();
            }

            FieldInfo field = typeof(T).GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (field == null)
            {
                return name;
            }

            DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute == null ? name : attribute.Description;
        }

        /// <summary>
        /// One cache per closed generic type keeps keys strongly typed, which avoids boxing on every lookup.
        /// </summary>
        private static class Descriptions<T> where T : struct, Enum
        {
            internal static readonly ConcurrentDictionary<T, string> Values = new ConcurrentDictionary<T, string>();
        }
    }
}
