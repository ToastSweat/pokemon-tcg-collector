# Phase 6 — manual-first physical-card shopping slice

## Authority / current process

The owner approved performance build f04 and explicitly requested continuation (`Performance_Human_Acceptance.json`). All140 immutable files were rechecked unchanged without launching/rebuilding or reading saves. Eleven used players /1504 immutable members are protected by `.artifacts/phase6-parent/before.json`.

Three-set opening, supplied art, common back, wrapper variants, loading and background durability are accepted. The binder tracks virtual and manual physical ownership. No shopping UI, provider session, price-fetcher, checkout or fund ledger exists. Owner preferences: USA / USD / raw ungraded Near Mint only. Optional seller preferences remain unspecified; no postal code is sent by this slice.

## Projected first slice

- An optional **Find physical card** action for a selected printing in the physical binder, and a corresponding action for a revealed/inspected card missing physically. Never interrupt each reveal.
- A read-only wanted-card descriptor from the approved registry: exact canonical ID, English set, Unlimited, collector number/total, holo/non-holo, raw Near Mint. Supplied First Edition-stamped images must NOT influence shopping identities or query terms.
- User-clicked eBay US / TCGplayer search links and a copyable exact description. A search is explicitly **unverified candidates**, not an exact-match or cheapest-price claim. No network request, browser opening or clipboard change merely from opening a panel.
- Manual listing comparisons saved locally per active profile: provider/listing URL, seller/title/note, asking price, shipping, tax/fees when known, USD and dated observation. Unknown amounts remain unknown; partial subtotals are never called delivered totals. No automatic currency conversion.
- Explicit manual review checklist for English, set/number, Unlimited, finish, raw/ungraded and Near Mint. An ambiguous or unreviewed listing is marked unverified, never silently accepted. Known mismatches cannot be marked matched. Descriptions/titles alone are not proof.
- Shopping records never alter pack history, generation/odds, virtual awards, physical holdings, ordered/purchased state or money. Only existing physical-ownership confirmation changes ownership.

## Fixed boundaries

Only normal HTTPS URLs at closed eBay/TCGplayer domains, no credentials/custom ports/local hosts/unsafe schemes or inferred redirects. Generated queries are URL-encoded and use approved identity, not raw filenames. Listing URLs must be listing-shaped, separate from generated search URLs. Never execute checkout or embed account secrets. Site cookie/location behavior remains the user's external browser context.

Use cents (Int64) plus explicit known flags for amounts; accept at most two decimal places, reject negative/nonfinite/overflow/ambiguous money. Unknown shipping/tax/fees prevents a complete-total label and cheapest-delivered ranking. Data is user-entered asking-price snapshots, not live stock or completed-sale evidence.

Shopping storage is an independent versioned local sidecar inside the validated active profile root. Use exclusive ownership, bounded parse/field/schema checks and durable atomic save/backup; uncertain writes retain exact intent and Retry. Reject corrupt/unsupported stores without replacing them with blank notes. Keep store serialization separate from the domain and no dependency on online providers. Current collection ZIP backups do not include shopping notes unless explicitly implemented and verified; the UI must state that limit. Profile restore creates a new profile, not a hot swap or merge.

Opening/binder work remains usable offline and shopping must fail independently of collection storage. Preserve accepted async save/quit authority: wait for idle/ready before shopping mutation; dispose shopping leases through owner cleanup without bypassing completion/exit. Shopping overlays must prevent underlying hidden input and restore prior UI state safely. No global foreground/cursor manipulation.

## APIs / work isolation

Use a small pure C# `VintageCollector.Shopping` domain and a separate serializer/store. API coordination is via implementation handoffs; do not guess or duplicate domain semantics in the GUI.

- Domain/query proposal: `.artifacts/phase6-shopping-domain-work/`.
- Shopping sidecar proposal: `.artifacts/phase6-shopping-store-work/`.
- UI/wiring begins after those APIs are frozen; minimal passive binder/inspection hooks, no battle/marketplace/account framework.
- All controlled files/temp/logs remain inside the approved project. Original assets and all used players/saves are read-only. New playable output proposed `Builds/Phase6ShoppingWindows` and short owned stage `.artifacts/s6`.

## Validation / checkpoint

TDD across all227 eligible wanted printings; holo/non-holo same-name separation, Base Machamp exclusion, search encoding and mismatch flags; unsafe URLs; money unknown/overflow/partial totals; sidecar fault/Retry/reopen/corruption/unsupported preservation; UI stale selection/input gating; actual user-triggered outbound command with fake launcher tests versus real network/browser observations clearly distinguished. No purchases during tests.

Require exact-source review, installed Unity-reference compilation, guarded new-scene build and real native opening→physical-missing card→shopping review→reopen before claiming a working slice. Preserve eleven used player manifests and old artwork/runtime dependencies. Keep provider failures/offline behavior and unverified matches visible.

This proposal implements the handoff's manual fallback first. Direct live-listing APIs are not assumed available (official documentation evidence is retained in `Phase_6_Discovery.md`). No financial integration is authorized.
