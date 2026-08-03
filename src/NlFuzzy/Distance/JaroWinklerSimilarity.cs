using System;

namespace NlFuzzy.Distance;

/// <summary>Jaro-Winkler similarity in [0, 1].</summary>
public sealed class JaroWinklerSimilarity : IStringSimilarity
{
    private const double DefaultScalingFactor = 0.1;
    private const int MaxPrefixLength = 4;

    /// <summary>The shared instance with the standard scaling factor of 0.1.</summary>
    public static JaroWinklerSimilarity Instance { get; } = new();

    private readonly double _scalingFactor;

    /// <summary>Creates a new instance.</summary>
    /// <param name="scalingFactor">
    /// The Winkler prefix scaling factor. Must be a finite number in [0, 0.25]. Defaults to 0.1.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the scaling factor is not finite or out of range.
    /// </exception>
    public JaroWinklerSimilarity(double scalingFactor = DefaultScalingFactor)
    {
        if (!MathCompat.IsFinite(scalingFactor) || scalingFactor < 0 || scalingFactor > 0.25)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scalingFactor),
                scalingFactor,
                "Scaling factor must be a finite number between 0 and 0.25.");
        }

        _scalingFactor = scalingFactor;
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

        if (a.Length == 0 && b.Length == 0)
        {
            return 1d;
        }

        if (a.Length == 0 || b.Length == 0)
        {
            return 0d;
        }

        if (string.Equals(a, b, StringComparison.Ordinal))
        {
            return 1d;
        }

        var jaro = Jaro(a, b);

        if (jaro <= 0d)
        {
            return 0d;
        }

        var prefix = 0;
        var limit = Math.Min(MaxPrefixLength, Math.Min(a.Length, b.Length));

        while (prefix < limit && a[prefix] == b[prefix])
        {
            prefix++;
        }

        return jaro + prefix * _scalingFactor * (1d - jaro);
    }

    /// <summary>Computes the plain Jaro similarity, without the Winkler prefix bonus.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The Jaro similarity in [0, 1].</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static double JaroSimilarity(string a, string b)
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

        if (a.Length == 0 || b.Length == 0)
        {
            return 0d;
        }

        return string.Equals(a, b, StringComparison.Ordinal) ? 1d : Jaro(a, b);
    }

    private static double Jaro(string a, string b)
    {
        var matchWindow = Math.Max(a.Length, b.Length) / 2 - 1;

        if (matchWindow < 0)
        {
            matchWindow = 0;
        }

        var aMatched = new bool[a.Length];
        var bMatched = new bool[b.Length];
        var matches = 0;

        for (var i = 0; i < a.Length; i++)
        {
            var start = Math.Max(0, i - matchWindow);
            var end = Math.Min(i + matchWindow + 1, b.Length);

            for (var j = start; j < end; j++)
            {
                if (bMatched[j] || a[i] != b[j])
                {
                    continue;
                }

                aMatched[i] = true;
                bMatched[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
        {
            return 0d;
        }

        var transpositions = 0;
        var k = 0;

        for (var i = 0; i < a.Length; i++)
        {
            if (!aMatched[i])
            {
                continue;
            }

            while (!bMatched[k])
            {
                k++;
            }

            if (a[i] != b[k])
            {
                transpositions++;
            }

            k++;
        }

        var half = transpositions / 2d;

        return (matches / (double)a.Length
                + matches / (double)b.Length
                + (matches - half) / matches) / 3d;
    }
}
