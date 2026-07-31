using NlFuzzy.Phonetics;

namespace NlFuzzy.Tests.Phonetics;

public class DutchPhoneticEncoderTests
{
    private static readonly DutchPhoneticEncoder Tolerant = new(DutchFuzzyOptions.Default);
    private static readonly DutchPhoneticEncoder Strict = new(DutchFuzzyOptions.Default with { Profile = DutchPhoneticProfile.Strict });
    private static readonly DutchPhoneticEncoder Names = new(DutchFuzzyOptions.Names);

    [Theory]
    // Krols: v/w merge in the tolerant profile.
    [InlineData("vrede", "wrede")]
    // Krols: uy/ui and cks/ks.
    [InlineData("luycks", "luiks")]
    [InlineData("meijer", "meier")]
    [InlineData("meijer", "meyer")]
    [InlineData("bruijn", "bruyn")]
    [InlineData("smit", "smid")]
    [InlineData("bosch", "bos")]
    [InlineData("theo", "teo")]
    [InlineData("philips", "filips")]
    [InlineData("gijsbrecht", "gijsbregt")]
    [InlineData("koning", "coning")]
    [InlineData("quirijn", "kwirijn")]
    [InlineData("xander", "ksander")]
    [InlineData("cypres", "sijpres")]
    public void Encode_ProducesTheSameKeyForEquivalentSpellings(string a, string b)
    {
        Assert.Equal(Tolerant.Encode(a), Tolerant.Encode(b));
    }

    [Theory]
    [InlineData("vandenbroecke", "vandenberghe")]
    [InlineData("jansen", "hansen")]
    [InlineData("maes", "moes")]
    [InlineData("bakker", "dekker")]
    public void Encode_ProducesDifferentKeysForDistinctNames(string a, string b)
    {
        Assert.NotEqual(Names.Encode(a), Names.Encode(b));
    }

    [Theory]
    [InlineData("baobab")]
    [InlineData("china")]
    [InlineData("shampoo")]
    [InlineData("sjamaan")]
    [InlineData("tjokvol")]
    [InlineData("chocolade")]
    [InlineData("psychologie")]
    [InlineData("gezellig")]
    public void Encode_IsStableAndNonEmptyForTrickyWords(string value)
    {
        var key = Tolerant.Encode(value);

        Assert.False(string.IsNullOrEmpty(key));
        Assert.Equal(key, Tolerant.Encode(value));
    }

    [Fact]
    public void Encode_IsCaseAndDiacriticInsensitive()
    {
        Assert.Equal(Tolerant.Encode("Coordinatie"), Tolerant.Encode("coördinatie"));
        Assert.Equal(Tolerant.Encode("KUIPERS"), Tolerant.Encode("kuipers"));
        Assert.Equal(Tolerant.Encode("Céline"), Tolerant.Encode("celine"));
    }

    [Fact]
    public void Encode_ReturnsEmptyForEmptyInput()
    {
        Assert.Equal(string.Empty, Tolerant.Encode(string.Empty));
        Assert.Equal(string.Empty, Tolerant.Encode("   "));
        Assert.Empty(Tolerant.EncodeTokens(string.Empty));
    }

    [Fact]
    public void Encode_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => Tolerant.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => Tolerant.EncodeTokens(null!));
        Assert.Throws<ArgumentNullException>(() => new DutchPhoneticEncoder(null!));
    }

    [Fact]
    public void EncodeTokens_ReturnsOneKeyPerToken()
    {
        var tokens = Tolerant.EncodeTokens("van den broecke");

        Assert.Equal(3, tokens.Count);
        Assert.Equal(string.Join(" ", tokens), Tolerant.Encode("van den broecke"));
    }

    [Fact]
    public void StrictProfile_KeepsVoicedAndVoicelessApart()
    {
        Assert.NotEqual(Strict.Encode("vader"), Strict.Encode("wader"));
        Assert.Equal(Tolerant.Encode("vader"), Tolerant.Encode("wader"));
    }

    [Fact]
    public void StrictProfile_KeepsSAndZApart()
    {
        Assert.NotEqual(Strict.Encode("zee"), Strict.Encode("see"));
        Assert.Equal(Tolerant.Encode("zee"), Tolerant.Encode("see"));
    }

    [Fact]
    public void StrictProfile_KeepsDAndTApart()
    {
        Assert.NotEqual(Strict.Encode("doren"), Strict.Encode("toren"));
        Assert.Equal(Tolerant.Encode("doren"), Tolerant.Encode("toren"));
    }

    [Fact]
    public void BothProfiles_TreatChAndGAlike()
    {
        // ch and g spell the same Dutch velar fricative, in both profiles.
        Assert.Equal(Strict.Encode("gijsbrecht"), Strict.Encode("gijsbregt"));
        Assert.Equal(Tolerant.Encode("gijsbrecht"), Tolerant.Encode("gijsbregt"));
    }

    [Fact]
    public void EncodeVowels_ChangesTheKey()
    {
        var withVowels = new DutchPhoneticEncoder(DutchFuzzyOptions.Default with { EncodeVowels = true });
        var withoutVowels = new DutchPhoneticEncoder(DutchFuzzyOptions.Default with { EncodeVowels = false });

        Assert.NotEqual(withVowels.Encode("maes"), withVowels.Encode("moes"));
        Assert.Equal(withoutVowels.Encode("maes"), withoutVowels.Encode("moes"));
    }

    [Fact]
    public void MaxKeyLength_TruncatesEachTokenKey()
    {
        var encoder = new DutchPhoneticEncoder(DutchFuzzyOptions.Default with { MaxKeyLength = 3 });

        foreach (var token in encoder.EncodeTokens("vandenbroecke schoenmakers"))
        {
            Assert.True(token.Length <= 3, $"'{token}' exceeds MaxKeyLength.");
        }
    }

    [Fact]
    public void CollapseRepeats_RemovesDoubledConsonants()
    {
        Assert.Equal(Tolerant.Encode("bakker"), Tolerant.Encode("baker"));
        Assert.Equal(Tolerant.Encode("bosschaert"), Tolerant.Encode("boschaert"));
    }

    [Fact]
    public void Encode_UsesOnlyTheDocumentedAlphabet()
    {
        const string Alphabet = "PBTDKGFVWSZXJLMNRHQ1234567 ";
        var corpus = new[]
        {
            "vandenbroecke", "schoenmakers", "gijsbrecht", "quirijn", "xander", "psychologie",
            "tsjechov", "baobab", "shampoo", "sjamaan", "tjokvol", "chocolade", "meeuwis",
            "nieuwenhuis", "oosterhuis", "'s-hertogenbosch", "van 't hoff",
        };

        var encoder = new DutchPhoneticEncoder(DutchFuzzyOptions.Default with { EncodeVowels = true });

        foreach (var word in corpus)
        {
            foreach (var c in encoder.Encode(word))
            {
                Assert.True(Alphabet.Contains(c), $"'{word}' produced unexpected symbol '{c}'.");
            }
        }
    }

    [Fact]
    public void IsVowelClass_RecognizesDigits()
    {
        Assert.True(DutchPhoneticEncoder.IsVowelClass('1'));
        Assert.True(DutchPhoneticEncoder.IsVowelClass('7'));
        Assert.False(DutchPhoneticEncoder.IsVowelClass('8'));
        Assert.False(DutchPhoneticEncoder.IsVowelClass('K'));
    }

    [Fact]
    public void Default_UsesDefaultOptions()
    {
        Assert.Equal(DutchPhoneticEncoder.Default.Encode("kuipers"), Tolerant.Encode("kuipers"));
    }
}
