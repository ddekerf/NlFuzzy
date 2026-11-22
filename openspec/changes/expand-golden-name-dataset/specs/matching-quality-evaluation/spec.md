# Spec Delta

## Purpose

Measures how well the library matches Dutch and Flemish names on a large, labeled set of name pairs
it was not tuned on, reports that quality per variant category, and fails the build when it regresses.

## ADDED Requirements

### Requirement: A labeled evaluation dataset of name pairs
The repository SHALL contain an evaluation dataset of at least 1,000 name pairs, of which at least 600
are labeled `match` and at least 400 `nomatch`. Every pair SHALL record its two names, its label, a
variant category, its source and its split.

#### Scenario: Dataset size and fields
- **WHEN** the dataset integrity test loads the evaluation dataset
- **THEN** it finds at least 600 `match` and 400 `nomatch` pairs, and every row has a known label, category, source and split

#### Scenario: Clean data
- **WHEN** the dataset integrity test checks for duplicates
- **THEN** no pair occurs twice in either order, and no pair is labeled both `match` and `nomatch`

### Requirement: Every variant category is covered
Each match category SHALL hold at least 15 pairs. The match categories SHALL cover at least the
spelling variants documented in README §4, separated and glued name particles, the elided article,
token order, diacritics and ligatures, the `-se`/`-sen` patronymic swap, first-name variants
and single-character typos.

#### Scenario: Category coverage
- **WHEN** the dataset integrity test groups pairs by category
- **THEN** every listed match category and every no-match category has at least 15 pairs

### Requirement: No-match pairs cover the known confusions
The no-match categories SHALL cover at least: a different first sound, a different final sound or
plural, the same prefix with a different stem, short names that differ in one sound, a different vowel
class, a derived name (`Bos`/`Bosman`) and unrelated names.

#### Scenario: Derived names are no-match
- **WHEN** a Wikidata variant link connects a name to a derived name such as `Bos`/`Bosman`
- **THEN** the pair is labeled `nomatch` in the derived-name category, or left out, never labeled `match`

### Requirement: Pairs have traceable, licence-compatible sources
Every pair SHALL be either hand-curated or taken from Wikidata, which is CC0. A Wikidata pair SHALL
record both item ids, and the repository SHALL contain the query and a dated snapshot of the raw
result. The dataset SHALL NOT contain combinations of first names and surnames that identify a real
private person.

#### Scenario: Wikidata provenance
- **WHEN** a row's source is Wikidata
- **THEN** it names both Wikidata item ids, and both ids occur in the committed snapshot

### Requirement: Labels do not follow the scorer
Labels SHALL reflect linguistic judgement about whether two spellings denote the same name, and SHALL
NOT be changed to match the scorer's output. A pair the scorer gets wrong stays in the dataset and
counts as a miss.

#### Scenario: Known miss
- **WHEN** the `Names` preset scores the matching pair `Willemsen`/`Willemse` below the threshold
- **THEN** the pair stays labeled `match` and is counted as a miss in its category

### Requirement: A fixed held-out split
About 30% of the pairs SHALL be in a `holdout` split and the rest in a `tune` split, assigned by a
stable hash of the normalized pair, so the assignment does not depend on file order and never changes
for an existing pair.

#### Scenario: Stable assignment
- **WHEN** the dataset integrity test recomputes the split of every row from its pair
- **THEN** it matches the recorded split, and the holdout share is between 25% and 35%

### Requirement: Quality is reported per category and overall
The evaluation SHALL score every pair with the `Names` preset at its default threshold and report, per
split: recall for each match category, true-negative rate for each no-match category, and overall
precision, recall and F1.

#### Scenario: Readable report
- **WHEN** the evaluation tests run
- **THEN** the test output contains a table with one row per category and an overall row, for both splits

### Requirement: Regressions below the baseline fail the build
The repository SHALL record a baseline of the per-category and overall metrics. The evaluation tests
SHALL fail when any of those metrics drops below its baseline by more than the tolerance of one pair
in that category, or 0.5 percentage points overall. The baseline SHALL only change in a commit that
states why.

#### Scenario: A category regresses
- **WHEN** a change makes two more pairs in one category fail than the baseline records
- **THEN** the evaluation test fails and names that category with its baseline and current value

#### Scenario: Known misses alone do not fail
- **WHEN** the scorer is unchanged and the baseline is current
- **THEN** the evaluation tests pass, even though some pairs are misses
