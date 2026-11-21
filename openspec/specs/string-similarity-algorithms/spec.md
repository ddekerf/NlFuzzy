# string-similarity-algorithms Specification

## Purpose

Provides the classic, language-neutral string distance and similarity measures as standalone public
building blocks, with exact, documented contracts that the Dutch scorer also relies on.

## Requirements

### Requirement: Levenshtein distance
`LevenshteinDistance.Compute` SHALL return the minimum number of single-character insertions,
deletions and substitutions that turn one string into the other. `Normalized` SHALL divide that
distance by the length of the longer input, giving a value in [0, 1].

#### Scenario: Reference values
- **WHEN** the pairs `kitten`/`sitting`, `saturday`/`sunday`, `flaw`/`lawn` and `meijer`/`meier` are computed
- **THEN** the distances are 3, 3, 2 and 1

#### Scenario: Empty input
- **WHEN** an empty string and `kuipers` are computed
- **THEN** the distance is 7

### Requirement: Damerau-Levenshtein uses optimal string alignment
`DamerauLevenshteinDistance.Compute` SHALL count an adjacent transposition as one edit, using the
optimal string alignment variant, in which no substring is edited more than once.

#### Scenario: Typing error
- **WHEN** `kuipers` and `kuipres` are computed
- **THEN** the distance is 1

#### Scenario: OSA, not unrestricted Damerau
- **WHEN** `ca` and `abc` are computed
- **THEN** the distance is 3

### Requirement: Jaro-Winkler similarity
`JaroWinklerSimilarity` SHALL return the Jaro similarity plus the Winkler bonus for a common prefix of
up to four characters, using a scaling factor in [0, 0.25] (default 0.1). The constructor SHALL
reject a scaling factor that is out of range or not finite.

#### Scenario: Reference values
- **WHEN** `MARTHA`/`MARHTA`, `DWAYNE`/`DUANE` and `DIXON`/`DICKSONX` are compared
- **THEN** the similarities are 0.96111, 0.84000 and 0.81333 (to five decimals)

#### Scenario: Plain Jaro
- **WHEN** `CRATE` and `TRACE` are compared with `JaroSimilarity`
- **THEN** the similarity is 0.73333

#### Scenario: Invalid scaling factor
- **WHEN** a `JaroWinklerSimilarity` is constructed with NaN, an infinite value or a value outside [0, 0.25]
- **THEN** an `ArgumentOutOfRangeException` is thrown

### Requirement: Dice coefficient over bigrams
`DiceCoefficientSimilarity` SHALL return the Sørensen-Dice coefficient over the distinct character
bigrams of both strings.

#### Scenario: Reference value
- **WHEN** `night` and `nacht` are compared
- **THEN** the similarity is 0.25

#### Scenario: Single characters
- **WHEN** `a`/`a` and `a`/`b` are compared
- **THEN** the similarities are 1.0 and 0.0

### Requirement: Token-set similarity ignores token order
`TokenSetSimilarity` SHALL sort the whitespace-separated tokens of both strings ordinally, join them
with single spaces, and apply an inner similarity (Jaro-Winkler by default).

#### Scenario: Reordered tokens
- **WHEN** `van den broeke` and `broeke den van` are passed to `SortTokens`
- **THEN** both produce the same string

### Requirement: Shared contracts
Every similarity SHALL be symmetric and return a finite value in [0, 1], with 1.0 for two empty
strings and 0.0 when exactly one side is empty. Every measure SHALL throw `ArgumentNullException` for
null input. Instances SHALL be stateless and thread-safe.

#### Scenario: Empty strings
- **WHEN** Jaro-Winkler compares two empty strings, and then `abc` with an empty string
- **THEN** the similarities are 1.0 and 0.0

#### Scenario: Symmetry
- **WHEN** `kuipers` and `kuypers` are compared in both orders by each similarity
- **THEN** both orders give the same value
