# Retrieval audit

Each source has exact URL/title/time/status/headers/hash metadata in the matching `.retrieval.json`. Source IDs below are ledger identifiers, not citations endorsing the page contents.

| Key | Ledger ID | HTTP / error | Actual title | Research use |
| --- | --- | --- | --- | --- |
| australia-box | 13 | 200 | Jungle Booster Box Made in Belgium - General - Elite Fourum | Discovery only / not used for factual claim support |
| belgium-box | 12 | 200 | 1999-2000 Jungle Booster Box Opened - General - Elite Fourum | Reported excluded-product order/date hazard |
| booster-pack | 5 | 200 | Booster pack (TCG) - Bulbapedia, the community-driven Pokémon encyclopedia | General-era pack size text |
| bulbapedia | 3 | 200 | Jungle (TCG) - Bulbapedia, the community-driven Pokémon encyclopedia | Independent complete metadata; scope/variant/wrapper text |
| efour-search | 15 | 200 | efour-search (no HTML title) | Discovery only / not used for factual claim support |
| jungle-pokewiki | 11 | HTTP Error 403: Forbidden | (no successful response title) | Discovery only / not used for factual claim support |
| order-thread | 16 | 200 | Unlimited Jungle boosters with 1st uncommons? - General - Elite Fourum | Reported nominal order; internally conflicting mixed-edition claims |
| pokebeach | 6 | 200 | Jungle - PokeBeach / PokéBeach | Thin HTML page: missing substantive checklist body |
| pokellector | 2 | 200 | Jungle Pokemon Card Set List | Third complete number/name list |
| rarity-guide | 4 | 200 | The English Pokémon card rarity guide - Articles - Elite Fourum | Textual sheet/collation interpretation; image tables NOT read |
| search-bing | 9 | 200 | Pokemon Jungle booster pack composition 7 3 1 - Search | UNUSABLE for question: irrelevant generic Pokemon results |
| search-composition-efour | 17 | 200 | search-composition-efour (no HTML title) | Discovery only; result/snippet NOT used as full-page claim evidence |
| search-composition | 7 | 200 | Google Search | UNUSABLE: Google redirect/interstitial despite HTTP 200 |
| search-ddg | 10 | 202 | DuckDuckGo | UNUSABLE: human/bot challenge despite body response |
| search-energy-efour | 19 | HTTP Error 429: Too Many Requests | (no successful response title) | Discovery only; result/snippet NOT used as full-page claim evidence |
| search-holo-efour | 18 | 200 | search-holo-efour (no HTML title) | Discovery only; result/snippet NOT used as full-page claim evidence |
| search-order | 8 | 200 | Google Search | UNUSABLE: Google redirect/interstitial despite HTTP 200 |
| serebii | 14 | 200 | Jungle - Serebii.net Pokémon Card Database | Discovery only / not used for factual claim support |
| tcg-data | 1 | 200 | PokemonTCG/pokemon-tcg-data cards/en/base2.json (JSON; no page title) | Complete metadata source |
| wotc-chats | 20 | 200 | WotC Chat Logs from Feb 2000 to Nov 2000 - Q&A w/ Employees - Articles - Elite Fourum | Community reproduction of historical WotC statements; original archive not independently verified |
| wotc-search | 21 | 200 | wotc-search (no HTML title) | Discovery only / not used for factual claim support |

## Execution limitations retained honestly

- The first terminal attempt was blocked because its `workdir` contained `[`; the retry used the exact quoted repository `cd` and succeeded. No alternate repository was used.
- One long inline quote-attachment command hit a shell quoting/truncation error; smaller batches succeeded. No source retrieval body or quote was invented to bypass it.
- The citation helper rejected too-short JSON excerpts and one excerpt whose final punctuation spacing differed from the extracted text. Final saved excerpts all passed the helper and were rechecked against their retained snapshot hashes.
- Initial HTML-to-text extraction is intentionally simple and includes unrelated navigation/CSS debris. For checklist rarity/kind, validation reads original raw HTML attributes rather than relying on stripped text.
- HTTP 403 (Fandom) and HTTP 429 (the later Energy search) are retained in metadata without fabricated successful contents. Alternative retrieved sources supplied actual checklist and composition evidence. No image challenge was solved or downloaded.
- The forum order thread contains opposing claims about mixed editions; both were read. The early assertion that First Edition cannot appear in Unlimited is not treated as settled fact.
- No opening video, physical pack, numerical sheet image or branded art was fetched. Unknown physical orientation and exact selected-run weights remain explicit blockers, not guessed results.
