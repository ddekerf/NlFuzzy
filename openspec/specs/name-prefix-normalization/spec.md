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

### Requirement: An apostrophe after d is only a separator
The normalizer SHALL treat the apostrophe in `D'` as a word separator, leaving a lone `d` that is not a
particle.

#### Scenario: Elided de
- **WHEN** `D'Hondt` is normalized with prefix handling disabled
- **THEN** the result is `d hondt`

### Requirement: Glued Flemish prefixes are split when enabled
When `SplitGluedNamePrefixes` is true, the normalizer SHALL split a known glued particle spelling
(`van`, `vande`, `vanden`, `vander`, `vanhet`, `de`, `den`, `der`, `ten`, `ter`, `ver`, and the
`op`/`in`/`uit`/`aan` + `de`/`den` compounds) off the start of every token, longest spelling first,
when at least three letters remain. `ver` SHALL expand to `van der`.

#### Scenario: Glued surnames
- **WHEN** `vandenbroecke`, `vandemoortele`, `verbrugghe`, `vanhee`, `opdebeeck`, `indenberg` and `desmet` are passed to `SplitGluedPrefixes`
- **THEN** the results are `van den broecke`, `van de moortele`, `van der brugghe`, `van hee`, `op de beeck`, `in den berg` and `de smet`

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
