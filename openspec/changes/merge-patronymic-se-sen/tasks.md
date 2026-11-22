# Tasks

## 1. Option and encoder rule

- [ ] 1.1 Add `DutchPhoneticEncoderTests` for the spec scenarios (`Willemsen`/`Willemse` and `Pietersen`/`Pieterse` share a key with `Names`; `Willems` differs; `Hansen`/`Hanzen` share a key; `Assen`/`Asse` differ; `Default` keeps today's key for `Willemsen`); confirm the sharing cases fail on the current code
- [ ] 1.2 Add `MergeSeSenEndings` (default false) to `DutchFuzzyOptions`, set it to true in the `Names` preset, and add `DutchFuzzyOptionsTests` and README preset-table assertions for all three presets; update `PublicApi.approved.txt` and verify `PublicApiTests` passes
- [ ] 1.3 In `DutchPhoneticEncoder.EncodeToken`, encode `token[..^1]` when the option is on, the token has at least six letters and ends in `sen` or `zen`; verify the 1.1 tests pass and the core golden dataset is unchanged

## 2. Measure and raise the baseline

- [ ] 2.1 Run `EvaluationTests` with the detailed logger; verify the floor test still passes, `patronymic-se-sen` rises on both splits, and no other category loses a pass (record the before/after counts in this task)
- [ ] 2.2 Rewrite `evaluation-baseline.json` with `NLFUZZY_WRITE_BASELINE="<reason>"` and commit only that file, with the reason and the new holdout numbers in the message; verify the floor test passes against the new baseline

## 3. Docs

- [ ] 3.1 README: add a `-sen / -se` row to the §4 variant table (noting it is `Names` only), a `MergeSeSenEndings` column to the §5 preset table, and the new numbers in the "Kwaliteit" section, with matching `ReadmeExamplesTests`; verify they pass
- [ ] 3.2 Add a `[Unreleased]` CHANGELOG entry under "Gewijzigd" with a **BREAKING** note for `Names` keys of `-sen`/`-zen` tokens; verify it names the option and the preset

## 4. Integration check

- [ ] 4.1 Run the full `dotnet test -c Release` suite and build all three TFMs without warnings; confirm the core golden tests and every evaluation floor pass
