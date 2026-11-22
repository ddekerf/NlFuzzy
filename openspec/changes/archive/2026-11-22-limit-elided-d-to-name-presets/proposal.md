# Proposal

## Why

`handle-elided-d-prefix` reads `d'` as the particle `de` in every preset. In the `Words` preset,
which is meant for ordinary words rather than names, that also rewrites `d'accord` to `de accord`.
The elided article only matters for surnames such as `D'Hondt`, so the rule belongs with the other
name-particle handling.

## What Changes

- The `d'` → `de` rewrite only applies when `StripNamePrefixes` is true: the `Default` and `Names`
  presets. With `StripNamePrefixes` false, including the `Words` preset, the apostrophe is a plain
  word separator again.
- `D'Hondt` still matches `De Hondt` in `Default` and `Names`; the glued `dh` rule is unchanged.
- **BREAKING (phonetic keys, pre-release)**: keys for `d'` input in presets with `StripNamePrefixes`
  false return to their 1.0.0 values. Nothing has been published to nuget.org, so no persisted keys
  are affected.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `name-prefix-normalization`: "Elided article d' is read as the particle de" is limited to
  options with `StripNamePrefixes` true, with a new scenario for the `Words` preset.

## Impact

- Code: `src/NlFuzzy/Normalization/DutchTextNormalizer.cs` (one condition).
- Tests: `DutchTextNormalizerTests` (the `Plain` options case and a new `Words` case).
- Docs: the existing `[Unreleased]` CHANGELOG entry for `d'` is corrected; README §4 is unaffected
  (it uses the `Default` preset).
- Public API: no changes.
