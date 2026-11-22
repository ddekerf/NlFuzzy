# Design

## Context

`tests/NlFuzzy.Tests/Golden/` holds `positive-pairs.csv` (42 rows) and `negative-pairs.csv`
(10 rows), each `left,right,comment`. `GoldenDatasetTests` asserts every row with the `Names` preset:
positives share a key or score ≥ 0.85, negatives have different keys and score < 0.75. The CSV
reader is a plain `Split(',')`, so names cannot contain commas today.

A probe of Wikidata (2026-11-22) found 2,351 "said to be the same as" (P460) links from Dutch-language
family names (P407 = Dutch), and 350 where both sides are Dutch family names. A sample shows true
variants (`De Bruin`/`De Bruyn`, `Klaasen`/`Klaassen`, `Brans`/`Brants`), derived names
(`Bos`/`Bosman`, `Kort`/`Korting`, `Hageman`/`Hage`) and items without a label.

## Goals / Non-Goals

**Goals:**
- A dataset large enough that per-category numbers mean something, with a split tuning never sees.
- Floors that catch regressions without demanding that every pair pass.

**Non-Goals:**
- Changing weights, rules, thresholds or presets (see proposal.md).
- Measuring the `Default` and `Words` presets. The loader and metrics take the options as a
  parameter, so adding them later is one line each.
- Real-world frequency weighting. Every pair counts once.

## Decisions

### 1. One CSV with a fixed column set, quoted fields allowed
`evaluation-pairs.csv` with header `left,right,label,category,source,split,note`.
- `label`: `match` or `nomatch`.
- `category`: a kebab-case id from a fixed list kept in the test code, e.g. `ij-ei-y`, `ae-aa`,
  `particle-glued`, `patronymic-sen-se`, `typo-transposition` for matches, and `nm-first-sound`,
  `nm-derived` for no-matches.
- `source`: `hand` or `wikidata:Q123/Q456`.
- `split`: `tune` or `holdout`.

The reader is replaced by a small RFC 4180-style parser (quotes and escaped quotes), so
`"Bakker, Jan"` is possible. The core files keep their format and loader.

*Alternative: one file per category.* Rejected: harder to deduplicate across categories, and the
split and integrity checks would need to walk many files.

### 2. Wikidata: committed snapshot, then hand labeling
`tests/NlFuzzy.Tests/Golden/sources/wikidata-p460-dutch-surnames.csv` holds the raw query result
(item ids, Dutch labels, fetch date), with the SPARQL query in a `README.md` next to it. Fetching is a
manual, documented step, never part of the build, so tests stay offline and reproducible. Each raw
pair is then labeled by hand:
- spelling variant → `match`
- derived or composite name → `nomatch` / `nm-derived`
- foreign cognate (`Timmermans`/`Zimmermann`) or unlabeled item → left out, with the reason noted in
  the snapshot so it is not reconsidered.

Wikidata data is CC0, so no attribution is required; it is still named in PROVENANCE.md and
THIRD-PARTY-NOTICES.md for traceability.

*Alternative: query Wikidata in CI.* Rejected: network-dependent tests, and the source can change
under a recorded baseline.

### 3. Hand-curated pairs fill the categories
Wikidata is surname-only and uneven across categories. Hand-curated pairs bring every match category
to at least 15 pairs and supply all first-name, typo, token-order and no-match categories. The pairs
are compiled from general knowledge of Dutch and Flemish spelling and name forms, like the existing
core.

### 4. Labeling happens before scoring
All labels are written and committed before the first evaluation run produces numbers. The commit that
records the baseline only adds the baseline file. This keeps "Labels do not follow the scorer"
checkable in history.

### 5. Split by a stable hash
`split = holdout` when `FNV-1a(lower(left) + "\u0001" + lower(right))`, taken over the alphabetically
ordered pair, mod 100 is below 30. FNV-1a is a few lines and gives the same result on every runtime;
`string.GetHashCode` is randomized per process. The split is stored in the CSV and re-verified by a
test, so a reviewer sees it and nobody can move a pair by hand.

### 6. Metrics and floors
`EvaluationMetrics` (test project) scores each pair once with `DutchFuzzy.Similarity` and the `Names`
preset. A pair counts as predicted-match when the score is at least `DefaultThreshold`. Per category
it reports the pass count and rate; overall it reports precision, recall and F1, separately for `tune`
and `holdout`.

`evaluation-baseline.json` stores the pass count per category and split, plus the overall metrics.
Floor test:
- per category: current passes ≥ baseline passes − 1;
- overall metrics: ≥ baseline − 0.005.

A higher score is fine; it prints a hint to raise the baseline.

*Alternative: floors as rates.* Rejected: with 15–40 pairs per category, one pair moves a rate by
several points, so a fixed rate tolerance is either too loose or flaky. A tolerance of one pair is
exact and easy to explain.

### 7. Report
The evaluation test writes a Markdown table through xUnit's `ITestOutputHelper`: category, split,
pairs, passes, rate, baseline. README gets a short "Kwaliteit" section with the overall holdout
numbers and a pointer to that test.

## Risks / Trade-offs

- [Labels are judgement calls] → Every row carries a category and an optional note, the PR is reviewed
  pair by pair, and ambiguous pairs are left out rather than guessed.
- [Hand-curated pairs carry the author's bias toward cases the rules handle] → Curate categories from
  the variant list, not from encoder output, and include the known weak categories (patronymics,
  first names) on purpose.
- [Wikidata P460 is noisy] → Hand labeling plus the "left out" reasons in the snapshot. Raw pairs are
  never used unlabeled.
- [Floors freeze today's misses as acceptable] → Intended: the floor only blocks regressions. Lifting
  quality is the follow-up change, and it must raise the baseline.

## Open Questions

- The exact category list may gain or merge entries during curation (for example, splitting
  `double-consonant` from `double-vowel`). The spec only fixes the minimum coverage and the 15-pair
  floor, so this does not change the plan.
