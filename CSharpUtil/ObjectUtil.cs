using System.Collections.Generic;

namespace CSharpUtil.ObjectUtil
{
    /// <summary>
    /// ObjectUtil provides utility methods for object manipulation and checks.
    /// </summary>
    public static class ObjectUtil
    {
        /// <summary>
        /// Determines if an object is null.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNull(this object obj)
        {
            return obj == null;
        }

        /// <summary>
        /// Determines if an object is not null.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNotNull(this object obj)
        {
            return obj != null;
        }

        /// <summary>
        /// Casts the object to T. Despite the name, this is a plain cast: it does not copy, convert or clone
        /// anything, and throws InvalidCastException when the runtime type does not match T (or
        /// NullReferenceException when the receiver is null and T is a value type).
        /// Use <see cref="As{T}"/> when a failed cast should yield null instead of throwing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T CopyTo<T>(this object obj)
        {
            return (T)obj;
        }

        /// <summary>
        /// Casts the object to T, returning null instead of throwing when the runtime type does not match.
        /// T is constrained to reference types so the null result is always expressible.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T As<T>(this object obj) where T : class
        {
            return obj as T;
        }

        /// <summary>
        /// Determines whether the value equals the default value of T: null for reference types,
        /// all-zero (or the struct's own default) for value types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsDefault<T>(this T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default);
        }

        /// <summary>
        /// Returns the value when it is not null, otherwise <paramref name="fallback"/>. Restricted to
        /// reference types so the result is always a valid T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static T Coalesce<T>(this T value, T fallback) where T : class
        {
            return value ?? fallback;
        }
    }
}
