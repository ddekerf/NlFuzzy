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

### Requirement: A single adjacent transposition raises the score to a floor
When `TranspositionFloor` is greater than 0 and the two normalized texts are identical except for one
pair of adjacent characters that appear in swapped order, where neither character is a space and the
token that contains them has at least five characters, the score SHALL be at least
`TranspositionFloor`. Otherwise the floor SHALL have no effect. The default is 0; the `Names` preset
uses 0.85.

#### Scenario: Swap inside a name
- **WHEN** `Kuipers` and `Kuipres` are compared with the `Names` preset
- **THEN** the similarity is at least 0.85

#### Scenario: Swap at the end of a name
- **WHEN** `Hoekstra` and `Hoekstar` are compared with the `Names` preset
- **THEN** the similarity is at least 0.85, despite the different final sound

#### Scenario: Swap inside one token of a full name
- **WHEN** `Jan Kuipers` and `Jan Kuipres` are compared with the `Names` preset
- **THEN** the similarity is at least 0.85

#### Scenario: Short tokens are left alone
- **WHEN** `Born` and `Bron`, and `Jan Smit` and `Jan Smti`, are compared with the `Names` preset
- **THEN** both scores equal the scores computed with `TranspositionFloor` set to 0

#### Scenario: Only exactly one swap counts
- **WHEN** `Kuipers` is compared with `Kuirpes` (two swaps) and with `Kuipars` (a substitution) using the `Names` preset
- **THEN** both scores equal the scores computed with `TranspositionFloor` set to 0

#### Scenario: Off by default
- **WHEN** `Kuipers` and `Kuipres` are compared with the `Default` preset
- **THEN** the similarity is the same as before this option existed

#### Scenario: Same-sounding spellings still rank higher
- **WHEN** `Meijer` is ranked against the candidates `Meier` and `Mejier` with `FuzzyMatchDutch` and the `Names` preset
- **THEN** `Meier` ranks above `Mejier`

### Requirement: Score contract
The score SHALL always be a finite number in [0, 1], symmetric in its arguments, and reflexive
(1.0 for a value compared with itself). It SHALL throw `ArgumentNullException` for null input.

#### Scenario: Property check
- **WHEN** the seeded property tests score random inputs, including very long ones
- **THEN** every score is finite, in [0, 1], symmetric, and 1.0 for identical input

### Requirement: The Names preset passes the golden dataset
With the `Names` preset, every positive pair in the must-pass core golden dataset SHALL either share a
phonetic key or score at least 0.85, and every negative pair SHALL have different keys and score below
0.75. The larger evaluation dataset is measured against regression floors instead, as specified in
`matching-quality-evaluation`.

#### Scenario: Positive pairs
- **WHEN** `Vandenbroecke`/`Van den Broecke`, `Claes`/`Klaas` and `vrede`/`wrede` are compared with the `Names` preset
- **THEN** each pair shares a key or scores at least 0.85

#### Scenario: Negative pairs
- **WHEN** `Jansen`/`Hansen`, `Maes`/`Moes` and `Kuipers`/`Kuiper` are compared with the `Names` preset
- **THEN** each pair has different keys and scores below 0.75

### Requirement: Token order does not affect similarity when enabled
When the `IgnoreTokenOrder` option is true, the similarity score SHALL treat inputs that contain the
same tokens in a different order as a near-perfect match. A reordered but otherwise identical pair
SHALL score at least 0.95.

#### Scenario: Surname-first with a comma
- **WHEN** `Jan Bakker` and `Bakker, Jan` are compared with the `Names` preset
- **THEN** the similarity is at least 0.95

#### Scenario: Reordered name with a particle
- **WHEN** `Piet de Vries` and `de Vries, Piet` are compared with the `Default` preset
- **THEN** the similarity is at least 0.95

#### Scenario: Reordering combined with a spelling variant
- **WHEN** `Jan Kuipers` and `Cuypers, Jan` are compared with the `Names` preset
- **THEN** the similarity is at or above the default threshold of 0.82

### Requirement: Identical input ranks above reordered input
Identical normalized input SHALL score exactly 1.0, and a reordered input SHALL score strictly below
1.0, so ranking prefers the exact order when both are present.

#### Scenario: Ranking with an exact and a reordered candidate
- **WHEN** `Jan Bakker` is ranked against the candidates `Bakker Jan` and `Jan Bakker` with `FuzzyMatchDutch`
- **THEN** `Jan Bakker` is the first result with score 1.0 and `Bakker Jan` follows with a lower score

### Requirement: Token-order tolerance can be disabled
When `IgnoreTokenOrder` is false, the score SHALL be computed on the inputs in their original token
order only. The `Words` preset SHALL default to false; the `Default` and `Names` presets SHALL
default to true.

#### Scenario: Words preset keeps order
- **WHEN** `Jan Bakker` and `Bakker Jan` are compared with the `Words` preset
- **THEN** the score equals the score produced without token-order tolerance

### Requirement: Token-order tolerance never lowers a score
For any pair of inputs, the score with `IgnoreTokenOrder` true SHALL be greater than or equal to the
score with it false. Scores SHALL remain finite, symmetric and within [0, 1].

#### Scenario: Single-token inputs are unaffected
- **WHEN** `Meijer` and `Meier` are compared with `IgnoreTokenOrder` true and with it false
- **THEN** both scores are identical

#### Scenario: Property check
- **WHEN** the seeded property tests compare random multi-token inputs with both settings
- **THEN** every score is in [0, 1], symmetric, and the enabled score is never lower than the disabled score

### Requirement: Phonetic keys stay order-sensitive
Enabling token-order tolerance SHALL NOT change `PhoneticKey`, `PhoneticKeys`, `SoundsLike` or the
phonetic equality comparer.

#### Scenario: Key unchanged
- **WHEN** `PhoneticKey` is computed for `Bakker, Jan` with `IgnoreTokenOrder` true and with it false
- **THEN** both keys are identical
