namespace NlFuzzy.Normalization;

/// <summary>Normalizes raw input into the canonical lower-case form the encoders expect.</summary>
public interface IDutchTextNormalizer
{
    /// <summary>Normalizes <paramref name="value"/> according to <paramref name="options"/>.</summary>
    /// <param name="value">The raw input. May be null or empty.</param>
    /// <param name="options">The options that control diacritics and name-prefix handling.</param>
    /// <returns>A lower-case string containing only 'a'-'z' and single spaces; never null.</returns>
    string Normalize(string value, DutchFuzzyOptions options);
}
