using NlFuzzy.Phonetics;

namespace NlFuzzy.Tests.Phonetics;

public class ColognePhoneticEncoderTests
{
    // Reference values from the original Kölner Phonetik description.
    [Theory]
    [InlineData("Wikipedia", "3412")]
    [InlineData("Breschnew", "17863")]
    [InlineData("Muellerluedenscheidt", "65752682")]
    [InlineData("Peter", "127")]
    [InlineData("Petra", "127")]
    public void EncodeWord_MatchesReferenceValues(string value, string expected)
    {
        Assert.Equal(expected, ColognePhoneticEncoder.EncodeWord(value));
    }

    [Fact]
    public void EncodeWord_ReturnsEmptyForEmptyInput()
    {
        Assert.Equal(string.Empty, ColognePhoneticEncoder.EncodeWord(string.Empty));
    }

    [Fact]
    public void Encode_JoinsTokenKeys()
    {
        var encoder = ColognePhoneticEncoder.Default;

        Assert.Equal("127 127", encoder.Encode("Peter Petra"));
        Assert.Equal(2, encoder.EncodeTokens("Peter Petra").Count);
    }

    [Fact]
    public void Encode_IsDiacriticInsensitive()
    {
        Assert.Equal(
            ColognePhoneticEncoder.Default.Encode("Muller"),
            ColognePhoneticEncoder.Default.Encode("Müller"));
    }

    [Theory]
    [InlineData("Änne", "Anne")]
    [InlineData("Müller", "Muller")]
    [InlineData("Lüdenscheidt", "Ludenscheidt")]
    [InlineData("Öl", "Ol")]
    [InlineData("Straße", "Strasse")]
    [InlineData("STRAẞE", "Strasse")]
    public void EncodeWord_AppliesTheSameGermanPreprocessingAsEncode(string value, string ascii)
    {
        var encoded = ColognePhoneticEncoder.EncodeWord(value);

        Assert.NotEqual(string.Empty, encoded);
        Assert.Equal(ColognePhoneticEncoder.EncodeWord(ascii), encoded);
        Assert.Equal(ColognePhoneticEncoder.Default.Encode(value), encoded);
    }

    [Theory]
    [InlineData("Änne", "06")]
    [InlineData("Öl", "05")]
    public void EncodeWord_TreatsUmlautsAsTheirBaseVowel(string value, string expected)
    {
        Assert.Equal(expected, ColognePhoneticEncoder.EncodeWord(value));
    }

    [Fact]
    public void ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => ColognePhoneticEncoder.EncodeWord(null!));
        Assert.Throws<ArgumentNullException>(() => ColognePhoneticEncoder.Default.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => ColognePhoneticEncoder.Default.EncodeTokens(null!));
        Assert.Throws<ArgumentNullException>(() => new ColognePhoneticEncoder(null!));
    }
}
