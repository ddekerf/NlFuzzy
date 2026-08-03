using System;
using System.Collections.Generic;

namespace NlFuzzy.Indexing;

/// <summary>
/// In-memory index that keeps every item normalized and phonetically encoded, and additionally
/// buckets items by phonetic key (blocking) so that a query can optionally be scored against
/// candidates in matching buckets instead of the whole collection.
/// </summary>
/// <typeparam name="T">The indexed item type.</typeparam>
/// <remarks>
/// Two buckets are maintained per item: one on the full phonetic key and one on the first four
/// characters of that key.
/// <para>
/// Searching defaults to <see cref="DutchFuzzyIndexSearchMode.Exact"/>, which scores every indexed
/// item against the (already prepared) entries and therefore returns exactly what a linear scan
/// with <see cref="DutchEnumerableExtensions.FuzzyMatchDutch{T}"/> returns. Blocking is opt-in via
/// <see cref="DutchFuzzyIndexSearchMode.Blocked"/> and
/// <see cref="DutchFuzzyIndexSearchMode.BlockedOnly"/>, which are faster but approximate: matches
/// outside the buckets can be missed even when their score exceeds the threshold.
/// </para>
/// This type is not thread-safe for concurrent <see cref="Add"/> and
/// <see cref="Search(string, double?, int)"/>.
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

    /// <summary>Searches the index.</summary>
    /// <param name="query">The query string.</param>
    /// <param name="threshold">
    /// The minimum score. Defaults to the <see cref="DutchFuzzyOptions.DefaultThreshold"/> of the
    /// options this index was built with (0.82 for the default options).
    /// </param>
    /// <param name="take">The maximum number of results. Use 0 or a negative value for all.</param>
    /// <returns>The matches, ordered by descending score.</returns>
    /// <remarks>
    /// This overload uses <see cref="DutchFuzzyIndexSearchMode.Exact"/>, so its result is identical
    /// to a linear scan with <see cref="DutchEnumerableExtensions.FuzzyMatchDutch{T}"/>. Use the
    /// overload with an explicit <see cref="DutchFuzzyIndexSearchMode"/> to trade recall for speed.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is NaN, infinite or outside [0, 1].
    /// </exception>
    public IReadOnlyList<FuzzyMatch<T>> Search(
        string query,
        double? threshold = null,
        int take = 10) =>
        Search(query, threshold, take, DutchFuzzyIndexSearchMode.Exact);

    /// <summary>Searches the index using an explicit recall/speed trade-off.</summary>
    /// <param name="query">The query string.</param>
    /// <param name="threshold">
    /// The minimum score. Defaults to the <see cref="DutchFuzzyOptions.DefaultThreshold"/> of the
    /// options this index was built with when null is passed.
    /// </param>
    /// <param name="take">The maximum number of results. Use 0 or a negative value for all.</param>
    /// <param name="mode">The search mode. See <see cref="DutchFuzzyIndexSearchMode"/>.</param>
    /// <returns>The matches, ordered by descending score.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is NaN, infinite or outside [0, 1], or when
    /// <paramref name="mode"/> is not a defined enum value.
    /// </exception>
    public IReadOnlyList<FuzzyMatch<T>> Search(
        string query,
        double? threshold,
        int take,
        DutchFuzzyIndexSearchMode mode)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (!EnumCompat.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown search mode.");
        }

        var minimumScore = Thresholds.Resolve(threshold, _options, nameof(threshold));
        var prepared = _scorer.Prepare(query);
        var matches = new TopMatchCollector<T>(take);

        if (mode == DutchFuzzyIndexSearchMode.Exact)
        {
            ScoreRange(prepared, minimumScore, matches, skip: null);

            return matches.ToSortedList();
        }

        var candidates = CollectCandidates(prepared.Key);

        foreach (var position in candidates)
        {
            ScoreEntry(position, prepared, minimumScore, matches);
        }

        if (mode == DutchFuzzyIndexSearchMode.Blocked && NeedsFullScan(matches.Count, take))
        {
            ScoreRange(prepared, minimumScore, matches, candidates);
        }

        return matches.ToSortedList();
    }

    private static bool NeedsFullScan(int found, int take) => take > 0 ? found < take : found == 0;

    private void ScoreRange(
        Comparison.DutchFuzzyScorer.PreparedText query,
        double minimumScore,
        TopMatchCollector<T> matches,
        HashSet<int>? skip)
    {
        for (var position = 0; position < _entries.Count; position++)
        {
            if (skip is not null && skip.Contains(position))
            {
                continue;
            }

            ScoreEntry(position, query, minimumScore, matches);
        }
    }

    private void ScoreEntry(
        int position,
        Comparison.DutchFuzzyScorer.PreparedText query,
        double minimumScore,
        TopMatchCollector<T> matches)
    {
        var entry = _entries[position];
        var score = _scorer.Score(query, entry.Prepared);

        if (score >= minimumScore)
        {
            matches.Add(new FuzzyMatch<T>(entry.Item, entry.Text, score, entry.Key));
        }
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
