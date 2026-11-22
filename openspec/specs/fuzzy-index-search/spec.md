# fuzzy-index-search Specification

## Purpose

Defines what `DutchFuzzyIndex<T>` returns for each search mode, and how the approximate blocked modes
relate to a linear scan over the same items, for callers that search the same collection repeatedly.

## Requirements

### Requirement: Items are prepared once
The index SHALL normalize and encode each item's selected text once, when the item is added, and reuse
that work for every search. A null selected text SHALL be indexed as an empty string. Items added with
`Add` after construction SHALL be searchable immediately.

#### Scenario: Adding after construction
- **WHEN** an item is added to an existing index with `Add`
- **THEN** a following search can return it, and `Count` includes it

### Requirement: Exact mode equals the linear scan
`Search` SHALL default to `Exact` mode, which scores every indexed item and returns the same matches,
scores and order as `FuzzyMatchDutch` over the same items, options, threshold and `take`, including
when `IgnoreTokenOrder` is enabled.

#### Scenario: Adversarial data
- **WHEN** an index and `FuzzyMatchDutch` are run over the same adversarial dataset with thresholds 0.5, 0.7 and 0.9 and several `take` values
- **THEN** both return identical results

#### Scenario: Default threshold
- **WHEN** `Search` is called without a threshold
- **THEN** the `DefaultThreshold` of the index's options is used

#### Scenario: Reordered names in exact mode
- **WHEN** an index over `Bakker, Jan` and `Visser, Klaas` is searched for `Jan Bakker` in `Exact` mode with the `Names` preset
- **THEN** the result equals what `FuzzyMatchDutch` returns for the same input, and its first match is `Bakker, Jan`

### Requirement: Blocked modes score phonetic blocks first
`Blocked` and `BlockedOnly` SHALL first score only the items that share the query's full phonetic key,
the first four characters of it, or (when `IgnoreTokenOrder` is enabled) its token-order-independent
key. `Blocked` SHALL then also score the remaining items when the blocks yield fewer than `take`
matches (or no match when `take` is not positive). `BlockedOnly` SHALL never scan the remaining items.

#### Scenario: Fallback to a full scan
- **WHEN** `Blocked` mode finds fewer matches in the blocks than `take`
- **THEN** it also scores the items outside the blocks

#### Scenario: Blocks only
- **WHEN** `BlockedOnly` mode is used and a qualifying item shares no block with the query
- **THEN** that item is not returned

#### Scenario: Reordered name in blocked-only mode
- **WHEN** an index over 1,000 unrelated names plus `Bakker, Jan` is searched for `Jan Bakker` in `BlockedOnly` mode with the `Names` preset
- **THEN** `Bakker, Jan` is returned

### Requirement: Search modes are validated
`Search` SHALL throw `ArgumentOutOfRangeException` for an undefined search mode or an invalid threshold,
and `ArgumentNullException` for a null query.

#### Scenario: Unknown mode
- **WHEN** `Search` is called with a mode value cast from an undefined integer
- **THEN** an `ArgumentOutOfRangeException` is thrown

### Requirement: Concurrency contract
Concurrent searches on an index that is not being modified SHALL be safe and return stable results.
Concurrent `Add` and `Search` calls are not supported.

#### Scenario: Parallel search
- **WHEN** the same query is run concurrently from many threads on a shared index
- **THEN** every thread gets the same result
