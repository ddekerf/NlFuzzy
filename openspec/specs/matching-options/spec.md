# matching-options Specification

## Purpose

Defines the immutable options that drive normalization, encoding and scoring, the three ready-made
presets, and how invalid configurations are rejected before they can produce silent wrong results.

## Requirements

### Requirement: Options are immutable values
`DutchFuzzyOptions` SHALL be an immutable record. Callers derive variants with `with` expressions,
which SHALL leave the original untouched, and two option instances with the same values SHALL be
equal.

#### Scenario: Deriving a variant
- **WHEN** `DutchFuzzyOptions.Names with { MaxKeyLength = 6 }` is created
- **THEN** the new instance has `MaxKeyLength` 6 and `DutchFuzzyOptions.Names` still has 0

#### Scenario: Value equality
- **WHEN** two instances are created with the same property values
- **THEN** they are equal and have the same hash code

### Requirement: Documented defaults
A new `DutchFuzzyOptions` SHALL default to: `Tolerant` profile, `RemoveDiacritics` true,
`StripNamePrefixes` true, `SplitGluedNamePrefixes` false, `EncodeVowels` false, `MaxKeyLength` 0
(unlimited), `IgnoreTokenOrder` true, weights 0.35 orthographic, 0.45 phonetic and 0.20 n-gram,
`IdenticalKeyFloor` 0.90, `DefaultThreshold` 0.82 and both phoneme penalties 0.20.

#### Scenario: Default preset
- **WHEN** `DutchFuzzyOptions.Default` is inspected
- **THEN** every property has the documented default value

### Requirement: Three presets
The library SHALL offer three singleton presets that differ only in their encoding and ordering flags:
`Default` (Tolerant, vowels dropped, particles moved, no glued splitting, token order ignored),
`Names` (Tolerant, vowel classes, particles moved, glued splitting, token order ignored) and `Words`
(Strict, vowel classes, no particle handling, token order respected).

#### Scenario: Names preset
- **WHEN** `DutchFuzzyOptions.Names` is inspected
- **THEN** it uses the `Tolerant` profile with `EncodeVowels`, `StripNamePrefixes`, `SplitGluedNamePrefixes` and `IgnoreTokenOrder` all true

#### Scenario: Words preset
- **WHEN** `DutchFuzzyOptions.Words` is inspected
- **THEN** it uses the `Strict` profile with `EncodeVowels` true and `StripNamePrefixes`, `SplitGluedNamePrefixes` and `IgnoreTokenOrder` all false

#### Scenario: Singletons
- **WHEN** a preset property is read twice
- **THEN** both reads return the same instance

### Requirement: Invalid options are rejected
`Validate` SHALL throw `InvalidOperationException` when a weight is negative or not finite, when the
weights do not sum to a finite positive number, when `IdenticalKeyFloor` or `DefaultThreshold` is
outside [0, 1], when a phoneme penalty is outside [0, 1), when the profile is not a defined value, or
when `MaxKeyLength` is negative.

#### Scenario: Bad weights
- **WHEN** options with a weight of -0.1, NaN or infinity, or with all weights 0, are validated
- **THEN** an `InvalidOperationException` is thrown

#### Scenario: Bad penalty
- **WHEN** options with `InitialPhonemeMismatchPenalty` set to 1.0 are validated
- **THEN** an `InvalidOperationException` is thrown

#### Scenario: Unknown profile
- **WHEN** options with a `Profile` value cast from an undefined integer are validated
- **THEN** an `InvalidOperationException` is thrown

### Requirement: Consumers validate options up front
Every type that accepts options (scorer, encoder, equality comparer, index) SHALL validate them when
constructed, and every method that resolves a default threshold SHALL validate the effective options.

#### Scenario: Invalid options passed to a consumer
- **WHEN** a `DutchFuzzyScorer` is constructed with options whose weights are all 0
- **THEN** an `InvalidOperationException` is thrown at construction
