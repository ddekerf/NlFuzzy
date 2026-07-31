using System;
using System.Collections.Generic;

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

        if (a.Length == 0 && b.Length == 0)
        {
            return 1d;
        }

        if (a.Length < 2 || b.Length < 2)
        {
            return string.Equals(a, b, StringComparison.Ordinal) ? 1d : 0d;
        }

        var first = BuildBigrams(a);
        var second = BuildBigrams(b);
        var intersection = 0;

        foreach (var pair in first)
        {
            if (second.TryGetValue(pair.Key, out var count))
            {
                intersection += Math.Min(pair.Value, count);
            }
        }

        var total = a.Length - 1 + b.Length - 1;

        return total == 0 ? 0d : 2d * intersection / total;
    }

    // Bigrams are packed into an int (two UTF-16 code units) to avoid allocating a string per bigram.
    private static Dictionary<int, int> BuildBigrams(string value)
    {
        var result = new Dictionary<int, int>(value.Length);

        for (var i = 0; i < value.Length - 1; i++)
        {
            var bigram = (value[i] << 16) | value[i + 1];
            result[bigram] = result.TryGetValue(bigram, out var count) ? count + 1 : 1;
        }

        return result;
    }
}
