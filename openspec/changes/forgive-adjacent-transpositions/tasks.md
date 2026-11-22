# Tasks

## 1. Extend the evaluation set (before any scorer change)

- [x] 1.1 Add `nm-transposition` to `EvaluationDataset.NoMatchCategories`, and add integrity tests:
  - every `nm-transposition` pair is a single adjacent swap of its other side, and at least 5 pairs
    have five or more letters;
  - at least 20 `typo` pairs are not a single adjacent swap (today: exactly 10).

  Confirm that both tests fail on today's CSV.
- [x] 1.2 Add at least 15 `nm-transposition` rows to `evaluation-pairs.csv` (source `hand`). Each pair
  is two distinct, attested names one adjacent swap apart (`Born`/`Bron`, `Ruts`/`Rust`,
  `Aerts`/`Arets`, …), and at least 5 have five or more letters. Give each row a note on why both
  spellings are separate names, and set the split from the pair hash.
- [x] 1.3 Add at least 10 non-swap `typo` rows: substitutions, missing letters and extra letters, at
  the start, middle and end of names. Write the labels without scoring the pairs (design.md §5).
  Verify that all integrity tests pass, the holdout share stays between 25% and 35%, and no existing
  row's split changed.
- [ ] 1.4 Run `EvaluationTests` with the detailed logger and record the new categories' counts here.
  Rewrite `evaluation-baseline.json` with `NLFUZZY_WRITE_BASELINE="<reason>"`, and commit the dataset
  and baseline with that reason. Verify that the floor test passes.
  - Added 28 rows: 16 `nm-transposition` (8 of four letters, 8 of five or more) and 12 non-swap `typo`
    (5 substitutions, 4 missing letters, 3 extra letters), 10 of them in holdout. With the scorer
    unchanged, `nm-transposition` is 10/10 tune and 6/6 holdout. `typo` goes from 12/28 to 17/36
    (tune) and from 4/12 to 6/16 (holdout), so 7 of the 12 new typos already match. Overall F1:
    tune 0.946 → 0.944, holdout 0.914 → 0.910. Only overall recall tripped the floor (tune 0.9226 vs
    0.9228 allowed, holdout 0.8830 vs 0.8872), which is why the baseline is rewritten here.

## 2. Option and scorer

- [ ] 2.1 Add `DutchFuzzyScorerTests` for the spec scenarios. With `Names`:
  - `Kuipers`/`Kuipres`, `Hoekstra`/`Hoekstar` and `Jan Kuipers`/`Jan Kuipres` score at least 0.85;
  - `Born`/`Bron`, `Jan Smit`/`Jan Smti`, `Kuipers`/`Kuirpes` and `Kuipers`/`Kuipars` score the
    same as with `TranspositionFloor` 0;
  - `Meier` ranks above `Mejier` for `Meijer`.

  With `Default`, `Kuipers`/`Kuipres` keeps today's score. Confirm that the floor cases fail on the
  current code.
- [ ] 2.2 Add `TranspositionFloor` (default 0) to `DutchFuzzyOptions`, validated to [0, 1], and set it
  to 0.85 in `Names`. Add `DutchFuzzyOptionsTests` for the defaults, the three presets and the
  rejected values (-0.1, 1.5, NaN). Update `PublicApi.approved.txt` and verify that `PublicApiTests`
  passes.
- [ ] 2.3 In the private `DutchFuzzyScorer.Score` core, after the anchor penalties and only when the
  keys differ: raise the score to `TranspositionFloor` when the normalized texts are one adjacent swap
  apart. Neither character may be a space, and the token must have at least five characters (internal
  constant). Verify that the 2.1 tests and the property tests (finite, symmetric, [0, 1], token-order
  monotonicity) pass, and that the core golden dataset is unchanged.

## 3. Measure and raise the baseline

- [ ] 3.1 Run `EvaluationTests` with the detailed logger and record the before/after counts here.
  - Verify that `typo` rises on both splits.
  - `nm-transposition` losses on five-or-more-letter pairs are expected; record them here.
  - Check every other category against the floor tolerance.
  - If the holdout split loses more correct no-match answers than it gains matches, stop and revisit
    before raising the baseline (design.md risks).
- [ ] 3.2 Rewrite `evaluation-baseline.json` with `NLFUZZY_WRITE_BASELINE="<reason>"`, and commit only
  that file, with the reason and the new holdout numbers in the message. Verify that the floor test
  passes against the new baseline.
- [ ] 3.3 Run the scorer benchmark (`bench/NlFuzzy.Benchmarks`, `--job short --inProcess`) before and
  after 2.3, and verify that it is no more than 5% slower.

## 4. Docs

- [ ] 4.1 README:
  - add `TranspositionFloor` to the §5 preset table and the option list, with the five-letter minimum
    and an example (`Kuipers`/`Kuipres`);
  - update the "Kwaliteit" numbers, including the new categories;
  - add matching `ReadmeExamplesTests`, and verify that they pass.
- [ ] 4.2 Add an `[Unreleased]` CHANGELOG entry under "Toegevoegd" for the option, and under
  "Gewijzigd" for the `Names` scores of single-swap pairs and the extended evaluation set.

## 5. Integration check

- [ ] 5.1 Run the full `dotnet test -c Release` suite and build all three TFMs without warnings. Confirm
  that the core golden tests and every evaluation floor pass.
