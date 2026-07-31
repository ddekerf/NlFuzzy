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

        var expanded = ExpandSpecialCharacters(value);
        var folded = options.RemoveDiacritics ? RemoveDiacritics(expanded) : expanded;
        var lowered = folded.ToLowerInvariant();
        var cleaned = KeepLettersAndSpaces(lowered);

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

    private static string ExpandSpecialCharacters(string value)
    {
        var builder = new StringBuilder(value.Length + 4);

        foreach (var c in value)
        {
            switch (c)
            {
                case 'ß':
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
