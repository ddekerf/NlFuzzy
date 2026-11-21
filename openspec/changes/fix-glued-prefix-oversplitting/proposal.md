# Proposal

## Why

With `SplitGluedNamePrefixes` enabled (the `Names` preset, which is the recommended preset for person
names), the glued-prefix splitter treats the start of ordinary names as a Flemish particle:
`Dekker` becomes `kker de`, `Vanessa` becomes `essa van`, `Dennis` becomes `nis den`, and `Verena`,
`Delia` and `Tenzing` are mangled the same way. That produces nonsense phonetic keys (`Dekker` and
`Decker` both encode to `K2R T2`), and it breaks matching as soon as those names appear next to other
tokens. Keys are meant to be persisted, so this has to be fixed before the first nuget.org release,
while changing keys is still free.

## What Changes

- The glued-prefix splitter only splits when the remaining stem can plausibly start a Dutch word: a
  stem that begins with a doubled consonant (`kk`, `nn`, `ss`, ...) or with `ck` is never split off.
- A curated list of high-frequency first names and place names that begin with a glued-prefix
  spelling (`Dennis`, `Denise`, `Delia`, `Vanessa`, `Verena`, `Veronique`, `Tenzing`, `Deventer`,
  `Dendermonde`, `Tervuren`, ...) is never split.
- Genuine glued Flemish surnames keep splitting exactly as today (`Vandenbroecke`, `Desmet`,
  `Devos`, `Debruyne`, `Verbrugghe`, `Vanhee`).
- **BREAKING (phonetic keys)**: keys produced with `SplitGluedNamePrefixes = true` change for every
  input that was split wrongly before (for example, `Dekker` no longer encodes to `K2R T2`). Keys from presets
  without glued-prefix splitting (`Default`, `Words`) are unchanged.
- The golden dataset gains cases that pin both the guard (no split) and the existing splits.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `name-prefix-normalization`: the glued-prefix splitting requirement gains guards, plus two new
  requirements: no split off an implausible stem, and a never-split list.

## Impact

- Code: `src/NlFuzzy/Normalization/DutchAffixes.cs` (split guard and exception list).
- Tests: `DutchAffixesTests`, `DutchTextNormalizerTests`, the golden CSV files and possibly
  `ReadmeExamplesTests` if a documented score moves.
- Docs: `CHANGELOG.md` under `[Unreleased]` with a phonetic-key **BREAKING** note; README §5 if the
  `Names` preset table changes.
- Public API: no signature changes.
