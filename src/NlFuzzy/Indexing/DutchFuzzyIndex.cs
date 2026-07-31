using System;
using System.Collections.Generic;

namespace NlFuzzy.Indexing;

/// <summary>
/// In-memory index that buckets items by phonetic key (blocking), so that a query only has to
/// be scored against candidates in matching buckets instead of the whole collection.
/// </summary>
/// <typeparam name="T">The indexed item type.</typeparam>
/// <remarks>
/// Two buckets are maintained per item: one on the full phonetic key and one on the first four
/// characters of that key. A query scores the union of both buckets. When the union is empty the
/// index falls back to a full scan, which can be disabled per query.
/// This type is not thread-safe for concurrent <see cref="Add"/> and <see cref="Search(string, double, int)"/>.
/// </remarks>
public sealed class DutchFuzzyIndex<T>
{
    /// <summary>The length of the phonetic key prefix used as the secondary blocking key.</summary>
    public const int PrefixBlockLength = 4;

    private readonly Dictionary<string, List<int>> _byKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<int>> _byPrefix = new(StringComparer.Ordinal);
    private readonly List<Entry> _entries = new();
    private readonly Func<T, string> _selector;
    private readonly DutchFuzzyOptions _options;
    private readonly Comparison.DutchFuzzyScorer _scorer;

    /// <summary>Creates an index over <paramref name="items"/>.</summary>
    /// <param name="items">The items to index.</param>
    /// <param name="selector">Projects an item onto the text that is indexed and compared.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public DutchFuzzyIndex(IEnumerable<T> items, Func<T, string> selector, DutchFuzzyOptions? options = null)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _options = options ?? DutchFuzzyOptions.Default;
        _options.Validate();
        _scorer = DutchFuzzy.GetScorer(_options);

        foreach (var item in items)
        {
            Add(item);
        }
    }

    /// <summary>The number of indexed items.</summary>
    public int Count => _entries.Count;

    /// <summary>The options this index was built with.</summary>
    public DutchFuzzyOptions Options => _options;

    /// <summary>Adds a single item to the index.</summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        var text = _selector(item) ?? string.Empty;
        var prepared = _scorer.Prepare(text);
        var key = prepared.Key;
        var position = _entries.Count;

        _entries.Add(new Entry(item, text, prepared));

        AddTo(_byKey, key, position);

        if (key.Length > PrefixBlockLength)
        {
            AddTo(_byPrefix, key.Substring(0, PrefixBlockLength), position);
        }
        else if (key.Length > 0)
        {
            AddTo(_byPrefix, key, position);
        }
    }

    /// <summary>Searches the index, scoring only the items in matching buckets.</summary>
    /// <param name="query">The query string.</param>
    /// <param name="threshold">The minimum score. Defaults to 0.82.</param>
    /// <param name="take">The maximum number of results. Use 0 or a negative value for all.</param>
    /// <returns>The matches, ordered by descending score.</returns>
    public IReadOnlyList<FuzzyMatch<T>> Search(
        string query,
        double threshold = DutchStringExtensions.DefaultThreshold,
        int take = 10) =>
        Search(query, threshold, take, allowFullScan: true);

    /// <summary>Searches the index, scoring only the items in matching buckets.</summary>
    /// <param name="query">The query string.</param>
    /// <param name="threshold">The minimum score. Defaults to 0.82.</param>
    /// <param name="take">The maximum number of results. Use 0 or a negative value for all.</param>
    /// <param name="allowFullScan">
    /// When true (the default) and no bucket matches, every indexed item is scored. Set to false to
    /// return an empty result instead, which keeps the worst case cheap on very large indexes.
    /// </param>
    /// <returns>The matches, ordered by descending score.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    public IReadOnlyList<FuzzyMatch<T>> Search(string query, double threshold, int take, bool allowFullScan)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var prepared = _scorer.Prepare(query);
        var key = prepared.Key;
        var candidates = CollectCandidates(key);

        if (candidates.Count == 0)
        {
            if (!allowFullScan)
            {
                return Array.Empty<FuzzyMatch<T>>();
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                candidates.Add(i);
            }
        }

        var matches = new List<FuzzyMatch<T>>();

        foreach (var position in candidates)
        {
            var entry = _entries[position];
            var score = _scorer.Score(prepared, entry.Prepared);

            if (score >= threshold)
            {
                matches.Add(new FuzzyMatch<T>(entry.Item, entry.Text, score, entry.Key));
            }
        }

        matches.Sort(DutchEnumerableExtensions.CompareByScoreDescending);

        if (take > 0 && matches.Count > take)
        {
            matches.RemoveRange(take, matches.Count - take);
        }

        return matches;
    }

    private HashSet<int> CollectCandidates(string key)
    {
        var candidates = new HashSet<int>();

        if (key.Length == 0)
        {
            return candidates;
        }

        if (_byKey.TryGetValue(key, out var exact))
        {
            candidates.UnionWith(exact);
        }

        var prefix = key.Length > PrefixBlockLength ? key.Substring(0, PrefixBlockLength) : key;

        if (_byPrefix.TryGetValue(prefix, out var blocked))
        {
            candidates.UnionWith(blocked);
        }

        return candidates;
    }

    private static void AddTo(Dictionary<string, List<int>> buckets, string key, int position)
    {
        if (key.Length == 0)
        {
            return;
        }

        if (!buckets.TryGetValue(key, out var bucket))
        {
            bucket = new List<int>();
            buckets[key] = bucket;
        }

        bucket.Add(position);
    }

    private readonly struct Entry
    {
        internal Entry(T item, string text, Comparison.DutchFuzzyScorer.PreparedText prepared)
        {
            Item = item;
            Text = text;
            Prepared = prepared;
        }

        internal T Item { get; }

        internal string Text { get; }

        internal Comparison.DutchFuzzyScorer.PreparedText Prepared { get; }

        internal string Key => Prepared.Key;
    }
}
