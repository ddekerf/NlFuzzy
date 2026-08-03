using System;

namespace NlFuzzy;

/// <summary>Convenience extension methods for Dutch phonetic and fuzzy comparison of strings.</summary>
public static class DutchStringExtensions
{
    /// <summary>The similarity threshold used when neither the caller nor the options specify one.</summary>
    /// <remarks>
    /// This is the same value as <see cref="DutchFuzzyOptions.DefaultThreshold"/> on
    /// <see cref="DutchFuzzyOptions.Default"/>. Supply custom options to change the effective default.
    /// </remarks>
    public const double DefaultThreshold = 0.82;

    /// <summary>Returns the Dutch phonetic key for this string.</summary>
    /// <param name="value">The string to encode. May be null or empty.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The phonetic key; an empty string when there is nothing to encode.</returns>
    public static string ToDutchPhoneticKey(this string value, DutchFuzzyOptions? options = null) =>
        DutchFuzzy.PhoneticKey(value, options);

    /// <summary>True when both strings produce the same Dutch phonetic key.</summary>
    /// <param name="value">The first string.</param>
    /// <param name="other">The second string.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>True when the keys are equal and non-empty.</returns>
    public static bool SoundsLikeDutch(this string value, string other, DutchFuzzyOptions? options = null) =>
        DutchFuzzy.SoundsLike(value, other, options);

    /// <summary>Combined phonetic + orthographic similarity in [0, 1].</summary>
    /// <param name="value">The first string.</param>
    /// <param name="other">The second string.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The similarity score.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static double DutchSimilarity(this string value, string other, DutchFuzzyOptions? options = null) =>
        DutchFuzzy.Similarity(value, other, options);

    /// <summary>True when <see cref="DutchSimilarity"/> is greater than or equal to the threshold.</summary>
    /// <param name="value">The first string.</param>
    /// <param name="other">The second string.</param>
    /// <param name="threshold">
    /// The minimum score. Defaults to the <see cref="DutchFuzzyOptions.DefaultThreshold"/> of
    /// <paramref name="options"/> (0.82 for the default options).
    /// </param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>True when the score reaches the threshold.</returns>
    /// <remarks>
    /// This relation is not transitive, which is why no <c>IEqualityComparer&lt;string&gt;</c> is
    /// offered for it. Use <see cref="Comparison.DutchPhoneticEqualityComparer"/> when you need a
    /// comparer for a dictionary or a set.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is NaN, infinite or outside [0, 1].
    /// </exception>
    public static bool DutchFuzzyEquals(
        this string value,
        string other,
        double? threshold = null,
        DutchFuzzyOptions? options = null) =>
        DutchFuzzy.AreEquivalent(value, other, threshold, options);
}
