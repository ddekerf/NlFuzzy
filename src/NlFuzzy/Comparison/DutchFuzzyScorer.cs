using System;

namespace NlFuzzy.Comparison;

/// <summary>
/// Combines a phonetic component, an orthographic component and an n-gram component into a
/// single normalized similarity score in [0, 1].
/// </summary>
/// <remarks>Instances are immutable once constructed and safe to share between threads.</remarks>
public sealed class DutchFuzzyScorer
{
    /// <summary>
    /// Scale applied to the score of the token-sorted form, so that reordered input ranks just
    /// below identical input.
    /// </summary>
    internal const double ReorderedScoreFactor = 0.97;

    /// <summary>
    /// The shortest word in which <see cref="DutchFuzzyOptions.TranspositionFloor"/> applies. A swap in
    /// a shorter word often spells another name ("Born"/"Bron").
    /// </summary>
    internal const int MinimumTranspositionTokenLength = 5;

    private static readonly char[] TokenSeparator = { ' ' };

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
        var tokens = normalized.Split(TokenSeparator, StringSplitOptions.RemoveEmptyEntries);
        var canonicalWanted = _options.IgnoreTokenOrder && tokens.Length > 1;

        if (_encoder is not Phonetics.DutchPhoneticEncoder dutch)
        {
            var customKey = _encoder.Encode(value);

            if (!canonicalWanted)
            {
                return new PreparedText(normalized, customKey);
            }

            // A custom encoder gives no per-token alignment, so order by spelling instead.
            Array.Sort(tokens, StringComparer.Ordinal);
            var sortedText = string.Join(" ", tokens);

            return WithCanonical(normalized, customKey, sortedText, () => _encoder.Encode(sortedText));
        }

        var keys = dutch.EncodeAlignedTokens(tokens);
        var key = JoinNonEmpty(keys);

        if (!canonicalWanted)
        {
            return new PreparedText(normalized, key);
        }

        // Order tokens by their phonetic key, so spelling variants ("cuypers"/"kuipers") land in
        // the same position; break ties on the spelling to keep the order deterministic.
        var order = new int[tokens.Length];

        for (var i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (left, right) =>
        {
            var byKey = string.CompareOrdinal(keys[left], keys[right]);
            return byKey != 0 ? byKey : string.CompareOrdinal(tokens[left], tokens[right]);
        });

        var canonicalTokens = new string[order.Length];
        var canonicalKeys = new string[order.Length];

        for (var i = 0; i < order.Length; i++)
        {
            canonicalTokens[i] = tokens[order[i]];
            canonicalKeys[i] = keys[order[i]];
        }

        return WithCanonical(normalized, key, string.Join(" ", canonicalTokens), () => JoinNonEmpty(canonicalKeys));
    }

    /// <summary>
    /// Builds a prepared value with a canonical form. When the canonical text equals the original
    /// text, the original strings and bigrams are reused, which also lets <see cref="Score(PreparedText, PreparedText)"/>
    /// recognise that reordering cannot change anything.
    /// </summary>
    private static PreparedText WithCanonical(string normalized, string key, string canonicalText, Func<string> canonicalKey)
    {
        var bigrams = Distance.DiceCoefficientSimilarity.SortedBigrams(normalized);

        if (string.Equals(canonicalText, normalized, StringComparison.Ordinal))
        {
            return new PreparedText(normalized, key, bigrams, normalized, key, bigrams);
        }

        return new PreparedText(
            normalized,
            key,
            bigrams,
            canonicalText,
            canonicalKey(),
            Distance.DiceCoefficientSimilarity.SortedBigrams(canonicalText));
    }

    /// <summary>Scores two values that were prepared by <see cref="Prepare"/>.</summary>
    /// <remarks>
    /// When both values carry a canonical (token-sorted) form, the result is the better of the
    /// score in the original order and the canonical score scaled by
    /// <see cref="ReorderedScoreFactor"/>, so reordered input scores high but stays below identical
    /// input. The canonical score is skipped whenever it provably cannot win, which never changes
    /// the result.
    /// </remarks>
    internal double Score(PreparedText a, PreparedText b)
    {
        var ordered = Score(a.Normalized, a.Key, a.Bigrams, b.Normalized, b.Key, b.Bigrams);

        if (a.CanonicalNormalized is null || b.CanonicalNormalized is null)
        {
            return ordered;
        }

        // A canonical score is at most 1, so after scaling it cannot beat an ordered score that is
        // already at least the scale factor.
        if (ordered >= ReorderedScoreFactor)
        {
            return ordered;
        }

        // When both values are already in canonical order, the canonical score equals the ordered
        // score, and scaling it down can only lose.
        if (ReferenceEquals(a.CanonicalNormalized, a.Normalized) && ReferenceEquals(b.CanonicalNormalized, b.Normalized))
        {
            return ordered;
        }

        var reordered = Score(
                            a.CanonicalNormalized, a.CanonicalKey!, a.CanonicalBigrams!,
                            b.CanonicalNormalized, b.CanonicalKey!, b.CanonicalBigrams!)
                        * ReorderedScoreFactor;

        return Math.Max(ordered, reordered);
    }

    private double Score(string normalizedA, string keyA, int[] bigramsA, string normalizedB, string keyB, int[] bigramsB)
    {
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

        var orthographic = Distance.JaroWinklerSimilarity.Instance.Similarity(normalizedA, normalizedB);
        var phonetic = 1d - Distance.LevenshteinDistance.Normalized(keyA, keyB);
        var ngram = Distance.DiceCoefficientSimilarity.Similarity(normalizedA, bigramsA, normalizedB, bigramsB);

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

            if (_options.TranspositionFloor > 0d && IsSingleAdjacentSwap(normalizedA, normalizedB))
            {
                score = Math.Max(score, _options.TranspositionFloor);
            }
        }

        return MathCompat.Clamp(score, 0d, 1d);
    }

    /// <summary>
    /// True when both texts are identical except for one pair of adjacent letters in swapped order
    /// ("kuipers"/"kuipres"), where neither letter is a space and the word holding them has at least
    /// <see cref="MinimumTranspositionTokenLength"/> characters.
    /// </summary>
    internal static bool IsSingleAdjacentSwap(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var i = 0;

        while (i < a.Length && a[i] == b[i])
        {
            i++;
        }

        if (i >= a.Length - 1)
        {
            return false;
        }

        var first = a[i];
        var second = a[i + 1];

        if (first == ' ' || second == ' ' || b[i] != second || b[i + 1] != first)
        {
            return false;
        }

        for (var j = i + 2; j < a.Length; j++)
        {
            if (a[j] != b[j])
            {
                return false;
            }
        }

        var start = a.LastIndexOf(' ', i) + 1;
        var end = a.IndexOf(' ', i + 2);

        return (end < 0 ? a.Length : end) - start >= MinimumTranspositionTokenLength;
    }

    private static string JoinNonEmpty(string[] keys)
    {
        var count = 0;

        foreach (var key in keys)
        {
            if (key.Length > 0)
            {
                count++;
            }
        }

        if (count == keys.Length)
        {
            return string.Join(" ", keys);
        }

        var nonEmpty = new string[count];
        var index = 0;

        foreach (var key in keys)
        {
            if (key.Length > 0)
            {
                nonEmpty[index++] = key;
            }
        }

        return string.Join(" ", nonEmpty);
    }

    /// <summary>A value that has already been normalized and phonetically encoded.</summary>
    internal readonly struct PreparedText
    {
        internal PreparedText(string normalized, string key)
            : this(normalized, key, Distance.DiceCoefficientSimilarity.SortedBigrams(normalized), null, null, null)
        {
        }

        internal PreparedText(
            string normalized,
            string key,
            int[] bigrams,
            string? canonicalNormalized,
            string? canonicalKey,
            int[]? canonicalBigrams)
        {
            Normalized = normalized;
            Key = key;
            Bigrams = bigrams;
            CanonicalNormalized = canonicalNormalized;
            CanonicalKey = canonicalKey;
            CanonicalBigrams = canonicalBigrams;
        }

        internal string Normalized { get; }

        internal string Key { get; }

        /// <summary>The sorted, packed character bigrams of <see cref="Normalized"/>.</summary>
        internal int[] Bigrams { get; }

        /// <summary>The sorted, packed character bigrams of <see cref="CanonicalNormalized"/>, or null.</summary>
        internal int[]? CanonicalBigrams { get; }

        /// <summary>
        /// The normalized tokens in canonical (phonetic-key) order, or null when token order is
        /// respected or the value has fewer than two tokens.
        /// </summary>
        internal string? CanonicalNormalized { get; }

        /// <summary>The phonetic key matching <see cref="CanonicalNormalized"/>, or null.</summary>
        internal string? CanonicalKey { get; }
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
