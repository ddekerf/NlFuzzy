# Spec Delta

## ADDED Requirements

### Requirement: Keys fold diacritics regardless of RemoveDiacritics
The Dutch encoder SHALL fold diacritics in each normalized token before applying its rules, so the
phonetic key of a value does not depend on `RemoveDiacritics`, and accented letters that a custom
normalizer returns are encoded as their base letters. With `RemoveDiacritics` false, accents SHALL
still count for the orthographic and n-gram components of the similarity score.

#### Scenario: Same key with and without folding
- **WHEN** `Céline`, `Hélène`, `coördinatie` and `Müller` are encoded with the `Default` preset and with `RemoveDiacritics` set to false
- **THEN** each value gets the same key both times (`SLN`, `ALN`, `KRTNS`, `MLR`)

#### Scenario: Accents count for spelling only
- **WHEN** `Céline` and `Celine` are compared with `RemoveDiacritics` set to false
- **THEN** `SoundsLike` is true, and the similarity is at least 0.90 and below 1.0

#### Scenario: Folding on is unchanged
- **WHEN** `Céline` and `Celine` are compared with the `Default` preset
- **THEN** the similarity is 1.0
