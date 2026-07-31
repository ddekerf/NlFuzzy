using NlFuzzy.Distance;

namespace NlFuzzy.Tests.Distance;

public class DiceCoefficientSimilarityTests
{
    [Theory]
    [InlineData("", "", 1d)]
    [InlineData("a", "a", 1d)]
    [InlineData("a", "b", 0d)]
    [InlineData("night", "night", 1d)]
    [InlineData("night", "nacht", 0.25d)]
    [InlineData("abc", "xyz", 0d)]
    public void Similarity_MatchesKnownValues(string a, string b, double expected)
    {
        Assert.Equal(expected, DiceCoefficientSimilarity.Instance.Similarity(a, b), 10);
    }

    [Fact]
    public void Similarity_CountsRepeatedBigramsOnce()
    {
        // "aaaa" has three "aa" bigrams, "aa" has one; the intersection is limited to one.
        Assert.Equal(2d * 1 / (3 + 1), DiceCoefficientSimilarity.Instance.Similarity("aaaa", "aa"), 10);
    }

    [Theory]
    [InlineData("kuipers", "kuypers")]
    [InlineData("abc", "xyz")]
    public void Similarity_IsSymmetric(string a, string b)
    {
        Assert.Equal(
            DiceCoefficientSimilarity.Instance.Similarity(a, b),
            DiceCoefficientSimilarity.Instance.Similarity(b, a),
            10);
    }

    [Fact]
    public void Similarity_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => DiceCoefficientSimilarity.Instance.Similarity(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => DiceCoefficientSimilarity.Instance.Similarity("a", null!));
    }
}
