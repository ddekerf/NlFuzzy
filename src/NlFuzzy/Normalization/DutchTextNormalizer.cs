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

        var folded = FoldForPhonetics(value, options.RemoveDiacritics);

        // The elided article is a name particle, so it follows the name-particle switch:
        // "D'Hondt" is "De Hondt", but ordinary words such as "d'accord" stay as they are.
        var keepAccents = !options.RemoveDiacritics;

        if (options.StripNamePrefixes)
        {
            folded = ExpandElidedArticles(folded, keepAccents);
        }

        var cleaned = KeepLettersAndSpaces(folded, keepAccents);

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
        var folded = removeDiacritics ? RemoveDiacritics(lowered) : ComposeAccents(lowered);

        return ExpandSpecialCharacters(folded);
    }

    /// <summary>The first character covered by <see cref="KeptAccentedLetters"/>.</summary>
    private const char FirstAccentedLetter = 'À';

    /// <summary>The last character covered by <see cref="KeptAccentedLetters"/>.</summary>
    private const char LastAccentedLetter = 'ɏ';

    /// <summary>
    /// For every character from U+00C0 to U+024F (Latin-1 Supplement and Latin Extended-A/B): its
    /// lower-case base letter a-z when it decomposes to that letter followed only by combining marks,
    /// otherwise '\0'. Built once.
    /// </summary>
    private static readonly char[] KeptAccentedLetters = BuildKeptAccentedLetters();

    /// <summary>
    /// True for an accented Latin letter that the normalizer keeps when diacritic folding is off:
    /// a character from U+00C0 to U+024F whose canonical decomposition is one letter a-z (either case)
    /// followed only by combining marks ("é", "ü", "ç", "ñ", "å"). Letters without such a
    /// decomposition ("ł", "ø") and characters outside that range are not kept.
    /// </summary>
    internal static bool IsKeptAccentedLetter(char c) => BaseLetter(c) != '\0';

    /// <summary>
    /// Maps a kept accented letter to its lower-case base letter ("é" to "e"), and returns every other
    /// character unchanged. The mapping is one to one, so positions in a token stay aligned.
    /// </summary>
    internal static char FoldKeptAccent(char c)
    {
        var baseLetter = BaseLetter(c);

        return baseLetter == '\0' ? c : baseLetter;
    }

    /// <summary>
    /// <see cref="FoldKeptAccent"/> applied to every character, so "dé" becomes "de". Returns the
    /// value itself when it has nothing to fold. Name-particle checks use this, so they decide the
    /// same way whether or not diacritics were folded.
    /// </summary>
    internal static string FoldKeptAccents(string value)
    {
        char[]? folded = null;

        for (var i = 0; i < value.Length; i++)
        {
            var baseLetter = BaseLetter(value[i]);

            if (baseLetter != '\0')
            {
                folded ??= value.ToCharArray();
                folded[i] = baseLetter;
            }
        }

        return folded is null ? value : new string(folded);
    }

    private static char BaseLetter(char c) =>
        c >= FirstAccentedLetter && c <= LastAccentedLetter ? KeptAccentedLetters[c - FirstAccentedLetter] : '\0';

    /// <summary>The rule behind <see cref="IsKeptAccentedLetter"/>, computed without the table.</summary>
    internal static bool DecomposesToAccentedLatinLetter(char c)
    {
        var decomposed = c.ToString().Normalize(NormalizationForm.FormD);

        if (decomposed.Length < 2)
        {
            return false;
        }

        var baseLetter = char.ToLowerInvariant(decomposed[0]);

        if (baseLetter < 'a' || baseLetter > 'z')
        {
            return false;
        }

        for (var i = 1; i < decomposed.Length; i++)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(decomposed[i]) != UnicodeCategory.NonSpacingMark)
            {
                return false;
            }
        }

        return true;
    }

    private static char[] BuildKeptAccentedLetters()
    {
        var table = new char[LastAccentedLetter - FirstAccentedLetter + 1];

        for (var i = 0; i < table.Length; i++)
        {
            var c = (char)(FirstAccentedLetter + i);

            if (DecomposesToAccentedLatinLetter(c))
            {
                table[i] = char.ToLowerInvariant(c.ToString().Normalize(NormalizationForm.FormD)[0]);
            }
        }

        return table;
    }

    /// <summary>
    /// Composes the text to Unicode form C, so "e" plus a combining acute becomes "é", and keeps the
    /// accented Latin letters (<see cref="IsKeptAccentedLetter"/>). Every other non-ASCII character is
    /// folded exactly as with diacritic folding on: a lone combining mark disappears, "ǽ" becomes "æ"
    /// and "ḱ" becomes "k". So the text only differs from the folded text in the kept letters.
    /// </summary>
    private static string ComposeAccents(string value)
    {
        if (IsPlainAscii(value))
        {
            return value;
        }

        var composed = value.Normalize(NormalizationForm.FormC);
        StringBuilder? builder = null;

        for (var i = 0; i < composed.Length; i++)
        {
            var c = composed[i];

            if (c <= '\u007f' || IsKeptAccentedLetter(c))
            {
                builder?.Append(c);
                continue;
            }

            builder ??= new StringBuilder(composed.Length).Append(composed, 0, i);
            builder.Append(RemoveDiacritics(c.ToString()));
        }

        return builder?.ToString() ?? composed;
    }

    private static bool IsKeptLetter(char c, bool keepAccents) =>
        (c >= 'a' && c <= 'z') || (keepAccents && IsKeptAccentedLetter(c));

    /// <summary>
    /// Rewrites the elided article <c>d'</c> ("D'Hondt") to <c>de </c>, so that it becomes the
    /// particle "de" instead of a lone "d". This must run while the apostrophe still exists, because
    /// the apostrophe is the only thing that tells the article apart from an initial ("D. Jansen").
    /// The <c>d</c> must start a token, and at least three letters must follow the apostrophe, so
    /// that colloquial "d'r" is left alone. <see cref="Normalize"/> only applies this when
    /// <see cref="DutchFuzzyOptions.StripNamePrefixes"/> is true, so "d'accord" in the
    /// <see cref="DutchFuzzyOptions.Words"/> preset keeps its apostrophe as a separator.
    /// </summary>
    /// <param name="value">Lower-cased, folded text.</param>
    /// <param name="keepAccents">True when accented letters count as letters (diacritic folding is off).</param>
    /// <returns>The rewritten text, or <paramref name="value"/> itself when nothing changes.</returns>
    internal static string ExpandElidedArticles(string value, bool keepAccents = false)
    {
        StringBuilder? builder = null;
        var copiedUpTo = 0;

        for (var i = 0; i + 1 < value.Length; i++)
        {
            if (FoldKeptAccent(value[i]) != 'd' || !IsApostrophe(value[i + 1]))
            {
                continue;
            }

            if (i > 0 && char.IsLetter(value[i - 1]))
            {
                continue;
            }

            if (CountLetters(value, i + 2, keepAccents) < 3)
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

    private static int CountLetters(string value, int start, bool keepAccents)
    {
        var count = 0;

        while (start + count < value.Length && IsKeptLetter(value[start + count], keepAccents))
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

    private static string KeepLettersAndSpaces(string value, bool keepAccents)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasSpace = true;

        foreach (var c in value)
        {
            if (IsKeptLetter(c, keepAccents))
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
