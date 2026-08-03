using System;
using System.Collections.Generic;

namespace NlFuzzy;

/// <summary>Extension methods that rank a sequence by Dutch fuzzy similarity.</summary>
public static class DutchEnumerableExtensions
{
    /// <summary>Ranks the source by Dutch fuzzy similarity to the query, best match first.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The items to rank.</param>
    /// <param name="query">The query string.</param>
    /// <param name="selector">Projects an item onto the text that is compared.</param>
    /// <param name="threshold">
    /// The minimum score an item must reach. Defaults to the <see cref="DutchFuzzyOptions.DefaultThreshold"/>
    /// of <paramref name="options"/> (0.82 for the default options).
    /// </param>
    /// <param name="take">The maximum number of results. Use 0 or a negative value for all.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The matching items, ordered by descending score.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is NaN, infinite or outside [0, 1].
    /// </exception>
    public static IEnumerable<FuzzyMatch<T>> FuzzyMatchDutch<T>(
        this IEnumerable<T> source,
        string query,
        Func<T, string> selector,
        double? threshold = null,
        int take = 10,
        DutchFuzzyOptions? options = null)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var minimumScore = Thresholds.Resolve(threshold, options, nameof(threshold));
        var scorer = DutchFuzzy.GetScorer(options);
        var preparedQuery = scorer.Prepare(query);
        var matches = new TopMatchCollector<T>(take);

        foreach (var item in source)
        {
            var text = selector(item) ?? string.Empty;
            var prepared = scorer.Prepare(text);
            var score = scorer.Score(preparedQuery, prepared);

            if (score >= minimumScore)
            {
                matches.Add(new FuzzyMatch<T>(item, text, score, prepared.Key));
            }
        }

        return matches.ToSortedList();
    }

    /// <summary>Convenience overload for a sequence of strings.</summary>
    /// <param name="source">The strings to rank.</param>
    /// <param name="query">The query string.</param>
    /// <param name="threshold">
    /// The minimum score an item must reach. Defaults to the <see cref="DutchFuzzyOptions.DefaultThreshold"/>
    /// of <paramref name="options"/> (0.82 for the default options).
    /// </param>
    /// <param name="take">The maximum number of results. Use 0 or a negative value for all.</param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The matching strings, ordered by descending score.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is NaN, infinite or outside [0, 1].
    /// </exception>
    public static IEnumerable<FuzzyMatch<string>> FuzzyMatchDutch(
        this IEnumerable<string> source,
        string query,
        double? threshold = null,
        int take = 10,
        DutchFuzzyOptions? options = null) =>
        source.FuzzyMatchDutch(query, static text => text, threshold, take, options);

    /// <summary>Returns the single best match, or null when nothing reaches the threshold.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The items to search.</param>
    /// <param name="query">The query string.</param>
    /// <param name="selector">Projects an item onto the text that is compared.</param>
    /// <param name="threshold">
    /// The minimum score. Defaults to the <see cref="DutchFuzzyOptions.DefaultThreshold"/> of
    /// <paramref name="options"/> (0.82 for the default options).
    /// </param>
    /// <param name="options">Optional options. Defaults to <see cref="DutchFuzzyOptions.Default"/>.</param>
    /// <returns>The best match, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="threshold"/> is NaN, infinite or outside [0, 1].
    /// </exception>
    public static FuzzyMatch<T>? BestDutchMatch<T>(
        this IEnumerable<T> source,
        string query,
        Func<T, string> selector,
        double? threshold = null,
        DutchFuzzyOptions? options = null)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        if (threshold is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold must be in [0, 1].");
        }

        var minimumScore = Thresholds.Resolve(threshold, options, nameof(threshold));
        var scorer = DutchFuzzy.GetScorer(options);
        var preparedQuery = scorer.Prepare(query);
        var best = default(FuzzyMatch<T>);
        var found = false;

        foreach (var item in source)
        {
            var text = selector(item) ?? string.Empty;
            var prepared = scorer.Prepare(text);
            var score = scorer.Score(preparedQuery, prepared);

            if (score < minimumScore || (found && score <= best.Score))
            {
                continue;
            }

            best = new FuzzyMatch<T>(item, text, score, prepared.Key);
            found = true;
        }

        return found ? best : null;
    }

    internal static int CompareByScoreDescending<T>(FuzzyMatch<T> left, FuzzyMatch<T> right)
    {
        var byScore = right.Score.CompareTo(left.Score);

        return byScore != 0 ? byScore : string.CompareOrdinal(left.Text, right.Text);
    }
}
