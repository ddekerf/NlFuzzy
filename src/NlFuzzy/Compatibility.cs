using System;

#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    /// <summary>Enables <c>init</c> accessors and records on netstandard2.0.</summary>
    internal static class IsExternalInit
    {
    }
}
#endif

namespace NlFuzzy
{
    /// <summary>
    /// Small helpers that keep the library free of runtime dependencies on every target framework.
    /// </summary>
    internal static class MathCompat
    {
        /// <summary>Clamps <paramref name="value"/> into the closed interval [min, max].</summary>
        internal static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        /// <summary>True when <paramref name="value"/> is neither NaN nor an infinity.</summary>
        internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>Enum helpers that work on every target framework.</summary>
    internal static class EnumCompat
    {
        /// <summary>True when <paramref name="value"/> is a declared member of <typeparamref name="TEnum"/>.</summary>
        internal static bool IsDefined<TEnum>(TEnum value)
            where TEnum : struct, Enum =>
#if NETSTANDARD2_0
            Enum.IsDefined(typeof(TEnum), value);
#else
            Enum.IsDefined(value);
#endif
    }
}
