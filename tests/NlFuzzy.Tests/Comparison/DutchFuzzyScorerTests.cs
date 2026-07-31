using NlFuzzy.Comparison;

namespace NlFuzzy.Tests.Comparison;

public class DutchFuzzyScorerTests
{
    private static readonly DutchFuzzyScorer Names = new(DutchFuzzyOptions.Names);

    [Theory]
    [InlineData("kuipers", "kuipers")]
    [InlineData("Van den Broecke", "van den broecke")]
    [InlineData("Céline", "celine")]
    public void Score_ReturnsOneForEquivalentInput(string a, string b)
    {
        Assert.Equal(1d, Names.Score(a, b), 10);
    }

    [Theory]
    [InlineData("kuipers", "abcdefg")]
    [InlineData("x", "y")]
    public void Score_StaysWithinUnitInterval(string a, string b)
    {
        Assert.InRange(Names.Score(a, b), 0d, 1d);
    }

    [Theory]
    [InlineData("meijer", "meier")]
    [InlineData("bruijn", "bruyn")]
    [InlineData("sijmons", "simons")]
    [InlineData("de smet", "desmet")]
    public void Score_IsHighForKnownVariants(string a, string b)
    {
        Assert.True(Names.Score(a, b) >= 0.85, $"{a}/{b} scored {Names.Score(a, b)}.");
    }

    [Theory]
    [InlineData("jansen", "hansen")]
    [InlineData("maes", "moes")]
    [InlineData("kuipers", "kuiper")]
    [InlineData("vermeulen", "verhoeven")]
    public void Score_IsLowForDistinctNames(string a, string b)
    {
        Assert.True(Names.Score(a, b) < 0.75, $"{a}/{b} scored {Names.Score(a, b)}.");
    }

    [Theory]
    [InlineData("meijer", "meier")]
    [InlineData("jansen", "hansen")]
    [InlineData("", "kuipers")]
    public void Score_IsSymmetric(string a, string b)
    {
        Assert.Equal(Names.Score(a, b), Names.Score(b, a), 10);
    }

    [Fact]
    public void Score_ReturnsZeroWhenOneSideIsEmpty()
    {
        Assert.Equal(0d, Names.Score(string.Empty, "kuipers"), 10);
        Assert.Equal(0d, Names.Score("kuipers", string.Empty), 10);
    }

    [Fact]
    public void Score_ReturnsOneWhenBothSidesAreEmpty()
    {
        Assert.Equal(1d, Names.Score(string.Empty, string.Empty), 10);
    }

    [Fact]
    public void IdenticalKeyFloor_RaisesTheScoreForSameSoundingInput()
    {
        var withFloor = new DutchFuzzyScorer(DutchFuzzyOptions.Names with { IdenticalKeyFloor = 0.99 });
        var withoutFloor = new DutchFuzzyScorer(DutchFuzzyOptions.Names with { IdenticalKeyFloor = 0d });

        Assert.True(withFloor.Score("gijsbrecht", "gijsbregt") >= 0.99);
        Assert.True(withoutFloor.Score("gijsbrecht", "gijsbregt") <= withFloor.Score("gijsbrecht", "gijsbregt"));
    }

    [Fact]
    public void Weights_ChangeTheOutcome()
    {
        var phoneticOnly = new DutchFuzzyScorer(DutchFuzzyOptions.Names with
        {
            PhoneticWeight = 1d,
            OrthographicWeight = 0d,
            NGramWeight = 0d,
            IdenticalKeyFloor = 0d,
        });

        var orthographicOnly = new DutchFuzzyScorer(DutchFuzzyOptions.Names with
        {
            PhoneticWeight = 0d,
            OrthographicWeight = 1d,
            NGramWeight = 0d,
            IdenticalKeyFloor = 0d,
        });

        Assert.NotEqual(phoneticOnly.Score("philips", "filips"), orthographicOnly.Score("philips", "filips"), 3);
    }

    [Fact]
    public void ExposesOptionsAndEncoder()
    {
        Assert.Same(DutchFuzzyOptions.Names, Names.Options);
        Assert.NotNull(Names.Encoder);
    }

    [Fact]
    public void ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DutchFuzzyScorer(null!));
        Assert.Throws<ArgumentNullException>(() => Names.Score(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => Names.Score("a", null!));
    }

    [Fact]
    public void Default_UsesDefaultOptions()
    {
        Assert.Same(DutchFuzzyOptions.Default, DutchFuzzyScorer.Default.Options);
    }
}
