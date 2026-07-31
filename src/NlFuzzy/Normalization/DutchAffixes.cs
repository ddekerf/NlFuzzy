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
    /// </summary>
    /// <param name="token">A single lower-case token.</param>
    /// <returns>True when the token is a particle.</returns>
    public static bool IsParticle(string token) => token is not null && Particles.Contains(token);

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
    public static string MovePrefixesToEnd(string value, bool includeGluedOnlyParticles)
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
               && IsRecognisedParticle(tokens[leading], includeGluedOnlyParticles))
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
            if (!IsRecognisedParticle(tokens[i], includeGluedOnlyParticles))
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
    /// </remarks>
    public static string SplitGluedPrefixes(string value)
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

            builder.Append(SplitToken(token));
        }

        return builder.ToString();
    }

    private static string SplitToken(string token)
    {
        if (Particles.Contains(token) || GluedOnlyParticles.Contains(token))
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

            if (!token.StartsWith(glued, StringComparison.Ordinal))
            {
                continue;
            }

            return prefix.Value + " " + token.Substring(glued.Length);
        }

        return token;
    }

    private static bool IsRecognisedParticle(string token, bool includeGluedOnlyParticles) =>
        Particles.Contains(token) || (includeGluedOnlyParticles && GluedOnlyParticles.Contains(token));
}
