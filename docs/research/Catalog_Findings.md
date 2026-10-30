# Catalog findings — 1999 English Base Set Unlimited

**Phase 1 research only. Accessed 2026-10-30** (local clock verified, UTC−05:00). Descriptive source labels below are for the parent's citation ledger, not invented global citation IDs. No generator, probabilities, runtime schema/IDs, Unity launch, C# or git actions are included.

## Outcomes and printing boundary

- The **set-level checklist is 102 distinct collector numbers, 1/102–102/102**. Both the saved Pokémon TCG Developers JSON and an independently retrieved Pokellector checklist contain every number exactly once. This is checklist coverage, **not certification of 102 physical 1999 Unlimited printings**. [Catalog JSON]; [Pokellector checklist].
- The target is **English, shadowed, 1999 Unlimited**, not every card labelled Base Set. The Wizards copyright must be **©1999**, not **©1999–2000**; the First Edition and unstamped Shadowless layouts are excluded. Pokémon art-window shadows and thicker HP/attack text distinguish the Unlimited layout. Absence of an edition stamp alone is insufficient to exclude Shadowless. [Bulbapedia Base Set]; [Relentless Dragon identification].
- **Do not apply a Pokémon-shadow test to Trainers/Energy.** The identification guide says shadows were used on Pokémon themselves and not trainer cards; copyright/layout details must be appropriate to card kind. [Relentless Dragon identification].
- **Machamp #8 is a blocking scope decision.** The ordinary shadowed Base Set Machamp retains a 1st Edition stamp; Bulbapedia says an unstamped Unlimited Base Set version was never officially released. Keep #8 in the 102-number research checklist but do not silently include it under a literal “exclude all First Edition-stamped cards” rule. A strict unstamped collection would have **101 eligible number slots**; a conventional 102-number set needs an explicitly approved Machamp exception and verification of the qualifying starter print. No exception is approved here. [Bulbapedia Machamp]; [Relentless Dragon Machamp].
- **A concrete metadata error for this printing:** primary JSON #73 is **Impostor Professor Oak**, whereas the Unlimited spelling is **Imposter Professor Oak**. Bulbapedia explicitly assigns “Impostor” to 1st Edition/Shadowless and “Imposter” to subsequent prints; Pokellector agrees with the latter. The raw source name is preserved, not silently corrected. [Catalog JSON]; [Bulbapedia Oak]; [Pokellector checklist].

### Copyright strings and limitations

These are verbatim website transcriptions from [Relentless Dragon identification], **not transcriptions of downloaded card art**:

| Printing | Copyright substring quoted by the guide | Treatment |
| --- | --- | --- |
| Shadowless | `© 1995, 96, 98, 99 Nintendo, Creatures, GAMEFREAK. © 1999 Wizards.` | Excluded, stamped or unstamped |
| Standard Unlimited | `© 1995, 96, 98 Nintendo, Creatures, GAMEFREAK. ©1999 Wizards.` | Intended target, subject to per-card verification |
| Later date variant | `©1995, 96, 98 Nintendo, Creatures, GAMEFREAK. ©1999-2000 Wizards.` | Excluded pending separate research |

Spacing is the website's transcription; this research does not certify physical-card typography or line breaks. “1999” is a printing discriminator, not proof of a manufacturing day. Pokellector's January 9, 1999 date is **set-level** metadata, not proof of an Unlimited-run release date.

**Run-count terminology is not settled factory history.** Bulbapedia describes four English print categories (1st Edition, Shadowless, Unlimited, ©1999–2000 Unlimited); the retailer guide reports six production printings within Unlimited. The sources agree on the copyright/layout distinction, not on an actual manufacturing-run count. Do not make “third/fourth printing”, reported quantities, or alleged geography into identity or probability rules. [Bulbapedia Base Set]; [Relentless Dragon identification].

## Metadata evidence, not runtime authoring data

- `docs/research/evidence/catalog/card_metadata.json`: **102 primary card records**. Source card keys/values are preserved, including source `id`, `number`, `name`, `supertype`, `subtypes`, Pokémon `types`, card text and `rarity` where supplied. Only `images` was removed; no image bytes were fetched. A provenance wrapper stores exact source URL/access timestamp/original-response SHA-256 and scope cautions. `base1-*` remains a source identifier, not a chosen runtime ID. [Catalog JSON].
- `docs/research/evidence/catalog/tcgdex_supplement.json`: **11 overlapping supplementary records**, #8, #31, #58, #73 and #96–102, with per-record URLs/access timestamps/hashes. Original fields are preserved except recursively removed media URL keys (`image`, `logo`, `symbol`) and `pricing`. These are not additional checklist cards. All exact endpoint URLs appear below. [TCGdex supplement].
- The primary source **omits rarity on #97–102**. TCGdex explicitly calls all six `Common`; the research table marks those independently supplied values with ‡. No missing primary field was invented. TCGdex describes Machamp rarity as `Rare` plus `variants.holo:true`, whereas the primary combines that as `Rare Holo`; source vocabularies are not identical. [Catalog JSON]; [TCGdex supplement].
- Primary Energy-card objects **do not supply `types`**. Their supplied-energy labels below follow source names, corroborated by Bulbapedia's Energy table. TCGdex `energyType` is `Normal` for Basic Energy and `Special` for Double Colorless Energy: **Normal is not an elemental type called Colorless**. No `types` property was fabricated for Energy objects. [Catalog JSON]; [TCGdex supplement]; [Bulbapedia Energy].
- `legalities.unlimited` means legality in the **Unlimited tournament format**, not an Unlimited-edition printing. Never use it as a physical-print filter. [API card schema].
- The repository now identifies itself as legacy/deprecated and says the API is scheduled offline **March 1, 2027**. This historical evidence snapshot does not endorse a new runtime dependency. [Catalog README].

## Independent checklist comparison

The saved `pokellector.txt` was parsed and compared by collector number: **102 unique numbers in each; zero missing/extra numbers**. Seven raw name strings differ:

| Number | Primary JSON, preserved | Pokellector | Finding |
| --- | --- | --- | --- |
| 55 | Nidoran ♂ | Nidoran M | Symbol versus textual male marker |
| 73 | Impostor Professor Oak | Imposter Professor Oak | Actual printing-linked name change, not just normalization |
| 76 | Pokémon Breeder | Pokemon Breeder | Accent rendering |
| 77 | Pokémon Trader | Pokemon Trader | Accent rendering |
| 85 | Pokémon Center | Pokemon Center | Accent rendering |
| 86 | Pokémon Flute | Pokemon Flute | Accent rendering |
| 87 | Pokédex | Pokedex | Accent rendering |

All other number/name pairs match exactly. The list comparison verifies number/name coverage, **not all rarities or exact physical prints**. Sources: [Catalog JSON]; [Pokellector checklist]; #73 interpretation: [Bulbapedia Oak].

## Complete collector-number checklist

This is a research view, not a runtime schema. Values are from [Catalog JSON] except † #73's printing-specific name from [Bulbapedia Oak] and ‡ the six Basic Energy rarities from [TCGdex supplement]. Pokémon type uses source `types`; Energy supplied uses the source name and [Bulbapedia Energy]. `Rare Holo` identifies the baseline holo entries; non-holo is the remaining baseline checklist, not permission to create an alternate finish for every card.

Calculated from saved primary evidence: **69 Pokémon, 26 Trainers, 7 Energy cards**; **16 Rare Holo, 16 Rare, 32 Uncommon, 32 Common, and 6 omitted rarities**. The independent Energy supplement makes a combined view of 38 Common. **16 holo / 86 non-holo** includes Machamp and describes the 102-number checklist, not pack odds or an approved strict-print completion denominator.

| No. | Name (Unlimited research view) | Kind | Pokémon type / Energy supplied | Rarity | Finish |
| --- | --- | --- | --- | --- | --- |
| 1/102 | Alakazam | Pokémon / Stage 2 | Psychic | Rare Holo | Holo |
| 2/102 | Blastoise | Pokémon / Stage 2 | Water | Rare Holo | Holo |
| 3/102 | Chansey | Pokémon / Basic | Colorless | Rare Holo | Holo |
| 4/102 | Charizard | Pokémon / Stage 2 | Fire | Rare Holo | Holo |
| 5/102 | Clefairy | Pokémon / Basic | Colorless | Rare Holo | Holo |
| 6/102 | Gyarados | Pokémon / Stage 1 | Water | Rare Holo | Holo |
| 7/102 | Hitmonchan | Pokémon / Basic | Fighting | Rare Holo | Holo |
| 8/102 | Machamp [scope pending] | Pokémon / Stage 2 | Fighting | Rare Holo | Holo |
| 9/102 | Magneton | Pokémon / Stage 1 | Lightning | Rare Holo | Holo |
| 10/102 | Mewtwo | Pokémon / Basic | Psychic | Rare Holo | Holo |
| 11/102 | Nidoking | Pokémon / Stage 2 | Grass | Rare Holo | Holo |
| 12/102 | Ninetales | Pokémon / Stage 1 | Fire | Rare Holo | Holo |
| 13/102 | Poliwrath | Pokémon / Stage 2 | Water | Rare Holo | Holo |
| 14/102 | Raichu | Pokémon / Stage 1 | Lightning | Rare Holo | Holo |
| 15/102 | Venusaur | Pokémon / Stage 2 | Grass | Rare Holo | Holo |
| 16/102 | Zapdos | Pokémon / Basic | Lightning | Rare Holo | Holo |
| 17/102 | Beedrill | Pokémon / Stage 2 | Grass | Rare | Non-holo |
| 18/102 | Dragonair | Pokémon / Stage 1 | Colorless | Rare | Non-holo |
| 19/102 | Dugtrio | Pokémon / Stage 1 | Fighting | Rare | Non-holo |
| 20/102 | Electabuzz | Pokémon / Basic | Lightning | Rare | Non-holo |
| 21/102 | Electrode | Pokémon / Stage 1 | Lightning | Rare | Non-holo |
| 22/102 | Pidgeotto | Pokémon / Stage 1 | Colorless | Rare | Non-holo |
| 23/102 | Arcanine | Pokémon / Stage 1 | Fire | Uncommon | Non-holo |
| 24/102 | Charmeleon | Pokémon / Stage 1 | Fire | Uncommon | Non-holo |
| 25/102 | Dewgong | Pokémon / Stage 1 | Water | Uncommon | Non-holo |
| 26/102 | Dratini | Pokémon / Basic | Colorless | Uncommon | Non-holo |
| 27/102 | Farfetch'd | Pokémon / Basic | Colorless | Uncommon | Non-holo |
| 28/102 | Growlithe | Pokémon / Basic | Fire | Uncommon | Non-holo |
| 29/102 | Haunter | Pokémon / Stage 1 | Psychic | Uncommon | Non-holo |
| 30/102 | Ivysaur | Pokémon / Stage 1 | Grass | Uncommon | Non-holo |
| 31/102 | Jynx | Pokémon / Basic | Psychic | Uncommon | Non-holo |
| 32/102 | Kadabra | Pokémon / Stage 1 | Psychic | Uncommon | Non-holo |
| 33/102 | Kakuna | Pokémon / Stage 1 | Grass | Uncommon | Non-holo |
| 34/102 | Machoke | Pokémon / Stage 1 | Fighting | Uncommon | Non-holo |
| 35/102 | Magikarp | Pokémon / Basic | Water | Uncommon | Non-holo |
| 36/102 | Magmar | Pokémon / Basic | Fire | Uncommon | Non-holo |
| 37/102 | Nidorino | Pokémon / Stage 1 | Grass | Uncommon | Non-holo |
| 38/102 | Poliwhirl | Pokémon / Stage 1 | Water | Uncommon | Non-holo |
| 39/102 | Porygon | Pokémon / Basic | Colorless | Uncommon | Non-holo |
| 40/102 | Raticate | Pokémon / Stage 1 | Colorless | Uncommon | Non-holo |
| 41/102 | Seel | Pokémon / Basic | Water | Uncommon | Non-holo |
| 42/102 | Wartortle | Pokémon / Stage 1 | Water | Uncommon | Non-holo |
| 43/102 | Abra | Pokémon / Basic | Psychic | Common | Non-holo |
| 44/102 | Bulbasaur | Pokémon / Basic | Grass | Common | Non-holo |
| 45/102 | Caterpie | Pokémon / Basic | Grass | Common | Non-holo |
| 46/102 | Charmander | Pokémon / Basic | Fire | Common | Non-holo |
| 47/102 | Diglett | Pokémon / Basic | Fighting | Common | Non-holo |
| 48/102 | Doduo | Pokémon / Basic | Colorless | Common | Non-holo |
| 49/102 | Drowzee | Pokémon / Basic | Psychic | Common | Non-holo |
| 50/102 | Gastly | Pokémon / Basic | Psychic | Common | Non-holo |
| 51/102 | Koffing | Pokémon / Basic | Grass | Common | Non-holo |
| 52/102 | Machop | Pokémon / Basic | Fighting | Common | Non-holo |
| 53/102 | Magnemite | Pokémon / Basic | Lightning | Common | Non-holo |
| 54/102 | Metapod | Pokémon / Stage 1 | Grass | Common | Non-holo |
| 55/102 | Nidoran ♂ | Pokémon / Basic | Grass | Common | Non-holo |
| 56/102 | Onix | Pokémon / Basic | Fighting | Common | Non-holo |
| 57/102 | Pidgey | Pokémon / Basic | Colorless | Common | Non-holo |
| 58/102 | Pikachu | Pokémon / Basic | Lightning | Common | Non-holo |
| 59/102 | Poliwag | Pokémon / Basic | Water | Common | Non-holo |
| 60/102 | Ponyta | Pokémon / Basic | Fire | Common | Non-holo |
| 61/102 | Rattata | Pokémon / Basic | Colorless | Common | Non-holo |
| 62/102 | Sandshrew | Pokémon / Basic | Fighting | Common | Non-holo |
| 63/102 | Squirtle | Pokémon / Basic | Water | Common | Non-holo |
| 64/102 | Starmie | Pokémon / Stage 1 | Water | Common | Non-holo |
| 65/102 | Staryu | Pokémon / Basic | Water | Common | Non-holo |
| 66/102 | Tangela | Pokémon / Basic | Grass | Common | Non-holo |
| 67/102 | Voltorb | Pokémon / Basic | Lightning | Common | Non-holo |
| 68/102 | Vulpix | Pokémon / Basic | Fire | Common | Non-holo |
| 69/102 | Weedle | Pokémon / Basic | Grass | Common | Non-holo |
| 70/102 | Clefairy Doll | Trainer | — | Rare | Non-holo |
| 71/102 | Computer Search | Trainer | — | Rare | Non-holo |
| 72/102 | Devolution Spray | Trainer | — | Rare | Non-holo |
| 73/102 | Imposter Professor Oak † | Trainer | — | Rare | Non-holo |
| 74/102 | Item Finder | Trainer | — | Rare | Non-holo |
| 75/102 | Lass | Trainer | — | Rare | Non-holo |
| 76/102 | Pokémon Breeder | Trainer | — | Rare | Non-holo |
| 77/102 | Pokémon Trader | Trainer | — | Rare | Non-holo |
| 78/102 | Scoop Up | Trainer | — | Rare | Non-holo |
| 79/102 | Super Energy Removal | Trainer | — | Rare | Non-holo |
| 80/102 | Defender | Trainer | — | Uncommon | Non-holo |
| 81/102 | Energy Retrieval | Trainer | — | Uncommon | Non-holo |
| 82/102 | Full Heal | Trainer | — | Uncommon | Non-holo |
| 83/102 | Maintenance | Trainer | — | Uncommon | Non-holo |
| 84/102 | PlusPower | Trainer | — | Uncommon | Non-holo |
| 85/102 | Pokémon Center | Trainer | — | Uncommon | Non-holo |
| 86/102 | Pokémon Flute | Trainer | — | Uncommon | Non-holo |
| 87/102 | Pokédex | Trainer | — | Uncommon | Non-holo |
| 88/102 | Professor Oak | Trainer | — | Uncommon | Non-holo |
| 89/102 | Revive | Trainer | — | Uncommon | Non-holo |
| 90/102 | Super Potion | Trainer | — | Uncommon | Non-holo |
| 91/102 | Bill | Trainer | — | Common | Non-holo |
| 92/102 | Energy Removal | Trainer | — | Common | Non-holo |
| 93/102 | Gust of Wind | Trainer | — | Common | Non-holo |
| 94/102 | Potion | Trainer | — | Common | Non-holo |
| 95/102 | Switch | Trainer | — | Common | Non-holo |
| 96/102 | Double Colorless Energy | Energy / Special | Colorless ×2 | Uncommon | Non-holo |
| 97/102 | Fighting Energy | Energy / Basic | Fighting | Common ‡ | Non-holo |
| 98/102 | Fire Energy | Energy / Basic | Fire | Common ‡ | Non-holo |
| 99/102 | Grass Energy | Energy / Basic | Grass | Common ‡ | Non-holo |
| 100/102 | Lightning Energy | Energy / Basic | Lightning | Common ‡ | Non-holo |
| 101/102 | Psychic Energy | Energy / Basic | Psychic | Common ‡ | Non-holo |
| 102/102 | Water Energy | Energy / Basic | Water | Common ‡ | Non-holo |

## Art, foil, promotional and error boundaries

- **Pikachu #58:** the retrieved release section says Unlimited print runs only feature **yellow cheeks**. Red-cheek and stamped promotional versions are not eligible merely because their card object is `base1-58`. Both cheek versions have E3 promotions; TCGdex's single object also lists shadowless-red-cheek, 1999–2000, jumbo and poketour-99 variants. Exclude these non-target variants. No probability claim is imported from the source's estimate for excluded editions. [Bulbapedia Pikachu]; [TCGdex supplement].
- **Machamp #8:** Bulbapedia distinguishes shadowless/non-Shadowless starter copies, a **non-holo Trainer Deck A prototype without the stamp**, and a later **Cosmos Holofoil CD-ROM starter** version. The prototype is not a retail unstamped Unlimited replacement, and the later foil variant is not automatically 1999 Unlimited. TCGdex's `unlimited` subtype does not establish the stamp/date/foil combination; its generic list even includes a shadowless variant without a stamp field, so it must not override the card-specific release history. Eligibility remains pending. [Bulbapedia Machamp]; [TCGdex supplement].
- **Jynx #31:** Bulbapedia describes black-to-dark-purple recoloring for international Base Set and further lightening for later reprints. This is a **cross-release art hazard**, not evidence of two accepted variants within 1999 English Unlimited. Set/name/artist/type metadata does not certify exact coloration, recolor timing, copyright or an eligible visual source. [Bulbapedia Jynx].
- **Text/error variants:** #73 has a directly evidenced name change. Bulbapedia also lists errors/additional cards, including Wartortle evolution-box and Diglett symbol issues; being on the Base Set page does not establish that each belongs to 1999 Unlimited. No error, shade, ink, foil-pattern or art-variant master set is approved. [Bulbapedia Base Set].
- **Other releases excluded:** Base Set 2, Legendary Collection, Celebrations/modern reprints, other languages, test/prototype decks, promotional stamps and oversized cards are not automatically equivalent even where names/art/collector numbers recur. The source pages themselves cover multiple releases. No asset-level verification or licensing determination occurred; no branded images, logos or audio were downloaded.

## Completion scope: resolved research versus unresolved decisions

### Research completed
- [x] All 102 collector numbers, names, rarity evidence, card kind/energy and baseline foil status recorded.
- [x] Independent number/name checklist comparison with raw differences retained.
- [x] Original structured source fields/identifiers preserved separately from printing-specific interpretations.
- [x] Copyright/date, stamp/shadow, Machamp and art/variant hazards documented with quotes.

### Decisions returned to parent; not silently selected
1. **Machamp exception and denominator:** strict no-Edition-stamp scope gives 101 eligible number slots; conventional 102-number completion requires explicit permission for the shadowed starter Machamp plus verification of its qualifying date/foil. Do not alter the 102-number evidence checklist to hide the problem.
2. **Completion semantics:** one eligible copy per accepted collector number versus a variant/master set. A booster-only objective must be reconciled with the separate pack research; this document sets no acquisition rules or probabilities.
3. **Display-name/rarity normalization:** source #73 stays `Impostor Professor Oak`; separately sourced Unlimited display is `Imposter Professor Oak`. Parent must choose how to author corrections, diacritics/symbols and omitted-versus-supplemented rarity without overwriting provenance.
4. **Future per-card visual/printing gate:** verify eligible copyright, stamp, number, art/recolor and foil before face/asset authoring. This is not a complete physical-print variant census.
5. **Runtime authoring format/stable IDs:** deferred entirely. Source `base1-*` IDs are not project identity decisions.

## Sources, titles, access dates and exact supporting excerpts

**All accessed 2026-10-30.** Titles below are retrieved titles, or explicit descriptions of titleless raw/JSON resources. Quotes are short verbatim visible-text substrings; Markdown emphasis/link markup is omitted. Evidence excerpts may be partial, not whole-site archives. Search results/AI summaries were discovery only, **not cited evidence**. No global numeric citation IDs were created.

### Catalog JSON
- **Title:** `cards/en/base1.json` — Pokémon TCG Data (titleless structured JSON).
- **URL:** https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/cards/en/base1.json
- **Source field excerpts:** `"id": "base1-1"`; `"name": "Alakazam"`; `"rarity": "Rare Holo"`; `"name": "Water Energy"`; `"number": "102"`.
- **Evidence:** `evidence/catalog/card_metadata.json`, source media fields removed, original-response hash retained.
- **Limit:** set-level historical developer catalog, not physical-print certification or permission to reproduce assets.

### Catalog README
- **Title:** Pokémon TCG Data.
- **URL:** https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/README.md
- **Quote:** “This repository contains the card data historically used by the” (followed by the Pokémon TCG API link).
- **Quote:** “The Pokémon TCG API is scheduled to be taken offline on March 1, 2027.”
- **Evidence:** `evidence/catalog/api-readme.txt`.
- **Limit:** historical compatibility data, not a recommended new service dependency.

### API card schema
- **Title:** The card object | Pokémon TCG API Documentation.
- **URL:** https://docs.pokemontcg.io/api-reference/cards/card-object
- **Quote:** “The supertype of the card, such as Pokémon, Energy, or Trainer.”
- **Quote:** “The energy types for a card, such as Fire, Water, Grass, etc.”
- **Quote:** “The legality ruling for Unlimited. Can be either Legal, Banned, or not present.”
- **Evidence:** `evidence/catalog/api-card-docs.txt`.
- **Limit:** schema meanings, not exact-print evidence.

### Pokellector checklist
- **Title:** Base Set Pokemon Card Set List.
- **URL:** https://www.pokellector.com/Base-Set-Expansion/
- **Exact checklist excerpts:** “#1 - Alakazam”; “#8 - Machamp”; “#73 - Imposter Professor Oak”; “#102 - Water Energy”.
- **Evidence:** `evidence/catalog/pokellector.txt`, complete 102 rows parsed; companion links JSON retained.
- **Limit:** consumer set checklist, not a printing or all-rarity adjudication source.

### Bulbapedia Base Set
- **Title:** Base Set (TCG) - Bulbapedia, the community-driven Pokémon encyclopedia.
- **URL:** https://bulbapedia.bulbagarden.net/wiki/Base_Set_(TCG)#Print_runs
- **Quote:** “The first print run, featuring the 1st Edition stamp, thinner HP and attack text, and no drop shadow around the illustration window.”
- **Quote:** “The third print run, introducing the drop shadow around the illustration window and thicker HP and attack text.”
- **Quote:** “The fourth print run, identical to the Unlimited print except for the updated copyright line, which reads ©1999–2000 instead of ©1999.”
- **Evidence:** `evidence/catalog/bulbapedia-base-extracted.md`.
- **Limit:** community article covers additional cards, errors and other languages, not solely this printing.

### Relentless Dragon identification
- **Title:** Identifying Early Pokémon Cards.
- **URL:** https://relentlessdragon.com/pokemon-card-game/identifying-early-pokemon-cards/
- **Quote:** “note that shadows were only ever used on Pokémon themselves—trainer cards never had drop shadows”.
- **Quote:** “There were reportedly six separate printings of the Unlimited Edition of the” (followed by Base Set).
- **Exact copyright quotes:** the three copyright substrings in the boundary table above.
- **Evidence:** `evidence/catalog/relentlessdragon-early.md`.
- **Limit:** retailer explanation; reported run counts/alleged distribution not accepted as proven factory history. Independently corroborates the copyright distinction.

### Bulbapedia Machamp
- **Title:** Machamp (Base Set 8) - Bulbapedia, the community-driven Pokémon encyclopedia.
- **URL:** https://bulbapedia.bulbagarden.net/wiki/Machamp_(Base_Set_8)
- **Quote:** “While an Unlimited Edition version of the Base Set print of this card was never officially released”.
- **Quote:** “a Non Holofoil version of this card lacking the 1st Edition stamp was included in the Trainer Deck A”.
- **Quote:** “Another 1st Edition Base Set version of this card, with the Cosmos Holofoil treatment”.
- **Evidence:** `evidence/catalog/bulbapedia-machamp.md` (release paragraph; the separate anchor extract omits some of that paragraph).
- **Limit:** formal “Unlimited Edition” language differs from colloquial descriptions of shadowed/stamped Machamp. The documented prototype is not ordinary retail Unlimited.

### Relentless Dragon Machamp
- **Title:** Identifying Early Pokémon Cards: Base Set Machamp.
- **URL:** https://relentlessdragon.com/pokemon-card-game/base-set-machamp/
- **Quote:** “Card #8/102, Machamp could never actually be found in Base Set booster packs!”
- **Quote:** “Every print run of Machamp was printed with an” (followed by the quoted Edition 1 logo wording).
- **Evidence:** `evidence/catalog/relentlessdragon-machamp.md`.
- **Limit:** ordinary retail framing does not override the separately documented Trainer Deck A prototype. Pack consequences belong to the other workstream. The retailer article and Bulbapedia differ in formal naming, not in the existence of a shadowed stamped version.

### Bulbapedia Pikachu
- **Title:** Pikachu (Base Set 58) - Bulbapedia, the community-driven Pokémon encyclopedia.
- **URL:** https://bulbapedia.bulbagarden.net/wiki/Pikachu_(Base_Set_58)#Release_information
- **Quote:** “print runs only feature the yellow cheeks version.”
- **Quote:** “Both cheek versions have promotional variants with a gold foil” (followed by E3 stamp wording).
- **Evidence:** `evidence/catalog/bulbapedia-pikachu-release.md`.
- **Limit:** article includes other releases and an estimated excluded-edition cheek split; no split/probability adopted.

### Bulbapedia Jynx
- **Title:** Jynx (Base Set 31) - Bulbapedia, the community-driven Pokémon encyclopedia.
- **URL:** https://bulbapedia.bulbagarden.net/wiki/Jynx_(Base_Set_31)
- **Quote:** “Jynx's face was changed from black to dark purple for the Base Set and lightened even more so for subsequent reprints.”
- **Evidence:** `evidence/catalog/bulbapedia-jynx.md`.
- **Limit:** cross-release recolor history, not a demonstrated within-1999-Unlimited variant census.

### Bulbapedia Oak
- **Title:** Imposter Professor Oak (Base Set 73) - Bulbapedia, the community-driven Pokémon encyclopedia (requested Impostor URL redirects to this title).
- **URL:** https://bulbapedia.bulbagarden.net/wiki/Impostor_Professor_Oak_(Base_Set_73)
- **Quote:** “The 1st Edition and Shadowless print runs of this card were spelled Impostor Professor Oak and changed to Imposter for all further print runs and reprints.”
- **Evidence:** `evidence/catalog/bulbapedia-oak.md`.
- **Limit:** use the explicit release paragraph; extracted templates elsewhere on this article contain unrelated rule text and must not be treated as card-specific printed rules.

### Bulbapedia Energy
- **Title:** Energy card (TCG) - Bulbapedia, the community-driven Pokémon encyclopedia.
- **URL:** https://bulbapedia.bulbagarden.net/wiki/Energy_card_(TCG)
- **Quote:** “Energy cards provide Energy of a specified Energy type.”
- **Table excerpts:** “Basic Fighting Energy”; “Double Colorless Energy”. The retrieved Provides table associates the six Basic Energy names with their types and Double Colorless with two Colorless symbols.
- **Evidence:** `evidence/catalog/bulbapedia-energy.md`.
- **Limit:** general Energy article, corroborating energy meaning rather than certifying a physical printing.

### TCGdex supplement
- **Title:** TCGdex English Base Set card-object metadata (titleless JSON endpoints).
- **Exact source JSON excerpts:** #97–102 each contain `"rarity":"Common"` and `"holo":false`; #97 contains `"name":"Fighting Energy"` and `"energyType":"Normal"`; #96 contains `"energyType":"Special"`; #8 contains `"holo":true`. JSON whitespace in the saved snapshot is pretty-printed, but field values are preserved.
- **URLs, all fetched individually:**
  - https://api.tcgdex.net/v2/en/cards/base1-8
  - https://api.tcgdex.net/v2/en/cards/base1-31
  - https://api.tcgdex.net/v2/en/cards/base1-58
  - https://api.tcgdex.net/v2/en/cards/base1-73
  - https://api.tcgdex.net/v2/en/cards/base1-96
  - https://api.tcgdex.net/v2/en/cards/base1-97
  - https://api.tcgdex.net/v2/en/cards/base1-98
  - https://api.tcgdex.net/v2/en/cards/base1-99
  - https://api.tcgdex.net/v2/en/cards/base1-100
  - https://api.tcgdex.net/v2/en/cards/base1-101
  - https://api.tcgdex.net/v2/en/cards/base1-102
- **Evidence:** `evidence/catalog/tcgdex_supplement.json`.
- **Limit:** these objects aggregate multiple variants/printings. A variant flag is not a physical-card certificate; Machamp generic variants require special skepticism. No pricing/media data retained or media fetched.

## Verification performed

`python docs/research/evidence/catalog/verify_catalog.py` passed: complete unique 1–102 number coverage in both lists, all 102 research-table rows, source-kind/rarity/finish counts, the exact seven raw name differences, six externally sourced Basic Energy rarities, metadata media/pricing-key exclusion, and **29/29 quote checks** against saved source text. Results are saved in `evidence/catalog/verification.json`.

A separate fresh download of the primary JSON also passed an exact object comparison: all 102 saved card objects equal the retrieved source objects after removing only `images`; the original-response SHA-256 matches the recorded provenance. No assertion that these objects represent certified exact physical printings follows from those checks.

## Retrieval issues and evidence handling

Direct Bulbapedia/retailer HTTP requests returned 403; the text-extraction service returned readable article sections. Some extraction requests timed out or hit a free-tier limit, and some returned only partial text; alternate article anchors/full extracts supplied the quoted sections. Bing ignored important query terms and Google sometimes returned AI summaries: neither is used to support findings. A guessed PSA article URL resolved to unrelated baseball content and a guessed TCGplayer article returned Not Found; neither is cited. No issue was hidden by fabricating checklist data.

Saved `retrieve_catalog.py` and `collect_supplement.py` are text/metadata research retrieval helpers only, not a game generator. The first is a one-time snapshot collector; re-running it replaces its local snapshot, so retain/reapply the provenance caution additions deliberately if refreshing. Evidence hashes describe original response bytes, not a false claim that transformed files are byte-identical originals. Source rights are not transferred by metadata access.

[Catalog JSON]: https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/cards/en/base1.json
[Catalog README]: https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/README.md
[API card schema]: https://docs.pokemontcg.io/api-reference/cards/card-object
[Pokellector checklist]: https://www.pokellector.com/Base-Set-Expansion/
[Bulbapedia Base Set]: https://bulbapedia.bulbagarden.net/wiki/Base_Set_(TCG)#Print_runs
[Relentless Dragon identification]: https://relentlessdragon.com/pokemon-card-game/identifying-early-pokemon-cards/
[Bulbapedia Machamp]: https://bulbapedia.bulbagarden.net/wiki/Machamp_(Base_Set_8)
[Relentless Dragon Machamp]: https://relentlessdragon.com/pokemon-card-game/base-set-machamp/
[Bulbapedia Pikachu]: https://bulbapedia.bulbagarden.net/wiki/Pikachu_(Base_Set_58)#Release_information
[Bulbapedia Jynx]: https://bulbapedia.bulbagarden.net/wiki/Jynx_(Base_Set_31)
[Bulbapedia Oak]: https://bulbapedia.bulbagarden.net/wiki/Impostor_Professor_Oak_(Base_Set_73)
[Bulbapedia Energy]: https://bulbapedia.bulbagarden.net/wiki/Energy_card_(TCG)
[TCGdex supplement]: https://api.tcgdex.net/v2/en/cards/base1-97
