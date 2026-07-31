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
        EncodeVowels = true
    };

    /// <summary>Options tuned for comparing ordinary Dutch words rather than proper names.</summary>
    public static DutchFuzzyOptions Words { get; } = new()
    {
        Profile = DutchPhoneticProfile.Strict,
        StripNamePrefixes = false,
        SplitGluedNamePrefixes = false,
        EncodeVowels = true
    };

    /// <summary>Phonetic profile. Default: <see cref="DutchPhoneticProfile.Tolerant"/>.</summary>
    public DutchPhoneticProfile Profile { get; init; } = DutchPhoneticProfile.Tolerant;

    /// <summary>Remove diacritics before encoding. Default: true.</summary>
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

    /// <summary>Default threshold used by <c>DutchFuzzyEquals</c> and search helpers. Default: 0.82.</summary>
    public double DefaultThreshold { get; init; } = 0.82;

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
    /// Thrown when a weight is negative, when all weights are zero, when a penalty falls outside
    /// [0, 1), when a threshold or floor falls outside [0, 1], or when <see cref="MaxKeyLength"/> is negative.
    /// </exception>
    public void Validate()
    {
        EnsureNonNegative(OrthographicWeight, nameof(OrthographicWeight));
        EnsureNonNegative(PhoneticWeight, nameof(PhoneticWeight));
        EnsureNonNegative(NGramWeight, nameof(NGramWeight));

        var total = OrthographicWeight + PhoneticWeight + NGramWeight;

        if (total <= 0d)
        {
            throw new InvalidOperationException("The sum of the similarity weights must be greater than zero.");
        }

        EnsureUnitInterval(IdenticalKeyFloor, nameof(IdenticalKeyFloor));
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
        if (double.IsNaN(value) || value < 0d)
        {
            throw new InvalidOperationException(name + " must be a non-negative number.");
        }
    }

    private static void EnsureUnitInterval(double value, string name)
    {
        if (double.IsNaN(value) || value < 0d || value > 1d)
        {
            throw new InvalidOperationException(name + " must lie in the closed interval [0, 1].");
        }
    }

    private static void EnsureFraction(double value, string name)
    {
        if (double.IsNaN(value) || value < 0d || value >= 1d)
        {
            throw new InvalidOperationException(name + " must lie in the half-open interval [0, 1).");
        }
    }
}
