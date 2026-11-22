# Tasks

## 1. Limit the rule

- [x] 1.1 Add a `DutchTextNormalizerTests` case that `d'accord` normalizes to `d accord` with the `Words` preset, and change the `Plain`-options `D'Hondt` cases back to `d hondt`; confirm they fail on the current code
- [x] 1.2 Apply `ExpandElidedArticles` in `DutchTextNormalizer.Normalize` only when `StripNamePrefixes` is true, and update its XML doc; verify the 1.1 tests and the existing `d'` tests for `Default` and `Names` pass

## 2. Docs and check

- [x] 2.1 Correct the `[Unreleased]` CHANGELOG entry for `d'`: the rule applies with `StripNamePrefixes` (presets `Default` and `Names`), not in every preset; verify the BREAKING note names only those presets for `d'`
- [x] 2.2 Run the full `dotnet test -c Release` suite including the golden dataset and README examples, and build all three TFMs without warnings
