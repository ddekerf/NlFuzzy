# Implementatieplan: `NlFuzzy` — Nederlandse sounds-like / fuzzy string matching voor .NET

Status: ontwerp, klaar voor implementatie
Doelgroep van dit document: een AI-coding-agent (GitHub Copilot, Opus 5) die dit repo-per-repo uitvoert
Doeltaal: C#, .NET, distributie als NuGet package

---

## 0. Hoe dit document gebruiken

Dit document is de single source of truth voor de implementatie. Werk de fases in
hoofdstuk 9 sequentieel af. Elke fase is afgerond wanneer de bijhorende tests groen zijn.

Regels voor de agent:

1. Wijk niet af van de publieke API in hoofdstuk 5 zonder het plan mee aan te passen.
2. Voeg geen runtime NuGet-dependencies toe. Zie hoofdstuk 3, beslissing D3.
3. Elke regel uit de fonetische regeltabel (hoofdstuk 6.3) krijgt minstens één test.
4. De gouden dataset (hoofdstuk 7.2) is normatief: als een testcase faalt, pas de
   regeltabel aan, niet de dataset — tenzij de dataset aantoonbaar fout is.
5. Alles wat publiek is krijgt XML-documentatie in het Nederlands of Engels, maar
   consistent (voorstel: Engels in code, Nederlands in README).

---

## 1. Doel en scope

### 1.1 Wat het moet doen

- Twee strings vergelijken op basis van hoe ze in het Nederlands klinken
  ("Bruijn" ≈ "Bruyn" ≈ "Bruin"), niet op basis van Engelse uitspraakregels.
- Een fonetische sleutel produceren die je kan persisteren en indexeren
  (kolom in SQL, index in Azure AI Search, key in een dictionary).
- Een genormaliseerde gelijkenisscore tussen 0.0 en 1.0 geven, die fonetiek én
  spelafstand combineert.
- Eenvoudige extension methods aanbieden, zodat een gebruiker in één regel klaar is.
- Werken op zowel persoonsnamen/familienamen (het klassieke gebruiksgeval) als op
  gewone Nederlandse woorden, straatnamen en gemeentenamen.

### 1.2 Wat het niet moet doen (v1)

- Geen taaldetectie (BMPM-stijl). De library gaat er van uit dat de invoer Nederlands is.
- Geen semantische matching, geen embeddings, geen ML.
- Geen full-text search engine. Wel: bouwstenen die je in een search engine kan hangen.
- Geen ondersteuning voor niet-Latijnse schriften.

### 1.3 Bekende beperkingen die expliciet in de README moeten

- Fonetische codering is per definitie lossy: hogere recall, lagere precisie.
- Er bestaan veel Nederlandse en Vlaamse dialecten; de encoder mikt op een
  regio-agnostische codering en zal daardoor voor sommige dialecten te grof zijn.

---

## 2. Onderzoek: wat bestaat er al voor het Nederlands

Dit hoofdstuk is de motivatie voor de ontwerpkeuzes. Alle bronnen staan in hoofdstuk 11.

### 2.1 Generieke fonetische algoritmes (niet Nederlands)

| Algoritme | Taal waarvoor ontworpen | Bruikbaar voor NL |
|---|---|---|
| Soundex | Engels | Slecht. <cite index="1-1">Soundex is een fonetisch algoritme voor het indexeren van namen naar hun uitspraak in het Engels.</cite> Vaste lengte van 4 tekens, Engelse letterklassen. |
| Metaphone / Double Metaphone | Engels, DM minder Engels-centrisch | Matig zonder aanpassing, maar wel een goede *basis* om NL-regels op te enten. |
| NYSIIS, Caverphone, Match Rating | Engels / Nieuw-Zeelands | Niet relevant. |
| Kölner Phonetik (Postel, 1969) | Duits | Redelijk bruikbaar voor NL als baseline. <cite index="41-1">Het algoritme mapt elke letter op een cijfer tussen 0 en 8, is verwant aan Soundex maar geoptimaliseerd voor het Duits, en werd in 1969 gepubliceerd door Hans Joachim Postel.</cite> Duits en Nederlands delen veel orthografische patronen (sch, ch, ij/ei vs ei, w/v). |
| Daitch-Mokotoff Soundex | Slavische/Germaanse (joodse) familienamen | Beperkt relevant. |
| Beider-Morse Phonetic Matching (BMPM) | Meertalig, inclusief Nederlands | Het meest interessante bestaande werk voor NL. |

### 2.2 Beider-Morse: het enige grote bestaande werk met expliciete NL-ondersteuning

<cite index="12-1">BMPM is ontwikkeld door Alexander Beider (Parijs) en Stephen P. Morse (San Francisco); het doel is herkennen dat twee verschillend gespelde woorden fonetisch equivalent kunnen zijn, waarbij de "klinkt-als"-test niet alleen op spelling maar op linguïstische eigenschappen van talen gebaseerd is.</cite>
<cite index="18-1">De encoding verloopt in meerdere stappen: eerst raadt een regeltabel de taal van het woord, daarna wordt het woord met een taalspecifieke fonetiektabel omgezet, wat een verzameling mogelijke representaties oplevert.</cite>
<cite index="11-1">Er worden 16 talen ondersteund, waaronder Nederlands.</cite>

Relevantie voor ons: BMPM bewijst dat taalspecifieke regeltabellen werken, maar het is
zwaar (grote regelsets, meerdere kandidaat-encodings per woord) en de referentie-
implementatie is GPLv3-gelicenseerd, wat voor een MIT/Apache NuGet package een probleem is.

Praktisch alternatief voor .NET: `Lucene.Net.Analysis.Phonetic` bevat een .NET-port van de
Apache Commons Codec encoders. De NuGet-tags vermelden expliciet
<cite index="21-1">soundex, double metaphone, beider morse, cologne, caverphone, nysiis en match rating</cite>,
en het pakket is <cite index="22-1">Apache-2.0 gelicenseerd</cite>.
Let op: het pakket is al jaren enkel als prerelease (`4.8.0-betaXXXXX`) beschikbaar.

### 2.3 Nederlands-specifiek werk dat ik gevonden heb

**Dutch Metaphone (Diederik Krols, U2U, 2011).** Dit is de meest concrete, publiek
beschikbare Nederlandse fonetische regelset die ik gevonden heb. De auteur beschrijft
dat hij een vrije Metaphone-implementatie in T-SQL heeft genomen en aangepast aan het
Nederlands, en merkt op dat Metaphone alle klinkers behalve de eerste negeert en niet
vastlegt hoe je een 'g' of 'r' uitspreekt, waardoor de encodering redelijk regio-agnostisch
blijft — belangrijk gezien het grote aantal Nederlandse dialecten. Zijn voorbeelden
(vrede/wrede, luycks/luiks, Vandenbroecke/Vandenberghe, sjamaan/china/shampoo) zijn
bruikbaar als testmateriaal. De volledige T-SQL-broncode staat in het artikel.

Uit die regelset overnemen we onder meer: `b` finaal wordt `p`, `d` finaal wordt `t`,
`ch` wordt `x` initieel en `g` intern, `c` voor e/i/y wordt `s`, `ph` wordt `f`,
`s` voor h/j wordt `x`, `ti` + klinker wordt `s`, `x` wordt `ks`, `qu`/`q` wordt `k`.

**Dutch Soundex-varianten in de praktijk.** Er circuleren aangepaste Soundex-versies
waarbij de lettergroepen anders zijn ingedeeld omwille van het verschil tussen de
Nederlandse en Engelse uitspraak — bijvoorbeeld beschreven in een Oracle-context door AMIS.
Ook in .NET-blogposts (Jan Jongboom, 2010) wordt een "DutchSoundex" gebruikt in
combinatie met diakrietverwijdering via `String.Normalize(NormalizationForm.FormD)`.
Kwaliteit: hobbyniveau, geen testsuite, geen package. Bruikbaar als inspiratie, niet als basis.

**Wat ik niet gevonden heb.** Ik vond in mijn zoekopdrachten geen onderhouden NuGet-package
die specifiek Nederlandse fonetische matching aanbiedt. Dat is geen bewijs dat het niet
bestaat — controleer voor publicatie zelf nuget.org op naamconflicten en op eventuele
bestaande packages.

### 2.4 Linguïstische feiten die de regeltabel schragen

- Eind-verstemlozing (final devoicing): Nederlands kent dit systematisch;
  <cite index="70-1">stemhebbende obstruenten aan het woordeinde worden stemloos, en de vroegste sporen hiervan verschijnen in het Oudnederlands rond de 9e of 10e eeuw.</cite>
  Concreet: `heb` klinkt op `p`, `hond` op `t`. Dit rechtvaardigt de b→p en d→t regels finaal.
- `ei` en `ij` zijn homofoon. Taalportaal (Meertens/INT) stelt:
  <cite index="73-1">het Nederlands spelt [ɛi] op twee manieren, als &lt;ei&gt; en als &lt;ij&gt;; die weerspiegelen verschillende historische oorsprong, maar geen enkele standaardvariëteit van het Nederlands maakt dat onderscheid in uitspraak nog.</cite>
  Dit rechtvaardigt het samenvallen van ei/ij/y in één klinkerklasse.
- `ou` en `au` vallen samen, net als `ui`/`uy`. Dit is algemeen bekend in beschrijvingen
  van de Nederlandse diftongen (ei/ij, ui, ou/au als de drie kerndiftongen).

### 2.5 Bestaande .NET-bibliotheken voor de afstandskant

- `F23.StringSimilarity` — .NET-port van java-string-similarity;
  <cite index="47-1">bevat onder meer Levenshtein en varianten, Jaro-Winkler, Longest Common Subsequence en cosine similarity.</cite>
- `FuzzyCompare` — <cite index="52-1">Jaro-Winkler en Levenshtein op basis van `ReadOnlySpan&lt;char&gt;`.</cite>
- `Lucene.Net.Analysis.Phonetic` — zie 2.2.

Conclusie: de afstandsalgoritmes zijn triviaal en goed gedocumenteerd; wij implementeren
ze zelf om dependency-vrij te blijven (zie D3).

---

## 3. Ontwerpbeslissingen

| ID | Beslissing | Motivatie |
|---|---|---|
| D1 | Eigen Nederlandse fonetische encoder, regel-gebaseerd, geïnspireerd op Metaphone + de Krols-regelset + Nederlandse orthografieregels. | Geen bestaande NL-encoder met acceptabele licentie en testdekking. |
| D2 | Kölner Phonetik meeleveren als tweede, onafhankelijke encoder. | Goede baseline, publiek gedocumenteerd, laat vergelijking en fallback toe. |
| D3 | Nul runtime-dependencies. Levenshtein, Damerau-Levenshtein, Jaro-Winkler en Dice zelf implementeren. | De algoritmes zijn klein en stabiel; een package zonder dependencies is veel makkelijker te adopteren in enterprise-context (o.a. supply chain review). |
| D4 | Multi-targeting: `netstandard2.0;net8.0;net10.0`. | netstandard2.0 dekt oude .NET Framework-consumers; de moderne TFM's laten `ReadOnlySpan<char>`-optimalisaties en `System.Text.Rune` toe. |
| D5 | Alles immutable en thread-safe; encoders zijn stateless singletons. | Bedoeld voor gebruik in Azure Functions / APIM-backends met hoge concurrency. |
| D6 | Configuratie via één `DutchFuzzyOptions` record met een `Default`-instantie. | Voorspelbaar, testbaar, geen DI vereist maar wel DI-vriendelijk. |
| D7 | Geen `AutoMapper`-achtige indirectie; directe, expliciete mapping en pure functies. | Consistent met bestaande voorkeuren in de codebase. |
| D8 | Twee profielen: `Tolerant` (default) en `Strict`. | Namen matchen vraagt tolerantie (v/f/w, z/s samenvoegen); woordenlijsten matchen vraagt precisie. |
| D9 | Licentie MIT. | Geen afgeleide code van GPL-bronnen. Belangrijk: de BMPM-referentie-implementatie is GPLv3 — daar mag geen code uit overgenomen worden. |

---

## 4. Repo-structuur

```
NlFuzzy/
  NlFuzzy.sln
  Directory.Build.props
  Directory.Packages.props
  README.md
  LICENSE
  CHANGELOG.md
  .editorconfig
  .github/
    workflows/
      ci.yml
      release.yml
  src/
    NlFuzzy/
      NlFuzzy.csproj
      DutchFuzzy.cs                          -- statische facade
      DutchFuzzyOptions.cs
      DutchPhoneticProfile.cs
      FuzzyMatch.cs
      Extensions/
        DutchStringExtensions.cs
        DutchEnumerableExtensions.cs
      Normalization/
        IDutchTextNormalizer.cs
        DutchTextNormalizer.cs
        DutchAffixes.cs                      -- tussenvoegsels, prefixen
      Phonetics/
        IPhoneticEncoder.cs
        DutchPhoneticEncoder.cs
        DutchPhoneticRules.cs                -- de regeltabel
        ColognePhoneticEncoder.cs
      Distance/
        IStringDistance.cs
        IStringSimilarity.cs
        LevenshteinDistance.cs
        DamerauLevenshteinDistance.cs
        JaroWinklerSimilarity.cs
        DiceCoefficientSimilarity.cs
        TokenSetSimilarity.cs
      Comparison/
        DutchFuzzyScorer.cs
        DutchPhoneticEqualityComparer.cs
      Indexing/
        DutchFuzzyIndex.cs
  tests/
    NlFuzzy.Tests/
      NlFuzzy.Tests.csproj
      Normalization/DutchTextNormalizerTests.cs
      Phonetics/DutchPhoneticEncoderTests.cs
      Phonetics/DutchPhoneticRuleCoverageTests.cs
      Phonetics/ColognePhoneticEncoderTests.cs
      Distance/*.cs
      Comparison/DutchFuzzyScorerTests.cs
      Golden/GoldenDatasetTests.cs
      Golden/positive-pairs.csv
      Golden/negative-pairs.csv
      Extensions/ExtensionTests.cs
  bench/
    NlFuzzy.Benchmarks/
      NlFuzzy.Benchmarks.csproj
      EncoderBenchmarks.cs
  samples/
    NlFuzzy.Sample.Console/
```

---

## 5. Publieke API

Dit is de contractdefinitie. Implementeer exact deze signatures.

### 5.1 Opties en profielen

```csharp
namespace NlFuzzy;

/// <summary>Determines how aggressively phonetically similar sounds are merged.</summary>
public enum DutchPhoneticProfile
{
    /// <summary>Keeps voicing distinctions except for word-final devoicing. Higher precision.</summary>
    Strict = 0,

    /// <summary>Merges b/p, d/t, v/f/w, z/s and g/ch. Higher recall. Recommended for names.</summary>
    Tolerant = 1
}

/// <summary>Immutable configuration for all Dutch fuzzy matching operations.</summary>
public sealed record DutchFuzzyOptions
{
    /// <summary>The default options: Tolerant profile, name-oriented normalization.</summary>
    public static DutchFuzzyOptions Default { get; } = new();

    /// <summary>Options tuned for comparing ordinary Dutch words rather than proper names.</summary>
    public static DutchFuzzyOptions Words { get; } = new()
    {
        Profile = DutchPhoneticProfile.Strict,
        StripNamePrefixes = false,
        SplitGluedNamePrefixes = false,
        EncodeVowels = true
    };

    /// <summary>Phonetic profile. Default: <see cref="DutchPhoneticProfile.Tolerant"/>.</summary>
    public DutchPhoneticProfile Profile { get; init; } = DutchPhoneticProfile.Tolerant;

    /// <summary>Remove diacritics before encoding. Default: true.</summary>
    public bool RemoveDiacritics { get; init; } = true;

    /// <summary>
    /// Move Dutch name prefixes (van, van der, de, ten, ...) to the end of the string so that
    /// "Van den Broeke" and "Broeke, van den" normalize identically. Default: true.
    /// </summary>
    public bool StripNamePrefixes { get; init; } = true;

    /// <summary>
    /// Also split glued Flemish prefixes ("Vandenbroecke" -> "van den broecke",
    /// "Verbrugghe" -> "van der brugghe"). Increases recall, decreases precision. Default: false.
    /// </summary>
    public bool SplitGluedNamePrefixes { get; init; }

    /// <summary>
    /// Encode vowel classes instead of dropping all non-initial vowels. Default: false.
    /// True gives longer, more selective keys.
    /// </summary>
    public bool EncodeVowels { get; init; }

    /// <summary>Maximum length of the produced phonetic key. 0 = unlimited. Default: 0.</summary>
    public int MaxKeyLength { get; init; }

    /// <summary>Weight of the orthographic Jaro-Winkler component. Default: 0.35.</summary>
    public double OrthographicWeight { get; init; } = 0.35;

    /// <summary>Weight of the phonetic edit distance component. Default: 0.45.</summary>
    public double PhoneticWeight { get; init; } = 0.45;

    /// <summary>Weight of the bigram Dice component. Default: 0.20.</summary>
    public double NGramWeight { get; init; } = 0.20;

    /// <summary>Score floor applied when both phonetic keys are identical. Default: 0.90.</summary>
    public double IdenticalKeyFloor { get; init; } = 0.90;

    /// <summary>Default threshold used by <c>DutchFuzzyEquals</c> and search helpers. Default: 0.82.</summary>
    public double DefaultThreshold { get; init; } = 0.82;

    /// <summary>Validates that weights are non-negative and sum to a positive value.</summary>
    public void Validate();
}
```

### 5.2 Kerninterfaces

```csharp
namespace NlFuzzy.Phonetics;

public interface IPhoneticEncoder
{
    /// <summary>Encodes a single word or a whole phrase into a phonetic key.</summary>
    string Encode(string value);

    /// <summary>Encodes each whitespace-separated token separately.</summary>
    IReadOnlyList<string> EncodeTokens(string value);
}

namespace NlFuzzy.Distance;

public interface IStringDistance
{
    /// <summary>Returns a distance where 0 means identical. Range depends on the algorithm.</summary>
    double Distance(string a, string b);
}

public interface IStringSimilarity
{
    /// <summary>Returns a normalized similarity in the closed interval [0, 1].</summary>
    double Similarity(string a, string b);
}

namespace NlFuzzy.Normalization;

public interface IDutchTextNormalizer
{
    string Normalize(string value, DutchFuzzyOptions options);
}
```

### 5.3 Facade

```csharp
namespace NlFuzzy;

/// <summary>Entry point for Dutch phonetic and fuzzy string comparison.</summary>
public static class DutchFuzzy
{
    public static string PhoneticKey(string value, DutchFuzzyOptions? options = null);
    public static IReadOnlyList<string> PhoneticKeys(string value, DutchFuzzyOptions? options = null);
    public static bool SoundsLike(string a, string b, DutchFuzzyOptions? options = null);
    public static double Similarity(string a, string b, DutchFuzzyOptions? options = null);
    public static bool AreEquivalent(string a, string b, double threshold, DutchFuzzyOptions? options = null);
}
```

### 5.4 Extension methods

```csharp
namespace NlFuzzy;

public static class DutchStringExtensions
{
    /// <summary>Returns the Dutch phonetic key for this string.</summary>
    public static string ToDutchPhoneticKey(this string value, DutchFuzzyOptions? options = null);

    /// <summary>True when both strings produce the same Dutch phonetic key.</summary>
    public static bool SoundsLikeDutch(this string value, string other, DutchFuzzyOptions? options = null);

    /// <summary>Combined phonetic + orthographic similarity in [0, 1].</summary>
    public static double DutchSimilarity(this string value, string other, DutchFuzzyOptions? options = null);

    /// <summary>True when <see cref="DutchSimilarity"/> is greater than or equal to the threshold.</summary>
    public static bool DutchFuzzyEquals(this string value, string other, double threshold = 0.82,
                                        DutchFuzzyOptions? options = null);
}

public static class DutchEnumerableExtensions
{
    /// <summary>Ranks the source by Dutch fuzzy similarity to the query, best match first.</summary>
    public static IEnumerable<FuzzyMatch<T>> FuzzyMatchDutch<T>(
        this IEnumerable<T> source,
        string query,
        Func<T, string> selector,
        double threshold = 0.82,
        int take = 10,
        DutchFuzzyOptions? options = null);

    /// <summary>Convenience overload for a sequence of strings.</summary>
    public static IEnumerable<FuzzyMatch<string>> FuzzyMatchDutch(
        this IEnumerable<string> source,
        string query,
        double threshold = 0.82,
        int take = 10,
        DutchFuzzyOptions? options = null);

    /// <summary>Returns the single best match, or default when nothing reaches the threshold.</summary>
    public static FuzzyMatch<T>? BestDutchMatch<T>(
        this IEnumerable<T> source,
        string query,
        Func<T, string> selector,
        double threshold = 0.82,
        DutchFuzzyOptions? options = null);
}

/// <summary>A scored match result.</summary>
public readonly record struct FuzzyMatch<T>(T Value, string Text, double Score, string PhoneticKey);
```

Beoogd gebruik:

```csharp
"Bruijn".SoundsLikeDutch("Bruyn");                 // true
"Vandenbroecke".DutchSimilarity("Van den Broeke"); // ~0.9
"Meijer".ToDutchPhoneticKey();                     // stabiele sleutel voor je index

var klanten = await db.Klanten.ToListAsync();
var treffers = klanten.FuzzyMatchDutch("kuipers", k => k.Familienaam, threshold: 0.8, take: 5);
```

### 5.5 EqualityComparer

Belangrijk correctheidspunt voor de agent: een drempel-gebaseerde vergelijking is
**niet transitief** en mag dus niet als `IEqualityComparer<string>` in een `Dictionary`
of `HashSet` gebruikt worden. Lever daarom alleen een comparer op basis van exacte
sleutelgelijkheid, die wel transitief is.

```csharp
namespace NlFuzzy.Comparison;

/// <summary>
/// Compares strings by their Dutch phonetic key. Suitable as a dictionary or set comparer
/// because key equality is reflexive, symmetric and transitive.
/// </summary>
public sealed class DutchPhoneticEqualityComparer : IEqualityComparer<string>
{
    public static DutchPhoneticEqualityComparer Default { get; }
    public DutchPhoneticEqualityComparer(DutchFuzzyOptions? options = null);
    public bool Equals(string? x, string? y);
    public int GetHashCode(string obj);
}
```

In de XML-docs én de README expliciet vermelden dat `DutchFuzzyEquals` bewust géén
`IEqualityComparer` implementeert.

### 5.6 Index voor bulk lookups

```csharp
namespace NlFuzzy.Indexing;

/// <summary>
/// In-memory index that buckets items by phonetic key (blocking), so that a query only has to
/// be scored against candidates in matching buckets instead of the whole collection.
/// </summary>
public sealed class DutchFuzzyIndex<T>
{
    public DutchFuzzyIndex(IEnumerable<T> items, Func<T, string> selector, DutchFuzzyOptions? options = null);
    public int Count { get; }
    public IReadOnlyList<FuzzyMatch<T>> Search(string query, double threshold = 0.82, int take = 10);
    public void Add(T item);
}
```

Blocking-strategie: bucket op (a) de volledige fonetische sleutel en (b) de eerste 4 tekens
van de sleutel. Query haalt de union van beide buckets op en scoort enkel die.
Als de union leeg is, val terug op een volledige scan (documenteer dit, met een
`Search(..., bool allowFullScan = true)`-parameter indien nodig).

---

## 6. Algoritmespecificaties

### 6.1 `DutchTextNormalizer`

Stappen, in deze volgorde:

1. `null` of whitespace → lege string terug.
2. Trim, en `value.Normalize(NormalizationForm.FormD)`; verwijder alle tekens met
   `CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark`
   wanneer `RemoveDiacritics` true is. Daarna `Normalize(NormalizationForm.FormC)`.
   Let op de bekende valstrik: `ß` wordt door FormD niet ontleed — map die expliciet naar `ss`.
   Idem `æ` → `ae`, `œ` → `oe`, `ø` → `o`, `đ` → `d`.
3. Lowercase met `CultureInfo.InvariantCulture` (`ToLowerInvariant`).
4. Vervang het Nederlandse ligatuurteken `ĳ` (U+0133) en `Ĳ` (U+0132) door `ij`.
5. Vervang koppeltekens, apostroffen (`'`, `’`, `` ` ``), punten en komma's door een spatie.
   Behandel `'s-Hertogenbosch` → `s hertogenbosch`, `'t Zand` → `t zand`.
6. Verwijder alle overige tekens die geen `a`-`z` of spatie zijn.
7. Collapse meervoudige spaties naar één.
8. Wanneer `StripNamePrefixes` true is: herken een leidende tussenvoegselreeks
   (zie 6.2) en verplaats die naar het einde, gescheiden door een spatie.
   `van den broeke` → `broeke van den`. Zo matchen "Van den Broeke" en "Broeke, van den".
9. Wanneer `SplitGluedNamePrefixes` true is: pas de gluesplitsing toe (6.2).

Volledige referentie-implementatie van de kern:

```csharp
using System;
using System.Globalization;
using System.Text;

namespace NlFuzzy.Normalization;

public sealed class DutchTextNormalizer : IDutchTextNormalizer
{
    public static DutchTextNormalizer Instance { get; } = new();

    public string Normalize(string value, DutchFuzzyOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var expanded = ExpandSpecialCharacters(value);
        var folded = options.RemoveDiacritics ? RemoveDiacritics(expanded) : expanded;
        var lowered = folded.ToLowerInvariant();
        var cleaned = KeepLettersAndSpaces(lowered);

        if (cleaned.Length == 0)
        {
            return string.Empty;
        }

        if (options.SplitGluedNamePrefixes)
        {
            cleaned = DutchAffixes.SplitGluedPrefixes(cleaned);
        }

        if (options.StripNamePrefixes)
        {
            cleaned = DutchAffixes.MovePrefixesToEnd(cleaned);
        }

        return cleaned;
    }

    private static string ExpandSpecialCharacters(string value)
    {
        var builder = new StringBuilder(value.Length + 4);

        foreach (var c in value)
        {
            switch (c)
            {
                case 'ß':
                    builder.Append("ss");
                    break;
                case 'æ':
                case 'Æ':
                    builder.Append("ae");
                    break;
                case 'œ':
                case 'Œ':
                    builder.Append("oe");
                    break;
                case 'ø':
                case 'Ø':
                    builder.Append('o');
                    break;
                case 'đ':
                case 'Đ':
                    builder.Append('d');
                    break;
                case 'ĳ':
                case 'Ĳ':
                    builder.Append("ij");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    private static string RemoveDiacritics(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string KeepLettersAndSpaces(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasSpace = true;

        foreach (var c in value)
        {
            if (c >= 'a' && c <= 'z')
            {
                builder.Append(c);
                lastWasSpace = false;
                continue;
            }

            if (!lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }
}
```

### 6.2 `DutchAffixes`

Tussenvoegsels die als leidende reeks herkend worden (langste match eerst, maximaal
drie tokens):

```
van der, van den, van de, van het, van t, van, ver, vander, vanden, vande,
de, den, der, des, del, ten, ter, te, het, t, s,
op de, op den, op het, op t, op,
in de, in den, in het, in t, in,
uit de, uit den, uit het, uit,
aan de, aan den, aan het,
bij de, bij den,
onder de, onder den,
over de, over den,
tot, toe, la, le, du, di, da, dos, das, el, al, ibn, bin, mac, mc, o
```

Let op: `ver` en `vander` alleen strippen wanneer `SplitGluedNamePrefixes` aan staat.
`s` en `t` komen uit `'s-Hertogenbosch` en `'t Zand`.

Glue-splitsing (opt-in, `SplitGluedPrefixes`): wanneer een enkel token begint met
`vander`, `vanden`, `vande`, `van`, `opde`, `inde`, `terr`, `ver`, en de rest minstens
drie letters lang is, splits dan af. `verbrugghe` → `van der brugghe` is bewust
agressief; documenteer dat dit recall verhoogt en precisie verlaagt.

### 6.3 `DutchPhoneticEncoder` — de kern

Werkwijze: table-driven longest-match-first herschrijving over de genormaliseerde string,
per token, met sentinelmarkers voor woordbegin en woordeinde.

**Fonemenalfabet in de output** (één teken per foneem):

| Teken | Klank | Voorbeeld |
|---|---|---|
| `P` | /p/ | pot, heb (finaal) |
| `B` | /b/ | bad (alleen in Strict) |
| `T` | /t/ | tak, hond (finaal), th |
| `D` | /d/ | dak (alleen in Strict) |
| `K` | /k/ | kat, cactus, quiz |
| `G` | /x/ of /ɣ/ | goed, lachen, nacht |
| `F` | /f/ | fiets, foto, lief (finaal v) |
| `V` | /v/ | vader (alleen in Strict) |
| `W` | /ʋ/ | water (alleen in Strict) |
| `S` | /s/ | sok, cent, huis (finaal z) |
| `Z` | /z/ | zon (alleen in Strict) |
| `X` | /ʃ/ | sjaal, chef, shampoo |
| `J` | /j/ | jaar, yoghurt |
| `L` `M` `N` `R` `H` | idem | — |
| `Q` | /ŋ/ | zingen (ng) |
| `1`..`7` | klinkerklassen, alleen bij `EncodeVowels` | zie tabel hieronder |

**Klinkerklassen** (alleen relevant wanneer `EncodeVowels` true is, anders vallen alle
klinkers behalve de eerste weg — de Metaphone-aanpak):

| Klasse | Spellingen | Motivatie |
|---|---|---|
| `1` | a, aa, ah | — |
| `2` | e, ee, eh, é, è | — |
| `3` | i, ie, ieh, y (tussen medeklinkers, niet als /ɛi/) | — |
| `4` | o, oo, oh | — |
| `5` | u, uu, uh | — |
| `6` | ij, ei, y, ij (allemaal /ɛi/) | ei en ij zijn homofoon; zie 2.4 |
| `7` | ou, au, ouw, auw, ui, uy, oe, eu | diftongen en lange klinkerdigrafen samengevoegd; splits later op indien de gouden dataset dat vraagt |

**Regeltabel.** Volgorde is normatief: pas de eerste regel toe die matcht op de huidige
positie, en spring vervolgens `Length` tekens verder. `^` = woordbegin, `$` = woordeinde.

| # | Patroon | Context | Uitvoer | Bron / motivatie |
|---|---|---|---|---|
| 1 | `sch` | `^` | `SG` | schoen, Scheldt |
| 2 | `sch` | `$` | `S` | historische spelling: visch → vis, Bosch → Bos |
| 3 | `sch` | overig | `SG` | misschien |
| 4 | `tsj` | — | `X` | tsjilpen |
| 5 | `sj` | — | `X` | sjaal; Krols: s voor h/j → x |
| 6 | `sh` | — | `X` | shampoo |
| 7 | `cht` | — | `GT` | nacht, gracht |
| 8 | `ch` | `^` | `X` | chef, china (Franse leenwoorden). Zie noot A. |
| 9 | `ch` | overig | `G` | lachen; Krols: ch intern → g |
| 10 | `ck` | — | `K` | Dircks |
| 11 | `cq` | — | `K` | Cocquyt |
| 12 | `qu` | — | `KW` | quiz. In Tolerant: `KV` |
| 13 | `q` | — | `K` | — |
| 14 | `x` | — | `KS` | Krols |
| 15 | `cks` | — | `KS` | Dircks/Dirx |
| 16 | `ks` | — | `KS` | Luycks/Luiks |
| 17 | `ph` | — | `F` | Krols |
| 18 | `th` | — | `T` | Thijs; th in inheemse woorden = /t/ |
| 19 | `dt` | `$` | `T` | wordt, Brandt |
| 20 | `tie` | `$` | `SI` of `S` | actie, natie. `SI` wanneer `EncodeVowels`. |
| 21 | `ti` | gevolgd door klinker | `S` | Krols: tia/tio/tie/tiu → s |
| 22 | `ij` | — | klinkerklasse `6` | zie 2.4 |
| 23 | `ei` | — | klinkerklasse `6` | zie 2.4 |
| 24 | `ui`, `uy` | — | klinkerklasse `7` | huis/huys |
| 25 | `ou`, `ouw`, `au`, `auw` | — | klinkerklasse `7` | koud/kauw |
| 26 | `oe` | — | klinkerklasse `7` | boek |
| 27 | `eu` | — | klinkerklasse `7` | neus |
| 28 | `ie` | — | klinkerklasse `3` | fiets |
| 29 | `aa`,`ee`,`oo`,`uu` | — | klinkerklasse `1`,`2`,`4`,`5` | lange klinkers |
| 30 | `y` | gevolgd door klinker | `J` | yoghurt; Krols |
| 31 | `y` | overig | klinkerklasse `6` | Bruyn = Bruijn |
| 32 | `ij` | — | zie 22 | — |
| 33 | `ng` | niet gevolgd door klinker | `Q` | zingen. Zie noot B. |
| 34 | `nk` | — | `QK` | bank |
| 35 | `c` | gevolgd door `e`,`i`,`y`,`ij` | `S` | cent; Krols |
| 36 | `c` | overig | `K` | cactus; Krols |
| 37 | `g`,`gh` | — | `G` | Verbrugghe/Verbrugge |
| 38 | `b` | `$` | `P` | eind-verstemlozing; Krols |
| 39 | `b` | overig | `B` (Strict) / `P` (Tolerant) | — |
| 40 | `d` | `$` of gevolgd door `$t`/`$s` | `T` | hond; Krols |
| 41 | `d` | overig | `D` (Strict) / `T` (Tolerant) | — |
| 42 | `v` | `$` | `F` | — |
| 43 | `v` | overig | `V` (Strict) / `F` (Tolerant) | — |
| 44 | `w` | gevolgd door `r` | `V` | wrede/vrede; Krols |
| 45 | `w` | overig | `W` (Strict) / `F` (Tolerant) | v/w-verwarring is frequent in NL/VL namen |
| 46 | `z` | `$` | `S` | — |
| 47 | `z` | overig | `Z` (Strict) / `S` (Tolerant) | huis/huiz-, Zeeuws/Seeuws |
| 48 | `h` | `^` en gevolgd door klinker | `H` | Strict houdt `H`, Tolerant laat vallen |
| 49 | `h` | overig | drop | Krols laat h vallen na c/s/p/t/g |
| 50 | `j` | voorafgegaan door `t` | `X` | tjokvol; Krols |
| 51 | `j` | overig | `J` | — |
| 52 | `f`,`k`,`l`,`m`,`n`,`p`,`r`,`s`,`t` | — | hoofdletter idem | — |
| 53 | overige klinkers `a`,`e`,`i`,`o`,`u` | — | klinkerklasse of drop | zie postprocessing |

Noot A: regel 8 is een heuristiek. `ch` aan het begin is in inheemse Nederlandse woorden
zeldzaam; wanneer het voorkomt komt het meestal uit het Frans (`chef`, `chocolade`) of
uit het Grieks (`chaos`, `chirurg` → eerder /x/ of /k/). Krols kiest voor `x` aan het
woordbegin. Behoud die keuze in v1, maar dek `chaos` en `chirurg` af in de gouden dataset
en herevalueer als ze falen.

Noot B: `Q` als teken voor /ŋ/ botst niet met regel 13, want `q` in de invoer wordt daar
al naar `K` herschreven vóór de output-fase.

**Postprocessing** (in deze volgorde):

1. Collapse identieke opeenvolgende fonemen: `PP` → `P`. Uitzondering: doe dit ná stap 2
   zodat `KS` uit `x` niet per ongeluk gemangeld wordt.
2. Wanneer `EncodeVowels` false is: behoud enkel de klinkerklasse op positie 0 van elk token
   (Metaphone-gedrag: alleen de beginklinker telt) en verwijder alle overige klinkerklassen.
3. Wanneer `EncodeVowels` false is: normaliseer de behouden beginklinkerklasse naar één
   generiek teken `A`, zodat "Overijssel" en "Overrijssel" identiek coderen.
4. Wanneer `MaxKeyLength > 0`: kap af.
5. Bij een frase: voeg de tokencodes samen met een spatie. `Encode` geeft de gejoinde string,
   `EncodeTokens` de lijst.

**Skelet van de encoder** (de agent vult `DutchPhoneticRules.Rules` in volgens de tabel):

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace NlFuzzy.Phonetics;

public sealed class DutchPhoneticEncoder : IPhoneticEncoder
{
    private readonly DutchFuzzyOptions _options;
    private readonly Normalization.IDutchTextNormalizer _normalizer;

    public static DutchPhoneticEncoder Default { get; } = new(DutchFuzzyOptions.Default);

    public DutchPhoneticEncoder(DutchFuzzyOptions options,
                                Normalization.IDutchTextNormalizer? normalizer = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _normalizer = normalizer ?? Normalization.DutchTextNormalizer.Instance;
    }

    public string Encode(string value)
    {
        var tokens = EncodeTokens(value);
        return tokens.Count == 0 ? string.Empty : string.Join(" ", tokens);
    }

    public IReadOnlyList<string> EncodeTokens(string value)
    {
        var normalized = _normalizer.Normalize(value, _options);

        if (normalized.Length == 0)
        {
            return Array.Empty<string>();
        }

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            var encoded = EncodeToken(part);

            if (encoded.Length > 0)
            {
                results.Add(encoded);
            }
        }

        return results;
    }

    private string EncodeToken(string token)
    {
        var builder = new StringBuilder(token.Length + 4);
        var index = 0;

        while (index < token.Length)
        {
            var rule = DutchPhoneticRules.FindMatch(token, index, _options.Profile);

            if (rule is null)
            {
                index++;
                continue;
            }

            builder.Append(rule.Output);
            index += rule.Length;
        }

        return PostProcess(builder.ToString());
    }

    private string PostProcess(string raw)
    {
        var collapsed = CollapseRepeats(raw);
        var vowelHandled = _options.EncodeVowels ? collapsed : DropNonInitialVowels(collapsed);

        if (_options.MaxKeyLength > 0 && vowelHandled.Length > _options.MaxKeyLength)
        {
            return vowelHandled.Substring(0, _options.MaxKeyLength);
        }

        return vowelHandled;
    }

    private static string CollapseRepeats(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        builder.Append(value[0]);

        for (var i = 1; i < value.Length; i++)
        {
            if (value[i] != value[i - 1])
            {
                builder.Append(value[i]);
            }
        }

        return builder.ToString();
    }

    private static string DropNonInitialVowels(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var first = value[0];

        builder.Append(IsVowelClass(first) ? 'A' : first);

        for (var i = 1; i < value.Length; i++)
        {
            if (!IsVowelClass(value[i]))
            {
                builder.Append(value[i]);
            }
        }

        return builder.ToString();
    }

    private static bool IsVowelClass(char c) => c >= '1' && c <= '7';
}
```

En de regelstructuur:

```csharp
namespace NlFuzzy.Phonetics;

internal sealed record PhoneticRule(string Pattern, string Output, int Length)
{
    public static PhoneticRule Create(string pattern, string output) =>
        new(pattern, output, pattern.Length);
}

internal static class DutchPhoneticRules
{
    /// <summary>
    /// Returns the first rule that matches at <paramref name="index"/>, honouring
    /// longest-match-first ordering and positional context. Returns null when no rule applies,
    /// in which case the caller skips one character.
    /// </summary>
    internal static PhoneticRule? FindMatch(string token, int index, DutchPhoneticProfile profile);
}
```

Implementatiehint voor `FindMatch`: bouw een statische `Dictionary<char, PhoneticRule[]>`
met als sleutel het eerste teken en als waarde de regels gesorteerd op aflopende
patroonlengte. Contextcondities (`^`, `$`, "gevolgd door klinker") als predicaat
`Func<string, int, bool>` in de rule record. Houd de tabel op één plaats, in
`DutchPhoneticRules`, zodat de coverage-test (7.3) hem kan reflecteren.

### 6.4 `ColognePhoneticEncoder`

Implementeer de Kölner Phonetik als tweede encoder. Codeer elke letter naar een cijfer 0-8,
collapse herhalingen, verwijder alle nullen behalve een eventuele leidende nul.
De letters `w` en `v` krijgen beide cijfer 3, en de code voor "Wikipedia" is "3412" — gebruik
dat als eerste unittest.

Codeertabel:

| Letter | Context | Code |
|---|---|---|
| a, e, i, j, o, u, y | — | 0 |
| h | — | geen code |
| b | — | 1 |
| p | niet voor `h` | 1 |
| d, t | niet voor `c`,`s`,`z` | 2 |
| f, v, w | — | 3 |
| p | voor `h` | 3 |
| g, k, q | — | 4 |
| c | aan woordbegin, voor `a,h,k,l,o,q,r,u,x` | 4 |
| c | niet na `s`,`z`, voor `a,h,k,o,q,u,x` | 4 |
| x | niet na `c`,`k`,`q` | 48 |
| l | — | 5 |
| m, n | — | 6 |
| r | — | 7 |
| s, z | — | 8 |
| c | na `s`,`z` | 8 |
| c | aan woordbegin, niet voor `a,h,k,l,o,q,r,u,x` | 8 |
| c | niet voor `a,h,k,o,q,u,x` | 8 |
| d, t | voor `c`,`s`,`z` | 8 |
| x | na `c`,`k`,`q` | 8 |

Verifieer de implementatie tegen de tabel op de Wikipedia-pagina en tegen de testvectoren
van Apache Commons Codec `ColognePhonetic`. Documenteer duidelijk dat dit een Duitse
codering is die *ook* redelijk werkt voor het Nederlands, en niet de aanbevolen default.

### 6.5 Afstands- en gelijkenisalgoritmes

Implementeer alle vier zelf, met `ReadOnlySpan<char>`-overloads op de moderne TFM's.

```csharp
using System;

namespace NlFuzzy.Distance;

/// <summary>Wagner-Fischer Levenshtein distance using two rolling rows.</summary>
public sealed class LevenshteinDistance : IStringDistance
{
    public static LevenshteinDistance Instance { get; } = new();

    public double Distance(string a, string b) => Compute(a, b);

    public static int Compute(string a, string b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        if (string.Equals(a, b, StringComparison.Ordinal)) return 0;

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                var deletion = previous[j] + 1;
                var insertion = current[j - 1] + 1;
                var substitution = previous[j - 1] + cost;

                current[j] = Math.Min(Math.Min(deletion, insertion), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    /// <summary>Levenshtein distance scaled to [0, 1] by the length of the longest input.</summary>
    public static double Normalized(string a, string b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var longest = Math.Max(a.Length, b.Length);

        return longest == 0 ? 0d : Compute(a, b) / (double)longest;
    }
}
```

```csharp
using System;

namespace NlFuzzy.Distance;

/// <summary>Jaro-Winkler similarity in [0, 1].</summary>
public sealed class JaroWinklerSimilarity : IStringSimilarity
{
    private const double DefaultScalingFactor = 0.1;
    private const int MaxPrefixLength = 4;

    public static JaroWinklerSimilarity Instance { get; } = new();

    private readonly double _scalingFactor;

    public JaroWinklerSimilarity(double scalingFactor = DefaultScalingFactor)
    {
        if (scalingFactor < 0 || scalingFactor > 0.25)
        {
            throw new ArgumentOutOfRangeException(nameof(scalingFactor),
                "Scaling factor must be between 0 and 0.25.");
        }

        _scalingFactor = scalingFactor;
    }

    public double Similarity(string a, string b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        if (a.Length == 0 && b.Length == 0) return 1d;
        if (a.Length == 0 || b.Length == 0) return 0d;
        if (string.Equals(a, b, StringComparison.Ordinal)) return 1d;

        var jaro = Jaro(a, b);

        if (jaro <= 0d)
        {
            return 0d;
        }

        var prefix = 0;
        var limit = Math.Min(MaxPrefixLength, Math.Min(a.Length, b.Length));

        while (prefix < limit && a[prefix] == b[prefix])
        {
            prefix++;
        }

        return jaro + prefix * _scalingFactor * (1d - jaro);
    }

    private static double Jaro(string a, string b)
    {
        var matchWindow = Math.Max(a.Length, b.Length) / 2 - 1;

        if (matchWindow < 0)
        {
            matchWindow = 0;
        }

        var aMatched = new bool[a.Length];
        var bMatched = new bool[b.Length];
        var matches = 0;

        for (var i = 0; i < a.Length; i++)
        {
            var start = Math.Max(0, i - matchWindow);
            var end = Math.Min(i + matchWindow + 1, b.Length);

            for (var j = start; j < end; j++)
            {
                if (bMatched[j] || a[i] != b[j])
                {
                    continue;
                }

                aMatched[i] = true;
                bMatched[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
        {
            return 0d;
        }

        var transpositions = 0;
        var k = 0;

        for (var i = 0; i < a.Length; i++)
        {
            if (!aMatched[i])
            {
                continue;
            }

            while (!bMatched[k])
            {
                k++;
            }

            if (a[i] != b[k])
            {
                transpositions++;
            }

            k++;
        }

        var half = transpositions / 2d;

        return (matches / (double)a.Length
                + matches / (double)b.Length
                + (matches - half) / matches) / 3d;
    }
}
```

```csharp
using System;
using System.Collections.Generic;

namespace NlFuzzy.Distance;

/// <summary>Sorensen-Dice coefficient over character bigrams.</summary>
public sealed class DiceCoefficientSimilarity : IStringSimilarity
{
    public static DiceCoefficientSimilarity Instance { get; } = new();

    public double Similarity(string a, string b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        if (a.Length == 0 && b.Length == 0) return 1d;
        if (a.Length < 2 || b.Length < 2)
        {
            return string.Equals(a, b, StringComparison.Ordinal) ? 1d : 0d;
        }

        var first = BuildBigrams(a);
        var second = BuildBigrams(b);
        var intersection = 0;

        foreach (var pair in first)
        {
            if (second.TryGetValue(pair.Key, out var count))
            {
                intersection += Math.Min(pair.Value, count);
            }
        }

        var total = a.Length - 1 + b.Length - 1;

        return total == 0 ? 0d : 2d * intersection / total;
    }

    private static Dictionary<string, int> BuildBigrams(string value)
    {
        var result = new Dictionary<string, int>(value.Length, StringComparer.Ordinal);

        for (var i = 0; i < value.Length - 1; i++)
        {
            var bigram = value.Substring(i, 2);
            result[bigram] = result.TryGetValue(bigram, out var count) ? count + 1 : 1;
        }

        return result;
    }
}
```

`TokenSetSimilarity`: split beide strings op spaties, sorteer de tokens alfabetisch,
join opnieuw, en pas dan Jaro-Winkler toe. Dit lost "Broeke van den" versus
"van den Broeke" op wanneer `StripNamePrefixes` uit staat.

`DamerauLevenshteinDistance`: optimal string alignment variant (aangrenzende transposities),
nodig voor typfouten als "Kuipres" / "Kuipers".

### 6.6 `DutchFuzzyScorer`

```csharp
using System;

namespace NlFuzzy.Comparison;

public sealed class DutchFuzzyScorer
{
    private readonly DutchFuzzyOptions _options;
    private readonly Phonetics.IPhoneticEncoder _encoder;
    private readonly Normalization.IDutchTextNormalizer _normalizer;

    public static DutchFuzzyScorer Default { get; } = new(DutchFuzzyOptions.Default);

    public DutchFuzzyScorer(DutchFuzzyOptions options,
                            Phonetics.IPhoneticEncoder? encoder = null,
                            Normalization.IDutchTextNormalizer? normalizer = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _normalizer = normalizer ?? Normalization.DutchTextNormalizer.Instance;
        _encoder = encoder ?? new Phonetics.DutchPhoneticEncoder(_options, _normalizer);
    }

    public double Score(string a, string b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var normalizedA = _normalizer.Normalize(a, _options);
        var normalizedB = _normalizer.Normalize(b, _options);

        if (normalizedA.Length == 0 && normalizedB.Length == 0)
        {
            return 1d;
        }

        if (normalizedA.Length == 0 || normalizedB.Length == 0)
        {
            return 0d;
        }

        if (string.Equals(normalizedA, normalizedB, StringComparison.Ordinal))
        {
            return 1d;
        }

        var keyA = _encoder.Encode(a);
        var keyB = _encoder.Encode(b);

        var orthographic = Distance.JaroWinklerSimilarity.Instance.Similarity(normalizedA, normalizedB);
        var phonetic = 1d - Distance.LevenshteinDistance.Normalized(keyA, keyB);
        var ngram = Distance.DiceCoefficientSimilarity.Instance.Similarity(normalizedA, normalizedB);

        var totalWeight = _options.OrthographicWeight + _options.PhoneticWeight + _options.NGramWeight;

        var score = (orthographic * _options.OrthographicWeight
                     + phonetic * _options.PhoneticWeight
                     + ngram * _options.NGramWeight) / totalWeight;

        if (keyA.Length > 0 && string.Equals(keyA, keyB, StringComparison.Ordinal))
        {
            score = Math.Max(score, _options.IdenticalKeyFloor);
        }

        return Math.Clamp(score, 0d, 1d);
    }
}
```

Let op: `Math.Clamp` bestaat niet in `netstandard2.0`. Voeg een interne polyfill toe
onder `#if NETSTANDARD2_0`, of gebruik `Math.Min(Math.Max(...))` overal.

---

## 7. Testplan

### 7.1 Testframework

xUnit + `FluentAssertions` (of gewone asserts, als je ook in tests dependency-arm wil blijven).
Coverage via `coverlet.collector`. Doel: minstens 90% line coverage op `src/NlFuzzy`.

### 7.2 Gouden dataset

Twee CSV-bestanden, ingelezen via `[Theory]` + `MemberData`, met kolommen
`left,right,comment`.

`positive-pairs.csv` — moeten dezelfde fonetische sleutel opleveren, of score >= 0.85:

```
Bruijn,Bruyn,ij/y
Bruijn,Bruin,ij/ui varianten in familienamen
Meijer,Meyer,ij/y
Meijer,Meier,ij/ei homofoon
Kuypers,Kuipers,uy/ui
Luycks,Luiks,uy/ui + cks/ks
Sijmons,Symons,ij/y
Sijmons,Simons,ij/i
Gijsbrecht,Gijsbregt,cht/gt
Verbrugghe,Verbrugge,gh/g
Vandenbroecke,Van den Broecke,geplakt versus los
Van den Broecke,Van den Broeke,ck/k
De Smet,Desmet,spatie
De Smet,De Smedt,dt finaal = t
Claes,Claas,aa/ae
Claes,Klaas,c/k
Coppens,Koppens,c/k
Maes,Maas,ae/aa
Peeters,Peters,ee/e
Janssen,Jansen,dubbele s
Wouters,Woutters,dubbele t
Rijkaard,Reijkaard,ij/ei
Dirks,Dirx,ks/x
Thijs,Tijs,th/t
Overeisel,Overrijssel,ei/ij + dubbele r
Sint-Niklaas,Sint Niklaas,koppelteken
s-Hertogenbosch,'s-Hertogenbosch,apostrof
Antwerpen,Antwerpn,ontbrekende klinker
vrede,wrede,w voor r = v
actie,aktie,tie/ktie
cadeau,kadoo,c/k + eau/oo
sigaret,cigaret,c/s
foto,photo,ph/f
school,skool,sch/sk
chocolade,sjokolade,ch/sj aan woordbegin
```

`negative-pairs.csv` — mogen géén identieke sleutel hebben en moeten score < 0.75 geven:

```
Peeters,Willems,volledig verschillend
Vermeulen,Verhoeven,zelfde prefix andere stam
Dijkstra,Dijkhuizen,zelfde prefix andere stam
Bakker,Bakkum,verschillende stam
Jansen,Hansen,initiaal verschilt
Maes,Moes,klinkerklasse verschilt
Bos,Bosch,zie noot
Kuipers,Kuiper,enkelvoud/meervoud
fiets,fles,verschillend woord
gracht,grot,verschillend woord
```

Noot bij `Bos,Bosch`: regel 2 zegt dat `-sch` finaal als `S` codeert, waardoor Bos en Bosch
identiek coderen. Dat is historisch-fonetisch correct (Bosch spreek je als "Bos"), maar het
zijn wel verschillende familienamen. Beslissing: laat ze matchen op sleutelniveau en
documenteer dit als bewuste keuze; verwijder dit paar uit de negatieve set en zet het in
`positive-pairs.csv` met commentaar. De agent moet hier expliciet een keuze maken en die
in de README documenteren.

### 7.3 Structurele tests

- `DutchPhoneticRuleCoverageTests`: itereert over `DutchPhoneticRules` en faalt wanneer een
  regel geen enkele testcase heeft die hem aanraakt. Implementeer via een teller die de
  encoder in debug-modus vult, of via een expliciete lijst van `[InlineData]` per regel.
- Property-based sanity checks (bijvoorbeeld met FsCheck, optioneel):
  - `Similarity(a, a) == 1.0` voor elke niet-lege a
  - `Similarity(a, b) == Similarity(b, a)` (symmetrie)
  - `Similarity(a, b)` ligt altijd in [0, 1]
  - `PhoneticKey(a)` is deterministisch en bevat geen spaties binnen één token
- Null- en leeg-invoer tests voor elke publieke methode.
- Thread-safety test: 1000 parallelle `Encode`-aanroepen op dezelfde encoderinstantie
  leveren identieke resultaten.

### 7.4 Benchmarks

BenchmarkDotNet, minimaal: `Encode` op een korte naam, `Score` op twee korte namen,
`DutchFuzzyIndex.Search` op 100.000 items. Richtwaarde om na te streven en te documenteren:
`Encode` op een naam van 10 tekens onder de 2 microseconden, zonder allocaties in de hot path
buiten de resultaatstring. Meet dit, claim het niet vooraf.

---

## 8. Packaging en CI

### 8.1 `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

### 8.2 `src/NlFuzzy/NlFuzzy.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IsPackable>true</IsPackable>
    <PackageId>NlFuzzy</PackageId>
    <Title>NlFuzzy - Dutch phonetic and fuzzy string matching</Title>
    <Description>Sounds-like and fuzzy string matching tuned for the Dutch language, with a Dutch phonetic encoder, Cologne phonetics, Levenshtein, Damerau-Levenshtein, Jaro-Winkler and Dice similarity, plus simple extension methods. No runtime dependencies.</Description>
    <PackageTags>dutch;nederlands;fuzzy;phonetic;soundex;metaphone;soundslike;string-matching;fonetisch</PackageTags>
    <Authors>REPLACE_ME</Authors>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageProjectUrl>https://github.com/REPLACE_ME/NlFuzzy</PackageProjectUrl>
    <RepositoryUrl>https://github.com/REPLACE_ME/NlFuzzy</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="System.Memory" Version="4.6.0" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="NlFuzzy.Tests" />
  </ItemGroup>

</Project>
```

Noot: `Microsoft.SourceLink.GitHub` is `PrivateAssets="All"` en verschijnt dus niet als
runtime-dependency. `System.Memory` op netstandard2.0 is de enige echte dependency en enkel
nodig als je `Span<char>` gebruikt op dat target — laat hem weg als je dat niet doet.
Controleer de exacte, actuele versienummers zelf; ze kunnen inmiddels hoger zijn.

### 8.3 CI workflow

`.github/workflows/ci.yml`:

```yaml
name: ci

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            10.0.x

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"

      - name: Pack
        run: dotnet pack src/NlFuzzy/NlFuzzy.csproj --no-build --configuration Release --output ./artifacts

      - uses: actions/upload-artifact@v4
        with:
          name: nupkg
          path: ./artifacts/*.nupkg
```

`.github/workflows/release.yml`: getriggerd op tags `v*`, doet hetzelfde plus
`dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json`.
Gebruik MinVer of Nerdbank.GitVersioning voor versienummering op basis van tags, of zet
`<Version>` gewoon handmatig in de csproj. Kies één aanpak en documenteer die.

### 8.4 README-inhoud (verplicht)

1. Wat het is, in twee zinnen.
2. Installatie.
3. Vijf voorbeelden met extension methods.
4. Tabel met de gedekte Nederlandse spellingvarianten (ij/ei/y, ui/uy, ou/au, ks/x,
   c/k/s, gh/g, cht/gt, dt/t finaal, sch/s).
5. Uitleg profielen Strict versus Tolerant met concrete score-voorbeelden.
6. Sectie "Wanneer dit niet werkt": dialecten, niet-Nederlandse namen, korte strings
   van minder dan 4 tekens.
7. Sectie over persisteren van sleutels: bij een nieuwe major versie kunnen sleutels
   wijzigen; adviseer een `PhoneticKeyVersion`-kolom naast de sleutelkolom.
8. Bronvermelding en licentie.

Belangrijk versiebeleid: **een wijziging in de fonetische regeltabel die bestaande sleutels
verandert, is een breaking change.** Documenteer dit en houd je eraan in de CHANGELOG.

---

## 9. Werkfases

### Fase 1 — Skelet
- [ ] Solution, projecten, `Directory.Build.props`, `.editorconfig`, LICENSE (MIT), lege README.
- [ ] CI-workflow die bouwt en test.
- [ ] `DutchFuzzyOptions`, `DutchPhoneticProfile`, `FuzzyMatch<T>`, alle interfaces.
- Klaar wanneer: `dotnet build` en `dotnet test` groen zijn met één triviale test.

### Fase 2 — Normalisatie
- [ ] `DutchTextNormalizer` volledig, inclusief `ß`, `ĳ`, `'s-`, `'t `.
- [ ] `DutchAffixes` met tussenvoegsellijst en `MovePrefixesToEnd`.
- [ ] `SplitGluedPrefixes` achter de opt-in optie.
- [ ] Unittests: minstens 30 cases, waaronder alle voorbeelden uit 6.1 en 6.2.
- Klaar wanneer: alle normalisatietests groen.

### Fase 3 — Afstandsalgoritmes
- [ ] Levenshtein, Damerau-Levenshtein (OSA), Jaro-Winkler, Dice, TokenSet.
- [ ] Tests met bekende referentiewaarden uit de literatuur
      (bijvoorbeeld Jaro-Winkler MARTHA/MARHTA ≈ 0.961, DWAYNE/DUANE ≈ 0.840 —
      verifieer deze waarden zelf tegen een tweede implementatie voordat je ze vastlegt).
- Klaar wanneer: alle afstandstests groen en de symmetrie-properties gelden.

### Fase 4 — Fonetische encoder
- [ ] `DutchPhoneticRules` met de volledige regeltabel uit 6.3.
- [ ] `DutchPhoneticEncoder` met `FindMatch`, postprocessing, beide profielen.
- [ ] `ColognePhoneticEncoder` met verificatie "Wikipedia" → "3412".
- [ ] Tests per regel plus de Krols-voorbeelden (vrede/wrede, luycks/luiks,
      Vandenbroecke/Vandenberghe, baobab, china, shampoo, sjamaan, tjokvol).
- Klaar wanneer: elke regel minstens één test heeft en de coverage-test groen is.

### Fase 5 — Scoring en publieke API
- [ ] `DutchFuzzyScorer`, `DutchFuzzy` facade, beide extensieklassen,
      `DutchPhoneticEqualityComparer`.
- [ ] Gouden dataset ingeladen en groen. Stem gewichten en drempels af tot de dataset
      volledig slaagt — pas de gewichten aan, niet de dataset.
- Klaar wanneer: `positive-pairs.csv` en `negative-pairs.csv` beide volledig groen zijn.

### Fase 6 — Index en performantie
- [ ] `DutchFuzzyIndex<T>` met bucketing.
- [ ] Benchmarks, resultaten in de README.
- [ ] Allocaties terugdringen waar de benchmark dat rechtvaardigt.
- Klaar wanneer: search op 100.000 items onder de 50 ms per query blijft (meet en pas aan).

### Fase 7 — Publicatie
- [ ] README volledig, CHANGELOG, packagemetadata.
- [ ] `dotnet pack` en lokale installatie in een testproject valideren.
- [ ] Naam op nuget.org controleren op conflicten voordat je publiceert.
- [ ] Tag `v1.0.0` en release workflow.

---

## 10. Acceptatiecriteria

1. `dotnet build` slaagt zonder waarschuwingen op alle drie de TFM's.
2. Alle tests groen, inclusief de volledige gouden dataset.
3. Line coverage op `src/NlFuzzy` minstens 90%.
4. Het gegenereerde `.nupkg` heeft nul runtime-dependencies op `net8.0` en `net10.0`.
5. De publieke API komt exact overeen met hoofdstuk 5. Voeg een public-API-approval test toe
   (bijvoorbeeld met `PublicApiGenerator` + approved `.txt`-bestand) zodat onbedoelde
   API-wijzigingen een test breken.
6. Elk voorbeeld in de README compileert en klopt (schrijf ze als tests).
7. Geen enkele regel code afgeleid van GPL-gelicenseerde bronnen.

---

## 11. Bronnen

Fonetische algoritmes, algemeen:
- Soundex, achtergrond: https://en.wikipedia.org/wiki/Soundex
- Kölner Phonetik / Cologne phonetics: https://en.wikipedia.org/wiki/Cologne_phonetics
- Apache Commons Codec `ColognePhonetic`: https://commons.apache.org/codec/apidocs/org/apache/commons/codec/language/ColognePhonetic.html
- Apache Commons Codec `BeiderMorseEncoder`: https://commons.apache.org/codec/apidocs/org/apache/commons/codec/language/bm/BeiderMorseEncoder.html
- Beider-Morse Phonetic Matching, officiële pagina: https://stevemorse.org/phonetics/bmpm.htm

Nederlands specifiek:
- Diederik Krols, "Fuzzy lookup of names with a Dutch Metaphone implementation" (T-SQL-regelset, 2011): https://blogs.u2u.be/diederik/post/Fuzzy-lookup-of-Names-with-a-Dutch-Metaphone-implementation
- Jan Jongboom, "Diakritische tekens en Soundex in .NET" (2010): https://blog.janjongboom.com/algoritmes/2010/12/14/diakritische-tekens-en-soundex-in-net.html
- AMIS, "Ever wondered how Soundex works?" (met Nederlandse variant in Oracle): https://technology.amis.nl/oracle/ever-wondered-how-soundex-works/

Nederlandse fonologie:
- Taalportaal (INT/Meertens), diftong /ɛi/ en de spelling ei/ij: https://taalportaal.org/taalportaal/topic/pid/topic_20150611211611172
- Final-obstruent devoicing: https://en.wikipedia.org/wiki/Final-obstruent_devoicing

.NET-bibliotheken:
- Lucene.Net.Analysis.Phonetic: https://www.nuget.org/packages/Lucene.Net.Analysis.Phonetic/
- Lucene.NET Phonetic API-documentatie: https://lucenenet.apache.org/docs/4.8.0-beta00016/api/analysis-phonetic/Lucene.Net.Analysis.Phonetic.html
- F23.StringSimilarity: https://www.nuget.org/packages/F23.StringSimilarity
- StringSimilarity.NET broncode: https://github.com/feature23/StringSimilarity.NET
- FuzzyCompare: https://www.nuget.org/packages/FuzzyCompare

Onzekerheden die de agent moet verifiëren:
- Beschikbaarheid van de package-id `NlFuzzy` op nuget.org.
- Actuele versienummers van `Microsoft.SourceLink.GitHub` en `System.Memory`.
- De exacte Jaro-Winkler referentiewaarden in fase 3.
- Of `net10.0` in jullie build-omgeving beschikbaar is; zo niet, laat dat target vallen.
