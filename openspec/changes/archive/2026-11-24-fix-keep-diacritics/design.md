# Design

## Context

`DutchTextNormalizer.Normalize` runs these steps:

1. `FoldForPhonetics` lower-cases the text. When `RemoveDiacritics` is true it decomposes to NFD,
   drops the combining marks and recomposes; then it expands special letters (`ß`, `ĳ`, …).
2. `ExpandElidedArticles` rewrites `d'` to `de` (name presets only), counting the letters after the
   apostrophe as `a`–`z`.
3. `KeepLettersAndSpaces` keeps `a`–`z` and turns everything else into a single space.
4. The particle handling follows.

With folding off, step 1 leaves `é` in place and step 3 turns it into a space, so `Céline` → `c line`.
The Dutch encoder then encodes each token of the normalized text, and it silently skips any character
for which no rule matches. Simply keeping `é` in step 3 would therefore drop the vowel from the key
(`Céline` → `K…`). The Cologne encoder already avoids this by folding its input.

## Goals / Non-Goals

**Goals:**
- With `RemoveDiacritics` false, accented Latin letters survive normalization as single letters, and
  words stay whole.
- The phonetic key never depends on `RemoveDiacritics`.
- With folding on (every preset), normalized text, keys and scores stay exactly as they are.

**Non-Goals:**
- Phonetic rules for accented letters (`é` as a separate vowel class).
- Keeping non-Latin scripts. Greek, Cyrillic and letters without a decomposition such as `ł` stay
  separators, exactly as with folding on.
- Changing the presets.

## Decisions

### 1. Which letters are kept
A character is kept when it is `a`–`z`, or when its NFD decomposition is one `a`–`z` letter followed
only by characters of category `NonSpacingMark`. The text has already been lower-cased.

The check runs on the text after it is composed to NFC, because a decomposed `e` + `◌́` must end up as
one kept `é`. With folding off, step 1 composes the text to NFC instead of stripping marks; plain ASCII
skips both, as it does today.

A per-character `Normalize(FormD)` would allocate, so `IsKeptAccentedLetter(char)` uses a lookup table
for U+00C0–U+024F (Latin-1 Supplement and Latin Extended-A/B), built once from that same rule. Anything
outside that range is not kept. That range covers every precomposed Latin letter with a diacritic that
Dutch, Flemish, French, German and the Scandinavian languages use.

*Alternative: keep everything for which `char.IsLetter` is true.* Rejected by choice: non-Latin letters
would then count for spelling only, and the encoder has no rules for them.

*Revised during implementation:* every non-ASCII character that is not kept is folded exactly as with
folding on, instead of being left for the letter filter. Otherwise `ǽ` (`æ` + accent, base not `a`–`z`)
became a separator instead of `ae`, a lone combining mark after `t` split the word, and `ạ` (U+1EA1,
outside the table) became a separator instead of `a`. The keep-mode text now differs from the folded
text only in the kept letters, and each kept letter maps to exactly one base letter. The table stores
that base letter, not just a flag.

### 2. The encoder folds each token
`DutchPhoneticEncoder.EncodeToken` starts by folding the token with the existing diacritic removal when
it contains a character above U+007F, before the `-sen` rule and the rule table. With every preset the
tokens are plain ASCII, so the check costs one scan and nothing else.

This also covers a custom `IDutchTextNormalizer` that returns accented text.

*Alternative: let the normalizer return two forms (folded for keys, unfolded for spelling).* Rejected:
it changes `IDutchTextNormalizer` (public API) for one option.

### 3. What the option now means
"Accents count for spelling, not for sound." With folding off, `Céline`/`Celine` share the key `SLN`,
so they score at least `IdenticalKeyFloor` (0.90), but the Jaro-Winkler and Dice components see one
different character, so the score stays below 1.0. That is what a caller asks for when switching
folding off, and keys, `SoundsLike` and index blocking stay stable.

### 4. Elided article
`CountLetters` in `ExpandElidedArticles` counts kept letters (`a`–`z` or `IsKeptAccentedLetter`), so
`d'Été` becomes `de été` → `été de` without folding, just as it becomes `ete de` with folding. The
check that the `d` starts a token already uses `char.IsLetter` and stays as it is.

### 5. Name particles decide on the folded token
*Revised during implementation.* The plan said that `DutchAffixes` was unaffected because "an accented
particle is not a particle in either mode". That was wrong. With folding on, `dé` becomes `de` and is
moved to the end. With folding off it stayed in front, so the token order and the key differed. A
property test found it (`ó` as the particle `o`).

`DutchAffixes` now makes every decision (particle lists, never-split list, glued prefixes, plausible
stem start, `dh` + vowel) on `FoldKeptAccents(token)`. Because that maps one character to one, it can
still copy stems with their accents from the original token. A rewritten glued prefix is emitted in
ASCII (`ván` + `den…` → `van den …`), just as with folding on. The elided-article check compares the
folded `ď` too. The public `DutchAffixes.IsParticle` ignores accents as a result (`dé` → true).

### 6. Other code that assumes `a`–`z`
The `TranspositionFloor` check and the Dice bigram packing work on any UTF-16 characters.

## Risks / Trade-offs

- [A precomposed letter outside U+00C0–U+024F, such as Vietnamese `ạ` (U+1EA1), loses its accent even
  with folding off (`ạ` → `a`)] → This matches the requested scope (accented Latin letters in the
  common range), and it behaves exactly as with folding on. It is documented in the XML doc.
- [`DutchAffixes.IsParticle` now ignores accents] → A public method answers differently for accented
  input (`dé`). Recorded in the CHANGELOG; no ASCII input is affected.
- [Callers who relied on the old splitting] → Unlikely: the old output was broken (`c line`). It is
  recorded under "Opgelost" in the CHANGELOG.
- [Hot path] → Every preset folds, so normalization takes the same path as before, and the encoder
  adds one ASCII scan per token. The existing benchmark confirms it.
