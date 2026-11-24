# dutch-phonetic-encoding Specification

## Purpose

Converts Dutch and Flemish spelling into a sounds-like key ("Dutch Metaphone"), so that spelling
variants of the same pronunciation share a key that can be compared, hashed, used for blocking and
persisted.

## Requirements

### Requirement: Spelling variants of the same sound share a key
The encoder SHALL map Dutch spelling variants of the same pronunciation onto the same key. This
covers at least `ij`/`ei`/`y`, `ui`/`uy`/`uij`, `ae`/`aa`, `c`/`k`, `ks`/`x`/`cks`, `ph`/`f`, `th`/`t`,
`gh`/`g`, `cht`/`gt`, `qu`/`kw`, `c`/`s` before `e`, `i` or `y`, final `dt`/`t`, final `d`/`t` and
final `sch`/`s`.

#### Scenario: Default preset keys
- **WHEN** `Bruijn`, `Bruyn` and `Bruin` are encoded with the `Default` preset
- **THEN** all three keys are `PRN`

#### Scenario: ij, ei and y
- **WHEN** `Meijer`, `Meier` and `Meyer` are encoded with the `Default` preset
- **THEN** all three keys are `MR`

#### Scenario: Consonant spellings
- **WHEN** the pairs `Philips`/`Filips`, `Thijs`/`Tijs`, `Quirijn`/`Kwirijn`, `Smid`/`Smit` and `Bosch`/`Bos` are encoded with the `Default` preset
- **THEN** each pair shares a key: `FLPS`, `TS`, `KFRN`, `SMT` and `PS`

#### Scenario: Distinct names stay apart
- **WHEN** the pairs `jansen`/`hansen`, `maes`/`moes` and `bakker`/`dekker` are encoded with the `Default` preset
- **THEN** the keys of each pair differ

### Requirement: Rules apply longest match first
At each position of a token, the encoder SHALL apply the longest matching rule from the Dutch rule
table, using its positional context (word start, word end, before a vowel, before `r`, after `t`), and
SHALL skip a character that no rule matches. Every rule in the table SHALL be exercised by the test
corpus in both profiles.

#### Scenario: Positional context
- **WHEN** `chocolade` is encoded with the `Default` preset
- **THEN** the key is `XKLT`, with word-initial `ch` encoded as `X`

#### Scenario: w before r
- **WHEN** `vrede` and `wrede` are encoded without vowel classes, once with the `Strict` profile and once with the `Tolerant` profile
- **THEN** both words share the key in each profile: `VRD` in `Strict` and `FRT` in `Tolerant`

### Requirement: The Tolerant profile merges voiced and voiceless pairs
With the `Tolerant` profile, the encoder SHALL merge `b`/`p`, `d`/`t`, `v`/`f`/`w`, `z`/`s` and
`sk`/`sch`, and SHALL drop a word-initial `h`. The `Strict` profile SHALL keep the voiced and voiceless
consonants apart, except for word-final devoicing. Both profiles SHALL treat `ch` and `g` alike.

#### Scenario: Voicing
- **WHEN** `zee`, `see`, `doren` and `toren` are encoded without vowel classes
- **THEN** `Strict` gives `Z`, `S`, `DRN` and `TRN`, while `Tolerant` gives `S`, `S`, `TRN` and `TRN`

#### Scenario: sk and sch
- **WHEN** `school` and `skool` are encoded without vowel classes
- **THEN** `Strict` gives `SGL` and `SKL`, while `Tolerant` gives `SGL` for both

### Requirement: Vowels are encoded as classes or dropped
When `EncodeVowels` is true, vowels SHALL be encoded as the classes `1` (a), `2` (e), `3` (i/ie),
`4` (o), `5` (u), `6` (ei/ij/y) and `7` (ui/ou/au/oe/eu). When it is false, the default, non-initial
vowels SHALL be dropped and a word-initial vowel SHALL be encoded as `A`.

#### Scenario: Vowels dropped
- **WHEN** `Anna` and `Ooms` are encoded with the `Default` preset
- **THEN** the keys are `AN` and `AMS`

#### Scenario: Vowel classes
- **WHEN** `Maes`, `Maas` and `Moes` are encoded with the `Names` preset
- **THEN** the keys are `M1S`, `M1S` and `M7S`

### Requirement: Keys are post-processed per token
The encoder SHALL encode each whitespace-separated token on its own, collapse immediately repeated
output characters, and join the token keys with single spaces. When `MaxKeyLength` is positive, each
token key SHALL be truncated to that length. The key SHALL only use the characters
`P B T D K G F V W S Z X J L M N R H Q`, the digits `1`–`7`, `A` and the space.

#### Scenario: One key per token
- **WHEN** `PhoneticKeys` is called for `Jan Willem Kuipers` with the `Default` preset
- **THEN** it returns `JN`, `FLM` and `KPRS`

#### Scenario: Truncation per token
- **WHEN** `Jan Schoenmakers` is encoded with the `Default` preset and `MaxKeyLength` 3
- **THEN** the key is `JN SGN`

#### Scenario: Alphabet
- **WHEN** the seeded property tests encode random input
- **THEN** every key uses only the documented output alphabet

### Requirement: Patronymic -sen is encoded like -se when enabled
When `MergeSeSenEndings` is true, the encoder SHALL encode a token of at least five letters that ends in
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

#### Scenario: The stem's spelling does not matter
- **WHEN** `Gijsen` and `Gysen` are encoded with the `Names` preset
- **THEN** both produce the same phonetic key

#### Scenario: Very short tokens are left alone
- **WHEN** the four-letter tokens `Esen` and `Isen` are encoded with the `Names` preset
- **THEN** they keep their final `N`, so `Esen` and `Ese` have different keys

#### Scenario: Off by default
- **WHEN** `Willemsen` and `Willemse` are encoded with the `Default` preset
- **THEN** `Willemsen` keeps the same key it has today

### Requirement: Keys fold diacritics regardless of RemoveDiacritics
The Dutch encoder SHALL fold diacritics in each normalized token before applying its rules, so the
phonetic key of a value does not depend on `RemoveDiacritics`, and accented letters that a custom
normalizer returns are encoded as their base letters. With `RemoveDiacritics` false, accents SHALL
still count for the orthographic and n-gram components of the similarity score.

#### Scenario: Same key with and without folding
- **WHEN** `Céline`, `Hélène`, `coördinatie` and `Müller` are encoded with the `Default` preset and with `RemoveDiacritics` set to false
- **THEN** each value gets the same key both times (`SLN`, `ALN`, `KRTNS`, `MLR`)

#### Scenario: Accented particles
- **WHEN** `dé Vries`, `Vándenbroecke` and `ď'Hondt` are encoded with the `Names` preset, with and without `RemoveDiacritics`
- **THEN** each value gets the same key both times

#### Scenario: Any input
- **WHEN** the seeded property tests encode random input, composed or decomposed, with every profile
- **THEN** the key with `RemoveDiacritics` false equals the key with it true

#### Scenario: Accents count for spelling only
- **WHEN** `Céline` and `Celine` are compared with `RemoveDiacritics` set to false
- **THEN** `SoundsLike` is true, and the similarity is at least 0.90 and below 1.0

#### Scenario: Folding on is unchanged
- **WHEN** `Céline` and `Celine` are compared with the `Default` preset
- **THEN** the similarity is 1.0

### Requirement: Encoding is total and deterministic
The encoder SHALL return an empty key for input with nothing to encode, SHALL throw
`ArgumentNullException` for null input, and SHALL return the same key for the same input and options
on every call and thread.

#### Scenario: Nothing to encode
- **WHEN** an empty string or `123` is encoded
- **THEN** the key is empty

#### Scenario: Parallel encoding
- **WHEN** the same values are encoded concurrently from many threads
- **THEN** every thread gets identical keys

### Requirement: Key changes are major-version changes
Any change to the rule table, the post-processing or the normalization that changes the key produced
for an existing input SHALL only ship in a new major version, and SHALL be recorded in the CHANGELOG.
Changes to score weights and thresholds are not covered by this rule.

#### Scenario: Rule table change
- **WHEN** a pull request changes the key of any input in the golden dataset or the rule-coverage corpus
- **THEN** the change is recorded as **BREAKING** in the CHANGELOG and released in a new major version
