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

    [Theory]
    [InlineData("dekker")]
    [InlineData("decker")]
    [InlineData("dekkers")]
    [InlineData("vanniekerk")]
    [InlineData("terra")]
    public void SplitGluedPrefixes_DoesNotSplitOffAStemThatNoDutchWordStartsWith(string token)
    {
        // Doubled consonants and "ck" never start a Dutch word, so "de|kker", "de|cker",
        // "van|niekerk" is fine but "ter|ra" is not.
        var expected = token == "vanniekerk" ? "van niekerk" : token;

        Assert.Equal(expected, DutchAffixes.SplitGluedPrefixes(token));
    }

    [Theory]
    [InlineData("devos", "de vos")]
    [InlineData("dewit", "de wit")]
    [InlineData("debruyne", "de bruyne")]
    [InlineData("desmet", "de smet")]
    [InlineData("vanhee", "van hee")]
    [InlineData("verbrugghe", "van der brugghe")]
    [InlineData("vandenbroecke", "van den broecke")]
    public void SplitGluedPrefixes_StillSplitsGluedSurnames(string token, string expected)
    {
        Assert.Equal(expected, DutchAffixes.SplitGluedPrefixes(token));
    }

    [Theory]
    [InlineData("vanessa")]
    [InlineData("dennis")]
    [InlineData("denise")]
    [InlineData("delia")]
    [InlineData("verena")]
    [InlineData("veronique")]
    [InlineData("tenzing")]
    [InlineData("deventer")]
    [InlineData("dendermonde")]
    [InlineData("tervuren")]
    [InlineData("derksen")]
    [InlineData("denissen")]
    public void SplitGluedPrefixes_NeverSplitsListedNames(string token)
    {
        Assert.Equal(token, DutchAffixes.SplitGluedPrefixes(token));
        Assert.Equal(token, DutchTextNormalizer.Instance.Normalize(token, DutchFuzzyOptions.Names));
    }

    [Fact]
    public void SplitGluedPrefixes_NeverSplitListOnlyHoldsTokensThatWouldOtherwiseSplit()
    {
        // A listed token that the rules would not split anyway is dead weight; keep the list honest.
        foreach (var token in DutchAffixes.NeverSplitTokens)
        {
            Assert.NotEqual(token, DutchAffixes.SplitGluedPrefixesIgnoringExceptions(token));
        }
    }

    [Theory]
    [InlineData("dhondt", "de hondt")]
    [InlineData("dhaene", "de haene")]
    [InlineData("dhooghe", "de hooghe")]
    [InlineData("dhr", "dhr")]
    [InlineData("dhont", "de hont")]
    [InlineData("dhae", "dhae")]
    public void SplitGluedPrefixes_SplitsAGluedDhAsTheParticleDe(string token, string expected)
    {
        Assert.Equal(expected, DutchAffixes.SplitGluedPrefixes(token));
    }

    [Fact]
    public void Normalize_SplitsAGluedDhOnlyWhenGluedSplittingIsEnabled()
    {
        Assert.Equal("hondt de", DutchTextNormalizer.Instance.Normalize("Dhondt", DutchFuzzyOptions.Names));
        Assert.Equal("dhondt", DutchTextNormalizer.Instance.Normalize("Dhondt", DutchFuzzyOptions.Default));
    }

    [Fact]
    public void SplitGluedPrefixes_FallsBackToAShorterPrefixWhenALongerOneIsRejected()
    {
        // "vande" leaves "llen", which is rejected; "van" leaves "dellen", which is fine.
        Assert.Equal("van dellen", DutchAffixes.SplitGluedPrefixes("vandellen"));
    }
}
