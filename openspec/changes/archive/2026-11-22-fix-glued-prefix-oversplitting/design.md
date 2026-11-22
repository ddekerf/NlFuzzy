# Design

## Context

`DutchAffixes.SplitToken` walks `GluedPrefixes` (longest first) and splits on the first prefix for
which the token starts with that prefix and is at least `prefix.Length + MinimumStemLength` (3)
characters long. Length is the only guard, so any first name or surname that happens to start with
`de`, `den`, `van`, `ver`, `ten` or `ter` and has three more letters is split. The split output then
feeds `MovePrefixesToEnd`, so `Dekker` ends up as `kker de`. See proposal.md for the observed
damage.

Constraints: no runtime dependencies, `netstandard2.0` support, and phonetic keys are persisted by
callers, so the new behaviour must be deterministic and produce a single key per input.

## Goals / Non-Goals

**Goals:**
- Stop wrong splits for common Dutch/Flemish first names, surnames and place names.
- Keep every split the golden dataset relies on (`Vandenbroecke`, `Desmet`, `Verbrugghe`, ...) and
  short glued surnames such as `Devos`, `Dewit` and `Vanhee`.

**Non-Goals:**
- Knowing whether a token is a first name or a surname from its position. The library has no field
  information and both orders occur in real data.
- Making splitting perfect. It stays a recall-oriented heuristic behind an opt-in option.

## Decisions

### 1. Phonotactic guard on the stem
Reject a split when the stem starts with a doubled consonant (`bb`, `dd`, `ff`, `gg`, `kk`, `ll`,
`mm`, `nn`, `pp`, `rr`, `ss`, `tt`, `zz`) or with `ck`. After a rejection, try the next (shorter)
prefix. This removes the `Dekker`/`Decker`/`Dekkers`/`Dennis` → `de nnis` family with no list at all.

*Alternative: raise `MinimumStemLength` from 3 to 4.* Rejected: it also kills `Devos`, `Dewit` and
`Vanhee`, which are among the most common glued Flemish surnames, while still splitting `Vanessa`
(`essa`) and `Tenzing` (`zing`).

### 2. A curated never-split list for the rest
Names such as `Vanessa`, `Dennis` (`den` + `nis`), `Delia`, `Verena`, `Veronique`, `Tenzing`,
`Deventer`, `Dendermonde` and `Tervuren` pass any phonotactic rule. A hand-built, ordinal
`HashSet<string>` of normalized tokens that are never split is the simplest deterministic fix.
Seed it from the most frequent first names in NL/BE that start with a glued-prefix spelling, plus
municipalities with such names. Names and place names are facts, so compiling them by hand raises
no licensing issue; no external file is copied (see PROVENANCE.md).

*Alternative: score both the split and the unsplit variant and keep the maximum.* Rejected for this
change: it doubles scoring cost and still leaves the persisted key ambiguous (which variant is
"the" key for `PhoneticKey`, `SoundsLike` and the equality comparer?).

### 3. Keep the list internal
The list is not exposed or configurable. Callers who disagree can turn `SplitGluedNamePrefixes` off
or supply their own `IDutchTextNormalizer`. A public extension point can follow later without a
breaking change.

## Risks / Trade-offs

- [A glued surname that is also on the never-split list, or that starts with a doubled consonant
  after the prefix] → Accept as a miss. Add regression cases to the golden dataset whenever one is
  reported.
- [The list goes stale or misses names] → Add a parametrised test over the list so additions are
  cheap, and invite additions in CONTRIBUTING.md.
- [Keys change for wrongly split inputs] → Not published yet. Note it as a phonetic-key **BREAKING**
  change in `[Unreleased]` so the key-version policy stays honest.
