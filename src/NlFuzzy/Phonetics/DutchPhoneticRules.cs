using System;
using System.Collections.Generic;

namespace NlFuzzy.Phonetics;

/// <summary>
/// The normative Dutch phonetic rule table. Rules are applied longest-match-first at every
/// position of a normalized token.
/// </summary>
/// <remarks>
/// Output alphabet: P B T D K G F V W S Z X J L M N R H Q for consonants and the digits
/// 1..7 for vowel classes (1 = a, 2 = e, 3 = i/ie, 4 = o, 5 = u, 6 = ei/ij/y, 7 = diphthongs).
/// </remarks>
internal static class DutchPhoneticRules
{
    private static readonly Func<string, int, int, bool> AtStart = (token, index, length) => index == 0;

    private static readonly Func<string, int, int, bool> AtEnd =
        (token, index, length) => index + length == token.Length;

    private static readonly Func<string, int, int, bool> BeforeVowel =
        (token, index, length) => index + length < token.Length && IsVowelLetter(token[index + length]);

    private static readonly Func<string, int, int, bool> StartAndBeforeVowel =
        (token, index, length) => index == 0 && index + length < token.Length && IsVowelLetter(token[index + length]);

    private static readonly Func<string, int, int, bool> BeforeSoftVowel =
        (token, index, length) =>
        {
            if (index + length >= token.Length)
            {
                return false;
            }

            var next = token[index + length];

            return next == 'e' || next == 'i' || next == 'y';
        };

    private static readonly Func<string, int, int, bool> BeforeR =
        (token, index, length) => index + length < token.Length && token[index + length] == 'r';

    private static readonly Func<string, int, int, bool> AfterT =
        (token, index, length) => index > 0 && token[index - 1] == 't';

    private static readonly Func<string, int, int, bool> FinalObstruent =
        (token, index, length) =>
        {
            var next = index + length;

            if (next == token.Length)
            {
                return true;
            }

            return next == token.Length - 1 && (token[next] == 't' || token[next] == 's');
        };

    private static readonly PhoneticRule[] AllRules = BuildRules();

    private static readonly Dictionary<char, PhoneticRule[]> Lookup = BuildLookup(AllRules);

    /// <summary>All rules, in declaration order.</summary>
    internal static IReadOnlyList<PhoneticRule> Rules => AllRules;

    /// <summary>
    /// Returns the first rule that matches at <paramref name="index"/>, honouring
    /// longest-match-first ordering and positional context. Returns null when no rule applies,
    /// in which case the caller skips one character.
    /// </summary>
    internal static PhoneticRule? FindMatch(string token, int index, DutchPhoneticProfile profile)
    {
        _ = profile;

        if (token is null || index < 0 || index >= token.Length)
        {
            return null;
        }

        if (!Lookup.TryGetValue(token[index], out var candidates))
        {
            return null;
        }

        foreach (var rule in candidates)
        {
            if (rule.Matches(token, index))
            {
                return rule;
            }
        }

        return null;
    }

    internal static bool IsVowelLetter(char c) =>
        c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' || c == 'y';

    private static Dictionary<char, PhoneticRule[]> BuildLookup(PhoneticRule[] rules)
    {
        var grouped = new Dictionary<char, List<PhoneticRule>>();

        foreach (var rule in rules)
        {
            var key = rule.Pattern[0];

            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<PhoneticRule>();
                grouped[key] = list;
            }

            list.Add(rule);
        }

        var result = new Dictionary<char, PhoneticRule[]>(grouped.Count);

        foreach (var pair in grouped)
        {
            var ordered = new List<PhoneticRule>(pair.Value);

            // Longest match first; declaration order decides between rules of equal length,
            // so the more specific contextual variant must be declared first.
            ordered.Sort((left, right) =>
            {
                var byLength = right.Length.CompareTo(left.Length);

                return byLength != 0 ? byLength : left.Id.CompareTo(right.Id);
            });

            result[pair.Key] = ordered.ToArray();
        }

        return result;
    }

    private static PhoneticRule[] BuildRules()
    {
        var id = 0;
        var rules = new List<PhoneticRule>();

        void Add(string pattern, string strict, string tolerant, string description,
            Func<string, int, int, bool>? context = null)
        {
            rules.Add(new PhoneticRule(++id, pattern, strict, tolerant, description, context));
        }

        // --- sibilants and affricates -------------------------------------------------
        Add("sch", "S", "S", "Word-final -sch is silent: Bosch sounds like Bos", AtEnd);
        Add("sch", "SG", "SG", "sch elsewhere: school, misschien");
        Add("sk", "SK", "SG", "sk is the phonetic respelling of sch in Tolerant mode: skool/school");
        Add("sj", "X", "X", "sj as in sjaal");
        Add("sh", "X", "X", "sh as in shampoo");
        Add("tsj", "X", "X", "tsj as in tsjilpen");

        // --- velar fricatives ---------------------------------------------------------
        Add("cht", "GT", "GT", "cht as in nacht, gracht");
        Add("ch", "X", "X", "Word-initial ch is a French loan sound: chef, chocolade", AtStart);
        Add("ch", "G", "G", "ch elsewhere sounds like g: lachen");
        Add("cks", "KS", "KS", "cks as in Dircks");
        Add("ck", "K", "K", "ck as in Dircks");
        Add("cq", "K", "K", "cq as in Cocquyt");
        Add("c", "S", "S", "c before e, i or y sounds like s: cent, cigaret", BeforeSoftVowel);
        Add("c", "K", "K", "c elsewhere sounds like k: cactus, Claes");

        // --- plosives and clusters ----------------------------------------------------
        Add("qu", "KW", "KF", "qu as in quiz");
        Add("q", "K", "K", "q alone sounds like k");
        Add("x", "KS", "KS", "x spells ks: Dirx");
        Add("ks", "KS", "KS", "ks as in Luiks");
        Add("ph", "F", "F", "ph sounds like f: photo/foto");
        Add("th", "T", "T", "th sounds like t: Thijs/Tijs");
        Add("dt", "T", "T", "Word-final dt sounds like t: Brandt, De Smedt", AtEnd);
        Add("tie", "S3", "S3", "Word-final -tie sounds like -sie: actie, natie", AtEnd);
        Add("ti", "S", "S", "ti before a vowel sounds like s: natio-, patiënt", BeforeVowel);
        Add("nk", "QK", "QK", "nk as in bank");
        Add("ng", "Q", "Q", "ng is the velar nasal: zingen, Jong");
        Add("gh", "G", "G", "gh sounds like g: Verbrugghe/Verbrugge");
        Add("g", "G", "G", "g as in goed");
        Add("b", "P", "P", "Word-final b is devoiced: heb", AtEnd);
        Add("b", "B", "P", "b elsewhere; merged with p in Tolerant mode");
        Add("d", "T", "T", "Word-final d is devoiced, also before a final t or s: hond, honds",
            FinalObstruent);
        Add("d", "D", "T", "d elsewhere; merged with t in Tolerant mode");

        // --- fricatives ---------------------------------------------------------------
        Add("v", "F", "F", "Word-final v is devoiced", AtEnd);
        Add("v", "V", "F", "v elsewhere; merged with f in Tolerant mode");
        Add("w", "V", "F", "w before r sounds like v: wrede/vrede", BeforeR);
        Add("w", "W", "F", "w elsewhere; merged with f/v in Tolerant mode");
        Add("z", "S", "S", "Word-final z is devoiced", AtEnd);
        Add("z", "Z", "S", "z elsewhere; merged with s in Tolerant mode");
        Add("h", "H", "", "Word-initial h before a vowel; dropped in Tolerant mode",
            StartAndBeforeVowel);
        Add("h", "", "", "h is dropped everywhere else");
        Add("j", "X", "X", "j after t sounds like sj: tjokvol", AfterT);
        Add("j", "J", "J", "j as in jaar");

        // --- plain consonants ---------------------------------------------------------
        Add("f", "F", "F", "f as in fiets");
        Add("k", "K", "K", "k as in kat");
        Add("l", "L", "L", "l as in laan");
        Add("m", "M", "M", "m as in maan");
        Add("n", "N", "N", "n as in naam");
        Add("p", "P", "P", "p as in pot");
        Add("r", "R", "R", "r as in raam");
        Add("s", "S", "S", "s as in sok");
        Add("t", "T", "T", "t as in tak");

        // --- vowels and diphthongs ----------------------------------------------------
        Add("aaij", "1", "1", "aaij is the long a class: Braaijen");
        Add("aai", "1", "1", "aai is the long a class: draaien");
        Add("aay", "1", "1", "aay is the long a class");
        Add("aauw", "7", "7", "aauw is the historic spelling of auw: Blaauw/Blauw");
        Add("auw", "7", "7", "auw belongs to the ou/au diphthong class: kauw");        Add("aa", "1", "1", "aa is the long a class: Maas");
        Add("ae", "1", "1", "ae is the historic spelling of aa: Maes/Maas");
        Add("ah", "1", "1", "ah is the long a class");
        Add("au", "7", "7", "au belongs to the ou/au diphthong class");
        Add("a", "1", "1", "a class");
        Add("eeuw", "2", "2", "eeuw is the long e class: leeuw");
        Add("eij", "6", "6", "eij is the historic spelling of ei: Reijkaard");
        Add("eau", "4", "4", "eau is the o class: cadeau/kadoo");
        Add("ee", "2", "2", "ee is the long e class: Peeters/Peters");
        Add("ei", "6", "6", "ei and ij are homophones");
        Add("ey", "6", "6", "ey is the historic spelling of ei: Meyer");
        Add("eu", "7", "7", "eu belongs to the diphthong class: neus");
        Add("eh", "2", "2", "eh is the long e class");
        Add("e", "2", "2", "e class");
        Add("ieuw", "3", "3", "ieuw is the i class: nieuw");
        Add("ieh", "3", "3", "ieh is the i class");
        Add("ie", "3", "3", "ie is the i class: fiets");
        Add("ij", "6", "6", "ij and ei are homophones: Thijs");
        Add("i", "3", "3", "i class");
        Add("ooij", "4", "4", "ooij is the o class: Ooijen");
        Add("ooi", "4", "4", "ooi is the o class: mooi");
        Add("ooy", "4", "4", "ooy is the o class: Ooyen");
        Add("ouw", "7", "7", "ouw belongs to the ou/au diphthong class: vrouw");
        Add("oo", "4", "4", "oo is the long o class: kadoo");
        Add("oe", "7", "7", "oe belongs to the diphthong class: boek");
        Add("oh", "4", "4", "oh is the long o class");
        Add("ou", "7", "7", "ou belongs to the ou/au diphthong class: koud");
        Add("o", "4", "4", "o class");
        Add("uij", "7", "7", "uij is the historic spelling of ui: Bruijn");
        Add("ui", "7", "7", "ui belongs to the diphthong class: Bruin");
        Add("uy", "7", "7", "uy is the historic spelling of ui: Bruyn, Kuypers");
        Add("uu", "5", "5", "uu is the long u class");
        Add("uh", "5", "5", "uh is the long u class");
        Add("uw", "5", "5", "uw is the u class: ruw");
        Add("u", "5", "5", "u class");
        Add("y", "J", "J", "y before a vowel is a consonant: yoghurt", BeforeVowel);
        Add("y", "6", "6", "y elsewhere spells the ei/ij sound: Bruyn, Symons");

        return rules.ToArray();
    }
}
