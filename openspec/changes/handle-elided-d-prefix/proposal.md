# Proposal

## Why

Flemish surnames with an elided article (`D'Hondt`, `D'Haene`, `D'Hooghe`) and their apostrophe-less
spellings (`Dhondt`, `Dhaene`, `Dhooghe`) are common, and they are variants of `De Hondt`,
`De Haene` and `De Hooghe`. Today the apostrophe is replaced with a space, leaving a lone `d` that is
not recognised as a particle: `D'Hondt` vs `De Hondt` scores 0.28 and `Dhondt` vs `De Hondt`
scores 0.35 with the `Names` preset. The fix changes phonetic keys, so it belongs before the first
nuget.org release.

## What Changes

- An elided article `d'` (any common apostrophe character) in front of a name is treated as the
  particle `de`, in every preset: `D'Hondt` normalizes like `De Hondt`.
- A lone `d` that is not followed by an apostrophe, such as an initial in `D. Jansen`, is left alone.
- With `SplitGluedNamePrefixes` enabled, a token that starts with `dh` followed by a vowel is split
  as `de h…`: `Dhondt` normalizes like `De Hondt`.
- **BREAKING (phonetic keys)**: keys change for every input that contains `d'` before a name, in
  all presets, and for `dh…` names in presets with glued-prefix splitting.
- The golden dataset gains `D'Hondt`/`De Hondt`, `Dhondt`/`De Hondt` and `D'Haene`/`Dhaene`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `name-prefix-normalization`: removes "An apostrophe after d is only a separator" and adds the
  elided-article, initial-`d` and glued-`dh` requirements. `fix-glued-prefix-oversplitting` modifies
  other requirements in the same capability.

## Impact

- Code: `src/NlFuzzy/Normalization/DutchTextNormalizer.cs` (elision before punctuation is stripped)
  and `src/NlFuzzy/Normalization/DutchAffixes.cs` (the `dh` glued form).
- Tests: `DutchTextNormalizerTests` currently pins `D'Hondt` → `d hondt` and must change; golden CSVs.
- Docs: CHANGELOG `[Unreleased]` with a phonetic-key **BREAKING** note; README §4 variant table.
- Public API: no signature changes.
