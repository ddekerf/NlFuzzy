# Design

## Context

`DutchFuzzyScorer`'s private `Score` core mixes Jaro-Winkler on the normalized text (0.35), one minus
the normalized Levenshtein distance between the phonetic keys (0.45) and bigram Dice (0.20). It then
applies `IdenticalKeyFloor` when the keys are equal, or the anchor penalties when they are not. A swap
of two adjacent letters hits all three components and often an anchor too:

| Pair | Keys (`Names`) | Score today |
|---|---|---:|
| `Kuipers`/`Kuipres` | `K7P2RS` / `K7PR2S` | 0.740 |
| `Hoekstra`/`Hoekstar` | `7KSTR1` / `7KST1R` (last differs) | 0.627 |
| `Smit`/`Smti` | `SM3T` / `SMT3` (last differs) | 0.495 |

Typos are scored on spelling, not sound, so the fix belongs in the scorer, not the encoder.

## Goals / Non-Goals

**Goals:**
- A single adjacent swap in a name of five or more letters scores at least the default threshold with
  `Names`.
- No change to phonetic keys, `SoundsLike`, the equality comparer, or the `Default` and `Words` presets.
- The cost (distinct names that become matches) becomes measurable in the evaluation set.

**Non-Goals:**
- Substitutions (`Koster`/`Kosrer`), missing or extra letters, or keyboard-distance models. The new
  non-swap typo pairs only measure them.
- Making `Blocked`/`BlockedOnly` index search find swaps whose keys differ.
- Changing weights, penalties or `IdenticalKeyFloor`.

## Decisions

### 1. A floor, not a different distance
After the weighted score and the anchor penalties: when `TranspositionFloor > 0` and the normalized
texts are one adjacent swap apart, `score = max(score, TranspositionFloor)`. The keys must differ,
since equal keys already get `IdenticalKeyFloor`.

*Alternatives measured on the current set:*

| Variant | `typo` | New false matches |
|---|---:|---:|
| Today | 16/40 | — |
| Optimal-string-alignment distance on the keys (a swap is one edit) | 21/40 | 0 |
| ...plus no anchor penalties for a swap | 27/40 | 0 |
| Floor 0.85 | 39/40 | 0 |
| Floor 0.85 plus both of the above | 39/40 | 0 |

The floor alone does the most and is the easiest to explain. It also leaves the three components and
their spec untouched. Rejected: the combined variant adds code without gaining a pair.

### 2. Detecting the swap
The check is a single pass over the normalized texts. They must have equal length and differ at
exactly two positions `i` and `i + 1`, with `a[i] == b[i + 1]` and `a[i + 1] == b[i]`. Neither
character may be a space, and the token around `i` (bounded by spaces or the ends of the text) must
have at least five characters. A swap across a space (`Jan Smit` / `Ja nSmit`) does not count, so the
floor cannot join or split tokens.

The check uses the normalized text, so case, accents and particle placement are already resolved.
`de Vries`/`de Vreis` count: the particle is moved but identical on both sides. The check also runs on
the canonical, token-sorted form, so a reordered name with a swap scores `0.85 × 0.97 ≈ 0.82`, which
is still at the default threshold.

### 3. A `double` option, on in `Names`
`TranspositionFloor` (`double`, `init`, default 0 = off), validated to [0, 1] like
`IdenticalKeyFloor`. `Names` sets 0.85. The value sits above the default threshold (0.82) with a margin,
and below `IdenticalKeyFloor` (0.90), so `Meier` still ranks above `Mejier` when both are compared with
`Meijer`. A `double` instead of a `bool` keeps the floor tunable, in the same way as the existing
floor.

`Words` stays at 0, because in running text swapped words are often different words (`form`/`from`).
`Default` stays at 0 so that the plain string extensions keep their current scores.

### 4. Five-character minimum per token
Measured: no minimum gives `typo` 39/40, and a five-character minimum gives 38/40 (it loses
`Smit`/`Smti`). In four-letter names, a swap often spells another real name (`Born`/`Bron`,
`Ruts`/`Rust`, `Kort`/`Krot`), and the evaluation set already treats short names as the risky area
(`nm-short`). This is an internal constant, like `MinimumSeSenTokenLength`.

### 5. Dataset first, labeled blind
The new pairs are added and labeled before the floor is written, and the baseline is rewritten for
them in a commit of their own (`NLFUZZY_WRITE_BASELINE`). Only then does the floor go in, with a second
baseline commit. This keeps "the set got harder" apart from "the scorer got better", and it follows
the rule that labels do not follow the scorer.

- `nm-transposition` (no-match, new): at least 15 pairs of distinct, attested names that are one
  adjacent swap apart. At least 5 have five or more letters (for example `Aerts`/`Arets`), so the
  floor's false matches show. The four-letter ones (`Born`/`Bron`) document the minimum.
- `typo` (match, extended): at least 10 more non-swap typos (20 in all; today it has exactly 10): substitutions, missing letters and extra
  letters, at different positions.
- Integrity tests check the new rules: every `nm-transposition` pair is one swap, at least 5 have five
  or more letters, and at least 20 `typo` pairs are not swaps. The split hash is per pair, so existing
  rows keep their split.

## Risks / Trade-offs

- [Distinct names of five or more letters that are one swap apart now match (`Aerts`/`Arets` 0.739 →
  0.850)] → Measured by the new category's five-or-more-letter pairs. If the floor costs more than it
  gains on the holdout split, stop and revisit before raising the baseline.
- [Every single-swap typo gets the same score of 0.85, so they tie when ranked] → A higher component
  score still wins, and ties are already broken deterministically by the ranking code.
- [The `typo` category was mostly swaps, so 39/40 overstates general typo tolerance] → The non-swap
  pairs make the category honest. Their misses are expected and stay labeled `match`.
- [Hot-path cost] → One length check, then a scan that stops at the third differing position. It only
  runs when the keys differ and the floor is above 0. Check with the existing scorer benchmark: no
  more than 5% slower.
