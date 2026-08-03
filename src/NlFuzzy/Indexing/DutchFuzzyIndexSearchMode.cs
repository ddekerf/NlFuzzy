namespace NlFuzzy.Indexing;

/// <summary>
/// Selects the trade-off between recall and speed for <see cref="DutchFuzzyIndex{T}.Search(string, double?, int, DutchFuzzyIndexSearchMode)"/>.
/// </summary>
public enum DutchFuzzyIndexSearchMode
{
    /// <summary>
    /// Scores every indexed item and therefore returns exactly the same matches as
    /// <see cref="DutchEnumerableExtensions.FuzzyMatchDutch{T}"/> on the same items and options.
    /// This is the default: the index still avoids re-normalizing and re-encoding every item,
    /// but it never silently drops a qualifying match. Cost: O(n) scores per query.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Scores only the items in the matching phonetic buckets, and falls back to a full scan when
    /// those buckets yield fewer than <c>take</c> matches (or no match at all when <c>take</c> is
    /// zero or negative). Faster than <see cref="Exact"/>, but approximate: once the buckets deliver
    /// enough matches, an item outside the buckets that would have scored above the threshold is
    /// not returned.
    /// </summary>
    Blocked = 1,

    /// <summary>
    /// Scores only the items in the matching phonetic buckets and never falls back to a full scan.
    /// The cheapest and least complete mode; use it when a hard upper bound on query cost matters
    /// more than recall.
    /// </summary>
    BlockedOnly = 2
}
