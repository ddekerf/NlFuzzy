# Design

## Context

`DutchTextNormalizer.Normalize` calls `ExpandElidedArticles` on every input, regardless of options.
See proposal.md for why that is wrong for the `Words` preset.

## Goals / Non-Goals

**Goals:**
- `d'` is only read as `de` when the caller treats the input as names.

**Non-Goals:**
- Changing the glued `dh` rule, which already depends on `SplitGluedNamePrefixes`.

## Decisions

### Gate on `StripNamePrefixes`
`StripNamePrefixes` is the existing switch that says "treat leading particles as name particles". The
elided article is a name particle, so it follows the same switch: `Default` and `Names` keep the
rewrite, `Words` and any custom options with `StripNamePrefixes` false do not.

*Alternative: a new `ExpandElidedArticles` option.* Rejected: one more option for a behaviour that
always goes together with particle handling, plus a public API change, for no use case that needs them
apart.

*Alternative: gate on `SplitGluedNamePrefixes`.* Rejected: that would drop the rewrite from `Default`,
where `D'Hondt` vs `De Hondt` currently scores 1.0.

## Risks / Trade-offs

- [A caller with `StripNamePrefixes` false who matches names loses `D'Hondt` = `De Hondt`] → Such
  callers already opted out of particle handling, so `De Hondt` vs `Hondt` does not match for them
  either; the CHANGELOG states which presets apply.
