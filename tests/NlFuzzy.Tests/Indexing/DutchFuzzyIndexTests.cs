using System.Collections.Generic;
using System.Linq;
using NlFuzzy.Indexing;

namespace NlFuzzy.Tests.Indexing;

public class DutchFuzzyIndexTests
{
    private sealed record Customer(int Id, string Name);

    private static readonly Customer[] Customers =
    [
        new(1, "Jan Meijer"),
        new(2, "Jan Meier"),
        new(3, "Piet Bakker"),
        new(4, "Klaas Visser"),
        new(5, "Jan de Bruijn"),
        new(6, "Anna Schoenmakers"),
        new(7, "Anna Schoonmakers"),
    ];

    private static DutchFuzzyIndex<Customer> CreateIndex() =>
        new(Customers, c => c.Name, DutchFuzzyOptions.Names);

    [Fact]
    public void Search_FindsPhoneticVariants()
    {
        var results = CreateIndex().Search("Jan Meyer", 0.6);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Value.Id == 1);
        Assert.Contains(results, r => r.Value.Id == 2);
    }

    [Fact]
    public void Search_OrdersByDescendingScore()
    {
        var results = CreateIndex().Search("Anna Schoenmakers", 0.5, take: 10);

        Assert.Equal(
            results.OrderByDescending(r => r.Score).Select(r => r.Value.Id),
            results.Select(r => r.Value.Id));
    }

    [Fact]
    public void Search_HonorsTake()
    {
        Assert.Single(CreateIndex().Search("Jan Meyer", 0.5, take: 1));
    }

    [Fact]
    public void Search_ReturnsEmptyWhenNothingMatches()
    {
        Assert.Empty(CreateIndex().Search("Zzyzx Qqq", 0.9));
    }

    [Fact]
    public void Search_ProducesTheSameResultsAsALinearScan()
    {
        const double Threshold = 0.7;

        var indexed = CreateIndex()
            .Search("Jan Meyer", Threshold, take: 100)
            .Select(r => r.Value.Id)
            .OrderBy(id => id)
            .ToList();

        var linear = Customers
            .FuzzyMatchDutch("Jan Meyer", c => c.Name, Threshold, take: 100, DutchFuzzyOptions.Names)
            .Select(r => r.Value.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(linear, indexed);
    }

    [Fact]
    public void Add_MakesAnItemSearchable()
    {
        var index = new DutchFuzzyIndex<Customer>([], c => c.Name, DutchFuzzyOptions.Names);

        Assert.Empty(index.Search("Meijer", 0.5));

        index.Add(new Customer(99, "Meijer"));

        Assert.Single(index.Search("Meier", 0.5));
    }

    [Fact]
    public void Search_WithoutFullScan_UsesBucketsOnly()
    {
        var index = CreateIndex();

        var bucketed = index.Search("Jan Meyer", 0.5, 10, DutchFuzzyIndexSearchMode.BlockedOnly);

        Assert.All(bucketed, m => Assert.InRange(m.Score, 0.5, 1d));
    }

    [Fact]
    public void Search_PopulatesMatchMetadata()
    {
        var match = CreateIndex().Search("Jan Meijer", 0.5)[0];

        Assert.Equal("Jan Meijer", match.Text);
        Assert.False(string.IsNullOrEmpty(match.PhoneticKey));
    }

    [Fact]
    public void ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DutchFuzzyIndex<Customer>(null!, c => c.Name));
        Assert.Throws<ArgumentNullException>(() => new DutchFuzzyIndex<Customer>(Customers, null!));
        Assert.Throws<ArgumentNullException>(() => CreateIndex().Search(null!));
    }

    [Fact]
    public void Search_ScalesToALargeIndex()
    {
        var items = Enumerable.Range(0, 20_000)
            .Select(i => new Customer(i, $"Persoon{i}"))
            .Concat([new Customer(999_999, "Vandenbroecke")])
            .ToList();

        var index = new DutchFuzzyIndex<Customer>(items, c => c.Name, DutchFuzzyOptions.Names);

        var results = index.Search("Van den Broecke", 0.8, take: 5);

        Assert.Contains(results, r => r.Value.Id == 999_999);
    }

    [Fact]
    public void Search_FindsMatchesOutsideTheBuckets()
    {
        // Regression: a single, irrelevant bucket candidate used to suppress every match outside
        // the buckets, so this query returned nothing at all.
        var index = new DutchFuzzyIndex<string>(
            ["Anna Schoenmakers", "Anne Visser"],
            static value => value);

        var results = index.Search("Anne Schoenmakers", 0.82);

        Assert.Contains(results, r => r.Value == "Anna Schoenmakers");
    }

    [Fact]
    public void Search_BlockedFallsBackToAFullScanWhenTheBucketsAreTooSmall()
    {
        var index = new DutchFuzzyIndex<string>(
            ["Anna Schoenmakers", "Anne Visser"],
            static value => value);

        var blocked = index.Search("Anne Schoenmakers", 0.82, 10, DutchFuzzyIndexSearchMode.Blocked);

        Assert.Contains(blocked, r => r.Value == "Anna Schoenmakers");
    }

    [Fact]
    public void Search_BlockedOnlyIsDocumentedAsApproximate()
    {
        var index = new DutchFuzzyIndex<string>(
            ["Anna Schoenmakers", "Anne Visser"],
            static value => value);

        var exact = index.Search("Anne Schoenmakers", 0.82, 10, DutchFuzzyIndexSearchMode.Exact);
        var blockedOnly = index.Search("Anne Schoenmakers", 0.82, 10, DutchFuzzyIndexSearchMode.BlockedOnly);

        Assert.Contains(exact, r => r.Value == "Anna Schoenmakers");
        Assert.True(blockedOnly.Count <= exact.Count);
    }

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(0.5, 3)]
    [InlineData(0.7, 5)]
    [InlineData(0.9, 10)]
    public void Search_ExactIsIdenticalToALinearScanOnAdversarialData(double threshold, int take)
    {
        var items = BuildAdversarialDataset();
        var index = new DutchFuzzyIndex<Customer>(items, c => c.Name, DutchFuzzyOptions.Names);

        foreach (var query in Queries)
        {
            var indexed = index.Search(query, threshold, take)
                .Select(m => (m.Value.Id, Score: Math.Round(m.Score, 12)))
                .ToList();

            var linear = items
                .FuzzyMatchDutch(query, c => c.Name, threshold, take, DutchFuzzyOptions.Names)
                .Select(m => (m.Value.Id, Score: Math.Round(m.Score, 12)))
                .ToList();

            Assert.Equal(linear, indexed);
        }
    }

    [Fact]
    public void Search_TakeReturnsThePrefixOfTheCompleteRanking()
    {
        var items = BuildAdversarialDataset();
        var index = new DutchFuzzyIndex<Customer>(items, c => c.Name, DutchFuzzyOptions.Names);

        foreach (var query in Queries)
        {
            var all = index.Search(query, 0.4, take: 0)
                .Select(m => Math.Round(m.Score, 12))
                .Take(4)
                .ToList();

            var top = index.Search(query, 0.4, take: 4)
                .Select(m => Math.Round(m.Score, 12))
                .ToList();

            Assert.Equal(all, top);
        }
    }

    [Fact]
    public void Search_RejectsUnknownModes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateIndex().Search("Jan", 0.5, 10, (DutchFuzzyIndexSearchMode)42));
    }

    [Fact]
    public void Search_UsesTheDefaultThresholdOfItsOptions()
    {
        var loose = DutchFuzzyOptions.Names with { DefaultThreshold = 0.1 };
        var strict = DutchFuzzyOptions.Names with { DefaultThreshold = 0.999 };

        Assert.NotEmpty(new DutchFuzzyIndex<Customer>(Customers, c => c.Name, loose).Search("Jan Meyer"));
        Assert.Empty(new DutchFuzzyIndex<Customer>(Customers, c => c.Name, strict).Search("Jan Meyer"));
    }

    private static readonly string[] Queries =
    [
        "Anne Schoenmakers",
        "Jan Meyer",
        "Vandenbroecke",
        "Klaas Fisser",
        "Zzyzx Qqq",
        "Bakkers",
        "de Bruyn",
        string.Empty,
    ];

    private static List<Customer> BuildAdversarialDataset()
    {
        var firstNames = new[] { "Anna", "Anne", "Jan", "Johan", "Klaas", "Piet", "Marieke", "Sanne" };
        var lastNames = new[]
        {
            "Schoenmakers", "Schoonmakers", "Visser", "Fisser", "Meijer", "Meier", "Bakker",
            "Bakkers", "Vandenbroecke", "Van den Broeke", "de Bruijn", "Bruyn", "Kuiper", "Kuipers",
        };

        var random = new Random(20260731);
        var items = new List<Customer>();

        for (var i = 0; i < 400; i++)
        {
            var first = firstNames[random.Next(firstNames.Length)];
            var last = lastNames[random.Next(lastNames.Length)];

            items.Add(new Customer(i, $"{first} {last}"));
        }

        return items;
    }
}
