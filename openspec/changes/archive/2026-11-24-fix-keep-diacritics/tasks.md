# Tasks

## 1. Normalizer keeps accented Latin letters

- [x] 1.1 Replace `Normalize_KeepsDiacriticsWhenDisabled` (`ren e`) with `DutchTextNormalizerTests` for
  the spec scenarios, all with `RemoveDiacritics` false:
  - `Renée`, `Céline`, `François` and `Müller` → `renée`, `céline`, `françois`, `müller`;
  - precomposed and decomposed `Renée` → the same `renée`;
  - `Jan Łukasz` and `Ωmega` → the same result as with folding on;
  - `Straße` and `Ĳsselmeer` → `strasse` and `ijsselmeer`;
  - `d'Été` with `Names` → `été de`.

  Confirm the new tests fail on the current code.
- [x] 1.2 Add an internal `IsKeptAccentedLetter(char)` backed by a table for U+00C0–U+024F, built once
  from the rule in design.md §1. Add unit tests:
  - `é`, `ü`, `ç`, `ñ`, `å` are kept;
  - `ł`, `ø`, `ω` and `ạ` (U+1EA1) are not;
  - for every character in the range, the table agrees with the rule computed on the fly.
- [x] 1.3 In `DutchTextNormalizer`:
  - with folding off, compose the lower-cased text to NFC (skipping plain ASCII);
  - let `KeepLettersAndSpaces` keep `IsKeptAccentedLetter` characters only when `RemoveDiacritics` is
    false;
  - let `CountLetters` count kept letters.

  Verify that the 1.1 tests pass and that every existing normalizer test passes unchanged.

## 2. Encoder folds each token

- [x] 2.1 Add `DutchPhoneticEncoderTests` and `DutchFuzzyScorerTests` for the spec scenarios:
  - `Céline`, `Hélène`, `coördinatie` and `Müller` get the same key with `Default` and with
    `RemoveDiacritics` false (`SLN`, `ALN`, `KRTNS`, `MLR`);
  - a custom normalizer that returns `céline` still yields `SLN`;
  - with folding off, `Céline`/`Celine` give `SoundsLike` true and a similarity in [0.90, 1.0);
  - with `Default`, the similarity is 1.0.

  Confirm that the folding cases fail before 2.2.
- [x] 2.2 In `DutchPhoneticEncoder.EncodeToken`, fold the token (existing diacritic removal, only when
  it contains a character above U+007F) before the `-sen` rule and the rule table. Verify that the 2.1
  tests pass.
- [x] 2.3 *(Added during implementation; see design.md §1 revision and §5.)*
  - With folding off, fold every non-ASCII character that is not kept, exactly as with folding on
    (`ǽ` → `ae`, `ḱ` → `k`, a lone combining mark disappears).
  - Make `DutchAffixes` and the elided-article check decide on `FoldKeptAccents(token)`, with
    `IsParticle` included.
  - Add tests for `dé Vries`, `Vándenbroecke`, `ď'Hondt` and `Dhóndt` (same key in both modes),
    `IsParticle("dé")`, and the folded characters.
  - A new property test (`PhoneticKey_DoesNotDependOnRemoveDiacritics`) found the gap. With the fix it
    passes, and a scratch stress run of 200,000 random inputs, many with accented particles, found no
    mismatch.

## 3. Properties and regression

- [x] 3.1 In `PropertyTests`, stop skipping `RemoveDiacritics = false` in the canonical-equivalence
  property. With folding off, composed and decomposed input must still give the same key and score.
  Verify that every property test (idempotence, finite, symmetric, [0, 1]) passes for all profiles.
- [x] 3.2 Run the evaluation tests and the core golden tests. They must be unchanged, because every
  preset folds. Verify that the evaluation baseline needs no rewrite.
- [x] 3.3 Run the scorer benchmark (`bench/NlFuzzy.Benchmarks`, `--job short --inProcess`) before and
  after, and verify that it is no more than 5% slower.
  - The short job was too noisy (±1 µs), so old and new code were compared side by side, using a
    worktree at the proposal commit and medium jobs.
  - The first reliable pair put `SingleTokenScore` 5.4% slower, over budget. The cause was the
    accent-blind particle checks scanning every token. They now skip that scan when the normalizer
    knows the text is plain a-z (folding on), via an internal `foldAccents` flag in `DutchAffixes`.
    The public methods stay accent-blind.
  - `Score` turned out to be bimodal: the unchanged code itself measured about 1.3 µs in some runs and
    about 1.7 µs in others. So the final check ran out of process with four launches per side:
    1,668 → 1,700 ns (+1.9%).
  - `SingleTokenScore`, median of three alternating pairs: 1,039 → 1,043 ns (+0.4%).
  - Both are within budget.

## 4. Docs

- [x] 4.1 Update the XML doc of `DutchFuzzyOptions.RemoveDiacritics`. False keeps accented Latin letters
  in the text for the spelling components, while keys always fold. Name the range limit from design.md
  §1.
- [x] 4.2 Add a CHANGELOG `[Unreleased]` entry under "Opgelost": what the option did before
  (`Céline` → `c line`, key `K LN`) and what it does now.

## 5. Integration check

- [x] 5.1 Run the full `dotnet test -c Release` suite and build all three TFMs without warnings. Confirm
  that line coverage stays at or above the 95% gate.
  - 709 tests pass on net10.0 (Release); all three TFMs rebuild from scratch with 0 warnings; line
    coverage 97.66% against the 95% gate. The net8.0 test run is left to CI.
