namespace NlFuzzy.Distance;

/// <summary>A normalized similarity measure.</summary>
public interface IStringSimilarity
{
    /// <summary>Returns a normalized similarity in the closed interval [0, 1].</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>1 when the strings are considered identical, 0 when they share nothing.</returns>
    double Similarity(string a, string b);
}
