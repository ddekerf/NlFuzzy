using System;

namespace NlFuzzy;

/// <summary>
/// One central place where every public entry point resolves and validates a similarity threshold,
/// so that the contract (a finite number in the closed interval [0, 1]) cannot drift per method.
/// </summary>
internal static class Thresholds
{
    /// <summary>
    /// Returns <paramref name="threshold"/> when it is supplied, otherwise the
    /// <see cref="DutchFuzzyOptions.DefaultThreshold"/> of the effective options.
    /// </summary>
    /// <param name="threshold">The caller supplied threshold, or null to use the configured default.</param>
    /// <param name="options">The options in effect, or null for <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <param name="parameterName">The parameter name reported on failure.</param>
    /// <returns>The validated threshold.</returns>
    internal static double Resolve(double? threshold, DutchFuzzyOptions? options, string parameterName)
    {
        if (threshold is null)
        {
            var effective = options ?? DutchFuzzyOptions.Default;
            effective.Validate();

            return effective.DefaultThreshold;
        }

        return Validate(threshold.Value, parameterName);
    }

    /// <summary>Validates that a threshold is finite and inside [0, 1].</summary>
    /// <param name="threshold">The threshold to validate.</param>
    /// <param name="parameterName">The parameter name reported on failure.</param>
    /// <returns>The validated threshold.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the threshold is NaN, infinite or outside [0, 1].
    /// </exception>
    internal static double Validate(double threshold, string parameterName)
    {
        if (!MathCompat.IsFinite(threshold) || threshold < 0d || threshold > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                threshold,
                "Threshold must be a finite number in the closed interval [0, 1].");
        }

        return threshold;
    }
}
