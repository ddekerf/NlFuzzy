using System;
using System.Collections.Generic;
using System.Text;

namespace NlFuzzy.Phonetics;

/// <summary>
/// Rule-based Dutch phonetic encoder ("Dutch Metaphone"): converts Dutch spelling into a
/// sounds-like key so that "Bruijn", "Bruyn" and "Bruin" collapse onto the same value.
/// </summary>
/// <remarks>
/// The encoder is stateless once constructed and therefore safe to share between threads.
/// </remarks>
public sealed class DutchPhoneticEncoder : IPhoneticEncoder
{
    private static readonly char[] SpaceSeparator = { ' ' };

    private readonly DutchFuzzyOptions _options;
    private readonly Normalization.IDutchTextNormalizer _normalizer;

    /// <summary>An encoder configured with <see cref="DutchFuzzyOptions.Default"/>.</summary>
    public static DutchPhoneticEncoder Default { get; } = new(DutchFuzzyOptions.Default);

    /// <summary>Creates a new encoder.</summary>
    /// <param name="options">The options that drive normalization and encoding.</param>
    /// <param name="normalizer">
    /// An optional custom normalizer. Defaults to <see cref="Normalization.DutchTextNormalizer.Instance"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public DutchPhoneticEncoder(
        DutchFuzzyOptions options,
        Normalization.IDutchTextNormalizer? normalizer = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _normalizer = normalizer ?? Normalization.DutchTextNormalizer.Instance;
    }

    /// <inheritdoc />
    public string Encode(string value)
    {
        var tokens = EncodeTokens(value);

        return tokens.Count == 0 ? string.Empty : string.Join(" ", tokens);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> EncodeTokens(string value) => EncodeTokens(value, null);

    internal IReadOnlyList<string> EncodeTokens(string value, ICollection<int>? usedRuleIds)
    {
        var normalized = _normalizer.Normalize(value, _options);

        return EncodeNormalizedTokens(normalized, usedRuleIds);
    }

    /// <summary>
    /// Encodes text that has already been normalized with the same options, skipping the
    /// (relatively expensive) normalization step.
    /// </summary>
    internal IReadOnlyList<string> EncodeNormalizedTokens(string normalized, ICollection<int>? usedRuleIds)
    {
        if (normalized.Length == 0)
        {
            return Array.Empty<string>();
        }

        var parts = normalized.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
        var results = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            var encoded = EncodeToken(part, usedRuleIds);

            if (encoded.Length > 0)
            {
                results.Add(encoded);
            }
        }

        return results;
    }

    internal string EncodeNormalized(string normalized)
    {
        var tokens = EncodeNormalizedTokens(normalized, null);

        return tokens.Count == 0 ? string.Empty : string.Join(" ", tokens);
    }

    private string EncodeToken(string token, ICollection<int>? usedRuleIds)
    {
        var builder = new StringBuilder(token.Length + 4);
        var index = 0;

        while (index < token.Length)
        {
            var rule = DutchPhoneticRules.FindMatch(token, index, _options.Profile);

            if (rule is null)
            {
                index++;
                continue;
            }

            usedRuleIds?.Add(rule.Id);
            builder.Append(rule.OutputFor(_options.Profile));
            index += rule.Length;
        }

        return PostProcess(builder.ToString());
    }

    private string PostProcess(string raw)
    {
        var collapsed = CollapseRepeats(raw);
        var vowelHandled = _options.EncodeVowels ? collapsed : DropNonInitialVowels(collapsed);

        if (_options.MaxKeyLength > 0 && vowelHandled.Length > _options.MaxKeyLength)
        {
            return vowelHandled.Substring(0, _options.MaxKeyLength);
        }

        return vowelHandled;
    }

    private static string CollapseRepeats(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        builder.Append(value[0]);

        for (var i = 1; i < value.Length; i++)
        {
            if (value[i] != value[i - 1])
            {
                builder.Append(value[i]);
            }
        }

        return builder.ToString();
    }

    private static string DropNonInitialVowels(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var first = value[0];

        builder.Append(IsVowelClass(first) ? 'A' : first);

        for (var i = 1; i < value.Length; i++)
        {
            if (!IsVowelClass(value[i]))
            {
                builder.Append(value[i]);
            }
        }

        return builder.ToString();
    }

    /// <summary>True when <paramref name="c"/> is one of the vowel class digits 1..7.</summary>
    /// <param name="c">The character to inspect.</param>
    /// <returns>True for '1' through '7'.</returns>
    public static bool IsVowelClass(char c) => c >= '1' && c <= '7';
}
