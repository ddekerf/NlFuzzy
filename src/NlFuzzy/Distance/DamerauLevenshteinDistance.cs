using System;

namespace NlFuzzy.Distance;

/// <summary>
/// Damerau-Levenshtein distance, optimal string alignment variant: insertions, deletions,
/// substitutions and transpositions of adjacent characters.
/// </summary>
/// <remarks>
/// The optimal string alignment variant does not allow a substring to be edited more than once,
/// so it is not a true metric, but it is the variant that matches typing errors such as
/// "Kuipres" versus "Kuipers".
/// </remarks>
public sealed class DamerauLevenshteinDistance : IStringDistance
{
    /// <summary>The shared, stateless instance.</summary>
    public static DamerauLevenshteinDistance Instance { get; } = new();

    /// <inheritdoc />
    public double Distance(string a, string b) => Compute(a, b);

    /// <summary>Computes the optimal string alignment distance between two strings.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The minimum number of edits, counting an adjacent transposition as one edit.</returns>
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

        var matrix = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                var value = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);

                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                {
                    value = Math.Min(value, matrix[i - 2, j - 2] + 1);
                }

                matrix[i, j] = value;
            }
        }

        return matrix[a.Length, b.Length];
    }

    /// <summary>Damerau-Levenshtein distance scaled to [0, 1] by the length of the longest input.</summary>
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
