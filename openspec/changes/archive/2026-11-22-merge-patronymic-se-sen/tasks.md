# Tasks

## 1. Option and encoder rule

- [x] 1.1 Add `DutchPhoneticEncoderTests` for the spec scenarios (`Willemsen`/`Willemse` and `Pietersen`/`Pieterse` share a key with `Names`; `Willems` differs; `Hansen`/`Hanzen` share a key; `Assen`/`Asse` differ; `Default` keeps today's key for `Willemsen`); confirm the sharing cases fail on the current code
- [x] 1.2 Add `MergeSeSenEndings` (default false) to `DutchFuzzyOptions`, set it to true in the `Names` preset, and add `DutchFuzzyOptionsTests` and README preset-table assertions for all three presets; update `PublicApi.approved.txt` and verify `PublicApiTests` passes
- [x] 1.3 In `DutchPhoneticEncoder.EncodeToken`, encode `token[..^1]` when the option is on, the token has at least five letters (six in the original plan; see design.md §3) and ends in `sen` or `zen`; verify the 1.1 tests pass and the core golden dataset is unchanged

## 2. Measure and raise the baseline

- [x] 2.1 Run `EvaluationTests` with the detailed logger; verify the floor test still passes, `patronymic-se-sen` rises on both splits, and any other loss stays within the floor tolerance and is explained here (record the before/after counts in this task)
  - Floor test passes. Changed categories against the baseline: `patronymic-se-sen` tune 0 → 22 (of 22) and holdout 0 → 3 (of 3); `nm-vowel` tune 22 → 23 (`Janssen`/`Jenssen` now correctly rejected, 0.828 → 0.813); `surname-variant` tune 19 → 18 (`Stienissen`/`Stinnissen`, 0.827 → 0.811, see design.md risks). Overall holdout: precision 0.936 → 0.937, recall 0.874 → 0.892, F1 0.904 → 0.914; tune F1 0.920 → 0.946. With the original six-letter minimum, `Gijsen`/`Gysen` (ij-ei-y) also broke (0.900 → 0.594); lowering it to five fixed that (design.md §3).
- [x] 2.2 Rewrite `evaluation-baseline.json` with `NLFUZZY_WRITE_BASELINE="<reason>"` and commit only that file, with the reason and the new holdout numbers in the message; verify the floor test passes against the new baseline

## 3. Docs

- [x] 3.1 README: add a `-sen / -se` row to the §4 variant table (noting it is `Names` only), a `MergeSeSenEndings` column to the §5 preset table, and the new numbers in the "Kwaliteit" section, with matching `ReadmeExamplesTests`; verify they pass
  - README §5 quoted exact scores for `Jansen`/`Hansen` that moved because both now end their key in `-se` (0.846/0.677 → 0.831/0.665; still a no-match, different first sound); text and test updated.
- [x] 3.2 Add a `[Unreleased]` CHANGELOG entry under "Gewijzigd" with a **BREAKING** note for `Names` keys of `-sen`/`-zen` tokens; verify it names the option and the preset

## 4. Integration check

- [x] 4.1 Run the full `dotnet test -c Release` suite and build all three TFMs without warnings; confirm the core golden tests and every evaluation floor pass
