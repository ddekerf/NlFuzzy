using System;
using System.Globalization;
using System.Text;

namespace NlFuzzy.Normalization;

/// <summary>
/// The default Dutch text normalizer: folds diacritics, expands Dutch and Germanic ligatures,
/// lower-cases, strips punctuation and optionally rewrites Dutch name particles.
/// </summary>
/// <remarks>This type is stateless and therefore thread-safe.</remarks>
public sealed class DutchTextNormalizer : IDutchTextNormalizer
{
    /// <summary>The shared, stateless instance.</summary>
    public static DutchTextNormalizer Instance { get; } = new();

    /// <inheritdoc />
    public string Normalize(string value, DutchFuzzyOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var folded = ExpandElidedArticles(FoldForPhonetics(value, options.RemoveDiacritics));
        var cleaned = KeepLettersAndSpaces(folded);

        if (cleaned.Length == 0)
        {
            return string.Empty;
        }

        if (options.SplitGluedNamePrefixes)
        {
            cleaned = DutchAffixes.SplitGluedPrefixes(cleaned);
        }

        if (options.StripNamePrefixes)
        {
            cleaned = DutchAffixes.MovePrefixesToEnd(cleaned, options.SplitGluedNamePrefixes);
        }

        return cleaned;
    }

    /// <summary>
    /// Lower-cases, optionally folds diacritics and expands the Dutch and Germanic special
    /// characters, in the only order that is safe: characters that appear because of lower-casing
    /// or Unicode decomposition (<c>ẞ</c> -&gt; <c>ß</c>, <c>ǽ</c> -&gt; <c>æ</c>) are still expanded
    /// instead of being dropped by the later letter filter.
    /// </summary>
    /// <param name="value">The text to fold.</param>
    /// <param name="removeDiacritics">True to strip combining marks.</param>
    /// <returns>The folded text.</returns>
    internal static string FoldForPhonetics(string value, bool removeDiacritics = true)
    {
        var lowered = value.ToLowerInvariant();
        var folded = removeDiacritics ? RemoveDiacritics(lowered) : lowered;

        return ExpandSpecialCharacters(folded);
    }

    /// <summary>
    /// Rewrites the elided article <c>d'</c> ("D'Hondt") to <c>de </c>, so that it becomes the
    /// particle "de" instead of a lone "d". This must run while the apostrophe still exists, because
    /// the apostrophe is the only thing that tells the article apart from an initial ("D. Jansen").
    /// The <c>d</c> must start a token, and at least three letters must follow the apostrophe, so
    /// that colloquial "d'r" is left alone.
    /// </summary>
    /// <param name="value">Lower-cased, folded text.</param>
    /// <returns>The rewritten text, or <paramref name="value"/> itself when nothing changes.</returns>
    internal static string ExpandElidedArticles(string value)
    {
        StringBuilder? builder = null;
        var copiedUpTo = 0;

        for (var i = 0; i + 1 < value.Length; i++)
        {
            if (value[i] != 'd' || !IsApostrophe(value[i + 1]))
            {
                continue;
            }

            if (i > 0 && char.IsLetter(value[i - 1]))
            {
                continue;
            }

            if (CountLetters(value, i + 2) < 3)
            {
                continue;
            }

            builder ??= new StringBuilder(value.Length + 4);
            builder.Append(value, copiedUpTo, i - copiedUpTo).Append("de ");
            copiedUpTo = i + 2;
            i++;
        }

        if (builder is null)
        {
            return value;
        }

        builder.Append(value, copiedUpTo, value.Length - copiedUpTo);

        return builder.ToString();
    }

    private static bool IsApostrophe(char c) =>
        c is '\'' or '’' or '‘' or '`' or '´';

    private static int CountLetters(string value, int start)
    {
        var count = 0;

        while (start + count < value.Length && value[start + count] >= 'a' && value[start + count] <= 'z')
        {
            count++;
        }

        return count;
    }

    private static string ExpandSpecialCharacters(string value)
    {
        if (!NeedsExpansion(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 4);

        foreach (var c in value)
        {
            switch (c)
            {
                case 'ß':
                case 'ẞ':
                    builder.Append("ss");
                    break;
                case 'æ':
                case 'Æ':
                    builder.Append("ae");
                    break;
                case 'œ':
                case 'Œ':
                    builder.Append("oe");
                    break;
                case 'ø':
                case 'Ø':
                    builder.Append('o');
                    break;
                case 'đ':
                case 'Đ':
                    builder.Append('d');
                    break;
                case 'ĳ':
                case 'Ĳ':
                    builder.Append("ij");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    private static bool NeedsExpansion(string value)
    {
        foreach (var c in value)
        {
            if (c > '\u007f')
            {
                return true;
            }
        }

        return false;
    }

    private static string RemoveDiacritics(string value)
    {
        if (IsPlainAscii(value))
        {
            return value;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsPlainAscii(string value)
    {
        foreach (var c in value)
        {
            if (c > '\u007f')
            {
                return false;
            }
        }

        return true;
    }

    private static string KeepLettersAndSpaces(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasSpace = true;

        foreach (var c in value)
        {
            if (c >= 'a' && c <= 'z')
            {
                builder.Append(c);
                lastWasSpace = false;
                continue;
            }

            if (!lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }
}
