using System;

namespace NlFuzzy.Distance;

/// <summary>Sorensen-Dice coefficient over character bigrams.</summary>
public sealed class DiceCoefficientSimilarity : IStringSimilarity
{
    /// <summary>The shared, stateless instance.</summary>
    public static DiceCoefficientSimilarity Instance { get; } = new();

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

        if (a.Length < 2 || b.Length < 2)
        {
            return ShortInputSimilarity(a, b);
        }

        return Similarity(a.Length, SortedBigrams(a), b.Length, SortedBigrams(b));
    }

    /// <summary>
    /// Returns the bigrams of <paramref name="value"/>, packed into an int (two UTF-16 code units)
    /// and sorted, so that repeated scoring can intersect them without hashing or allocating.
    /// Returns an empty array for strings shorter than two characters.
    /// </summary>
    internal static int[] SortedBigrams(string value)
    {
        if (value.Length < 2)
        {
            return Array.Empty<int>();
        }

        var bigrams = new int[value.Length - 1];

        for (var i = 0; i < bigrams.Length; i++)
        {
            bigrams[i] = (value[i] << 16) | value[i + 1];
        }

        Array.Sort(bigrams);

        return bigrams;
    }

    /// <summary>
    /// Dice similarity from precomputed <see cref="SortedBigrams"/>. Repeated bigrams count as
    /// often as they occur in both strings (the minimum of both counts).
    /// </summary>
    internal static double Similarity(string a, int[] bigramsA, string b, int[] bigramsB)
    {
        if (a.Length < 2 || b.Length < 2)
        {
            return ShortInputSimilarity(a, b);
        }

        return Similarity(a.Length, bigramsA, b.Length, bigramsB);
    }

    private static double ShortInputSimilarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0)
        {
            return 1d;
        }

        return string.Equals(a, b, StringComparison.Ordinal) ? 1d : 0d;
    }

    private static double Similarity(int lengthA, int[] bigramsA, int lengthB, int[] bigramsB)
    {
        var intersection = 0;
        var i = 0;
        var j = 0;

        // Both arrays are sorted, so a merge counts min(countA, countB) for every bigram.
        while (i < bigramsA.Length && j < bigramsB.Length)
        {
            var left = bigramsA[i];
            var right = bigramsB[j];

            if (left == right)
            {
                intersection++;
                i++;
                j++;
            }
            else if (left < right)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        var total = lengthA - 1 + lengthB - 1;

        return total == 0 ? 0d : 2d * intersection / total;
    }
}
