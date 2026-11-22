# Spec Delta

## REMOVED Requirements

### Requirement: An apostrophe after d is only a separator
**Reason**: Leaving a lone `d` made `D'Hondt` score 0.28 against `De Hondt`. The elided article is now
read as the particle `de`.
**Migration**: Recompute persisted phonetic keys for values that contain `d'` before a name. The new
behaviour is described by "Elided article d' is read as the particle de".

## ADDED Requirements

### Requirement: Elided article d' is read as the particle de
In every preset, the normalizer SHALL treat a `d` at the start of a token, followed directly by an
apostrophe character (`'`, `’`, `‘`, `` ` `` or `´`) and then by a name of at least three letters, as the
particle `de`.

#### Scenario: Apostrophe form matches the separated form
- **WHEN** `D'Hondt` and `De Hondt` are compared with the `Names` preset
- **THEN** both produce the same phonetic key and the similarity is 1.0

#### Scenario: Typographic apostrophe
- **WHEN** `D’Haene` (right single quotation mark) and `De Haene` are compared with the `Default` preset
- **THEN** both produce the same phonetic key

#### Scenario: Elision inside a full name
- **WHEN** `Marie D'Hooghe` and `Marie De Hooghe` are compared with the `Names` preset
- **THEN** both produce the same phonetic key

### Requirement: An initial d is not read as an article
The normalizer SHALL NOT treat a `d` as the particle `de` when it is not directly followed by an
apostrophe, or when fewer than three letters follow the apostrophe.

#### Scenario: Initial with a period
- **WHEN** `D. Jansen` is normalized with the `Names` preset
- **THEN** the result does not contain the particle `de`

#### Scenario: Colloquial short form
- **WHEN** `d'r` is normalized with the `Words` preset
- **THEN** the result does not contain the particle `de`

### Requirement: Glued dh spelling is split when glued-prefix splitting is enabled
When `SplitGluedNamePrefixes` is true, the normalizer SHALL split a token that starts with `dh`
followed by a vowel, and is at least five letters long, into the particle `de` and a stem starting
with `h`.

#### Scenario: Apostrophe-less spelling matches the separated form
- **WHEN** `Dhondt` and `De Hondt` are compared with the `Names` preset
- **THEN** both produce the same phonetic key and the similarity is 1.0

#### Scenario: All three spellings agree
- **WHEN** `D'Haene`, `Dhaene` and `De Haene` are encoded with the `Names` preset
- **THEN** all three phonetic keys are identical

#### Scenario: Splitting is opt-in
- **WHEN** `Dhondt` is normalized with the `Default` preset
- **THEN** the token is not split
