# Passive tabletop mouse-hover integration

## Implemented boundary

`TabletopPresentation.pointerTiltEnabled` is a **public Unity-serialized bool, default false**. Historical scenes and fixtures keep their existing 2D path, command rectangles, face hitboxes, audio commit policy and print rendering. The new scene/builder must explicitly enable it. No scene, builder, existing Media/Core/Runtime/persistence source, supplied asset or helper/shader was changed by this integration worker.

When enabled:

- Native `OnGUI` client-pixel mouse coordinates are read **before** installing the layout GUI matrix, then divided by the same `PresentationRules.Layout(...).scale`. There is no wheel, drag, OS cursor polling, pointer setter, save or command seam in the hover path.
- Each actual Repaint collects the surfaces drawn by the current opening/results/inspection path. Eligibility excludes unrevealed fronts and modal background surfaces. Reverse paint order selects one fitted rectangle. Four stacked backs have distinct keys and only the topmost covering back receives hover.
- Context is length-prefixed transaction + stage + visible count + table/inspection index; role and position complete each controller key. Context change drops the previous controllers. Retention is capped at 32 (normal opening uses at most five; results eleven). No card identity is added to back keys.
- `HoverTiltMotion` advances using the existing running gate, and its target returns to neutral when another surface/UI/letterboxing receives the pointer. Pause, focus loss, error, Binder/input block and a hidden collection overlay freeze pose, targets, light clock, context and pointer. A hidden overlay now freezes the clock even when its input-block property has not also been set.
- The actual selected face and corresponding mask go through `TiltedSurfaceRenderer.Draw` together. Holo requires a matching holo mask; nonholo and backs are matte; wrapper masks come only from the optional `IWrapperMetallicMedia` role matching the face actually selected. A false backend result uses the unchanged 2D texture/overlay path.
- Conservative draw rectangles are clipped to quiet layout regions, clear of history buttons, inspection information/Back, result footer labels and unrevealed-count labels. Original fitted rectangles and click rectangles do not move.
- The hint is outside the print. A native framebuffer regression caught and fixed an early hint overlap with the accepted Skip button.
- Destruction disposes the owned tilted renderer and drops controllers. External media textures remain owned by their provider. Existing `DrawFace(Rect, OpeningCardView, int)` and `DrawBack(Rect)` reflective fixture seams remain exact; role-aware internal methods have separate names to avoid ambiguous reflection.

## Frozen parent observation API

```csharp
public TiltObservation[] TiltObservations { get; }
[Serializable] public sealed class TiltObservation {
    public string key;
    public Rect bounds;
    public float pitchDegrees, yawDegrees;
    public bool hovered;
    public int finish, renderFrame;
}
```

The nested type is public. Each getter returns a newly allocated array and newly allocated entries; changing them has no input, controller or rendering authority. `bounds` is the original **fitted logical rectangle**, not projected geometry, and `renderFrame` identifies the actual Repaint. Finish values follow `SurfaceFinish` (`Matte=0`, `Holo=1`, `Metallic=2`). No historical cache or hidden-front identity is exposed. Disabled, hidden Binder, paused, error, absent view, or a view whose context has not yet rendered returns an empty set. Inspection exposes only inspection, not its background.

Additional readonly diagnostics: `TiltContextKey`, `TiltPointer` (logical GUI units), `TiltFrozen`, `TiltControllerCount`, `TiltPresentationClock`. During a freeze, the context/pointer remain the previously sampled ones intentionally; they are not authority to resurrect hidden surfaces.

## Isolated native proof

All stages, players, mailbox files, temporary files, logs and screenshots are under `.artifacts`. The exclusively owned stage is `.artifacts/tilt-presentation-project`; no interactive Editor, accepted player or live save is used. `TestsTilt/IntegrationFixture.cs` supplies deliberate read-only views and loads all **121 actual media bindings** through the unchanged library. It has no game/runtime/award/save authority. Its request mailbox controls fixture views, captures, explicit clock holding and teardown, **never pointer coordinates**.

`tools/run_tilt_presentation_input.py` identifies the HWND by its launched PID, then posts actual `WM_MOUSEMOVE` messages to it. No `SetCursorPos`, `SendInput`, foreground activation API or global keyboard input is used. The real production pointer path updates actual controller poses and the native framebuffer. The test-only fixed clock is set after production Update, before rendering; a separate slice runs the clock normally. Focus proof uses owned-window `WM_ACTIVATE`, `WM_ACTIVATEAPP`, and focus messages. **Use `WM_ACTIVATE` as well as `WM_ACTIVATEAPP`; posting only the latter was insufficient to establish an inactive transition in this fixture.**

### Current admitted evidence

Under `.artifacts/tilt-presentation/`:

| Evidence label | Real result |
|---|---|
| `final2-contract` | Native editor exit 0; 7 exact API/default/serialization/legacy-seam checks |
| `final3-integration-build` | Native Unity build exit 0; source/stage SHA-256 pins and executable SHA-256 |
| `final3-integration-input` | Native player exit 0; **194 checks, 43 complete PNGs, 60 actual WM_MOUSEMOVE messages**, plus six explicit button-click pairs |
| `final3-fallback-build`, `final3-fallback-native` | Only the staged new shader omitted; native exits 0; 11 checks prove real hover poses still change while old 2D output remains stable |
| `accepted-phase2` | Unchanged native Phase 2 checks: 62 passed |
| `accepted-phase3` | Unchanged native Phase 3 audio/commit checks: 29 passed |
| `accepted-materials-render` | Unchanged native material/print renderer fixture: 67 checks and 31 PNGs passed |

The integration report reconciles declared/enumerated check, screenshot and event counts. All PNGs have signature, chunk CRC, dimensions and terminal IEND validated. Framebuffer comparisons use decoded actual PNGs. Images cover 1280×720, 1600×900 and 2560×1080.

The 194-check native suite covers wrapper/nonholo/holo/inspection neutral and four directions at fixed clock, coordinate parity, actual framebuffer changes, moving clock, return on leave, button hover passivity, stacked-back overlap priority, results selection, results-modal priority, rejected hidden inspection, final-card-11 hold, detached observation mutation, context reset/cache bound, exact pause/Binder/hidden-overlay/error/focus freeze, material disposal without destroying external textures, and actual original FaceHit/history/Skip/Next/final-results/Back click targets. Hover produces no commands, cues or AudioSources. The one focus-loss pause request and six explicit clicks are intentionally separate from hover. The fixture produces no `.save` files; actual game save/award invariance remains the parent's assembled-player gate, not an assertion fabricated from this passive fixture.

`source-after.json` and `built-source-pins.json` bind the final input run to the actual build source and assert unchanged input bytes afterward. Original media files are copied only from manifest-declared paths; they are never modified.

### Reproduction

Use fresh safe labels (existing evidence directories are deliberately not overwritten):

```text
python tools/run_tilt_presentation_native.py --label my-contract
python tools/run_tilt_presentation_native.py --label my-build --mode build
python tools/run_tilt_presentation_input.py --label my-input --slice all --build-label my-build

python tools/run_tilt_presentation_native.py --label my-fallback-build --mode build --without-backend
python tools/run_tilt_presentation_input.py --label my-fallback --slice fallback --build-label my-fallback-build

python tools/run_tilt_presentation_native.py --label my-phase2 --mode legacy2
python tools/run_tilt_presentation_native.py --label my-phase3 --mode legacy3
python tools/run_tilt_presentation_native.py --label my-old-render --mode legacy-render
```

Run stage operations serially; they share this worker's one isolated project. Rebuild the normal fixture after the intentionally backend-free build before normal input proof.

## TDD and harness issues retained honestly

Recorded real failing assertions precede their implementation: `optin-red-assertion`, `observations-red`, `pointer-red`, `faces-red`, `binder-red`, `overlay-red`, `hint-red`, `hint-overlap-red`, `legacy-seams-red`, and `conservative-bounds-red`. Their corresponding green slices and the admitted full run remain available. Initial staging/compile failures are **not** counted as RED feature assertions. Earlier incomplete full runs remain failures, not renamed successes.

Windows atomic mailbox replacement exposed transient sharing violations in both readers and publishers. The test harness now retries on the next actual frame/bounded driver retry; it does not synthesize a state response. The raw failure evidence remains in `full-native-2` and `final2-integration-input`.

`Assets/VintageCollector/Editor/TiltPresentationChecks.cs` was audited: it references only Unity and production presentation types, with **no compile-time reference to `TestsTilt`, `IntegrationFixture` or its builder**. Those test-only components are staged explicitly by the runner, never required by the normal Assets source tree. Full normal-project/candidate compilation, scene opt-in, actual game saves/regressions and human tilt/reflection feel remain parent-owned gates. No human acceptance, publication or roadmap phase closure is claimed.
