# similarity-scoring Specification

## Purpose

Defines how two strings are turned into one similarity score in [0, 1], and which differences
(spelling, pronunciation, particle placement) the score tolerates.

## Requirements

### Requirement: Score is a weighted mix of three components
The score SHALL be the weighted average of an orthographic component (Jaro-Winkler on the normalized
text), a phonetic component (one minus the normalized Levenshtein distance between the phonetic keys)
and an n-gram component (bigram Dice on the normalized text), using `OrthographicWeight`,
`PhoneticWeight` and `NGramWeight`.

#### Scenario: Spelling variant
- **WHEN** `Schoenmakers` and `Schoonmakers` are compared with the `Default` preset
- **THEN** the similarity is 0.936 (to three decimals)

#### Scenario: Weights matter
- **WHEN** the same pair is scored with two option sets that differ only in their weights
- **THEN** the scores differ

### Requirement: Identical normalized input scores 1.0
When both values normalize to the same text, the score SHALL be 1.0. Two values that both normalize to
an empty string SHALL score 1.0, and a pair where only one side normalizes to an empty string SHALL
score 0.0.

#### Scenario: Case, accents and particle placement
- **WHEN** `Céline`/`celine` and `Van den Broecke`/`van den broecke` are compared with the `Default` preset
- **THEN** both similarities are 1.0

#### Scenario: One side empty
- **WHEN** an empty string and `kuipers` are compared
- **THEN** the similarity is 0.0

### Requirement: Identical keys raise the score to a floor
When both phonetic keys are equal and non-empty, the score SHALL be at least `IdenticalKeyFloor`
(default 0.90).

#### Scenario: Same-sounding names
- **WHEN** `Meijer` and `Meier` are compared with the `Default` preset
- **THEN** the similarity is at least 0.90

#### Scenario: Custom floor
- **WHEN** `Meijer` and `Meier` are compared with `IdenticalKeyFloor` set to 0.99
- **THEN** the similarity is 0.99

### Requirement: Differing first or last sounds are penalised
When the phonetic keys differ, the score SHALL be multiplied by one minus
`InitialPhonemeMismatchPenalty` when their first characters differ, and by one minus
`FinalPhonemeMismatchPenalty` when their last characters differ. Both penalties default to 0.20.

#### Scenario: Different initial sound
- **WHEN** `Jansen` and `Hansen` are compared with the `Default` preset, and again with both penalties set to 0
- **THEN** the first score is lower than the second and below the default threshold of 0.82

#### Scenario: Singular and plural
- **WHEN** `Kuipers` and `Kuiper` are compared with the `Default` preset, and again with both penalties set to 0
- **THEN** the first score is lower than the second and below the default threshold of 0.82

### Requirement: Score contract
The score SHALL always be a finite number in [0, 1], symmetric in its arguments, and reflexive
(1.0 for a value compared with itself). It SHALL throw `ArgumentNullException` for null input.

#### Scenario: Property check
- **WHEN** the seeded property tests score random inputs, including very long ones
- **THEN** every score is finite, in [0, 1], symmetric, and 1.0 for identical input

### Requirement: The Names preset passes the golden dataset
With the `Names` preset, every positive pair in the golden dataset SHALL either share a phonetic key or
score at least 0.85, and every negative pair SHALL have different keys and score below 0.75.

#### Scenario: Positive pairs
- **WHEN** `Vandenbroecke`/`Van den Broecke`, `Claes`/`Klaas` and `vrede`/`wrede` are compared with the `Names` preset
- **THEN** each pair shares a key or scores at least 0.85

#### Scenario: Negative pairs
- **WHEN** `Jansen`/`Hansen`, `Maes`/`Moes` and `Kuipers`/`Kuiper` are compared with the `Names` preset
- **THEN** each pair has different keys and scores below 0.75
