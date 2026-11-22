# Spec Delta

## MODIFIED Requirements

### Requirement: Documented defaults
A new `DutchFuzzyOptions` SHALL default to: `Tolerant` profile, `RemoveDiacritics` true,
`StripNamePrefixes` true, `SplitGluedNamePrefixes` false, `EncodeVowels` false, `MergeSeSenEndings`
false, `MaxKeyLength` 0 (unlimited), `IgnoreTokenOrder` true, weights 0.35 orthographic, 0.45 phonetic
and 0.20 n-gram, `IdenticalKeyFloor` 0.90, `DefaultThreshold` 0.82 and both phoneme penalties 0.20.

#### Scenario: Default preset
- **WHEN** `DutchFuzzyOptions.Default` is inspected
- **THEN** every property has the documented default value

### Requirement: Three presets
The library SHALL offer three singleton presets that differ only in their encoding and ordering flags:
`Default` (Tolerant, vowels dropped, particles moved, no glued splitting, token order ignored),
`Names` (Tolerant, vowel classes, particles moved, glued splitting, `-sen` encoded like `-se`, token
order ignored) and `Words` (Strict, vowel classes, no particle handling, token order respected).

#### Scenario: Names preset
- **WHEN** `DutchFuzzyOptions.Names` is inspected
- **THEN** it uses the `Tolerant` profile with `EncodeVowels`, `StripNamePrefixes`, `SplitGluedNamePrefixes`, `MergeSeSenEndings` and `IgnoreTokenOrder` all true

#### Scenario: Words preset
- **WHEN** `DutchFuzzyOptions.Words` is inspected
- **THEN** it uses the `Strict` profile with `EncodeVowels` true and `StripNamePrefixes`, `SplitGluedNamePrefixes`, `MergeSeSenEndings` and `IgnoreTokenOrder` all false

#### Scenario: Singletons
- **WHEN** a preset property is read twice
- **THEN** both reads return the same instance
