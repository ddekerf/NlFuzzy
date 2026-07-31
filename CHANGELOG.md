# Changelog

Alle noemenswaardige wijzigingen aan dit project worden hier vastgelegd.

Het formaat is gebaseerd op [Keep a Changelog](https://keepachangelog.com/nl/1.1.0/).
Dit project volgt [Semantic Versioning](https://semver.org/lang/nl/).

## Versiebeleid voor fonetische sleutels

> **Een wijziging in de fonetische regeltabel die bestaande sleutels verandert, is een
> breaking change en vereist een nieuwe major versie.**

Dit geldt voor elke wijziging in `DutchPhoneticRules`, in de postprocessing van
`DutchPhoneticEncoder`, of in de normalisatie die de uitvoer van
`DutchFuzzy.PhoneticKey` beïnvloedt — ook als de publieke API zelf ongewijzigd blijft.

Persisteer je sleutels in een database, sla dan de major versie op in een
`PhoneticKeyVersion`-kolom naast de sleutelkolom en herbereken bij een major upgrade.
Zie de sectie "Sleutels persisteren" in de README.

Score-gewichten en drempels (`PhoneticWeight`, `DefaultThreshold`, de ankerstraffen, …)
vallen hier **niet** onder: dat zijn instelbare defaults en een wijziging daarin is een
minor change, mits die duidelijk in deze changelog staat.

Versienummering gebeurt handmatig via `<Version>` in `src/NlFuzzy/NlFuzzy.csproj`.
De release-workflow controleert dat de git-tag `v<Version>` daarmee overeenkomt.

## [Unreleased]

Nog geen wijzigingen.

## [1.0.0] - 2026-07-31

Eerste release. Volledige implementatie van het ontwerp in `PLAN-NlFuzzy.md`.

### Toegevoegd

- **Normalisatie** — `DutchTextNormalizer` (Unicode NFD-diakritverwijdering, `ß` → `ss`,
  `ĳ`/`Ĳ` → `ij`, `'s-Hertogenbosch` → `hertogenbosch`, `'t Zand` → `zand`, interpunctie,
  witruimtenormalisatie) en `DutchAffixes` (tussenvoegsellijst, `StripPrefixes`,
  `MovePrefixesToEnd`, `SplitGluedPrefixes`).
- **Afstandsalgoritmes** — `LevenshteinDistance`, `DamerauLevenshteinDistance` (OSA),
  `JaroWinklerSimilarity`, `DiceCoefficientSimilarity` (bigrammen),
  `TokenSetSimilarity`.
- **Fonetiek** — `DutchPhoneticEncoder` met een regeltabel van 86 Nederlandse regels en
  twee profielen (`Strict`, `Tolerant`), plus `ColognePhoneticEncoder` als referentie-
  encoder (geverifieerd tegen `Wikipedia` → `3412`, `Breschnew` → `17863`,
  `Muellerluedenscheidt` → `65752682`, `Peter` = `Petra` = `127`).
- **Scoring** — `DutchFuzzyScorer` met een gewogen combinatie van fonetische,
  orthografische en n-gramgelijkenis, `IdenticalKeyFloor` en ankerstraffen.
- **Publieke API** — de `DutchFuzzy`-facade, `DutchStringExtensions`,
  `DutchEnumerableExtensions`, `DutchPhoneticEqualityComparer`, `FuzzyMatch<T>`,
  `DutchFuzzyOptions` (records met `with`-support) en `DutchFuzzyIndex<T>` met bucketing
  op volledige sleutel plus prefixblok.
- **Presets** — `DutchFuzzyOptions.Default`, `.Names` en `.Words`.
- Multi-targeting op `netstandard2.0`, `net8.0` en `net10.0` met **nul**
  runtime-dependencies.
- Testproject met 429 tests, waaronder de volledige gouden dataset (36 positieve en
  9 negatieve paren), een regeldekkingstest die elke fonetische regel afdwingt, een
  publieke-API-approvaltest en tests voor elk README-voorbeeld.
- Voorbeeldproject `samples/NlFuzzy.Sample.Console` en benchmarks in
  `bench/NlFuzzy.Benchmarks` (BenchmarkDotNet).

### Afwijkingen van `PLAN-NlFuzzy.md`

Onderstaande beslissingen wijken bewust af van het ontwerpdocument. Ze zijn allemaal
empirisch gedreven: het plan schrijft voor dat de gouden dataset normatief is en dat de
regeltabel en gewichten worden bijgesteld tot de dataset volledig slaagt — niet andersom.

1. **Nieuwe preset `DutchFuzzyOptions.Names`** (Tolerant + `EncodeVowels = true` +
   `SplitGluedNamePrefixes = true` + `StripNamePrefixes = true`). De gouden dataset wordt
   hiertegen gevalideerd, niet tegen `Default`. Zonder klinkerklassen zakt het negatieve
   paar `Maes`/`Moes` niet onder de drempel; zonder het splitsen van vastgeschreven
   tussenvoegsels halen `Vandenbroecke`/`Van den Broecke` (0.268) en `Desmet`/`De Smet`
   (0.451) de drempel niet.
2. **Twee nieuwe opties**: `InitialPhonemeMismatchPenalty` en
   `FinalPhonemeMismatchPenalty` (beide standaard 0.20, multiplicatief toegepast wanneer
   het eerste respectievelijk laatste foneem verschilt). Zonder deze straffen scoren
   `Jansen`/`Hansen` 0.846 en `Kuipers`/`Kuiper` 0.897, ruim boven de drempel van 0.82,
   terwijl beide negatieve paren zijn. Geen enkele herweging van de drie bestaande
   componenten lost dat op zonder positieve paren te breken.
3. **Nieuwe regel `sk` → `SG` (Tolerant) / `SK` (Strict)** zodat `school` en `skool`
   samenvallen in het tolerante profiel.
4. **Extra klinkerdigrafen die niet in het plan staan**: `uij`, `eij`, `ey`, `ae`, `eau`,
   `aai`/`aaij`/`aay`, `ooi`/`ooij`/`ooy`, `ieuw`, `eeuw`, `uw` en `aauw`. De laatste is
   nodig omdat `Blaauw`/`Blauw` anders 0.591 scoort.
5. **`ng` → `Q` onvoorwaardelijk.** Het plan beperkt dit tot "niet gevolgd door een
   klinker"; die conditie maakte `Vlaanderen`-achtige gevallen inconsistent en levert geen
   meetbaar voordeel op de dataset.
6. **`ver` is opgenomen in het splitsen van vastgeschreven tussenvoegsels**, zodat
   `Verbrugghe` → `van der brugghe`.
7. **`qu` → `KF` in Tolerant** (het plan schreef `KV`). Tolerant voegt `v` en `w` samen tot
   `F`; `KV` zou daarmee inconsistent zijn.
8. **`Microsoft.SourceLink.GitHub` en `System.Memory` zijn weggelaten.** Het plan noemt ze
   optioneel; weglaten levert een package met letterlijk nul dependencies op alle drie de
   TFM's op, wat acceptatiecriterium 4 haalt met marge.
9. **`DutchTextNormalizer.Normalize(null)` gooit `ArgumentNullException`** in plaats van een
   lege string terug te geven (plan §6.1 stap 1). Gekozen voor consistentie met de rest van
   de API, die overal null-guards heeft.
10. **`FuzzyMatchDutch` en `BestDutchMatch` gooien `ArgumentOutOfRangeException`** wanneer
    `threshold` buiten `[0, 1]` valt. Het plan specificeert dit niet.
11. **`PublicApiGenerator` wordt niet gebruikt.** `PublicApiTests` doet een eigen
    reflectie-dump tegen `Api/PublicApi.approved.txt`, wat hetzelfde effect heeft zonder
    een extra testafhankelijkheid.

### Bekende beperkingen

- `DutchFuzzyIndex<T>.Search` valt terug op een volledige scan wanneer geen enkele bucket
  raakt. Geef `allowFullScan: false` mee als je een harde bovengrens op de zoektijd nodig
  hebt.
- Het `Strict`-profiel onderscheidt uitsluitend stemhebbend/stemloos (b/p, d/t, v/f, z/s).
  `ch` en `g` vallen in **beide** profielen samen, net als `w` vóór `r`.
- `DamerauLevenshteinDistance` implementeert de OSA-variant: `Compute("ca", "abc")` is 3,
  niet 2.

[Unreleased]: https://github.com/ddekerf/NlFuzzy/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/ddekerf/NlFuzzy/releases/tag/v1.0.0
