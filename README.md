# Vintage Card Collector

Private, personal Unity desktop project. Neutral working title; not a redistribution or licensing claim.

## Current status

Phase 1 is accepted. Phase 2's **working Windows opening slice is ready for your playtest**: implementation, actual-player verification and both independent reviews passed. Approved provisional Model A `0.1.0` is unchanged. Human acceptance remains pending, Machamp's collection-target exception is unanswered, and Phase 3 remains gated.

- [Authoritative phased handoff](docs/hermes/Hermes_Vintage_Card_Collector_Handoff.md)
- [Approved discovery proposal](docs/hermes/Phase_0_Discovery.md)
- [Accepted Phase 1 checkpoint](docs/hermes/Phase_1_Data_Proof.md)
- [Current Phase 2 status and session handoff](docs/hermes/Phase_2_Playable_Slice.md)
- [Pack research register](docs/research/Pack_Research_Register.md)

Confirmed direction: Windows desktop, Unity 6000.2.14f1, and 1999 English Base Set Unlimited. Other date/printing variants require separate research before support.

All project-controlled files, future Unity project folders, builds, test artifacts, supplied assets, and development saves must stay inside this directory. Unity's normal system-managed logs, licensing state, and global caches are explicitly permitted outside it. Do not create a sibling project or place project files in the parent directory.

No supplied art is present. Do not scrape or bundle branded assets, publish materials/builds, implement marketplace services, or connect financial accounts under the current phase.

## Play Phase 2

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

No runtime API or online card service is required. Model A `0.1.0` is a documented approximation, not exact factory collation. The 101 runtime booster printings exclude starter-only Machamp; they do **not** establish a collection-completion denominator. The 102-number research checklist is preserved separately.

- [Domain API and portable RNG contract](docs/technical/Domain_API.md)
- [Detailed distribution/test evidence](docs/verification/Domain_Verification.md)
- [Approved model and unresolved scope decision](docs/research/Provisional_Model_Proposal.md)
