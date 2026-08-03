using System.Collections.Generic;
using System.Linq;

namespace NlFuzzy.Tests.Extensions;

public class DutchStringExtensionsTests
{
    [Fact]
    public void ToDutchPhoneticKey_MatchesTheFacade()
    {
        Assert.Equal(DutchFuzzy.PhoneticKey("Schoenmakers"), "Schoenmakers".ToDutchPhoneticKey());
        Assert.Equal(
            DutchFuzzy.PhoneticKey("Schoenmakers", DutchFuzzyOptions.Names),
            "Schoenmakers".ToDutchPhoneticKey(DutchFuzzyOptions.Names));
    }

    [Theory]
    [InlineData("Meijer", "Meier", true)]
    [InlineData("Bruijn", "Bruyn", true)]
    [InlineData("Bakker", "Dekker", false)]
    public void SoundsLikeDutch_ComparesKeys(string a, string b, bool expected)
    {
        Assert.Equal(expected, a.SoundsLikeDutch(b));
    }

    [Fact]
    public void DutchSimilarity_ReturnsAScoreInUnitInterval()
    {
        Assert.InRange("Kuipers".DutchSimilarity("Kuypers"), 0d, 1d);
        Assert.Equal(1d, "Kuipers".DutchSimilarity("kuipers"), 10);
    }

    [Fact]
    public void DutchFuzzyEquals_UsesTheDefaultThreshold()
    {
        Assert.Equal(0.82, DutchStringExtensions.DefaultThreshold, 10);
        Assert.True("Meijer".DutchFuzzyEquals("Meier"));
        Assert.False("Bakker".DutchFuzzyEquals("Visser"));
    }

    [Fact]
    public void DutchFuzzyEquals_HonorsAnExplicitThreshold()
    {
        Assert.True("Kuipers".DutchFuzzyEquals("Kuiper", 0.5));
        Assert.False("Kuipers".DutchFuzzyEquals("Kuiper", 0.99));
    }

    [Fact]
    public void ThrowsOnNull()
    {
        string? nullValue = null;

        Assert.Throws<ArgumentNullException>(() => nullValue!.ToDutchPhoneticKey());
        Assert.Throws<ArgumentNullException>(() => nullValue!.SoundsLikeDutch("a"));
        Assert.Throws<ArgumentNullException>(() => "a".SoundsLikeDutch(null!));
        Assert.Throws<ArgumentNullException>(() => nullValue!.DutchSimilarity("a"));
        Assert.Throws<ArgumentNullException>(() => nullValue!.DutchFuzzyEquals("a"));
    }
}

public class DutchEnumerableExtensionsTests
{
    private sealed record Customer(int Id, string Name);

    private static readonly Customer[] Customers =
    [
        new(1, "Jan Meijer"),
        new(2, "Jan Meier"),
        new(3, "Piet Bakker"),
        new(4, "Klaas Visser"),
        new(5, "Jan de Bruijn"),
    ];

    [Fact]
    public void FuzzyMatchDutch_ReturnsMatchesOrderedByDescendingScore()
    {
        var matches = Customers.FuzzyMatchDutch("Jan Meyer", c => c.Name, threshold: 0.6).ToList();

        Assert.NotEmpty(matches);
        Assert.Equal(matches.OrderByDescending(m => m.Score).Select(m => m.Value.Id), matches.Select(m => m.Value.Id));
        Assert.Contains(matches, m => m.Value.Id is 1 or 2);
    }

    [Fact]
    public void FuzzyMatchDutch_HonorsTake()
    {
        var matches = Customers.FuzzyMatchDutch("Jan", c => c.Name, threshold: 0d, take: 2).ToList();

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void FuzzyMatchDutch_PopulatesEveryField()
    {
        var match = Customers.FuzzyMatchDutch("Jan Meijer", c => c.Name, threshold: 0.5).First();

        Assert.Equal("Jan Meijer", match.Text);
        Assert.Equal(1, match.Value.Id);
        Assert.InRange(match.Score, 0d, 1d);
        Assert.False(string.IsNullOrEmpty(match.PhoneticKey));
    }

    [Fact]
    public void FuzzyMatchDutch_WorksOnStringSequences()
    {
        var matches = new[] { "Meijer", "Bakker" }.FuzzyMatchDutch("Meier", threshold: 0.8).ToList();

        Assert.Single(matches);
        Assert.Equal("Meijer", matches[0].Value);
    }

    [Fact]
    public void BestDutchMatch_ReturnsTheHighestScoringItem()
    {
        var best = Customers.BestDutchMatch("Jan Meyer", c => c.Name, threshold: 0.6);

        Assert.NotNull(best);
        Assert.Contains(best!.Value.Value.Id, new[] { 1, 2 });
    }

    [Fact]
    public void BestDutchMatch_ReturnsNullWhenNothingClearsTheThreshold()
    {
        Assert.Null(Customers.BestDutchMatch("Zzzzzz", c => c.Name, threshold: 0.95));
    }

    [Fact]
    public void ThrowsOnNull()
    {
        IEnumerable<Customer>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.FuzzyMatchDutch("a", c => c.Name).ToList());
        Assert.Throws<ArgumentNullException>(() => Customers.FuzzyMatchDutch(null!, c => c.Name).ToList());
        Assert.Throws<ArgumentNullException>(() => Customers.FuzzyMatchDutch("a", null!).ToList());
        Assert.Throws<ArgumentNullException>(() => Customers.BestDutchMatch("a", null!));
    }

    [Fact]
    public void FuzzyMatchDutch_ThrowsOnInvalidThreshold()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Customers.FuzzyMatchDutch("a", c => c.Name, threshold: 1.5).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => Customers.FuzzyMatchDutch("a", c => c.Name, threshold: -0.1).ToList());
    }

    [Fact]
    public void FuzzyMatchDutch_TakeReturnsThePrefixOfTheCompleteRanking()
    {
        var all = Customers.FuzzyMatchDutch("Jan Meyer", c => c.Name, 0d, take: 0)
            .Select(m => Math.Round(m.Score, 12))
            .ToList();

        for (var take = 1; take <= Customers.Length; take++)
        {
            var top = Customers.FuzzyMatchDutch("Jan Meyer", c => c.Name, 0d, take)
                .Select(m => Math.Round(m.Score, 12))
                .ToList();

            Assert.Equal(all.Take(take), top);
        }
    }

    [Fact]
    public void FuzzyMatchDutch_BoundedTakeKeepsTheBestScoresOnLargeInput()
    {
        var random = new Random(4242);
        var syllables = new[] { "ba", "kui", "mei", "schoen", "vis", "jan", "de", "broe", "ker", "sen" };

        var items = Enumerable.Range(0, 5_000)
            .Select(i => new Customer(
                i,
                string.Concat(Enumerable.Range(0, 3).Select(_ => syllables[random.Next(syllables.Length)]))))
            .ToList();

        var top5 = items.FuzzyMatchDutch("kuipersen", c => c.Name, 0d, take: 5).ToList();
        var expected = items
            .FuzzyMatchDutch("kuipersen", c => c.Name, 0d, take: 0)
            .Take(5)
            .Select(m => Math.Round(m.Score, 12))
            .ToList();

        Assert.Equal(5, top5.Count);
        Assert.Equal(expected, top5.Select(m => Math.Round(m.Score, 12)));
    }
}
