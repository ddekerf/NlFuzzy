# Proposal

## Why

Patronymic surnames end in `-sen` and `-se` interchangeably (`Willemsen`/`Willemse`,
`Pietersen`/`Pieterse`): the `-se` form is common in Zeeland and in records where spoken Dutch drops
the final `-n` after a schwa. They are the same name, but the evaluation set from
`expand-golden-name-dataset` matches **0 of 25** such pairs with the `Names` preset. With vowel
classes the keys differ only by a trailing `N` (`F3L2MS2N` vs `F3L2MS2`), and the final-sound penalty
then pushes the score to about 0.74.

## What Changes

- New option `DutchFuzzyOptions.MergeSeSenEndings`: when true, a token of at least five letters that
  ends in `-sen` or `-zen` is encoded as if it ended in `-se` / `-ze`. `Willemsen` and `Willemse`
  share a key, so `SoundsLike`, `DutchPhoneticEqualityComparer` and index blocking treat them as the
  same name too.
- The option is **true in `Names`** and false in `Default` and `Words`. Without vowel classes
  (`Default`), `Willems` and `Willemse` already share a key, so the rule would also merge `Willems` with
  `Willemsen`, which the evaluation set labels as different names.
- `-s` versus `-sen` (`Willems`/`Willemsen`) and other ending changes keep different keys.
- The evaluation baseline is raised in a commit of its own, once the improvement is measured on both
  splits.
- **BREAKING (phonetic keys, `Names` only)**: keys change for every token ending in `-sen`/`-zen` of five
  or more letters. Nothing is published to nuget.org yet, so no persisted keys are affected.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `dutch-phonetic-encoding`: a new requirement for encoding `-sen`/`-zen` like `-se`/`-ze` when the
  option is on.
- `matching-options`: the documented defaults and the presets gain `MergeSeSenEndings`.

## Impact

- Code: `src/NlFuzzy/Phonetics/DutchPhoneticEncoder.cs` (one rewrite before a token is encoded) and
  `src/NlFuzzy/DutchFuzzyOptions.cs` (option and `Names` preset).
- Public API: one new `init` property; `PublicApi.approved.txt` must be updated.
- Tests: encoder and options tests, README examples, and a raised `evaluation-baseline.json`.
- Docs: README §4 and §5, the "Kwaliteit" numbers, and a CHANGELOG entry with a key BREAKING note.
- Depends on `expand-golden-name-dataset` (PR #14) for the evaluation set and the floors; this branch
  is stacked on it.
