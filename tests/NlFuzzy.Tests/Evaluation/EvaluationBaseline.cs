using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NlFuzzy.Tests.Evaluation;

/// <summary>Overall metrics recorded in the baseline.</summary>
public sealed class BaselineOverall
{
    public double Precision { get; set; }

    public double Recall { get; set; }

    public double F1 { get; set; }
}

/// <summary>
/// The recorded quality baseline in <c>Golden/evaluation-baseline.json</c>: pass counts per split and
/// category, and the overall metrics per split. Change it only in a commit that says why.
/// </summary>
public sealed class EvaluationBaseline
{
    public const string FileName = "evaluation-baseline.json";

    /// <summary>A category may lose at most this many passes against the baseline.</summary>
    public const int CategoryTolerance = 1;

    /// <summary>The overall metrics may drop at most this much (0.5 percentage points).</summary>
    public const double OverallTolerance = 0.005;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Preset { get; set; } = "Names";

    public double Threshold { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>Pass counts: split → category → passes.</summary>
    public SortedDictionary<string, SortedDictionary<string, int>> Passes { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Overall metrics per split.</summary>
    public SortedDictionary<string, BaselineOverall> Overall { get; set; } = new(StringComparer.Ordinal);

    public int? PassesFor(string split, string category) =>
        Passes.TryGetValue(split, out var categories) && categories.TryGetValue(category, out var passes) ? passes : null;

    public static EvaluationBaseline From(EvaluationResult result, double threshold, string reason)
    {
        var baseline = new EvaluationBaseline { Threshold = threshold, Reason = reason };

        foreach (var split in EvaluationResult.Splits)
        {
            baseline.Passes[split] = new SortedDictionary<string, int>(
                result.Categories.Where(c => c.Split == split).ToDictionary(c => c.Category, c => c.Passes),
                StringComparer.Ordinal);

            var overall = result.OverallFor(split);
            baseline.Overall[split] = new BaselineOverall
            {
                Precision = Math.Round(overall.Precision, 4),
                Recall = Math.Round(overall.Recall, 4),
                F1 = Math.Round(overall.F1, 4),
            };
        }

        return baseline;
    }

    public static EvaluationBaseline? TryLoad(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<EvaluationBaseline>(File.ReadAllText(path), Json) : null;

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Json) + "\n");

    /// <summary>Every floor that <paramref name="result"/> breaks, as readable messages.</summary>
    public IReadOnlyList<string> Violations(EvaluationResult result)
    {
        var violations = new List<string>();

        foreach (var split in EvaluationResult.Splits)
        {
            if (!Passes.TryGetValue(split, out var categories))
            {
                continue;
            }

            foreach (var (category, expected) in categories)
            {
                var actual = result.Find(split, category)?.Passes ?? 0;

                if (actual < expected - CategoryTolerance)
                {
                    violations.Add($"{category} ({split}): {actual} passes, baseline {expected}, allowed {expected - CategoryTolerance}");
                }
            }

            if (Overall.TryGetValue(split, out var floor))
            {
                var overall = result.OverallFor(split);
                Check(violations, split, "precision", overall.Precision, floor.Precision);
                Check(violations, split, "recall", overall.Recall, floor.Recall);
                Check(violations, split, "F1", overall.F1, floor.F1);
            }
        }

        return violations;
    }

    private static void Check(List<string> violations, string split, string metric, double actual, double expected)
    {
        if (actual < expected - OverallTolerance)
        {
            violations.Add(string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"overall {metric} ({split}): {actual:F4}, baseline {expected:F4}, allowed {expected - OverallTolerance:F4}"));
        }
    }
}
