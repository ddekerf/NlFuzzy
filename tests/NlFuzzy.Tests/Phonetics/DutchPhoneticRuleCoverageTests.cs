using System.Collections.Generic;
using System.Linq;
using NlFuzzy.Phonetics;

namespace NlFuzzy.Tests.Phonetics;

/// <summary>
/// Acceptance criterion from the plan: every rule in the normative rule table must be exercised by
/// at least one corpus word, in both profiles.
/// </summary>
public class DutchPhoneticRuleCoverageTests
{
    private static readonly string[] Corpus =
    [
        // Names.
        "Jansen", "De Jong", "Van den Berg", "Bakker", "Janssen", "Visser", "Smit", "Meijer",
        "De Boer", "Mulder", "De Groot", "Bos", "Bosch", "Vos", "Peters", "Hendriks", "Van Leeuwen",
        "Dekker", "Brouwer", "De Wit", "Dijkstra", "Smits", "De Graaf", "Van der Meer", "Van der Linden",
        "Kok", "Jacobs", "Vermeulen", "Van den Heuvel", "Van der Veen", "Van den Broek", "De Bruijn",
        "De Bruyn", "Van der Heijden", "Schouten", "Van Beek", "Willems", "Van Vliet", "Van de Ven",
        "Hoekstra", "Maas", "Verhoeven", "Koster", "Van Dijk", "Prins", "Blom", "Huisman", "Peeters",
        "De Haan", "Kuipers", "Van Veen", "Post", "Kuiper", "Veenstra", "Kramer", "Van den Brink",
        "Scholten", "Van Wijk", "Postma", "Martens", "Vink", "De Ruiter", "Timmermans", "Groen",
        "Gerritsen", "Jonker", "Van Loon", "Boer", "Van der Wal", "Dijksterhuis", "Sijmons", "Simons",
        "Schoenmakers", "Schoonmakers", "Vandenbroecke", "Vandenberghe", "Desmet", "De Smet",
        "Verbrugghe", "Van der Brugghe", "Ghijs", "Cornelis", "Kornelis", "Philips", "Filips",
        "Theodoor", "Teodoor", "Quirijn", "Kwirijn", "Xander", "Ksander", "Cypres", "Sijpres",
        "Luycks", "Luiks", "Nieuwenhuis", "Meeuwis", "Tsjechov", "Viehoff", "Behrens", "Kuhn",
        "Ooyen", "Kaaij", "Kaay", "Qadir", "Johan", "Alhier", "Ghyselen", "Van 't Hoff",
        "'s-Hertogenbosch", "Van Aaij", "Verhaeghe", "Beaumont", "Michielsen", "Jacques",
        "Rijkaard", "Ruyter", "Zeeuw", "Leeuwen", "Nooijen", "Mooij", "Aaij", "Hooijmaijers",
        // Words.
        "school", "skool", "scheiden", "schrijven", "wetenschap", "chocolade", "china", "chirurg",
        "psychologie", "sjaal", "sjamaan", "shampoo", "tjokvol", "baobab", "gezellig", "vrede",
        "wrede", "koning", "zingen", "zing", "bang", "banken", "cadeau", "niveau", "quiz",
        "quotum", "extra", "examen", "taxi", "auto", "mouw", "nieuw", "eeuw", "duw", "ruw",
        "aai", "mooi", "hooi", "draaien", "kraaien", "vrouw", "blauw", "goud", "koud", "hout",
        "licht", "nacht", "recht", "zicht", "vlucht", "gracht", "haring", "honing", "woning",
        "tien", "zien", "dier", "bier", "vier", "hier", "meer", "keer", "deur", "kleur",
        "huis", "muis", "buis", "kuil", "tuin", "bruin", "geel", "meel", "veel", "heel",
        "boek", "boer", "koe", "moe", "roef", "poes", "hoed", "goed", "voet", "zoet",
        "ijs", "wijs", "prijs", "rijst", "eind", "trein", "klein", "geit", "feit", "reis",
        "thee", "thuis", "thans", "ritme", "rytme", "syntaxis", "systeem", "typisch", "cijfer",
        "cent", "circus", "citroen", "cel", "cirkel", "coach", "club", "computer", "creatie",
        "gnoom", "knoop", "knie", "wraak", "wrat", "zwart", "zwaar", "twee", "tweede",
        "dorp", "werk", "berg", "burg", "dijk", "brug", "veld", "bosch", "heide", "duin",
        "graaf", "gravin", "leeg", "leger", "vraag", "vragen", "dag", "dagen", "weg", "wegen",
        "hand", "handen", "kind", "kinderen", "hond", "honden", "band", "banden", "wind",
        "lief", "lieve", "brief", "brieven", "half", "halve", "wolf", "wolven", "golf",
        "huis", "huizen", "reis", "reizen", "vies", "vieze", "gras", "grazen", "glas", "glazen",
        "yoghurt", "yoga", "royaal", "loyaal", "mayonaise", "ampersand",
        "Brandt", "De Smedt", "Meyer", "Kahn", "Dahlman", "uur", "muur", "duur", "vuur", "buurt",
        "Blaauw", "Blauw",
    ];

    [Theory]
    [InlineData(DutchPhoneticProfile.Tolerant)]
    [InlineData(DutchPhoneticProfile.Strict)]
    public void EveryRuleIsExercisedByTheCorpus(DutchPhoneticProfile profile)
    {
        var options = DutchFuzzyOptions.Default with { Profile = profile, EncodeVowels = true };
        var encoder = new DutchPhoneticEncoder(options);
        var used = new HashSet<int>();

        foreach (var word in Corpus)
        {
            encoder.EncodeTokens(word, used);
        }

        var missing = DutchPhoneticRules.Rules
            .Where(rule => !used.Contains(rule.Id))
            .Select(rule => $"#{rule.Id} '{rule.Pattern}' ({rule.Description})")
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} rule(s) were never applied in profile {profile}:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    [Fact]
    public void RuleIdsAreUniqueAndContiguous()
    {
        var ids = DutchPhoneticRules.Rules.Select(rule => rule.Id).OrderBy(id => id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(Enumerable.Range(ids[0], ids.Count), ids);
    }

    [Fact]
    public void EveryRuleHasAPatternAndDescription()
    {
        foreach (var rule in DutchPhoneticRules.Rules)
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Pattern));
            Assert.False(string.IsNullOrWhiteSpace(rule.Description));
        }
    }

    [Fact]
    public void FindMatch_PrefersTheLongestPattern()
    {
        var match = DutchPhoneticRules.FindMatch("schip", 0, DutchPhoneticProfile.Tolerant);

        Assert.NotNull(match);
        Assert.Equal("sch", match!.Pattern);
    }
}
