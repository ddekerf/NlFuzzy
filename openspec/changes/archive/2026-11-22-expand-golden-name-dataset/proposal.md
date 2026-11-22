# Proposal

## Why

The golden dataset has 52 pairs (42 match, 10 no-match), and the scoring weights were tuned until all
of them passed. Every score we quote is therefore measured on the data it was fitted to. The review
probe of 69 unseen pairs already showed the gap: on that set the `Names` preset gets 38/44 matches and
19/25 non-matches, with whole categories (the `-se`/`-sen` patronymic endings) failing. Before we
retune anything, we need a dataset large enough to measure per category, and a held-out part that
tuning never sees.

## What Changes

- New evaluation dataset of about **1,000 labeled name pairs** (about 600 match, 400 no-match). Each
  pair records its label, a variant category, its source and a fixed split.
- Two sources:
  - **hand-curated pairs per category**: the spelling variants in README §4, name particles, word
    order, diacritics, the `-se`/`-sen` patronymic swap, first names, typos, and the matching no-match categories;
  - **Wikidata surname variant links** ("said to be the same as", P460, between Dutch family names),
    which are CC0. The 350 raw pairs are a mix of true spelling variants, derived names
    (`Bos`/`Bosman`) and unlabeled items, so every one is labeled by hand before it is used.
- A deterministic **held-out split** (about 30%), assigned by a stable hash, so later tuning can be
  checked on pairs it was not tuned on.
- **Metrics per category and overall** at the `Names` preset and its default threshold: recall for
  match categories, true-negative rate for no-match categories, plus overall precision, recall and F1.
- **Regression floors**: the current baseline is recorded, and tests fail when any category or the
  overall numbers drop below it. Known misses do not fail the build.
- The existing 52 pairs stay a **must-pass core**, unchanged.
- **No retuning**: weights, rules and thresholds stay exactly as they are. Improving the weak
  categories is a follow-up change, validated against the held-out split.

## Capabilities

### New Capabilities

- `matching-quality-evaluation`: the labeled evaluation dataset (format, categories, sources,
  provenance, split), the metrics computed over it, and the regression floors that guard them.

### Modified Capabilities

- `similarity-scoring`: "The Names preset passes the golden dataset" is reworded to refer to the
  must-pass **core** golden dataset, and to point to `matching-quality-evaluation` for the larger set.

## Impact

- Test data: new `tests/NlFuzzy.Tests/Golden/evaluation-pairs.csv`, a committed Wikidata snapshot with
  its query, and a baseline file with the recorded floors.
- Tests: a loader with dataset-integrity checks, a metrics calculator, per-category floor tests, and a
  readable report in the test output.
- Docs: a README section with the headline quality numbers; PROVENANCE.md and THIRD-PARTY-NOTICES.md
  record the Wikidata source and its CC0 licence.
- Library code, public API and phonetic keys: **unchanged**.
- CI time: about 1,000 extra pairs are scored once each, which takes well under a second.
