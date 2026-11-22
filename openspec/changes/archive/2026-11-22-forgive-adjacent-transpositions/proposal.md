# Proposal

## Why

The `typo` category is the weakest in the evaluation set: with the `Names` preset only **16 of 40**
pairs match. 30 of the 40 are two adjacent letters typed in the wrong order (`Kuipers`/`Kuipres`,
`Hoekstra`/`Hoekstar`). The scorer handles a swap badly on every component:

- the phonetic keys usually differ in two positions, which Levenshtein counts as two edits;
- the bigram component loses up to three bigrams;
- a swap at the end of a name changes the last key character, so the final-sound penalty
  multiplies the score by 0.8 (`Hoekstra`/`Hoekstar` 0.627, `Smit`/`Smti` 0.495).

A trial on the current set, with a score floor of 0.85 for a single adjacent swap, raised `typo` to
39/40 and overall F1 from 0.938 to 0.957, with no new false match. The set cannot show that floor's
cost yet, though: none of its 402 no-match pairs is a single swap, while real names such as
`Born`/`Bron` or `Aerts`/`Arets` are.

## What Changes

- New option `DutchFuzzyOptions.TranspositionFloor` (`double`, default 0 = off). When both normalized
  texts are identical except for one pair of adjacent letters in swapped order, inside a token of at
  least five letters, the score is raised to at least this floor.
- The option is **0.85 in `Names`** and 0 in `Default` and `Words`. 0.85 is above the default
  threshold of 0.82 but below `IdenticalKeyFloor` (0.90), so a same-sounding spelling still ranks
  above a typo.
- Tokens shorter than five letters are excluded, because short swaps often spell a different name
  (`Born`/`Bron`, `Ruts`/`Rust`). This gives up `Smit`/`Smti`.
- The evaluation set grows:
  - a new no-match category `nm-transposition`: at least 15 pairs of distinct names that differ by
    one adjacent swap, including at least 5 of five or more letters, so the floor's cost shows;
  - at least 10 more `typo` pairs that are not swaps (substitutions, missing letters, extra letters),
    so the category no longer consists mostly of swaps.

  The new pairs are labeled before anything is scored, and the labels are for review in the PR.
- The evaluation baseline is rewritten once for the new pairs and once for the floor, each in a commit
  of its own.
- Not breaking for keys: phonetic keys, `SoundsLike` and the equality comparer are unchanged. `Names`
  scores change only for single-swap pairs.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `similarity-scoring`: a new requirement for the transposition floor.
- `matching-options`: the documented defaults, the presets and validation gain `TranspositionFloor`.
- `matching-quality-evaluation`: the typo category covers more than swaps, and the no-match categories
  cover distinct names that differ by one adjacent swap.

## Impact

- Code: `src/NlFuzzy/Comparison/DutchFuzzyScorer.cs` (one check after the weighted score) and
  `src/NlFuzzy/DutchFuzzyOptions.cs` (option, `Names` preset, validation).
- Public API: one new `init` property; `PublicApi.approved.txt` must be updated.
- Tests: scorer and options tests, README examples, `EvaluationDataset` (new category),
  `evaluation-pairs.csv` and `evaluation-baseline.json`.
- Docs: README §5 (preset table and option list) and the "Kwaliteit" numbers, plus a CHANGELOG entry.
- Index: `Exact` search scores every item, so it finds swap typos. `Blocked` and `BlockedOnly` look up
  candidates by phonetic key, and a swap changes the key, so they can still miss them, as they already
  do for any key difference.
- Stacked on `patronymic-se-sen` (PR #15), whose baseline this change starts from.
