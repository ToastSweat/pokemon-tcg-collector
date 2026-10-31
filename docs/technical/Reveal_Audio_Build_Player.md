# Assignable reveal audio — isolated build and native player evidence

## Delivered candidate

- Player: `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/Builds/Phase3RevealAudioWindows/VintageCardCollector.exe` (keep the complete directory together).
- Source scene: `Assets/VintageCollector/Scenes/BootstrapRevealAudio.unity`, natively cloned from the accepted tilt scene, with only the additional co-located `RevealAudioHost`/`RevealAudioSmokeProbe` components and the frozen `Audio/DefaultRevealAudioProfile.asset` reference.
- Owned stage: `.artifacts/reveal-audio-build-project`; main-project Editor operations are refused before mutation.
- Latest corrected build **native-build-fixed-14**: real Unity6000.2.14f1 Windows64 Mono/Direct3D11, Succeeded, **0errors/0warnings**, **480source inputs/484compiler inputs**, **137immutable player members**. The four compiler extras are independently validated actual native-generated folder metas, not extra scripts/assets.
- Full player manifest, source/compiler/report handles and SHA-256 pins: `../verification/Reveal_Audio_Player_Manifest.json`. Actual full build evidence: `.artifacts/reveal-audio-build/native-build-fixed-14/`; final reconciliation and full immutable preservation maps: `.artifacts/reveal-audio-fixes/final-readback.json` and `preservation-readback-maps.json`. Historical `native-build-07` reports remain unchanged; its original manifest is retained at `.artifacts/reveal-audio-fixes/original-player-manifest-native-build-07.json`.

## Final independent approval

The runtime/audio review and corrected build rereview both passed with **no security concerns or logic errors**. The rereview independently checked all81frozen index/live members, the two production corrections, fresh negative preservation/order regressions, exact480/484/137source/compiler/player closure, all five corrected native suites and preservation maps. The earlier rejection remains unchanged historical evidence. [Runtime/audio review](../verification/Reveal_Audio_Runtime_Independent_Review.json) · [Corrected build approval](../verification/Reveal_Audio_Build_Independent_Review.json) · [Final execution status](../verification/Reveal_Audio_Execution.json).

Nonblocking shared-fixture isolation, common build-admission reuse and future whitespace-tolerant meta-test improvements are recorded in the review; they were not expanded into new implementation scope. Human listening remains pending.

## Actual native execution

Latest corrected candidate (all five verifier exits **0**, with new isolated saves/report labels):

| Run | External assertions | In-player assertions | Actual images |
|---|---:|---|---:|
| `reveal-audio-player/fixed-audio-15` |17|279 audio +40 reload|9|
| `phase4-player/fixed-services-16` |36|62 services +11 reopen;400+400 material|11 gameplay +6 material|
| `phase4-player/fixed-input-17` |590|163 input +11 reopen;400+400 material|161 gameplay +6 material|
| `reveal-audio-legacy/fixed-restart-18` |30|12 prepare +23 resume +8 error|12|
| `reveal-audio-legacy/fixed-crash-19` |23|1 after-kill; before-kill is actual owned-process observation|4|

The current readback decoded **209 PNG paths / 148 distinct contents**, with complete PNG CRC/IEND/row checks. Input's observed extra framebuffer capture (`native-159`) accounts for its one additional native assertion/image and one external PNG check versus the original run; these are measured counts, not an expanded feature. All 14 imported clips played in 36 nonzero actual source/listener observations. `native-suites-fixed-14.json` records all five real verifier exits and report handles. Fresh holo/reveal-8, save-error/Retry and eleven-card results images were visually inspected. No image or automation result establishes physical listening.

### Historical original candidate runs (retained unchanged)

| Run | External assertions | In-player assertions | Actual images |
|---|---:|---|---:|
| `reveal-audio-player/native-audio-08` |17|279audio +40reload|9|
| `phase4-player/reveal-services-09` |36|62services +11reopen;400+400material|11gameplay +6material|
| `phase4-player/reveal-input-10` |589|162input +11reopen;400+400material|160gameplay +6material|
| `reveal-audio-legacy/reveal-restart-11` |30|12prepare +23resume +8error|12|
| `reveal-audio-legacy/reveal-crash-12` |23|1after-kill; before-kill is a real driver observation, not a fabricated report|4|

Each run's `verification.json` is below its named `.artifacts/` directory. All processes actually exited as expected, all report counts reconcile with enumerated checks, and all required checks passed. These are separate evidence layers, not a combined unique-test total. The historical original audit decoded **208PNG paths/150distinct contents**, validating signature/chunk CRCs/IEND/decompressed rows. Holo reveal, save-error and results screenshots were visually inspected: supplied art, reveal8, Retry, eleven-card results and existing controls remain visible. No physical-hearing claim follows from images.

### Audio evidence

- Native Editor and actual player independently loaded **14distinct imported WAV clips**, their real samples and exact production profile references. Native import pins include GUID, source asset path, sample count, frequency, peak and float32 sample SHA-256.
- Production renderer, real generator/controller/save acknowledgements, actual child `AudioSource.isPlaying`, expected/actual clip identity, source `GetOutputData` and listener mix `GetOutputData` were observed for **36playbacks**. Latest corrected-run source peaks ranged from **0.0036000562831759453** to **0.16777411103248596** (the original run minimum was **0.0018733444157987833**); every counted playback had nonzero source and listener signal.
- Actual acknowledged rare outcomes came from frozen-generator seeds1(non-Pokémon),2(holo),3(Pokémon). No catalog, generator, order or save schema was changed. Metadata was consulted only after each reveal acknowledgement.
- All eleven reveal positions were exercised. All fourteen assigned clips actually played, including `Pull_08` through a temporary **cloned** profile with absent rare overrides. The imported profile was never edited by the probe.
- Silent startup, real failed-save silence, once-only successful Retry, duplicate acknowledgements, muted reveal consumption/no-unmute replay, pause/Resume, Binder/back, inspect/back, explicit results, one generic Skip rather than catch-up fanfares, and independent-process silent reload all passed.
- Native audio session generated5packs/55cards; reload generated0 and preserved exact ordered history, flags, revisions and awards. Full independent gameplay regression additionally exercised real physical edits, failed acknowledgements/retries, backup/readonly preview/cancel/new-profile confirmation, process restart, partial-reveal restart, and actual hard-kill recovery through owned native clicks.
- Human hearing, musical preference and physical-device acceptance remain pending.

## Safety and exact-source gates

`RevealAudioBuildPolicy` canonicalizes both operands and admits only the exact new owned stage, never the main project or old tilt stage. Candidate Saves refusal is case-insensitive and read-only, before any directory/scene/player mutation. Python admission additionally pins both accepted runtime manifests. No accepted output is reused.

The runner copies exact Assets/Packages/ProjectSettings inputs, refuses every undeclared staged `.cs`, `.asmdef`, `.asmref`, `.dll` and `.rsp` across **all three roots**, including embedded/local package directories and code without an Editor path segment, **before any stage marker write or tree removal**, without deleting or executing stale inputs, validates source metadata, verifies complete independently enumerated compiler inputs (not a delivered subset), rehashes source after native execution, and freezes every immutable candidate member. Compiler traversal deliberately has **no Saves/Logs name exclusions inside source trees**: `Assets/Logs/Editor/*.cs` can compile. Only player-preservation traversal prunes Saves/Logs before attributes, descent or reading bytes.

Scene preparation is a separate admission from the final build. Only this worker's four actual native-generated CS metas and the new native scene/meta were adopted back to source; existing metas/scene references were not rewritten. Native-scene-05 retained a genuine failed scene-closure result (Unity exit0 but serialized host profile null). Reacquiring the profile after opening the target scene, explicit scene dirtying and non-null native readback corrected the owned builder; native-scene-06 and final build07 passed. The failed attempt remains failed.

Every Win32 mouse/key/character/activation message uses the imported proven `OwnedWindowInput`: still-live launched process, current HWND owner PID and second liveness check before each individual post. No foreground-input/global-cursor APIs are used. Existing builders/probes/verifiers were imported read-only, not modified or run against their accepted outputs. Native audio automation and the unchanged legacy runner activate only under exact bounded new fixture markers plus the existing validated smoke paths. Ordinary startup remains inert.

## TDD and executed setup

Owned explicit suites: `TestsRevealAudio/BuildAdmissionTests.py` (5), `BuildEntrypointTests.py` (3), `BuildPolicyTests.cs` (13), `PlayerOptInTests.cs` (12), `PlayerVerifierTests.py` (4). Real missing-feature RED logs and later GREEN output are retained under `.artifacts/reveal-audio-tests/` and `.artifacts/reveal-audio-compile/`. Runtime and owned Editor builder compiled against the installed real Unity assemblies with warnings-as-errors; these managed results are not substituted for the native playback evidence above. A reused policy fixture left by a RED run correctly blocked a later attempt; that unsuccessful output is retained, and fresh per-run policy fixtures fixed only the harness setup.

Owned files: three `Editor/RevealAudio*.cs`, `Runtime/RevealAudioSmokeProbe.cs`, the new scene and their native metas; the five explicit tests above; `tools/run_reveal_audio_build.py`, `run_reveal_audio_compile.py`, `verify_reveal_audio_native.py`, `verify_reveal_audio_player.py`, `verify_reveal_audio_legacy.py`; this handoff and the new manifest. Audio implementation/assets and parent renderer remain separately owned.

## BUILD-01 / PLAYER-01 independent-review corrections

The independent build review remains **rejected historical evidence**, not relabelled PASS: `.artifacts/reveal-audio-independent-build/review.json`. Its original native runs had passed; the review found two safety/determinism gaps those successful executions could not disprove.

- **BUILD-01:** only `tools/run_reveal_audio_build.py` changed. `stage_exact` now enumerates `Assets`, `Packages`, and `ProjectSettings` through the complete compiler manifest before writing the stage marker/removing any tree. It rejects undeclared code-capable suffixes regardless of Editor folder names, without Saves/Logs pruning. Native-generated folder metas remain admissible and the final native build independently validated its four actual generated metas.
- **PLAYER-01:** only `Assets/VintageCollector/Runtime/RevealAudioSmokeProbe.cs` changed: execution order **-180**, plus its dependency comment. The original synchronous `Phase4CollectionHost(-200)` and its source/order are untouched; the audio probe now validates and fences before **Tilt(-175)**, **Materials(-150)**, and Phase4's default order. `-175` would collide with Tilt and was deliberately not used. Admission, marker/path policy, runtime behavior, assets and normal-startup opt-in rules were not changed.
- **Retained exact regressions:** `TestsRevealAudio/ReviewFixRegressionTests.py` adds two unittest build cases (17 negative subcases plus generated-folder-meta admission) and one exact-source player case (seven compiled managed scheduling/admission/fencing assertions). Each negative subcase snapshots every byte including the marker, package.json and previous staged declaration. The actual package Editor fixture and no-Editor-folder fixture remain on disk under the GREEN artifact root.
- **Observed RED:** `.artifacts/reveal-audio-fixes/build01-red-14/results.json` records **17 failures**, exit **1**, including actual deletion by the old implementation; its pre-call bytes remain under `before-bytes-*`. `player01-red-14/results.json` records the compiled exact-prefix ordering failure, compiler exit **0** / harness exit **1**, with source order **-200/-200**. Neither rejected report was overwritten.
- **Observed GREEN:** `build01-final-green-14/results.json` passes both build tests and proves all 17 refused fixtures byte-identical; `player01-final-green-14/results.json` passes the exact-source prefix plus real admission method with explicit managed component/scheduling doubles. The host's actual synchronous initialization chain is source-checked; ordinary startup neither automates nor fences other probes. These are not labelled native playback tests.
- **Full managed recheck:** `.artifacts/reveal-audio-fixes/managed-final-14/results.json` records seven commands, all exit **0**: new regressions, existing Python 5/3/4 tests, and real policy compilation/execution (13 checks). `.artifacts/reveal-audio-compile/fixed-compile-14/results.json` records four exit-0 real-reference runtime/builder/opt-in compile/run steps and 12 opt-in checks, all with warnings-as-errors. The actual native rebuild and all five suites above followed the production edits.
- **Rereview pins:** `.artifacts/reveal-audio-fixes/review-pin-delta.json` compares all original **75** live/index pins. Exactly **two production files** differ; the new regression test and this handoff/manifest are the fixer's test/documentation changes. Concurrent parent edits to Profile/Renderer documentation are recorded separately, not attributed to this fixer. The original Git index and frozen diff/pins remain unchanged. The actual source compiler manifest differs from build07 only in the new audio probe; all other 479 source inputs match.

An initial readback audit exit **1** detected concurrent parent Profile/Renderer documentation updates outside the fixer's original documentation allowlist. This was retained in `audit-attempt01.json`; after explicitly separating those documentation-only deltas, `audit-attempt02.log` passed. No production acceptance gate was weakened.

## Preservation

Final reconciliation against the parent's pre-work baseline proves unchanged:

- **7accepted players/951immutable members**, including accepted tilt/materials/Phase4 players;
- **694shared files**, including domain/data/persistence/collections, supplied manifests/art, original Pokemon-Base-Set files and all302user-owned Pokemon Cries files;
- all22baseline Runtime files, excluding this new probe by design.

No live Saves/Logs were traversed. User-owned `ProjectSettings/PackageManagerSettings.asset` was preserved; exact source pins remained equal after all native executions. No downloads/uploads, controller/data/save-protocol edits, old-tool edits or commits were performed by this worker.

## Reproduce with fresh labels

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python -B tools/run_reveal_audio_compile.py --label fresh-compile
python -B TestsRevealAudio/BuildAdmissionTests.py
python -B TestsRevealAudio/BuildEntrypointTests.py
python -B TestsRevealAudio/PlayerVerifierTests.py
python -B TestsRevealAudio/ReviewFixRegressionTests.py --suite build --label fresh-build-guard
python -B TestsRevealAudio/ReviewFixRegressionTests.py --suite player --label fresh-player-order
# Only after all owners freeze source; populated candidate Saves still refuse rebuilding.
python -B tools/run_reveal_audio_build.py --label fresh-build --build --host-ready --restage
python -B tools/verify_reveal_audio_native.py --label fresh-audio --build-label fresh-build
python -B tools/verify_reveal_audio_player.py --label fresh-services --build-label fresh-build --mode services
python -B tools/verify_reveal_audio_player.py --label fresh-input --build-label fresh-build --mode input
python -B tools/verify_reveal_audio_legacy.py --label fresh-restart --build-label fresh-build --mode smoke
python -B tools/verify_reveal_audio_legacy.py --label fresh-crash --build-label fresh-build --mode crash
```

The existing source scene already passed closure. Regeneration, when explicitly needed, uses a separate `--prepare-scene --host-ready --restage` invocation and must precede a newly pinned final build. Any source/runner/player drift must fail admission rather than reusing the current PASS labels.
