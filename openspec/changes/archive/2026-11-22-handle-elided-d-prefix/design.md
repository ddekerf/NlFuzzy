# Design

## Context

`DutchTextNormalizer.Normalize` lower-cases and folds the input (`FoldForPhonetics`), then
`KeepLettersAndSpaces` turns every non-letter, apostrophes included, into a space. After that the
apostrophe is gone: `d'hondt` and `d. hondt` both become `d hondt`, and `d` is not in the particle
list. `'s-` and `'t` already work only because `s` and `t` *are* listed particles. The current test
`DutchTextNormalizerTests` pins `D'Hondt` → `d hondt`.

The glued spelling `dhondt` has no prefix entry in `DutchAffixes.GluedPrefixes`, so it is left as one
token whose key starts with `T4NT` instead of matching `hondt de`.

## Goals / Non-Goals

**Goals:**
- `D'Hondt`, `D’Hondt`, `Dhondt` (with splitting on) and `De Hondt` normalize identically.
- No false positives for initials (`D. Jansen`) or colloquial `d'r`.

**Non-Goals:**
- Other elisions such as `l'`, `O'` or Italian/French `dell'`. They are rare in NL/BE records; add
  them later if real data asks for it.
- Adding `d` to the particle list. A bare `d` is far more often an initial than an article.

## Decisions

### 1. Rewrite the elision before punctuation is stripped
Add a step between `FoldForPhonetics` and `KeepLettersAndSpaces` that rewrites `d` + apostrophe to
`de ` when the `d` starts a token (start of string, or after a space or hyphen) and at least three
letters follow the apostrophe. The apostrophe is the only signal that separates an article from an
initial, so the rule has to run while it still exists. It runs in every preset, because `D'Hondt` is
the canonical spelling of the name, not a glued variant.

The apostrophe set is `'`, `’`, `‘`, `` ` `` and `´`. Typographic quotes are common in data pasted from
Word and Excel. Do the check with a single forward scan that only allocates when a rewrite happens,
so the ASCII fast path stays cheap.

*Alternative: regex.* Rejected: a hand-written scan is consistent with the rest of the normalizer,
allocation-free in the common case, and behaves the same on `netstandard2.0`.

### 2. Treat `dh` + vowel as a glued prefix only when splitting is on
In `DutchAffixes.SplitToken`, when no regular glued prefix matched, split a token of five or more
letters that starts with `dh` + vowel into `de` + `h…`. This lives behind `SplitGluedNamePrefixes`
like the other glued forms, because the apostrophe-less spelling is the same recall/precision
trade-off.

### 3. Coordination with `fix-glued-prefix-oversplitting`
Both changes touch `SplitToken`. The `dh` rule must also respect the never-split list introduced
there. Whichever change lands second rebases onto the first; the scenarios of both specs must still
pass.

## Risks / Trade-offs

- [Non-Dutch `dh` words such as `Dharma` or `Dhaka` get split with the `Names` preset] → Accept or
  add them to the never-split list from `fix-glued-prefix-oversplitting`. They are rare in NL/BE
  person data.
- [`D'Angelo` now normalizes like `De Angelo`] → Acceptable and arguably correct: both spellings
  occur for the same Italian surname.
- [Keys change for `d'` names in every preset] → Not published yet. Log a phonetic-key **BREAKING**
  note in `[Unreleased]`.
