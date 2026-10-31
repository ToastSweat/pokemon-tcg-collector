# Phase 3A parent verification — generic media-ready candidate

Status: corrected execution and both cycle-two independent reviews passed. The earlier rejected review remains recorded; its three blockers are resolved. Phase 3A is ready for the generic human feedback gate, not full real-material Phase 3 acceptance or Phase 4 authorization.

## Candidate and preserved data

- Player: `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/Builds/Phase3Windows/VintageCardCollector.exe`.
- Keep its whole directory together. This is a separate test candidate with its **own** `Builds/Phase3Windows/Saves`, initially unused by automation. It does not merge/copy the accepted Phase 2 collection. Continue the accepted player to use that collection.
- Source scene: `Assets/VintageCollector/Scenes/BootstrapPhase3.unity`. Editor play still uses the existing Editor-only `Saves/Phase2` policy; that is separate from both standalone folders.
- All 135 immutable accepted Phase 2 files matched their original manifest after testing. Accepted build/save directories were not replaced. Domain/Data/Persistence/OpeningGame/GamePaths/UnityStateSerializer matched checkpoint d649e46 byte-for-byte.
- The real supplied manifest remains an empty assets array. No branded art/audio was downloaded, invented or represented as supplied. Positive media tests use original synthetic patterns/PCM in explicit isolated `.artifacts` overrides only.

## Reproducible staged build

The original interactive Unity Editor was open. It was not killed, closed, driven or asked to discard unsaved work. A byte-verified source stage under `.artifacts/phase3-build-project` was used instead:

1. `python -B tools/stage_phase3_build.py` copies only owned project assets, production manifest, Packages and tracked ProjectSettings; it excludes the pre-existing untracked PackageManagerSettings.asset and all live saves. Every copied input's source/staged hash is compared.
2. Run Unity 6000.2.14f1 batch against that stage, `-executeMethod VintageCollector.EditorTools.Phase3BuildChecks.Run`, with log/TEMP under this root.
3. Run `-executeMethod VintageCollector.EditorTools.Phase3Build.BuildWindows` against the stage. Output is explicitly the separate approved-root `Builds/Phase3Windows` directory.
4. Initial copyback missed two source MonoScript metadata files; first review correctly rejected that source-preview claim. The exact known stage Host/Probe metadata are now restored in source, with real RED/GREEN coverage and all five source scene script references resolving uniquely. The source scene itself was not changed by that correction. Original Bootstrap.unity and the interactive build settings remain unchanged.

Actual BuildReport: **Succeeded, zero errors, zero warnings**, Windows x64, Mono, D3D11, non-development, default 1280×720 resizable. The Resources-retained foil shader worked in the real release player. Build policy refuses to rebuild over populated candidate Saves; it never clears them automatically. Rebuilding regenerates the controlled source scene; preserve custom scene changes separately.

## Actual parent evidence

| Check | Result | Report |
| --- | --- | --- |
| Pure safe media/PCM/parser/path/cue data | 13 checks passed | `.artifacts/phase3-media/parent-pure/results.json` |
| Pure deterministic motion/unchanged layout | 39 checks passed | `.artifacts/phase3-presentation/parent-motion.json` |
| Candidate admission/read-only/save preservation | 2 checks passed with retained RED/GREEN | `.artifacts/phase3-build-policy/parent-final.json` |
| Exact-source real host/controller/media | 87 native + 18 Play Mode assertions passed; report counts reconciled | `.artifacts/phase3-host/final-native-integration/results.json`, `final-native-play/results.json` |
| Separate candidate scene/wiring/original scene preservation | 4 checks passed | `.artifacts/phase3-build-project/.artifacts/phase3-build/scene-checks.json` |
| Accepted controller/native state regression | 13 groups / 297 assertions passed on parent stage rerun | `.artifacts/phase3-build-project/.artifacts/phase2-runtime/phase3-regression.json` |
| Accepted persistence regression | 109 checks passed on parent rerun | `.artifacts/phase2-persistence/phase3-regression/results.json` |
| Accepted generator/distribution regression | 21 tests + 183 distribution checks over100000 packs passed | `.artifacts/domain/phase3-regression-*.json` |
| Core-connected production player | Five suites, **111 driver checks /60 validated captures** passed | `.artifacts/phase3-player/{integrated-01,layouts-01,mouse-01,hardkill-01,keyboard-01}/verification.json` |
| Real optional-loader/audio integration | **61 driver checks /34 validated captures** passed | `.artifacts/phase3-media-player/media-01/verification.json` |
| Actual supplied-test face/mask pixel containment | 3 checks passed;81947 effect pixels inside mask, zero escaping pixels/print-band changes | `.artifacts/phase3-media-player/media-01/pixel-verification.json` |

Counts include data boundaries, repeated cases, PNG CRC/dimensions and provenance checks; they are not distinct-feature or human acceptance totals. Isolated worker renderer/media fixture totals are retained in technical documents and not added to these production-driver totals. All14 actual graphical-player logs were scanned for exception/error/warning terms; none matched. Editor licensing-token diagnostics remain retained and nonblocking, not erased or called pristine.

## Production cases and audio evidence

The unchanged core smoke flow ran in the actual Phase 3 player: generate/save sealed contents, wrapper, ordered reveals/inspection, Skip/results, repeated-completion rejection, separate-process count-three resume, prepared-save failure/Retry, deliberately repeated seed duplicate flags, actual forced Unity termination and writer-lock release/recovery, background HWND mouse actions and Escape/Back/Pause/Resume. No foreground-cursor API was used.

Default empty-manifest audio was verified through real native state and PCM/DSP readback:

- Zero supplied bindings, no synthetic override, no startup cue, no warnings.
- Four cached finite/nonzero original generic clips. Source is 2D, nonlooping, playOnAwake false.
- Actual acknowledged Generate/Open/Next produced source playback. Pause stopped it; muted Next was consumed without sound/cue increment; Unmute/Resume did not replay; the next new acknowledged reveal produced exactly one cue.
- The final observed run had four playback requests, `everPlaying=true`, volume0.5 and native maximum DSP output peak0.0265501718968153. This is engine-output evidence, **not physical hearing or quality approval**.
- Supplied-test PCM16 open cue decoded at44100Hz/44100frames; default generic clips remain22050Hz. This proves actual replacement rather than an invented RIFF-shaped response.

Positive original PNG/WAV integration loaded five exact bindings (face/mask/back/wrapper/open cue). A bad face hash and orphan mask fell back independently while valid bindings and gameplay survived. A face without a mask remained valid, with the real-face foil disabled. Actual whole printed faces were unobscured; masked and maskless captures were compared: effect pixels stayed inside the aligned region with2-pixel sampling tolerance, and top/bottom print bands were byte-identical. Parent also visually loaded default reveal, valid-mask, maskless and bad-hash inspection captures.

## Reproduce graphical suites

Use fresh labels and run graphical suites sequentially:

```text
python -B tools/verify_phase3_player.py --mode smoke --label new-core
python -B tools/verify_phase3_player.py --mode layouts --label new-layouts
python -B tools/verify_phase3_player.py --mode input --label new-mouse
python -B tools/verify_phase3_player.py --mode crash --label new-crash
python -B tools/verify_phase3_player.py --mode keyboard --label new-keys
python -B tools/verify_phase3_media_player.py --label new-media
python -B tools/verify_phase3_capture_pixels.py new-media
```

Do not use batchmode/nographics for graphical players; IMGUI can be suppressed even with graphics initialized. These tools create isolated saves/artifacts, never live-save migrations. The compiled extra observers are explicit-smoke guarded and do not play sounds, generate cards or write reports on normal startup. Host/provider handles media only; current controller/save behavior is unchanged.

## Correction closure and retained intermediate attempts

First review was rejected and remains in Phase_3A_Review_Cycle_1.json. The separate fix worker's exact RED/GREEN changes and honest historical read-hashing disclosure are in ../technical/Phase_3_Review_Fixes.md. Host selection now validates the manifest leaf before its existence delegate; unsafe optional files fall back to production. Hash runners prune case-insensitive Saves before traversal/content access. Exact staged Host/Probe metadata are restored; all five source MonoScript references resolve uniquely. Parent reran both synthetic privacy/metadata regressions, then rebuilt the candidate.

The final current player evidence is under `.artifacts/phase3-player/{final-core,final-layouts,final-mouse,final-hardkill,final-keyboard}` and `.artifacts/phase3-media-player/final-media`, with the same complete passing driver/capture counts. The corrected manifest/summary linked below supersede the initial candidate for handoff, without overwriting its historical records.

Two intermediate rerun failures are retained rather than counted as passes: `corrected-media-01` timed out once waiting for the unmuted Next cue, while saved progress/volume were correct. No production sound fix was inferred without cause; an explicit smoke-only acknowledgement observer was added, four fresh full media suites and the final rebuilt suite then passed, with all final action/volume/focus/cue transitions recorded. `corrected-layouts-02` checked an asynchronous screenshot too early after a fixed four frames; the PNG subsequently existed with valid CRCs/dimensions. The smoke-only capture fence now waits for IEND under a bounded deadline, followed by full external CRC/dimension checks. Neither change alters normal gameplay, generation or awards. The accepted Phase 2 binary was not rebuilt.

## Remaining human/material gates

- User must listen to real speakers/headphones and judge comfort, cue volume/timbre/timing, mute/pause behavior, subtle motion, readability and performance. Native injected input/DSP samples do not establish human comfort/hearing.
- This is generic original foley and fallback art. Actual branded faces/back/wrapper/masks/recordings and provenance remain deferred. Their real-material gate is not complete.
- No full binder/physical edits/completion denominator, other sets, market or finance. Machamp target remains unanswered. Model A0.1.0 is still provisional, not factory truth.
- Source/build hash membership and independent review are separate gates. Candidate manifest is Phase_3A_Corrected_Player_Manifest.json; execution summary is Phase_3A_Corrected_Execution_Summary.json. Preserve accepted Phase2 and stop after the Phase3A human checkpoint, not Phase4.
