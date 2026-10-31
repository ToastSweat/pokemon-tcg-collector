# Supplied-material presentation / optional host

## Delivered boundary

This is working renderer/host code with **isolated native Unity evidence**, including actual private supplied textures. It is **not** the parent candidate build, a real generated candidate pack/Binder playtest, human visual/audio approval, or phase closure. Originals and supplied-asset preparation belong to the asset worker; media decoding/budget/deduplication belongs to the media worker.

Only production changes in this slice:

- `Runtime/Phase3MediaHost.cs`: two additive trusted serialized scene fields and forwarding to the five-argument media loader.
- `Presentation/TabletopPresentation.cs`: optional, role-aligned wrapper reflection and owned material cleanup.
- `Resources/Presentation/WrapperMetallic.shader` and its `.meta`: build-retained masked silver reflection.

No Domain/Data/Persistence/Collections/OpeningGame/GamePaths/builder/source-scene/project-settings edits, gameplay input, randomness, or automatic generation/save/playback were introduced. No accepted player or live-save bytes were read, copied, hashed, or changed. An initial bounded broad filename discovery returned legacy-build paths; subsequent discovery/content access was narrowed to source and owned artifacts. The main interactive Editor was not opened, controlled, or closed by this worker. No Git/index operations, installs, network calls, or uploads were performed.

## Parent integration API

```csharp
// Set ONLY on the parent's new candidate scene/host before initialization.
mediaHost.manifestRelativePath = "Assets/Supplied/BaseSetMaterials/Manifest.json";
mediaHost.fullBaseSetBudget = true;
```

Legacy defaults remain:

```csharp
[SerializeField] public string manifestRelativePath = "Assets/Supplied/AssetManifest.json";
[SerializeField] public bool fullBaseSetBudget = false;
```

The host now calls `PresentationMediaLibrary.Load(root, catalog, manifest, test, fullBaseSetBudget)`. The flag is trusted scene configuration, not a manifest/environment/process override. The media worker's default 16 MiPixel cap remains in force without explicit opt-in; the full-set opt-in uses its 64 MiPixel cap. A native host fixture with four unique large faces admits two by default and all four only with the flag.

Root validation, profile-aware `GamePaths` revalidation, initialization/error prerequisites, optional smoke override validation and lifetime ordering are unchanged. Only an existing validated smoke `media-manifest.json` enables test inputs; a configured `.artifacts` path does not. Unsafe/missing configured manifests remain optional failures with generic foley and usable gameplay. The host never requests `Sound`, creates an AudioSource, acknowledges an action, retries a save, or starts a pack. Renderer detach stops/clears a bound source before disposing host-owned clips/textures. External providers remain external.

**Do not change the old manifest.** It still has an empty assets array and SHA-256:

`f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8`

Actual prepared material manifest used by the final native tests:

`7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be`

The host binds all **121 declarations** without warnings through the real loader: 101 faces, 15 aligned holo-mask bindings, five global bindings. Exact source/staged supplied-file pins are retained per run. The media implementation compiled here has SHA-256 `1cfd7e620f6ff87886e3a3d1ceee5b9564eb78181fbfac9410d44833a4775c85`.

## Rendering / unchanged controls

- Existing full supplied face/back `ScaleToFit` paths remain intact. Supplied faces return before all generic name/rarity/collector-number/NEW-DUPLICATE painting. No new on-card warning, printing identity, crop, recolor, or edition label was added. The user's replaceable scan choice is preserved.
- Actual NEW and duplicate faces, a distinct actual nonholo face, and the full production inspection were compared with neutral texture-reference draws. **Zero HP/attack/copyright/face-overwrite pixels** were observed. Sidebar/inspection metadata remains outside the art; results retain the existing outside-art summaries/physical label mechanism. Binder UI was not redesigned.
- Existing card foil remains unchanged: only explicit holo finish plus a same-dimension illustration mask enables it; the supplied face's exact fitted rectangle drives mask coordinates. Nonholo faces never animate foil. `Phase3Foil.shader` is unchanged.
- Wrappers still prefer `WrapperFront`, then use `WrapperBack` only when front is absent. If the provider also implements optional `IWrapperMetallicMedia`, the renderer selects the corresponding front/back mask **for that same chosen role**, rejects missing/misaligned masks, and draws only in the exact fitted texture rectangle. It never borrows another role's mask or infers a full-face mask. Old `IPresentationMedia` is unchanged.
- `WrapperMetallic` uses red times alpha coverage, a broad low-opacity moving silver highlight, and the existing explicit presentation clock. There is no shader `_Time`, opaque repaint, rainbow wrapper treatment, alternate RNG, or new gesture/button. Actual per-channel wrapper changes stayed at or below 0.1176471 in the retained proof while printed logo/color/text remained visible.
- Solid black-mask pixels outside the explicitly classified one-texel bilinear transition footprint and letterboxing were **byte-identical** between baseline and effect/movement frames. A tall front and deliberately wide synthetic back test different aspect fits. Actual front/back textures and masks were also exercised. The back-only fixture uses production's existing front-null fallback, not a new gameplay orientation control.
- Generic wrapper/card/foil, tear, hitboxes, motion, card-11 hold, explicit results, inspection Back, Skip, collection gate, pause/focus/error clock freezing and consumed-once cue behavior are unchanged. Frozen effects retain their last phase rather than disappearing while paused, matching existing rules.
- A native deferred destruction check verifies the wrapper material is destroyed while externally owned textures survive. Mask/face lookup and rendering perform no file IO or playback.

## Reproduce

Run from the repository root under MSYS Bash with **fresh labels**; native modes share one isolated project and must be serialized:

```bash
python -B tools/run_materials_render_pure.py --label fresh-pure
python -B tools/run_materials_render_native.py --label fresh-host --mode native
python -B tools/run_materials_render_native.py --label fresh-pixels --mode graphics
python -B tools/run_materials_render_native.py --label fresh-presentation --mode legacy-native
python -B tools/run_materials_render_native.py --label fresh-phase2 --mode legacy-phase2
python -B tools/run_materials_render_native.py --label fresh-host-legacy --mode legacy-host
python -B tools/run_materials_render_native.py --label fresh-host-play --mode legacy-host-play
python -B tools/run_materials_render_native.py --label fresh-legacy-pixels --mode legacy-graphics
python -B tools/run_materials_render_audit.py
```

The audit intentionally reconciles the named retained final runs below rather than treating a fresh arbitrary label as accepted evidence. It checks actual exits/counts, current exact source/staged pins, supplied pins, complete PNG CRCs and the unchanged empty manifest.

Native editor executable: `C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe`.

Explicit new entry points:

- `MaterialsPresentationChecks.Run`: native host/default/opt-in/path/profile/actual-restore checks.
- `RenderFixtureBuild.Run`: isolated release Mono/D3D11 fixture build.
- `RenderFixtureBuild.RunLegacy`: the unchanged legacy graphical assertions in the same owned fixture project.

Project: `.artifacts/materials-render-project`. Its own test scenes/player/settings/packages/TEMP/TMP/TMPDIR, save fixtures and pure outputs stay below that project. Evidence/logs/PNGs are retained under `.artifacts/materials-render/<label>`. Staging copies current production sources byte-exactly and uses explicit test entry files, not recursive test compilation. No test packages are installed. All Unity dependencies are installed built-in modules. Player launch dimensions are explicit to avoid an earlier legacy fixture's persisted window size contaminating later proofs. No foreground/window-focus control is used; the unchanged legacy fixture sends messages only to its own player window.

## Verified final results

Every final report is nonempty, count-matched, passed, and has real exit 0. Audit index: `.artifacts/materials-render/evidence-index.json`.

| Final report label | Native/pure assertions | Result |
|---|---:|---|
| `10-final-host-restore` | 40 | all 121 actual bindings; default/explicit budget; unsafe config fallback; profile revalidation; silent startup; real isolated save and exact fresh-controller restore; no reaward/audio/save-byte mutation on rebind; source stop and disposal |
| `12-corrected-oracle-green` | 67 | actual and synthetic card/wrapper/mask/back GPU proofs; clean production inspection/results fixture; no startup source; material disposal |
| `10-final-legacy-native` | 29 | unchanged Phase 3 native presentation/audio checks |
| `10-final-legacy-phase2` | 62 | unchanged Phase 2 native component/controls/layout checks |
| `10-final-legacy-host` | 87 | unchanged real controller/store/host/Retry/restore/default-empty-media checks |
| `10-final-legacy-host-play` | 18 | actual Editor Play Mode startup/AudioSource/deferred destruction/recovery/restore checks |
| `11-final-legacy-graphics` | 234 | unchanged native graphical/input/audio/hidden-identity checks at normal, short and ultrawide sizes |
| `08-final-pure` | 39 | unchanged pure deterministic motion/layout checks, compiled warnings-as-errors |

**576 final assertions**, reconciled programmatically. New graphics retained **31 PNGs**; legacy graphics retained **34 PNGs**; all **65** final PNGs passed signature/dimension/complete-chunk CRC/IEND validation. Both release fixture builds reported `Succeeded errors=0 warnings=0`. Unity's nonblocking licensing access-token diagnostic remains verbatim in editor logs.

Actual private screenshots inspected through the image tool include:

- `actual-production-clean-inspection.png`: full Alakazam print, HP/attacks/copyright visible, outside-art collection information and reachable Back.
- `actual-production-results-fixture.png`: eleven explicitly projected copies of the representative face, with no overprinted local badges. This is **not** a generated game pack or probability evidence.
- `actual-holo-mask.png`, `actual-card-back.png`.
- `actual-wrapper-front-metallic.png`, `actual-wrapper-back-metallic.png`: clean full fitted wrapper front/back, neutral restrained reflection and readable print.

The final GPU suite adds a neutral-reference comparison for the entire actual production inspection, not just a cue counter; this prevents a blank screenshot from passing as visual proof.

## Real RED/GREEN and retained diagnostics

Four vertical native tracer bullets were observed before implementing their production changes:

1. `01-host-red` -> `01-host-green`: missing trusted serialized fields, then legacy-safe defaults. The first RED has zero successful predecessor checks because its first assertion failed; its actual failure and native exit 1 are retained, not presented as a nonempty passing suite.
2. `02-wrapper-red` -> `02-wrapper-green-corrected-oracle`: missing wrapper reflection (zero changing pixels), then explicit masked movement.
3. `03-host-selection-red` -> `03-host-selection-green-profile`: configured manifest ignored, then selected for legacy/default and exact GUID-N profiles.
4. `04-host-budget-red` -> `04-host-budget-green`: full flag ignored (two admitted faces), then four admitted only for explicit opt-in.

All paired RED exits are 1; GREEN exits are 0; source/report pins and expected assertion text are retained in the audit index. Historical pins are not overwritten with current hashes. The final corrected pixel oracle was additionally re-exercised against an explicitly recorded **isolated-stage-only missing-wrapper-call defect**: `12-corrected-oracle-defect-red` failed with zero moving pixels, then exact unmodified production staging passed as `12-corrected-oracle-green`. The audit verifies that only the declared staged call differed in the control and production source remained unchanged. This is an oracle control, not a fifth production TDD cycle.

Intermediate fixture failures were retained honestly and are not counted as extra TDD cycles or final passes: a CPU/GPU bilinear-edge oracle initially lacked its one-texel interpolation footprint; an invalid synthetic profile token was replaced with an exact GUID-N fixture; one legacy output-path adaptation was unnecessary and removed; persisted window dimensions caused cropped capture until explicit launch dimensions were added; neutral face references initially differed from production's outside-face shadow; and deferred material destruction needed the actual end-of-frame boundary. Image inspection also exposed an initially blank full-inspection capture, so the fixture now invokes the real production OnGUI path and verifies it against a neutral fitted print reference. No production feature was removed or weakened to hide these fixture issues.

## Remaining parent gates

The parent must set the two fields only in its separate new candidate scene, preserve GUID/resource closure, build its separate candidate, exercise a **real** complete generated pack plus Binder/retry/restore flows, independently inspect candidate visuals/audio, and stop for user feedback. This worker did not build or accept the full candidate, modify accepted player/save bytes, assert physical hearing, or close work. Media/asset-worker native pixel/decode/provenance inventories remain separate evidence, not duplicated totals here.

This document records the reusable isolated TDD/native rendering workflow locally because profile/skill writes are outside the authorized scope.
