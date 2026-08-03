using System.Linq;
using NlFuzzy.Indexing;

namespace NlFuzzy.Tests;

/// <summary>
/// Every public entry point resolves its threshold through the same helper: an omitted threshold
/// falls back to <see cref="DutchFuzzyOptions.DefaultThreshold"/>, and an invalid threshold throws
/// instead of silently returning true or an empty result.
/// </summary>
public class ThresholdContractTests
{
    private static readonly string[] Names = ["Meijer", "Meier", "Bakker", "Visser"];

    public static TheoryData<double> InvalidThresholds =>
        new() { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.1, 1.1 };

    [Theory]
    [MemberData(nameof(InvalidThresholds))]
    public void AreEquivalent_RejectsInvalidThresholds(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DutchFuzzy.AreEquivalent("a", "b", threshold));
    }

    [Theory]
    [MemberData(nameof(InvalidThresholds))]
    public void DutchFuzzyEquals_RejectsInvalidThresholds(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "a".DutchFuzzyEquals("b", threshold));
    }

    [Theory]
    [MemberData(nameof(InvalidThresholds))]
    public void FuzzyMatchDutch_RejectsInvalidThresholds(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Names.FuzzyMatchDutch("Meijer", threshold).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Names.FuzzyMatchDutch("Meijer", static name => name, threshold).ToList());
    }

    [Theory]
    [MemberData(nameof(InvalidThresholds))]
    public void BestDutchMatch_RejectsInvalidThresholds(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Names.BestDutchMatch("Meijer", static name => name, threshold));
    }

    [Theory]
    [MemberData(nameof(InvalidThresholds))]
    public void IndexSearch_RejectsInvalidThresholds(double threshold)
    {
        var index = new DutchFuzzyIndex<string>(Names, static name => name);

        Assert.Throws<ArgumentOutOfRangeException>(() => index.Search("Meijer", threshold));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => index.Search("Meijer", threshold, 10, DutchFuzzyIndexSearchMode.Blocked));
    }

    [Fact]
    public void NegativeThresholdNoLongerMakesEverythingEquivalent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DutchFuzzy.AreEquivalent("a", "b", -1d));
    }

    [Fact]
    public void OmittedThresholdUsesTheConfiguredDefault()
    {
        var strict = DutchFuzzyOptions.Default with { DefaultThreshold = 0.999 };
        var loose = DutchFuzzyOptions.Default with { DefaultThreshold = 0.10 };

        Assert.True(DutchFuzzy.AreEquivalent("Meijer", "Meier"));
        Assert.False(DutchFuzzy.AreEquivalent("Meijer", "Meier", threshold: null, strict));
        Assert.True(DutchFuzzy.AreEquivalent("Meijer", "Meier", threshold: null, loose));

        Assert.False("Meijer".DutchFuzzyEquals("Meier", threshold: null, strict));
        Assert.True("Meijer".DutchFuzzyEquals("Meier", threshold: null, loose));
    }

    [Fact]
    public void OmittedThresholdUsesTheConfiguredDefaultInSearchHelpers()
    {
        var strict = DutchFuzzyOptions.Default with { DefaultThreshold = 0.999 };
        var loose = DutchFuzzyOptions.Default with { DefaultThreshold = 0.10 };

        Assert.Empty(Names.FuzzyMatchDutch("Meyer", threshold: null, take: 10, options: strict));
        Assert.NotEmpty(Names.FuzzyMatchDutch("Meyer", threshold: null, take: 10, options: loose));

        Assert.Null(Names.BestDutchMatch("Meyer", static name => name, threshold: null, strict));
        Assert.NotNull(Names.BestDutchMatch("Meyer", static name => name, threshold: null, loose));

        Assert.Empty(new DutchFuzzyIndex<string>(Names, static name => name, strict).Search("Meyer"));
        Assert.NotEmpty(new DutchFuzzyIndex<string>(Names, static name => name, loose).Search("Meyer"));
    }

    [Fact]
    public void ExplicitThresholdWinsOverTheConfiguredDefault()
    {
        var strict = DutchFuzzyOptions.Default with { DefaultThreshold = 0.999 };

        Assert.True(DutchFuzzy.AreEquivalent("Meijer", "Meier", 0.5, strict));
        Assert.NotEmpty(Names.FuzzyMatchDutch("Meyer", 0.5, 10, strict));
    }

    [Fact]
    public void ConstantMatchesTheDefaultOptions()
    {
        Assert.Equal(DutchStringExtensions.DefaultThreshold, DutchFuzzyOptions.Default.DefaultThreshold, 10);
    }
}
