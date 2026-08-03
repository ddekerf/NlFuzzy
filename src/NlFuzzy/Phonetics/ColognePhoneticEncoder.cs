using System;
using System.Collections.Generic;
using System.Text;

namespace NlFuzzy.Phonetics;

/// <summary>
/// The Kölner Phonetik (Cologne phonetics) of Hans Joachim Postel, 1969.
/// </summary>
/// <remarks>
/// This is a German algorithm. It is shipped as a second, independent baseline encoder because
/// German and Dutch share many orthographic patterns, but it is <b>not</b> the recommended default
/// for Dutch: use <see cref="DutchPhoneticEncoder"/> for that.
/// </remarks>
public sealed class ColognePhoneticEncoder : IPhoneticEncoder
{
    private static readonly char[] SpaceSeparator = { ' ' };

    private readonly DutchFuzzyOptions _options;
    private readonly Normalization.IDutchTextNormalizer _normalizer;

    /// <summary>An encoder configured with <see cref="DutchFuzzyOptions.Words"/>.</summary>
    public static ColognePhoneticEncoder Default { get; } = new(DutchFuzzyOptions.Words);

    /// <summary>Creates a new encoder.</summary>
    /// <param name="options">The options used for normalization. Only normalization settings apply.</param>
    /// <param name="normalizer">
    /// An optional custom normalizer. Defaults to <see cref="Normalization.DutchTextNormalizer.Instance"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public ColognePhoneticEncoder(
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
    public IReadOnlyList<string> EncodeTokens(string value)
    {
        var normalized = _normalizer.Normalize(value, _options);

        if (normalized.Length == 0)
        {
            return Array.Empty<string>();
        }

        var parts = normalized.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
        var results = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            var encoded = EncodeWord(part);

            if (encoded.Length > 0)
            {
                results.Add(encoded);
            }
        }

        return results;
    }

    /// <summary>
    /// Encodes a single word into its Cologne code. The input is lower-cased and folded first
    /// (<c>ä</c>, <c>ö</c>, <c>ü</c> and <c>ß</c> are treated as <c>a</c>, <c>o</c>, <c>u</c> and
    /// <c>ss</c>), exactly like <see cref="Encode"/> does.
    /// </summary>
    /// <param name="word">The word to encode.</param>
    /// <returns>The Cologne code, or an empty string when nothing is left after encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="word"/> is null.</exception>
    public static string EncodeWord(string word)
    {
        if (word is null)
        {
            throw new ArgumentNullException(nameof(word));
        }

        word = Normalization.DutchTextNormalizer.FoldForPhonetics(word);

        var raw = new StringBuilder(word.Length + 2);

        for (var i = 0; i < word.Length; i++)
        {
            var current = word[i];
            var previous = i > 0 ? word[i - 1] : '\0';
            var next = i + 1 < word.Length ? word[i + 1] : '\0';

            raw.Append(CodeFor(current, previous, next, i == 0));
        }

        return Compact(raw.ToString());
    }

    private static string CodeFor(char current, char previous, char next, bool atStart)
    {
        switch (current)
        {
            case 'a':
            case 'e':
            case 'i':
            case 'j':
            case 'o':
            case 'u':
            case 'y':
                return "0";
            case 'h':
                return string.Empty;
            case 'b':
                return "1";
            case 'p':
                return next == 'h' ? "3" : "1";
            case 'd':
            case 't':
                return IsAnyOf(next, "csz") ? "8" : "2";
            case 'f':
            case 'v':
            case 'w':
                return "3";
            case 'g':
            case 'k':
            case 'q':
                return "4";
            case 'c':
                if (atStart)
                {
                    return IsAnyOf(next, "ahkloqrux") ? "4" : "8";
                }

                if (IsAnyOf(previous, "sz"))
                {
                    return "8";
                }

                return IsAnyOf(next, "ahkoqux") ? "4" : "8";
            case 'x':
                return IsAnyOf(previous, "ckq") ? "8" : "48";
            case 'l':
                return "5";
            case 'm':
            case 'n':
                return "6";
            case 'r':
                return "7";
            case 's':
            case 'z':
                return "8";
            default:
                return string.Empty;
        }
    }

    private static bool IsAnyOf(char c, string set) => c != '\0' && set.IndexOf(c) >= 0;

    private static string Compact(string codes)
    {
        if (codes.Length == 0)
        {
            return string.Empty;
        }

        var collapsed = new StringBuilder(codes.Length);
        collapsed.Append(codes[0]);

        for (var i = 1; i < codes.Length; i++)
        {
            if (codes[i] != codes[i - 1])
            {
                collapsed.Append(codes[i]);
            }
        }

        var result = new StringBuilder(collapsed.Length);

        for (var i = 0; i < collapsed.Length; i++)
        {
            if (collapsed[i] != '0' || i == 0)
            {
                result.Append(collapsed[i]);
            }
        }

        return result.ToString();
    }
}
