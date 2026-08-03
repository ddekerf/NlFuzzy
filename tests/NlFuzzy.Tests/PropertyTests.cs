using System;
using System.Linq;
using System.Text;
using NlFuzzy.Normalization;

namespace NlFuzzy.Tests;

/// <summary>
/// Seeded fuzz tests for the invariants the public contract promises, on deliberately hostile
/// input: mixed scripts, combining marks, ligatures, punctuation and very long strings.
/// </summary>
public class PropertyTests
{
    private const int Iterations = 750;

    private static readonly char[] Alphabet =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ áéíóúäëïöüàèìòùçñßẞæÆœŒøØĳĲđĐǽ '-.,0123456789"
            .ToCharArray();

    private static readonly DutchFuzzyOptions[] Profiles =
    [
        DutchFuzzyOptions.Default,
        DutchFuzzyOptions.Names,
        DutchFuzzyOptions.Words,
        DutchFuzzyOptions.Default with { MaxKeyLength = 6 },
        DutchFuzzyOptions.Default with { RemoveDiacritics = false },
        DutchFuzzyOptions.Default with { EncodeVowels = true, Profile = DutchPhoneticProfile.Strict },
    ];

    [Fact]
    public void Similarity_IsAlwaysFiniteInsideTheUnitInterval()
    {
        var random = new Random(1);

        for (var i = 0; i < Iterations; i++)
        {
            var options = Profiles[random.Next(Profiles.Length)];
            var a = RandomText(random);
            var b = random.Next(4) == 0 ? a : RandomText(random);

            var score = DutchFuzzy.Similarity(a, b, options);

            Assert.False(double.IsNaN(score), $"NaN for '{a}' vs '{b}'.");
            Assert.False(double.IsInfinity(score), $"Infinite for '{a}' vs '{b}'.");
            Assert.InRange(score, 0d, 1d);
        }
    }

    [Fact]
    public void Similarity_IsReflexiveAndSymmetric()
    {
        var random = new Random(2);

        for (var i = 0; i < Iterations; i++)
        {
            var options = Profiles[random.Next(Profiles.Length)];
            var a = RandomText(random);
            var b = RandomText(random);

            Assert.Equal(
                DutchFuzzy.Similarity(a, b, options),
                DutchFuzzy.Similarity(b, a, options),
                10);

            Assert.Equal(1d, DutchFuzzy.Similarity(a, a, options), 10);
        }
    }

    [Fact]
    public void Normalization_IsIdempotent()
    {
        var random = new Random(3);

        for (var i = 0; i < Iterations; i++)
        {
            var options = Profiles[random.Next(Profiles.Length)];
            var once = DutchTextNormalizer.Instance.Normalize(RandomText(random), options);
            var twice = DutchTextNormalizer.Instance.Normalize(once, options);

            Assert.Equal(once, twice);
        }
    }

    [Fact]
    public void CanonicallyEquivalentInputProducesTheSameKey()
    {
        var random = new Random(4);

        for (var i = 0; i < Iterations; i++)
        {
            var options = Profiles[random.Next(Profiles.Length)];
            var value = RandomText(random);
            var composed = value.Normalize(NormalizationForm.FormC);
            var decomposed = value.Normalize(NormalizationForm.FormD);

            if (!options.RemoveDiacritics)
            {
                continue;
            }

            Assert.Equal(
                DutchFuzzy.PhoneticKey(composed, options),
                DutchFuzzy.PhoneticKey(decomposed, options));

            Assert.Equal(
                DutchFuzzy.Similarity(composed, "Kuipers", options),
                DutchFuzzy.Similarity(decomposed, "Kuipers", options),
                10);
        }
    }

    [Fact]
    public void PhoneticKey_OnlyUsesTheDocumentedOutputAlphabet()
    {
        const string Alfabet = "PBTDKGFVWSZXJLMNRHQA1234567 ";

        var random = new Random(5);

        for (var i = 0; i < Iterations; i++)
        {
            var options = Profiles[random.Next(Profiles.Length)];
            var key = DutchFuzzy.PhoneticKey(RandomText(random), options);

            foreach (var c in key)
            {
                Assert.Contains(c, Alfabet);
            }
        }
    }

    [Fact]
    public void RankingIsConsistentWithTheScorer()
    {
        var random = new Random(6);
        var items = Enumerable.Range(0, 200).Select(_ => RandomText(random)).ToList();
        var query = "Schoenmakers";

        var matches = items.FuzzyMatchDutch(query, 0d, take: 0).ToList();

        Assert.Equal(items.Count, matches.Count);

        for (var i = 1; i < matches.Count; i++)
        {
            Assert.True(matches[i - 1].Score >= matches[i].Score);
        }

        foreach (var match in matches)
        {
            Assert.Equal(DutchFuzzy.Similarity(query, match.Text), match.Score, 10);
        }
    }

    [Fact]
    public void LongInputStaysWithinTheContract()
    {
        var random = new Random(7);
        var long1 = new string('a', 4_000);
        var long2 = new string('a', 3_999) + "b";

        Assert.InRange(DutchFuzzy.Similarity(long1, long2), 0d, 1d);
        Assert.InRange(DutchFuzzy.Similarity(long1, RandomText(random, 2_000)), 0d, 1d);
    }

    private static string RandomText(Random random, int maxLength = 24)
    {
        var length = random.Next(0, maxLength);
        var builder = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            builder.Append(Alphabet[random.Next(Alphabet.Length)]);

            if (random.Next(12) == 0)
            {
                // A combining acute accent, so the generator also produces decomposed forms.
                builder.Append('\u0301');
            }
        }

        return builder.ToString();
    }
}
