# Phase 1 — Pack rules and data proof

Status: COMPLETE / USER ACCEPTED. User explicitly accepted the delivered Phase 1 checkpoint and authorized Phase 2 with “yes i approve”. Model A 0.1.0 remains provisional and unchanged. This acceptance is of the data-proof checkpoint, not human playtesting of a playable opening. Machamp completion-target exception remains unanswered.

## Current process

The Unity project exists directly at the authorized root, using Unity 6000.2.14f1 and the Built-in Render Pipeline. Product name is the neutral working title Vintage Card Collector. The template's multiplayer-center package was removed; remaining built-in modules are not implemented services. Pure C# catalog/generation, source-derived JSON, package-free tests, CLI diagnostics, and an explicit Editor-only diagnostic menu now work. There is no scene, playable opening, collection award, save transaction, marketplace, financial integration, or box simulator.

## Verified setup

- Local Git repository initialized with no remote configured.
- Baseline documentation checkpoint: `f1162ec`.
- Unity project configuration checkpoint: `4952eee`.
- Unity project creation returned exit code 0.
- Second batch-mode import returned exit code 0 and logged successful batch exit, with no `error CS` compiler diagnostics. No custom game code existed during this import.
- The actual editor version file identifies 6000.2.14f1 (589824c1fc31).
- Graphics settings have no custom render pipeline assigned.
- Package lock no longer contains the removed multiplayer-center package.
- Unity's bundled Mono and C# compiler executed their version commands successfully. This is tooling availability, not a test-suite pass or compilation of game code.
- Logs are inside `.artifacts/logs/`; generated caches, development output, and future private supplied materials have Git exclusions.

### Observed diagnostics

Unity resolved Personal entitlements and continued despite an unavailable access-token refresh diagnostic. Both batch runs ended successfully. The logs also contain curl callback-aborted and Mono abort-thread shutdown messages. The initial build-graph cache miss (Bee exit code 4) was followed by a successful frontend regeneration and backend exit code 0. These diagnostics are retained in the local logs, not hidden or described as a completely pristine run.

## Projected changes and research gate

Catalog and pack-behavior research are complete for this checkpoint, with limitations retained. Parent reviewed both findings documents, re-ran their verification scripts (102-number checklist coverage, 29 catalog quote checks and 19 pack quote checks passed), visually checked the referenced collector weight table, and freshly re-fetched the primary metadata to verify exact source objects/hash after image-field removal. These are evidence checks, not generator tests. The user subsequently approved Model A's provisional order, probability, weighting and duplicate policies. See ../research/Pack_Research_Register.md and ../research/Provisional_Model_Proposal.md. The Machamp collection-target exception was unanswered and remains unresolved.

Any material approximation must be presented to the user and explicitly agreed before the generator implements it. Phase 1 approval authorizes research and code/data proof, not silent adoption of historical guesses.

The approved model is now implemented in a pure C# catalog and replaceable versioned generator with retained test-first cycles. Ordered results freeze stable printing IDs and model/catalog/RNG metadata before presentation. This phase does not implement opening animation, collection saves, shopping, finance, or box simulation.

## Verified implementation and how to inspect it

- Parent independently reran the bundled-Mono compiler with warnings-as-errors, all **21 C# tests**, **6 Python oracle/runner tests**, and **5 Python authoring tests**. All passed.
- Parent reran **100,000 packs / 1,100,000 cards** with seed 2463534242: **183 distribution checks passed**, **0 pack invariant violations**, **33,247 holo packs**. Repeated seeded reruns are not additional independent statistical samples.
- Distribution thresholds are `abs(actual-N*p) <= 6*sqrt(N*p*(1-p))+2`, with actual conditional/without-replacement expectations rather than naive sheet shares. Exact results, limits and per-card checks are in [domain verification](../verification/Domain_Verification.md).
- Actual Unity batch execution passed **4 Editor integration checks**, with no C# compiler warnings/errors in the final log: real JsonUtility parsing/generation; provenance/limits; complete named order matching the portable seed vector; and menu registration plus real project-local JSON/text export.
- Editor seed 4 demonstrates two Lightning Energies at positions 6–7 and holo Alakazam at position 8. This is generated diagnostic output, not a historical observed pack.
- In Unity, use **Tools > Vintage Collector > Phase 1 > Generate Diagnostic Pack**. It intentionally uses fixed seed 4 for a reproducible diagnostic, not a live pack-opening feature. Output: `.artifacts/editor/phase1-diagnostic-pack.json` and `.txt`.
- From the project root: `python -B tools/run_phase1.py --tests --diagnostics --batch 100000 --label local-check`; `python -B Tests/test_phase1_runner.py`; `python -B tools/test_phase1_data.py`.
- Explicit Unity batch check: `-executeMethod VintageCollector.EditorTools.Phase1EditorVerification.Run`, with `-projectPath` set to this root and `-logFile` under `.artifacts/logs`. No test runs automatically in ordinary Play or startup.
- There is no player executable or startup scene in Phase 1. Successful headless Editor checks do not establish rendering, opening feel, persistence/restart recovery, or a playable test. Those belong to Phase 2 and later.

API and exact bounded RNG mapping are frozen in [Domain API](../technical/Domain_API.md) and [implementation contract](../technical/Phase_1_Implementation_Contract.md). Parent data-authoring RED/GREEN logs are under `.artifacts/logs/data-authoring-*`; Editor cycles under `.artifacts/logs/phase1-editor-*`. The separate worker's verification document preserves the initially incorrect rejection oracle and its corrected RED/GREEN evidence rather than hiding it.

Citation verifier compatibility: the installed helper strips literal closing parentheses from plain source URLs. Parent verified research documents with an in-memory URL-token regex override that preserves those characters; no installed/profile skill files were changed because writes are restricted to this project. Source URLs and ledger entries were not altered to evade the issue.

## Tasks

- [x] Record explicit Phase 0 approval and Phase 1 authorization.
- [x] Create local Git baseline, scoped Unity project, and exercise editor import and compiler availability.
- [x] Verify source-grounded catalog/checklist evidence and identify printing-boundary decisions.
- [x] Verify composition/order/probability evidence and record limitations.
- [x] Obtain explicit agreement for provisional Model A's sheet-informed weights, order, independent 1/3 holo approximation, unique C/U draws and repeatable Basic Energies.
- [x] Author versioned catalog/rule JSON: 101 booster-eligible printings; preserve the 102-number research checklist and unresolved Machamp collection exception.
- [x] Exercise authoring guards with observed RED/GREEN cycles: five Python tests, including mutated approved rules, printing-specific #73 correction, unsupported schema, and unapproved completion-target changes.
- [x] Implement catalog and approved versioned generator with RED/GREEN evidence.
- [x] Produce ordered diagnostic packs and invariant/boundary/distribution test results.
- [x] Verify Unity compilation and real Editor JSON-to-domain diagnostic export.
- [x] Independently review code: no blocking security/correctness findings; parent verified all staged/live review pins unchanged before closing documentation.
- [x] Prepare verified local checkpoint and stop for Phase 1 user review. No remote configured or upload performed.
- [x] User explicitly accepted Phase 1 and authorized Phase 2. This is not physical/human playtesting of a playable opening.

## Independent review

Verified implementation checkpoint: `cda1fc1` (`[verified] feat: prove provisional Base Set pack generation and Unity diagnostics`). Closing documentation is a separate nonbehavioral checkpoint. Working tree and absence of remotes were checked after the implementation commit.

See [review verdict and code/data pins](../verification/Independent_Review.json) and [parent execution summary](../verification/Phase_1_Execution_Summary.json). The reviewer freshly reran the pure-code/runner/authoring/distribution checks, inspected existing Unity evidence without launching Unity, and reconciled Unity seed-4 output with fresh Mono output. No code/data changed after the approved review. Closing status-document edits are not behavioral changes.

Nonblocking research-helper suggestion retained for future cleanup: replace assert-only label validation in the one-time pack retrieval script with an explicit exception before using that script under optimized Python. This is not a game/runtime path; no new retrieval is required to use the offline generator. The review's stale-documentation suggestion was resolved here. Generated evidence/OCR whitespace and Unity's generated empty metadata values are deliberately preserved; handwritten production/test code retains normal whitespace checks.

## Asset requests

None. Text/metadata research does not require downloading branded art or sound. Placeholder asset references will remain replaceable.

## Session handoff

Phase 1 is explicitly user-accepted and Phase 2 is authorized. Preserve its generator/model/data baseline; the unresolved Machamp completion target is not implicitly answered. Current work and acceptance gates now live in Phase_2_Playable_Slice.md. All project-controlled writes stay in the authorized root; no uploads/services are authorized. Phase 2 uses placeholders and adds only the playable opening, recoverable generated-pack persistence and exactly-once virtual awards. Phase 3 and later remain gated. Phase 1 acceptance is not a claim of human-tested rendering/controls or exact factory collation.
