# Tasks

## 1. Format and integrity checks

- [x] 1.1 Add the evaluation CSV reader with RFC 4180 quoting in the test project, with unit tests for quoted commas and escaped quotes; verify the reader tests pass
- [x] 1.2 Add the fixed category list (match and no-match ids) and `EvaluationDatasetTests` for: known label/category/source/split values, no duplicates in either order, no pair in both labels, no overlap with the core files, at least 600 `match` / 400 `nomatch`, and at least 15 pairs per category; verify they fail on an empty `evaluation-pairs.csv` with a header only
- [x] 1.3 Implement the FNV-1a split function and a test that every row's recorded split matches it and the holdout share is 25–35%; verify with three hand-computed pairs

## 2. Wikidata source

- [x] 2.1 Add `Golden/sources/README.md` with the SPARQL query (Dutch family names linked by P460 to Dutch family names), the fetch command and the CC0 note; verify the query runs and returns about 350 rows
- [x] 2.2 Commit the dated raw snapshot `wikidata-p460-dutch-surnames.csv` with item ids and Dutch labels; verify the row count matches the query result
- [x] 2.3 Label every snapshot row as `match`, `nomatch` (derived) or excluded with a reason, and copy the labeled pairs into `evaluation-pairs.csv` with `source = wikidata:Qa/Qb`; verify the provenance test that every Wikidata row's ids occur in the snapshot

## 3. Hand-curated pairs

- [x] 3.1 Curate match pairs per category (README §4 variants, particles separated/glued/elided, token order, diacritics and ligatures, patronymic `-s`/`-se`/`-sen`, first names, typos) until every match category has at least 15 pairs and the total reaches about 600 with the Wikidata matches; verify the category-coverage test for match categories
- [x] 3.2 Curate no-match pairs (first sound, final sound/plural, same prefix with a different stem, short names, vowel class, derived names, unrelated) to about 400; verify the size and coverage tests pass
  - Only a `-se`/`-sen` swap is labeled `match`; other ending changes (`-s`, `-e`, `-en`, `-s`/`-sen`) are `nomatch` (`nm-final-sound`), consistent with the core `Kuipers`/`Kuiper`. The spec delta and proposal were corrected accordingly. Wikidata yielded 254 labeled rows (96 excluded, including two case-only duplicates found by the integrity test).
- [x] 3.3 Assign every row's split with the FNV-1a function, then commit the labeled dataset before any evaluation run; verify the split test and the full integrity suite pass

## 4. Metrics, baseline and floors

- [x] 4.1 Add `EvaluationMetrics` (per-category pass counts and rates; overall precision, recall and F1 per split) with unit tests on a five-pair synthetic set with known results
- [x] 4.2 Add the evaluation test that writes the per-category Markdown report through `ITestOutputHelper`; verify the report shows every category for both splits
- [x] 4.3 Run the evaluation once on the committed dataset and record `evaluation-baseline.json` in a commit of its own; verify the floor test passes on the unchanged scorer
- [x] 4.4 Verify the floor test fails as specified: temporarily set `FinalPhonemeMismatchPenalty` to 0.6 in a scratch run, check that the failing category and its baseline are named, then discard the scratch change
  - Raising `FinalPhonemeMismatchPenalty` to 0.6 did **not** trip the floor: it only lowers scores for pairs whose last sound differs, so the no-match categories get stricter and no category regresses. The check was redone with `InitialPhonemeMismatchPenalty` near zero (0.001; 0 itself trips analyzer CA1805), which failed as specified: `nm-first-sound (tune): 32 passes, baseline 45, allowed 44`, `nm-first-sound (holdout): 4 passes, baseline 7`, plus overall precision and F1. The scratch change was reverted.

## 5. Docs

- [x] 5.1 Add the README "Kwaliteit" section with the overall holdout numbers and how to run the evaluation; verify the numbers match the baseline file
- [x] 5.2 Record the Wikidata source (query, date, CC0) in PROVENANCE.md and THIRD-PARTY-NOTICES.md, and note in CONTRIBUTING.md how to add pairs and when the baseline may change; verify the links resolve in the Markdown preview
  - THIRD-PARTY-NOTICES.md also had stale development-dependency versions from before the Dependabot updates on main; corrected to xunit.runner.visualstudio 4.0.1, Microsoft.NET.Test.Sdk 18.11.0, coverlet 10.1.0 and BenchmarkDotNet 0.15.8.

## 6. Integration check

- [x] 6.1 Run the full `dotnet test -c Release` suite and confirm the core golden tests are unchanged, the evaluation adds under one second, and the build has no warnings on all three TFMs
