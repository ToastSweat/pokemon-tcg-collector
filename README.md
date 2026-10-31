# Vintage Card Collector

Private, personal Unity desktop project. Neutral working title; not a redistribution or licensing claim.

## Current status

**Phase 4 is accepted:** the user reported **all five review checks PASS**. The136-member player manifest was rechecked unchanged without reading live saves, and no rebuild was performed. The ordinary card11 table stop and explicit **View pack results** remain intact. Both binders target **101 unstamped eligible printings, excluding Machamp**. See [human acceptance](docs/verification/Phase_4_Human_Acceptance.json).

The user confirmed the supplied art and other functionality PASS; that acceptance is preserved. The requested mouse-over tilt / catching-light correction is now implemented in a separate, automated-player-verified and independently reviewed candidate. Moving across cards or the pack controls pitch/yaw and normal-dependent masked reflection; leaving eases neutral. Only focused human tilt/finish feedback remains pending; no Phase5. [Feedback](docs/verification/Materials_Human_Feedback.json) · [Tilt contract](docs/technical/Hover_Tilt_Contract.md).

## Mouse-over tilt candidate — focused retest

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase3TiltWindows\VintageCardCollector.exe`

Keep the whole folder under this project. Move the mouse across the pack, revealed cards or inspection left/right/up/down—no click, drag or wheel needed for tilt. Perspective and masked light respond to the viewing angle; ordinary cards remain matte. Original hitboxes, card11 stop, Binder, saves, odds and supplied images are unchanged. This separate candidate has its own save root and does not overwrite the previous material/Phase4 players. Source scene: **Assets/VintageCollector/Scenes/BootstrapTilt.unity**.

[Focused tilt retest](docs/hermes/Hover_Tilt_Human_Retest.md) · [Actual native input/build proof](docs/technical/Tilt_Build_Player.md) · [Frozen player pins](docs/verification/Tilt_Player_Execution.json). Build36 succeeded0errors0warnings; actual hover at1280x720/1600x900/2560x1080, full game/retry/import/restart and independent review gates passed. Human feel/perceptibility is not inferred from those tests.

## Supplied-art / foil candidate — ready for visual review

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase3MaterialsWindows\VintageCardCollector.exe`

Keep the entire folder together inside this project. Source scene: **Assets/VintageCollector/Scenes/BootstrapMaterials.unity**. The supplied101eligible card faces, back,15holo-mask bindings and masked wrapper materials load at original resolution. Reflection is mask-confined; printed HP/attacks/copyright remain unobscured. Charizard is the initial wrapper front; alternate supplied fronts remain available without changing pack RNG. Binder keeps its accepted checklist layout.

This candidate has its own save root; accepted Phase4Windows and all previous collections remain untouched. The old empty media manifest is unchanged, so adding these materials does not silently change accepted older binaries. Optional progress transfer uses exported ZIP/reviewed new-profile restore, not raw-save overwrite; see the checklist.

[Three-item visual review](docs/hermes/Materials_Human_Visual_Review.md) · [Build / actual input and restart proof](docs/technical/Materials_Build_Player.md) · [Exact execution and pins](docs/verification/Materials_Player_Execution.json). Native build succeeded with0errors/0warnings, full source/compiler/player closure and independent reviews passed. All art remains private/local; no uploads or source-image changes.

**The earlier Phase4 baseline passed independent reviews and the user's five checks.** Its accepted player/save folders below remain preserved and separate from the new materials visual-feedback gate; no automatic migration or merge is performed. See [current status](docs/hermes/Phase_4_Binder_Discovery.md), [implementation contract](docs/technical/Phase_4_Implementation_Contract.md), [collection core](docs/technical/Phase_4_Collections.md), [runtime](docs/technical/Phase_4_Runtime.md), and [actual player evidence](docs/technical/Phase_4_Build_Player.md).

## Phase 4 — binder / physical ownership candidate

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase4Windows\VintageCardCollector.exe`

Keep the **entire Phase4Windows folder** together and inside this project. It starts its own collection under `Builds/Phase4Windows/Saves`; older players and their progress are untouched. Do not overwrite a populated save folder to import earlier progress. Schema1 legacy records remain compatible; moving an older player's collection is a separate deliberate closed-save operation, not an automatic merge.

- **Binder / Tab** opens the binder. Choose **Virtual revealed pulls** or **Physical ownership**; use search, All/Owned/Missing/Duplicates/Holo and pages. Unrevealed active-pack awards do not spoil binder ownership/completion.
- Physical edits require selecting a printing, condition and quantity, then **Confirm ownership**. Conditions are independent quantity buckets; zero removes only that bucket. Opening packs never changes physical ownership.
- **Create backup**, select a backup and **Review restore**. Cancel changes nothing. Confirmation creates a **new profile for next launch**, preserving the old profile; Quit/reopen activates it. It is not an overwrite or merge. A successful restore locks further changes until restart.
- A failed acknowledged operation exposes **Retry SAME ownership save / reviewed restore**. Do not replace its intent, delete saves or create a new transaction to recover it.
- If forcibly stopped during an unfinished restore, startup can block while preserving all evidence. This candidate has no cross-process recovery wizard; ask for scoped recovery assistance rather than deleting pending/profile/save files. Normal confirmed restore/restart and same-process Retry are verified.

Editable source scene: **Assets/VintageCollector/Scenes/BootstrapPhase4.unity** in Unity6000.2.14f1. Generic visuals appear on Play. Editor uses its existing separate Editor save root; do not use live Editor progress for automation.

[Focused human review](docs/hermes/Phase_4_Human_Playtest.md) · [Build / native-player evidence and reproduction](docs/technical/Phase_4_Build_Player.md). Stop before Phase5 pending user review.

- [Authoritative phased handoff](docs/hermes/Hermes_Vintage_Card_Collector_Handoff.md)
- [Approved discovery proposal](docs/hermes/Phase_0_Discovery.md)
- [Accepted Phase 1 checkpoint](docs/hermes/Phase_1_Data_Proof.md)
- [Accepted Phase 2 checkpoint](docs/hermes/Phase_2_Playable_Slice.md)
- [Current Phase 3 status and session handoff](docs/hermes/Phase_3_Opening_Feel.md)
- [Small representative asset request](docs/hermes/Phase_3_Asset_Request.md)
- [Pack research register](docs/research/Pack_Research_Register.md)

Confirmed direction: Windows desktop, Unity 6000.2.14f1, and 1999 English Base Set Unlimited. Other date/printing variants require separate research before support.

All project-controlled files, future Unity project folders, builds, test artifacts, supplied assets, and development saves must stay inside this directory. Unity's normal system-managed logs, licensing state, and global caches are explicitly permitted outside it. Do not create a sibling project or place project files in the parent directory.

No supplied art is present. Do not scrape or bundle branded assets, publish materials/builds, implement marketplace services, or connect financial accounts under the current phase.

## Latest corrected table flow

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase3TableFlowWindows\VintageCardCollector.exe`

Card11 remains on the normal opening table, not a forced inspection. Click **View pack results** to continue. Optional inspection/Back returns to the table; Skip is still direct. This separate build has its own collection; older players/saves are preserved. The previous Phase3FinalCardWindows modal workaround is superseded.

[Correction and evidence](docs/hermes/Final_Card_Table_Flow.md).

## Test Phase 3A — separate generic-polish candidate

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase3Windows\VintageCardCollector.exe`

This candidate starts its **own collection** in `Builds/Phase3Windows/Saves`. Your accepted Phase 2 player/collection below are unchanged; nothing is migrated, merged or reset. Keep the whole candidate directory together and inside this project root.

Open `Assets/VintageCollector/Scenes/BootstrapPhase3.unity` to preview source in Unity. Editor play continues to use the existing Editor-only save folder, not either standalone folder. The interactive original Editor was left open; this candidate was built from a byte-verified project-local stage.

Changes: original generic committed-action foley, immediate mute/pause/no-replay behavior, subtle depth/motion/foil cues and optional stable-ID images/WAVs through the empty private manifest. The actual production manifest has **zero supplied assets**. Real faces are never repainted; their foil requires an aligned explicit mask. Branded materials and human audio/visual approval remain deferred.

- [Phase 3A production/audio/render verification](docs/verification/Phase_3A_Parent_Verification.md)
- [Six-item generic audio/visual playtest](docs/hermes/Phase_3A_Human_Playtest.md)
- [Corrected independent reviews and exact pins](docs/verification/Phase_3A_Independent_Review.json)
- [Candidate execution summary](docs/verification/Phase_3A_Corrected_Execution_Summary.json)
- [Candidate file/hash manifest](docs/verification/Phase_3A_Corrected_Player_Manifest.json)
- [Media formats, provenance and limits](docs/technical/Phase_3_Media.md)

The Phase 3 build helper refuses populated candidate saves. Do not delete a collection to rebuild; close and preserve the whole save directory or use an explicitly reviewed new candidate path.

## Play the accepted Phase 2 baseline

Windows player:

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase2Windows\VintageCardCollector.exe`

Keep its entire directory together with `_Data`, `MonoBleedingEdge` and `UnityPlayer.dll`. The approved-root policy intentionally requires this build to remain somewhere under this project root.

In Unity 6000.2.14f1, open **Assets/VintageCollector/Scenes/Bootstrap.unity** and press Play. The sparse editor hierarchy is intentional: tabletop visuals/UI appear on Play.

- Click **Generate**, **Open**, then **Reveal next card**, or **Skip to results**.
- Click a revealed card or its numbered history position to inspect; **Back/Escape** returns to the table.
- **Escape/Pause** pauses; **Resume** is explicit, including after focus loss. Volume/mute controls are present, but this placeholder presentation is intentionally silent.
- Quit at any point. The next launch resumes the same stored transaction, order, wrapper state and reveal progress. Completion does not award again.
- Storage error: use **Retry pending action** or Quit; never delete saves as a retry strategy.

Standalone saves live in **Builds/Phase2Windows/Saves**; Editor play saves live in **Saves/Phase2**. These are separate development/player collections, not automatically merged. For manual backup, close the game and copy the entire corresponding save directory. Backup/export/import UI is not implemented yet.

Rebuild with **Tools > Vintage Collector > Phase 2 > Build Windows Player**. It regenerates the controlled Bootstrap scene from source; preserve any intentional scene customizations separately and back up existing closed player saves before rebuilding over the player directory.

Rules remain the approved **provisional Model A 0.1.0**, not exact factory collation. Generic card faces/wrapper are clearly labelled placeholders. Physical tracking, binder editing, supplied art/audio/feel polish and later roadmap phases are not implemented here.

- [Production-player checks, captures and limits](docs/verification/Phase_2_Parent_Verification.md)
- [Five-item human playtest checklist](docs/hermes/Phase_2_Human_Playtest.md)
- [Independent reviews and code/build pins](docs/verification/Phase_2_Independent_Review.json)
- [Execution summary](docs/verification/Phase_2_Execution_Summary.json)
- [Player dependency/file pins](docs/verification/Phase_2_Player_Manifest.json)

## Inspect the accepted Phase 1 data proof

Open **this directory** in Unity **6000.2.14f1**. Use **Tools > Vintage Collector > Phase 1 > Generate Diagnostic Pack**. This explicit Editor-only command generates the fixed seed-4 example and writes readable text plus JSON under `.artifacts/editor/phase1-diagnostic-pack.*`. It does not award cards or save a collection.

This older diagnostic menu is separate from the playable Phase 2 scene above. It remains fixed-seed data proof and does not award cards.

From this project root, using Python and the installed Unity bundled Mono/compiler:

```bash
python -B tools/run_phase1.py --tests --diagnostics --batch 100000 --label local-check
python -B Tests/test_phase1_runner.py
python -B tools/test_phase1_data.py
```

No runtime API or online card service is required. Model A `0.1.0` remains a documented approximation, not exact factory collation. The101 runtime printings exclude starter-only Machamp. The user has separately selected those101 unstamped printings as both completion targets; the102-number research checklist is retained as provenance. Only completion approval metadata changed, not card identities, catalog compatibility version, pack probabilities or historical save records.

- [Domain API and portable RNG contract](docs/technical/Domain_API.md)
- [Detailed distribution/test evidence](docs/verification/Domain_Verification.md)
- [Approved model and unresolved scope decision](docs/research/Provisional_Model_Proposal.md)
