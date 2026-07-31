using NlFuzzy.Distance;

namespace NlFuzzy.Tests.Distance;

public class TokenSetSimilarityTests
{
    [Theory]
    [InlineData("van den broeke", "broeke den van")]
    [InlineData("de smet", "smet de")]
    [InlineData("", "")]
    public void SortTokens_ProducesTheSameCanonicalOrder(string a, string b)
    {
        Assert.Equal(TokenSetSimilarity.SortTokens(a), TokenSetSimilarity.SortTokens(b));
    }

    [Fact]
    public void Similarity_IgnoresTokenOrder()
    {
        Assert.Equal(1d, TokenSetSimilarity.Instance.Similarity("van den broeke", "broeke van den"), 10);
    }

    [Fact]
    public void Similarity_IsLowerForDifferentTokens()
    {
        Assert.True(TokenSetSimilarity.Instance.Similarity("van den broeke", "van den bosch") < 1d);
    }

    [Fact]
    public void SortTokens_HandlesSingleToken()
    {
        Assert.Equal("kuipers", TokenSetSimilarity.SortTokens("  kuipers  "));
    }

    [Fact]
    public void Constructor_AcceptsCustomInnerSimilarity()
    {
        var similarity = new TokenSetSimilarity(DiceCoefficientSimilarity.Instance);

        Assert.Equal(1d, similarity.Similarity("a b", "b a"), 10);
    }

    [Fact]
    public void ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new TokenSetSimilarity(null!));
        Assert.Throws<ArgumentNullException>(() => TokenSetSimilarity.SortTokens(null!));
        Assert.Throws<ArgumentNullException>(() => TokenSetSimilarity.Instance.Similarity(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => TokenSetSimilarity.Instance.Similarity("a", null!));
    }
}
