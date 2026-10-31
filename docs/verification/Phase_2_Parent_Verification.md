# Phase 2 parent verification — production Unity opening slice

Status: execution gates, both independent pre-commit reviews and the subsequent five-item human playtest all passed. User reported “all PASS”; see Phase_2_Human_Acceptance.json. Phase 3 still requires separate approval. Independent-review/execution JSONs retain their original point-in-time human-pending flags rather than rewriting historical evidence.

## Artifact

- Player: `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/Builds/Phase2Windows/VintageCardCollector.exe`.
- Keep the entire `Phase2Windows` directory together, including `_Data`, `MonoBleedingEdge`, and `UnityPlayer.dll`.
- Source scene: `Assets/VintageCollector/Scenes/Bootstrap.unity`. Open this scene and Play in Unity 6000.2.14f1. Its simple hierarchy is intentional: the tabletop/UI are generated on Play by `TabletopPresentation`.
- Build settings: Windows x64, Mono, D3D11, resizable window, default 1280×720, non-development build. Actual BuildReport: Succeeded, **0 errors / 0 warnings**.
- Rebuild using **Tools > Vintage Collector > Phase 2 > Build Windows Player**. Scene creation/build propagates the editor save-prompt cancellation before mutation outside batch mode.
- Build file pins and canonical manifest digest: `Phase_2_Player_Manifest.json`. User `Saves/**` is excluded from build identity, not included in the artifact pins.

## What actually ran

| Layer | Parent-observed result | Evidence |
| --- | --- | --- |
| Accepted generator baseline | 21 C# tests passed; Domain/catalog/model raw bytes still equal c349db4 | `.artifacts/domain/phase2-baseline-tests.json`; execution summary protected hashes |
| Pure persistence | **109 checks passed**, actual Mono compilation with warnings-as-errors; injected exceptions, abrupt test-child exits, corruption/unsupported schema, real sharing failures and process locks | `.artifacts/phase2-persistence/parent/results.json` |
| Pure path policy | **5 tests passed**, including real Windows directory junctions and default/explicit path guards | `.artifacts/phase2-paths/parent-initial.json` |
| Passive presentation | **5 pure groups passed** in parent rerun; separate worker retains isolated native/component/fixture evidence | `.artifacts/logs/phase2-presentation-parent.log`; presentation technical document |
| Real Unity controller/serializer | **13 groups / 297 named assertions passed** on parent native Editor rerun, count arrays reconciled | `.artifacts/phase2-runtime/parent-integrated.json` |
| Bootstrap | **4 native scene checks passed**, real serialized TextAssets/root, camera/listener and enabled startup scene | `.artifacts/phase2-build/scene-checks.json` |
| Actual production player | **5 verification suites / 111 driver checks passed**, **60 CRC/dimension-validated captures**, across **10 player process logs** | execution summary; five player verification JSONs below |

The driver checks include PNG validation/provenance/count reconciliation, not 111 distinct gameplay features. The runtime assertion count likewise includes per-boundary checks. The isolated renderer's synthetic fixture counts are not added to production-player totals.

### Production player scenarios

1. **Full loop and clean restart** (`integrated-01`): graphics-enabled player generated a fixed-seed pack, committed it before showing the wrapper, opened/revealed three cards, inspected and paused, then exited while incomplete. A different player process loaded identical transaction ID, ordered IDs, new flags, revision and reveal count, with zero generator invocations. It finished/skipped without additional awards, rejected repeated completed commands, then generated exactly one second pack. Repeating the deliberate seed produced no pity/new-card boosting and all duplicate flags. Primary/backup and real archive totals were read after exit.
2. **Prepared-write error/Retry** (same suite, separate isolated save): actual `after-prepare` fault blocked presentation; further Generate was rejected. Retry recovered/acknowledged the same result with one generator invocation and eleven awards total, then continued. Captured the actual error and recovered screens.
3. **Layouts** (`layouts-01`): real core-connected player rendered ready, sealed, revealing, inspection, pause and results at **1600×900, 2560×1080 and 1280×360**. PNG chunk CRCs and physical dimensions were checked. Main loop/input also ran at **1280×720**.
4. **Native mouse** (`mouse-01`): background Windows messages to the owned player's HWND drove actual GUI buttons through Generate, Open, Next, inspection/Back, Pause/Resume, Skip/results, result-card inspection and Quit. The random result at position eight was nonholo Electabuzz; actual holo inspection is separately demonstrated by the fixed-seed integrated/layout suites. Every required effect was confirmed by controller-state read-back, not inferred from PostMessage success. No user cursor movement or foreground-focus API was used.
5. **Actual Unity hard kill** (`hardkill-01`): native clicks generated/opened/revealed three cards, then the owned Unity player was forcibly terminated without OnApplicationQuit/Shutdown. A different player resumed identical transaction/cards/flags/revision at count three, with zero generation and eleven awards; native Next/Skip continued. This also demonstrates the OS writer lock releasing on real player death. Pre-crash image writes can be incomplete and are explicitly excluded from validated-capture totals.
6. **Native Escape** (`keyboard-01`): own-window key messages demonstrably opened pause, closed inspection as Back, and paused again. No persistent revision/award changed; explicit mouse Resume worked. This is native-message automation, not physical hardware/human playtesting.

All ten actual player logs were inspected programmatically for exception/error/warning terms; none matched. Editor licensing-token refresh/teardown diagnostics remain documented separately and are not called a completely pristine Editor run. An initial parent build-helper compilation failed because Unity's summary counters are Int32 rather than UInt32; it was corrected to the actual API type, the scene checks then passed, and the production build completed cleanly. Failed attempt logs remain retained.

## Render inspection

Parent actually viewed production captures for: count-three reveal, all-eleven results (including duplicate Energy), saved holo inspection, save-error Retry, ultrawide inspection and short-window Pause. Controls are in bounds and ordinary-size text is readable. Short-window typography is small; 1280×720 or larger is recommended. The renderer remains a deliberately generic flat tabletop study, not final authentic artwork/materials. Holo cue is bounded to placeholder illustration, not a full-face rainbow.

Representative files:

- `.artifacts/phase2-player/integrated-01/prepare/revealing-three.png`
- `.artifacts/phase2-player/integrated-01/resume/results-first.png`
- `.artifacts/phase2-player/integrated-01/resume/inspection-holo.png`
- `.artifacts/phase2-player/integrated-01/error/save-error.png`
- `.artifacts/phase2-player/layouts-01/1280x360/pause.png`
- `.artifacts/phase2-player/layouts-01/2560x1080/inspection-holo.png`

## Reproduce safely

From project root, using fresh labels (driver refuses to recycle directories):

```text
python -B tools/run_phase2_persistence.py --tests --label rerun
python -B tools/run_phase2_runtime_paths.py rerun
python -B tools/run_phase2_presentation.py --label rerun
python -B tools/verify_phase2_player.py --mode smoke --label smoke-rerun
python -B tools/verify_phase2_player.py --mode layouts --label layouts-rerun
python -B tools/verify_phase2_player.py --mode input --label input-rerun
python -B tools/verify_phase2_player.py --mode crash --label crash-rerun
python -B tools/verify_phase2_player.py --mode keyboard --label keyboard-rerun
```

Run graphical suites sequentially; focus interactions between separate windows would confound input tests. Do not use `-batchmode` or `-nographics` for graphical players: the former can suppress IMGUI even with D3D initialized. Tests use explicit project-local isolated save/artifact roots; they do not open, overwrite or delete live saves. At this checkpoint neither default Editor nor default player save directory had been created by tests.

Normal saves: Editor `<project>/Saves/Phase2`; player `<executable-directory>/Saves`. All are under the authorized root. The serialized approved-root policy intentionally rejects moving the player outside that root; portability elsewhere needs a separate scope/path change. Do not delete saves as a retry strategy. For manual backup, close the game and copy its whole save directory; export/import UI is a later phase.

## Remaining limits / human gate

- User subsequently accepted the presented opening/inspection, quit/resume, results/awards, controls/focus/window and placeholder feel/readability checks. Native-message automation itself did not establish these human passes; the direct “all PASS” report did. Sustained performance benchmarks, exhaustive hardware coverage and final material/audio preferences remain unestablished.
- Placeholder visuals and intentionally **silent** presentation. Volume/mute controls work as state but are not evidence of audio playback. Supplied art, wrapper textures, sounds and authentic foil/tear polish belong to Phase 3.
- No full binder editor, physical-ownership edits, completion denominator, other sets, marketplace, funds or transfers. Physical ownership is labelled not configured. Machamp completion exception remains unanswered and #8 remains absent from boosters.
- Save protection covers documented process-crash/recoverable-I/O cases, not arbitrary power loss, device failure, hostile edits or deletion of all evidence. Archive work is proportional to history and has a 32 MiB cap. No migration/compaction/import UI is promised here.
- Source/review/build verification and user acceptance are separate gates. Stop after Phase 2 delivery; do not begin Phase 3 without approval.
