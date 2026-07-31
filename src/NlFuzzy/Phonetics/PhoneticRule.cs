using System;

namespace NlFuzzy.Phonetics;

/// <summary>
/// A single rewrite rule of the Dutch phonetic rule table.
/// </summary>
internal sealed class PhoneticRule
{
    internal PhoneticRule(
        int id,
        string pattern,
        string strictOutput,
        string tolerantOutput,
        string description,
        Func<string, int, int, bool>? context = null)
    {
        Id = id;
        Pattern = pattern;
        StrictOutput = strictOutput;
        TolerantOutput = tolerantOutput;
        Description = description;
        Context = context;
    }

    /// <summary>The stable rule number, used by the rule coverage test.</summary>
    internal int Id { get; }

    /// <summary>The literal lower-case pattern this rule matches.</summary>
    internal string Pattern { get; }

    /// <summary>The phoneme(s) emitted under <see cref="DutchPhoneticProfile.Strict"/>.</summary>
    internal string StrictOutput { get; }

    /// <summary>The phoneme(s) emitted under <see cref="DutchPhoneticProfile.Tolerant"/>.</summary>
    internal string TolerantOutput { get; }

    /// <summary>A human readable description used in test failure messages.</summary>
    internal string Description { get; }

    /// <summary>The positional condition, or null when the rule always applies.</summary>
    internal Func<string, int, int, bool>? Context { get; }

    /// <summary>The number of input characters consumed when this rule fires.</summary>
    internal int Length => Pattern.Length;

    internal string OutputFor(DutchPhoneticProfile profile) =>
        profile == DutchPhoneticProfile.Strict ? StrictOutput : TolerantOutput;

    internal bool Matches(string token, int index)
    {
        if (index + Pattern.Length > token.Length)
        {
            return false;
        }

        for (var i = 0; i < Pattern.Length; i++)
        {
            if (token[index + i] != Pattern[i])
            {
                return false;
            }
        }

        return Context is null || Context(token, index, Pattern.Length);
    }
}
