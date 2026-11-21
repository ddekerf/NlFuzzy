# phonetic-equality-comparison Specification

## Purpose

Lets callers use Dutch phonetic equality in hash-based collections and LINQ (dictionaries, sets,
`GroupBy`, `Distinct`) through a well-behaved `IEqualityComparer<string>`.

## Requirements

### Requirement: Equality is phonetic key equality
`DutchPhoneticEqualityComparer` SHALL consider two strings equal when their Dutch phonetic keys, built
with the comparer's options, are ordinally equal. Two nulls SHALL be equal and a null SHALL not equal
a non-null string.

#### Scenario: Same-sounding names
- **WHEN** `meijer`/`meier`, `smit`/`smid` and `Philips`/`filips` are compared with the default comparer
- **THEN** each pair is equal

#### Scenario: Different-sounding names
- **WHEN** `bakker`/`dekker` are compared with the default comparer
- **THEN** they are not equal

#### Scenario: Nulls
- **WHEN** null/null and null/`a` are compared
- **THEN** the results are true and false

### Requirement: Hash codes follow equality
`GetHashCode` SHALL return the ordinal hash of the phonetic key, so equal strings have equal hash
codes, and SHALL throw `ArgumentNullException` for null.

#### Scenario: Deduplication in a set
- **WHEN** `Meijer`, `Meier`, `Meyer` and `Bakker` are added to a `HashSet<string>` that uses the default comparer
- **THEN** the set contains 2 items

### Requirement: Only transitive equality gets a comparer
The library SHALL NOT offer an equality comparer based on a similarity threshold, because threshold
equality is not transitive and would corrupt hash-based collections.

#### Scenario: Public API
- **WHEN** the public API is inspected
- **THEN** the only `IEqualityComparer<string>` it exposes is `DutchPhoneticEqualityComparer`

### Requirement: Options are honoured
A comparer constructed with options SHALL validate them and encode with them.

#### Scenario: Custom options
- **WHEN** a comparer is built with the `Words` preset
- **THEN** it compares using `Strict`-profile keys, so `zee` and `see` are not equal
