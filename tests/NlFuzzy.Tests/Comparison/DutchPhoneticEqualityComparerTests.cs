using System.Collections.Generic;
using System.Linq;
using NlFuzzy.Comparison;

namespace NlFuzzy.Tests.Comparison;

public class DutchPhoneticEqualityComparerTests
{
    [Theory]
    [InlineData("meijer", "meier")]
    [InlineData("bruijn", "bruyn")]
    [InlineData("smit", "smid")]
    [InlineData("Philips", "filips")]
    public void Equals_ReturnsTrueForSameSoundingStrings(string a, string b)
    {
        Assert.True(DutchPhoneticEqualityComparer.Default.Equals(a, b));
        Assert.Equal(
            DutchPhoneticEqualityComparer.Default.GetHashCode(a),
            DutchPhoneticEqualityComparer.Default.GetHashCode(b));
    }

    [Theory]
    [InlineData("bakker", "dekker")]
    [InlineData("kuipers", "visser")]
    public void Equals_ReturnsFalseForDifferentSoundingStrings(string a, string b)
    {
        Assert.False(DutchPhoneticEqualityComparer.Default.Equals(a, b));
    }

    [Fact]
    public void Equals_HandlesNulls()
    {
        var comparer = DutchPhoneticEqualityComparer.Default;

        Assert.True(comparer.Equals(null, null));
        Assert.False(comparer.Equals(null, "a"));
        Assert.False(comparer.Equals("a", null));
    }

    [Fact]
    public void GetHashCode_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => DutchPhoneticEqualityComparer.Default.GetHashCode(null!));
    }

    [Fact]
    public void WorksAsDictionaryComparer()
    {
        var dictionary = new Dictionary<string, int>(DutchPhoneticEqualityComparer.Default)
        {
            ["Meijer"] = 1,
        };

        Assert.True(dictionary.ContainsKey("Meier"));
        Assert.Equal(1, dictionary["meyer"]);
    }

    [Fact]
    public void WorksAsHashSetComparer()
    {
        var set = new HashSet<string>(
            ["Bruijn", "Bruyn", "de Bruin", "Bakker"],
            DutchPhoneticEqualityComparer.Default);

        Assert.Equal(3, set.Count);
    }

    [Fact]
    public void GroupBy_CollapsesSpellingVariants()
    {
        var groups = new[] { "Meijer", "Meier", "Meyer", "Bakker" }
            .GroupBy(x => x, DutchPhoneticEqualityComparer.Default)
            .ToList();

        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void CustomOptions_AreHonored()
    {
        var strict = new DutchPhoneticEqualityComparer(
            DutchFuzzyOptions.Default with { Profile = DutchPhoneticProfile.Strict });

        Assert.False(strict.Equals("vader", "wader"));
        Assert.True(DutchPhoneticEqualityComparer.Default.Equals("vader", "wader"));
    }
}
