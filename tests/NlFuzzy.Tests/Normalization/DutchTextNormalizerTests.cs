using NlFuzzy;
using NlFuzzy.Normalization;

namespace NlFuzzy.Tests.Normalization;

public class DutchTextNormalizerTests
{
    private static readonly DutchTextNormalizer Normalizer = DutchTextNormalizer.Instance;

    private static readonly DutchFuzzyOptions Plain = DutchFuzzyOptions.Default with
    {
        StripNamePrefixes = false,
        SplitGluedNamePrefixes = false
    };

    [Theory]
    [InlineData("Bruijn", "bruijn")]
    [InlineData("  Bruijn  ", "bruijn")]
    [InlineData("BRUIJN", "bruijn")]
    [InlineData("Jan Willem", "jan willem")]
    [InlineData("Jan   Willem", "jan willem")]
    [InlineData("Sint-Niklaas", "sint niklaas")]
    [InlineData("Bergen op Zoom", "bergen op zoom")]
    [InlineData("D'Hondt", "d hondt")]
    [InlineData("D\u2019Hondt", "d hondt")]
    [InlineData("O'Brien", "o brien")]
    [InlineData("Jan, Willem", "jan willem")]
    [InlineData("A.J. Kuipers", "a j kuipers")]
    [InlineData("Kuipers 123", "kuipers")]
    [InlineData("Ge\u00efntegreerd", "geintegreerd")]
    [InlineData("Ren\u00e9e", "renee")]
    [InlineData("Fran\u00e7ois", "francois")]
    [InlineData("\u00c9mile Z\u00f6ller", "emile zoller")]
    [InlineData("Stra\u00dfe", "strasse")]
    [InlineData("\u00c6gir", "aegir")]
    [InlineData("\u0152uvre", "oeuvre")]
    [InlineData("S\u00f8ren", "soren")]
    [InlineData("\u0110uro", "duro")]
    [InlineData("\u0133sbrand", "ijsbrand")]
    [InlineData("\u0132sbrand", "ijsbrand")]
    [InlineData("A\u00f1o", "ano")]
    [InlineData("STRA\u1E9EE", "strasse")]
    [InlineData("\u1E9E", "ss")]
    [InlineData("\u01FDgir", "aegir")]
    [InlineData("\u01FCGIR", "aegir")]
    [InlineData("\u01FFslo", "oslo")]
    [InlineData("Rene\u0301e", "renee")]
    [InlineData("Zo\u0308ller", "zoller")]
    public void Normalize_CleansInput(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, Plain));
    }

    [Theory]
    [InlineData("Ren\u00e9e", "Rene\u0301e")]
    [InlineData("Z\u00f6ller", "Zo\u0308ller")]
    [InlineData("Ge\u00efntegreerd", "Gei\u0308ntegreerd")]
    public void Normalize_TreatsCanonicallyEquivalentInputTheSame(string composed, string decomposed)
    {
        Assert.Equal(
            Normalizer.Normalize(composed, Plain),
            Normalizer.Normalize(decomposed, Plain));

        Assert.Equal(DutchFuzzy.PhoneticKey(composed), DutchFuzzy.PhoneticKey(decomposed));
    }

    [Theory]
    [InlineData("STRA\u1E9EE", "Strasse")]
    [InlineData("\u01FDgir", "Aegir")]
    public void Normalize_ExpandsCharactersThatOnlyAppearAfterFolding(string input, string equivalent)
    {
        Assert.Equal(
            Normalizer.Normalize(equivalent, Plain),
            Normalizer.Normalize(input, Plain));
    }

    [Theory]
    [InlineData("'s-Hertogenbosch", "s hertogenbosch")]
    [InlineData("'t Zand", "t zand")]
    public void Normalize_HandlesApostropheForms(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, Plain));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("!!!")]
    public void Normalize_ReturnsEmptyForBlankInput(string input)
    {
        Assert.Equal(string.Empty, Normalizer.Normalize(input, Plain));
    }

    [Fact]
    public void Normalize_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => Normalizer.Normalize(null!, Plain));
        Assert.Throws<ArgumentNullException>(() => Normalizer.Normalize("kuipers", null!));
    }

    [Fact]
    public void Normalize_KeepsDiacriticsWhenDisabled()
    {
        var options = Plain with { RemoveDiacritics = false };

        // The diacritic itself is not a letter a-z, so it is dropped by the letter filter
        // instead of being folded onto its base letter.
        Assert.Equal("ren e", Normalizer.Normalize("Ren\u00e9e", options));
    }

    [Theory]
    [InlineData("Van den Broeke", "broeke van den")]
    [InlineData("van der Meulen", "meulen van der")]
    [InlineData("De Smet", "smet de")]
    [InlineData("Ten Kate", "kate ten")]
    [InlineData("Op de Beeck", "beeck op de")]
    [InlineData("In het Veld", "veld in het")]
    [InlineData("'s-Hertogenbosch", "hertogenbosch s")]
    [InlineData("Van 't Hof", "hof van t")]
    [InlineData("Kuipers", "kuipers")]
    [InlineData("Van", "van")]
    public void Normalize_MovesNamePrefixesToEnd(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, DutchFuzzyOptions.Default));
    }

    [Theory]
    [InlineData("Vandenbroecke", "broecke van den")]
    [InlineData("Vanderbrugge", "brugge van der")]
    [InlineData("Verbrugghe", "brugghe van der")]
    [InlineData("Vandemoortele", "moortele van de")]
    [InlineData("Desmet", "smet de")]
    [InlineData("Opdebeeck", "beeck op de")]
    public void Normalize_SplitsGluedPrefixes(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, DutchFuzzyOptions.Names));
    }

    [Fact]
    public void Normalize_DoesNotSplitGluedPrefixesByDefault()
    {
        Assert.Equal("vandenbroecke", Normalizer.Normalize("Vandenbroecke", DutchFuzzyOptions.Default));
    }

    [Fact]
    public void Normalize_DoesNotSplitShortStems()
    {
        Assert.Equal("vanel", Normalizer.Normalize("Vanel", DutchFuzzyOptions.Names));
    }

    [Fact]
    public void Normalize_ThrowsOnNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() => Normalizer.Normalize("test", null!));
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var once = Normalizer.Normalize("Van den Broecke", DutchFuzzyOptions.Default);
        var twice = Normalizer.Normalize(once, DutchFuzzyOptions.Default);

        Assert.Equal(once, twice);
    }
}
