using NlFuzzy.Distance;

namespace NlFuzzy.Tests.Distance;

public class DamerauLevenshteinDistanceTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("kuipers", "kuipers", 0)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    [InlineData("kuipers", "kuipres", 1)]
    [InlineData("ca", "abc", 3)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("meijer", "meiejr", 1)]
    public void Compute_MatchesKnownValues(string a, string b, int expected)
    {
        Assert.Equal(expected, DamerauLevenshteinDistance.Compute(a, b));
    }

    [Fact]
    public void Compute_TreatsTranspositionAsSingleEdit()
    {
        Assert.Equal(2, LevenshteinDistance.Compute("kuipers", "kuipres"));
        Assert.Equal(1, DamerauLevenshteinDistance.Compute("kuipers", "kuipres"));
    }

    [Fact]
    public void Normalized_ScalesByLongestInput()
    {
        Assert.Equal(0d, DamerauLevenshteinDistance.Normalized("", ""), 10);
        Assert.Equal(1d / 7d, DamerauLevenshteinDistance.Normalized("kuipers", "kuipres"), 10);
    }

    [Fact]
    public void Instance_ImplementsInterface()
    {
        IStringDistance distance = DamerauLevenshteinDistance.Instance;

        Assert.Equal(1d, distance.Distance("kuipers", "kuipres"));
    }

    [Fact]
    public void Compute_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => DamerauLevenshteinDistance.Compute(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => DamerauLevenshteinDistance.Compute("a", null!));
        Assert.Throws<ArgumentNullException>(() => DamerauLevenshteinDistance.Normalized(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => DamerauLevenshteinDistance.Normalized("a", null!));
    }
}
