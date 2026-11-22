# Spec Delta

## ADDED Requirements

### Requirement: Patronymic -sen is encoded like -se when enabled
When `MergeSeSenEndings` is true, the encoder SHALL encode a token of at least six letters that ends in
`sen` or `zen` as if its final `n` were absent, so the `-sen` and `-se` forms of a patronymic share a
key. Shorter tokens, and tokens with other endings, SHALL be encoded unchanged.

#### Scenario: Patronymic forms share a key
- **WHEN** `Willemsen` and `Willemse`, and `Pietersen` and `Pieterse`, are encoded with the `Names` preset
- **THEN** each pair produces the same phonetic key

#### Scenario: The -s form stays different
- **WHEN** `Willems`, `Willemse` and `Willemsen` are encoded with the `Names` preset
- **THEN** `Willems` has a different key from the other two

#### Scenario: z and s spellings still agree
- **WHEN** `Hansen` and `Hanzen` are encoded with the `Names` preset
- **THEN** both produce the same phonetic key

#### Scenario: Short tokens are left alone
- **WHEN** `Assen` and `Asse` are encoded with the `Names` preset
- **THEN** their keys differ

#### Scenario: Off by default
- **WHEN** `Willemsen` and `Willemse` are encoded with the `Default` preset
- **THEN** `Willemsen` keeps the same key it has today
