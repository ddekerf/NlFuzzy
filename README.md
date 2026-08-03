# NlFuzzy

**NlFuzzy** is een .NET-bibliotheek die Nederlandse en Vlaamse namen en woorden vergelijkt op
*klank* in plaats van op spelling: "Bruijn", "Bruyn" en "Bruin" krijgen dezelfde fonetische sleutel.
Naast een Nederlandstalige fonetische encoder levert de bibliotheek een gewogen gelijkenisscore,
klassieke afstandsalgoritmes, een `IEqualityComparer<string>` en een geblokte index voor bulk lookups —
zonder enige runtime-dependency.

[![ci](https://github.com/ddekerf/NlFuzzy/actions/workflows/ci.yml/badge.svg)](https://github.com/ddekerf/NlFuzzy/actions/workflows/ci.yml)

---

## 1. Wat het is

NlFuzzy zet Nederlandse spelling om in een sounds-like sleutel ("Dutch Metaphone") en combineert die
met orthografische en n-gram gelijkenis tot één score tussen 0 en 1. Het is bedoeld voor
klantenbestanden, adresboeken, deduplicatie en zoekvelden waarin mensen namen fonetisch intypen.

## 2. Installatie

```bash
dotnet add package NlFuzzy
```

Doelframeworks: `netstandard2.0`, `net8.0` en `net10.0`. Nul runtime-dependencies.

## 3. Vijf voorbeelden

Alle voorbeelden hieronder staan als test in `tests/NlFuzzy.Tests/ReadmeExamplesTests.cs`.

### 3.1 Een fonetische sleutel maken

```csharp
using NlFuzzy;

string sleutel = "Bruijn".ToDutchPhoneticKey();   // "PRN"
bool gelijk = sleutel == "Bruyn".ToDutchPhoneticKey();   // true
```

### 3.2 Klinkt het hetzelfde?

```csharp
using NlFuzzy;

bool a = "Meijer".SoundsLikeDutch("Meier");    // true
bool b = "Philips".SoundsLikeDutch("Filips");  // true
bool c = "Bakker".SoundsLikeDutch("Dekker");   // false
```

### 3.3 Een score in plaats van een ja/nee

```csharp
using NlFuzzy;

double score = "Schoenmakers".DutchSimilarity("Schoonmakers");   // ~0.936
bool zelfde = "Meijer".DutchFuzzyEquals("Meier");                // true, drempel 0.82
```

### 3.4 Een lijst rangschikken

```csharp
using NlFuzzy;

var klanten = new[]
{
    new { Id = 1, Naam = "Marie Vandenbroecke" },
    new { Id = 2, Naam = "Piet Bakker" },
};

var treffers = klanten.FuzzyMatchDutch(
    "Marie van den Broeke",
    k => k.Naam,
    threshold: 0.80,
    take: 5,
    options: DutchFuzzyOptions.Names);

// treffers[0].Value.Id == 1
```

### 3.5 Een index voor herhaald zoeken

```csharp
using NlFuzzy;
using NlFuzzy.Indexing;

var index = new DutchFuzzyIndex<string>(
    ["Anna Schoenmakers", "Klaas Visser"],
    naam => naam,
    DutchFuzzyOptions.Names);

var resultaat = index.Search("Anna Schoenmaker", threshold: 0.70, take: 3);
// resultaat[0].Value == "Anna Schoenmakers"

// Sneller, maar approximatief: alleen de fonetische blokken.
var snel = index.Search("Anna Schoenmaker", 0.70, 3, DutchFuzzyIndexSearchMode.BlockedOnly);
```

`Search` staat standaard op `DutchFuzzyIndexSearchMode.Exact` en geeft dus exact dezelfde treffers
als `FuzzyMatchDutch` op dezelfde items en opties. Zie [§6](#6-wanneer-dit-niet-werkt) voor de
afweging tussen `Exact`, `Blocked` en `BlockedOnly`.

### 3.6 Drempels

Laat je `threshold` weg, dan gebruiken `DutchFuzzyEquals`, `FuzzyMatchDutch`, `BestDutchMatch` en
`DutchFuzzyIndex.Search` de `DefaultThreshold` van de meegegeven opties (0.82 voor de presets):

```csharp
var streng = DutchFuzzyOptions.Names with { DefaultThreshold = 0.90 };

bool zelfde = "Meijer".DutchFuzzyEquals("Meier", threshold: null, streng); // gebruikt 0.90
```

Een drempel moet een eindig getal in `[0, 1]` zijn; `NaN`, oneindig en waarden erbuiten leveren een
`ArgumentOutOfRangeException` op in plaats van een stil verkeerd resultaat.

### Bonus: ontdubbelen met de equality comparer

```csharp
using System.Collections.Generic;
using NlFuzzy.Comparison;

var uniek = new HashSet<string>(
    ["Meijer", "Meier", "Meyer", "Bakker"],
    DutchPhoneticEqualityComparer.Default);

// uniek.Count == 2
```

## 4. Gedekte Nederlandse spellingvarianten

De encoder laat de volgende paren op dezelfde sleutel uitkomen (profiel `Tolerant`, preset `Default`):

| Variatie | Voorbeeld | Sleutel |
|---|---|---|
| ij / ei / y | `Meijer` = `Meier` = `Meyer` | `MR` |
| ij / ui / uy | `Bruijn` = `Bruyn` = `Bruin` | `PRN` |
| uy / ui | `Kuypers` = `Kuipers` | `KPRS` |
| aa / a + uw | `Blaauw` = `Blauw` | `PL` |
| ae / aa | `Maes` = `Maas` | `MS` |
| ks / x / cks | `Dirx` = `Dircks` | `TRKS` |
| c / k | `Cornelis` = `Kornelis` | `KRNLS` |
| gh / g | `Verbrugghe` = `Verbrugge` | `FRPRG` |
| cht / gt | `Gijsbrecht` = `Gijsbregt` | `GSPRGT` |
| dt / t (finaal) | `Brandt` = `Brant` | `PRNT` |
| sch / s (finaal) | `Bosch` = `Bos` | `PS` |
| ph / f | `Philips` = `Filips` | `FLPS` |
| th / t | `Thijs` = `Tijs` | `TS` |
| x / ks | `Xander` = `Ksander` | `KSNTR` |
| qu / kw | `Quirijn` = `Kwirijn` | `KFRN` |
| c / s (voor e, i, y) | `Cypres` = `Sijpres` | `SPRS` |
| cks / ks | `Luycks` = `Luiks` | `LKS` |
| d / t (finale verstemlozing) | `Smit` = `Smid` | `SMT` |
| v / w | `vrede` = `wrede` | `FRT` |
| ou / au / auw / ouw | `kauw` = `kou` (klasse 7) | — |

Het uitvoeralfabet is `P B T D K G F V W S Z X J L M N R H Q` plus de klinkerklassen `1`-`7`
(alleen zichtbaar wanneer `EncodeVowels = true`) en `A` voor een woordinitiële klinker wanneer
`EncodeVowels = false`.

## 5. Profielen en presets

### Strict versus Tolerant

`Tolerant` (standaard) voegt stemhebbende en stemloze paren samen — b/p, d/t, v/f/w, z/s, g/ch.
`Strict` houdt ze uit elkaar. Concreet:

| Woord | `Strict` | `Tolerant` |
|---|---|---|
| `vrede` | `VRD` | `FRT` |
| `wrede` | `VRD` | `FRT` |
| `zee` | `Z` | `S` |
| `see` | `S` | `S` |
| `doren` | `DRN` | `TRN` |
| `toren` | `TRN` | `TRN` |
| `school` | `SGL` | `SGL` |
| `skool` | `SKL` | `SGL` |

Scores voor dezelfde paren:

| Paar | `Default` (Tolerant) | `Words` (Strict) |
|---|---|---|
| `school` / `skool` | 0.900 | 0.720 |
| `Vermeulen` / `Verhoeven` | 0.641 | 0.708 |

> `w` gevolgd door `r` klinkt in het Nederlands als `v`; daarom vallen `vrede` en `wrede` ook in
> `Strict` samen. Dat is opzet, geen bug.

### De drie presets

| Preset | Profiel | `EncodeVowels` | `StripNamePrefixes` | `SplitGluedNamePrefixes` | Waarvoor |
|---|---|---|---|---|---|
| `DutchFuzzyOptions.Default` | Tolerant | nee | ja | nee | algemeen, hoge recall |
| `DutchFuzzyOptions.Names` | Tolerant | ja | ja | ja | persoonsnamen, ook Vlaamse aaneengeschreven tussenvoegsels |
| `DutchFuzzyOptions.Words` | Strict | ja | nee | nee | gewone woorden in plaats van namen |

`Names` is de preset waartegen de gouden dataset gevalideerd is. Het verschil is groot bij
aaneengeschreven tussenvoegsels:

| Paar | `Default` | `Names` |
|---|---|---|
| `Vandenbroecke` / `Van den Broecke` | 0.268 | 1.000 |
| `Desmet` / `De Smet` | 0.451 | 1.000 |
| `Maes` / `Moes` | 0.900 | 0.664 |

Eigen instellingen maak je met een `with`-expressie:

```csharp
var opties = DutchFuzzyOptions.Names with
{
    MaxKeyLength = 6,
    PhoneticWeight = 0.60,
    OrthographicWeight = 0.30,
    NGramWeight = 0.10,
};

opties.Validate();
```

### Ankerstraffen op de eerste en laatste foneem

Naast de gewogen som past de scorer twee straffen toe wanneer de sleutels verschillen:
`InitialPhonemeMismatchPenalty` en `FinalPhonemeMismatchPenalty` (beide standaard 0.20).
Nederlandse namen die met een andere klank beginnen zijn zelden dezelfde naam, terwijl klassieke
afstandsmaten dat verschil nauwelijks opmerken:

| Paar | zonder ankerstraf | met ankerstraf (standaard) |
|---|---|---|
| `Jansen` / `Hansen` | 0.846 | 0.677 |
| `Kuipers` / `Kuiper` | 0.897 | 0.717 |

Zet ze op `0` om het klassieke gedrag terug te krijgen.

## 6. Wanneer dit niet werkt

- **Fonetische codering is per definitie lossy.** Je koopt recall met precisie. Gebruik de sleutel
  om kandidaten op te halen (blocking) en de score om te rangschikken — nooit de sleutel alleen om
  records automatisch samen te voegen.
- **Dialecten.** Nederland en Vlaanderen kennen veel dialecten; de encoder mikt op een
  regio-agnostische codering en is daardoor voor sommige dialecten te grof.
- **Niet-Nederlandse namen.** Turkse, Marokkaanse, Poolse of Aziatische namen volgen andere
  klank-spellingregels. De regeltabel is Nederlandstalig; voor meertalige bestanden is
  Beider-Morse een betere keuze.
- **Korte strings.** Bij minder dan vier tekens is er te weinig signaal: `Bos` en `Bus` liggen
  fonetisch dicht bij elkaar, en de score is dan nauwelijks informatief. Verhoog de drempel of
  vergelijk zulke velden exact.
- **Cijfers en leestekens** worden weggegooid; huisnummers en initialen dragen niets bij.
- **De zoekmodus van de index.** `DutchFuzzyIndex.Search` staat standaard op
  `DutchFuzzyIndexSearchMode.Exact`: elk item wordt gescoord (met hergebruikte, voorbewerkte
  sleutels), zodat het resultaat identiek is aan een lineaire scan. `Blocked` scoort eerst de
  fonetische blokken en valt terug op een volledige scan zodra die minder dan `take` treffers
  opleveren; `BlockedOnly` doet dat nooit. Beide blokmodi zijn dus **approximatief**: een item
  buiten de blokken kan gemist worden ook al haalt het de drempel. Kies bewust — recall (`Exact`)
  of een harde bovengrens op de querykost (`BlockedOnly`).
- **Zeer lange invoer.** De edit-distance-componenten zijn kwadratisch in de lengte. Begrens de
  invoer wanneer die van buiten komt; zie [§9](#9-prestaties).

## 7. Sleutels persisteren

Wil je fonetische sleutels in een database bewaren (bijvoorbeeld als index voor blocking), houd
dan rekening met het volgende:

> **Een wijziging in de fonetische regeltabel die bestaande sleutels verandert, is een breaking
> change.** Dat gebeurt daarom alleen in een major versie, en staat altijd in de CHANGELOG.

Advies: zet naast je sleutelkolom een `PhoneticKeyVersion`-kolom met de major versie van NlFuzzy
waarmee de sleutel berekend is.

```sql
ALTER TABLE Klant ADD PhoneticKey        NVARCHAR(64) NULL;
ALTER TABLE Klant ADD PhoneticKeyVersion INT          NOT NULL DEFAULT 1;
CREATE INDEX IX_Klant_PhoneticKey ON Klant (PhoneticKey);
```

Bij een upgrade naar een nieuwe major versie herbereken je alle rijen waarvan
`PhoneticKeyVersion` lager is dan de huidige. Zolang dat niet gebeurd is, kun je de oude en nieuwe
sleutels naast elkaar bevragen.

De preset en de opties horen bij de sleutel: een sleutel die met `Names` berekend is, is niet
vergelijkbaar met een sleutel die met `Default` berekend is. Leg dus ook vast wélke preset je
gebruikt hebt.

## 8. Overzicht van de API

| Type | Doel |
|---|---|
| `DutchFuzzy` | statische facade: `PhoneticKey`, `PhoneticKeys`, `SoundsLike`, `Similarity`, `AreEquivalent` |
| `DutchStringExtensions` | `ToDutchPhoneticKey`, `SoundsLikeDutch`, `DutchSimilarity`, `DutchFuzzyEquals` |
| `DutchEnumerableExtensions` | `FuzzyMatchDutch`, `BestDutchMatch` |
| `DutchFuzzyOptions` | onveranderlijke opties, met presets `Default`, `Names`, `Words` |
| `DutchPhoneticEncoder` | de Nederlandse regelgebaseerde encoder |
| `ColognePhoneticEncoder` | Kölner Phonetik, als tweede, onafhankelijke baseline |
| `DutchFuzzyScorer` | de gewogen score |
| `DutchPhoneticEqualityComparer` | `IEqualityComparer<string>` voor `Dictionary`, `HashSet`, `GroupBy` |
| `DutchFuzzyIndex<T>` | in-memory index met voorbewerkte sleutels; exact of geblokt zoeken |
| `DutchFuzzyIndexSearchMode` | `Exact` (standaard), `Blocked`, `BlockedOnly` |
| `LevenshteinDistance`, `DamerauLevenshteinDistance` (OSA), `JaroWinklerSimilarity`, `DiceCoefficientSimilarity`, `TokenSetSimilarity` | losse afstands- en gelijkenisalgoritmes |

Alle types zijn draadveilig zodra ze geconstrueerd zijn; `DutchFuzzyIndex<T>` is dat niet voor
gelijktijdige `Add` en `Search`.

## 9. Prestaties

Gemeten op .NET 10 (Release, x64, Windows) met 100.000 synthetische Nederlandse namen en de
preset `Names`. Reproduceer met `dotnet run --project bench/NlFuzzy.Benchmarks -c Release -- --filter '*'`.

| Operatie | Meting |
|---|---|
| `DutchFuzzy.PhoneticKey` | ~1,0 µs per string (50.000 sleutels in ~52 ms) |
| `DutchFuzzy.Similarity` | ~4,8 µs per paar |
| `FuzzyMatchDutch` (lineaire scan) | ~171 ms over 50.000 items |
| `new DutchFuzzyIndex<T>(...)` | ~415 ms voor 100.000 items |
| `DutchFuzzyIndex.Search` (`Blocked`) | ~12 ms per query op 100.000 items |

Drie dingen om te weten:

- **De index loont pas bij bulk.** Voor een eenmalige lookup is de lineaire `FuzzyMatchDutch`
  goedkoper dan het bouwen van een index.
- **`Exact` kost O(n) scores per query**, maar hergebruikt wel de voorbewerkte normalisatie en
  sleutel van elk item, en is daardoor nog steeds sneller dan een lineaire scan over ruwe strings.
  `Blocked` en `BlockedOnly` zijn sneller en minder volledig; benchmark beide op je eigen data met
  `IndexBenchmarks` (bevat `ExactSearch`, `BlockedSearch`, `BlockedOnlySearch` en
  `WorstCaseBucketSearch` voor hoge en lage drempels, met en zonder `take`).
- **De meting hierboven is een worst case.** Synthetische namen delen veel prefixen, waardoor de
  blokken groot worden. Op echte, meer gespreide data is een query beduidend sneller. Meet dus op
  je eigen data, en gebruik `DutchFuzzyIndexSearchMode.BlockedOnly` als je een harde bovengrens
  nodig hebt.

### Invoerlengte en onbetrouwbare invoer

Levenshtein, Damerau-Levenshtein en Jaro-Winkler zijn `O(n·m)` in de lengte van de invoer. Komt de
invoer van een webformulier of API, begrens die dan expliciet voordat je vergelijkt. Als richtlijn:
namen en adressen blijven ruim onder de 128 tekens; hanteer 256 tekens als bovengrens en weiger of
kap alles wat langer is. `InputLengthBenchmarks` in `bench/NlFuzzy.Benchmarks` meet die groei
(8 t/m 2.048 tekens) inclusief allocaties.

Interne optimalisaties die je niet hoeft aan te zetten: normalisatie en fonetische codering worden
per string één keer gedaan en hergebruikt over alle vergelijkingen (`PreparedText`), diakriet-
verwijdering slaat de Unicode-normalisatie over voor pure ASCII, de Dice-bigrammen worden als
gepakte `int`-sleutels vergeleken in plaats van als strings, en bij een positieve `take` houden
`FuzzyMatchDutch` en `DutchFuzzyIndex.Search` alleen een begrensde top-k-heap bij in plaats van
alle treffers te bewaren en volledig te sorteren.

## 10. Repository

```
src/NlFuzzy                     de bibliotheek
tests/NlFuzzy.Tests             xUnit-tests, inclusief de gouden dataset
samples/NlFuzzy.Sample.Console  rondleiding langs de hele API
bench/NlFuzzy.Benchmarks        BenchmarkDotNet-benchmarks
```

```bash
dotnet build -c Release
dotnet test
dotnet run --project samples/NlFuzzy.Sample.Console
dotnet run --project bench/NlFuzzy.Benchmarks -c Release -- --filter '*'
```

Versienummering gebeurt handmatig via `<Version>` in `src/NlFuzzy/NlFuzzy.csproj`. Een release
is een bump van dat nummer, een entry in [CHANGELOG.md](CHANGELOG.md) en een git-tag `v<Version>`;
`.github/workflows/release.yml` controleert dat tag en `<Version>` overeenkomen en publiceert
daarna naar nuget.org.

> Publiceren naar nuget.org vereist een repository-secret `NUGET_API_KEY`. Zolang die niet
> ingesteld is, faalt alleen de laatste stap van de release-workflow; `ci.yml` draait gewoon.

## 11. Bronnen

Fonetische algoritmes:

- Soundex — <https://en.wikipedia.org/wiki/Soundex>
- Kölner Phonetik / Cologne phonetics — <https://en.wikipedia.org/wiki/Cologne_phonetics>
- Apache Commons Codec, `ColognePhonetic` — <https://commons.apache.org/codec/apidocs/org/apache/commons/codec/language/ColognePhonetic.html>
- Beider-Morse Phonetic Matching — <https://stevemorse.org/phonetics/bmpm.htm>

Nederlands specifiek:

- Diederik Krols, *Fuzzy lookup of names with a Dutch Metaphone implementation* (2011) — <https://blogs.u2u.be/diederik/post/Fuzzy-lookup-of-Names-with-a-Dutch-Metaphone-implementation>
- Jan Jongboom, *Diakritische tekens en Soundex in .NET* (2010) — <https://blog.janjongboom.com/algoritmes/2010/12/14/diakritische-tekens-en-soundex-in-net.html>
- AMIS, *Ever wondered how Soundex works?* — <https://technology.amis.nl/oracle/ever-wondered-how-soundex-works/>

Nederlandse fonologie:

- Taalportaal (INT/Meertens), diftong /ɛi/ en de spelling ei/ij — <https://taalportaal.org/taalportaal/topic/pid/topic_20150611211611172>
- Final-obstruent devoicing — <https://en.wikipedia.org/wiki/Final-obstruent_devoicing>

Verwante .NET-bibliotheken:

- Lucene.Net.Analysis.Phonetic — <https://www.nuget.org/packages/Lucene.Net.Analysis.Phonetic/>
- F23.StringSimilarity — <https://www.nuget.org/packages/F23.StringSimilarity>

Alle algoritmes in deze bibliotheek zijn zelfstandig geïmplementeerd op basis van openbare
beschrijvingen. Er is geen code overgenomen uit GPL-gelicenseerde bronnen en geen implementatie van
derden geport. De onderbouwing per component staat in [PROVENANCE.md](PROVENANCE.md); de
dependency-status staat in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## 12. Licentie

MIT. Zie [LICENSE](LICENSE).

Bijdragen vallen onder dezelfde MIT-licentie (inbound = outbound); zie
[CONTRIBUTING.md](CONTRIBUTING.md).
