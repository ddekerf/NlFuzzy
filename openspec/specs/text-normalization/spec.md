# text-normalization Specification

## Purpose

Turns raw user input (names, words, addresses) into the canonical lower-case form that every encoder
and similarity measure works on, so that case, diacritics, ligatures, punctuation and spacing do not
affect matching.

## Requirements

### Requirement: Output is lower-case ASCII letters separated by single spaces
The normalizer SHALL return only the letters `a`–`z`, separated by single spaces, with no leading or
trailing whitespace. Case SHALL be folded to lower case, and runs of whitespace or punctuation SHALL
collapse into one space.

#### Scenario: Case and surrounding whitespace
- **WHEN** `  BRUIJN  ` is normalized
- **THEN** the result is `bruijn`

#### Scenario: Punctuation and repeated spaces become single separators
- **WHEN** `Jan,   Willem`, `Sint-Niklaas` and `A.J. Kuipers` are normalized
- **THEN** the results are `jan willem`, `sint niklaas` and `a j kuipers`

#### Scenario: Digits are discarded
- **WHEN** `Kuipers 123` is normalized
- **THEN** the result is `kuipers`

### Requirement: Diacritics are folded onto their base letter
When `RemoveDiacritics` is true (the default), the normalizer SHALL remove combining marks, so
accented letters normalize to their base letter, and composed and decomposed Unicode forms of the
same text SHALL normalize identically.

#### Scenario: Common accented names
- **WHEN** `Renée`, `François`, `Geïntegreerd` and `Émile Zöller` are normalized
- **THEN** the results are `renee`, `francois`, `geintegreerd` and `emile zoller`

#### Scenario: Canonically equivalent input
- **WHEN** `Renée` written with a precomposed `é` and `Renée` written as `e` plus a combining acute accent are normalized
- **THEN** both results are `renee`

### Requirement: Accented letters are dropped when diacritic folding is disabled
When `RemoveDiacritics` is false, the normalizer SHALL NOT fold accented letters, and because only
`a`–`z` are kept, each accented letter SHALL be replaced by a word separator.

#### Scenario: Folding disabled
- **WHEN** `Renée` is normalized with `RemoveDiacritics` set to false
- **THEN** the result is `ren e`

### Requirement: Dutch and Germanic special letters are expanded
The normalizer SHALL expand `ß`/`ẞ` to `ss`, `æ` to `ae`, `œ` to `oe`, `ø` to `o`, `đ` to `d` and the
Dutch ligature `ĳ`/`Ĳ` to `ij`. Expansion SHALL also apply to characters that only appear after
lower-casing or Unicode decomposition.

#### Scenario: Ligatures and sharp s
- **WHEN** `Straße`, `Ægir`, `Œuvre`, `Søren` and `ĳsbrand` are normalized
- **THEN** the results are `strasse`, `aegir`, `oeuvre`, `soren` and `ijsbrand`

#### Scenario: Characters that appear only after folding
- **WHEN** `STRAẞE` (capital sharp s) and `ǽgir` (ae ligature with acute) are normalized
- **THEN** the results are `strasse` and `aegir`

### Requirement: Blank input normalizes to an empty string
The normalizer SHALL return an empty string for input that is empty, whitespace-only, or contains no
letters at all. It SHALL throw `ArgumentNullException` for a null value or null options.

#### Scenario: Nothing left after cleaning
- **WHEN** an empty string, `   `, `123` or `!!!` is normalized
- **THEN** the result is an empty string

#### Scenario: Null input
- **WHEN** null is normalized
- **THEN** an `ArgumentNullException` is thrown

### Requirement: Normalization is idempotent
Normalizing an already normalized value with the same options SHALL return that value unchanged.

#### Scenario: Normalizing twice
- **WHEN** any input is normalized and the result is normalized again with the same options
- **THEN** both results are identical
