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

    [Theory]
    [InlineData("Kuipers", "Kuipres")]
    [InlineData("Hoekstra", "Hoekstar")]
    [InlineData("Jan Kuipers", "Jan Kuipres")]
    [InlineData("Kuipres", "Kuipers")]
    public void TranspositionFloor_RaisesASingleAdjacentSwap(string a, string b)
    {
        Assert.True(Names.Score(a, b) >= 0.85, $"{a}/{b} scored {Names.Score(a, b)}.");
    }

    [Theory]
    [InlineData("Born", "Bron")]
    [InlineData("Jan Smit", "Jan Smti")]
    [InlineData("Kuipers", "Kuirpes")]
    [InlineData("Kuipers", "Kuipars")]
    [InlineData("Jan Smit", "Ja nSmit")]
    public void TranspositionFloor_LeavesOtherPairsAlone(string a, string b)
    {
        var withoutFloor = new DutchFuzzyScorer(DutchFuzzyOptions.Names with { TranspositionFloor = 0d });

        Assert.Equal(withoutFloor.Score(a, b), Names.Score(a, b), 10);
    }

    [Theory]
    [InlineData("kuipers", "kuipres", true)]
    [InlineData("hoekstra", "hoekstar", true)]
    [InlineData("akker", "kakker", false)]
    [InlineData("kuipers", "kuipers", false)]
    [InlineData("kuipers", "kuipera", false)]
    [InlineData("kuipers", "kuirpes", false)]
    [InlineData("kuipers", "kuiprse", false)]
    [InlineData("jan smit", "jan smti", false)]
    [InlineData("jan smit", "ja nsmit", false)]
    [InlineData("smit jan", "smit ajn", false)]
    [InlineData("kuipers jan", "kuipres jan", true)]
    [InlineData("jan kuipers", "jan kuipres", true)]
    [InlineData("born", "bron", false)]
    [InlineData("aerts", "arets", true)]
    public void IsSingleAdjacentSwap_ChecksOneSwapInAFiveLetterWord(string a, string b, bool expected)
    {
        Assert.Equal(expected, DutchFuzzyScorer.IsSingleAdjacentSwap(a, b));
        Assert.Equal(expected, DutchFuzzyScorer.IsSingleAdjacentSwap(b, a));
    }

    [Fact]
    public void TranspositionFloor_IsOffInTheDefaultPreset()
    {
        // The score Default gave before TranspositionFloor existed (the keys are equal there).
        Assert.Equal(0.900, DutchFuzzyScorer.Default.Score("Kuipers", "Kuipres"), 3);
        Assert.Equal(0d, DutchFuzzyScorer.Default.Options.TranspositionFloor, 10);
    }

    [Fact]
    public void TranspositionFloor_KeepsSameSoundingSpellingsAhead()
    {
        var ranked = new[] { "Mejier", "Meier" }
            .FuzzyMatchDutch("Meijer", x => x, threshold: 0d, take: 2, options: DutchFuzzyOptions.Names)
            .ToList();

        Assert.Equal(["Meier", "Mejier"], ranked.Select(m => m.Value));
        Assert.True(ranked[1].Score >= 0.85);
    }

    [Fact]
    public void KeepingDiacritics_CountsAccentsForSpellingOnly()
    {
        var keepAccents = DutchFuzzyOptions.Default with { RemoveDiacritics = false };
        var score = DutchFuzzy.Similarity("Céline", "Celine", keepAccents);

        Assert.True("Céline".SoundsLikeDutch("Celine", keepAccents));
        Assert.InRange(score, 0.90, 0.9999);
        Assert.Equal(1d, DutchFuzzy.Similarity("Céline", "Celine"), 10);
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

    [Theory]
    [InlineData("Jan Bakker", "Bakker, Jan")]
    [InlineData("Jan Bakker", "Bakker Jan")]
    public void TokenOrder_ReorderedNamesScoreAsANearMatch(string a, string b)
    {
        var score = Names.Score(a, b);

        Assert.True(score >= 0.95, $"{a}/{b} scored {score}.");
        Assert.True(score < 1d, $"{a}/{b} scored {score}; reordered input must rank below identical input.");
    }

    [Fact]
    public void TokenOrder_ReorderedNameWithAParticleScoresAsANearMatch()
    {
        var score = DutchFuzzyScorer.Default.Score("Piet de Vries", "de Vries, Piet");

        Assert.True(score >= 0.95, $"Scored {score}.");
    }

    [Fact]
    public void TokenOrder_ReorderingCombinesWithSpellingVariants()
    {
        var score = Names.Score("Jan Kuipers", "Cuypers, Jan");

        Assert.True(score >= DutchStringExtensions.DefaultThreshold, $"Scored {score}.");
    }

    [Fact]
    public void TokenOrder_WordsPresetKeepsTheOriginalOrder()
    {
        var words = new DutchFuzzyScorer(DutchFuzzyOptions.Words);
        var ordered = new DutchFuzzyScorer(DutchFuzzyOptions.Words with { IgnoreTokenOrder = false });

        Assert.Equal(ordered.Score("Jan Bakker", "Bakker Jan"), words.Score("Jan Bakker", "Bakker Jan"), 10);
        Assert.True(words.Score("Jan Bakker", "Bakker Jan") < 0.5);
    }

    [Theory]
    [InlineData("Meijer", "Meier")]
    [InlineData("Jansen", "Hansen")]
    [InlineData("Kuipers", "Kuiper")]
    [InlineData("Bakker", "Jan Bakker")]
    public void TokenOrder_DoesNotChangeScoresUnlessBothSidesHaveSeveralTokens(string a, string b)
    {
        var tolerant = new DutchFuzzyScorer(DutchFuzzyOptions.Names);
        var ordered = new DutchFuzzyScorer(DutchFuzzyOptions.Names with { IgnoreTokenOrder = false });

        Assert.Equal(ordered.Score(a, b), tolerant.Score(a, b), 10);
    }

    [Fact]
    public void TokenOrder_DoesNotChangePhoneticKeys()
    {
        var ordered = DutchFuzzyOptions.Names with { IgnoreTokenOrder = false };

        Assert.Equal(
            DutchFuzzy.PhoneticKey("Bakker, Jan", ordered),
            DutchFuzzy.PhoneticKey("Bakker, Jan", DutchFuzzyOptions.Names));
    }

    [Fact]
    public void Prepare_BuildsTheSameCanonicalFormForReorderedTokens()
    {
        var first = Names.Prepare("bakker jan");
        var second = Names.Prepare("jan bakker");

        Assert.NotNull(first.CanonicalNormalized);
        Assert.Equal(first.CanonicalNormalized, second.CanonicalNormalized);
        Assert.Equal(first.CanonicalKey, second.CanonicalKey);
        Assert.NotEqual(first.Key, second.Key);
    }

    [Fact]
    public void Prepare_SkipsTheCanonicalFormForSingleTokensAndWhenDisabled()
    {
        var ordered = new DutchFuzzyScorer(DutchFuzzyOptions.Names with { IgnoreTokenOrder = false });

        Assert.Null(Names.Prepare("bakker").CanonicalNormalized);
        Assert.Null(ordered.Prepare("jan bakker").CanonicalNormalized);
    }
}
