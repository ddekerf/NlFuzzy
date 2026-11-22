using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace NlFuzzy.Tests.Evaluation;

/// <summary>
/// Scores the evaluation dataset with the <c>Names</c> preset, reports quality per category and fails
/// when a category or the overall metrics drop below the recorded baseline.
/// </summary>
/// <remarks>
/// To record a new baseline, set <c>NLFUZZY_WRITE_BASELINE</c> to a reason and run these tests; the
/// file is written next to this source file's <c>Golden</c> folder. Commit it on its own, with the reason.
/// </remarks>
public class EvaluationTests
{
    private static readonly Lazy<EvaluationResult> Result =
        new(() => EvaluationMetrics.EvaluateNamesPreset(EvaluationDataset.Load()));

    private readonly ITestOutputHelper _output;

    public EvaluationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Metrics_CountPassesAndConfusionCorrectly()
    {
        var pairs = new[]
        {
            Pair("a", "a2", match: true, "c1", EvaluationDataset.Tune),
            Pair("b", "b2", match: true, "c1", EvaluationDataset.Tune),
            Pair("c", "c2", match: false, "n1", EvaluationDataset.Tune),
            Pair("d", "d2", match: false, "n1", EvaluationDataset.Tune),
            Pair("e", "e2", match: true, "c1", EvaluationDataset.Holdout),
        };

        // Scores: a=0.9 (TP), b=0.5 (FN), c=0.9 (FP), d=0.1 (TN), e=0.95 (TP, holdout).
        var scores = new Dictionary<string, double> { ["a"] = 0.9, ["b"] = 0.5, ["c"] = 0.9, ["d"] = 0.1, ["e"] = 0.95 };
        var result = EvaluationMetrics.Evaluate(pairs, (left, _) => scores[left], 0.82);

        Assert.Equal(1, result.Find(EvaluationDataset.Tune, "c1")!.Passes);
        Assert.Equal(2, result.Find(EvaluationDataset.Tune, "c1")!.Pairs);
        Assert.Equal(1, result.Find(EvaluationDataset.Tune, "n1")!.Passes);
        Assert.Equal(1, result.Find(EvaluationDataset.Holdout, "c1")!.Passes);

        var tune = result.OverallFor(EvaluationDataset.Tune);
        Assert.Equal((1, 1, 1, 1), (tune.TruePositives, tune.FalsePositives, tune.FalseNegatives, tune.TrueNegatives));
        Assert.Equal(0.5, tune.Precision, 10);
        Assert.Equal(0.5, tune.Recall, 10);
        Assert.Equal(0.5, tune.F1, 10);

        var holdout = result.OverallFor(EvaluationDataset.Holdout);
        Assert.Equal(1d, holdout.Precision, 10);
        Assert.Equal(1d, holdout.Recall, 10);
    }

    [Fact]
    public void Baseline_ReportsACategoryThatLostMoreThanOnePass()
    {
        var baseline = new EvaluationBaseline
        {
            Passes = new(StringComparer.Ordinal)
            {
                [EvaluationDataset.Tune] = new(StringComparer.Ordinal) { ["c1"] = 5, ["c2"] = 5 },
            },
        };

        var result = new EvaluationResult(
            [
                new CategoryResult(EvaluationDataset.Tune, "c1", true, 10, 4),
                new CategoryResult(EvaluationDataset.Tune, "c2", true, 10, 3),
            ],
            [new OverallResult(EvaluationDataset.Tune, 7, 0, 13, 0), new OverallResult(EvaluationDataset.Holdout, 0, 0, 0, 0)]);

        var violations = baseline.Violations(result);

        Assert.Single(violations);
        Assert.Contains("c2 (tune): 3 passes, baseline 5", violations[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluation_ReportsQualityPerCategory()
    {
        var baseline = EvaluationBaseline.TryLoad(EvaluationDataset.GoldenPath(EvaluationBaseline.FileName));
        var report = Result.Value.ToMarkdown(baseline);

        _output.WriteLine(report);

        foreach (var category in EvaluationDataset.MatchCategories.Concat(EvaluationDataset.NoMatchCategories))
        {
            Assert.Contains($"| {category} | {EvaluationDataset.Tune} |", report, StringComparison.Ordinal);
            Assert.Contains($"| {category} | {EvaluationDataset.Holdout} |", report, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Evaluation_StaysAboveTheRecordedBaseline()
    {
        var reason = Environment.GetEnvironmentVariable("NLFUZZY_WRITE_BASELINE");

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var path = SourceGoldenPath(EvaluationBaseline.FileName);
            EvaluationBaseline.From(Result.Value, DutchFuzzyOptions.Names.DefaultThreshold, reason).Save(path);
            _output.WriteLine($"Baseline written to {path}.");
            return;
        }

        var baseline = EvaluationBaseline.TryLoad(EvaluationDataset.GoldenPath(EvaluationBaseline.FileName));

        Assert.True(baseline is not null, $"No {EvaluationBaseline.FileName}. Record one with NLFUZZY_WRITE_BASELINE=<reason>.");

        var violations = baseline!.Violations(Result.Value);

        _output.WriteLine(Result.Value.ToMarkdown(baseline));

        Assert.True(violations.Count == 0, "Quality dropped below the baseline:\n" + string.Join("\n", violations));

        var improved = Result.Value.Categories.Where(c => c.Passes > (baseline.PassesFor(c.Split, c.Category) ?? int.MaxValue)).ToList();

        if (improved.Count > 0)
        {
            _output.WriteLine("Improved categories; consider raising the baseline: " + string.Join(", ", improved.Select(c => $"{c.Category} ({c.Split})")));
        }
    }

    private static EvaluationPair Pair(string left, string right, bool match, string category, string split) =>
        new(left, right, match, category, "hand", split, string.Empty, 0);

    private static string SourceGoldenPath(string fileName, [CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(thisFile)!)!, "Golden", fileName);
}
