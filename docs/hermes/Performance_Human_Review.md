# Shared back, Base wrappers and performance — focused player review

## Launch

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase5PerformanceWindows\VintageCardCollector.exe`

Keep the entire folder together. This is a separate candidate; all ten earlier player folders and their saves remain untouched. Its collection lives under `Builds/Phase5PerformanceWindows/Saves`; no automatic migration/merge occurs. To transfer progress, use the previous player's Create backup and this player's reviewed new-profile restore, then Quit/reopen. Do not overwrite raw saves.

## Fixed

- **One supplied Pokémon card back** is shared by Base, Jungle and Fossil.
- Base now uses **Blastoise, Charizard and Venusaur** fronts, selected by the same transaction-stable appearance helper as Jungle/Fossil. Retry/reopen retain the chosen design; variants need not cycle every three packs. Original filenames and images were not changed.
- Base art is pre-imported into a native media profile instead of being read, verified and decoded as a full PNG library on each new-candidate launch. Legacy fallback remains available.
- A lightweight startup scene displays actual scene-loading status before the heavy artwork scene, then waits for actual collection/media readiness. It does not use elapsed time as fake progress. Cached loads may jump between phases too quickly to show an intermediate fraction.
- Durable opening saves run on one serial worker. The UI stays active and shows **Saving...** while waiting. The next card, collection update and cue still appear only after durable acknowledgement. Duplicate actions cannot replace the pending operation.

## Measured results and limits

On this machine, with uncontrolled/warm OS caches and matched2560×1369 windows:
- Previous first complete pack framebuffer: **9.84 seconds** from launch, including initialization+Generate/Open+capture.
- New first complete pack framebuffer: **5.57 seconds**, readiness observed at **5.04 seconds**; separate reopen ready at **3.55 seconds**.
- Native durable-action dispatch median about **0.014ms**; saving acknowledgement still about **183ms median**. Rendering advanced734 frames during busy intervals and Saving was seen on58 storage operations. One frame-gap sample reached **198ms**; this is not a zero-stutter or cold-start guarantee.

The native series validated227 faces,15 Base mask bindings, all nine wrapper variants and three shared-back contexts; nine packs/99 cards; exact independent-process reopen; fault/Retry; worker-thread JsonUtility; and resource cleanup. An owned test copy was closed while a real prepared write existed: the worker joined, primary/backup matched and exit was normal without a forced kill. Full reentrant/focus/Binder tours and physical input acceptance remain separate from these tests.

## Please check these changed behaviors

1. Confirm the common blue back appears in every set and that Base no longer always shows the same front.
2. Check startup status is visible and packs/reveals feel more responsive. A brief Saving state is intentional: durability has not been weakened.

The first time you launch a newly built executable, disk cache/security scanning can differ from these runs. Report the observed startup time and any remaining pauses; the previous minute-long delay was not reproduced in the controlled baseline.

Source scenes: `StartupPerformance.unity` and `BootstrapPerformance.unity` in `Assets/VintageCollector/Scenes`. Execution/manifest: `docs/verification/Performance_Execution.json`.

Human acceptance is **approved**, explicitly reported as “good, approved, pelase continue.” All140 immutable f04 player files were rechecked unchanged without launching/rebuilding or traversing saves. See `docs/verification/Performance_Human_Acceptance.json`. Phase6 shopping work has resumed.
