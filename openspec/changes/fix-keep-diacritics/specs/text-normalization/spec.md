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
decomposition is a letter `a`–`z` followed only by combining marks, composed in Unicode form C, so
composed and decomposed input normalize identically. Any other character that is not `a`–`z` SHALL be
a word separator, as it is when folding is on. Ligatures and special letters SHALL still be expanded.

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

#### Scenario: Elided article before an accented name
- **WHEN** `d'Été` is normalized with `RemoveDiacritics` set to false and to true, using the `Names` preset
- **THEN** both results start with the name followed by the particle `de` (`été de` and `ete de`)
