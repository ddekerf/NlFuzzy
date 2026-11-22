using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NlFuzzy.Tests.Evaluation;

/// <summary>Pass counts for one category in one split.</summary>
public sealed record CategoryResult(string Split, string Category, bool IsMatchCategory, int Pairs, int Passes)
{
    public double Rate => Pairs == 0 ? 0d : (double)Passes / Pairs;
}

/// <summary>Confusion counts and the derived metrics for one split.</summary>
public sealed record OverallResult(string Split, int TruePositives, int FalsePositives, int FalseNegatives, int TrueNegatives)
{
    public double Precision => TruePositives + FalsePositives == 0 ? 0d : (double)TruePositives / (TruePositives + FalsePositives);

    public double Recall => TruePositives + FalseNegatives == 0 ? 0d : (double)TruePositives / (TruePositives + FalseNegatives);

    public double F1 => Precision + Recall == 0d ? 0d : 2d * Precision * Recall / (Precision + Recall);
}

/// <summary>The result of scoring the evaluation dataset once.</summary>
public sealed record EvaluationResult(IReadOnlyList<CategoryResult> Categories, IReadOnlyList<OverallResult> Overall)
{
    public static readonly string[] Splits = [EvaluationDataset.Tune, EvaluationDataset.Holdout];

    public CategoryResult? Find(string split, string category) =>
        Categories.FirstOrDefault(c => c.Split == split && c.Category == category);

    public OverallResult OverallFor(string split) => Overall.Single(o => o.Split == split);

    /// <summary>A Markdown report: one row per category and split, then the overall rows.</summary>
    public string ToMarkdown(EvaluationBaseline? baseline = null)
    {
        var builder = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        builder.AppendLine("| Category | Split | Pairs | Passes | Rate | Baseline |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|");

        foreach (var c in Categories.OrderBy(c => c.IsMatchCategory ? 0 : 1).ThenBy(c => c.Category, StringComparer.Ordinal).ThenBy(c => c.Split == EvaluationDataset.Tune ? 0 : 1))
        {
            var expected = baseline?.PassesFor(c.Split, c.Category);
            builder.Append(inv, $"| {c.Category} | {c.Split} | {c.Pairs} | {c.Passes} | {c.Rate:P0} | {(expected is null ? "—" : expected.Value.ToString(inv))} |").AppendLine();
        }

        builder.AppendLine();
        builder.AppendLine("| Split | Precision | Recall | F1 | TP | FP | FN | TN |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var o in Overall)
        {
            builder.Append(inv, $"| {o.Split} | {o.Precision:F3} | {o.Recall:F3} | {o.F1:F3} | {o.TruePositives} | {o.FalsePositives} | {o.FalseNegatives} | {o.TrueNegatives} |").AppendLine();
        }

        return builder.ToString();
    }
}

/// <summary>Scores labeled pairs and aggregates the results per category and split.</summary>
public static class EvaluationMetrics
{
    /// <summary>Scores every pair with <paramref name="score"/>; a score at or above <paramref name="threshold"/> predicts a match.</summary>
    public static EvaluationResult Evaluate(IEnumerable<EvaluationPair> pairs, Func<string, string, double> score, double threshold)
    {
        var categories = new Dictionary<(string Split, string Category), (bool IsMatch, int Pairs, int Passes)>();
        var confusion = EvaluationResult.Splits.ToDictionary(s => s, _ => new int[4]);

        foreach (var pair in pairs)
        {
            var predictedMatch = score(pair.Left, pair.Right) >= threshold;
            var passed = predictedMatch == pair.IsMatch;
            var key = (pair.Split, pair.Category);

            categories.TryGetValue(key, out var entry);
            categories[key] = (pair.IsMatch, entry.Pairs + 1, entry.Passes + (passed ? 1 : 0));

            var counts = confusion[pair.Split];
            var index = (pair.IsMatch, predictedMatch) switch
            {
                (true, true) => 0,
                (false, true) => 1,
                (true, false) => 2,
                _ => 3,
            };
            counts[index]++;
        }

        var categoryResults = categories
            .Select(kv => new CategoryResult(kv.Key.Split, kv.Key.Category, kv.Value.IsMatch, kv.Value.Pairs, kv.Value.Passes))
            .ToList();

        var overall = EvaluationResult.Splits
            .Select(s => new OverallResult(s, confusion[s][0], confusion[s][1], confusion[s][2], confusion[s][3]))
            .ToList();

        return new EvaluationResult(categoryResults, overall);
    }

    /// <summary>Scores the dataset the way the evaluation specifies: the Names preset at its default threshold.</summary>
    public static EvaluationResult EvaluateNamesPreset(IEnumerable<EvaluationPair> pairs)
    {
        var options = DutchFuzzyOptions.Names;

        return Evaluate(pairs, (a, b) => DutchFuzzy.Similarity(a, b, options), options.DefaultThreshold);
    }
}
