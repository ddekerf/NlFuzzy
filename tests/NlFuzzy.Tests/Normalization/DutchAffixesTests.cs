using NlFuzzy.Normalization;

namespace NlFuzzy.Tests.Normalization;

public class DutchAffixesTests
{
    [Theory]
    [InlineData("van")]
    [InlineData("de")]
    [InlineData("den")]
    [InlineData("der")]
    [InlineData("ten")]
    [InlineData("ter")]
    [InlineData("t")]
    [InlineData("s")]
    public void IsParticle_RecognisesDutchParticles(string token)
    {
        Assert.True(DutchAffixes.IsParticle(token));
    }

    [Theory]
    [InlineData("broeke")]
    [InlineData("kuipers")]
    [InlineData("ver")]
    [InlineData(null)]
    public void IsParticle_RejectsOtherTokens(string? token)
    {
        Assert.False(DutchAffixes.IsParticle(token!));
    }

    [Theory]
    [InlineData("van den broeke", "broeke van den")]
    [InlineData("van der meulen", "meulen van der")]
    [InlineData("de smet", "smet de")]
    [InlineData("broeke van den", "broeke van den")]
    [InlineData("kuipers", "kuipers")]
    [InlineData("", "")]
    public void MovePrefixesToEnd_RewritesLeadingParticles(string input, string expected)
    {
        Assert.Equal(expected, DutchAffixes.MovePrefixesToEnd(input));
    }

    [Fact]
    public void MovePrefixesToEnd_NeverConsumesTheLastToken()
    {
        Assert.Equal("van de", DutchAffixes.MovePrefixesToEnd("van de"));
    }

    [Fact]
    public void MovePrefixesToEnd_StopsAfterThreeParticles()
    {
        Assert.Equal("het beeck van de op", DutchAffixes.MovePrefixesToEnd("van de op het beeck"));
    }

    [Fact]
    public void MovePrefixesToEnd_HonoursGluedOnlyParticles()
    {
        Assert.Equal("brugge ver", DutchAffixes.MovePrefixesToEnd("ver brugge", includeGluedOnlyParticles: true));
        Assert.Equal("ver brugge", DutchAffixes.MovePrefixesToEnd("ver brugge", includeGluedOnlyParticles: false));
    }

    [Theory]
    [InlineData("vandenbroecke", "van den broecke")]
    [InlineData("vanderbrugge", "van der brugge")]
    [InlineData("vandemoortele", "van de moortele")]
    [InlineData("verbrugghe", "van der brugghe")]
    [InlineData("vanhee", "van hee")]
    [InlineData("opdebeeck", "op de beeck")]
    [InlineData("indenberg", "in den berg")]
    [InlineData("desmet", "de smet")]
    [InlineData("van", "van")]
    [InlineData("de", "de")]
    [InlineData("kuipers", "kuipers")]
    [InlineData("", "")]
    public void SplitGluedPrefixes_SplitsKnownPrefixes(string input, string expected)
    {
        Assert.Equal(expected, DutchAffixes.SplitGluedPrefixes(input));
    }

    [Fact]
    public void SplitGluedPrefixes_LeavesShortStemsAlone()
    {
        Assert.Equal("vanel", DutchAffixes.SplitGluedPrefixes("vanel"));
    }

    [Fact]
    public void SplitGluedPrefixes_HandlesEveryToken()
    {
        Assert.Equal("jan van den broecke", DutchAffixes.SplitGluedPrefixes("jan vandenbroecke"));
    }
}
