using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NlFuzzy.Tests.Evaluation;

/// <summary>Integrity checks for the evaluation dataset. These never score a pair.</summary>
public class EvaluationDatasetTests
{
    private static readonly Lazy<IReadOnlyList<EvaluationPair>> Pairs = new(EvaluationDataset.Load);

    [Theory]
    [InlineData("a,b,c", new[] { "a", "b", "c" })]
    [InlineData("\"Bakker, Jan\",Jan Bakker,match", new[] { "Bakker, Jan", "Jan Bakker", "match" })]
    [InlineData("\"say \"\"hi\"\"\",x", new[] { "say \"hi\"", "x" })]
    [InlineData("a,,c", new[] { "a", "", "c" })]
    [InlineData(" a , b ", new[] { "a", "b" })]
    public void CsvLine_ParsesQuotedFieldsAndEscapedQuotes(string line, string[] expected)
    {
        Assert.Equal(expected, CsvLine.Parse(line));
    }

    [Fact]
    public void CsvLine_RejectsAnUnterminatedQuote()
    {
        Assert.Throws<FormatException>(() => CsvLine.Parse("\"open,b"));
    }

    [Fact]
    public void Parse_RejectsAnUnknownLabel()
    {
        var lines = new[] { EvaluationDataset.Header, "a,b,maybe,ij-ei-y,hand,tune," };

        Assert.Throws<FormatException>(() => EvaluationDataset.Parse(lines));
    }

    [Theory]
    // Expected buckets computed with an independent PowerShell implementation of FNV-1a.
    [InlineData("Meijer", "Meyer", 20)]
    [InlineData("Meyer", "Meijer", 20)]
    [InlineData("Jan Bakker", "Bakker, Jan", 90)]
    [InlineData("Bos", "Bosman", 6)]
    public void SplitBucket_IsStableAndOrderIndependent(string left, string right, int expected)
    {
        Assert.Equal(expected, EvaluationDataset.SplitBucket(left, right));
    }

    [Fact]
    public void Fnv1a_MatchesTheReferenceValues()
    {
        // Reference values of 32-bit FNV-1a: the offset basis for "", and the published "a" vector.
        Assert.Equal(2166136261u, EvaluationDataset.Fnv1a(string.Empty));
        Assert.Equal(0xE40C292Cu, EvaluationDataset.Fnv1a("a"));
    }

    [Fact]
    public void Dataset_HasTheRequiredSize()
    {
        var matches = Pairs.Value.Count(p => p.IsMatch);
        var nomatches = Pairs.Value.Count(p => !p.IsMatch);

        Assert.True(matches >= 600, $"Expected at least 600 match pairs, found {matches}.");
        Assert.True(nomatches >= 400, $"Expected at least 400 nomatch pairs, found {nomatches}.");
    }

    [Fact]
    public void Dataset_UsesOnlyKnownCategoriesSourcesAndSplits()
    {
        foreach (var pair in Pairs.Value)
        {
            var categories = pair.IsMatch ? EvaluationDataset.MatchCategories : EvaluationDataset.NoMatchCategories;

            Assert.True(categories.Contains(pair.Category), $"Line {pair.LineNumber}: unknown {(pair.IsMatch ? "match" : "nomatch")} category '{pair.Category}'.");
            Assert.True(pair.Source == "hand" || pair.Source.StartsWith("wikidata:Q", StringComparison.Ordinal), $"Line {pair.LineNumber}: unknown source '{pair.Source}'.");
            Assert.True(pair.Split is EvaluationDataset.Tune or EvaluationDataset.Holdout, $"Line {pair.LineNumber}: unknown split '{pair.Split}'.");
            Assert.False(string.IsNullOrWhiteSpace(pair.Left) || string.IsNullOrWhiteSpace(pair.Right), $"Line {pair.LineNumber}: empty name.");
        }
    }

    [Fact]
    public void Dataset_HasNoDuplicatesOrConflictingLabels()
    {
        var duplicates = Pairs.Value
            .GroupBy(p => EvaluationDataset.PairKey(p.Left, p.Right))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.First().Left}/{g.First().Right} (lines {string.Join(", ", g.Select(p => p.LineNumber))})")
            .ToList();

        Assert.True(duplicates.Count == 0, "Duplicate pairs: " + string.Join("; ", duplicates));
        Assert.All(Pairs.Value, p => Assert.False(
            string.Equals(p.Left.Trim(), p.Right.Trim(), StringComparison.OrdinalIgnoreCase),
            $"Line {p.LineNumber}: both names are identical."));
    }

    [Fact]
    public void Dataset_DoesNotRepeatTheCoreGoldenPairs()
    {
        var core = new[] { "positive-pairs.csv", "negative-pairs.csv" }
            .SelectMany(f => File.ReadAllLines(EvaluationDataset.GoldenPath(f)))
            .Select(l => l.Split(','))
            .Where(p => p.Length >= 2 && !string.Equals(p[0], "left", StringComparison.OrdinalIgnoreCase))
            .Select(p => EvaluationDataset.PairKey(p[0], p[1]))
            .ToHashSet(StringComparer.Ordinal);

        var overlap = Pairs.Value.Where(p => core.Contains(EvaluationDataset.PairKey(p.Left, p.Right))).ToList();

        Assert.True(overlap.Count == 0, "Pairs already in the core dataset: " + string.Join("; ", overlap.Select(p => $"{p.Left}/{p.Right} (line {p.LineNumber})")));
    }

    [Fact]
    public void Dataset_CoversEveryCategory()
    {
        var counts = Pairs.Value.GroupBy(p => p.Category).ToDictionary(g => g.Key, g => g.Count());
        var short_ = EvaluationDataset.MatchCategories
            .Concat(EvaluationDataset.NoMatchCategories)
            .Where(c => counts.GetValueOrDefault(c) < EvaluationDataset.MinimumPerCategory)
            .Select(c => $"{c} ({counts.GetValueOrDefault(c)})")
            .ToList();

        Assert.True(short_.Count == 0, $"Categories below {EvaluationDataset.MinimumPerCategory} pairs: " + string.Join(", ", short_));
    }

    [Theory]
    [InlineData("Born", "Bron", true)]
    [InlineData("Aerts", "Arets", true)]
    [InlineData("Hoekstra", "Hoekstar", true)]
    [InlineData("Kuipers", "kuipres", true)]
    [InlineData("Kuipers", "Kuirpes", false)]
    [InlineData("Kuipers", "Kuipars", false)]
    [InlineData("Brouwer", "Brouwr", false)]
    [InlineData("Bakker", "Bakker", false)]
    public void IsSingleAdjacentSwap_RecognizesExactlyOneSwap(string left, string right, bool expected)
    {
        Assert.Equal(expected, EvaluationDataset.IsSingleAdjacentSwap(left, right));
    }

    [Fact]
    public void Dataset_TranspositionPairsAreSingleSwapsWithEnoughLongOnes()
    {
        var pairs = Pairs.Value.Where(p => p.Category == "nm-transposition").ToList();
        var notSwaps = pairs
            .Where(p => p.IsMatch || !EvaluationDataset.IsSingleAdjacentSwap(p.Left, p.Right))
            .Select(p => $"line {p.LineNumber} {p.Left}/{p.Right}")
            .ToList();

        Assert.True(notSwaps.Count == 0, "nm-transposition rows that are not a single-swap nomatch: " + string.Join("; ", notSwaps));

        var longPairs = pairs.Count(p => p.Left.Trim().Length >= EvaluationDataset.LongTranspositionLength);

        Assert.True(longPairs >= 5, $"nm-transposition needs at least 5 pairs of {EvaluationDataset.LongTranspositionLength}+ letters, found {longPairs}.");
    }

    [Fact]
    public void Dataset_TypoCategoryIsNotOnlySwaps()
    {
        var nonSwaps = Pairs.Value.Count(p => p.Category == "typo" && !EvaluationDataset.IsSingleAdjacentSwap(p.Left, p.Right));

        Assert.True(nonSwaps >= 20, $"typo needs at least 20 pairs that are not a single adjacent swap, found {nonSwaps}.");
    }

    [Fact]
    public void Dataset_SplitMatchesTheHashAndHasTheExpectedShare()
    {
        var wrong = Pairs.Value
            .Where(p => p.Split != EvaluationDataset.ComputeSplit(p.Left, p.Right))
            .Select(p => $"line {p.LineNumber} {p.Left}/{p.Right}: recorded {p.Split}, expected {EvaluationDataset.ComputeSplit(p.Left, p.Right)}")
            .ToList();

        Assert.True(wrong.Count == 0, "Split mismatches: " + string.Join("; ", wrong.Take(20)));

        var share = Pairs.Value.Count(p => p.Split == EvaluationDataset.Holdout) * 100d / Pairs.Value.Count;

        Assert.InRange(share, 25d, 35d);
    }

    [Fact]
    public void Dataset_WikidataRowsComeFromTheSnapshot()
    {
        var snapshot = File.ReadAllLines(EvaluationDataset.GoldenPath(Path.Combine("sources", WikidataSnapshot.FileName)));
        var ids = WikidataSnapshot.ItemIds(snapshot);

        foreach (var pair in Pairs.Value.Where(p => p.Source.StartsWith("wikidata:", StringComparison.Ordinal)))
        {
            var parts = pair.Source["wikidata:".Length..].Split('/');

            Assert.True(parts.Length == 2, $"Line {pair.LineNumber}: a Wikidata source must name two item ids.");
            Assert.True(ids.Contains(parts[0]) && ids.Contains(parts[1]), $"Line {pair.LineNumber}: {pair.Source} is not in the snapshot.");
        }
    }
}
