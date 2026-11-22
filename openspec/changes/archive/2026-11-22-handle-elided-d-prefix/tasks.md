# Tasks

## 1. Elided d'

- [x] 1.1 Change the `D'Hondt` → `d hondt` case in `DutchTextNormalizerTests` to expect `hondt de` (prefixes moved), and add cases for `D’Haene`, `Marie D'Hooghe`, `D. Jansen` (no `de`) and `d'r` with the `Words` preset (no `de`); confirm the new expectations fail on the current code
- [x] 1.2 Add the elision scan to `DutchTextNormalizer` between `FoldForPhonetics` and `KeepLettersAndSpaces`, covering the five apostrophe characters, a token-start check and the three-letter minimum; verify the 1.1 tests pass
- [x] 1.3 Add a `PropertyTests` case checking that normalization stays idempotent for random inputs containing `d'`; verify the property run is green

## 2. Glued dh

- [x] 2.1 Add `DutchAffixesTests` cases: `dhondt` → `de hondt` and `dhaene` → `de haene` when splitting is on, `dhondt` unchanged with the `Default` preset, `dhr` unchanged (no vowel); confirm they fail first
- [x] 2.2 Add the `dh` + vowel rule (five or more letters) to `DutchAffixes.SplitToken` after the regular prefixes, respecting the never-split list if `fix-glued-prefix-oversplitting` has landed; verify the 2.1 tests pass

## 3. Golden dataset and docs

- [x] 3.1 Add positive pairs `D'Hondt,De Hondt`, `Dhondt,De Hondt` and `D'Haene,Dhaene` to `positive-pairs.csv`; verify `GoldenDatasetTests` is green with every existing pair still passing
- [x] 3.2 Add a `d' / de` row to the README §4 variant table, with a matching `ReadmeExamplesTests` case; verify it passes
- [x] 3.3 Add a `[Unreleased]` CHANGELOG entry under "Opgelost" with a phonetic-key **BREAKING** note (all presets for `d'`, splitting presets for `dh`); verify the entry is present

## 4. Integration check

- [x] 4.1 Run the full `dotnet test -c Release` suite and re-run the review probe for `D'Hondt`/`De Hondt` and `Dhondt`/`De Hondt`; confirm both now score 1.0 with the `Names` preset
