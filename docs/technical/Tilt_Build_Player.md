# Separate tilt candidate — builder and player gate

## Parent current result — actual candidate / pointer proof and independent reviews pass; focused human feedback pending

Source compilation dependency was corrected by the surface owner using reflective isolated-fixture lookup; no test fixture was injected into production. Parent adopted only the last missing Editor check meta from a byte-equal actual native integration stage, refusing an older stage's stale source hash first. Source/meta record: `Tilt_Meta_Adoption.json`.

Actual scene `BootstrapTilt.unity` and **parent-tilt-build-36** were natively generated/built in the exact owned stage; build admitted/Succeeded0errors0warnings,432source inputs/436compiler inputs (only4validated folder metas),136candidate members. Other old players, manifests and saves are preserved; no live saves were traversed.

| Current native suite | External checks | Captures / material images | Owned messages |
|---|---:|---:|---:|
| parent-tilt-targets-skip-green-37 (1600×900) |416|36|139|
| parent-tilt-hover-720-final-38 |416|36|139|
| parent-tilt-hover-wide-final-39 |416|36|139|
| parent-tilt-demo-final-40 |23|15|separate demo|
| parent-tilt-services-final-41 |36|17|separate services/reopen|
| parent-tilt-input-final-42 |592|169|separate input/reopen|

All final native commands exited0 and report counts/pins/current source match. The three hover runs prove actual WM_MOUSEMOVE across wrapper/matte/holo/inspection/results in all4directions, original bounds stable, no hover writes/reveals/awards/cues, leave-centering, button hover passivity and freeze gates. Direct Skip is additionally exercised on an actual second pack; total22awards/2generations, no duplicate completion. Gameplay-native73services+176input and actual-material-native426demo+800services+800input are separate evidence layers, not combined unique tests. Parent decoded all309PNG paths (245distinct contents). Full exact source/compiler/player/report/framebuffer pins: `../verification/Tilt_Player_Execution.json`, `Tilt_Player_Manifest.json`.

Retained real failed attempts: first720hover28 timed out because WM_ACTIVATEAPP alone did not establish focus loss; driver now pairs owned-window activation/app/focus notifications as the integration fixture requires, with no foreground/global APIs. Actual Skip/results-target RED35 exposed an outdated source-pinned diagnostic primary rectangle and missing Skip target; corrected in the new TiltSmokeProbe only, no game/hitbox change, then freshbuild36 and all final suites passed. `controlsSource` explicitly says source-pinned table layout, not observed registration. Parent loaded actual left/right inspected-holo and wrapper-right frames: perspective/finish changes visibly present, no observed print overwrite/edge clipping. Human feel and perceptibility remain a separate focused retest.

Independent surface/integration review passed; final candidate review's sole input-ownership blocker was corrected and its targeted re-review passed. No human PASS, phase closure or Phase5 is inferred. Earlier blocked-native paragraphs below are historical, not current build status.

---

## Input ownership correction / final guarded suites

Final candidate review rejected INPUT-OWNERSHIP-001: inherited gameplay-input helpers posted without the per-message live-PID guard already present in hover mode. Parent removed those unguarded globals/imported click helper and routed every input mouse/key/character—including nested quantity/search/quit clicks—through `OwnedWindowInput`. Each individual message checks the launched process is live, queries the HWND's current PID, rechecks process liveness, then posts. Four pure regression cases observed missing-boundary RED→GREEN:6messages/6queries, stale owner, dead process, and recycled owner mid-click stopping further posts.

During the fresh run, hover45 hit a genuine Windows read-sharing denial while atomically replacing its diagnostic request; old complete sequence1 and work sequence2 were preserved. The Python publisher now retries the SAME complete body/sequence only for Windows5/32/33, bounded101attempts, preserving both files on persistent failure and propagating unrelated errors. Three pure RED→GREEN cases establish transient success/persistent bounded refusal/other-error propagation. No gameplay/binary source changed for either correction.

Current frozen-driver native evidence: hover90049/hover72050/hoverwide51 each416checks/139owned messages/36captures; demo52 has23external/426material assertions/15images; services53 has36external/73gameplay/800material/17images; input54 has587external/171gameplay/800material/164images. All exit0 with current runner pins and same build36. Parent revalidated304PNG paths/247distinct contents, all counts and source/compiler/player preservation. Earlier37-42/43-45 remain point-in-time success/failure evidence, not current driver pins. Surface/integration independent review and targeted tool-boundary re-review passed with no blocking security/logic findings. The latter independently reran4ownership+3publication tests and reconciled all six final native reports and unchanged432source/436compiler/136candidate pins. No human perceptibility/feel PASS is inferred.

## Historical independent tooling handoff (superseded by current result above)

The actual isolated Unity invocation `20-native-scene` exited **1**, before the new scene generator could execute:

```text
Assets/VintageCollector/Editor/TiltSurfaceChecks.cs(12,81): error CS0246:
The type or namespace name 'SurfaceGpuFixture' could not be found
```

That foreign surface-worker Editor check depends on a fixture not present in the production Assets compiler closure. It was not edited, removed, patched in the stage, or replaced with a fake fixture. The subsequent `22-dependent-build-gate` correctly refused the missing production tilt scene without restaging or invoking another Editor. No worker-file polling or wait loop was used.

**No tilt candidate exists, no player was launched, no HWND was obtained, and no player framebuffer was captured by this worker.** Actual mouse response, the three player sizes, native hover assertions, and retained gameplay regression suites are therefore **not verified**. Helper/GPU results from other workers are not whole-player evidence. The source scene GUID-closure test remains RED until actual native scene generation succeeds.

Final forensic record: `.artifacts/tilt-build/dependent-gate-pins.json` contains **425 current source inputs**, **434 actual post-import stage inputs**, all source/stage hashes, missing source metas, exact source drift since the native attempt, native metadata provenance, and preservation maps. These are failed-attempt forensic pins, not an admitted compiler or candidate manifest.

## Created files and APIs

- `Assets/VintageCollector/Editor/TiltBuildPolicy.cs` and its actual native `.meta`: read-only `ValidateProject`, `RequireOwnedStage`, `Resolve`, `ImmutableManifest`.
- `Assets/VintageCollector/Editor/TiltBuild.cs` and its actual native `.meta`: `CreateBootstrap()` and `BuildWindows()`.
- `Assets/VintageCollector/Runtime/TiltSmokeProbe.cs` and its actual native `.meta`: `ValidateOptIn(GamePaths)` and `ValidateRequest(string,int)`; inert ordinary startup, dedicated bounded hover observation/capture coroutine.
- Explicit suites: `TestsTilt/BuildPolicyTests.cs`, `BuildToolTests.py`, `BuildEntrypointTests.py`, `PlayerOptInTests.cs`, `PlayerVerifierTests.py`.
- Owned tools: `tools/run_tilt_build.py`, `run_tilt_build_policy.py`, `run_tilt_build_compile.py`, `verify_tilt_player.py`.
- This document. Intended native-generated source scene: `Assets/VintageCollector/Scenes/BootstrapTilt.unity` plus meta; **not generated yet**.

Both Editor entrypoints call common admission before mutation. `Project()` calls `TiltBuildPolicy.RequireOwnedStage(Path.GetDirectoryName(Application.dataPath),ApprovedRoot)`. The policy canonicalizes BOTH operands through Full/GetFullPath and compares with `StringComparison.OrdinalIgnoreCase`. The forward-slash approved literal is never compared directly to a canonical Windows path. Main-project paths in either slash spelling are rejected; only exact `.artifacts/tilt-build-project` is admitted. Populated case-insensitive candidate Saves reject before directory creation, scene mutation, source traversal, or native build.

The new scene generator resolves the actual compiled runtime types before creating a scene. It includes OpeningGame, TabletopPresentation, Phase3MediaHost, Phase4CollectionHost, BinderPresentation, unchanged Phase4SmokeProbe, existing MaterialsSmokeProbe, optional TiltSmokeProbe, and Camera/AudioListener. It preserves catalog/model references and explicitly sets only the new scene's `pointerTiltEnabled=true`, material manifest `Assets/Supplied/BaseSetMaterials/Manifest.json`, and `fullBaseSetBudget=true`. It does not replace EditorBuildSettings scenes.

The separate output is exclusively `Builds/Phase3TiltWindows/VintageCardCollector.exe`. The final builder requires exact delivered source metas before staging, exact source/stage equality, a successful native BuildReport, complete compiler closure, and unchanged current source afterward. Only bounded, actual, hash-validated native folder metas with one GUID, `fileFormatVersion: 2`, `folderAsset: yes`, an actual corresponding directory and `DefaultImporter` may be compiler extras. Extra scripts/asset metas are not final-build exceptions.

## Native metadata handoff

The unsuccessful native import still generated **8 actual new asset metas**. The exact records are in `.artifacts/tilt-build/20-native-scene/native-source-meta-generated.json`, including stage path, SHA-256 and source-content SHA-256 at import.

Only this worker's **3 owned CS metas** were copied byte-exactly back to source. **5 foreign metas remain unadopted**, for:

1. `Assets/VintageCollector/Editor/TiltPresentationChecks.cs`
2. `Assets/VintageCollector/Editor/TiltSurfaceChecks.cs`
3. `Assets/VintageCollector/Presentation/HoverTiltMotion.cs`
4. `Assets/VintageCollector/Presentation/TiltedSurfaceRenderer.cs`
5. `Assets/VintageCollector/Resources/Presentation/TiltedSurface.shader`

The parent/owners must resolve those with the recorded actual native metas, never synthesized GUIDs or overwritten existing sidecars. Recheck each source hash and GUID uniqueness before adoption. The final build's strict meta gate remains unchanged.

`--prepare-scene` alone has a separately tested metadata-bootstrap exception for the exact eight new tilt asset paths above, not arbitrary source assets. It can import to obtain actual sidecars and generate only the new owned scene. This is not final build admission. Normal final `--build` permits no missing source asset metas.

## Probe and driver protocol

`ValidateOptIn` returns null immediately for null/non-smoke paths and remains inert for unrelated smoke launches without the additional marker. An exact, BOM-free, bounded UTF-8 `tilt-fixture.json` is accepted only under the existing GamePaths smoke sandbox:

```text
.artifacts/phase4-player/<safe-label>/{services,input,reopen}
.artifacts/phase4-player/<safe-label>/session
```

The existing `--vcc-smoke=input`, save-root and artifact flags must agree. Exact marker modes are `observe` and `hover`; `hover` is admitted only in `input`. No new application flag, root override, gesture, button, wheel command or pointer-injection API was added.

Normal play and `observe` add no generation, input, writes or captures. Dedicated `hover` disables MaterialsSmokeProbe and Phase4SmokeProbe before their Start methods, preventing the known material/legacy autoquit race. Existing services/input/demo modes remain separate: Materials(-150) performs actual asset verification before releasing unchanged Phase4 regression automation. No legacy reports are fabricated.

Only in the dedicated hover fixture, the exact bounded next-sequence request permits `capture`, `seed4` or `fault`. `seed4` invokes the existing real generator once in an empty owned session; `fault` invokes the existing smoke failed-commit hook. Neither is pointer injection or ordinary startup activity. Captures synchronously read the real final framebuffer at end-of-frame. Detached snapshots copy the actual TabletopPresentation `TiltObservations` fields (`key`, original logical `bounds`, pitch/yaw, hovered, finish, renderFrame), plus actual pointer observations, screen scale, game/view/revision, audio cue count, clock, and owned authoritative-file SHA-256 pins. The probe does not use these DTOs as input authority.

The owned `hover` driver verifies HWND ownership against its launched PID before every background message, maps original logical rectangles through actual UI scale/client dimensions, and posts only owned-window messages. It requires four signed pose changes, stable original rectangles, one current hovered surface, actual pointer acknowledgement and actual PNG border/interior differences; center/leave must ease neutral. Wrapper, matte, holo, inspection and result paths, button non-hover, modal visibility, pause/error/Binder/focus gates, and no hover revision/reveal/save/cue changes are implemented. The PNG reader verifies all chunks' CRCs, exact dimensions, IEND, complete decompressed scanline rows and legal filters. These native assertions are **implemented but unexecuted**, not claimed evidence of visible geometry, masked light, physical mouse comfort or human acceptance.

The services/input driver is an owned adaptation of the existing real materials regression harness. It retains the actual 11-card hold, every result-inspection/Back hitbox, physical edit, failed acknowledgement/retry, backup, readonly preview, cancel, new-profile confirmation, process restart and unchanged order/flags assertions. A separate actual native Skip-path check is still required after the dependency gate; the inherited services/input suites do not establish that coverage. Existing tools/probes were not edited. Native material suites retain the actual 121-binding/107-texture full-quality oracle, not synthetic art. Unit tests independently decoded the actual manifest's 121 image bindings.

## Executed evidence (separate layers)

| Evidence | Actual result |
|---|---|
| `tilt-build-policy/01-policy-red` → `02-policy-green` | missing policy → **18/18** bundled-Mono checks, warnings-as-errors; includes both main-path slash spellings, exact/case-folded stage, foreign sibling/parent refusal and canary preservation |
| `tilt-build-compile/05-probe-optin-red` → `06-probe-optin-green` | missing probe → **12/12** opt-in checks |
| `08-request-red` → `09-request-green` | missing bounded parser → **19/19** checks |
| `12-hover-snapshot-red` → `13-hover-snapshot-green` | missing actual snapshot coroutine → **21/21** managed opt-in/protocol checks; runtime and owned builder compile against real installed Unity references with warnings-as-errors |
| `tilt-build/03` → `04`, `16` → `17`, `18` → `19` | missing driver, invalid-folder-meta acceptance and missing bootstrap restriction each observed RED, then corrected |
| `tilt-build/10` → `11`, `14` → `15` | missing owned player driver and unrecognized dedicated hover mode each observed RED, then corrected |
| `tilt-build/23-final-independent-tests.log` | **5** Python build-tool tests, **6** Python player-tool tests, **1** targeted entrypoint guard test pass; not native player assertions |
| `tilt-build/20-native-scene` | actual Unity process exit **1**, CS0246 dependency failure retained |
| `tilt-build/22-dependent-build-gate` | actual failed admission, no native build invoked |

All earlier failures remain failures under their original labels. The full BuildEntrypointTests suite still includes the intentionally unresolved real source-scene closure test; the targeted one-test guard pass does not relabel it.

## Preservation

Before/after native-attempt manifests match for **all six old players / 815 immutable members**: Phase2Windows (135), Phase3FinalCardWindows (136), Phase3MaterialsWindows (136), Phase3TableFlowWindows (136), Phase3Windows (136), and Phase4Windows (136). Saves/Logs are pruned case-insensitively before stat, descent or hashing. No accepted save bytes were read or copied. The native attempt records `olderPlayersUnchanged:true`; current gate pins independently reconcile the maps.

Original manifest0 remains `f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8`; supplied materials manifest remains `7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be`. Main Editor, old builds/settings/scenes/builders/probes/tools, persistence/domain/catalog/RNG/art assets, and Git were not modified by this worker. Controlled stages/TMP/evidence/fixture saves stay under the approved project root.

## Commands after the parent resolves the dependency

Use fresh labels. Do not retry/wait on a still-changing worker or weaken closure to obtain a player. First fix the surface worker's production Editor compiler dependency and adopt the validated native source sidecars. Then:

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python -B tools/run_tilt_build_policy.py --label fresh-policy
python -B tools/run_tilt_build_compile.py --label fresh-compile
python -B TestsTilt/BuildToolTests.py
python -B TestsTilt/PlayerVerifierTests.py
python -B tools/run_tilt_build.py --label fresh-scene --prepare-scene --host-ready --restage
python -B TestsTilt/BuildEntrypointTests.py
```

Freeze current source and metadata before the separate final candidate build. Only after successful admitted compiler/candidate closure:

```bash
python -B tools/run_tilt_build.py --label fresh-build --build --host-ready --restage
python -B tools/verify_tilt_player.py --label fresh-demo --build-label fresh-build --mode demo
python -B tools/verify_tilt_player.py --label fresh-services --build-label fresh-build --mode services
python -B tools/verify_tilt_player.py --label fresh-input --build-label fresh-build --mode input
python -B tools/verify_tilt_player.py --label fresh-hover-720 --build-label fresh-build --mode hover --size 1280x720
python -B tools/verify_tilt_player.py --label fresh-hover-900 --build-label fresh-build --mode hover --size 1600x900
python -B tools/verify_tilt_player.py --label fresh-hover-wide --build-label fresh-build --mode hover --size 2560x1080
```

These final native commands are prerequisites and unexecuted recipes, not promised pass results. Independently review source/candidate pins, actual GPU mask/perspective proof, player snapshots/PNGs and visual feedback afterward. Preserve the user's unrelated PASS results. No phase closure or human acceptance is inferred.

## Reusable boundary lessons

- Canonicalize both sides of Windows exact-stage admission before OrdinalIgnoreCase comparison; normalized-path versus literal comparisons do not protect the interactive project.
- Treat full production Editor compilation as a real dependency gate: a helper worker's copied fixture may pass while its production Editor check cannot compile without that fixture.
- Separate metadata discovery/owned scene generation from final source freeze/build admission; record actual native sidecars but never admit arbitrary non-folder compiler extras.
- Use a dedicated hover mode rather than racing legacy fixture coroutines that can generate, save or quit. Pointer observations and captures are diagnostic evidence, never command authority.
