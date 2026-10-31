# Separate supplied-materials candidate: builder and player verification

## Parent current result — verified and independently reviewed; human visual feedback pending

Latest exact candidate is **parent-materials-stage-safe-build-35**, not the earlierbuild23. Independent review of23 found one high-severity Editor-admission bug: comparing a canonical Windows backslash path against the forward-slash ApprovedRoot literal could admit the main project through the C# menu entrypoints. The Python driver had correctly used the isolated stage; no main Editor scene/settings mutation occurred. Parent observed real missing-stage-guard RED, added `MaterialsBuildPolicy.RequireOwnedStage` and routed the common `Project()` through it before every operation, then observed14/14policy checksGREEN including both slash forms of the main path and exact-stage acceptance. Both unsafe literal comparisons were removed.

Fresh build35 is admitted/Succeeded0errors0warnings with exact414source/418compiler/136candidate closure. All three fresh current-driver modes pass: demo36(23external/426material assertions), services37(36external/73gameplay/800material), input38(591external/175gameplay/800material). Parent rechecked all200PNG paths/143distinct contents, reports/runner pins/source closure and unchanged prior-player maps. These evidence layers are not combined unique tests. Targeted independent guard re-review passed with no security/logic blockers (fresh14policy +54canonical-path controls and495readback checks); parent rechecked414source/418compiler/136candidate pins afterward. No human material acceptance is claimed. Earlier build23/player30-32 remain truthful historical snapshots, not current source pins.

## Prior parent candidate snapshot (before the stage-only guard correction)

`Builds/Phase3MaterialsWindows/VintageCardCollector.exe` is now **parent-materials-coordinated-build-23**: native Editor exit0, Succeeded/errors0/warnings0, releaseWindows64/Mono/Direct3D11. Source414inputs match exact staging and418compiler inputs, with only4validated generated folder-meta exceptions; candidate136members remain pinned. Parent adopted113exactnative-generated PNG/JSON metas and recorded source GUID/hash/size in `../verification/Base_Set_Materials_Unity_Meta_Pins.json`; no originals/derivedpixels/oldmetas/manifest0 changed. Source closure was not weakened.

Fresh current-driver suites all exit0:

| Final label | External assertions | Gameplay native | Material native | Processes | Gameplay PNG / material image paths |
|---|---:|---:|---:|---:|---:|
| parent-materials-demo-final-30 |23|0|426|1|0 /15|
| parent-materials-services-final-31 |36|73|800|2|11 /6|
| parent-materials-input-final-32 |591|175|800|2|162 /6|

Layers are not combined unique tests. Parent independently checked all200PNG paths (133distinct contents), report counts, runner pins, source/compiler closure, candidate bytes and unchanged old-player maps including acceptedPhase4, excluding Saves/Logs before traversal. Full records: `../verification/Materials_Player_Execution.json` and `Materials_Player_Manifest.json`. Parent loaded actual generated-pack holo inspection, eleven-card results and wrapper-front framebuffers with vision: printed content stays unobscured, labels outside art, correct fit. Actualasset121bindings/107unique textures/no fallback and fullRGBA readback are asserted in each process; actualGPU masks are tested, not inferred from screenshots.

Retained true RED: services21 completed398material checks before the reused regression probe quit the reopen process. New MaterialsSmokeProbe now executes at-150 after collection host-200 and before legacy probeStart; only an explicit validated material fixture gates that legacy probe until native material verification is complete. Normal/unrelated launches are untouched; old Phase4SmokeProbe source remains unchanged. Build23/services24/input25 passed after this fix. Demo26 then exposed a driver-only assumption that the deliberately unstarted legacy probe must emit a report. Parent observed a targeted pure RED, separated standalone-demo material reports from mandatory services/input gameplay reports, and reran all3modes with the frozen driver above. No fake gameplay report was synthesized. Prior failed builds/reports are retained.

Authoring is being taught to preserve only explicitly pinned native sidecar metas, rather than treating them as foreign files; that tooling compatibility does not change this built player's assets or logic. Separate core/accounting, renderer and final candidate independent reviews remain the release gates. Human visual acceptance has not been reported. Do not begin Phase5 or overwrite acceptedPhase4 saves.

---

## Historical dependent gate (superseded by current result above)

The actual separate candidate was generated and compiled by Unity 6000.2.14f1. Native build label **08-native-build** returned **Succeeded, errors 0, warnings 0**, release Mono / Direct3D11, at:

`F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/Builds/Phase3MaterialsWindows/VintageCardCollector.exe`

**It is not admitted or player-verified.** The strict final compiler-input gate rejected **113 generated non-folder asset metas** absent from source. Four generated folder metas are the only allowable exceptions. The images/JSON themselves were copied byte-exactly, including all private full-quality prepared PNGs; ignoring private art in Git does not exclude it from staging.

Parent / asset-owner decision: adopt the exact native-imported asset metas listed in `.artifacts/materials-build/08-native-build/source-meta-adoption-required.json` into their corresponding source paths, or explicitly revise the closure contract. This worker did not write the asset owner's files, silently broaden the exception, edit old tools, or launch an unadmitted player.

A subsequent real opt-in policy regression also found and fixed the forward-slash-root/canonical-Windows-prefix mismatch in the new probe. The existing candidate therefore differs from current source in **MaterialsSmokeProbe.cs** and must be rebuilt even after the metadata gate is resolved. The unsuccessful build and all historical reports remain unmodified.

The current native command was retried as **18-current-dependent-gate**: it refuses the missing source asset metas **before restaging or invoking Unity**. Renderer/host/media code is available and compiled; the gate is metadata provenance, not a missing renderer implementation.

## New production API / scene

- `VintageCollector.EditorTools.MaterialsBuild.CreateBootstrap()` creates only `Assets/VintageCollector/Scenes/BootstrapMaterials.unity`, in the exact isolated stage.
- `VintageCollector.EditorTools.MaterialsBuild.BuildWindows()` builds only the separate candidate above.
- `MaterialsBuildPolicy.ValidateProject`, `Resolve`, `ImmutableManifest`: read-only path/build admission and preservation hashing.
- `VintageCollector.Verification.MaterialsSmokeProbe.ValidateOptIn(GamePaths)` returns null for normal play / unrelated smoke runs. It accepts only an additional exact bounded fixture in the existing `.artifacts/phase4-player/<label>/{services,input,reopen}` sandbox, with matching `<label>/session` and existing `--vcc-smoke=input` flags. It introduces no path/URI/root override or gameplay input.

The materialized scene contains the actual **OpeningGame, TabletopPresentation, Phase4CollectionHost, BinderPresentation, Phase3MediaHost, unchanged Phase4SmokeProbe, MaterialsSmokeProbe**, and Camera/AudioListener. Original catalog/model references and all script/data GUIDs resolve uniquely. Only this new scene serializes:

```csharp
manifestRelativePath = "Assets/Supplied/BaseSetMaterials/Manifest.json";
fullBaseSetBudget = true;
```

Existing scenes/settings/builders/probes/tools were not edited. The builder itself refuses the interactive source project; exact stage is `.artifacts/materials-build-project`. No main Editor, foreground input, accepted save, Git, install or network operation is used.

## Verification implementation / unexecuted native boundary

`tools/verify_materials_player.py` owns a separate driver derived coherently from the unchanged Phase4 verifier. Services/input retain its actual physical-edit, retry, backup, restore, restart, hidden-prefix, card-11 hold and explicit-results checks. It still uses Phase4SmokeProbe's strict existing sandbox; no legacy tool patch or monkey-patched API response is required. Native input is background Win32 messages to the launched process's own HWND, with actual current-render controls and observable postconditions. Player binaries, source pins and older players are checked before and after.

Before launch the driver independently hashes/decodes actual approved PNGs with Pillow, preserving original dimensions and full bottom-up RGBA SHA-256. It writes a bounded actual-image oracle, **synthetic:false**, never swatches or resized substitutes. The new probe's implemented native assertions check:

- all **121 bindings / 107 native textures / 51,479,000 unique pixels**, trusted full budget, no warning/fallback/test-manifest override;
- every actual Texture2D dimension and complete GetPixels32 RGBA hash, mask linearity/readability/no mipmaps, fifteen-to-one shared holo-mask identity;
- unchanged default 16 MiPixel refusal and two reload/idempotent deferred-dispose cycles;
- actual resource shader support and GPU readback of supplied holo/front/back masks, animated inside versus unchanged black-mask outside;
- original generic foley resources and unchanged manifest0;
- explicit isolated `demo` mode: real seed-4 pack, wrapper front and existing front-null/back fallback, reveal zero/card back, partial reveal, real failed-save Retry, pause/focus/error clock freeze, no resume/unmute auto-replay, final card-11 hold, holo/nonholo inspection, Binder Back and explicit Results; captures use actual framebuffers and bounded PNG completion.

The wrapper-back provider exists **only in explicit isolated demo mode**; it uses the renderer's existing fallback, not a new button, gesture or RNG call. The normal scene adds no wrapper-orientation gameplay.

**None of those player/GPU/capture assertions are claimed executed here:** the admitted-build prerequisite prevents launch. Native runtime screenshots/vision, background-input, restart and human hearing/visual feedback remain pending. The renderer worker's separate evidence is not substituted for candidate evidence.

## Executed evidence

| Evidence | Actual result |
|---|---|
| `materials-build-policy/01-policy-red` -> `02-policy-green` | missing policy assertion -> **10 pass**, real bundled Mono, warnings-as-errors |
| `03-tool-red.log` -> `04-tool-green.log` | missing separate driver -> compiler-closure fixture pass |
| `05-scene-red` -> `07-native-scene-green` | absent scene -> actual native generation / unique source GUID closure |
| `06-native-probe-red` | actual native assertion: missing MaterialsSmokeProbe, before scene mutation |
| `08-native-build` | native build succeeded 0/0, **strict compiler closure failed**, not admitted |
| `09-preservation-red.log` -> `10-preservation-green.log` | caught inherited Phase4 exclusion; corrected preservation includes Phase4 and excludes only new candidate |
| `12-compile-optin` -> `13-compile-optin-green` | caught real canonical-Windows-prefix defect; final **12 opt-in tests pass**; runtime + builder compile against actual installed Unity references with warnings-as-errors |
| `14-asset-meta-gate-red.log` -> `15-asset-meta-gate-green.log` | source-meta admission absent -> **3 Python tool tests pass** |
| `16-player-tools-green.log` | **3 Python verifier tests pass**, actual 121-image oracle, real PNG CRC/IEND/dimensions, unadmitted-build refusal without launching/creating a session |
| `17-final-policy` | **10 policy tests pass** |
| `18-current-dependent-gate` | correct early refusal, no restage/native execution |

Evidence is under `.artifacts/materials-build*`; all fixtures/TMP/logs stay below approved root. Python and Mono test totals are separate, not additional native player assertions.

`.artifacts/materials-build/dependent-gate-pins.json` retains current source (301 files), actual stage/compiler (418 files), full separate candidate immutable manifest (136 files), all older-player manifests, precise current source drift, and gate status. The accepted Phase4's **136 immutable files match `docs/verification/Phase_4_Player_Manifest.json` exactly**. Saves/Logs are pruned case-insensitively before stat/descent/hash. The earlier `parent-handoff-audit-04` refers to an older historical build; the frozen verification document is the applicable accepted baseline.

Original empty manifest0 remains SHA-256 `f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8`. Actual materials manifest remains `7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be`.

## Commands

From MSYS Bash, with fresh labels and the main Editor left untouched:

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python -B tools/run_materials_build_policy.py --label fresh-policy
python -B TestsMaterials/BuildToolTests.py
python -B tools/run_materials_build_compile.py --label fresh-compile-optin
python -B TestsMaterials/PlayerVerifierTests.py
```

After the parent resolves the 113 source-meta gate (do not loosen folder-only exceptions silently):

```bash
python -B tools/run_materials_build.py --label fresh-materials-build --build --host-ready --restage
python -B tools/verify_materials_player.py --label fresh-materials-demo --build-label fresh-materials-build --mode demo
python -B tools/verify_materials_player.py --label fresh-materials-services --build-label fresh-materials-build --mode services
python -B tools/verify_materials_player.py --label fresh-materials-input --build-label fresh-materials-build --mode input
```

Scene recreation, if deliberately required, is a separate native operation:

```bash
python -B tools/run_materials_build.py --label fresh-materials-scene --prepare-scene --host-ready --restage
```

Keep all candidate dependency files together. An EXE's existence or Unity BuildReport success does not satisfy provenance, current-source or player gates. No phase closure, Git acceptance or human approval is asserted. This local document records the reusable workflow because profile/skill writes are outside scope.
