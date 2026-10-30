# Phase 2 — Playable opening slice

Status: PHASE 2 DELIVERY READY — implementation, production-player verification and both independent reviews passed. Human playtest/acceptance remains pending; see Phase_2_Human_Playtest.md. Phase 3 is not authorized.

## Current process

Accepted baseline: cda1fc1 (implementation), c349db4 (closing documentation). Phase 1 generator/model/catalog are frozen; parent reran the 21 C# baseline tests successfully before Phase 2 work. An untracked ProjectSettings/PackageManagerSettings.asset was found and is being preserved without adoption into this change.

The accepted generator is now connected to durable persistence, passive tabletop presentation and a real Bootstrap scene. Parent reran 109 persistence checks, five presentation groups and native Unity controller checks (13 groups/297 assertions). The production Windows x64 Mono/D3D11 player built with zero errors/warnings, then passed five graphical/native verification suites. Domain/data remain byte-identical to the accepted baseline. See the parent verification document and execution summary for evidence, not just individual worker claims.

## Projected changes / action plan

1. Implement/test durable generated-pack transactions, exactly-once derived virtual holdings, saved wrapper/reveal progress, corruption handling and exclusive writer locks.
2. Implement a generic, clearly labelled placeholder tabletop, wrapper, stack, ordered card reveals/inspection and results. No branded material downloads; mouse/click controls, pause/focus handling and volume/mute.
3. Connect the unchanged generator to persistence and presentation in a Bootstrap scene. UI advances only after durable success; Retry preserves pending transaction/result/absolute reveal intent.
4. Export a Windows player under Builds/Phase2Windows. Editor saves stay under project/Saves/Phase2; player saves under its own build directory/Saves. Never use AppData persistentDataPath or PlayerPrefs.
5. Verify actual Unity and graphics-enabled player loop, multi-process restart at reveal, repeated completion, save-failure recovery, layout/screenshots and opt-in smoke isolation. Inspect real screenshots and separate scripted semantic checks from physical mouse/human feel.
6. Independent review, verified commit, then stop for Phase 2 human review before Phase 3.

## Decisions

- Phase 1 accepted; Phase 2 only authorized. Model A 0.1.0 remains explicitly provisional and byte-unchanged.
- Machamp's collection-target exception remains unanswered; it never enters boosters. Runtime catalog size is not a collection denominator.
- Virtual awards are derived once per durable transaction, not from reveal animation. No physical ownership writes or completion calculations in this phase.
- Physical tracking is not configured; do not present unknown physical ownership as confirmed missing/owned.
- Use one supported Base Set Unlimited choice and correctly proportioned generic placeholders. No asset request needed now; branded feel is Phase 3.
- Retain all manually controlled output under the authorized project root. The previously approved Unity-system-file exception remains unchanged.

## Validation so far

- Accepted domain baseline: 21 C# tests passed using bundled Unity Mono.
- Runtime path policy: five tests passed, with observed RED/GREEN logs. They cover default Editor/player locations, explicit isolated smoke directories, unknown/duplicate/unsafe overrides, canonical sibling/traversal escape rejection, and real Windows directory-junction rejection for explicit and default save paths.
- Reproduce path tests: `python -B tools/run_phase2_runtime_paths.py local-check`. Outputs/fixtures remain under .artifacts/phase2-paths. Resolution itself creates no files.
- Explicit smoke flags require a known scenario and separate project-local .artifacts save/artifact paths. Normal startup cannot accidentally use a smoke override; smoke tests cannot silently use the live save directory.
- Parent rerun: 109 persistence checks passed using real filesystem operations, fault/crash cases and process locks; five pure presentation groups passed. These validate owned layers, not the integrated game.
- Main-project native controller/serializer: 13 groups/297 named assertions passed on parent rerun. Bootstrap: four native wiring/build-list checks passed. Production BuildReport: Succeeded, zero errors/warnings.
- Actual production player: five suites, 111 driver checks, 60 validated screenshots and ten process logs; counts programmatically reconciled. Tests include full saved opening, clean separate-process restart, actual forced Unity termination and exact resume, repeat-completion/duplicate flags, prepared-write error/Retry without reroll, real HWND mouse-button effects and Escape/back/pause key-message effects. No cursor movement or foreground input API used.
- Actual captures were viewed for reveal/results/inspection/error, ultrawide inspection and short-window Pause. Ordinary-size labels/controls are readable and in bounds; tiny-height typography is small. Native injected input is not human hardware/feel acceptance.
- All tests used isolated .artifacts save directories; default Editor/player saves were not created or reset. No user collection data was overwritten.
- Still unverified: human enjoyment/timing/preferences, physical hardware/focus behavior and sustained performance. Presentation is intentionally silent and generic; real art/audio/foil/tear polish belongs to Phase 3.

## Tasks

- [x] Record acceptance/authorization and frozen contract.
- [x] Run Phase 1 baseline and pure runtime path/isolation tests.
- [x] Complete and independently rerun persistence implementation/tests (109 checks).
- [x] Complete passive presentation and independently rerun its pure tests (five groups).
- [x] Connect/test real controller, Unity serializer and UI.
- [x] Build Bootstrap and Windows player; run native/graphics/restart/failure/input checks.
- [x] Inspect actual production screenshots and reconcile reports/build pins.
- [x] Both independent reviews passed; no blocking security/correctness findings. Parent rechecked all 65 staged/live source entries and 135 player file hashes/membership against the frozen manifest.
- [ ] Record final local checkpoint and request Phase 2 human review.

## Review and remaining suggestions

Review verdicts/pins: ../verification/Phase_2_Independent_Review.json. No reviewed code/model/scene/build bytes changed after approval. One factual documentation phrase was corrected: the random native-mouse result inspection was nonholo Electabuzz, while fixed-seed suites separately prove holo inspection. Nonblocking future test suggestion: add recovery-specific sharing denial on backup/anchor work files; existing 109 persistence checks and documented limits remain accepted. Do not infer arbitrary power-loss or human performance guarantees.

## Candidate / controls

Player: `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/Builds/Phase2Windows/VintageCardCollector.exe`. Keep its whole directory together. Source scene: Assets/VintageCollector/Scenes/Bootstrap.unity. Click Generate, Open, Next or Skip; click revealed cards/history positions to inspect; Back/Escape closes inspection; Escape/Pause pauses; Resume is explicit. Quit at any point; next launch resumes saved progress. Do not delete saves to retry a storage error.

Player saves: <executable-directory>/Saves. Editor saves: <project>/Saves/Phase2. The root restriction intentionally remains enforced, so do not move this build outside the authorized project root. Source build/reproduction instructions and tested limits are in [parent verification](../verification/Phase_2_Parent_Verification.md), with [execution summary](../verification/Phase_2_Execution_Summary.json) and [player file manifest](../verification/Phase_2_Player_Manifest.json).

## Session handoff

Both independent reviews and parent pin verification passed. Freeze the tested source/build and stop for the bounded human checklist, not another implementation phase. Preserve the user-owned untracked PackageManagerSettings.asset and accepted Domain/data. User acceptance is not inferred from automation. No Phase 3/binder/physical/marketplace/finance work is authorized. Current asset request: none; request only a small representative batch when Phase 3 is approved. Intentionally silent generic presentation, physical/focus/feel/performance judgment and the unanswered Machamp completion target remain explicit limitations.
