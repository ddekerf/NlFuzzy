namespace NlFuzzy;

/// <summary>A scored match result.</summary>
/// <typeparam name="T">The type of the matched item.</typeparam>
/// <param name="Value">The matched item.</param>
/// <param name="Text">The text that was compared, as returned by the selector.</param>
/// <param name="Score">The similarity score in the closed interval [0, 1].</param>
/// <param name="PhoneticKey">The Dutch phonetic key of <paramref name="Text"/>.</param>
public readonly record struct FuzzyMatch<T>(T Value, string Text, double Score, string PhoneticKey);
