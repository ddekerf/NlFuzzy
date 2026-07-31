namespace NlFuzzy.Distance;

/// <summary>An edit-distance style metric.</summary>
public interface IStringDistance
{
    /// <summary>Returns a distance where 0 means identical. Range depends on the algorithm.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The distance between both strings.</returns>
    double Distance(string a, string b);
}
