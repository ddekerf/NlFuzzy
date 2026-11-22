# Spec Delta

## MODIFIED Requirements

### Requirement: Every variant category is covered
Each match category SHALL hold at least 15 pairs. The match categories SHALL cover at least the
spelling variants documented in README §4, separated and glued name particles, the elided article,
token order, diacritics and ligatures, the `-se`/`-sen` patronymic swap, first-name variants
and single-character typos. The typo category SHALL include adjacent swaps, substitutions, missing
letters and extra letters, with at least 20 pairs that are not adjacent swaps.

#### Scenario: Category coverage
- **WHEN** the dataset integrity test groups pairs by category
- **THEN** every listed match category and every no-match category has at least 15 pairs

#### Scenario: Typo kinds
- **WHEN** the dataset integrity test checks the `typo` pairs
- **THEN** at least 20 of them are not a single adjacent swap of the other side

### Requirement: No-match pairs cover the known confusions
The no-match categories SHALL cover at least: a different first sound, a different final sound or
plural, the same prefix with a different stem, short names that differ in one sound, a different vowel
class, a derived name (`Bos`/`Bosman`), unrelated names, and distinct names that differ by one
adjacent swap (`Born`/`Bron`). At least 5 of the swap pairs SHALL have five or more letters.

#### Scenario: Derived names are no-match
- **WHEN** a Wikidata variant link connects a name to a derived name such as `Bos`/`Bosman`
- **THEN** the pair is labeled `nomatch` in the derived-name category, or left out, never labeled `match`

#### Scenario: Swapped names are no-match
- **WHEN** the dataset integrity test checks the `nm-transposition` pairs
- **THEN** every pair is a single adjacent swap, labeled `nomatch`, and at least 5 pairs have five or more letters
