# Proposal

## Why

Similarity is computed on the full normalized string and the full phonetic key in their original
token order, so the same person written in a different order scores as a mismatch:
`Jan Bakker` vs `Bakker, Jan` scores 0.29 and `Piet de Vries` vs `de Vries, Piet` scores 0.27.
"Surname, First name" is the most common layout in customer records and exports, and deduplication
across such sources is the library's main use case. `TokenSetSimilarity` already ships in the
package, but the scorer never uses it.

## What Changes

- The similarity score becomes insensitive to token order: two inputs that contain the same names in
  a different order score as a near-perfect match, while an identical input still ranks above a
  reordered one.
- A new option `IgnoreTokenOrder` controls this. It is `true` in the `Default` and `Names` presets
  and `false` in the `Words` preset.
- `DutchFuzzyIndex<T>` in `Blocked` and `BlockedOnly` mode also finds reordered names, so the index
  stays consistent with the linear scan for this case.
- Phonetic keys are **not** changed: `PhoneticKey`, `SoundsLike` and `DutchPhoneticEqualityComparer`
  keep their order-sensitive behaviour.
- **BREAKING (scores, minor per the CHANGELOG policy)**: scores for multi-token inputs can only go up;
  callers with tuned thresholds may see extra matches.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `similarity-scoring`: adds token-order tolerance, the "identical ranks above reordered" rule, the
  opt-out, and the guarantee that keys stay order-sensitive.
- `fuzzy-index-search`: exact-mode parity explicitly covers `IgnoreTokenOrder`, and the blocked modes
  gain a token-order-independent block.
- `matching-options`: the documented defaults and the three presets gain `IgnoreTokenOrder`.

## Impact

- Code: `src/NlFuzzy/Comparison/DutchFuzzyScorer.cs` (prepared values and scoring),
  `src/NlFuzzy/DutchFuzzyOptions.cs` (new option and presets),
  `src/NlFuzzy/Indexing/DutchFuzzyIndex.cs` (extra block for the order-independent key).
- Public API: one new `init` property on `DutchFuzzyOptions`; `PublicApi.approved.txt` must be
  updated.
- Performance: one extra score per comparison when both inputs have two or more tokens; must be
  measured with the existing benchmarks.
- Docs: README §3 and §6, CHANGELOG `[Unreleased]`.
