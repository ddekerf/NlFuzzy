using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NlFuzzy;

/// <summary>Entry point for Dutch phonetic and fuzzy string comparison.</summary>
/// <remarks>
/// All members are thread-safe. Encoders and scorers are cached per options instance, so calling
/// these methods in a tight loop does not allocate a new encoder every time.
/// </remarks>
public static class DutchFuzzy
{
    private const int MaxCachedOptions = 64;

    private static readonly ConcurrentDictionary<DutchFuzzyOptions, Phonetics.DutchPhoneticEncoder> Encoders = new();
    private static readonly ConcurrentDictionary<DutchFuzzyOptions, Comparison.DutchFuzzyScorer> Scorers = new();

    /// <summary>Returns the Dutch phonetic key of <paramref name="value"/>.</summary>
    /// <param name="value">The text to encode. May be null or empty.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The phonetic key; an empty string when there is nothing to encode.</returns>
    public static string PhoneticKey(string value, DutchFuzzyOptions? options = null) =>
        GetEncoder(options).Encode(value);

    /// <summary>Returns one Dutch phonetic key per whitespace-separated token.</summary>
    /// <param name="value">The text to encode. May be null or empty.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The keys in token order; an empty list when there is nothing to encode.</returns>
    public static IReadOnlyList<string> PhoneticKeys(string value, DutchFuzzyOptions? options = null) =>
        GetEncoder(options).EncodeTokens(value);

    /// <summary>True when both strings produce exactly the same Dutch phonetic key.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>True when the keys are equal and non-empty.</returns>
    public static bool SoundsLike(string a, string b, DutchFuzzyOptions? options = null)
    {
        var encoder = GetEncoder(options);
        var keyA = encoder.Encode(a);
        var keyB = encoder.Encode(b);

        return keyA.Length > 0 && string.Equals(keyA, keyB, StringComparison.Ordinal);
    }

    /// <summary>Combined phonetic, orthographic and n-gram similarity in [0, 1].</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The similarity score.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static double Similarity(string a, string b, DutchFuzzyOptions? options = null) =>
        GetScorer(options).Score(a, b);

    /// <summary>True when <see cref="Similarity"/> reaches <paramref name="threshold"/>.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <param name="threshold">
    /// The minimum score. Defaults to the <see cref="DutchFuzzyOptions.DefaultThreshold"/> of
    /// <paramref name="options"/> (0.82 for the default options).
    /// </param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>True when the score is greater than or equal to the threshold.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is NaN, infinite or outside [0, 1].
    /// </exception>
    public static bool AreEquivalent(string a, string b, double? threshold = null, DutchFuzzyOptions? options = null)
    {
        var minimumScore = Thresholds.Resolve(threshold, options, nameof(threshold));

        return Similarity(a, b, options) >= minimumScore;
    }

    internal static Phonetics.DutchPhoneticEncoder GetEncoder(DutchFuzzyOptions? options)
    {
        var effective = options ?? DutchFuzzyOptions.Default;

        if (Encoders.TryGetValue(effective, out var cached))
        {
            return cached;
        }

        var encoder = new Phonetics.DutchPhoneticEncoder(effective);

        if (Encoders.Count < MaxCachedOptions)
        {
            Encoders.TryAdd(effective, encoder);
        }

        return encoder;
    }

    internal static Comparison.DutchFuzzyScorer GetScorer(DutchFuzzyOptions? options)
    {
        var effective = options ?? DutchFuzzyOptions.Default;

        if (Scorers.TryGetValue(effective, out var cached))
        {
            return cached;
        }

        var scorer = new Comparison.DutchFuzzyScorer(effective, GetEncoder(effective));

        if (Scorers.Count < MaxCachedOptions)
        {
            Scorers.TryAdd(effective, scorer);
        }

        return scorer;
    }
}
