using System;

namespace NlFuzzy;

/// <summary>Immutable configuration for all Dutch fuzzy matching operations.</summary>
/// <remarks>
/// Instances are immutable and thread-safe. Use <c>with</c> expressions to derive variants:
/// <c>DutchFuzzyOptions.Default with { Profile = DutchPhoneticProfile.Strict }</c>.
/// </remarks>
public sealed record DutchFuzzyOptions
{
    /// <summary>The default options: Tolerant profile, name-oriented normalization.</summary>
    public static DutchFuzzyOptions Default { get; } = new();

    /// <summary>
    /// Options tuned for matching Dutch person names, including Flemish glued prefixes
    /// ("Vandenbroecke" = "Van den Broecke") and vowel-class encoding.
    /// This is the preset the golden dataset is validated against.
    /// </summary>
    public static DutchFuzzyOptions Names { get; } = new()
    {
        Profile = DutchPhoneticProfile.Tolerant,
        StripNamePrefixes = true,
        SplitGluedNamePrefixes = true,
        EncodeVowels = true,
        MergeSeSenEndings = true,
        TranspositionFloor = 0.85
    };

    /// <summary>Options tuned for comparing ordinary Dutch words rather than proper names.</summary>
    public static DutchFuzzyOptions Words { get; } = new()
    {
        Profile = DutchPhoneticProfile.Strict,
        StripNamePrefixes = false,
        SplitGluedNamePrefixes = false,
        EncodeVowels = true,
        IgnoreTokenOrder = false
    };

    /// <summary>Phonetic profile. Default: <see cref="DutchPhoneticProfile.Tolerant"/>.</summary>
    public DutchPhoneticProfile Profile { get; init; } = DutchPhoneticProfile.Tolerant;

    /// <summary>
    /// Fold accented letters onto their base letter during normalization ("Céline" becomes "celine").
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// When false, accented Latin letters stay in the normalized text ("céline"), so the spelling
    /// components of the score see the accent: "Céline" and "Celine" score at least
    /// <see cref="IdenticalKeyFloor"/> but below 1.0. The phonetic key always folds accents, so
    /// <c>PhoneticKey</c>, <c>SoundsLike</c>, the equality comparer and index blocking do not depend
    /// on this option, and name particles are recognized with or without accents ("dé" is "de").
    /// Only letters from U+00C0 to U+024F that decompose to a-z plus combining marks are kept; every
    /// other character is handled exactly as when this option is true.
    /// </remarks>
    public bool RemoveDiacritics { get; init; } = true;

    /// <summary>
    /// Move Dutch name prefixes (van, van der, de, ten, ...) to the end of the string so that
    /// "Van den Broeke" and "Broeke, van den" normalize identically. Default: true.
    /// </summary>
    public bool StripNamePrefixes { get; init; } = true;

    /// <summary>
    /// Also split glued Flemish prefixes ("Vandenbroecke" -&gt; "van den broecke",
    /// "Verbrugghe" -&gt; "van der brugghe"). Increases recall, decreases precision. Default: false.
    /// </summary>
    public bool SplitGluedNamePrefixes { get; init; }

    /// <summary>
    /// Encode vowel classes instead of dropping all non-initial vowels. Default: false.
    /// True gives longer, more selective keys.
    /// </summary>
    public bool EncodeVowels { get; init; }

    /// <summary>
    /// Let the similarity score tolerate a different token order, so that "Jan Bakker" and
    /// "Bakker, Jan" score as a near match. Identical input still scores higher than reordered
    /// input. Phonetic keys are not affected. Default: true (false in <see cref="Words"/>).
    /// </summary>
    public bool IgnoreTokenOrder { get; init; } = true;

    /// <summary>
    /// Encode a token of five or more letters that ends in "-sen" or "-zen" as if it ended in "-se" or
    /// "-ze", so that the two forms of a patronymic share a key ("Willemsen" = "Willemse"). The "-s"
    /// form ("Willems") keeps its own key. Only meaningful with <see cref="EncodeVowels"/>, which is
    /// why it is only on in <see cref="Names"/>. Default: false.
    /// </summary>
    public bool MergeSeSenEndings { get; init; }

    /// <summary>Maximum length of the produced phonetic key. 0 = unlimited. Default: 0.</summary>
    public int MaxKeyLength { get; init; }

    /// <summary>Weight of the orthographic Jaro-Winkler component. Default: 0.35.</summary>
    public double OrthographicWeight { get; init; } = 0.35;

    /// <summary>Weight of the phonetic edit distance component. Default: 0.45.</summary>
    public double PhoneticWeight { get; init; } = 0.45;

    /// <summary>Weight of the bigram Dice component. Default: 0.20.</summary>
    public double NGramWeight { get; init; } = 0.20;

    /// <summary>Score floor applied when both phonetic keys are identical. Default: 0.90.</summary>
    public double IdenticalKeyFloor { get; init; } = 0.90;

    /// <summary>
    /// Score floor applied when the two normalized values are identical except for one pair of
    /// adjacent letters in swapped order, inside a word of five or more letters ("Kuipers" versus
    /// "Kuipres"): a typical typing error. Shorter words are left alone, because a swap there often
    /// spells another name ("Born" versus "Bron"). 0 disables the floor. Default: 0 (0.85 in
    /// <see cref="Names"/>).
    /// </summary>
    public double TranspositionFloor { get; init; }

    /// <summary>
    /// Threshold used by <c>DutchFuzzyEquals</c>, the ranking helpers and
    /// <see cref="Indexing.DutchFuzzyIndex{T}"/> whenever the caller omits the threshold argument.
    /// Default: 0.82.
    /// </summary>
    public double DefaultThreshold { get; init; } = DutchStringExtensions.DefaultThreshold;

    /// <summary>
    /// Relative score penalty applied when the first phoneme of both keys differs. Default: 0.20.
    /// Dutch names that start with a different sound are rarely the same name
    /// ("Jansen" versus "Hansen"), while classic edit-distance measures barely notice the difference.
    /// Set to 0 to disable.
    /// </summary>
    public double InitialPhonemeMismatchPenalty { get; init; } = 0.20;

    /// <summary>
    /// Relative score penalty applied when the last phoneme of both keys differs. Default: 0.20.
    /// Separates singular/plural and derivation pairs such as "Kuipers" versus "Kuiper".
    /// Set to 0 to disable.
    /// </summary>
    public double FinalPhonemeMismatchPenalty { get; init; } = 0.20;

    /// <summary>Validates that weights are non-negative and sum to a positive value.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a weight is negative or not finite, when the weights do not sum to a finite
    /// positive value, when a penalty falls outside [0, 1), when a threshold or floor falls outside
    /// [0, 1], when <see cref="Profile"/> is not a defined enum value, or when
    /// <see cref="MaxKeyLength"/> is negative.
    /// </exception>
    public void Validate()
    {
        if (!EnumCompat.IsDefined(Profile))
        {
            throw new InvalidOperationException(
                "Profile must be a defined DutchPhoneticProfile value (Strict or Tolerant).");
        }

        EnsureNonNegative(OrthographicWeight, nameof(OrthographicWeight));
        EnsureNonNegative(PhoneticWeight, nameof(PhoneticWeight));
        EnsureNonNegative(NGramWeight, nameof(NGramWeight));

        var total = OrthographicWeight + PhoneticWeight + NGramWeight;

        if (!MathCompat.IsFinite(total) || total <= 0d)
        {
            throw new InvalidOperationException(
                "The sum of the similarity weights must be a finite number greater than zero.");
        }

        EnsureUnitInterval(IdenticalKeyFloor, nameof(IdenticalKeyFloor));
        EnsureUnitInterval(TranspositionFloor, nameof(TranspositionFloor));
        EnsureUnitInterval(DefaultThreshold, nameof(DefaultThreshold));
        EnsureFraction(InitialPhonemeMismatchPenalty, nameof(InitialPhonemeMismatchPenalty));
        EnsureFraction(FinalPhonemeMismatchPenalty, nameof(FinalPhonemeMismatchPenalty));

        if (MaxKeyLength < 0)
        {
            throw new InvalidOperationException("MaxKeyLength must be zero (unlimited) or a positive number.");
        }
    }

    private static void EnsureNonNegative(double value, string name)
    {
        if (!MathCompat.IsFinite(value) || value < 0d)
        {
            throw new InvalidOperationException(name + " must be a finite, non-negative number.");
        }
    }

    private static void EnsureUnitInterval(double value, string name)
    {
        if (!MathCompat.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new InvalidOperationException(name + " must lie in the closed interval [0, 1].");
        }
    }

    private static void EnsureFraction(double value, string name)
    {
        if (!MathCompat.IsFinite(value) || value < 0d || value >= 1d)
        {
            throw new InvalidOperationException(name + " must lie in the half-open interval [0, 1).");
        }
    }
}
