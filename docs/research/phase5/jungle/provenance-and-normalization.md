# Jungle candidate — provenance, normalization and reproduction

## Scope and authority

Original project authority: `docs/hermes/Hermes_Vintage_Card_Collector_Handoff.md` and `docs/hermes/Phase_5_Discovery.md`. The present request authorizes research/data and placeholder-first work, not approval of a probability model. This worker owns only the Jungle files, `tools/prepare_jungle_catalog.py`, `TestsPhase5/Jungle*`, and the Jungle authoring candidate. Existing Unity runtime, scenes, Assets, builds, saves, settings, supplied originals, audio, and other workers' Fossil files are not modified. No commit is made.

**Runtime compatibility is not runtime activation.** The catalog is outside `Assets`, has completion status `proposed-not-approved`, and is not bound to a playable pack. It contains no `RuleData`, pools, RNG configuration, slot rules, weights or art payloads.

## Retained evidence chain

- Retrieval used Python standard-library HTTPS GETs with a research user-agent; only HTML/JSON/text URLs were requested. HTML containing image URL strings is textual metadata, not an image download. No images, logos, sound, music or branded art were fetched.
- Each `*.retrieval.json` records requested URL, resolved final URL, UTC retrieval time, HTTP response status and headers when available, actual HTML title (or explicit absence of an HTML title), raw byte length, paths and SHA-256 of raw bytes and text derivative. URLs were not guessed into the report as successful retrievals; actual source IDs were assigned by the project-local citation helper at retrieval.
- `*.raw.html` / `*.raw.json` are original response bytes. Failed requests retain error metadata; a failed request has no invented body, hash or title. A 200 response is NOT automatically usable evidence: Google returned redirect/interstitial content; Bing returned irrelevant generic Pokémon results; PokéBeach returned a thin page without checklist body; DuckDuckGo returned a human challenge with status 202. These are retained as limitations, not used as evidence for pack claims.
- Initial text derivatives decode UTF-8 with replacement, remove script/style blocks and HTML tags, unescape HTML entities and retain whitespace. Writes use UTF-8. Their hashes are byte-level hashes of files, not hashes of a hypothetical normalized page. Some unrelated Serebii navigation is visibly misdecoded; Serebii is retained but not used for name or rarity authority.
- The authoritative second checklist parser reads the original **raw HTML**, not that lossy tag-stripped derivative. It restricts to `Set_lists` before `Gallery`, selects numbered `/64` rows, reads explicit rarity-link titles and Pokémon type image **alt labels** or Trainer `T` text, and requires exactly ordered 1–64 once. It never downloads icons or infers Pokémon kind from the English card name. Additional-card/promotional tables before `Set_lists` are deliberately excluded; the Japanese unnumbered table does not enter membership.
- `evidence-quotes.json` records verbatim quote, evidence file/path and hash. The existing grounded-citations helper rejects nonmatching quotes. Matching ignores whitespace/case/Markdown; it does not permit paraphrase. A punctuation-space mismatch and too-short quotes were rejected during setup; only successfully matched final quotes are included. The raw retrievals remain unchanged.
- The project-local `ledger.json` owns URL-to-source numbering. Its URL identity policy strips fragments and trailing slash while preserving query strings; **retrieval metadata preserves the exact requested and final URL separately**. A redirect path is not silently rewritten into a different claim source. Sources blocks are rendered from the ledger, not handwritten.
- `snapshot-integrity.json` verifies all retained raw/text snapshot hashes and quote hashes against the recorded retrieval/evidence metadata. `artifact-hashes.json` pins the final owned deliverables except itself; it is a local audit index, not an authenticity signature or license claim.

## Exact normalization and candidate policy

1. The JSON source is actually Jungle under source IDs `base2-1` through `base2-64`; that upstream `base2` token is preserved as provenance and is **not** renamed to make it look like Base Set 2.[1] The source response was inspected before selecting it.
2. Validate JSON collector-number syntax as positive canonical decimal strings and require ID `base2-` plus the identical string. Compare all five normalized fields against the independent table; source labels `Rare Holo`, `Rare`, `Uncommon`, `Common` map only to `holo`, `rare`, `uncommon`, `common`.
3. `Rare Holo` means `finish=holo`; the three other selected rarity classes mean `finish=nonholo`. This is a nominal checklist finish translation, not individual-card photographic finish authentication.
4. The only name aliases are `Nidoran ♀` → `Nidoran♀`, Pokellector `Nidoran F` → `Nidoran♀`, and Pokellector `Poke Ball` → `Poké Ball`.[1][2][3] No broad punctuation/accent/gender stripping, fuzzy name match, or arbitrary number coercion is performed. JSON `sourceName` remains exactly as retrieved.
5. `sourceId`, `sourceName`, number, normalized name, rarity, kind and finish are source-backed membership/provenance. `language=en`, `edition=unlimited`, `printingId=jungle-en-unlimited1999`, `variant=standard`, and the new project IDs are **explicit proposed target policy**. Generic set lists do not independently authenticate those edition/batch attributes. Source `legalities.unlimited` means a play format, NOT proof of Unlimited physical edition, and is never used to select edition.[1]
6. Every candidate `boosterEligible=true` is likewise a **proposed standard-booster policy**, recorded separately from source membership in `membership-comparison.json`; the source datasets have no such field. This does not approve a physical completion target or imply variants/errors/promos were searched and accepted. Use of this candidate in runtime must wait for the consequential model/identity checkpoint.
7. Stable ID: `pkmn-jungle-en-unlimited1999-{number:D3}-{holo|nonholo}-standard`; serialized `collectorNumber` itself stays canonical unpadded decimal. `setId=jungle`; every `artReference=placeholder:card-face`. Display names are not identity keys. No approval flag or sheet weight is smuggled into any card.

## Reproduce the actual authoring checks

Run from the quoted repository path (terminal workdir validation rejects brackets, so use a quoted shell `cd`):

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python -B TestsPhase5/JungleAuthoringTests.py
python -B -c "from pathlib import Path; import sys,json; sys.path.insert(0,'tools'); import prepare_jungle_catalog as j; p=Path('docs/research/phase5/jungle'); c=json.loads(Path('Authoring/Phase5/Data/jungle-en-unlimited1999.catalog.json').read_text(encoding='utf-8')); print(json.dumps(j.validate_catalog(c,p),indent=2))"
```

The authoring module is deliberately offline and has no network calls, runtime hooks or default write side effects. `build_catalog(Path('docs/research/phase5/jungle'))` returns the candidate dictionary; regeneration is an explicit authoring operation, not a live game importer. It fails closed on disagreement between JSON and HTML metadata or the third number/name list. Source snapshots are immutable evidence inputs; obtain new snapshots as separate research evidence before accepting upstream changes.

The independent managed-code acceptance harness was compiled with Unity 6000.2.14f1's bundled Mono and **explicitly enumerated only** `TestsPhase5/JungleCatalogChecks.cs`, production `Assets/VintageCollector/Domain/Data.cs` and `Catalog.cs`. Full actual compiler/execution argv and exits are in `domain-validation.log`; no recursive inclusion of another worker's `Main` or tests. The harness loads the actual candidate through `JavaScriptSerializer` and the unchanged real `Catalog.FromData`; it is not a reimplementation of the runtime validator. Its exe lives alongside the evidence, outside Assets/builds.

RED/GREEN authoring logs store the real child test exit even though the outer research command that captured a deliberate RED completed successfully. The first four RED cycles failed because their named production behavior was absent; the fifth caught boolean/integer equality accepting the wrong JSON types. All GREEN cycles passed after implementation. The source-mutation regression operates on copies of retained real snapshots inside disposable directories under this Jungle evidence directory. No synthetic Pokémon database is represented as research.

## Fidelity limits and next research protocol

Two public source maintainers agreeing is useful corroboration but does not prove genealogical independence; the checklist agreement is not a factory audit. For stronger physical-order evidence, record a clearly identified standard English Unlimited sealed product, wrapper front/back and extraction end, untouched stack front/back orientation, complete original order before any rearrangement, exact print/date/stock clues and anomalies. Count distinct physical packs once, distinguish sealed-box provenance from loose/possibly weighed packs, and retain disagreements. For numeric sheet weights, require a selected-run attributable, auditable textual transcription of every sheet cell and normalization denominator; do not infer it from rarity-group cardinality or isolated openings. Box process and marginal pack odds must remain separate research claims.

## Sources

[1] https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/cards/en/base2.json — PokemonTCG/pokemon-tcg-data cards/en/base2.json (JSON; no page title)
    > ""id": "base2-1", "name": "Clefable","
    > ""rarity": "Rare Holo""
    > ""name": "Poké Ball", "supertype": "Trainer","
[2] https://www.pokellector.com/Jungle-Expansion — Jungle Pokemon Card Set List
    > "#1 - Clefable"
    > "#64 - Poke Ball"
    > "Cards 64 Released Jun 16th 1999"
[3] https://bulbapedia.bulbagarden.net/wiki/Jungle_(TCG) — Jungle (TCG) - Bulbapedia, the community-driven Pokémon encyclopedia
    > "The Japanese release contained 48 cards; however, the English release was expanded to 64 cards since Wizards of the Coast decided to include Non Holofoil prints of the Rare Holo cards."
    > "The English expansion was released on June 16, 1999, while the Japanese expansion was released on March 5, 1997."
    > "The 1st Edition print run also accidentally included the extremely rare 1st Edition Promo Pikachu"
    > "All the English Holofoil Rares in the unlimited edition Jungle set were also accidentally printed as error cards without set symbol."
    > "English Wigglytuff pack"
    > "English Scyther pack"
    > "English Flareon pack"
    > "In the Australian release, the Unlimited print used red lettering for the word "Pokémon" instead of yellow on relevant packaging."
    > "Pokémon League Prerelease promo"
    > "Gold border Pokémon Rolls exclusive"
    > "Duelist Magazine W Promo"
    > "1st Edition - "Edition d""
    > "Dark line on top left where background meets border"
