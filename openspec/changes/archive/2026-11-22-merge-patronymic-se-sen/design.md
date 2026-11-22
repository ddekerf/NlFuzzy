# Design

## Context

`DutchPhoneticEncoder.EncodeToken` walks a normalized token through the rule table, then
post-processes the output: repeats are collapsed, and vowels are kept as classes or dropped. With the
`Names` preset (vowel classes), `Willemsen` → `F3L2MS2N` and `Willemse` → `F3L2MS2`. The scorer adds
`IdenticalKeyFloor` (0.90) only when keys are equal, and multiplies by 0.8 when the last key
characters differ, so the pair ends near 0.74 (see proposal.md).

In the evaluation set, no `nomatch` pair is a `-se`/`-sen` swap. The `-s`/`-sen` no-matches
(`Willems`/`Willemsen`, `Hendriks`/`Hendriksen`, Wikidata's `Harmsen`/`Harms`) end in `S` versus
`S2N`; after this change that becomes `S` versus `S2`, which still differs. One Wikidata match,
`Hansen`/`Hanzen`, only shares a key today because Tolerant maps `z` to `S`; it must keep sharing it.

## Goals / Non-Goals

**Goals:**
- `-sen` and `-se` forms share a key with the `Names` preset, so the pair scores at least
  `IdenticalKeyFloor`.
- No change for `Default`, `Words`, or any other ending.

**Non-Goals:**
- General schwa-`n` deletion (`Vinken`/`Vinke`, `Schouten`/`Schoute`). The evaluation set labels
  `-e`/`-en` pairs as different names (`nm-final-sound`), and a general rule would merge them.
- Changing weights or penalties.

## Decisions

### 1. Rewrite the token's spelling, not the key
In `EncodeToken`, before the rule loop: when `MergeSeSenEndings` is on, the token has at least five
letters and ends in `sen` or `zen`, encode `token[..^1]` instead. Working on spelling keeps the rule
independent of profile and vowel mode, and it covers `-zen` explicitly (`Hanzen`).

*Alternative: strip a trailing `N` after `S2` from the key.* Rejected: that pattern also comes from
other spellings (`-tsen`, `-cen` before e/i), it depends on the vowel class, and it is harder to
explain than "`-sen` reads as `-se`".

*Alternative: rewrite in the normalizer.* Rejected: the normalized text feeds the orthographic and
n-gram components too, so `Willemsen`/`Willemse` would become identical text and score exactly 1.0,
leaving no preference for the exact spelling. In the encoder, the keys match while the text still
differs, so the pair scores about 0.9 and exact matches still rank first.

### 2. A separate option, on in `Names`
`MergeSeSenEndings` (`bool`, `init`, default false), set to true in the `Names` preset. This mirrors
`IgnoreTokenOrder`: a named behaviour that callers can switch off. Gating it on an existing flag such
as `EncodeVowels` would silently turn it on for `Words` as well, which is not a names preset.

### 3. Five-letter minimum
The rule only applies to tokens of five or more letters, so a bare `-sen` with at most one letter in
front stays as it is.

*Revised during implementation:* the plan said six letters, to keep the place name `Assen` apart from
the Flemish town `Asse`. The evaluation showed that a letter count depends on spelling: `Gijsen`
(6 letters) lost its `n` but `Gysen` (5 letters, the same name) did not, and the pair dropped from
0.900 to 0.594. A sound-based guard cannot help either, because `Gysen` and `Assen` both have a
two-sound stem. Spelling variants must not decide whether the rule applies, so the minimum is five,
and `Assen`/`Asse` now share a key. That fits the rule's own logic (a final `-n` after `-se` is
dropped in speech), and the option is `Names`-only.

### 4. Measure, then raise the baseline
The floor test must stay green on the unchanged categories. The patronymic category is expected to go
from 0/22 (tune) and 0/3 (holdout) to almost all of them. Once the report confirms it on both splits,
the baseline is rewritten with `NLFUZZY_WRITE_BASELINE` in a commit of its own that states the reason.

## Risks / Trade-offs

- [`-s`/`-sen` no-matches move one edit closer] → They keep a different final key character, so the
  final-sound penalty still applies. The floor test covers `nm-final-sound`; if any pair crosses the
  threshold, stop and revisit before raising the baseline.
- [A near-threshold pair loses a pass because the shared trailing `N` no longer pads the keys] → Seen
  once: `Stienissen`/`Stinnissen` (0.827 → 0.811). Its keys already differ a lot because of the
  existing `ti`-before-vowel rule (`S2N3S2` vs `ST3N3S2`), so dropping the `N` makes that difference
  count more. Accepted within the floor tolerance; the `ti` rule is a separate change.
- [Non-patronymic names of five or more letters ending in `-sen`/`-zen`, such as `Huizen`] →
  `Huizen`/`Huize` would share a key. In names these are spelling variants of the same name too, and
  the option is `Names`-only.
- [Keys change for `Names`] → No published package yet; record as **BREAKING** for keys in
  `[Unreleased]`.
