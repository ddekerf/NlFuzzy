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

/// <summary>
/// How the pairwise score scales with input length. Edit-distance based components are quadratic,
/// so this is the benchmark to consult before accepting untrusted, unbounded input.
/// </summary>
[MemoryDiagnoser]
public class InputLengthBenchmarks
{
    private readonly DutchFuzzyScorer _scorer = new(DutchFuzzyOptions.Names);

    private string _left = string.Empty;
    private string _right = string.Empty;

    [Params(8, 32, 128, 512, 2_048)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(7);
        var alphabet = "abcdefghijklmnopqrstuvwxyz ";

        _left = string.Concat(Enumerable.Range(0, Length).Select(_ => alphabet[random.Next(alphabet.Length)]));

        var tail = string.Concat(
            Enumerable.Range(0, Length - (Length / 2)).Select(_ => alphabet[random.Next(alphabet.Length)]));

        _right = string.Concat(_left.AsSpan(0, Length / 2), tail.AsSpan());
    }

    [Benchmark]
    public double Score() => _scorer.Score(_left, _right);
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
    private string[] _sameKeyNames = Array.Empty<string>();
    private DutchFuzzyIndex<string>? _index;
    private DutchFuzzyIndex<string>? _sameKeyIndex;

    [Params(10_000, 100_000)]
    public int Size { get; set; }

    /// <summary>A high threshold returns almost nothing; a low one keeps a large candidate set.</summary>
    [Params(0.60, 0.85)]
    public double Threshold { get; set; }

    /// <summary>Top-k versus "give me everything", which is what bounds sorting and allocations.</summary>
    [Params(5, 0)]
    public int Take { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _names = Enumerable.Range(0, Size)
            .Select(i => $"{Voornamen[i % Voornamen.Length]} {Achternamen[(i / Voornamen.Length) % Achternamen.Length]}")
            .ToArray();

        // Worst case for blocking: every item lands in the same bucket, so the candidate set is
        // the whole collection and the blocked modes degrade to a full scan.
        _sameKeyNames = Enumerable.Range(0, Size).Select(_ => "Jan Jansen").ToArray();

        _index = new DutchFuzzyIndex<string>(_names, x => x, DutchFuzzyOptions.Names);
        _sameKeyIndex = new DutchFuzzyIndex<string>(_sameKeyNames, x => x, DutchFuzzyOptions.Names);
    }

    [Benchmark]
    public DutchFuzzyIndex<string> Build() => new(_names, x => x, DutchFuzzyOptions.Names);

    [Benchmark]
    public int ExactSearch() =>
        _index!.Search("Sofie Vandenbroeke", Threshold, Take, DutchFuzzyIndexSearchMode.Exact).Count;

    [Benchmark]
    public int BlockedSearch() =>
        _index!.Search("Sofie Vandenbroeke", Threshold, Take, DutchFuzzyIndexSearchMode.Blocked).Count;

    [Benchmark]
    public int BlockedOnlySearch() =>
        _index!.Search("Sofie Vandenbroeke", Threshold, Take, DutchFuzzyIndexSearchMode.BlockedOnly).Count;

    [Benchmark]
    public int WorstCaseBucketSearch() =>
        _sameKeyIndex!.Search("Jan Jansen", Threshold, Take, DutchFuzzyIndexSearchMode.Blocked).Count;

    [Benchmark(Baseline = true)]
    public int LinearScan() =>
        _names.FuzzyMatchDutch("Sofie Vandenbroeke", x => x, Threshold, Take, DutchFuzzyOptions.Names).Count();
}
