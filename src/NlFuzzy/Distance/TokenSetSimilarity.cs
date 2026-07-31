using System;

namespace NlFuzzy.Distance;

/// <summary>
/// Order-insensitive similarity: both strings are split on whitespace, the tokens are sorted
/// alphabetically and joined again before Jaro-Winkler is applied.
/// </summary>
/// <remarks>
/// This solves "Broeke van den" versus "van den Broeke" when
/// <see cref="DutchFuzzyOptions.StripNamePrefixes"/> is disabled.
/// </remarks>
public sealed class TokenSetSimilarity : IStringSimilarity
{
    private static readonly char[] WhitespaceSeparators = { ' ', '\t', '\r', '\n' };

    /// <summary>The shared, stateless instance.</summary>
    public static TokenSetSimilarity Instance { get; } = new();

    private readonly IStringSimilarity _inner;

    /// <summary>Creates an instance that uses Jaro-Winkler on the sorted token sequence.</summary>
    public TokenSetSimilarity()
        : this(JaroWinklerSimilarity.Instance)
    {
    }

    /// <summary>Creates an instance that uses a custom inner similarity.</summary>
    /// <param name="inner">The similarity applied to the sorted, re-joined strings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is null.</exception>
    public TokenSetSimilarity(IStringSimilarity inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public double Similarity(string a, string b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        return _inner.Similarity(SortTokens(a), SortTokens(b));
    }

    /// <summary>Splits on whitespace, sorts the tokens ordinally and joins them with single spaces.</summary>
    /// <param name="value">The value to canonicalize.</param>
    /// <returns>The canonical token order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string SortTokens(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var tokens = value.Split(WhitespaceSeparators, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length < 2)
        {
            return tokens.Length == 0 ? string.Empty : tokens[0];
        }

        Array.Sort(tokens, StringComparer.Ordinal);

        return string.Join(" ", tokens);
    }
}
