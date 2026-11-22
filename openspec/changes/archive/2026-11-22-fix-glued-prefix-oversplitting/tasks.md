# Tasks

## 1. Phonotactic guard

- [x] 1.1 Add `DutchAffixesTests` cases asserting that `dekker`, `decker`, `dekkers` and `dennis` are not split via `de`, and that `devos`, `dewit`, `desmet`, `vanhee`, `verbrugghe` and `vandenbroecke` still split; confirm the new no-split cases fail on the current code
  - `dennis` is split as `den` + `nis` (not via `de`), which the guard cannot reject; it is covered by the never-split list in 2.1–2.2 instead. Extra cases: `vanniekerk` still splits, and `vandellen` falls back from `vande` to `van`.
- [x] 1.2 In `DutchAffixes.SplitToken`, reject a candidate prefix when the stem starts with a doubled consonant or `ck`, and fall through to shorter prefixes; verify the 1.1 tests pass
- [x] 1.3 Update the `SplitGluedPrefixes` XML doc to describe the guard; verify `dotnet build -c Release` has no warnings on all three TFMs

## 2. Never-split list

- [x] 2.1 Add a parametrised test asserting that `vanessa`, `dennis`, `denise`, `delia`, `verena`, `veronique`, `tenzing`, `deventer`, `dendermonde` and `tervuren` are returned unsplit by `SplitGluedPrefixes`, and by `DutchTextNormalizer` with the `Names` preset
- [x] 2.2 Add the internal never-split set to `DutchAffixes`, seeded from frequent NL/BE first names and municipalities that start with a glued-prefix spelling, and check it in `SplitToken`; verify the 2.1 tests pass
- [x] 2.3 Record in PROVENANCE.md that the list was compiled by hand from public name and place facts; verify the section renders in the Markdown preview

## 3. Golden dataset and docs

- [x] 3.1 Add positive pairs `Dekker,Decker`, `Dennis,Denis` and `Vanessa,Vanesa`, and the negative pair `Dekker,Bakker`, to the golden CSVs; verify `GoldenDatasetTests` passes with every existing pair still green
  - `Vanesa` (single s) was added to the never-split list too; without it `Vanessa`/`Vanesa` would still fail because `vanesa` splits as `van` + `esa`.
- [x] 3.2 Re-run `ReadmeExamplesTests` and update any README §5 score that moved, keeping text and test in sync; verify the full `dotnet test -c Release` run is green
- [x] 3.3 Add a `[Unreleased]` CHANGELOG entry under "Opgelost" with a phonetic-key **BREAKING** note for `SplitGluedNamePrefixes = true`; verify the entry names the affected preset (`Names`)

## 4. Integration check

- [x] 4.1 Re-run the unseen-name probe from the review (Dekker, Vanessa, Dennis, Verena, Delia, Tenzing) and confirm none is split and the glued Flemish surnames still match their separated forms
