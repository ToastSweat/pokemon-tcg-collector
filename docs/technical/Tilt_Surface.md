# Tilt surface backend — implemented and native GPU verified

## Scope / integration

This is the helper/backend slice of `Hover_Tilt_Contract.md`, not a new game phase or human acceptance. Only new motion, surface renderer/shader, owned checks/runners/tests and this document were changed. No TabletopPresentation, previous shader, media loader, supplied input, model, probability, persistence, scene, settings, accepted player or Git changes were made by this worker.

Frozen namespace/API is implemented exactly:

- `VintageCollector.Presentation.HoverTiltMotion`: four readonly pose/target properties; `MaxPitchDegrees` / `MaxYawDegrees` = 12; `SetPointer(float u,float v,bool inside)`, `Advance(float deltaSeconds,bool running)`, `Reset()`.
- `SurfaceFinish { Matte, Holo, Metallic }` and `TiltedSurfaceRenderer : IDisposable`: `Draw(Rect fittedRect,Texture2D face,Texture2D mask,SurfaceFinish finish,float pitchDegrees,float yawDegrees,float presentationClock)` and static `ConservativeDrawRect(Rect)`.
- Renderer owns one lazily Resources-loaded material, never provider textures. `Dispose()` is idempotent, destroys the native material using the appropriate play/edit-mode lifetime, and permanently disables that instance.
- Resource path: `Presentation/TiltedSurface`. A missing/unsupported shader, invalid rectangle, null face, disposed renderer or non-Repaint event returns false. A null/dimension-mismatched mask or unknown finish returns a successfully rendered **matte** face, not full glare.
- Same-sized masks must be semantically aligned by the provider. Dimension checks cannot prove that an arbitrary same-sized image is the correct mask; the actual supplied masks are tested below.

**Call-site requirements:** give Draw the ORIGINAL already-fitted face rectangle, not its conservative overscan; Draw paints the complete face and finish together, so do not also paint the old axis-aligned face/finish over it. Use the original rectangle for pointer normalization/hitboxes, never the overscan. Draw in the existing GUI coordinate system at Repaint. Avoid a GUI clipping group restricted to the original face rectangle. Keep existing ordering/modal ownership and lifecycle gates in the integration worker. Retain old rendering when Draw returns false. Dispose at teardown. New scene opt-in remains the parent/integration worker's responsibility; old scenes stay false.

`Editor/TiltSurfaceChecks.cs` has **no compile-time reference** to test-only `SurfaceGpuFixture`. Its Build entry resolves the fixture by string/reflection only when explicitly invoked by the isolated runner. A normal candidate source compile does not need any TestsTilt code in Assets. `--compile-only` verified this with the fixture removed from the owned stage.

## Motion math

`yawTarget = (2*clamp01(u)-1)*12`; `pitchTarget = (1-2*clamp01(v))*12`, so left/right have negative/positive yaw and top/bottom positive/negative pitch. Outside/nonfinite pointer input neutralizes both targets. The target is bounded, not accumulated.

Pose advances by the exact first-order response `pose += (target-pose)*(1-exp(-12*dt))` with double intermediates. This is a convex interpolation with no overshoot, independent of frame subdivision up to floating-point tolerance. Negative, zero, NaN or infinite dt does not advance. A finite huge dt settles safely. `running=false` changes no pose. Reset clears pose and target. The class references only System math; no Unity, RNG, DTO, I/O, input or clock dependency.

## Perspective / lighting math

This is an inverse ray-plane projection, **not** a 2D rotated outline or UV/time overlay. No scene/world/physics objects are created.

1. Normalize card dimensions by their longest fitted side `S`, giving `w=W/S`, `h=H/S`. The virtual camera is `C=(0,0,2.4)`; the unrotated card is centered on z=0.
2. Rotate the plane by `R=Ry(yaw)*Rx(pitch)`. Its basis is `bx=(cy,0,-sy)`, `by=(sy*sp,cp,cy*sp)` and normal `N=(sy*cp,-sp,cy*cp)`.
3. Each raster fragment maps to a ray `D=(screenX,screenY,-2.4)`. Intersect with the rotated plane using `t=-dot(N,C)/dot(N,D)` and `P=C+tD`.
4. Recover the **single shared UV** as `(dot(P,bx)/w, dot(P,by)/h)+0.5`. Both printed face and finish mask sample this UV. Discard fragments outside [0,1]^2; multiply output by the original face alpha through standard source-alpha blending.
5. `V=normalize(C-P)` and `reflect(-V,N)` determine the reflected view ray. Intersect it with a fixed virtual light plane z=2. Two broad super-Gaussian softboxes on that plane give a spatial reflection footprint. The surface normal and per-fragment view direction both affect it. Tilting moves that footprint at a completely fixed clock.
6. Holo adds a restrained color spectrum derived from reflected-light position and surface UV, only under `mask.r*mask.a`. Its small source attenuation retains illustration linework. Metallic adds equal RGB energy, capped by common channel headroom, so wrapper foil/crimps gain neutral silver rather than a rainbow. Matte takes neither branch. Paper shading is below approximately one percent over the allowed angles and never adds a metallic region.

The clock argument is retained and finite-sanitized for the frozen API, but this implementation deliberately has **no autonomous ambient time animation** and never reads `_Time`. A fixed pose is fixed light, including during pause. There is no deferred time-overlay implementation.

Overscan pads every side by `0.16*S+2` logical pixels and rounds its raster edges outward; the shader receives the corresponding center offset. At ±12 degrees each surface point remains far from the camera (`|z| <= sin(12)*(w+h)/2 < 0.208`), so perspective magnification stays bounded. The pad exceeds the corresponding worst projected excursion even for extreme aspect ratios. Outward rounding matters: native Graphics.DrawTexture otherwise implicitly truncated fractional raster edges, causing thin wrapper-edge CPU/GPU UV disagreements. Explicit raster dimensions and center correction removed this, without relaxing mask/background preservation.

## Reproducible commands

Run from a quoted repository path because this host rejects bracketed terminal workdir arguments:

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python tools/run_tilt_surface_pure.py --label NEW-motion
python tools/run_tilt_surface_native.py --label NEW-native --slice full
python tools/run_tilt_surface_native.py --label NEW-normal-compile --compile-only
python tools/run_tilt_surface_native.py --label NEW-missing --missing-shader
python tools/run_tilt_surface_native.py --label NEW-unsupported --unsupported-shader
python tools/run_tilt_surface_native.py --label NEW-disposal-control --slice safety --mutant-leak-material
python tools/run_tilt_surface_proof.py --label NEW-native
```

Labels must be new; evidence directories are intentionally not overwritten. The disposal mutation is isolated stage-only and is expected to exit 1. Missing/unsupported probes are expected to exit 0 because false-return/background preservation is their asserted behavior. The unsupported control limits the **staged** shader to glcore while the real player runs D3D11. Never use a defect-controlled stage as an integration source; original owned production sources are unchanged.

All controlled copies/TMP/player/logs/evidence are confined to `.artifacts/tilt-surface-project`. The runner enumerates explicit production/test source files. Pure compilation uses the installed Unity 6000.2.14f1 bundled Mono/mcs with `-warnaserror+`; no package installs are needed. The native build creates an isolated Mono Windows player forced to D3D11, then actually executes that player with graphics enabled. The batch Editor's `-nographics` applies only to building, **not** to the GPU proof player. No game/session/collection host or audio component is included in the fixture.

## Actual retained evidence

Canonical evidence root: `.artifacts/tilt-surface-project/evidence/`.

| Run | Actual result |
|---|---|
| `motion-red` | exit 1, API-complete no-op implementation fails right smooth yaw |
| `motion-right-green` | exit 0, first vertical slice passes |
| `motion-edges-red` | exit 1, yaw-only implementation fails up pitch |
| `motion-final-green` | exit 0, 215 named assertions; 200 are repeated bounded-reversal checks, **not 215 distinct behaviors** |
| `geometry-corrected-red` | exit 1, actual left silhouette mismatch 1130 pixels against axis-aligned renderer |
| `geometry-green-v2` | exit 0, ray-plane geometry/texture slice passes |
| `reflection-red` | exit 1, geometry-only shader gives 0 highlighted pixels at fixed clock |
| `disposal-control-red-v2` | exit 1, stage-only material leak fails actual deferred destruction |
| `safety-green` | exit 0, actual native lifetime/input/alpha tests |
| `missing-shader-green` | exit 0, false return and unchanged framebuffer |
| `unsupported-shader-green` | exit 0, false return and unchanged framebuffer on real D3D11 |
| `production-compile-final-green` | exit 0, production helpers + Editor check compile with **no fixture in Assets** |
| `native-final-green` | exit 0, **434 named assertions / 106 complete 720×720 native PNGs** |

Canonical native device: **NVIDIA GeForce RTX 5060 Ti**, **Direct3D11**, Unity **6000.2.14f1**. `native-final-green/unity.log` contains `SURFACE BUILD Succeeded errors=0 warnings=0`; no C# warning/error or shader error was found in that final run.

The five actual representative supplied inputs are Alakazam holo, Beedrill nonholo, cardback, wrapper front and wrapper back, with their actual holo/metallic masks. Every input is captured at neutral/left/right/up/down and all four diagonal ±12-degree combinations, always clock **7.25**. Their same-pose matte references are captured separately. The 434 count includes repeated assertions across these poses, not 434 unique visual tests.

In that final run:

- 47 geometry sets (45 representative poses plus two initial tracer poses) verify analytical inverse-projection silhouette, conservative bounds, GPU near-corner coverage and sampled texture landmarks.
- 27 coated-versus-matte comparisons preserve every tested solid-black-mask/background pixel **exactly** under the same geometry; 24 non-neutral comparisons independently change reflection in surface-UV bins versus neutral. These are not raw comparisons between differently projected cards.
- 18 whole-frame same-pose matte comparisons remain byte-identical at a different clock. Valid-mask matte references also demonstrate that finish, not mask availability alone, enables reflection.
- Null/wrong-size mask frames exactly equal matte; extreme angles exactly equal ±12 degrees; NaN/infinite pose and NaN clock exactly equal neutral; invalid rect/null face/disposed renderer preserve background.
- Actual supplied scans are opaque. A separately labeled synthetic-alpha texture therefore tests projected source-alpha holes; it is not represented as supplied art. Native material destruction is observed after deferred destruction, and the externally owned face remains readable.
- Geometry/mask classification samples every second framebuffer pixel. Source-color landmarks tolerate a half-texel footprint and 0.13 channel error for CPU/GPU bilinear differences and scan high-frequency text. Mask/alpha classification explicitly expands two source texels around transitions; **solid outside-footprint pixels remain exact**, not approximate. Geometry color sampling is not an OCR/readability guarantee.
- `png-evidence.json` validates each full PNG signature, every chunk CRC, IHDR dimensions, final IEND, no trailing bytes and SHA-256. `source-pins.json` and `source-after-pins.json` establish before/staged/after equality for the exact compiled sources. `protected-pins.json` proves all **107 unique supplied PNGs plus both manifests (109 files)** unchanged; only the eight representative PNGs required by the fixture are copied to the stage. The old empty manifest remains unchanged.

The initial `geometry-red` neutral color/background oracle incorrectly classified dark ink as empty background; it is retained as an unsuccessful attempt, not counted as the geometric RED. `geometry-corrected-red` supplies the valid axis-aligned defect observation. `reflection-green-v2` timed out before producing a report because the fixture attempted to infer current GUI event type from a coroutine; Unity can leave Event.current stale at Repaint. The fixture now observes actual Layout/input events inside OnGUI and records failures without stranding its coroutine. That interrupted run is not claimed as GREEN. An early fixture member-hiding warning was removed; final native compilation is clean.

## Local visual artifacts / interpretation

`evidence/visual-proof-native-final-green/` contains `masked-finishes.png`, `matte-surfaces.png`, a complete 9-frame `fixed-clock-native-poses.gif` and hashes/dimensions/PNG CRC/source-capture pins in `evidence.json`. These are labeled composites/sequences of actual native captures, **not generated replacement art**, live mouse input or smooth-transition recordings.

Vision inspection of the equivalent contact sheets observed opposing perspective edge/texture shapes, retained visible corners, moving holo coloration confined to the illustration, silver wrapper/crimp changes without added rainbow, and intact nonholo/cardback printing. At contact-sheet scale tiny legal text cannot be read; original full 720×720 frames retain the print. The 106 native frames and pixel assertions establish this backend's projection/light behavior, not human-perceived interaction quality, assembled-player pointer routing, modal priority, resize handling or accepted game regressions. Those remain with the integration/parent workers under the shared contract; no human acceptance or task/roadmap closure is inferred here.

## Production source pins

SHA-256 from the canonical native run:

- HoverTiltMotion.cs: `37187ab5a5cb0447f21a200e5193b25679fa78f68218aabafedd4b133107e895`
- TiltedSurfaceRenderer.cs: `11293d0013225b4a524f752eaad65043bdcc8a634882db240d902ff94ac96ad4`
- TiltedSurface.shader: `a38acf9855c59ccaa504fd6ecdbb8df8252c23006d9b9656f7b67ddd4a473861`
- TiltSurfaceChecks.cs: `8c7b588289710b6a2b3d855543fb140f995c4ef3823b90a53290f6513af080ff`

The four owned production `.meta` files were adopted from the actual isolated Unity stage. No other worker's metadata was adopted.
