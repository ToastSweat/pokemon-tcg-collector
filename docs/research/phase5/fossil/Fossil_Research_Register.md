# English Fossil Unlimited — Phase 5 research register

Research checkpoint: **2026-10-31** (host clock verified, UTC-05:00). Status: **candidate data only; no printing/completion target, generator model, or implementation approval recorded**.

## Current process and projected changes

Accepted Base Set runtime, RNG, ordered results, persistence, audio and art remain untouched. This work produces an offline Authoring catalog, research evidence, a complete crosscheck, and Python verification only. It does not admit Fossil to the live game. No RuleData, runtime slot array, RNG implementation, live save, settings, build, asset import, commit or upload is part of this checkpoint.

The proposed target is English, unstamped Unlimited, ordinary ©1999 Fossil, standard non-holos and usual Starlight/Galaxy-family holos; the exact foil terminology is not normalized into a newly invented physical variant. Bulbapedia calls the original treatment Starlight and later also contrasts Cosmos with the usual Galaxy foil.[3] `fossil-en-unlimited1999` is an **authoring target identifier**, not a claim that the set-level API identified an individual physical card's copyright, geography, stamp or foil. The API's `legalities.unlimited` is a gameplay-format legality, not evidence of Unlimited edition.[1]

## Sources, retrieval and provenance

Raw metadata/text HTML/JSON, extracted text and their hashes are in `evidence/` and `evidence-sha256.json`. Retrieval manifests preserve actual access timestamps, URLs, HTTP outcomes and raw response hashes. No image, wrapper art, logo, music or cry was downloaded. HTML/JSON may contain image URLs; those references were not fetched.

`citations-ledger.json` is this Fossil worker's workspace-local ledger. `quote-verification.json` preserves the initial quote checks, including one rejected two-word API fragment. `quote-correction.json` records its successful replacement with a longer exact API fragment. No failed quote is represented as accepted evidence.

The checklists are independently maintained sources, not three proofs of a physical printing: Pokémon TCG data repository, Bulbapedia, and Pokellector.[1][2][3] Common underlying source dependence cannot be ruled out. Their independence is publisher/editorial independence, not demonstrated archival independence. The WotC chat evidence is a **community-hosted reproduction of attributed period statements**, not a verified original WotC page or an authenticated factory document.[8]

### Actual network limitations

- Direct Bulbapedia booster article and its raw-wikitext alternative returned **403**. `web_extract` recovered an era-level count paragraph; the exact supporting paragraph is preserved in `evidence/booster-extracted.txt`.[7]
- Wayback availability API located snapshots for the booster article and original Wizards Fossil checklist, but fetching both snapshots returned **429**. Availability metadata alone was not treated as having read those pages. Archive.ph fallback also returned **429**.
- Google returned HTTP 200 interstitials, not usable results. DuckDuckGo returned HTTP 202 bot challenge. Bing returned broad first-token results that did not answer the query even after quoted refinements. These are retained as retrieval attempts, not evidence of composition/order.
- TCGplayer extraction returned an unknown-fetch error; Cardmarket extraction timed out. The attempted TCGplayer product ID was exploratory and was not authenticated; neither is cited as product evidence.
- Alternative Discourse JSON retrieval and focused search worked, yielding the Fossil rarity-guide post and reproduced WotC chats. Searches for a Fossil card trick/order did not yield a direct orientation observation. No video was watched, no pack was physically opened, and no factory sequence was recovered.

## Claims register

Every row below scopes the claim separately. “Candidate implementation” means a proposal or authoring-field mapping, **not a live rule**. All external sources were accessed 2026-10-31; exact per-request UTC timestamps live in the retrieval manifests.

| Claim ID | Exact claim / question | Scope | Evidence / exact supporting excerpt | Confidence | Candidate implementation | Limitation / unresolved decision |
|---|---|---|---|---|---|---|
| F01 | English numbered checklist has 62 entries, 1–62, with a unique collector number per entry. | English Fossil set-level list, not printing-resolved | Complete retrieved API and both checklists; `card-crosscheck.json` contains every row.[1][2][3] | Corroborated metadata | 62 proposed standard completion slots | Does not prove which physical foil/date/stamp each API record depicts. Completion target is not approved. |
| F02 | #1–15 are Rare Holo Pokémon; #16–30 are Rare non-holo Pokémon, with the same 15 names in the same order. | English checklist | API `"rarity": "Rare Holo"`; independent English table labels `Rare Holo` vs `Rare`.[1][3] | Corroborated metadata | 15 `holo`/`holo`, 15 `rare`/`nonholo` identities | Finish mapped from rarity, not authenticated image inspection. No shared ID between different numbers/finishes. |
| F03 | #31–45 plus #58 Mr. Fuji are uncommon; #46–57 plus #59–62 are common. | English checklist | Full per-number rarity match in independent English table and API.[1][3] | Corroborated metadata | 16 uncommon, 16 common | Rarity classes do not establish equal pull weights. |
| F04 | #1–57 are Pokémon; #58–62 are Trainer cards, including Energy Search and Mysterious Fossil. | Catalog classification, not in-play rules | Independent table marks trainers `T`; API marks `Trainer`.[1][3] | Corroborated metadata | 57 `pokemon`, 5 `trainer` | A Trainer mentioning energy is not an Energy card; Mysterious Fossil's in-play behavior does not change its catalog kind. |
| F05 | Ordinary target excludes First Edition, stamped promos, other languages, Cosmos foil, Australian/red-logo/date variants and gift-box-specific date printings. | Proposed ©1999 English Unlimited standard target | Bulbapedia distinguishes editions, Prerelease Aerodactyl, W Kabuto, Cosmos and date variants.[3] | Source-documented distinctions; target boundary is proposed | Only `variant=standard`, `edition=unlimited`; no extra promo/error/date IDs | Exact geography/run not authenticated from API. Exclusion is a scope policy, not a statement that these variants never existed. |
| F06 | Red-logo Australian packs' non-holos have ©1999–2000; their holos can still carry ©1999. | Australian red-logo products only | “The non-holographic cards obtained from these red logo packs have a 1999-2000 copyright…”; “the holographic cards … have the standard 1999 copyright…”; forum opening report: “holos do NOT have the 1999-2000 copyright date.”[3][5] | Corroborated reported observation; Bulbapedia partly cites this forum | Exclude red-logo run, not merely filter by holo year | ©1999 alone cannot establish that a holo is from the chosen ordinary distribution. Sources are not fully independent here. |
| F07 | International boosters in this era contain 11 cards. | Broad Base Set–Neo Destiny international era, including Fossil | “From the first Base Set through to Neo Destiny expansion, internationally released boosters contained 11 cards.”[7] | Documented secondary era-level statement, supported by reproduced period chat | Proposed total 11 | Not a retrieved scan of a particular Fossil wrapper. Mispack/error exceptions are not modeled. |
| F08 | Proposed composition: 7 common + 3 uncommon + 1 rare-or-holo. | Proposed ordinary English Fossil Unlimited | April 6, 2000 reproduced WotC answer about **Team Rocket**, not Fossil: “Should have the same odds as usual, 1 rare, 3 uncommon, 7 commons.”[8] | Era-supported inference, **not Fossil-specific proof** | Seven common draws, three uncommon draws, one rare branch | Do not relabel the quoted list as physical stack order. A Fossil-specific composition observation remains desirable. |
| F09 | Checklist has no basic or special Energy cards; no dedicated Energy allocation is proposed. | Fossil numbered set and proposed packs | Both full lists classify all 62 as Pokémon/Trainer, including Energy Search.[1][3] | Checklist absence corroborated; pack allocation inferred | Zero basic-energy/special-energy slots | A checklist alone cannot exclude cross-set inserted cards. No direct observation establishing Fossil's energy allocation was retrieved. **Do not import Base Set's Energy slots.** |
| F10 | Physical stack order and front/back-wrapper orientation are unknown from retrieved evidence. | Exact ordinary English Fossil Unlimited pack, opened without rearrangement | No retrieved textual source establishes orientation or a position-by-position original stack | **Unknown** | No historical order can yet be asserted | Requires a traceable untouched-pack observation specifying wrapper front/back, card faces/backs, removal direction and each position; modern card tricks are not evidence. |
| F11 | About one foil in three packs has period-era support; exact population probability for chosen Fossil run is unverified. | General WotC era, not run-resolved Fossil | Feb 3, 2000 reproduced WotC answer: “Basically a 1 in 3 chance of a foil card in a booster pack.”[8] | Attributed historical approximation | Proposed exact 1/3 branch **only if approved** | Generic statement responding to Base Set 2/Rocket comparison; no Fossil-run denominator/sample, no guarantee per box. |
| F12 | A holo is proposed to replace the non-holo rare, not add a twelfth card or replace a common. | Proposed Fossil model | Era-level 11-card count and single-rare composition support the proposal, but do not directly establish this Fossil replacement mechanism.[7][8] | Inference / unapproved model policy | Mutually exclusive one-card rare/holo branch | A direct Fossil pack observation was not retrieved. No silent reuse of another set's replacement behavior. |
| F13 | Sheet-based rarity differences are plausible; equal rarity-symbol weights are not historically established. | English Fossil, sheet/run detail unresolved | Guide: “Three of the four English sheets (holo, rares, uncommons) are known, as is a German commons sheet”; it says English commons were checked against openings, and cards occur on sheets differing numbers of times.[4] | Named secondary research; per-card weights **unverified here** | Provisional uniform pools require explicit approximation approval | Table is image-only. No table/sheet image was downloaded or counted. “No artificial rarity differences” is not “every card equally frequent.”[4] |
| F14 | There were multiple dated Unlimited Fossil print runs. | Production-run provenance, not card copyright | Sheet catalog: “Unlimited Rare - 4th Run … Date: 11/1/99 …”; also lists separate uncommon runs.[6] | Secondary inventory transcription | Do not pretend one factory sheet sequence applies to all Unlimited | Did not authenticate the pictured sheets. Sheet production date is not a card's copyright variant. |
| F15 | Duplicate restrictions within a pack are not established. | Same ID versus same name; ordinary Fossil packs | No retrieved source states a universal prohibition | **Unknown** | Proposed no repeated ID within common/uncommon pool per pack | This is a chosen approximation, not “factory guarantees no duplicates.” Sheet sequences may behave differently. |
| F16 | Individual-pack marginals do not establish independent packs, a fixed holo count per box, or wrapper-linked odds. | Repeated openings and sealed boxes | Reproduced WotC reply: “Some people get better odds, some get worse”; guide discusses sequence/sheet mechanisms.[8][4] | Correlation structure **unknown** | Proposed independent single-pack simulation, **not a sealed-box simulator** | Do not guarantee 12 holos/36 packs, periodic every-third-pack hits, no-repeat holos, or art-specific rates. The reproduced customer's every-four-pack claim is not an endorsed factory rule. |
| F17 | English pack art subjects listed in retrieved text are Lapras, Aerodactyl and Zapdos. | English Fossil gallery, not proven per-run wrapper variant | “English Lapras pack”; “English Aerodactyl pack”; “English Zapdos pack”.[3] | Documented gallery captions | Historical subject references only; approved temporary designs remain replaceable A/B/C | No images fetched; no claim that all depicted wrappers are the exact selected Unlimited/date run. Wrapper presentation must not consume generation RNG. |
| F18 | Cosmos release provenance is unknown; Aerodactyl Prerelease and other promos are additional variants, not standard numbered completion additions. | English Fossil variants | “How these English Cosmos holofoil Fossil cards were released is unknown…”; additional cards table lists Prerelease Aerodactyl, W Kabuto and gift-box entries.[3] | Documented secondary distinctions; rumor not promoted to fact | Excluded from this proposed 62-slot target | Do not infer ordinary booster eligibility from shared collector number or API image URLs. |
| F19 | Haunter/Zapdos/Krabby corrected-error distinctions exist in secondary documentation. | Fossil error/correction subvariants | Bulbapedia lists Haunter art stain, Zapdos foil cutout and incomplete Krabby set symbol.[3] | Documented secondary list, physical scope unresolved | No error chase slots or simulated error odds in candidate | `standard` is not an assertion that each source image is the corrected form. Decide whether ordinary errors are tolerated under the same completion slot before physical matching/shopping. |

## One concrete provisional model — NOT APPROVED, NOT IMPLEMENTED

Proposal identifier for discussion only: **fossil-en-unlimited1999-singlepack-provisional-v1**. This is prose, deliberately not a RuleData-compatible rules file.

1. Choose one ordinary 11-card pack, not a box. Candidate catalog's 62 entries are proposed eligible; promos, alternate foils/date runs and errors-as-chase-variants are outside this model.
2. Draw **7 distinct common IDs uniformly without replacement** from #46–57 and #59–62, and **3 distinct uncommon IDs uniformly without replacement** from #31–45 and #58. These duplicate/weight choices are consequential approximations; sheets could yield different marginal and joint probabilities.
3. With **exact probability 1/3**, use one uniformly selected holo ID #1–15. Otherwise use one uniformly selected non-holo rare ID #16–30. The holo **replaces that rare**. No Energy slot, extra card, pity, guaranteed new card or adaptive collection weighting.
4. Use the explicitly artificial first-facing/display sequence **C1 C2 C3 C4 C5 C6 C7 R* U1 U2 U3**, preserving the within-pool draw sequence. This is a concrete candidate order, **not a verified physical stack or seal orientation**. It must be labeled provisional; no modern card trick is silently applied. The unknown physical order is not resolved by naming this sequence.
5. Reset the without-replacement selection for each pack. Packs are independent in the proposal. No factory sheet stepping, box memory, case mapping, fixed holo count or wrapper-art correlations are promised.
6. A/B/C temporary wrapper selection remains presentation-only and outside generation RNG. Historical text supports the three subjects but does not supply art or authenticate run-specific wrapper bindings.[3]

Uniform weights alter per-card frequencies if the reported sheets are representative; without-replacement sampling changes same-pack co-occurrence and forbids duplicate IDs; independent packs erase manufacturing/box correlations. An exact 1/3 probability turns an attributed approximation into a precise simulation input. The proposed display order may place the rare at the wrong moment. These are substantive fidelity choices, not implementation defaults.

### Approval decisions required before runtime rule work

| Decision | Proposed choice | Current decision |
|---|---|---|
| FA1 — physical/completion scope | 62 ordinary ©1999 English Unlimited standard slots; exclude First Edition, Cosmos, Australian/red-logo/date variants, Prerelease/W/gift-box variants; no separate error checklist | **Awaiting user approval**; physical-printing match still unverified |
| FA2 — composition / allocation / replacement | 11 cards, 7C + 3U + one mutually exclusive R/H, no dedicated Energy | **Awaiting user approval** of era-supported inferences |
| FA3 — rate and weighting | Exact 1/3 branch, uniform weights within all four pools despite unresolved sheet multiplicities | **Awaiting explicit approximation approval** |
| FA4 — duplicate semantics | No repeated common/uncommon ID within a pack; reset each pack | **Awaiting explicit approximation approval** |
| FA5 — order/orientation | Artificial C×7, R*, U×3 display sequence while historic physical orientation remains unknown | **Awaiting explicit approximation approval or better evidence** |
| FA6 — collation scope | Independent single packs only; no sealed box simulation or fixed holo count | **Awaiting explicit approximation approval** |
| FA7 — temporary wrappers | Three distinguishable replaceable A/B/C designs, odds-independent | **Already authorized by Phase_5_Discovery.md**; no new blanket art approval needed; this worker did not implement them |

No entry in this table approves another entry by implication. In particular, permission to continue Phase 5 or use temporary art is not approval of FA1–FA6.

## Delivered data and actual verification

- `Authoring/Phase5/Data/fossil-en-unlimited1999.catalog.json`: schemaVersion 1; exact `CatalogData`/`CardData` field names; `completionTargetStatus=proposed-not-approved`; placeholder art only.
- `Fossil_Checklist.md`: all 62 rows, including separate holo/non-holo names and trainer classification.
- `card-crosscheck.json`: all 62 API/Bulbapedia normalized rows and matched Pokellector plaques. API↔Bulbapedia compares number, name, rarity, supertype; finish follows the holo/non-holo rarity mapping. Pokellector supplies number/name only, not claimed independent rarity verification.
- `catalog-verification.json`: actual executed coverage, uniqueness, rarity/kind/finish totals, eligibility, input hashes and candidate hash. Exact physical printing is explicitly false/unestablished.
- `TestsPhase5/FossilCatalogTests.py` and `tools/prepare_fossil_catalog.py`: stdlib-only, six vertical RED→GREEN cycles retained as `tdd-01` through `tdd-06` logs. Final suite ran **6 tests, all passed**. Negative cases cover missing/extra/noncanonical numbers, independent metadata disagreements, missing third source, mutated catalog fields/IDs/approval/eligibility and bool/int/float coercion. A test checks the real, read-only `Domain/Data.cs` field declarations; no Assets file is modified.

Commands actually executed from the project root:

```text
python -B TestsPhase5/FossilCatalogTests.py
python -B tools/prepare_fossil_catalog.py --check
```

Expected/checkpoint totals, established by those executions: **62 unique IDs; 15 holo + 15 rare + 16 uncommon + 16 common; 57 Pokémon + 5 Trainers; 15 holo + 47 nonholo; 0 Energy records; 62 proposed eligible entries**. No Unity/editor/player, RNG distribution, mixed-set persistence, order authenticity or gameplay test was performed; this is not Phase 5 playable acceptance.

### Reuse notes

For future set research, keep set-level metadata checks separate from exact physical-printing admission. Preserve raw sources and hashes before normalization; compare every row, not just group totals; retain original rarity symbols/alt text; require literal canonical collector strings; distinguish Energy Search from Energy; never convert a source's rarity ordering into a factory stack; do not equate a historical approximate marginal with box independence. Use the authoring CLI only for this frozen Fossil evidence snapshot, not as a generic set importer.

## Sources

[1] https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/cards/en/base3.json — api
    > ""rarity": "Rare Holo""
    > ""id": "base3-1",
    "name": "Aerodactyl",
    "supertype": "Pokémon","
[2] https://www.pokellector.com/Fossil-Expansion — pokellector
    > "#62 - Mysterious Fossil"
[3] https://bulbapedia.bulbagarden.net/wiki/Fossil_(TCG) — bulbapedia
    > "The non-holographic cards obtained from these red logo packs have a 1999-2000 copyright at the bottom of the card as well as a different card stock."
    > "Note that the holographic cards obtained from these red logo packs have the standard 1999 copyright at the bottom."
    > "Fossil was the final expansion to use the Starlight Holofoil."
    > "English Lapras pack"
    > "English Aerodactyl pack"
    > "English Zapdos pack"
    > "Pokémon League Prerelease promo (Gold Version)"
    > "How these English Cosmos holofoil Fossil cards were released is unknown, but rumors state they were leaked by WotC employees."
[4] https://www.elitefourum.com/t/the-english-pokemon-card-rarity-guide/39762 — rarity-guide
    > "Three of the four English sheets (holo, rares, uncommons) are known, as is a German commons sheet."
    > "Verification with box opening videos shows the English sheet to be the same as the German one."
    > "There are no artificial rarity differences, and the choice which cards appear on the sheet more often seems random."
[5] https://www.elitefourum.com/t/pokemon-fossil-1999-2000-australian-3rd-print-run/11344 — australian
    > "We opened packs and found that holos do NOT have the 1999-2000 copyright date."
[6] https://pokemuseum.weebly.com/uncut-sheets.html — sheets
    > "Unlimited Rare - 4th Run: Company: PBM Graphics, Date: 11/1/99 - Form 4, Job # W06071110 03 140, PBM # 81647"
[7] https://bulbapedia.bulbagarden.net/wiki/Booster_pack_(TCG) — Booster pack (TCG); recovered extraction after direct 403
    > "From the first Base Set through to Neo Destiny expansion, internationally released boosters contained 11 cards."
[8] https://www.elitefourum.com/t/wotc-chat-logs-from-feb-2000-to-nov-2000-q-a-w-employees/16872.json — Community reproduction of WotC chat logs, Feb-Nov 2000
    > "Will there be better odds of getting foils in Base Set 2 and Rocket? No, the same odds. Basically a 1 in 3 chance of a foil card in a booster pack."
    > "When the cards are printed, the number of foils is 1 in 3 packs. Some people get better odds, some get worse."
    > "What is the pack design of TR? Like 11 cards. 1 Rare, 2 Rare, Etc. Should have the same odds as usual, 1 rare, 3 uncommon, 7 commons."
