# Spec Delta

## MODIFIED Requirements

### Requirement: Glued Flemish prefixes are split when enabled
When `SplitGluedNamePrefixes` is true, the normalizer SHALL split a known glued particle spelling
(`van`, `vande`, `vanden`, `vander`, `vanhet`, `de`, `den`, `der`, `ten`, `ter`, `ver`, and the
`op`/`in`/`uit`/`aan` + `de`/`den` compounds) off the start of every token, longest spelling first,
when at least three letters remain and no guard below rejects the split. `ver` SHALL expand to
`van der`.

#### Scenario: Glued surnames
- **WHEN** `vandenbroecke`, `vandemoortele`, `verbrugghe`, `vanhee`, `opdebeeck`, `indenberg` and `desmet` are passed to `SplitGluedPrefixes`
- **THEN** the results are `van den broecke`, `van de moortele`, `van der brugghe`, `van hee`, `op de beeck`, `in den berg` and `de smet`

#### Scenario: Short glued surnames still split
- **WHEN** `devos`, `dewit` and `debruyne` are passed to `SplitGluedPrefixes`
- **THEN** the results are `de vos`, `de wit` and `de bruyne`

#### Scenario: Split and moved
- **WHEN** `Vandenbroecke` and `Van den Broecke` are compared with the `Names` preset
- **THEN** the similarity is 1.0

#### Scenario: Stem too short
- **WHEN** `vanel` is passed to `SplitGluedPrefixes`
- **THEN** it is returned unchanged

#### Scenario: Splitting is opt-in
- **WHEN** `Vandenbroecke` is normalized with the `Default` preset
- **THEN** the result is `vandenbroecke`

## ADDED Requirements

### Requirement: A glued prefix is not split off an implausible stem
The normalizer SHALL NOT split a glued prefix when the remaining stem starts with a doubled consonant
or with `ck`, because no Dutch word begins that way. When one prefix candidate is rejected, shorter
candidates for the same token SHALL still be tried under the same rule.

#### Scenario: Doubled consonant after the prefix
- **WHEN** `Dekker`, `Decker` and `Dekkers` are normalized with the `Names` preset
- **THEN** none of them is split, and `Dekker` and `Decker` produce the same phonetic key

#### Scenario: Ordinary surname is no longer mangled
- **WHEN** `Dekker` and `Bakker` are compared with the `Names` preset
- **THEN** the keys differ and the similarity stays below the default threshold of 0.82

### Requirement: Listed names are never split
The normalizer SHALL keep a curated list of first names and place names that begin with a glued-prefix
spelling but are not glued surnames, and SHALL never split a token on that list.

#### Scenario: First names stay intact
- **WHEN** `Vanessa`, `Dennis`, `Denise`, `Delia`, `Verena`, `Veronique` and `Tenzing` are normalized with the `Names` preset
- **THEN** each is returned as a single, unsplit token

#### Scenario: Place names stay intact
- **WHEN** `Deventer`, `Dendermonde` and `Tervuren` are normalized with the `Names` preset
- **THEN** each is returned as a single, unsplit token

#### Scenario: Spelling variants of a listed name still match
- **WHEN** `Vanessa` and `Vanesa` are compared with the `Names` preset
- **THEN** the similarity is at or above 0.82
