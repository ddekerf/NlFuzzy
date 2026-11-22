using System.Collections.Generic;

namespace NlFuzzy.Tests.Evaluation;

/// <summary>Reads the committed Wikidata snapshot in <c>Golden/sources</c>.</summary>
public static class WikidataSnapshot
{
    public const string FileName = "wikidata-p460-dutch-surnames.csv";

    /// <summary>Every item id (Q…) that occurs in the snapshot's first two columns.</summary>
    public static HashSet<string> ItemIds(IEnumerable<string> lines)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("a,", StringComparison.Ordinal))
            {
                continue;
            }

            var fields = CsvLine.Parse(line);

            if (fields.Count >= 2)
            {
                ids.Add(fields[0]);
                ids.Add(fields[1]);
            }
        }

        return ids;
    }
}
