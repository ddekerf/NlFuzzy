# fuzzy-matching-api Specification

## Purpose

Defines the public entry points that application code calls: the `DutchFuzzy` facade, the string and
sequence extension methods, ranking of candidates, and the common threshold contract they share.

## Requirements

### Requirement: Facade and string extensions agree
`DutchFuzzy.PhoneticKey`, `PhoneticKeys`, `SoundsLike`, `Similarity` and `AreEquivalent`, and their
string extensions `ToDutchPhoneticKey`, `SoundsLikeDutch`, `DutchSimilarity` and `DutchFuzzyEquals`,
SHALL return the same results for the same arguments. Omitted options SHALL mean
`DutchFuzzyOptions.Default`.

#### Scenario: Extension equals facade
- **WHEN** `"Bruijn".ToDutchPhoneticKey()` and `DutchFuzzy.PhoneticKey("Bruijn")` are called
- **THEN** both return `PRN`

### Requirement: SoundsLike compares non-empty keys
`SoundsLike` SHALL return true only when both phonetic keys are equal and non-empty.

#### Scenario: Same and different sounds
- **WHEN** `Meijer`/`Meier` and `Bakker`/`Dekker` are checked with `SoundsLike`
- **THEN** the results are true and false

#### Scenario: Nothing to encode
- **WHEN** `123` and `!!!` are checked with `SoundsLike`
- **THEN** the result is false

### Requirement: Threshold resolution
Every method that takes a `double? threshold` SHALL use the caller's value when given, and otherwise
the `DefaultThreshold` of the effective options. A match SHALL require a score greater than or equal
to the threshold. `DutchStringExtensions.DefaultThreshold` SHALL equal the `Default` preset's value.

#### Scenario: Omitted threshold
- **WHEN** `DutchFuzzyEquals` is called without a threshold and with options whose `DefaultThreshold` is 0.99
- **THEN** a pair scoring 0.92 is not equivalent

#### Scenario: Explicit threshold wins
- **WHEN** `DutchFuzzyEquals` is called with threshold 0.5 and options whose `DefaultThreshold` is 0.99
- **THEN** a pair scoring 0.92 is equivalent

### Requirement: Invalid thresholds are rejected
`AreEquivalent`, `DutchFuzzyEquals`, `FuzzyMatchDutch`, `BestDutchMatch` and the index search SHALL
throw `ArgumentOutOfRangeException` for a threshold that is NaN, infinite or outside [0, 1].

#### Scenario: Negative threshold
- **WHEN** `AreEquivalent("a", "b", -1)` is called
- **THEN** an `ArgumentOutOfRangeException` is thrown instead of returning true

#### Scenario: NaN threshold
- **WHEN** `FuzzyMatchDutch` is called with a NaN threshold
- **THEN** an `ArgumentOutOfRangeException` is thrown

### Requirement: Ranking returns the best matches first
`FuzzyMatchDutch` SHALL score every item's selected text against the query, keep the items that reach
the threshold, and return them by descending score, breaking ties by ordinal text. A positive `take`
(default 10) SHALL limit the results to the top `take`; zero or a negative value returns all matches.

#### Scenario: Ordering
- **WHEN** `Meijer` is matched against `Meijer`, `Meier`, `Meyer`, `Bakker` and `Mijer` with threshold 0.5 and `take` 0
- **THEN** `Meijer` comes first with 1.0, and `Bakker` is not returned

#### Scenario: Take is a prefix of the full ranking
- **WHEN** the same query is ranked once with `take` 3 and once with `take` 0
- **THEN** the first result equals the first three entries of the second

#### Scenario: Match metadata
- **WHEN** a match is returned
- **THEN** it carries the item, the compared text, the score and the phonetic key of the text

### Requirement: Best match
`BestDutchMatch` SHALL return the single highest-scoring item that reaches the threshold, or null when
no item does.

#### Scenario: No match
- **WHEN** `BestDutchMatch` is called with a query that no item reaches
- **THEN** the result is null

### Requirement: Null handling and thread safety
The facade and extensions SHALL throw `ArgumentNullException` for a null source, query or selector,
SHALL treat a null selected text as an empty string, and SHALL be safe to call concurrently.

#### Scenario: Parallel scoring
- **WHEN** the same pairs are scored concurrently from many threads
- **THEN** every thread gets identical scores
