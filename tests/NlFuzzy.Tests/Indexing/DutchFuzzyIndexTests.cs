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

        var bucketed = index.Search("Jan Meyer", 0.5, 10, allowFullScan: false);

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
}
