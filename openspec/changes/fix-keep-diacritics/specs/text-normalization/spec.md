# Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Accented letters are dropped when diacritic folding is disabled`
- TO: `### Requirement: Accented letters are kept when diacritic folding is disabled`

## MODIFIED Requirements

### Requirement: Output is lower-case ASCII letters separated by single spaces
The normalizer SHALL return only the letters `a`–`z`, separated by single spaces, with no leading or
trailing whitespace. When `RemoveDiacritics` is false, it SHALL also return the accented Latin letters
described in "Accented letters are kept when diacritic folding is disabled". Case SHALL be folded to
lower case, and runs of whitespace or punctuation SHALL collapse into one space.

#### Scenario: Case and surrounding whitespace
- **WHEN** `  BRUIJN  ` is normalized
- **THEN** the result is `bruijn`

#### Scenario: Punctuation and repeated spaces become single separators
- **WHEN** `Jan,   Willem`, `Sint-Niklaas` and `A.J. Kuipers` are normalized
- **THEN** the results are `jan willem`, `sint niklaas` and `a j kuipers`

#### Scenario: Digits are discarded
- **WHEN** `Kuipers 123` is normalized
- **THEN** the result is `kuipers`

### Requirement: Accented letters are kept when diacritic folding is disabled
When `RemoveDiacritics` is false, the normalizer SHALL keep every lower-case letter whose canonical
decomposition is a letter `a`–`z` followed only by combining marks, composed in Unicode form C. Every
other character SHALL be handled exactly as when folding is on, so the result differs from the folded
result only in the kept letters.

#### Scenario: Folding disabled
- **WHEN** `Renée`, `Céline`, `François` and `Müller` are normalized with `RemoveDiacritics` set to false
- **THEN** the results are `renée`, `céline`, `françois` and `müller`, each a single token

#### Scenario: Canonically equivalent input with folding disabled
- **WHEN** `Renée` written with a precomposed `é` and `Renée` written as `e` plus a combining acute accent are normalized with `RemoveDiacritics` set to false
- **THEN** both results are `renée`

#### Scenario: Other letters still separate
- **WHEN** `Jan Łukasz` and `Ωmega` are normalized with `RemoveDiacritics` set to false
- **THEN** the results equal those with `RemoveDiacritics` set to true

#### Scenario: Special letters are still expanded
- **WHEN** `Straße` and `Ĳsselmeer` are normalized with `RemoveDiacritics` set to false
- **THEN** the results are `strasse` and `ijsselmeer`

#### Scenario: Characters that are not kept are folded
- **WHEN** `ǽ`, `ḱ` and `t` plus a combining acute are normalized with `RemoveDiacritics` set to false
- **THEN** the results are `ae`, `k` and `t`, the same as with folding on

## ADDED Requirements

### Requirement: Name particles are recognized regardless of accents
Particle handling (moving leading particles, splitting glued prefixes, the elided article) SHALL treat
each accented Latin letter as its base letter, so it decides the same way with and without diacritic
folding. Stems SHALL keep their accents in the output.

#### Scenario: Accented particles
- **WHEN** `dé Vries` and `Vándenbroecke` are normalized with the `Names` preset and `RemoveDiacritics` set to false
- **THEN** the results are `vries dé` and `broecke van den`

#### Scenario: Elided article before an accented name
- **WHEN** `d'Été` is normalized with `RemoveDiacritics` set to false and to true, using the `Names` preset
- **THEN** both results start with the name followed by the particle `de` (`été de` and `ete de`)

#### Scenario: Particle check
- **WHEN** `DutchAffixes.IsParticle` is called with `dé`
- **THEN** it returns true
