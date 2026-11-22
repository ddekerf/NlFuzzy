# name-prefix-normalization Specification

## Purpose

Defines how Dutch and Flemish name particles ("tussenvoegsels") are recognised, split off when glued
to a surname, and moved, so that particle placement and spelling do not affect matching.

## Requirements

### Requirement: Dutch name particles are recognised
The library SHALL recognise a fixed list of name particles, including `van`, `de`, `den`, `der`,
`des`, `ten`, `ter`, `te`, `het`, the elided `t` and `s`, the prepositions `op`, `in`, `uit`, `aan`,
`bij`, `onder`, `over`, `tot`, `toe`, and common foreign particles such as `la`, `le`, `du`, `di`,
`da`, `el`, `al`, `mac`, `mc` and `o`.

#### Scenario: Particles
- **WHEN** `van`, `de`, `den`, `der`, `ten`, `ter`, `t` and `s` are checked with `IsParticle`
- **THEN** each is recognised as a particle

#### Scenario: Ordinary tokens
- **WHEN** `broeke`, `kuipers`, `ver` and null are checked with `IsParticle`
- **THEN** none is recognised as a particle

### Requirement: Leading particles are moved to the end
When `StripNamePrefixes` is true (the default), the normalizer SHALL move a leading run of particles
to the end of the value, so the separated form and the "surname, particles" form normalize
identically. At most three leading particles SHALL be moved, and the last token is never moved.

#### Scenario: Leading particles
- **WHEN** `Van den Broeke`, `De Smet`, `Op de Beeck` and `In het Veld` are normalized with the `Default` preset
- **THEN** the results are `broeke van den`, `smet de`, `beeck op de` and `veld in het`

#### Scenario: Particles already at the end
- **WHEN** `Van den Broeke` and `Broeke, van den` are compared with the `Default` preset
- **THEN** the similarity is 1.0

#### Scenario: Only particles
- **WHEN** `van de` is passed to `MovePrefixesToEnd`
- **THEN** it is returned unchanged

#### Scenario: More than three leading particles
- **WHEN** `van de op het beeck` is passed to `MovePrefixesToEnd`
- **THEN** the result is `het beeck van de op`

### Requirement: Apostrophe forms of 's and 't are treated as particles
Because apostrophes become separators, `'s-` and `'t` SHALL leave the particles `s` and `t`, which are
then moved like any other particle.

#### Scenario: Place name and surname with elided articles
- **WHEN** `'s-Hertogenbosch` and `Van 't Hof` are normalized with the `Default` preset
- **THEN** the results are `hertogenbosch s` and `hof van t`

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

### Requirement: Glued-only particles are moved when splitting is enabled
When `SplitGluedNamePrefixes` is true, `ver`, `vander`, `vanden` and `vande` SHALL also count as
leading particles to move. When it is false, they SHALL be treated as ordinary tokens.

#### Scenario: ver as a particle
- **WHEN** `ver brugge` is passed to `MovePrefixesToEnd` with glued-only particles included, and again without
- **THEN** the results are `brugge ver` and `ver brugge`

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

### Requirement: Elided article d' is read as the particle de
When `StripNamePrefixes` is true (the `Default` and `Names` presets), the normalizer SHALL treat a
`d` at the start of a token, followed directly by an apostrophe character (`'`, `’`, `‘`, `` ` `` or
`´`) and then by a name of at least three letters, as the particle `de`. When `StripNamePrefixes` is
false, the apostrophe SHALL remain a plain word separator.

#### Scenario: Apostrophe form matches the separated form
- **WHEN** `D'Hondt` and `De Hondt` are compared with the `Names` preset
- **THEN** both produce the same phonetic key and the similarity is 1.0

#### Scenario: Typographic apostrophe
- **WHEN** `D’Haene` (right single quotation mark) and `De Haene` are compared with the `Default` preset
- **THEN** both produce the same phonetic key

#### Scenario: Elision inside a full name
- **WHEN** `Marie D'Hooghe` and `Marie De Hooghe` are compared with the `Names` preset
- **THEN** both produce the same phonetic key

#### Scenario: Ordinary words keep the apostrophe as a separator
- **WHEN** `d'accord` is normalized with the `Words` preset
- **THEN** the result is `d accord`

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
