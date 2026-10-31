# Phase 3 — three review-blocker fixes

## Status and boundaries

The three reported source blockers are fixed and exercised. **This is not review approval or corrected-player acceptance.** The parent must pin the corrected source, rebuild its candidate, and obtain independent re-review. The existing candidate and Phase 2 accepted build were not modified by this fix task.

Only the files listed below were edited outside `.artifacts`. No Git/index/staging/commit operations, installations, graphical-player execution, main-project Editor launch/automation, accepted-build writes, or live-save reads/writes were performed. The user-owned interactive Editor was not closed or driven. Native runs used the existing isolated `.artifacts/phase3-host/native-project` and `.artifacts/phase3-media-project`; synthetic saves, local symlink targets, reports, compiled binaries, caches and logs remained under `.artifacts`.

## Findings, RED, minimal fixes, GREEN

### 1. Host optional-manifest existence probe preceded leaf validation

The retained original reproduction is `.artifacts/phase3-review/host-path-probe-6338343c096e4088b70b0c1b2a9bec73/execution.log`: exit 0, `HOST_BRANCH_SELECTS_LEAF_SYMLINK=True`, `LIBRARY_REJECTS_LEAF_SYMLINK=True`. The original host called `File.Exists(candidate)` before `MediaFiles.Resolve` could reject the reparse leaf. A later library rejection did not undo that metadata probe or restore normal manifest selection.

New RED evidence used the actual source host and actual resolver inside isolated native Unity, not a reimplementation: `.artifacts/phase3-host/review-fix-host-red/results.json`. A real artifact-local file symlink to an original local synthetic JSON target was created, its reparse attribute checked, and the actual resolver rejected it. The original host nevertheless selected it. The run recorded 20 checks, with the last check failing (`Unsafe optional override is rejected before selection`), and process exit 1. No UNC target or network probe was used.

Minimal correction: a private `OptionalManifest` selector performs the artifact-prefix gate and full `MediaFiles.Resolve` lexical/root-to-leaf reparse validation **before** its existence delegate is called. `Initialize` supplies `File.Exists` directly; no existence probe remains before selection validation. Unsafe optional selection returns null locally so the unchanged `Assets/Supplied/AssetManifest.json` production fallback is still loaded. The library retains its existing open-handle/reparse revalidation before content reads. Public APIs and normal-play policy are unchanged.

GREEN `.artifacts/phase3-host/review-fix-host-green/results.json`: 26/26 checks, exit 0. This includes real unsafe-override rejection, a warning-free empty production-manifest binding with original foley, and the same private selector exercised with a counting delegate wrapping real `File.Exists`: zero delegate calls for the unsafe leaf; one call and safe fallback for a missing ordinary file or ordinary directory; one call and successful selection for an ordinary file. The private delegate seam exists only to make ordering observable; there is no new public API.

### 2. Protected-input hashing traversed and read accepted-build Saves

The old shared `protected()` used `rglob('*')` over `Builds/Phase2Windows` and `read_bytes()` on every file. It therefore **could read and hash save contents**, including live accepted-build saves when present. The helper performed no save writes; historical unchanged-hash results establish neither no-read privacy compliance nor permission to inspect save contents. Historical evidence is retained unchanged and is not represented as having complied with the no-live-save-read boundary.

The new regression imported both pure and native callers but replaced the shared module's ROOT with an entirely synthetic `.artifacts` tree before calling either `protected()`. It supplied `Saves`, `saves`, `sAvEs`, and `SAVES` beneath distinct synthetic parent directories (necessary on Windows), including nested canary files. Spies recorded both `os.scandir` traversal and `Path.read_bytes` content reads. The old runner was **never invoked against live user data** to reproduce this defect.

RED `.artifacts/phase3-review-fixes/saves-red/`: one unittest, two failing caller subcases, zero errors, exit 1. Each caller recorded 16 save-path scandir calls and four synthetic-save content reads. Both spy JSON files are retained.

Minimal correction: shared `protected()` now uses top-down `os.walk(..., followlinks=False)` and removes case-insensitive Saves directory names from `dirs[:]` before descent; save-named leaf files are also excluded before file inspection/hashing. Non-save protected inputs are still hashed. `run_phase3_media_native.py` already imports this exact shared function, so it required no edit.

GREEN `.artifacts/phase3-review-fixes/saves-final-green/`: one unittest, both caller subcases pass, zero errors, exit 0. Both callers have zero save scans and zero save content reads; each still hashes the synthetic non-save baseline. The subsequent real pure/native media runners were invoked only after this regression passed. Their protected snapshots contain no Saves keys and match before/after; no live saves were read to prove nonmutation.

### 3. Source scene lacked two MonoScript metadata files

RED `.artifacts/phase3-review-fixes/meta-red/`: one test, one failure, zero errors, exit 1. Of the five `m_Script` references in source `BootstrapPhase3.unity`, host GUID `423dd9dd93da97c42a2f7405f23b7cd4` and probe GUID `57127709d749a6247a8361bad6215c5d` lacked source metadata; the other three resolved.

Both source `.meta` files were absent. They were restored byte-for-byte from `.artifacts/phase3-build-project/Assets/VintageCollector/Runtime/` after a batch preflight that would stop before any copy if an existing source file conflicted. No GUID was generated and the source scene was not edited.

GREEN `.artifacts/phase3-review-fixes/meta-final-green/`: one test, zero failures/errors, exit 0. All five source-scene script references resolve uniquely to source `.cs.meta` files with corresponding `.cs` sources, and both restored files equal their known staged originals byte-for-byte. `scene-guids.json` retains the complete mapping.

## Executed commands and results

Run from the project root with quoted `cd` on this bracketed Windows path. Labels are intentionally unique: Python regression outputs refuse reuse.

| Command | Real result |
|---|---|
| `python -B TestsPhase3/ReviewFixChecks.py --test saves --label saves-red` | RED: 1 test, 2 caller failures, 0 errors; exit 1 |
| `python -B TestsPhase3/ReviewFixChecks.py --test meta --label meta-red` | RED: 1 test, 1 failure, 0 errors; exit 1 |
| `python -B TestsPhase3/ReviewFixChecks.py --test saves --label saves-green` | GREEN: 1 test, 0 failures/errors; exit 0 |
| `python -B TestsPhase3/ReviewFixChecks.py --test meta --label meta-green` | GREEN: 1 test, 0 failures/errors; exit 0 |
| `python -B TestsPhase3/HostRun.py --label review-fix-host-red --slice review-fix` | RED: 19 passed, 1 failed, 20 enumerated; exit 1 |
| `python -B TestsPhase3/HostRun.py --label review-fix-host-green --slice review-fix` | GREEN: 26/26; exit 0 |
| `python -B tools/run_phase3_media.py --label review-fix-pure-green` | GREEN: 13/13; compile and execution exit 0 |
| `python -B tools/run_phase3_media_native.py --label review-fix-media-native-green` | GREEN: 49/49; native process exit 0 |
| `python -B TestsPhase3/HostRun.py --label review-fix-host-all-green --slice all` | GREEN: 87/87; native process exit 0 |
| `python -B TestsPhase3/HostRun.py --label review-fix-host-play-green --slice play` | GREEN: 18/18; actual isolated native Play Mode; exit 0 |
| `python -B TestsPhase3/ReviewFixChecks.py --test saves --label saves-final-green` | Final harness/source pins: 1 test, both caller subcases pass, exit 0 |
| `python -B TestsPhase3/ReviewFixChecks.py --test meta --label meta-final-green` | Final harness/source pins: 1 test, all 5 script references resolve, exit 0 |
| `python -B .artifacts/phase3-review-fixes/verify_reports.py` | Final exit 0; validates reports, counts, process exits, source pins, exact host staging, protected snapshots, synthetic-spy results and empty production manifest |

The final report-verifier initially exited 1 twice because it assumed all reviewed documentation was unchanged. Comparison showed additional drift in `README.md`, `docs/hermes/Phase_3_Opening_Feel.md`, and `docs/verification/Phase_3A_Parent_Verification.md`; this subagent did not edit those files. The verifier was narrowed to require exactly the three intended existing code-source changes and explicitly records the other documentation drift rather than overwriting it or claiming it is unchanged.

## Source pins and evidence

Machine-readable fix-worker handoff reconciliation: `.artifacts/phase3-review-fixes/verified-summary.json`. At that handoff the listed GREEN input pins matched. Parent subsequently changed only the smoke observers Phase2SmokeRunner.cs (bounded asynchronous PNG completion) and Phase3MediaProbe.cs (explicit acknowledgement diagnostics), then rebuilt/retested the separate candidate. Historical broad host runs consequently differ from current inputs in those two observer files; the host/media implementation pins remain unchanged. Do not claim every historical pin equals current source. Current observer/build coherence is separately verified in Phase_3A_Corrected_Execution_Summary.json and Phase_3A_Independent_Review.json. Host runs have 37 source pins and 30 exact staged inputs; pure/native media compile pin counts remain 10/12. Python final outputs include six pinned harness/helper/scene/metadata inputs.

Key final SHA-256 values:

| Source | SHA-256 |
|---|---|
| `Assets/VintageCollector/Runtime/Phase3MediaHost.cs` | `90d0b4ad1b2f92bfeb1f8c5054546537ba72e9b634207d2b7706376a6e04957f` |
| `Assets/VintageCollector/Editor/Phase3MediaHostChecks.cs` | `b78787f7af5ccfa2b7053279d44d8fa617d8156407a25c56cc2f91f5b99e5900` |
| `tools/run_phase3_media.py` | `fc850bc8a28b7c8a658c2c0c3ab77b45e73b28291b5b6adbe1673af299690dcf` |
| `TestsPhase3/ReviewFixChecks.py` | `dd39e2f2b628ffb4631345e9d82079ee10b940948dab9a6a4d842f6194244694` |
| `Assets/VintageCollector/Runtime/Phase3MediaHost.cs.meta` | `c2e0a756f621bb540745e9765b26d9bb311083a11d4c01631724282be0064b33` |
| `Assets/VintageCollector/Runtime/Phase3MediaProbe.cs.meta` | `0517e6499fe015dd2c2514b30debf32249d2108b44477069dd20677e674170eb` |
| unchanged `Assets/VintageCollector/Scenes/BootstrapPhase3.unity` | `9792499abcf3c1e066fe12ab1c9327ea93967c887094bfb74f312e0eaf13d09f` |
| unchanged `Assets/Supplied/AssetManifest.json` | `f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8` |

The production manifest still has an empty `assets` list. Generic output remains generic; none of these tests establishes supplied-asset authenticity, physical hearing, corrected-player behavior or review approval.

## Exact deliverable edits

- Modified `Assets/VintageCollector/Runtime/Phase3MediaHost.cs`.
- Modified `Assets/VintageCollector/Editor/Phase3MediaHostChecks.cs`.
- Modified `tools/run_phase3_media.py`.
- Created `TestsPhase3/ReviewFixChecks.py`.
- Restored `Assets/VintageCollector/Runtime/Phase3MediaHost.cs.meta`.
- Restored `Assets/VintageCollector/Runtime/Phase3MediaProbe.cs.meta`.
- Created this `docs/technical/Phase_3_Review_Fixes.md`.

Everything else created/updated by this task is isolated evidence or staged test input below `.artifacts`. `tools/run_phase3_media_native.py` and `TestsPhase3/HostRun.py` were exercised unchanged. Remaining gates belong to the parent: freeze/pin the corrected sources, rebuild the candidate without modifying Phase 2 acceptance, rerun required candidate/player verification, and obtain independent re-review. No review-pass or task-closure claim is made here.
