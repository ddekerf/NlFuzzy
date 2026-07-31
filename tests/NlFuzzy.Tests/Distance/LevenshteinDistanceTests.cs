using NlFuzzy.Distance;

namespace NlFuzzy.Tests.Distance;

public class LevenshteinDistanceTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("kuipers", "kuipers", 0)]
    [InlineData("", "kuipers", 7)]
    [InlineData("kuipers", "", 7)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("saturday", "sunday", 3)]
    [InlineData("flaw", "lawn", 2)]
    [InlineData("meijer", "meier", 1)]
    [InlineData("bruijn", "bruin", 1)]
    public void Compute_MatchesKnownValues(string a, string b, int expected)
    {
        Assert.Equal(expected, LevenshteinDistance.Compute(a, b));
    }

    [Theory]
    [InlineData("kitten", "sitting")]
    [InlineData("bruijn", "bruin")]
    [InlineData("", "abc")]
    public void Compute_IsSymmetric(string a, string b)
    {
        Assert.Equal(LevenshteinDistance.Compute(a, b), LevenshteinDistance.Compute(b, a));
    }

    [Fact]
    public void Normalized_ScalesByLongestInput()
    {
        Assert.Equal(0.5d, LevenshteinDistance.Normalized("ab", "cb"), 10);
        Assert.Equal(0d, LevenshteinDistance.Normalized("", ""), 10);
        Assert.Equal(1d, LevenshteinDistance.Normalized("abc", ""), 10);
    }

    [Fact]
    public void Instance_ImplementsInterface()
    {
        IStringDistance distance = LevenshteinDistance.Instance;

        Assert.Equal(3d, distance.Distance("kitten", "sitting"));
    }

    [Fact]
    public void Compute_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => LevenshteinDistance.Compute(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => LevenshteinDistance.Compute("a", null!));
        Assert.Throws<ArgumentNullException>(() => LevenshteinDistance.Normalized(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => LevenshteinDistance.Normalized("a", null!));
    }
}
