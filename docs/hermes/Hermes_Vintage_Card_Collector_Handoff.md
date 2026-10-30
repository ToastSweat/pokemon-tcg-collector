---
title: Vintage Card Collector — Hermes Unity Handoff
created: 2026-10-30
status: Planning — Phase 0 discovery required
platform: Unity desktop application
---

# Vintage Card Collector — Hermes Handoff

## Read this first

I want a personal Unity game that recreates opening vintage Pokémon booster packs and helps me build the corresponding physical collection. Start with English Base Set, then Jungle and Fossil. The pleasure of opening the pack matters, but accurate pack contents and probabilities matter most.

This is a new concept, not a claim that an existing Unity project or asset library is available. Inspect anything I provide before deciding what needs building. Do not assume a Unity version, render pipeline, repository path, or installed tools.

Work in phases. Stop at the end of every phase, show me what works and what remains uncertain, and wait for my approval before beginning the next phase. Do not turn the entire roadmap into one implementation task.

## The experience I want

1. Choose a Base Set, Jungle, or Fossil pack, limited to supported editions.
2. Open a convincing wrapper with satisfying textures, materials, movement, and sound.
3. Reveal the cards in the historically appropriate order.
4. Receive contents generated using the best-supported historical pack rules.
5. See which cards are new to my virtual collection and which I still need physically.
6. Browse my binder and track both collections separately.
7. Optionally look for matching physical singles at a good delivered price and condition.
8. Eventually, associate opening packs with setting money aside for the physical collection.

I miss the anticipation of opening packs, but want a low entry point. This is not an attempt to pay current sealed vintage-pack prices. Virtual pack pricing or savings contributions should be configurable and independent of market prices for sealed packs.

## Materials and IP handling

**I can provide materials as needed: card art, card backs, logos, wrapper images, sound effects, and other reference assets. Ask me for specific materials when a phase needs them.**

Do not let missing branded assets stop work on the underlying systems. Use clearly labeled placeholders with the correct aspect ratios and replaceable asset references. Give me a short asset request list with preferred formats, dimensions, transparency, and intended use. Avoid asking for the entire asset collection before the first prototype.

Do not independently scrape or bundle branded artwork, logos, music, or sound effects. Do not invent a license or assume that material I supply grants redistribution rights. Keep supplied assets identifiable and separate from original code and generic assets. Record their source, intended use, and any known restrictions in an asset manifest; mark unknown rights as unknown.

Default scope is a private, personal desktop project. Do not publish builds, upload supplied materials to public repositories, or add commercial distribution/monetization without a separate discussion. Use neutral internal project naming until I choose a name. These are project handling instructions, not a claim of legal clearance.

## Scope and priorities

### Essential

- Unity implementation with a playable desktop build; Windows is the proposed initial target, to confirm during discovery.
- Accurate card identity and edition matching.
- Pack rules that are explicit, researched, versioned, and testable.
- Enjoyable opening and card inspection.
- Persistent virtual collection and physical collection tracking.
- Base Set, Jungle, and Fossil eventually supported.
- Local operation for opening packs and viewing the binder.

### Later, useful additions

- Card-shopping searches and listing comparisons.
- Manual savings ledger with pack contributions and physical purchases.
- Supported financial integrations, only if feasibility justifies them.
- Additional editions and sets.

### Outside the first build

- Battle gameplay, multiplayer, trading, public accounts, cloud services, mobile support, graded-card valuation, live banking, automated purchases, or a full marketplace.
- First Edition and Shadowless packs unless separately selected and researched.
- Artificial pity systems, secretly boosted odds, or guaranteed new cards.

## Pack authenticity: research before claiming accuracy

The prior discussion did not verify historical pack composition, reveal order, or odds. No numeric pull rate in conversational notes should be treated as an approved specification.

Proposed first target: **English Base Set Unlimited**. Confirm the exact printing/reference before finalizing rules. English Jungle Unlimited and Fossil Unlimited follow later. Different printings, languages, holo/non-holo versions, errors, and variants must not silently share identity or probabilities.

Create a research register containing:

| Field | Required content |
| --- | --- |
| Claim | Exact composition, ordering, probability, or collation claim |
| Scope | Set, language, edition, printing where known |
| Evidence | Source link, title, access date, relevant observation |
| Confidence | Documented, corroborated observation, estimate, or unknown |
| Implementation | Which rule uses the claim |
| Limitation | Conflicts, sample bias, or missing information |

Research card counts, rarity slots, energy/basic-land-equivalent allocation where applicable, holo replacement behavior, physical stack order, duplicate restrictions, rarity-sheet weighting, and sheet/box collation where evidence exists. Do not assume every card in a rarity pool is equally likely or every slot is independent without documenting that assumption.

Distinguish individual-pack marginal probabilities from correlations across packs in a sealed box. Do not build a box simulation unless it is selected as scope and supported by evidence. A collection of opening videos can inform observations but does not alone establish exact population odds.

Implement the best-supported model. Where evidence is incomplete, label approximations honestly and document their effect. Do not fabricate exact factory collation or promise perfect historical accuracy. Bring unresolved choices that materially affect fidelity to me at the research checkpoint.

Physical pack order and any optional collector-style card rearrangement are separate behaviors. The default should reproduce the researched original opening experience, not automatically apply a modern “card trick.”

## Technical design principles

### Separate generation from presentation

The pack generator produces an ordered, immutable pack result before the animation reveals it. The opening scene consumes that result; it does not roll another card on each click.

Persist the generated pack and collection transaction before allowing reveals. Reloading resumes the same pack. Completing or replaying an animation must not award its cards again. Handle interrupted saves, repeated input, scene transitions, and application exits without losing or duplicating cards.

Use a transaction ID and a recoverable local-save strategy, such as a temporary write plus atomic replacement and a last-known-good backup. Document the transaction states and recovery behavior. This is ordinary local persistence, not a backend service.

### Keep data replaceable

Prefer a small, maintainable C# design using Unity-native patterns. Use pure C# for generation and collection rules where practical so tests can run without rendering. Choose data authoring/storage formats after inspecting the project. Do not introduce a framework or dependency just for architectural appearance.

Suggested domain records:

| Record | Purpose |
| --- | --- |
| Card printing | Stable ID, set, collector number, language, edition, variant, rarity, art reference |
| Pack definition | Supported printing, slot/order rules, probability model, model version |
| Opened pack | Transaction ID, generation time, ordered card IDs, model version, reveal state |
| Virtual holding | Card-printing ID and quantity pulled |
| Physical holding | Card-printing ID, quantity, condition, optional purchase details |
| Shopping candidate | Exact printing match, condition, seller, price, shipping, currency, URL, timestamp |
| Fund entry | Contribution, purchase, adjustment, or confirmed transfer with status and reference |

A card-printing ID must not depend on a display name, translated name, asset filename, or marketplace listing title. Different holo/edition variants need different identities even when the card name matches. Preserve historical opened-pack contents when a probability model changes.

Keep save schemas versioned. Support backup/export and validate imports. Do not overwrite an existing save with a blank collection when parsing fails.

### Sensible presentation scope

Start with a tabletop scene, a wrapper, a card stack, and readable UI. A convincing controlled tear animation is acceptable before expensive real-time foil or paper simulation. Propose the smallest implementation that delivers the intended feeling.

Cards should have clean edges, correct proportions, crisp readable faces at inspection scale, and a believable finish. Holographic treatment must preserve the printed face and use suitable masks/materials where supplied. Do not apply a full-card rainbow effect indiscriminately.

Support mouse interaction, a click alternative to dragging, adjustable volume, and skipping or speeding up repeated animations. Preserve reveal order when skipping. Do not require a precision gesture to access a purchased/generated pack.

## Binder and ownership behavior

Show a virtual binder with set order, empty slots, duplicates, and completion counts. Make the currently displayed collection unmistakable: virtual pulls or physical ownership.

Opening a pack affects only the virtual collection. A shopping search, a purchase click, and a savings contribution do not mark a card physically owned. Physical ownership is manually confirmed; later purchase tracking may add an “ordered” state that remains separate from “owned.”

Track completion by unique eligible printings, not total card quantity. Define which variants belong in each completion target before calculating percentages. A holo and non-holo version may be separate binder slots if the selected set checklist requires both.

After opening a pack, show new virtual cards, duplicates, and cards missing physically. Shopping prompts belong on the results screen and should be optional. Do not interrupt each reveal with a sales prompt.

## Shopping: exact matching before price comparison

Start with external searches or links that I review. Investigate current supported APIs and service terms before choosing providers; do not assume listings, prices, stock, or card images can be obtained freely. Manual lookup must remain a fallback.

Only recommend the selected language, set, edition, holo status, and printing/variant where distinguishable. Exclude First Edition or Shadowless when opening Unlimited packs. Ambiguous matches should be flagged or excluded, never silently substituted.

Ask me for my condition target and buying region. Near Mint raw cards are a proposed starting point, not a settled requirement. Compare condition, seller reputation, listing photos where available, currency, shipping, and known fees. Mark unknown shipping/tax rather than presenting an incomplete price as the final delivered total.

Use dated price snapshots and distinguish asking prices from completed sale data. Avoid claiming to find the absolute cheapest card across the entire market. Seller bundles can reduce total shipping, but advanced cart optimization is optional later work.

No automated checkout or purchasing is required.

## Savings and financial integration

The initial implementation should not require PayPal, bank credentials, or real transfers. Opening and collecting must work without connecting any financial account.

Start later with a manual ledger: pack opening can create a planned contribution; I can mark a transfer confirmed and record purchases. Clearly distinguish planned contributions from actual confirmed funds. Spending ledger money does not automatically update physical ownership.

PayPal or bank integration is a future feasibility spike. Research supported account types, APIs, authorization, transfer destinations, fees, and platform restrictions before proposing implementation. Do not assume an API can move money between my own accounts or create a savings bucket.

Any eventual transfer needs explicit user authorization for amount and destination, clear success/failure status, reconciliation, and duplicate-request protection. Do not trigger transfers by replaying an animation or retrying a failed network request blindly. Never store banking passwords in Unity or commit credentials. Do not implement live financial actions in a prototype.

## Hermes working agreement

- Read existing AGENTS.md and project documentation first, if present.
- Inventory the project and preserve working behavior. Use the minimum change needed for the current phase.
- Before implementation, provide an action plan, a **Current process** section describing what exists, and a **Projected changes** section describing the proposed behavior.
- Ask discovery questions in a small batch. Explain choices that need my input; do not make me choose routine implementation details.
- Keep a task list, decision log, research register, and concise session handoff current.
- Add root documentation and focused subsystem documentation as systems appear. Avoid duplicate or speculative documentation.
- Use version control checkpoints and meaningful commits where a repository exists. Do not commit credentials, generated caches, or supplied restricted assets to a public repository.
- Run relevant unit tests and a playable vertical-slice test for each implementation phase. Report what actually ran, what passed, and what could not be tested.
- If Unity/editor/build tools are unavailable, say so. Never describe code inspection as a successful playable test.
- At every phase end, report changes, validation, known limitations, asset requests, and the proposed next phase. Then stop for my review.

## Phased delivery plan

### Phase 0 — Discovery and project setup proposal

Inspect supplied files and tools. Confirm Unity version, target platform, project location, input method, available materials, and first printing. Present the minimum architecture and immediate research plan. Identify what exists versus what must be created.

**Done when:** I have reviewed a concrete plan and answered only the questions that affect the first slice. No financial or marketplace implementation.

### Phase 1 — Pack rules and data proof

Build the first set's card catalog and research register. Implement a pure pack generator with a replaceable, versioned rule definition. If research is incomplete, use an explicitly provisional model only with my agreement. Provide a diagnostic output showing the ordered contents of generated packs.

**Validation:** slot/count/eligibility invariants; deterministic test RNG; boundary tests for weighted choices; supported duplicate/order rules; batch distribution checks against the chosen model with sample size and statistical tolerance stated. Simulation validates the code against a model, not the model against history.

**Done when:** rules, confidence levels, unresolved evidence, and test results are reviewable.

### Phase 2 — Playable opening slice

Select one pack, generate and persist it, open a placeholder wrapper, reveal the ordered cards, inspect them, and view results. Resume an interrupted pack and commit cards exactly once.

**Validation:** full pack loop in Unity; restart during revealing; repeated completion input; recoverable save failures; no reroll or duplicate awards.

**Done when:** a desktop build or clearly identified editor-playable slice demonstrates the entire loop.

### Phase 3 — Opening feel and supplied materials

Request the specific assets needed. Improve wrapper tear, stack handling, timing, lighting, paper/foil finishes, sounds, and inspection. Use a small representative set of real materials before processing every card.

**Validation:** I review the opening feel; cards remain legible; finish behavior is appropriate; controls work without precise dragging; performance is checked on my hardware.

**Done when:** opening one pack feels good enough to justify expansion.

### Phase 4 — Binder and physical collection

Add persistent binder views, duplicates, completion, physical ownership editing, filters, and backup/export. Clearly separate virtual and physical holdings throughout.

**Validation:** repeat pulls; variant separation; collection edits; completion math; save migration; backup/restore; corrupt-save handling; end-to-end opening-to-binder flow.

**Done when:** I can use the binder to track my real collection independently of simulated pulls.

### Phase 5 — Jungle and Fossil

Research and add each set independently. Reuse established presentation and domain systems while retaining set-specific rules. Request assets in manageable batches.

**Validation:** each set's generator, ordering, eligibility, completion checklist, and mixed-set persistence. No accidental reuse of Base Set-only assumptions.

**Done when:** all three intended sets are playable with documented fidelity limits.

### Phase 6 — Physical card shopping

Research provider capabilities and present an integration proposal before implementation. Add exact-printing searches and reviewable listing comparisons with a manual fallback.

**Validation:** edition/holo/language mismatch exclusions; stale/missing listings; unknown shipping; provider/network failure; correct outbound links. No purchases during tests.

**Done when:** missing physical cards can lead to useful, correctly matched shopping choices.

### Phase 7 — Manual fund ledger

Add optional contribution planning, confirmed transfers, physical purchase entries, adjustments, and export. Keep opening available when the feature is disabled.

**Validation:** planned versus confirmed balances; transaction replay; correction entries; failed saves; no double counting.

**Done when:** I can use it as an honest record of the collection fund without connecting an account.

### Phase 8 — Optional finance feasibility spike

Research supported integration options and report practical limitations, costs, authorization flow, security needs, and maintenance. A finding that manual transfers are the best option is a valid outcome.

**Done when:** I can decide whether further work is worthwhile. Live integration is a separately approved implementation project.

## First-session prompt

> Read this handoff in full and inspect any existing project and AGENTS.md instructions. Begin with Phase 0 only. Tell me what currently exists, what you propose changing, and the small set of questions you need answered before the first playable slice. I can provide card art, logos, wrapper images, sound effects, and other materials as needed; request specific assets and use placeholders meanwhile. Do not invent historical pack probabilities or silently treat estimates as facts. Keep pack generation separate from presentation and preserve separate virtual and physical collections. Stop at the end of every phase for my review. Do not start marketplace, PayPal, or banking integration during the initial build.
