using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NlFuzzy;
using NlFuzzy.Comparison;
using NlFuzzy.Indexing;
using NlFuzzy.Phonetics;

namespace NlFuzzy.Samples;

/// <summary>A guided tour of the NlFuzzy API.</summary>
internal static class Program
{
    private sealed record Klant(int Id, string Naam, string Woonplaats);

    private static readonly Klant[] Klanten =
    [
        new(1001, "Jan Meijer", "Amsterdam"),
        new(1002, "Johan Meier", "Amsterdam"),
        new(1003, "J. Meyer", "Haarlem"),
        new(1004, "Piet Bakker", "Rotterdam"),
        new(1005, "Pieter Bakkers", "Rotterdam"),
        new(1006, "Klaas Visser", "Urk"),
        new(1007, "Anna Schoenmakers", "Eindhoven"),
        new(1008, "Anna Schoonmakers", "Eindhoven"),
        new(1009, "Marie Vandenbroecke", "Gent"),
        new(1010, "Marie van den Broecke", "Gent"),
        new(1011, "Bart Desmet", "Brugge"),
        new(1012, "Bart De Smet", "Brugge"),
        new(1013, "Sofie de Bruijn", "Utrecht"),
        new(1014, "Sofie de Bruyn", "Utrecht"),
        new(1015, "Willem Kuipers", "Groningen"),
        new(1016, "Willem Kuiper", "Groningen"),
        new(1017, "Hendrik Jansen", "Arnhem"),
        new(1018, "Hendrik Hansen", "Arnhem"),
        new(1019, "Theo Gijsbrecht", "Nijmegen"),
        new(1020, "Theo Gijsbregt", "Nijmegen"),
    ];

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Header("NlFuzzy - Nederlandse fonetische en fuzzy matching");

        PhonetischeSleutels();
        KlinktAls();
        Gelijkenis();
        LijstDoorzoeken();
        IndexDoorzoeken();
        Groeperen();
        Profielen();
        EigenInstellingen();
        Prestaties();

        Header("Klaar");
    }

    /// <summary>1. Phonetic keys: what does a name actually sound like?</summary>
    private static void PhonetischeSleutels()
    {
        Header("1. Fonetische sleutels");

        foreach (var naam in new[] { "Meijer", "Meier", "Meyer", "Bosch", "Bos", "Schoenmakers", "Vandenbroecke" })
        {
            Console.WriteLine(
                $"  {naam,-16} -> Default: {naam.ToDutchPhoneticKey(),-14} Names: {naam.ToDutchPhoneticKey(DutchFuzzyOptions.Names)}");
        }

        Console.WriteLine();
        Console.WriteLine("  Sleutel per token (preset Words):");

        foreach (var key in DutchFuzzy.PhoneticKeys("Van den Broecke", DutchFuzzyOptions.Words))
        {
            Console.WriteLine($"    {key}");
        }
    }

    /// <summary>2. SoundsLike: do two strings collapse onto the same key?</summary>
    private static void KlinktAls()
    {
        Header("2. Klinkt hetzelfde?");

        var paren = new[]
        {
            ("Meijer", "Meier"),
            ("Bruijn", "Bruyn"),
            ("Philips", "Filips"),
            ("Smit", "Smid"),
            ("Bakker", "Dekker"),
            ("Jansen", "Hansen"),
        };

        foreach (var (links, rechts) in paren)
        {
            var gelijk = links.SoundsLikeDutch(rechts);

            Console.WriteLine(
                $"  {links,-10} ~ {rechts,-10} {(gelijk ? "ja " : "nee")}  ({links.ToDutchPhoneticKey()} / {rechts.ToDutchPhoneticKey()})");
        }
    }

    /// <summary>3. A graded similarity score instead of a boolean.</summary>
    private static void Gelijkenis()
    {
        Header("3. Gelijkenisscore (0..1)");

        var paren = new[]
        {
            ("Vandenbroecke", "Van den Broecke"),
            ("Schoenmakers", "Schoonmakers"),
            ("Sijmons", "Simons"),
            ("Antwerpen", "Antwerpn"),
            ("Kuipers", "Kuiper"),
            ("Vermeulen", "Verhoeven"),
        };

        foreach (var (links, rechts) in paren)
        {
            var score = links.DutchSimilarity(rechts, DutchFuzzyOptions.Names);

            Console.WriteLine($"  {links,-16} ~ {rechts,-18} {Format(score)}  {Balk(score)}");
        }
    }

    /// <summary>4. Ranking an in-memory list.</summary>
    private static void LijstDoorzoeken()
    {
        Header("4. Een lijst doorzoeken");

        const string Zoekterm = "Marie Van den Broeke";

        Console.WriteLine($"  Zoekterm: \"{Zoekterm}\"");
        Console.WriteLine();

        foreach (var treffer in Klanten.FuzzyMatchDutch(Zoekterm, k => k.Naam, threshold: 0.70, take: 5, DutchFuzzyOptions.Names))
        {
            Console.WriteLine($"  {Format(treffer.Score)}  #{treffer.Value.Id}  {treffer.Value.Naam,-24} [{treffer.PhoneticKey}]");
        }

        var beste = Klanten.BestDutchMatch("Sofie de Bruin", k => k.Naam, threshold: 0.70, DutchFuzzyOptions.Names);

        Console.WriteLine();
        Console.WriteLine(beste is null
            ? "  Geen beste treffer voor \"Sofie de Bruin\"."
            : $"  Beste treffer voor \"Sofie de Bruin\": {beste.Value.Value.Naam} ({Format(beste.Value.Score)})");
    }

    /// <summary>5. The bucketed index for repeated queries over the same data.</summary>
    private static void IndexDoorzoeken()
    {
        Header("5. Index voor herhaald zoeken");

        var index = new DutchFuzzyIndex<Klant>(Klanten, k => k.Naam, DutchFuzzyOptions.Names);

        foreach (var zoekterm in new[] { "Anna Schoenmaker", "Bart Desmedt", "Theo Gijsbregt" })
        {
            Console.WriteLine($"  \"{zoekterm}\":");

            var treffers = index.Search(zoekterm, threshold: 0.70, take: 3);

            if (treffers.Count == 0)
            {
                Console.WriteLine("    (geen treffers)");
            }

            foreach (var treffer in treffers)
            {
                Console.WriteLine($"    {Format(treffer.Score)}  {treffer.Value.Naam} ({treffer.Value.Woonplaats})");
            }
        }
    }

    /// <summary>6. Using the equality comparer to deduplicate.</summary>
    private static void Groeperen()
    {
        Header("6. Ontdubbelen met de equality comparer");

        var groepen = Klanten
            .GroupBy(k => k.Naam, DutchPhoneticEqualityComparer.Default)
            .Where(g => g.Count() > 1);

        foreach (var groep in groepen)
        {
            Console.WriteLine($"  [{groep.Key.ToDutchPhoneticKey()}] {string.Join(" | ", groep.Select(k => k.Naam))}");
        }

        var uniek = new HashSet<string>(Klanten.Select(k => k.Naam), DutchPhoneticEqualityComparer.Default);

        Console.WriteLine();
        Console.WriteLine($"  {Klanten.Length} klanten -> {uniek.Count} fonetisch unieke namen.");
    }

    /// <summary>7. Comparing the three built-in presets.</summary>
    private static void Profielen()
    {
        Header("7. Presets naast elkaar");

        var presets = new (string Naam, DutchFuzzyOptions Options)[]
        {
            ("Default", DutchFuzzyOptions.Default),
            ("Names", DutchFuzzyOptions.Names),
            ("Words", DutchFuzzyOptions.Words),
        };

        var paren = new[]
        {
            ("Vandenbroecke", "Van den Broecke"),
            ("Maes", "Moes"),
            ("vrede", "wrede"),
            ("school", "skool"),
        };

        Console.WriteLine($"  {"paar",-34}{string.Concat(presets.Select(p => p.Naam.PadRight(10)))}");

        foreach (var (links, rechts) in paren)
        {
            var scores = presets.Select(p => Format(links.DutchSimilarity(rechts, p.Options)).PadRight(10));

            Console.WriteLine($"  {links + " ~ " + rechts,-34}{string.Concat(scores)}");
        }
    }

    /// <summary>8. Deriving your own options, encoder and scorer.</summary>
    private static void EigenInstellingen()
    {
        Header("8. Eigen instellingen");

        var opties = DutchFuzzyOptions.Names with
        {
            MaxKeyLength = 6,
            PhoneticWeight = 0.60,
            OrthographicWeight = 0.30,
            NGramWeight = 0.10,
        };

        opties.Validate();

        var encoder = new DutchPhoneticEncoder(opties);
        var scorer = new DutchFuzzyScorer(opties);

        Console.WriteLine($"  Sleutel (max 6):    {encoder.Encode("Schoenmakers")}");
        Console.WriteLine($"  Score:              {Format(scorer.Score("Schoenmakers", "Schoonmakers"))}");
        Console.WriteLine($"  Keulse fonetiek:    {ColognePhoneticEncoder.Default.Encode("Schoenmakers")}");
    }

    /// <summary>9. A rough indication of the throughput.</summary>
    private static void Prestaties()
    {
        Header("9. Prestaties (indicatief)");

        string[] voornamen =
        [
            "Jan", "Piet", "Klaas", "Anna", "Marie", "Sofie", "Willem", "Hendrik", "Theo", "Bart",
            "Eva", "Lotte", "Daan", "Sem", "Noor", "Tim", "Ilse", "Bram", "Fleur", "Joost",
        ];

        string[] achternamen =
        [
            "Meijer", "Bakker", "Visser", "Schoenmakers", "Vandenbroecke", "Desmet", "De Bruijn",
            "Kuipers", "Jansen", "Gijsbrecht", "Van der Meer", "Hoekstra", "Vermeulen", "Peeters",
            "Willems", "Dijkstra", "Van Leeuwen", "Brouwer", "Timmermans", "Verhoeven", "Postma",
            "Scholten", "Van Vliet", "Huisman", "Koster", "Prins", "Blom", "Groen", "Jonker", "Vink",
        ];

        var namen = Enumerable.Range(0, 50_000)
            .Select(i => $"{voornamen[i % voornamen.Length]} {achternamen[(i / voornamen.Length) % achternamen.Length]}")
            .ToArray();

        var stopwatch = Stopwatch.StartNew();

        foreach (var naam in namen)
        {
            _ = DutchFuzzy.PhoneticKey(naam, DutchFuzzyOptions.Names);
        }

        stopwatch.Stop();
        Console.WriteLine($"  {namen.Length:N0} sleutels berekend in {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var index = new DutchFuzzyIndex<string>(namen, x => x, DutchFuzzyOptions.Names);
        stopwatch.Stop();
        Console.WriteLine($"  Index gebouwd over {index.Count:N0} items in {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();

        const int Queries = 200;

        for (var i = 0; i < Queries; i++)
        {
            _ = index.Search("Sofie Vandenbroeke", 0.85, 5);
        }

        stopwatch.Stop();
        Console.WriteLine(
            $"  {Queries} zoekopdrachten via blokken in {stopwatch.ElapsedMilliseconds} ms " +
            $"({(double)stopwatch.ElapsedMilliseconds / Queries:F2} ms per zoekopdracht)");

        stopwatch.Restart();

        for (var i = 0; i < 10; i++)
        {
            _ = namen.FuzzyMatchDutch("Sofie Vandenbroeke", x => x, 0.85, 5, DutchFuzzyOptions.Names);
        }

        stopwatch.Stop();
        Console.WriteLine(
            $"  10 lineaire scans in {stopwatch.ElapsedMilliseconds} ms " +
            $"({stopwatch.ElapsedMilliseconds / 10d:F2} ms per zoekopdracht)");
        Console.WriteLine();
        Console.WriteLine("  Let op: vindt de index geen enkel blok, dan valt Search() terug op een");
        Console.WriteLine("  volledige scan. Gebruik Search(query, threshold, take, allowFullScan: false)");
        Console.WriteLine("  om die worst case uit te sluiten.");
    }

    private static string Format(double score) => score.ToString("F3", CultureInfo.InvariantCulture);

    private static string Balk(double score) => new('#', (int)Math.Round(score * 20, MidpointRounding.AwayFromZero));

    private static void Header(string titel)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(titel);
        Console.WriteLine(new string('=', 78));
    }
}
