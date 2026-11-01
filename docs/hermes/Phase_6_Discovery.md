# Phase 6 — physical-card shopping discovery

Status: the separate **Phase6ShoppingWindows** n02 candidate passed actual native shopping/reopen execution and final independent delivery review; **human acceptance is pending**. See `Phase_6_Shopping_Human_Review.md`, `docs/verification/Phase_6_Shopping_Execution.json` and `docs/verification/Phase_6_Shopping_Final_Independent_Review.json`. Manual exact-printing searches/query copy, dated local notes, review states, unknown-versus-zero amounts and durable Retry are implemented. No live-price API, scraping or financial operation is implemented. The accepted performance f04 player and all earlier players remain unchanged; the shopping candidate uses its own saves with no automatic migration. The original discovery and proposal below are retained as historical context, not unfinished implementation claims.

## Authority and approvals

`Hermes_Vintage_Card_Collector_Handoff.md` lines222–228 define Phase 6 as physical-card shopping, with provider research and an integration proposal before implementation. The latest explicit user approval is recorded verbatim in `docs/verification/Phase_5_Human_Acceptance.json`. All138 immutable n07 player files matched before recording approval; saves/logs were pruned before traversal. No launch or rebuild was needed.

## Current process

- Base, Jungle and Fossil packs and printing-aware virtual/physical binders are playable and human-accepted. Physical ownership is manually confirmed and independent of virtual openings.
- Results identify cards missing physically. No shopping service, listing parser, live-price promise, checkout or financial account is established by this discovery.
- Newly supplied `Pokemon-Jungle` contains64 numbered face files and three booster fronts; `Pokemon-Fossil` contains62 numbered face files and three booster fronts. All132 PNGs fully decoded, all126 faces mapped to approved canonical IDs, and every original hash/size/mtime was rechecked unchanged. Faces are600×825; wrappers260×475. The supplied faces show First Edition stamps; the owner explicitly approved their unchanged use as Unlimited presentation substitutes in `docs/verification/Jungle_Fossil_Art_Substitution_Approval.json`. There are no supplied dynamic masks or backs. The audit is indexed by `docs/verification/Jungle_Fossil_Supplied_Art_Audit.json`. The accepted n07 build remains unchanged. The separate supplied-art candidate at `Builds/Phase5SuppliedArtWindows` built as native release a04 with zero errors/warnings,831 complete compiler inputs and138 player files. Actual art/reopen processes passed321+263 checks, matched126 faces and six wrapper variants to source pixels, and preserved six-pack/66-card history on reload with no new generation. The former tooling rejections and failed long-path import are retained as history; the final artifact audit passed, and focused human art review remains pending. Root build settings and all nine accepted players remain preserved.

## Projected changes — proposed first slice, not implemented

1. Offer an optional shopping action for a selected physical-missing printing, on results or in the binder—not during every reveal.
2. Generate reviewable, exact-printing browser searches from the canonical catalog identity: English, set, Unlimited edition, collector number and holo/non-holo variant. A search result is only a candidate, never a verified exact match. Exclude or flag First Edition, Shadowless, non-English, graded, mismatched finish/number and ambiguous listings.
3. Keep a manual fallback: copy the exact wanted-card description and optionally record a seller/listing URL, condition, asking price, currency, shipping and capture date. Reviewable local comparisons can work without API credentials.
4. Display unknown shipping, fees and tax explicitly; do not label an incomplete amount as delivered total or promise the cheapest listing across the entire market. Currency conversion is not assumed.
5. Keep shopping optional and user-triggered. Offline opening/binders continue to work. No background scraping, automatic checkout, purchases, account creation, credentials in Unity, financial transfers or ownership changes from clicking a listing.
6. Validate outbound URLs and encoding before a user-initiated browser launch. Provider-specific domain/query details need verification; none is described as working yet.

## Initial official-provider findings

- eBay's current Buy API requirements document explicitly states that approval for production use is not guaranteed. A direct authenticated integration is therefore not assumed available; application eligibility, exact Browse use-case requirements and credentials would need separate validation before choosing it.[5]
- TCGplayer's getting-started documentation says it is no longer granting new API access. Do not build this private app around an assumed new TCGplayer developer key. The older support article's generic API promotion is not sufficient evidence of present access.[7]
- Cardmarket documentation was extractable through the retrieval backend, but a direct request returned410. Its access and regional suitability remain unconfirmed for this project; no live API claim or integration commitment is made.

These are documentation findings, not successful API calls or live listing/price tests. Raw responses, transformations, hashes and failures are retained in `docs/research/phase6/evidence/retrievals.json`; the citation ledger is workspace-local. No marketplace credential was requested or used.

## Owner decisions and remaining preferences

- Answered: shipping to USA, comparisons in USD; raw, ungraded **Near Mint only**. The more precise location was supplied and retained only in the private ignored project artifact `.artifacts/phase6-shopping-preferences.json`; it has not been sent to any marketplace.
- Optional provider/seller preferences were unanswered. Do not invent an exclusion or claim a preferred provider.
- Domestic-only versus international shipping remains unselected. The initial proposal should make this explicit rather than assuming an import policy.

## Execution gates

- Finish the supplied-art audit, then propose an identity-preserving presentation replacement separate from shopping logic. Do not modify odds, identities, saved packs or the accepted player's dependency paths while replacing visuals.
- Resolve owner preferences and present a concrete provider/link/comparison proposal before implementing Phase 6 UI or persistence.
- Test mismatch exclusions, stale/missing listings, unknown shipping, unavailable network/provider and correct outbound links. No purchases during tests.
- Preserve the accepted Phase5Windows player and its now potentially populated Saves directory. Any follow-up build needs a separate admitted candidate path; never overwrite the accepted player or use its saves for automation.

## Sources

[5] https://developer.ebay.com/api-docs/buy/static/buy-requirements.html
[7] https://docs.tcgplayer.com/docs/getting-started
