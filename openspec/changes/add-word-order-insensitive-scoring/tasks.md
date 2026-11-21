# Tasks

## 1. Option

- [ ] 1.1 Add `IgnoreTokenOrder` (default `true`) to `DutchFuzzyOptions`, set it to `false` in the `Words` preset, and add `DutchFuzzyOptionsTests` asserting the value for all three presets; verify the tests pass
- [ ] 1.2 Update `tests/NlFuzzy.Tests/Api/PublicApi.approved.txt` for the new property; verify `PublicApiTests` passes

## 2. Scoring

- [ ] 2.1 Add `DutchFuzzyScorerTests` for the spec scenarios (`Jan Bakker`/`Bakker, Jan` ≥ 0.95, `Piet de Vries`/`de Vries, Piet` ≥ 0.95, `Jan Kuipers`/`Cuypers, Jan` ≥ 0.82, `Words` preset unchanged, single-token pairs unchanged); confirm they fail on the current code
- [ ] 2.2 Extend `PreparedText` with the canonical normalized text and key, built in `Prepare` by sorting (normalized, key) token pairs by key token, only for multi-token values when the option is on; verify with a unit test on `Prepare` for `bakker jan` and `jan bakker`
- [ ] 2.3 Score max(ordered, canonical × 0.97) in `DutchFuzzyScorer.Score` when both sides have canonical forms; verify the 2.1 tests pass
- [ ] 2.4 Add an `ExtensionsTests` case where `FuzzyMatchDutch` ranks `Jan Bakker` (1.0) above `Bakker Jan` (< 1.0); verify it passes
- [ ] 2.5 Extend `PropertyTests` with multi-token inputs checking [0, 1], symmetry and "enabled ≥ disabled"; verify the property run is green

## 3. Index

- [ ] 3.1 Add `DutchFuzzyIndexTests` asserting that `Exact` mode equals `FuzzyMatchDutch` on reordered data, and that `BlockedOnly` finds `Bakker, Jan` for `Jan Bakker` among 1,000 unrelated names; confirm the `BlockedOnly` test fails first
- [ ] 3.2 Register multi-token items under their canonical key and look up the query's canonical key in `CollectCandidates` when the option is on; verify the 3.1 tests and the existing parity tests pass

## 4. Performance and docs

- [ ] 4.1 Run `dotnet run --project bench/NlFuzzy.Benchmarks -c Release -- --filter '*'` before and after; verify multi-token scoring regresses by at most 2× and single-token scoring is unchanged, and update README §9 with the new numbers
- [ ] 4.2 Add a README §3 example for "Achternaam, Voornaam" matching, with a matching `ReadmeExamplesTests` case, and a §6 note that keys and `SoundsLike` stay order-sensitive; verify `ReadmeExamplesTests` passes
- [ ] 4.3 Add a `[Unreleased]` CHANGELOG entry under "Gewijzigd" describing the scoring change and the opt-out; verify it names `IgnoreTokenOrder` and the affected presets

## 5. Integration check

- [ ] 5.1 Run the full `dotnet test -c Release` suite including the golden dataset, and re-run the review's name-order probe (`Jan Bakker`, `Piet de Vries`); confirm every pair scores ≥ 0.95 and no golden negative crosses the threshold
