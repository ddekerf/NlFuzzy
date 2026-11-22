# Spec Delta

## ADDED Requirements

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
