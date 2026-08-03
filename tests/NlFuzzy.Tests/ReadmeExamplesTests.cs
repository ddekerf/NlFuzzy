using System.Collections.Generic;
using System.Linq;
using NlFuzzy.Comparison;
using NlFuzzy.Indexing;

namespace NlFuzzy.Tests;

/// <summary>
/// Acceptance criterion 6 from the plan: every example in README.md compiles and is correct.
/// Keep this file in sync with README.md — one test per README snippet.
/// </summary>
public class ReadmeExamplesTests
{
    [Fact]
    public void Voorbeeld_3_1_FonetischeSleutel()
    {
        string sleutel = "Bruijn".ToDutchPhoneticKey();
        bool gelijk = sleutel == "Bruyn".ToDutchPhoneticKey();

        Assert.Equal("PRN", sleutel);
        Assert.True(gelijk);
    }

    [Fact]
    public void Voorbeeld_3_2_KlinktHetzelfde()
    {
        bool a = "Meijer".SoundsLikeDutch("Meier");
        bool b = "Philips".SoundsLikeDutch("Filips");
        bool c = "Bakker".SoundsLikeDutch("Dekker");

        Assert.True(a);
        Assert.True(b);
        Assert.False(c);
    }

    [Fact]
    public void Voorbeeld_3_3_Score()
    {
        double score = "Schoenmakers".DutchSimilarity("Schoonmakers");
        bool zelfde = "Meijer".DutchFuzzyEquals("Meier");

        Assert.Equal(0.936, score, 3);
        Assert.True(zelfde);
        Assert.Equal(0.82, DutchStringExtensions.DefaultThreshold, 10);
    }

    [Fact]
    public void Voorbeeld_3_4_LijstRangschikken()
    {
        var klanten = new[]
        {
            new { Id = 1, Naam = "Marie Vandenbroecke" },
            new { Id = 2, Naam = "Piet Bakker" },
        };

        var treffers = klanten.FuzzyMatchDutch(
            "Marie van den Broeke",
            k => k.Naam,
            threshold: 0.80,
            take: 5,
            options: DutchFuzzyOptions.Names).ToList();

        Assert.NotEmpty(treffers);
        Assert.Equal(1, treffers[0].Value.Id);
    }

    [Fact]
    public void Voorbeeld_3_5_Index()
    {
        var index = new DutchFuzzyIndex<string>(
            ["Anna Schoenmakers", "Klaas Visser"],
            naam => naam,
            DutchFuzzyOptions.Names);

        var resultaat = index.Search("Anna Schoenmaker", threshold: 0.70, take: 3);

        Assert.NotEmpty(resultaat);
        Assert.Equal("Anna Schoenmakers", resultaat[0].Value);
    }

    [Fact]
    public void Voorbeeld_Bonus_Ontdubbelen()
    {
        var uniek = new HashSet<string>(
            ["Meijer", "Meier", "Meyer", "Bakker"],
            DutchPhoneticEqualityComparer.Default);

        Assert.Equal(2, uniek.Count);
    }

    [Theory]
    // Section 4 of the README: covered Dutch spelling variants.
    [InlineData("Meijer", "Meier", "MR")]
    [InlineData("Meijer", "Meyer", "MR")]
    [InlineData("Bruijn", "Bruyn", "PRN")]
    [InlineData("Bruijn", "Bruin", "PRN")]
    [InlineData("Kuypers", "Kuipers", "KPRS")]
    [InlineData("Blaauw", "Blauw", "PL")]
    [InlineData("Maes", "Maas", "MS")]
    [InlineData("Dirx", "Dircks", "TRKS")]
    [InlineData("Cornelis", "Kornelis", "KRNLS")]
    [InlineData("Verbrugghe", "Verbrugge", "FRPRG")]
    [InlineData("Gijsbrecht", "Gijsbregt", "GSPRGT")]
    [InlineData("Brandt", "Brant", "PRNT")]
    [InlineData("Bosch", "Bos", "PS")]
    [InlineData("Philips", "Filips", "FLPS")]
    [InlineData("Thijs", "Tijs", "TS")]
    [InlineData("Xander", "Ksander", "KSNTR")]
    [InlineData("Quirijn", "Kwirijn", "KFRN")]
    [InlineData("Cypres", "Sijpres", "SPRS")]
    [InlineData("Luycks", "Luiks", "LKS")]
    [InlineData("Smit", "Smid", "SMT")]
    [InlineData("vrede", "wrede", "FRT")]
    public void Sectie4_SpellingvariantenTabel(string a, string b, string sleutel)
    {
        Assert.Equal(sleutel, DutchFuzzy.PhoneticKey(a));
        Assert.Equal(sleutel, DutchFuzzy.PhoneticKey(b));
    }

    [Fact]
    public void Sectie4_DiftongKlasseVoorOuEnAu()
    {
        var options = DutchFuzzyOptions.Default with { EncodeVowels = true };

        Assert.Equal(DutchFuzzy.PhoneticKey("kauw", options), DutchFuzzy.PhoneticKey("kou", options));
    }

    [Theory]
    // Section 5 of the README: Strict versus Tolerant.
    [InlineData("vrede", "VRD", "FRT")]
    [InlineData("wrede", "VRD", "FRT")]
    [InlineData("zee", "Z", "S")]
    [InlineData("see", "S", "S")]
    [InlineData("doren", "DRN", "TRN")]
    [InlineData("toren", "TRN", "TRN")]
    [InlineData("school", "SGL", "SGL")]
    [InlineData("skool", "SKL", "SGL")]
    public void Sectie5_StrictVersusTolerant(string woord, string strict, string tolerant)
    {
        var strictOptions = DutchFuzzyOptions.Default with { Profile = DutchPhoneticProfile.Strict };

        Assert.Equal(strict, DutchFuzzy.PhoneticKey(woord, strictOptions));
        Assert.Equal(tolerant, DutchFuzzy.PhoneticKey(woord));
    }

    [Theory]
    [InlineData("school", "skool", 0.900, 0.720)]
    [InlineData("Vermeulen", "Verhoeven", 0.641, 0.708)]
    public void Sectie5_ScoresPerProfiel(string a, string b, double standaard, double woorden)
    {
        Assert.Equal(standaard, DutchFuzzy.Similarity(a, b), 3);
        Assert.Equal(woorden, DutchFuzzy.Similarity(a, b, DutchFuzzyOptions.Words), 3);
    }

    [Theory]
    [InlineData("Vandenbroecke", "Van den Broecke", 0.268, 1.000)]
    [InlineData("Desmet", "De Smet", 0.451, 1.000)]
    [InlineData("Maes", "Moes", 0.900, 0.664)]
    public void Sectie5_ScoresDefaultVersusNames(string a, string b, double standaard, double namen)
    {
        Assert.Equal(standaard, DutchFuzzy.Similarity(a, b), 3);
        Assert.Equal(namen, DutchFuzzy.Similarity(a, b, DutchFuzzyOptions.Names), 3);
    }

    [Fact]
    public void Sectie5_PresetTabel()
    {
        Assert.Equal(DutchPhoneticProfile.Tolerant, DutchFuzzyOptions.Default.Profile);
        Assert.False(DutchFuzzyOptions.Default.EncodeVowels);
        Assert.True(DutchFuzzyOptions.Default.StripNamePrefixes);
        Assert.False(DutchFuzzyOptions.Default.SplitGluedNamePrefixes);

        Assert.Equal(DutchPhoneticProfile.Tolerant, DutchFuzzyOptions.Names.Profile);
        Assert.True(DutchFuzzyOptions.Names.EncodeVowels);
        Assert.True(DutchFuzzyOptions.Names.StripNamePrefixes);
        Assert.True(DutchFuzzyOptions.Names.SplitGluedNamePrefixes);

        Assert.Equal(DutchPhoneticProfile.Strict, DutchFuzzyOptions.Words.Profile);
        Assert.True(DutchFuzzyOptions.Words.EncodeVowels);
        Assert.False(DutchFuzzyOptions.Words.StripNamePrefixes);
        Assert.False(DutchFuzzyOptions.Words.SplitGluedNamePrefixes);
    }

    [Fact]
    public void Sectie5_EigenInstellingen()
    {
        var opties = DutchFuzzyOptions.Names with
        {
            MaxKeyLength = 6,
            PhoneticWeight = 0.60,
            OrthographicWeight = 0.30,
            NGramWeight = 0.10,
        };

        opties.Validate();

        Assert.All(DutchFuzzy.PhoneticKeys("Schoenmakers", opties), key => Assert.True(key.Length <= 6));
    }

    [Theory]
    [InlineData("Jansen", "Hansen", 0.846, 0.677)]
    [InlineData("Kuipers", "Kuiper", 0.897, 0.717)]
    public void Sectie5_Ankerstraffen(string a, string b, double zonder, double met)
    {
        var zonderStraf = DutchFuzzyOptions.Names with
        {
            InitialPhonemeMismatchPenalty = 0d,
            FinalPhonemeMismatchPenalty = 0d,
        };

        Assert.Equal(zonder, DutchFuzzy.Similarity(a, b, zonderStraf), 3);
        Assert.Equal(met, DutchFuzzy.Similarity(a, b, DutchFuzzyOptions.Names), 3);
    }

    [Fact]
    public void Sectie6_IndexKanZonderVolledigeScan()
    {
        var index = new DutchFuzzyIndex<string>(["Kuipers", "Bakker"], x => x, DutchFuzzyOptions.Names);

        Assert.Empty(index.Search("Zzyzx", 0.9, 10, DutchFuzzyIndexSearchMode.BlockedOnly));
    }

    [Fact]
    public void Sectie8_UitvoeralfabetIsGedocumenteerd()
    {
        const string Alfabet = "PBTDKGFVWSZXJLMNRHQ1234567 ";

        var options = DutchFuzzyOptions.Default with { EncodeVowels = true };

        foreach (var woord in new[] { "Vandenbroecke", "Schoenmakers", "'s-Hertogenbosch", "Quirijn" })
        {
            foreach (var c in DutchFuzzy.PhoneticKey(woord, options))
            {
                Assert.Contains(c, Alfabet);
            }
        }
    }
}
