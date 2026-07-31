using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NlFuzzy.Indexing;

namespace NlFuzzy.Tests;

public class DutchFuzzyTests
{
    [Fact]
    public void PhoneticKey_UsesTheDefaultOptionsWhenNoneAreSupplied()
    {
        Assert.Equal(DutchFuzzy.PhoneticKey("Kuipers", DutchFuzzyOptions.Default), DutchFuzzy.PhoneticKey("Kuipers"));
    }

    [Fact]
    public void PhoneticKeys_ReturnsOneKeyPerToken()
    {
        var keys = DutchFuzzy.PhoneticKeys("Van den Broecke", DutchFuzzyOptions.Default with { StripNamePrefixes = false });

        Assert.Equal(3, keys.Count);
    }

    [Theory]
    [InlineData("Meijer", "Meier", true)]
    [InlineData("Bakker", "Dekker", false)]
    public void SoundsLike_ComparesKeys(string a, string b, bool expected)
    {
        Assert.Equal(expected, DutchFuzzy.SoundsLike(a, b));
    }

    [Fact]
    public void Similarity_IsInUnitInterval()
    {
        Assert.InRange(DutchFuzzy.Similarity("Kuipers", "Kuyper"), 0d, 1d);
    }

    [Fact]
    public void AreEquivalent_HonorsTheThreshold()
    {
        Assert.True(DutchFuzzy.AreEquivalent("Meijer", "Meier", 0.8));
        Assert.False(DutchFuzzy.AreEquivalent("Meijer", "Bakker", 0.8));
    }

    [Fact]
    public void ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => DutchFuzzy.PhoneticKey(null!));
        Assert.Throws<ArgumentNullException>(() => DutchFuzzy.PhoneticKeys(null!));
        Assert.Throws<ArgumentNullException>(() => DutchFuzzy.SoundsLike(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => DutchFuzzy.SoundsLike("a", null!));
        Assert.Throws<ArgumentNullException>(() => DutchFuzzy.Similarity(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => DutchFuzzy.AreEquivalent(null!, "a", 0.8));
    }

    [Fact]
    public void CachesEncodersAndScorersPerOptionsInstance()
    {
        var options = DutchFuzzyOptions.Default with { MaxKeyLength = 5 };

        Assert.Same(DutchFuzzy.GetEncoder(options), DutchFuzzy.GetEncoder(options));
        Assert.Same(DutchFuzzy.GetScorer(options), DutchFuzzy.GetScorer(options));
        Assert.Same(DutchFuzzy.GetEncoder(null), DutchFuzzy.GetEncoder(DutchFuzzyOptions.Default));
    }
}

public class ThreadSafetyTests
{
    private static readonly string[] Corpus =
    [
        "Van den Broecke", "Vandenbroecke", "Meijer", "Meier", "Bruijn", "Bruyn",
        "Schoenmakers", "Schoonmakers", "De Smet", "Desmet", "Kuipers", "Kuiper",
    ];

    [Fact]
    public async Task ParallelEncodingProducesStableKeys()
    {
        var expected = Corpus.Select(x => DutchFuzzy.PhoneticKey(x, DutchFuzzyOptions.Names)).ToArray();
        var failures = new ConcurrentBag<string>();

        await Task.WhenAll(Enumerable.Range(0, 1_000).Select(i => Task.Run(() =>
        {
            var index = i % Corpus.Length;
            var key = DutchFuzzy.PhoneticKey(Corpus[index], DutchFuzzyOptions.Names);

            if (!string.Equals(key, expected[index], StringComparison.Ordinal))
            {
                failures.Add($"{Corpus[index]}: expected '{expected[index]}', got '{key}'.");
            }
        })));

        Assert.Empty(failures);
    }

    [Fact]
    public async Task ParallelScoringProducesStableScores()
    {
        var expected = DutchFuzzy.Similarity("Vandenbroecke", "Van den Broecke", DutchFuzzyOptions.Names);
        var failures = new ConcurrentBag<double>();

        await Task.WhenAll(Enumerable.Range(0, 1_000).Select(_ => Task.Run(() =>
        {
            var score = DutchFuzzy.Similarity("Vandenbroecke", "Van den Broecke", DutchFuzzyOptions.Names);

            if (Math.Abs(score - expected) > 1e-12)
            {
                failures.Add(score);
            }
        })));

        Assert.Empty(failures);
    }

    [Fact]
    public async Task ParallelSearchOnASharedIndexIsStable()
    {
        var index = new DutchFuzzyIndex<string>(Corpus, x => x, DutchFuzzyOptions.Names);
        var expected = index.Search("Van den Broecke", 0.7, 10).Count;
        var failures = new ConcurrentBag<int>();

        await Task.WhenAll(Enumerable.Range(0, 500).Select(_ => Task.Run(() =>
        {
            var count = index.Search("Van den Broecke", 0.7, 10).Count;

            if (count != expected)
            {
                failures.Add(count);
            }
        })));

        Assert.Empty(failures);
    }
}
