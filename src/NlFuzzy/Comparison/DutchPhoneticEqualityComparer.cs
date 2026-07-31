using System;
using System.Collections.Generic;

namespace NlFuzzy.Comparison;

/// <summary>
/// Compares strings by their Dutch phonetic key. Suitable as a dictionary or set comparer
/// because key equality is reflexive, symmetric and transitive.
/// </summary>
/// <remarks>
/// <see cref="DutchStringExtensions.DutchFuzzyEquals(string, string, double, DutchFuzzyOptions)"/>
/// deliberately does <b>not</b> get an <see cref="IEqualityComparer{T}"/>: a threshold-based
/// comparison is not transitive and would corrupt hash-based collections.
/// </remarks>
public sealed class DutchPhoneticEqualityComparer : IEqualityComparer<string>
{
    private readonly Phonetics.DutchPhoneticEncoder _encoder;

    /// <summary>A comparer configured with <see cref="DutchFuzzyOptions.Default"/>.</summary>
    public static DutchPhoneticEqualityComparer Default { get; } = new();

    /// <summary>Creates a new comparer.</summary>
    /// <param name="options">The options to use. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    public DutchPhoneticEqualityComparer(DutchFuzzyOptions? options = null)
    {
        var effective = options ?? DutchFuzzyOptions.Default;
        effective.Validate();
        _encoder = new Phonetics.DutchPhoneticEncoder(effective);
    }

    /// <summary>Returns true when both strings produce the same Dutch phonetic key.</summary>
    /// <param name="x">The first string.</param>
    /// <param name="y">The second string.</param>
    /// <returns>True when both keys are equal; two nulls are equal.</returns>
    public bool Equals(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return string.Equals(_encoder.Encode(x), _encoder.Encode(y), StringComparison.Ordinal);
    }

    /// <summary>Returns the hash code of the Dutch phonetic key of <paramref name="obj"/>.</summary>
    /// <param name="obj">The string to hash.</param>
    /// <returns>The ordinal hash code of the phonetic key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    public int GetHashCode(string obj)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        return StringComparer.Ordinal.GetHashCode(_encoder.Encode(obj));
    }
}
