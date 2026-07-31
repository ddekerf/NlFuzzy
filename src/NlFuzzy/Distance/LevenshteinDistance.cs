using System;

namespace NlFuzzy.Distance;

/// <summary>Wagner-Fischer Levenshtein distance using two rolling rows.</summary>
public sealed class LevenshteinDistance : IStringDistance
{
    /// <summary>The shared, stateless instance.</summary>
    public static LevenshteinDistance Instance { get; } = new();

    /// <inheritdoc />
    public double Distance(string a, string b) => Compute(a, b);

    /// <summary>Computes the Levenshtein distance between two strings.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The minimum number of insertions, deletions and substitutions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static int Compute(string a, string b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        if (string.Equals(a, b, StringComparison.Ordinal))
        {
            return 0;
        }

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                var deletion = previous[j] + 1;
                var insertion = current[j - 1] + 1;
                var substitution = previous[j - 1] + cost;

                current[j] = Math.Min(Math.Min(deletion, insertion), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    /// <summary>Levenshtein distance scaled to [0, 1] by the length of the longest input.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>0 when both strings are identical, 1 when nothing matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static double Normalized(string a, string b)
    {
        if (a is null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b is null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        var longest = Math.Max(a.Length, b.Length);

        return longest == 0 ? 0d : Compute(a, b) / (double)longest;
    }
}
