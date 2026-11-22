# Spec Delta

## MODIFIED Requirements

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
