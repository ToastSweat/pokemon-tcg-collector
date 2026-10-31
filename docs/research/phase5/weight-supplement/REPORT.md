# Jungle/Fossil quantitative sheet-weight supplement

**Outcome:** the supplied numeric charts are legible and support complete, source-qualified per-card weight vectors. They do **not** authenticate factory counts or identify which physical production run the candidate catalogs represent. No generator model is adopted; explicit user approval remains required before using any approximation.

## Source and variant identity

The saved article was actually retrieved at `2026-10-31T20:09:44.840397+00:00`; this supplement reused that pinned snapshot rather than claiming a new article fetch. The four supplied PNGs were fetched successfully on `2026-10-31` at the exact UTC times retained in `retrieval-manifest.json`. Only numeric tables were fetched/viewed, not linked sheet/card photographs. Each PNG's downloaded SHA-1 exactly matches its literal `imageSourceId`; SHA-256, dimensions, bytes and HTTP outcomes are retained.

The article explicitly introduces its work as providing “the best possible estimates,” defines `n` as number of different cards and `m` as magnitude, and lists three Jungle variants immediately before the three supplied Jungle tables.[1]

| Table / literal imageSourceId | Source-qualified attribution |
|---|---|
| J1 `c10b7cdcae9c7f888eea6b72108aca993c11c6bc` | “1) 1st Edition & Unlimited”: Ivy Pikachu removed after cutting, or blank removed; not a promo-emitting Unlimited pool.[1][2] |
| J2 `3a4f6ceeac70e776f92552a6b0097d6c62e3ac4e` | “2) Error version (1st Edition)”: Ivy not removed; **First Edition only**, not the Unlimited candidate.[1][3] |
| J3 `412a4015bff1ef1eb12453acbc355cf7448ab776` | “3) Unlimited (new uncommons sheet)”: blank replaced by Exeggutor; Asia-exclusive distribution explicitly **unknown**.[1][4] |
| F1 `18d87beedd4eb026e3af510c22670f09e3d2ce30` | Fossil post's “1) 1st Edition & Unlimited,” not a fourth Jungle table.[1][5] |

The additional Belgian Jungle variant is discussed in the article but its chart was not among the authorized four and was not inspected. These vectors must not be silently applied to it. J2/J3 explicitly say “other sheets assumed unchanged”; their H/R/C numerical groups are visible, but their exception-name assignments inherit J1 under that **author assumption**.[1][3][4]

## Transcription and independent arithmetic

All values below are `n × m`, followed by the independently calculated position total; these match the printed totals. H/R/U/C mean holo/rare/uncommon/common.[1]

| Table | H | R | U | C |
|---|---|---|---|---|
| J1 | 14×7 + 2×6 = 110 | 9×8 + 7×7 = 121 | 8×8 + 8×7 = **120** | 9×8 + 7×7 = 121 |
| J2 | 14×7 + 2×6 = 110 | 9×8 + 7×7 = 121 | 8×8 + 8×7 + 1×1 = **121** | 9×8 + 7×7 = 121 |
| J3 | 14×7 + 2×6 = 110 | 9×8 + 7×7 = 121 | 9×8 + 7×7 = **121** | 9×8 + 7×7 = 121 |
| F1 | 5×8 + 10×7 = 110 | 1×9 + 14×8 = 121 | 9×8 + 7×7 = 121 | 9×8 + 7×7 = 121 |

The Jungle calculations expand only the three seen Jungle numeric charts.[2][3][4]

The Fossil row comes from the seen F1 numeric chart.[5]

`manual-transcription.json` retains every numeric row, displayed decimal, exception list, footer and pixel-region locator. `sheet-weight-supplement.json` contains **254 mapped ordinary-card rows** across the four tables, plus the single separately recorded J2 promo outcome. `per-card-transcription.md` presents all ordinary catalog numbers/names and their multiplicities. All ordinary rows exactly reconcile to the two frozen candidate catalogs and their retained metadata sources; holo/nonholo identities remain distinct. The sole declared name alias is chart `Nidoran f` → display `Nidoran♀`, source metadata `Nidoran ♀`.

J1's uncommon denominator is **120 emitted positions**, not 121: removed material is not an eligible card.[2]

J2 adds `Pikachu (Promo)` at U1 outside the candidate, not common Jungle Pikachu #60.[3]

J3 changes ordinary Exeggutor #35 from U7 to U8, with the other uncommon assignments unchanged.[2][4]

A reconstructed single removed position is explanatory context, **not an authenticated count of factory blanks or error packs**.[1]

The arithmetic checks every pool's `n` total, `n×m` total, independently summed card rows, the sequence insertion totals and every displayed `p` value within rounding tolerance. The source's 216-pack sequence is an analysis convention, not proof of a case/box cycle; its 0.333/0.667 figures and computed reciprocals are estimates, not verified exact factory odds.[1][2][5]

## One provisional option versus uniform fallback

**Proposal only — `source-qualified-J1-F1-marginal-weights`:** conditionally use **J1 for Jungle and F1 for Fossil**, preserving each card's transcribed magnitude and normalizing only within that source's emitted rarity pool. J1 therefore uses denominators H110/R121/U120/C121; F1 uses H110/R121/U121/C121. No J3 averaging, no Ivy/blank outcome, no unsupported Belgian attribution.[2][5] This is a more faithful representation of these particular secondary tables than uniform weights, not evidence that J1 is the unique ordinary Unlimited production run.

`provisional-option.json` records this **single unapproved weights-only alternative** and the uniform fallback comparison. Uniform pools erase the chart's named frequency differences; they are not the historically demonstrated baseline.[2][5]

**Suitability:** yes for an explicitly labelled, source-qualified **provisional weighting discussion**; no for authenticated factory/box simulation. Branch odds, physical run selection, duplicate policy, replacement, slot order and independence remain unapproved/unresolved. Relative pool-draw mass is not automatically a card's probability of appearing at least once in a multi-card pack; weighted sampling without replacement would change inclusion marginals. No generation algorithm or replacement policy is selected here.

**Verification:** three offline tests pass, including ten corruption subcases and a separate caption/variant-mixing rejection. RED/GREEN logs are retained. The initially mis-scoped Fossil lookup caught its metadata image link before the post body; that failed run is preserved, and the corrected body-scoped lookup passes. Candidate/source input hashes remain unchanged. All new outputs are confined to this folder; no RuleData, runtime, Assets, saves, settings or commits were changed by this task.

## Sources

[1] https://www.elitefourum.com/t/the-english-pokemon-card-rarity-guide/39762 — The English Pokémon card rarity guide — existing retrieved article snapshot
[2] https://efour.b-cdn.net/uploads/default/original/3X/c/1/c10b7cdcae9c7f888eea6b72108aca993c11c6bc.png — Numeric rarity table c10b7cdcae9c7f888eea6b72108aca993c11c6bc
[3] https://efour.b-cdn.net/uploads/default/original/3X/3/a/3a4f6ceeac70e776f92552a6b0097d6c62e3ac4e.png — Numeric rarity table 3a4f6ceeac70e776f92552a6b0097d6c62e3ac4e
[4] https://efour.b-cdn.net/uploads/default/original/3X/4/1/412a4015bff1ef1eb12453acbc355cf7448ab776.png — Numeric rarity table 412a4015bff1ef1eb12453acbc355cf7448ab776
[5] https://efour.b-cdn.net/uploads/default/original/3X/1/8/18d87beedd4eb026e3af510c22670f09e3d2ce30.png — Numeric rarity table 18d87beedd4eb026e3af510c22670f09e3d2ce30
