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
    [InlineData("D'Hondt", "hondt de")]
    [InlineData("D’Haene", "haene de")]
    [InlineData("D‘Haene", "haene de")]
    [InlineData("D`Haene", "haene de")]
    [InlineData("D´Haene", "haene de")]
    [InlineData("Marie D'Hooghe", "marie de hooghe")]
    [InlineData("Van D'Hondt", "hondt van de")]
    public void Normalize_ReadsAnElidedDAsTheParticleDe(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, DutchFuzzyOptions.Default));
    }

    [Fact]
    public void Normalize_ElidedDMatchesTheSeparatedForm()
    {
        Assert.Equal(
            Normalizer.Normalize("De Hondt", DutchFuzzyOptions.Names),
            Normalizer.Normalize("D'Hondt", DutchFuzzyOptions.Names));
    }

    [Theory]
    [InlineData("D. Jansen", "d jansen")]
    [InlineData("A.D. Jansen", "a d jansen")]
    [InlineData("Ad'Hondt", "ad hondt")]
    public void Normalize_DoesNotReadAnInitialDAsAnArticle(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, Plain));
    }

    [Fact]
    public void Normalize_DoesNotExpandAShortElision()
    {
        Assert.Equal("d r", Normalizer.Normalize("d'r", DutchFuzzyOptions.Words));
    }

    [Theory]
    [InlineData("d'accord", "d accord")]
    [InlineData("D'Hondt", "d hondt")]
    public void Normalize_KeepsTheApostropheAsASeparatorForOrdinaryWords(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, DutchFuzzyOptions.Words));
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

    private static readonly DutchFuzzyOptions KeepAccents = Plain with { RemoveDiacritics = false };

    [Theory]
    [InlineData("Ren\u00e9e", "ren\u00e9e")]
    [InlineData("C\u00e9line", "c\u00e9line")]
    [InlineData("Fran\u00e7ois", "fran\u00e7ois")]
    [InlineData("M\u00fcller", "m\u00fcller")]
    [InlineData("H\u00c9L\u00c8NE", "h\u00e9l\u00e8ne")]
    public void Normalize_KeepsAccentedLettersWhenFoldingIsDisabled(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, KeepAccents));
    }

    [Theory]
    [InlineData('é', true)]
    [InlineData('ü', true)]
    [InlineData('ç', true)]
    [InlineData('ñ', true)]
    [InlineData('å', true)]
    [InlineData('É', true)]
    [InlineData('ł', false)]
    [InlineData('ø', false)]
    [InlineData('ω', false)]
    [InlineData('ạ', false)]
    [InlineData('e', false)]
    [InlineData('ß', false)]
    public void IsKeptAccentedLetter_KeepsOnlyAccentedLatinLetters(char c, bool expected)
    {
        Assert.Equal(expected, DutchTextNormalizer.IsKeptAccentedLetter(c));
    }

    [Fact]
    public void IsKeptAccentedLetter_TableMatchesTheRule()
    {
        for (var c = 'À'; c <= 'ɏ'; c++)
        {
            Assert.True(
                DutchTextNormalizer.DecomposesToAccentedLatinLetter(c) == DutchTextNormalizer.IsKeptAccentedLetter(c),
                $"U+{(int)c:X4} differs between the table and the rule.");
        }
    }

    [Fact]
    public void Normalize_ComposesDecomposedInputWhenFoldingIsDisabled()
    {
        Assert.Equal("ren\u00e9e", Normalizer.Normalize("Rene\u0301e", KeepAccents));
        Assert.Equal("ren\u00e9e", Normalizer.Normalize("Ren\u00e9e", KeepAccents));
    }

    [Theory]
    [InlineData("Jan \u0141ukasz")]
    [InlineData("\u03a9mega")]
    [InlineData("Stra\u00dfe")]
    [InlineData("\u0132sselmeer")]
    [InlineData("Kuipers 123")]
    public void Normalize_TreatsOtherCharactersAlikeWithAndWithoutFolding(string input)
    {
        Assert.Equal(Normalizer.Normalize(input, Plain), Normalizer.Normalize(input, KeepAccents));
    }

    [Fact]
    public void Normalize_ExpandsSpecialLettersWhenFoldingIsDisabled()
    {
        Assert.Equal("strasse", Normalizer.Normalize("Stra\u00dfe", KeepAccents));
        Assert.Equal("ijsselmeer", Normalizer.Normalize("\u0132sselmeer", KeepAccents));
    }

    [Theory]
    [InlineData("dé Vries", "vries dé", "vries de")]
    [InlineData("Vándenbroecke", "broecke van den", "broecke van den")]
    [InlineData("ď'Hondt", "hondt de", "hondt de")]
    [InlineData("Dhóndt", "hóndt de", "hondt de")]
    public void Normalize_RecognisesAccentedParticlesWhenFoldingIsDisabled(string input, string kept, string folded)
    {
        var names = DutchFuzzyOptions.Names;

        Assert.Equal(kept, Normalizer.Normalize(input, names with { RemoveDiacritics = false }));
        Assert.Equal(folded, Normalizer.Normalize(input, names));
        Assert.Equal(DutchFuzzy.PhoneticKey(input, names), DutchFuzzy.PhoneticKey(input, names with { RemoveDiacritics = false }));
    }

    [Fact]
    public void IsParticle_IgnoresAccents()
    {
        Assert.True(DutchAffixes.IsParticle("dé"));
        Assert.True(DutchAffixes.IsParticle("ván"));
        Assert.False(DutchAffixes.IsParticle("vries"));
    }

    [Theory]
    [InlineData("ǽ", "ae")]
    [InlineData("ḱ", "k")]
    [InlineData("t́", "t")]
    [InlineData("ạ", "a")]
    public void Normalize_FoldsCharactersItDoesNotKeep(string input, string expected)
    {
        Assert.Equal(expected, Normalizer.Normalize(input, KeepAccents));
        Assert.Equal(expected, Normalizer.Normalize(input, Plain));
    }

    [Fact]
    public void Normalize_ExpandsTheElidedArticleBeforeAnAccentedName()
    {
        var names = DutchFuzzyOptions.Names;

        Assert.Equal("\u00e9t\u00e9 de", Normalizer.Normalize("d'\u00c9t\u00e9", names with { RemoveDiacritics = false }));
        Assert.Equal("ete de", Normalizer.Normalize("d'\u00c9t\u00e9", names));
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
