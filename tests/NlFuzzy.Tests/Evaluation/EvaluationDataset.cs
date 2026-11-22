using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NlFuzzy.Tests.Evaluation;

/// <summary>One labeled pair from <c>Golden/evaluation-pairs.csv</c>.</summary>
public sealed record EvaluationPair(
    string Left,
    string Right,
    bool IsMatch,
    string Category,
    string Source,
    string Split,
    string Note,
    int LineNumber);

/// <summary>
/// The labeled evaluation dataset: about a thousand name pairs, each with a label, a variant
/// category, a source and a fixed split. See <c>openspec/specs/matching-quality-evaluation</c>.
/// </summary>
public static class EvaluationDataset
{
    public const string FileName = "evaluation-pairs.csv";

    public const string Header = "left,right,label,category,source,split,note";

    public const string Tune = "tune";

    public const string Holdout = "holdout";

    /// <summary>Match categories. Every category must hold at least <see cref="MinimumPerCategory"/> pairs.</summary>
    public static readonly IReadOnlyList<string> MatchCategories =
    [
        "ij-ei-y",
        "ui-uy-uij",
        "historic-vowel",
        "double-vowel",
        "c-k",
        "ck-ks-x",
        "ph-th-gh",
        "ch-g",
        "final-devoicing",
        "double-consonant",
        "particle-separated",
        "particle-glued",
        "elided-article",
        "token-order",
        "diacritics",
        "patronymic-se-sen",
        "first-name",
        "typo",
        "surname-variant",
    ];

    /// <summary>No-match categories. Every category must hold at least <see cref="MinimumPerCategory"/> pairs.</summary>
    public static readonly IReadOnlyList<string> NoMatchCategories =
    [
        "nm-first-sound",
        "nm-final-sound",
        "nm-same-prefix",
        "nm-short",
        "nm-vowel",
        "nm-derived",
        "nm-unrelated",
    ];

    public const int MinimumPerCategory = 15;

    /// <summary>The share of pairs, in percent, that the split hash sends to <see cref="Holdout"/>.</summary>
    public const int HoldoutPercent = 30;

    public static string GoldenPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Golden", fileName);

    /// <summary>Loads the dataset from the test output directory.</summary>
    public static IReadOnlyList<EvaluationPair> Load() => Parse(File.ReadAllLines(GoldenPath(FileName)));

    /// <summary>Parses dataset lines. Throws <see cref="FormatException"/> on a malformed row.</summary>
    public static IReadOnlyList<EvaluationPair> Parse(IReadOnlyList<string> lines)
    {
        var pairs = new List<EvaluationPair>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var fields = CsvLine.Parse(line);

            if (i == 0 || string.Equals(line.Trim(), Header, StringComparison.Ordinal))
            {
                if (!string.Equals(string.Join(",", fields), Header, StringComparison.Ordinal))
                {
                    throw new FormatException($"Line {i + 1}: expected the header '{Header}'.");
                }

                continue;
            }

            if (fields.Count != 7)
            {
                throw new FormatException($"Line {i + 1}: expected 7 fields, found {fields.Count}.");
            }

            var label = fields[2];

            if (label is not ("match" or "nomatch"))
            {
                throw new FormatException($"Line {i + 1}: label must be 'match' or 'nomatch', found '{label}'.");
            }

            pairs.Add(new EvaluationPair(
                fields[0],
                fields[1],
                label == "match",
                fields[3],
                fields[4],
                fields[5],
                fields[6],
                i + 1));
        }

        return pairs;
    }

    /// <summary>
    /// The split a pair belongs to: FNV-1a (32 bit) over the lower-cased, ordinally ordered pair
    /// joined by U+0001, modulo 100, below <see cref="HoldoutPercent"/> means holdout. The result is
    /// the same on every runtime and independent of the pair's order or position in the file.
    /// </summary>
    public static string ComputeSplit(string left, string right) =>
        SplitBucket(left, right) < HoldoutPercent ? Holdout : Tune;

    public static int SplitBucket(string left, string right)
    {
        var a = left.Trim().ToLowerInvariant();
        var b = right.Trim().ToLowerInvariant();

        if (string.CompareOrdinal(a, b) > 0)
        {
            (a, b) = (b, a);
        }

        return (int)(Fnv1a(a + "\u0001" + b) % 100u);
    }

    public static uint Fnv1a(string value)
    {
        var hash = 2166136261u;

        foreach (var c in value)
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return hash;
    }

    /// <summary>The order-independent identity of a pair, for duplicate checks.</summary>
    public static string PairKey(string left, string right)
    {
        var a = left.Trim().ToLowerInvariant();
        var b = right.Trim().ToLowerInvariant();

        return string.CompareOrdinal(a, b) <= 0 ? a + "\u0001" + b : b + "\u0001" + a;
    }
}

/// <summary>A small RFC 4180-style parser for one CSV line: quoted fields, escaped quotes, commas.</summary>
public static class CsvLine
{
    public static IReadOnlyList<string> Parse(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }

                continue;
            }

            if (c == '"' && current.Length == 0)
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (inQuotes)
        {
            throw new FormatException("Unterminated quoted field.");
        }

        fields.Add(current.ToString());

        return fields.Select(f => f.Trim()).ToList();
    }
}
