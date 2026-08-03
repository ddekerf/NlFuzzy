using NlFuzzy.Comparison;

namespace NlFuzzy.Tests;

public class DutchFuzzyOptionsTests
{
    [Fact]
    public void Default_HasTheDocumentedValues()
    {
        var options = DutchFuzzyOptions.Default;

        Assert.Equal(DutchPhoneticProfile.Tolerant, options.Profile);
        Assert.False(options.EncodeVowels);
        Assert.False(options.SplitGluedNamePrefixes);
        Assert.True(options.RemoveDiacritics);
        Assert.True(options.StripNamePrefixes);
        Assert.Equal(0, options.MaxKeyLength);
        Assert.Equal(0.45, options.PhoneticWeight, 10);
        Assert.Equal(0.35, options.OrthographicWeight, 10);
        Assert.Equal(0.20, options.NGramWeight, 10);
        Assert.Equal(1d, options.PhoneticWeight + options.OrthographicWeight + options.NGramWeight, 10);
        Assert.Equal(0.90, options.IdenticalKeyFloor, 10);
        Assert.Equal(0.82, options.DefaultThreshold, 10);
        Assert.Equal(0.20, options.InitialPhonemeMismatchPenalty, 10);
        Assert.Equal(0.20, options.FinalPhonemeMismatchPenalty, 10);
    }

    [Fact]
    public void Names_IsTunedForPersonNames()
    {
        var options = DutchFuzzyOptions.Names;

        Assert.Equal(DutchPhoneticProfile.Tolerant, options.Profile);
        Assert.True(options.EncodeVowels);
        Assert.True(options.StripNamePrefixes);
        Assert.True(options.SplitGluedNamePrefixes);
    }

    [Fact]
    public void Words_UsesTheStrictProfile()
    {
        var options = DutchFuzzyOptions.Words;

        Assert.Equal(DutchPhoneticProfile.Strict, options.Profile);
        Assert.True(options.EncodeVowels);
        Assert.False(options.StripNamePrefixes);
        Assert.False(options.SplitGluedNamePrefixes);
    }

    [Fact]
    public void Presets_AreSingletons()
    {
        Assert.Same(DutchFuzzyOptions.Default, DutchFuzzyOptions.Default);
        Assert.Same(DutchFuzzyOptions.Names, DutchFuzzyOptions.Names);
        Assert.Same(DutchFuzzyOptions.Words, DutchFuzzyOptions.Words);
    }

    [Fact]
    public void WithExpression_ProducesAnIndependentCopy()
    {
        var custom = DutchFuzzyOptions.Default with { MaxKeyLength = 4 };

        Assert.Equal(4, custom.MaxKeyLength);
        Assert.Equal(0, DutchFuzzyOptions.Default.MaxKeyLength);
        Assert.NotEqual(DutchFuzzyOptions.Default, custom);
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        Assert.Equal(DutchFuzzyOptions.Default, new DutchFuzzyOptions());
        Assert.Equal(DutchFuzzyOptions.Default.GetHashCode(), new DutchFuzzyOptions().GetHashCode());
    }

    [Fact]
    public void Validate_AcceptsEveryPreset()
    {
        DutchFuzzyOptions.Default.Validate();
        DutchFuzzyOptions.Names.Validate();
        DutchFuzzyOptions.Words.Validate();
    }

    [Theory]
    [InlineData(-0.1, 0.35, 0.20)]
    [InlineData(0.45, -0.35, 0.20)]
    [InlineData(0.45, 0.35, -0.20)]
    [InlineData(0d, 0d, 0d)]
    [InlineData(double.NaN, 0.35, 0.20)]
    [InlineData(double.PositiveInfinity, 0.35, 0.20)]
    [InlineData(0.45, double.PositiveInfinity, 0.20)]
    [InlineData(0.45, 0.35, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 0.35, 0.20)]
    [InlineData(1e308, 1e308, 1e308)]
    public void Validate_RejectsInvalidWeights(double phonetic, double orthographic, double nGram)
    {
        var options = DutchFuzzyOptions.Default with
        {
            PhoneticWeight = phonetic,
            OrthographicWeight = orthographic,
            NGramWeight = nGram,
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Validate_RejectsUnknownProfiles()
    {
        var options = DutchFuzzyOptions.Default with { Profile = (DutchPhoneticProfile)42 };

        Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Throws<InvalidOperationException>(() => DutchFuzzy.PhoneticKey("Kuipers", options));
    }

    [Fact]
    public void Validate_RejectsNonFiniteThresholdsFloorsAndPenalties()
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { DefaultThreshold = value }).Validate);
            Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { IdenticalKeyFloor = value }).Validate);
            Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { InitialPhonemeMismatchPenalty = value }).Validate);
            Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { FinalPhonemeMismatchPenalty = value }).Validate);
        }
    }

    [Fact]
    public void Score_StaysFiniteForEveryValidWeightCombination()
    {
        var options = DutchFuzzyOptions.Default with
        {
            OrthographicWeight = 1e300,
            PhoneticWeight = 1e300,
            NGramWeight = 0d,
        };

        var score = DutchFuzzy.Similarity("Meijer", "Meier", options);

        Assert.False(double.IsNaN(score));
        Assert.InRange(score, 0d, 1d);
    }

    [Fact]
    public void Validate_RejectsNegativeMaxKeyLength()
    {
        Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { MaxKeyLength = -1 }).Validate);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Validate_RejectsOutOfRangeFloorAndThreshold(double value)
    {
        Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { IdenticalKeyFloor = value }).Validate);
        Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { DefaultThreshold = value }).Validate);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1d)]
    [InlineData(1.1)]
    public void Validate_RejectsOutOfRangeAnchorPenalties(double penalty)
    {
        Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { InitialPhonemeMismatchPenalty = penalty }).Validate);
        Assert.Throws<InvalidOperationException>((DutchFuzzyOptions.Default with { FinalPhonemeMismatchPenalty = penalty }).Validate);
    }

    [Fact]
    public void Validate_IsCalledByConsumers()
    {
        var invalid = DutchFuzzyOptions.Default with { PhoneticWeight = -1d };

        Assert.Throws<InvalidOperationException>(() => new DutchFuzzyScorer(invalid));
    }
}
