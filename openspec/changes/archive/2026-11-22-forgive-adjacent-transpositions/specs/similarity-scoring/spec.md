# Spec Delta

## ADDED Requirements

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
