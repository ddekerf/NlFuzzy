using NlFuzzy.Distance;

namespace NlFuzzy.Tests.Distance;

public class JaroWinklerSimilarityTests
{
    // Reference values from the Winkler/Jaro literature, cross-checked against this implementation.
    [Theory]
    [InlineData("MARTHA", "MARHTA", 0.96111)]
    [InlineData("DWAYNE", "DUANE", 0.84000)]
    [InlineData("DIXON", "DICKSONX", 0.81333)]
    public void Similarity_MatchesReferenceValues(string a, string b, double expected)
    {
        Assert.Equal(expected, JaroWinklerSimilarity.Instance.Similarity(a, b), 5);
    }

    [Theory]
    [InlineData("MARTHA", "MARHTA", 0.94444)]
    [InlineData("DWAYNE", "DUANE", 0.82222)]
    [InlineData("DIXON", "DICKSONX", 0.76667)]
    [InlineData("CRATE", "TRACE", 0.73333)]
    public void JaroSimilarity_MatchesReferenceValues(string a, string b, double expected)
    {
        Assert.Equal(expected, JaroWinklerSimilarity.JaroSimilarity(a, b), 5);
    }

    [Theory]
    [InlineData("", "", 1d)]
    [InlineData("abc", "", 0d)]
    [InlineData("", "abc", 0d)]
    [InlineData("abc", "abc", 1d)]
    [InlineData("abc", "xyz", 0d)]
    public void Similarity_HandlesEdgeCases(string a, string b, double expected)
    {
        Assert.Equal(expected, JaroWinklerSimilarity.Instance.Similarity(a, b), 10);
        Assert.Equal(expected, JaroWinklerSimilarity.JaroSimilarity(a, b), 10);
    }

    [Theory]
    [InlineData("kuipers", "kuypers")]
    [InlineData("meijer", "meier")]
    [InlineData("abc", "xyz")]
    public void Similarity_IsSymmetric(string a, string b)
    {
        Assert.Equal(
            JaroWinklerSimilarity.Instance.Similarity(a, b),
            JaroWinklerSimilarity.Instance.Similarity(b, a),
            10);
    }

    [Fact]
    public void Similarity_StaysWithinUnitInterval()
    {
        foreach (var a in new[] { "bruijn", "kuipers", "van den broecke", "x" })
        {
            foreach (var b in new[] { "bruyn", "kuiper", "broecke van den", "y" })
            {
                var score = JaroWinklerSimilarity.Instance.Similarity(a, b);

                Assert.InRange(score, 0d, 1d);
            }
        }
    }

    [Fact]
    public void Constructor_RejectsOutOfRangeScalingFactor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new JaroWinklerSimilarity(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JaroWinklerSimilarity(0.3));
    }

    [Fact]
    public void Constructor_AcceptsCustomScalingFactor()
    {
        var similarity = new JaroWinklerSimilarity(0.25);

        Assert.True(similarity.Similarity("MARTHA", "MARHTA") > JaroWinklerSimilarity.Instance.Similarity("MARTHA", "MARHTA"));
    }

    [Fact]
    public void Similarity_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => JaroWinklerSimilarity.Instance.Similarity(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => JaroWinklerSimilarity.Instance.Similarity("a", null!));
        Assert.Throws<ArgumentNullException>(() => JaroWinklerSimilarity.JaroSimilarity(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => JaroWinklerSimilarity.JaroSimilarity("a", null!));
    }
}
