# Design

## Context

`DutchFuzzyScorer.Prepare` normalizes a value once and encodes it into a space-separated key whose
tokens line up one-to-one with the normalized tokens (`bakker jan` ↔ `P1K2R J1N`). `Score` then runs
Jaro-Winkler and Dice on the whole normalized string and Levenshtein on the whole key, and applies
anchor penalties to the first and last key characters. All three components and both penalties are
order-sensitive, which is why `Jan Bakker` vs `Bakker, Jan` collapses to 0.29.

`DutchFuzzyIndex<T>` caches a `PreparedText` per item and blocks on the full key and on its first
four characters (`PrefixBlockLength`). A reordered name lands in different blocks.

Particle handling (`MovePrefixesToEnd`) only moves a *leading* particle run, so `Piet de Vries` stays
`piet de vries` while `de Vries, Piet` becomes `vries piet de`. The token multisets are equal, which
is what this design relies on.

## Goals / Non-Goals

**Goals:**
- Reordered tokens score as a near-match without lowering any existing score.
- Prepared values stay reusable, so the index and `FuzzyMatchDutch` keep their prepare-once cost model.

**Non-Goals:**
- Partial or subset matching (`Bakker` vs `Jan Bakker`), initials (`J. Bakker` vs `Jan Bakker`), or
  missing middle names. These raise very different precision questions and get their own change if
  wanted.
- Order-insensitive phonetic keys. Keys are persisted by callers; changing them is a key-version
  decision with its own trade-offs.

## Decisions

### 1. Score a canonical token order next to the original order and take the maximum
In `Prepare`, also build a *canonical* form: pair each normalized token with its key token, sort the
pairs by key token (ordinal), tie-break by normalized token, and join both sides again. Sorting by
the phonetic key rather than the spelling keeps spelling variants (`Cuypers`/`Kuipers` → `K7P2RS`)
in the same position. When `IgnoreTokenOrder` is on and both sides have two or more tokens,
`Score` = max(ordered score, canonical score × 0.97). Single-token inputs skip the second
computation entirely.

The fixed 0.97 factor gives the "identical > reordered" ordering the spec requires and keeps a
reordered exact match at 0.97 (≥ 0.95). It is not an option: one more tuning knob is not worth the
extra API surface, and the factor can become an option later without breaking anything.

*Alternative: blend in the existing `TokenSetSimilarity` as a fourth weighted component.* Rejected:
it lowers scores for every pair whose tokens differ slightly in spelling (token set works on exact
token equality), so existing golden-dataset scores would shift and need retuning.

*Alternative: always sort before scoring.* Rejected: it loses the genuine signal in the original
order and changes scores for pairs that are already in the same order.

### 2. Store the canonical form in `PreparedText`
Add canonical normalized text and canonical key fields to the internal `PreparedText` struct, computed
once in `Prepare` only when the option is on and the value has two or more tokens. The scoring hot
path stays allocation-free beyond what it already does.

### 3. Index adds a canonical-key block
When the option is on, `DutchFuzzyIndex<T>` also registers each multi-token item under its
canonical key, and `CollectCandidates` looks up the query's canonical key too. The index is in memory
and rebuilt per process, so this is not a persisted-key change.

### 4. Option and presets
`DutchFuzzyOptions.IgnoreTokenOrder` (`bool`, `init`). The property initializer is `true`, so
`Default` and `Names` get it; `Words` sets it to `false` explicitly. `Validate` needs no change.

## Risks / Trade-offs

- [Extra matches for callers with tuned thresholds] → Scores only go up for multi-token inputs.
  Document it in the CHANGELOG as a scoring change (minor per policy), with a one-line opt-out.
- [Roughly twice the scoring cost for multi-token pairs] → Measure with the existing
  `IndexBenchmarks` and the linear-scan benchmark; acceptance is no more than a 2× regression on
  multi-token data and no change on single-token data. Update README §9 with the new numbers.
  Outcome: the straightforward version measured 2.1–2.6×, so three result-preserving
  optimizations were added. Dice bigrams are precomputed and sorted once per `PreparedText` and
  intersected by merge; Jaro-Winkler and Levenshtein reuse per-thread scratch buffers; and the
  canonical score is skipped when the ordered score is already ≥ 0.97 or both sides are already in
  canonical order. The result is ~1.0–1.3× on multi-token data, with single-token scoring slightly
  faster than before.
- [Anchor penalties now also apply to the canonical form] → Intended: the penalties still separate
  `Jan Jansen` from `Jan Hansen` in either order.
