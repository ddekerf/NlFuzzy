using System;
using System.Collections.Generic;
using System.Text;

namespace NlFuzzy.Normalization;

/// <summary>
/// Dutch name particles ("tussenvoegsels") and the rules that move or split them.
/// </summary>
public static class DutchAffixes
{
    /// <summary>The maximum number of leading particle tokens that is moved to the end.</summary>
    public const int MaxLeadingParticles = 3;

    /// <summary>The minimum length of the remaining stem before a glued prefix is split off.</summary>
    public const int MinimumStemLength = 3;

    private static readonly char[] SpaceSeparator = { ' ' };

    /// <summary>
    /// Particles that are recognised as a leading sequence and moved to the end of the name.
    /// </summary>
    private static readonly HashSet<string> Particles = new(StringComparer.Ordinal)
    {
        "van", "de", "den", "der", "des", "del", "ten", "ter", "te", "het", "t", "s",
        "op", "in", "uit", "aan", "bij", "onder", "over", "tot", "toe",
        "la", "le", "du", "di", "da", "dos", "das", "el", "al", "ibn", "bin", "mac", "mc", "o"
    };

    /// <summary>
    /// Particles that only count as a leading sequence when glued-prefix splitting is enabled,
    /// because they are indistinguishable from ordinary name stems otherwise.
    /// </summary>
    private static readonly HashSet<string> GluedOnlyParticles = new(StringComparer.Ordinal)
    {
        "ver", "vander", "vanden", "vande"
    };

    /// <summary>
    /// Normalized tokens that start with a glued-prefix spelling but are not glued surnames:
    /// frequent first names, Dutch and Flemish municipalities, and surnames such as "Derksen"
    /// ("der" + "ksen") that the stem rule cannot tell apart. Hand-compiled from public name and
    /// place facts (see PROVENANCE.md). Only tokens that would otherwise be split belong here.
    /// </summary>
    private static readonly HashSet<string> NeverSplit = new(StringComparer.Ordinal)
    {
        // First names
        "vanessa", "vanesa", "dennis", "denis", "denise", "deniz", "denzel", "deborah", "debora", "delia",
        "delphine", "delano", "demian", "derek", "desiree", "destiny", "devin", "devon",
        "verena", "veronique", "veronica", "vernon", "teresa", "terence", "tenzin", "tenzing",

        // Municipalities and towns
        "deventer", "delft", "delfzijl", "dedemsvaart", "denekamp", "dendermonde", "denderleeuw",
        "dentergem", "destelbergen", "deinze", "tervuren", "ternat", "terneuzen", "terschelling",
        "verviers",

        // Surnames that are not glued particles
        "derksen", "denissen", "deelen"
    };

    /// <summary>
    /// Glued Flemish prefixes and their expansion, ordered longest first.
    /// </summary>
    private static readonly KeyValuePair<string, string>[] GluedPrefixes =
    {
        new KeyValuePair<string, string>("vanhet", "van het"),
        new KeyValuePair<string, string>("vander", "van der"),
        new KeyValuePair<string, string>("vanden", "van den"),
        new KeyValuePair<string, string>("uitden", "uit den"),
        new KeyValuePair<string, string>("aanden", "aan den"),
        new KeyValuePair<string, string>("vande", "van de"),
        new KeyValuePair<string, string>("opden", "op den"),
        new KeyValuePair<string, string>("inden", "in den"),
        new KeyValuePair<string, string>("uitde", "uit de"),
        new KeyValuePair<string, string>("aande", "aan de"),
        new KeyValuePair<string, string>("opde", "op de"),
        new KeyValuePair<string, string>("inde", "in de"),
        new KeyValuePair<string, string>("van", "van"),
        new KeyValuePair<string, string>("ver", "van der"),
        new KeyValuePair<string, string>("den", "den"),
        new KeyValuePair<string, string>("der", "der"),
        new KeyValuePair<string, string>("ten", "ten"),
        new KeyValuePair<string, string>("ter", "ter"),
        new KeyValuePair<string, string>("de", "de")
    };

    /// <summary>
    /// Returns true when <paramref name="token"/> is a Dutch name particle such as "van" or "der".
    /// Accented Latin letters count as their base letter, so "dé" is the particle "de".
    /// </summary>
    /// <param name="token">A single lower-case token.</param>
    /// <returns>True when the token is a particle.</returns>
    public static bool IsParticle(string token) =>
        token is not null && Particles.Contains(DutchTextNormalizer.FoldKeptAccents(token));

    /// <summary>
    /// Moves a leading run of name particles to the end of the string, so that
    /// "van den broeke" and "broeke van den" become the same string.
    /// </summary>
    /// <param name="value">A normalized, space-separated string.</param>
    /// <returns>The rewritten string. Returns the input when there is nothing to move.</returns>
    public static string MovePrefixesToEnd(string value) => MovePrefixesToEnd(value, includeGluedOnlyParticles: false);

    /// <summary>
    /// Moves a leading run of name particles to the end of the string.
    /// </summary>
    /// <param name="value">A normalized, space-separated string.</param>
    /// <param name="includeGluedOnlyParticles">
    /// When true, "ver", "vander", "vanden" and "vande" also count as particles. Only meaningful
    /// when glued-prefix splitting is enabled.
    /// </param>
    /// <returns>The rewritten string. Returns the input when there is nothing to move.</returns>
    public static string MovePrefixesToEnd(string value, bool includeGluedOnlyParticles) =>
        MovePrefixesToEnd(value, includeGluedOnlyParticles, foldAccents: true);

    /// <summary>
    /// <see cref="MovePrefixesToEnd(string, bool)"/>, where <paramref name="foldAccents"/> false skips
    /// the accent folding of each token. Only for text that is known to be plain a-z.
    /// </summary>
    internal static string MovePrefixesToEnd(string value, bool includeGluedOnlyParticles, bool foldAccents)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var tokens = value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length < 2)
        {
            return value;
        }

        var leading = 0;

        while (leading < tokens.Length - 1
               && leading < MaxLeadingParticles
               && IsRecognisedParticle(tokens[leading], includeGluedOnlyParticles, foldAccents))
        {
            leading++;
        }

        if (leading == 0)
        {
            return value;
        }

        var hasStem = false;

        for (var i = leading; i < tokens.Length; i++)
        {
            if (!IsRecognisedParticle(tokens[i], includeGluedOnlyParticles, foldAccents))
            {
                hasStem = true;
                break;
            }
        }

        if (!hasStem)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);

        for (var i = leading; i < tokens.Length; i++)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(tokens[i]);
        }

        for (var i = 0; i < leading; i++)
        {
            builder.Append(' ').Append(tokens[i]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Splits glued Flemish prefixes from every token: "vandenbroecke" becomes "van den broecke"
    /// and "verbrugghe" becomes "van der brugghe".
    /// </summary>
    /// <param name="value">A normalized, space-separated string.</param>
    /// <returns>The rewritten string.</returns>
    /// <remarks>
    /// This is deliberately aggressive: it raises recall and lowers precision. It is only applied
    /// when <see cref="DutchFuzzyOptions.SplitGluedNamePrefixes"/> is enabled.
    /// <para>
    /// Prefixes are tried longest first. A prefix is only split off when at least
    /// <see cref="MinimumStemLength"/> letters remain and the remaining stem can start a Dutch word:
    /// a stem that starts with a doubled consonant or with "ck" is rejected, so "dekker" and
    /// "decker" stay intact. When a prefix is rejected, shorter prefixes are still tried
    /// ("vandellen" becomes "van dellen").
    /// </para>
    /// <para>
    /// A token of five or more letters that starts with "dh" + vowel is read as the apostrophe-less
    /// form of the elided article: "dhondt" becomes "de hondt", like "D'Hondt".
    /// </para>
    /// </remarks>
    public static string SplitGluedPrefixes(string value) => SplitGluedPrefixes(value, foldAccents: true);

    /// <summary>
    /// <see cref="SplitGluedPrefixes(string)"/>, where <paramref name="foldAccents"/> false skips the
    /// accent folding of each token. Only for text that is known to be plain a-z.
    /// </summary>
    internal static string SplitGluedPrefixes(string value, bool foldAccents)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var tokens = value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder(value.Length + 8);

        foreach (var token in tokens)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(SplitToken(token, honourNeverSplit: true, foldAccents));
        }

        return builder.ToString();
    }

    /// <summary>The tokens that are never split, for tests that keep the list honest.</summary>
    internal static IReadOnlyCollection<string> NeverSplitTokens => NeverSplit;

    /// <summary>Splits a single token as if the never-split list were empty. For tests only.</summary>
    internal static string SplitGluedPrefixesIgnoringExceptions(string token) => SplitToken(token, honourNeverSplit: false, foldAccents: true);

    private static string SplitToken(string token, bool honourNeverSplit, bool foldAccents)
    {
        // Decide on the folded token ("vándenbroecke" as "vandenbroecke"), but keep the accents in the
        // output. Folding maps one character to one, so positions line up.
        var folded = foldAccents ? DutchTextNormalizer.FoldKeptAccents(token) : token;

        if (Particles.Contains(folded) || GluedOnlyParticles.Contains(folded))
        {
            return token;
        }

        if (honourNeverSplit && NeverSplit.Contains(folded))
        {
            return token;
        }

        foreach (var prefix in GluedPrefixes)
        {
            var glued = prefix.Key;

            if (token.Length < glued.Length + MinimumStemLength)
            {
                continue;
            }

            if (!folded.StartsWith(glued, StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsPlausibleStemStart(folded, glued.Length))
            {
                continue;
            }

            return prefix.Value + " " + token.Substring(glued.Length);
        }

        // "Dhondt", "Dhaene": the apostrophe-less spelling of the elided article in D'Hondt.
        if (token.Length >= MinimumElidedDhLength
            && folded[0] == 'd'
            && folded[1] == 'h'
            && IsVowel(folded[2]))
        {
            return "de " + token.Substring(1);
        }

        return token;
    }

    /// <summary>The minimum token length for splitting "dh" + vowel into "de h…".</summary>
    private const int MinimumElidedDhLength = 5;

    /// <summary>
    /// False when the stem that would remain after a glued prefix starts in a way no Dutch word
    /// does: with a doubled consonant ("de|kker") or with "ck" ("de|cker").
    /// </summary>
    private static bool IsPlausibleStemStart(string token, int stemStart)
    {
        var first = token[stemStart];
        var second = token[stemStart + 1];

        if (first == 'c' && second == 'k')
        {
            return false;
        }

        return first != second || IsVowel(first);
    }

    private static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';

    private static bool IsRecognisedParticle(string token, bool includeGluedOnlyParticles, bool foldAccents)
    {
        var folded = foldAccents ? DutchTextNormalizer.FoldKeptAccents(token) : token;

        return Particles.Contains(folded) || (includeGluedOnlyParticles && GluedOnlyParticles.Contains(folded));
    }
}
