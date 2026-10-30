# Vintage Card Collector

Private, personal Unity desktop project. Neutral working title; not a redistribution or licensing claim.

## Current status

Phase 1 (pack rules and data proof) is **ready for user review**: implementation, parent test execution, Unity Editor integration, and independent code review passed. Approved provisional Model A `0.1.0` is implemented. The Machamp collection-target exception remains unanswered; Machamp is excluded from boosters. Phase 2 remains gated; there is no playable build yet.

- [Authoritative phased handoff](docs/hermes/Hermes_Vintage_Card_Collector_Handoff.md)
- [Approved discovery proposal](docs/hermes/Phase_0_Discovery.md)
- [Current Phase 1 status and session handoff](docs/hermes/Phase_1_Data_Proof.md)
- [Pack research register](docs/research/Pack_Research_Register.md)

Confirmed direction: Windows desktop, Unity 6000.2.14f1, and 1999 English Base Set Unlimited. Other date/printing variants require separate research before support.

All project-controlled files, future Unity project folders, builds, test artifacts, supplied assets, and development saves must stay inside this directory. Unity's normal system-managed logs, licensing state, and global caches are explicitly permitted outside it. Do not create a sibling project or place project files in the parent directory.

No supplied art is present. Do not scrape or bundle branded assets, publish materials/builds, implement marketplace services, or connect financial accounts under the current phase.

## Inspect the Phase 1 data proof

Open **this directory** in Unity **6000.2.14f1**. Use **Tools > Vintage Collector > Phase 1 > Generate Diagnostic Pack**. This explicit Editor-only command generates the fixed seed-4 example and writes readable text plus JSON under `.artifacts/editor/phase1-diagnostic-pack.*`. It does not award cards or save a collection.

There is no playable scene or player executable yet. Pressing Play is not a pack-opening demonstration in Phase 1; that is the separately gated Phase 2.

From this project root, using Python and the installed Unity bundled Mono/compiler:

```bash
python -B tools/run_phase1.py --tests --diagnostics --batch 100000 --label local-check
python -B Tests/test_phase1_runner.py
python -B tools/test_phase1_data.py
```

No runtime API or online card service is required. Model A `0.1.0` is a documented approximation, not exact factory collation. The 101 runtime booster printings exclude starter-only Machamp; they do **not** establish a collection-completion denominator. The 102-number research checklist is preserved separately.

- [Domain API and portable RNG contract](docs/technical/Domain_API.md)
- [Detailed distribution/test evidence](docs/verification/Domain_Verification.md)
- [Approved model and unresolved scope decision](docs/research/Provisional_Model_Proposal.md)
