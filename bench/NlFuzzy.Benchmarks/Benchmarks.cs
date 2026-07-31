using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NlFuzzy;
using NlFuzzy.Comparison;
using NlFuzzy.Distance;
using NlFuzzy.Indexing;
using NlFuzzy.Phonetics;

namespace NlFuzzy.Benchmarks;

internal static class Program
{
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

/// <summary>Cost of producing a single phonetic key.</summary>
[MemoryDiagnoser]
public class EncoderBenchmarks
{
    private readonly DutchPhoneticEncoder _default = new(DutchFuzzyOptions.Default);
    private readonly DutchPhoneticEncoder _names = new(DutchFuzzyOptions.Names);

    [Params("Bruijn", "Vandenbroecke", "Anna Schoenmakers", "'s-Hertogenbosch")]
    public string Value { get; set; } = string.Empty;

    [Benchmark(Baseline = true)]
    public string Default() => _default.Encode(Value);

    [Benchmark]
    public string Names() => _names.Encode(Value);

    [Benchmark]
    public string Cologne() => ColognePhoneticEncoder.Default.Encode(Value);
}

/// <summary>Cost of a single pairwise score and of its components.</summary>
[MemoryDiagnoser]
public class ScorerBenchmarks
{
    private const string Left = "Marie Vandenbroecke";
    private const string Right = "Marie van den Broecke";

    private readonly DutchFuzzyScorer _scorer = new(DutchFuzzyOptions.Names);

    [Benchmark(Baseline = true)]
    public double Score() => _scorer.Score(Left, Right);

    [Benchmark]
    public int Levenshtein() => LevenshteinDistance.Compute(Left, Right);

    [Benchmark]
    public int DamerauLevenshtein() => DamerauLevenshteinDistance.Compute(Left, Right);

    [Benchmark]
    public double JaroWinkler() => JaroWinklerSimilarity.Instance.Similarity(Left, Right);

    [Benchmark]
    public double Dice() => DiceCoefficientSimilarity.Instance.Similarity(Left, Right);

    [Benchmark]
    public double TokenSet() => TokenSetSimilarity.Instance.Similarity(Left, Right);
}

/// <summary>Index build cost and query cost versus a linear scan.</summary>
[MemoryDiagnoser]
public class IndexBenchmarks
{
    private static readonly string[] Voornamen =
    [
        "Jan", "Piet", "Klaas", "Anna", "Marie", "Sofie", "Willem", "Hendrik", "Theo", "Bart",
        "Eva", "Lotte", "Daan", "Sem", "Noor", "Tim", "Ilse", "Bram", "Fleur", "Joost",
    ];

    private static readonly string[] Achternamen =
    [
        "Meijer", "Bakker", "Visser", "Schoenmakers", "Vandenbroecke", "Desmet", "De Bruijn",
        "Kuipers", "Jansen", "Gijsbrecht", "Van der Meer", "Hoekstra", "Vermeulen", "Peeters",
        "Willems", "Dijkstra", "Van Leeuwen", "Brouwer", "Timmermans", "Verhoeven", "Postma",
        "Scholten", "Van Vliet", "Huisman", "Koster", "Prins", "Blom", "Groen", "Jonker", "Vink",
    ];

    private string[] _names = Array.Empty<string>();
    private DutchFuzzyIndex<string>? _index;

    [Params(10_000, 100_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _names = Enumerable.Range(0, Size)
            .Select(i => $"{Voornamen[i % Voornamen.Length]} {Achternamen[(i / Voornamen.Length) % Achternamen.Length]}")
            .ToArray();

        _index = new DutchFuzzyIndex<string>(_names, x => x, DutchFuzzyOptions.Names);
    }

    [Benchmark]
    public DutchFuzzyIndex<string> Build() => new(_names, x => x, DutchFuzzyOptions.Names);

    [Benchmark]
    public int IndexedSearch() => _index!.Search("Sofie Vandenbroeke", 0.85, 5).Count;

    [Benchmark(Baseline = true)]
    public int LinearScan() =>
        _names.FuzzyMatchDutch("Sofie Vandenbroeke", x => x, 0.85, 5, DutchFuzzyOptions.Names).Count();
}
