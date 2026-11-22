# Spec Delta

## MODIFIED Requirements

### Requirement: Elided article d' is read as the particle de
When `StripNamePrefixes` is true (the `Default` and `Names` presets), the normalizer SHALL treat a
`d` at the start of a token, followed directly by an apostrophe character (`'`, `’`, `‘`, `` ` `` or
`´`) and then by a name of at least three letters, as the particle `de`. When `StripNamePrefixes` is
false, the apostrophe SHALL remain a plain word separator.

#### Scenario: Apostrophe form matches the separated form
- **WHEN** `D'Hondt` and `De Hondt` are compared with the `Names` preset
- **THEN** both produce the same phonetic key and the similarity is 1.0

#### Scenario: Typographic apostrophe
- **WHEN** `D’Haene` (right single quotation mark) and `De Haene` are compared with the `Default` preset
- **THEN** both produce the same phonetic key

#### Scenario: Elision inside a full name
- **WHEN** `Marie D'Hooghe` and `Marie De Hooghe` are compared with the `Names` preset
- **THEN** both produce the same phonetic key

#### Scenario: Ordinary words keep the apostrophe as a separator
- **WHEN** `d'accord` is normalized with the `Words` preset
- **THEN** the result is `d accord`
