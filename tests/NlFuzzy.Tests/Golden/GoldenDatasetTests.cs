using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NlFuzzy.Tests.Golden;

/// <summary>
/// The normative acceptance dataset from the plan (chapter 7.2). The dataset is the specification:
/// when a case fails, the rule table or the scorer is adjusted, never the dataset.
/// </summary>
public class GoldenDatasetTests
{
    private const double PositiveThreshold = 0.85;
    private const double NegativeThreshold = 0.75;

    private static readonly DutchFuzzyOptions Options = DutchFuzzyOptions.Names;

    public static TheoryData<string, string> PositivePairs => Load("positive-pairs.csv");

    public static TheoryData<string, string> NegativePairs => Load("negative-pairs.csv");

    [Theory]
    [MemberData(nameof(PositivePairs))]
    public void PositivePairsMatch(string a, string b)
    {
        var keyA = DutchFuzzy.PhoneticKey(a, Options);
        var keyB = DutchFuzzy.PhoneticKey(b, Options);
        var score = DutchFuzzy.Similarity(a, b, Options);

        Assert.True(
            string.Equals(keyA, keyB, StringComparison.Ordinal) || score >= PositiveThreshold,
            $"'{a}' vs '{b}': keys '{keyA}' / '{keyB}', score {score.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"(expected identical keys or score >= {PositiveThreshold.ToString("F2", CultureInfo.InvariantCulture)}).");
    }

    [Theory]
    [MemberData(nameof(NegativePairs))]
    public void NegativePairsDoNotMatch(string a, string b)
    {
        var keyA = DutchFuzzy.PhoneticKey(a, Options);
        var keyB = DutchFuzzy.PhoneticKey(b, Options);
        var score = DutchFuzzy.Similarity(a, b, Options);

        Assert.False(
            string.Equals(keyA, keyB, StringComparison.Ordinal),
            $"'{a}' vs '{b}' unexpectedly share the phonetic key '{keyA}'.");

        Assert.True(
            score < NegativeThreshold,
            $"'{a}' vs '{b}': score {score.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"(expected < {NegativeThreshold.ToString("F2", CultureInfo.InvariantCulture)}).");
    }

    [Fact]
    public void DatasetsAreNonEmptyAndFreeOfDuplicates()
    {
        var positives = ReadPairs("positive-pairs.csv").ToList();
        var negatives = ReadPairs("negative-pairs.csv").ToList();

        Assert.True(positives.Count >= 30, $"Expected at least 30 positive pairs, found {positives.Count}.");
        Assert.True(negatives.Count >= 8, $"Expected at least 8 negative pairs, found {negatives.Count}.");
        Assert.Equal(positives.Count, positives.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(negatives.Count, negatives.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Empty(positives.Intersect(negatives, StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(PositivePairs))]
    public void ScoringIsSymmetric(string a, string b)
    {
        Assert.Equal(
            DutchFuzzy.Similarity(a, b, Options),
            DutchFuzzy.Similarity(b, a, Options),
            10);
    }

    private static IEnumerable<string> ReadPairs(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Golden", fileName);

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0
                || trimmed[0] == '#'
                || trimmed.StartsWith("left,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return trimmed;
        }
    }

    private static TheoryData<string, string> Load(string fileName)
    {
        var data = new TheoryData<string, string>();

        foreach (var line in ReadPairs(fileName))
        {
            var parts = line.Split(',');

            if (parts.Length >= 2)
            {
                data.Add(parts[0].Trim(), parts[1].Trim());
            }
        }

        return data;
    }
}
