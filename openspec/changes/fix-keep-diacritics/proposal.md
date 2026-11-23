# Proposal

## Why

`DutchFuzzyOptions.RemoveDiacritics = false` is supposed to keep accented letters, but it breaks the
input instead. The normalizer keeps only `a`–`z`, so with folding off every accented letter becomes a
word separator:

| Input | Normalized | Key | Key with folding on |
|---|---|---|---|
| `Céline` | `c line` | `K LN` | `SLN` |
| `Hélène` | `h l ne` | `L N` | `ALN` |
| `coördinatie` | `co rdinatie` | `K RTNS` | `KRTNS` |
| `Müller` | `m ller` | `M LR` | `MLR` |

One name turns into several tokens with wrong keys, so the option is unusable. The current
`text-normalization` spec even records this as intended ("Accented letters are dropped when diacritic
folding is disabled", `Renée` → `ren e`), because that spec describes the code as it was.

## What Changes

- With `RemoveDiacritics = false`, the normalizer **keeps accented Latin letters**: any lower-case
  letter that decomposes to `a`–`z` plus combining marks (`é`, `ü`, `ç`, `ñ`, `å`, …). It composes them
  to NFC, so composed and decomposed input give the same text. `Céline` → `céline`, and `Renée` →
  `renée`. Other letters (Greek, Cyrillic, `ł`) stay word separators, exactly as with folding on.
- The **phonetic key always folds diacritics**: the Dutch encoder folds each normalized token before
  applying its rules, as the Cologne encoder already does. With the option off, `Céline` keeps the key
  `SLN`, so `SoundsLike`, `DutchPhoneticEqualityComparer` and index blocking treat `Céline` and
  `Celine` alike.
- So the option now means "accents count for spelling, not for sound". With folding off, `Céline` and
  `Celine` share a key and score at least `IdenticalKeyFloor` (0.90), but below 1.0. With folding on
  they still score 1.0.
- The elided-article check counts kept accented letters as letters, so `d'Été` is handled the same way
  with and without folding.
- No preset sets `RemoveDiacritics` to false, so `Default`, `Names` and `Words` and their keys and
  scores are unchanged. Not breaking: the old behavior was a defect.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `text-normalization`: the output requirement allows accented Latin letters when folding is off, and
  "Accented letters are dropped…" is replaced by "Accented letters are kept…".
- `dutch-phonetic-encoding`: a new requirement that keys fold diacritics regardless of
  `RemoveDiacritics`.

## Impact

- Code:
  - `src/NlFuzzy/Normalization/DutchTextNormalizer.cs`: the letter filter, NFC composition when not
    folding, and the elided-article letter count.
  - `src/NlFuzzy/Phonetics/DutchPhoneticEncoder.cs`: fold the normalized token before encoding.
- Public API: none. The XML doc of `RemoveDiacritics` describes the new meaning.
- Tests:
  - normalizer tests replace the `ren e` assertion;
  - new encoder and scorer tests;
  - the property tests stop skipping `RemoveDiacritics = false` for canonical equivalence.
- Docs: a CHANGELOG entry under "Opgelost".
- The scorer, the index and the evaluation set are unaffected: every preset folds.
