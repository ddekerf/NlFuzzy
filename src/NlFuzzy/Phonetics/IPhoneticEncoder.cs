using System.Collections.Generic;

namespace NlFuzzy.Phonetics;

/// <summary>Converts text into a phonetic key.</summary>
public interface IPhoneticEncoder
{
    /// <summary>Encodes a single word or a whole phrase into a phonetic key.</summary>
    /// <param name="value">The text to encode. May be null or empty.</param>
    /// <returns>The phonetic key; an empty string when there is nothing to encode.</returns>
    string Encode(string value);

    /// <summary>Encodes each whitespace-separated token separately.</summary>
    /// <param name="value">The text to encode. May be null or empty.</param>
    /// <returns>One key per token, in the order they appear.</returns>
    IReadOnlyList<string> EncodeTokens(string value);
}
