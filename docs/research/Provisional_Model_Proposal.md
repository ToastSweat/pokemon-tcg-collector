# Proposed provisional single-pack model

Status: Model A APPROVED for Phase 1 implementation. The user explicitly selected the sheet-informed model and its stated order/holo/duplicate policies. The separate Machamp completion-target question was unanswered when the form timed out; it remains unresolved, not implicitly approved. The user subsequently accepted Phase 1 and separately authorized Phase 2; this does not change Model A's provisional assumptions. The model is not a claim of exact factory collation.

## What research supports, and where it stops

The ordinary eleven-card composition is well supported by collector descriptions and photographed wrapper wording. A specific retrospective report places five commons, two Basic Energies, the rare/holo, then three uncommons in that order.[4][6]

The exact factory holo rate and pack-to-pack independence were not established; approximately one in three is collector convention.[5][6]

A collector's candidate sheet model provides unequal per-card weights, but its rare-sheet attribution and obscured Energy positions involve explicit assumptions. Exact feeder sequence, pair correlations and all-run applicability remain unverified.[5]

The 102-number checklist includes Machamp.[1][2]

That card is not booster-obtainable, and ordinary shadowed starter Machamp is stamped, so it needs a deliberate exception if included in the collection target.[3][9]

## Model A — recommended sheet-informed provisional v0.1

These are proposed engineering choices, not asserted historical facts:

1. **Scope:** 1999 English shadowed Unlimited, with ordinary printings only. No error/shade/foil master set, modern reprints, red-cheek Pikachu, special promotional stamps or other date printings.
2. **Slots and result order:** positions 1–5 Common; 6–7 Basic Energy; 8 Rare or Holo; 9–11 Uncommon. This follows the reported order as a provisional convention, with its physical orientation/sub-run uncertainty disclosed. Do not rearrange rare-last by default.
3. **Holo choice:** exact 1/3 independent Bernoulli for this model, replacing the non-holo rare. This operationalizes the approximate convention; it is not a proven factory statistic. No pity, quotas, boosted new-card odds or box/case cycles.
4. **Within-pool starting weights:** use the collector's candidate multiplicities below. Preserve provenance/uncertainties in the rule definition.
5. **C/U repeats:** weighted sampling without replacement by card-printing identity within each pool. After a card is drawn, remove all of that identity's weight for the remainder of the pack. This deliberately avoids identical C/U copies; it is not a proven factory restriction. It preserves relative starting weights but **does not preserve sheet-proportional marginal inclusion probabilities** after multiple draws.
6. **Energy pairs:** two independent weighted draws with replacement; identical Basic Energies are allowed. Pair frequency is an acknowledged approximation, not a reconstructed factory sequence.
7. **Across packs:** each independently generated single pack starts with fresh pools. No shared depletion, guaranteed box totals or box simulator.
8. **Eligibility:** Machamp never appears in booster pools, whatever binder decision is made. No starter/deck-only item is made pullable for convenience.
9. **Versioning:** freeze an approved model version and stable printing IDs in ordered immutable results. Future models must not rewrite historical generated contents. Persistence and collection awards remain Phase 2.
10. **Evidence labelling:** diagnostics and future UI must identify this as a provisional single-pack model, not exact factory collation.

### Candidate starting weights, transcribed and checked

This table records the referenced collector model, not an approved historical specification.[5]

| Pool | Proposed starting weights | Total before any removal |
| --- | --- | --- |
| Holo | #3,9,11,13,16 weight 8; other booster holos weight 7; #8 excluded | 110 |
| Non-holo Rare | #70,72,73,75,77,78,79 weight 7; remaining non-holo rares weight 8 | 121 |
| Uncommon | #80,82,83,85,86,88,89 weight 3; remaining U weight 4, including Double Colorless #96 | 121 |
| Common, excluding Basic Energy | #67,69,91,92,93,94,95 weight 3; remaining C weight 4 | 121 |
| Basic Energy | #97 Fighting 9, #98 Fire 9, #99 Grass 17, #100 Lightning 35, #101 Psychic 34, #102 Water 17 | 121 |

Energy weights for Fighting/Fire are inferred by omission in the collector model, not individually legible named entries. Rare weights inherit a later-sheet equivalence assumption. The source's chosen 216-pack sequence will **not** be implemented. Check these proposed weights against any later authenticated evidence before calling them final.

## Model B — simpler uniform alternative

Same scope, slots/order, exact independent 1/3 holo choice, C/U no-repeat policy, Energy repeat policy and no-box limitation as Model A, but all eligible identities within each pool start with weight 1. This is easier to explain, but intentionally ignores evidence of unequal sheet multiplicities. It is not more historically accurate merely because it is simpler.

## Separate collection-scope decision

Recommended: conventional 102-number completion with **only the shadowed ©1999 stamped starter Machamp** as a clearly labelled starter-only exception. Its ID must identify that printing/finish and acquisition class rather than pretending it is an unstamped booster card. Qualifying supplied art/physical-match details still require verification. It may later be manually recorded as physically owned; it is never awarded by virtual booster opening. Full binder implementation is Phase 4.

Alternative: strict unstamped target with 101 eligible numbers, leaving #8 outside its denominator. Preserve all 102 source-checklist entries in research either way. The two collection objectives must not be conflated.

## Proposed validation, after approval

Use deterministic RNG and exact weighted-boundary tests; verify slots/counts/eligibility and no C/U identity repeats; allow repeated Basic Energies; confirm no Machamp or excluded printings; prove result/rule immutability and same-input determinism. Batch distribution tests must compare against the **actual selected sampling policy**, including without-replacement inclusion probabilities, with sample size and tolerance specified. Simulation validates implementation against the model, not history. No playable loop is expected in Phase 1.

## Decision log

- **Approved:** Model A, including its stated provisional weights/order/holo/duplicate policies; implemented as version 0.1.0. Model B remains an unselected alternative, not an active question.
- **Unanswered:** conventional 102-number target with the explicit starter Machamp exception, or strict 101-number unstamped target. Booster generation proceeds without Machamp; neither completion denominator is silently adopted.

Model A approval is not approval of Phase 2.

## Sources

[1] https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/cards/en/base1.json — Pokemon TCG Data: Base Set JSON
[2] https://www.pokellector.com/Base-Set-Expansion — Base Set Pokemon Card Set List
[3] https://relentlessdragon.com/pokemon-card-game/base-set-machamp — Identifying Early Pokemon Cards: Base Set Machamp
[4] https://www.elitefourum.com/t/base-set-packs-variations-of-card-sequence/23674 — Base Set Packs variations of card sequence
[5] https://www.elitefourum.com/t/the-english-pokemon-card-rarity-guide/39762 — The English Pokemon card rarity guide
[6] https://www.pokemastercenter.com/pokemon-base-set-guide — The Ultimate Pokemon 1999 WotC Base Set Guide
[9] https://bulbapedia.bulbagarden.net/wiki/Machamp_(Base_Set_8) — Machamp (Base Set 8)
