# cologne-phonetic-encoding Specification

## Purpose

Provides the German Kölner Phonetik (Postel, 1969) as a second, independent reference encoder next to
the Dutch encoder, for comparison and for data with German names.

## Requirements

### Requirement: Codes match the published algorithm
`EncodeWord` SHALL produce the Kölner Phonetik digit code of a single word, including the contextual
rules for `c`, `d`/`t`, `p` and `x`, merging of adjacent duplicate codes, and removal of every `0`
except a leading one.

#### Scenario: Reference values
- **WHEN** `Wikipedia`, `Breschnew` and `Muellerluedenscheidt` are encoded with `EncodeWord`
- **THEN** the codes are `3412`, `17863` and `65752682`

#### Scenario: Same code for a vowel change
- **WHEN** `Peter` and `Petra` are encoded with `EncodeWord`
- **THEN** both codes are `127`

### Requirement: German special characters are folded first
`EncodeWord` and `Encode` SHALL apply the same preprocessing, so `ä`, `ö`, `ü` and `ß`/`ẞ` encode as `a`,
`o`, `u` and `ss`.

#### Scenario: Umlauts and sharp s
- **WHEN** `Änne`/`Anne`, `Müller`/`Muller` and `Straße`/`Strasse` are encoded with `EncodeWord`
- **THEN** each pair produces the same code

### Requirement: Phrases are encoded per token
`Encode` SHALL normalize the input with the configured options, encode each token separately and join
the token codes with single spaces. `EncodeTokens` SHALL return the token codes as a list.

#### Scenario: Two-word name
- **WHEN** `Peter Müller` is encoded with an encoder built on the `Words` preset
- **THEN** the result is `127 657`

#### Scenario: Empty input
- **WHEN** an empty string is encoded with `EncodeWord`
- **THEN** the result is empty
