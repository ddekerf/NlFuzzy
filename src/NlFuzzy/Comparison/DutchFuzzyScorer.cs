using System;

namespace NlFuzzy.Comparison;

/// <summary>
/// Combines a phonetic component, an orthographic component and an n-gram component into a
/// single normalized similarity score in [0, 1].
/// </summary>
/// <remarks>Instances are immutable once constructed and safe to share between threads.</remarks>
public sealed class DutchFuzzyScorer
{
    private readonly DutchFuzzyOptions _options;
    private readonly Phonetics.IPhoneticEncoder _encoder;
    private readonly Normalization.IDutchTextNormalizer _normalizer;

    /// <summary>A scorer configured with <see cref="DutchFuzzyOptions.Default"/>.</summary>
    public static DutchFuzzyScorer Default { get; } = new(DutchFuzzyOptions.Default);

    /// <summary>Creates a new scorer.</summary>
    /// <param name="options">The weights, thresholds and encoding options.</param>
    /// <param name="encoder">An optional custom phonetic encoder.</param>
    /// <param name="normalizer">An optional custom normalizer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public DutchFuzzyScorer(
        DutchFuzzyOptions options,
        Phonetics.IPhoneticEncoder? encoder = null,
        Normalization.IDutchTextNormalizer? normalizer = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _normalizer = normalizer ?? Normalization.DutchTextNormalizer.Instance;
        _encoder = encoder ?? new Phonetics.DutchPhoneticEncoder(_options, _normalizer);
    }

    /// <summary>The options this scorer was created with.</summary>
    public DutchFuzzyOptions Options => _options;

    /// <summary>The phonetic encoder this scorer uses.</summary>
    public Phonetics.IPhoneticEncoder Encoder => _encoder;

    /// <summary>Scores two strings against each other.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>A similarity in the closed interval [0, 1].</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public double Score(string a, string b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        return Score(Prepare(a), Prepare(b));
    }

    /// <summary>
    /// Normalizes and encodes a value once, so that it can be scored repeatedly without repeating
    /// the (relatively expensive) normalization and encoding work.
    /// </summary>
    internal PreparedText Prepare(string value)
    {
        var normalized = _normalizer.Normalize(value, _options);
        var key = _encoder is Phonetics.DutchPhoneticEncoder dutch
            ? dutch.EncodeNormalized(normalized)
            : _encoder.Encode(value);

        return new PreparedText(normalized, key);
    }

    /// <summary>Scores two values that were prepared by <see cref="Prepare"/>.</summary>
    internal double Score(PreparedText a, PreparedText b)
    {
        var normalizedA = a.Normalized;
        var normalizedB = b.Normalized;

        if (normalizedA.Length == 0 && normalizedB.Length == 0)
        {
            return 1d;
        }

        if (normalizedA.Length == 0 || normalizedB.Length == 0)
        {
            return 0d;
        }

        if (string.Equals(normalizedA, normalizedB, StringComparison.Ordinal))
        {
            return 1d;
        }

        var keyA = a.Key;
        var keyB = b.Key;

        var orthographic = Distance.JaroWinklerSimilarity.Instance.Similarity(normalizedA, normalizedB);
        var phonetic = 1d - Distance.LevenshteinDistance.Normalized(keyA, keyB);
        var ngram = Distance.DiceCoefficientSimilarity.Instance.Similarity(normalizedA, normalizedB);

        var totalWeight = _options.OrthographicWeight + _options.PhoneticWeight + _options.NGramWeight;

        var score = (orthographic * _options.OrthographicWeight
                     + phonetic * _options.PhoneticWeight
                     + ngram * _options.NGramWeight) / totalWeight;

        if (keyA.Length > 0 && string.Equals(keyA, keyB, StringComparison.Ordinal))
        {
            score = Math.Max(score, _options.IdenticalKeyFloor);
        }
        else
        {
            score = ApplyAnchorPenalties(score, keyA, keyB);
        }

        return MathCompat.Clamp(score, 0d, 1d);
    }

    /// <summary>A value that has already been normalized and phonetically encoded.</summary>
    internal readonly struct PreparedText
    {
        internal PreparedText(string normalized, string key)
        {
            Normalized = normalized;
            Key = key;
        }

        internal string Normalized { get; }

        internal string Key { get; }
    }

    private double ApplyAnchorPenalties(double score, string keyA, string keyB)
    {
        if (keyA.Length == 0 || keyB.Length == 0)
        {
            return score;
        }

        if (keyA[0] != keyB[0])
        {
            score *= 1d - _options.InitialPhonemeMismatchPenalty;
        }

        if (keyA[keyA.Length - 1] != keyB[keyB.Length - 1])
        {
            score *= 1d - _options.FinalPhonemeMismatchPenalty;
        }

        return score;
    }
}
