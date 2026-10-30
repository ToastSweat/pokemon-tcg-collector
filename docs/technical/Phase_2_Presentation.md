# Phase 2 presentation — passive placeholder tabletop

## Delivered boundary

`VintageCollector.Presentation` is a complete immediate-mode Unity tabletop, not a generator or save service. It has no Domain/Persistence dependency, startup generation, collection writes, file IO, PlayerPrefs, Resources data copies, downloaded images, logos or audio. Main-project Unity was never launched for this work. Tests ran only in `.artifacts/phase2-presentation-project` and its synthetic fixture player.

The neutral working title is **Vintage Card Collector**. The only displayed option is **1999 English Base Set Unlimited**. The header, wrapper, card faces and inspection explain their PLACEHOLDER status. The footer always labels rules provisional; physical tracking is explicitly not configured. Nothing promises historical factory collation, physical collection updates, purchases or money features.

## Frozen integration API (unchanged)

Namespace: `VintageCollector.Presentation`.

```csharp
public sealed class OpeningView
{
    public string stage, transactionId, errorMessage, recoveryNotice,
                  modelLabel, statusLine, saveLocation;
    public int revealedCount, totalVirtualCards;
    public OpeningCardView[] cards;
    public bool paused;
    public float volume = 0.5f;
    public int inspectIndex = -1;
}
public sealed class OpeningCardView
{
    public string id, name, collectorNumber, rarity, kind, finish;
    public bool isNew;
}
public sealed class TabletopPresentation : UnityEngine.MonoBehaviour
{
    public OpeningView View { get; set; }
    public event System.Action<string> Command;
    public void NotifyCommitted(string command);
}
```

Stages: `ready`, `sealed`, `revealing`, `results`, `error`.

Commands emitted verbatim:

- `generate`, `open`, `next`, `skip`, `retry`
- `pause`, `resume`, `quit`
- `inspect:<zero-based index>`, `close-inspect`
- `volume:<invariant decimal 0..1>`

No new command or required public integration method was added. Public pure `PresentationRules`, `PresentationLayout` and `PresentationBox` helpers are local projection/layout policy, not authoritative application state. Internal `TextObserver` exists solely to observe actual native GUI label calls during isolated testing; no observer is installed in normal use.

### Parent wiring

Attach `TabletopPresentation` to the parent's scene object; no Canvas, EventSystem, materials, prefab, package or asset wiring is needed. Parent subscribes to `Command` and supplies `View` after processing commands. DTOs are not mutated by presentation.

```csharp
presentation.Command += HandleCommand;
presentation.View = BuildViewFromAuthoritativeState();
// In successful durable open handler ONLY, after OpenWrapper succeeds:
presentation.View = BuildViewFromAuthoritativeState();
presentation.NotifyCommitted("open");
```

Parent owns transaction identity, generation, durable action ordering, absolute reveal progress, retry identity, pause, inspection selection, volume and quit. Do not optimistically increase `revealedCount` before persistence. `NotifyCommitted` cannot advance cards, emit commands, change a DTO, generate contents or award anything. Only committed `open` starts the 0.55-second wrapper-strip cue. This cue never blocks or changes supplied visibility/order. There is deliberately **no audio**; volume/mute are real commands for the parent's settings, not a claim of sound playback.

A null `View` produces a disconnected waiting screen with actions disabled; a ready view does not issue `generate`. `saveLocation` and `transactionId` are accepted unchanged but not placed into an overflowing path label.

## Visual and interaction behavior

- Original restrained walnut/green/cream/brass geometry; built-in **LegacyRuntime.ttf**.
- Generic crimped wrapper, shadowed card stack, 63:88 card backs/faces and inspection face. Original procedural circle/diamond masks and abstract landscape illustration; no branded artwork.
- Ready: preview wrapper explicitly says NOT GENERATED; Generate requests the parent.
- Sealed: generic saved wrapper with Open button; no card names are read.
- Revealing: only the latest revealed face is shown, with remaining anonymous backs. Numbered **revealed-position** buttons keep every already-revealed card inspectable before results. These buttons do not expose future positions.
- Results: all 11 supplied positions appear in original order, six then five. Each shows NEW · VIRTUAL or DUPLICATE from the supplied per-position flag; summary counts are not collection-completion percentages. Malformed/incomplete results still hide unrevealed faces and cannot generate a new pack.
- Inspection: enlarged face, supplied name/number/kind/rarity/finish and virtual status. Invalid/unrevealed index shows a safe message and Back, never its name/face.
- Holo cue: static small highlights only inside the illustration and only for explicit `holo` finish (case-insensitive). `nonHolo` / `Non-holo` do not trigger it. No full-card rainbow.
- Every action is a mouse button; no dragging is required. Click a face or revealed-position number for inspection.
- Escape closes inspection first, otherwise requests pause. Focus loss requests pause; focus gain does not resume. A paused screen requires explicit Resume and includes Quit plus volume − / + / Mute / Unmute buttons. This layer does not change Time.timeScale.
- Underlying buttons are disabled under pause/inspection. Error offers Retry and Quit and shows no hidden faces.
- Logical layout: `min(pixelWidth/1600, pixelHeight/900)` scale, then logical viewport dimensions are derived from the **full** physical viewport. Short/ultrawide windows retain reachable Back/Resume/Quit. At very short heights typography necessarily becomes small under this contracted scaling; this is not an accessibility claim.
- Native-font fitting reduces long result names to a minimum 12 logical pixels as necessary; actual-font checks cover Super Energy Removal, Double Colorless Energy and Pokémon Breeder.

## Source ownership

Production:

- `Assets/VintageCollector/Presentation/OpeningView.cs`
- `Assets/VintageCollector/Presentation/PresentationRules.cs`
- `Assets/VintageCollector/Presentation/TabletopPresentation.cs`

Verification:

- `Assets/VintageCollector/Editor/Phase2PresentationChecks.cs`
- `TestsPhase2/PresentationTests.cs`
- `TestsPhase2/PresentationRenderFixture.cs`
- `TestsPhase2/PresentationFixtureBuild.cs`
- `tools/run_phase2_presentation.py`
- `tools/run_phase2_presentation_editor.py`
- `tools/run_phase2_presentation_render.py`

The render fixture and its build helper are **not** under the main Assets tree. They are copied only into the isolated project, create labelled synthetic DTOs without the core, and never belong in the parent's scene or player. No existing `Tests` files, runtime/controller, persistence, domain, data, package/root settings or Git state were changed by this worker. All controlled temporary/build/log files remain under project `.artifacts`; Unity-managed caches/licensing were permitted.

## Reproduce verification

From any directory, with this checkout path:

```text
python "F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/tools/run_phase2_presentation.py" --label rerun-pure
python "F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/tools/run_phase2_presentation_editor.py" --label rerun-engine
python "F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/tools/run_phase2_presentation_render.py" --label rerun-render
```

Do not run the two Unity commands concurrently: they share the isolated project. Installed editor used: Unity **6000.2.14f1**. Pure tests compile with its bundled Mono and no Unity references. Editor tests instantiate the real MonoBehaviour and test actual native Rect/font APIs. The graphical fixture builds Windows x64/Mono/D3D11, then runs **without `-batchmode` or `-nographics`** so Unity really executes OnGUI. The initial batchmode player produced no GUI label callbacks despite initializing D3D; that failed attempt is retained rather than called a render pass.

### Actual final results

All counts below were parsed and reconciled against their arrays, not inferred from process success:

| Checkpoint | Actual result | Evidence under `.artifacts/presentation/` |
|---|---|---|
| Pure policy/layout | **5 test groups passed**, exit 0; nested cases cover all reveal boundaries, hidden inspection, stages/commands, invariant-volume rejection, seven window sizes, 63:88 ratio, non-overlapping 11-card results/history controls, footer separation and finish policy | `12-history-green.log`, `12-history-green.json` |
| Real engine component | **62 named assertions passed**, exit 0; no startup generation, exact command emission, no disconnected advance/mutation, hidden-index rejection, focus pause/explicit Resume, mute, commit-only tear, native bounds and long-name fitting | `15-bold-delivery-engine-process.log`, `15-bold-delivery-engine-unity.log`, `15-bold-delivery-engine-engine.json` |
| Isolated fixture build | **Succeeded, 0 build errors, 0 build warnings**, exit 0 | `13-delivery-render-build-unity.log`, `13-delivery-render-build-process.log` |
| Graphical synthetic fixture | **2,468 assertions passed**, exit 0; this includes repeated per-label hidden-canary assertions, not 2,468 distinct feature tests | `13-delivery-render-render.json`, `13-delivery-render-player.log`, `13-delivery-render-player-process.log` |
| Rendered captures | **32 PNGs**, actual dimensions checked against filename: 1280×720, 1600×900, 2560×1080 and 1280×360; each includes ready/sealed/revealing/inspection/invalid-inspection/results/pause/error | `render/<width>x<height>-<stage>.png` |

Screenshots were actually loaded and visually inspected: normal ready/sealed/revealing/error, 1280×720 results, ultrawide inspection, short-window pause and short-window invalid inspection. Final examined captures show readable ordinary-window labels, bounded geometric art, all eleven result faces, visible history controls and reachable modal buttons. This is image inspection, not human mouse playtesting.

### RED/GREEN evidence and issues resolved

Retained logs include DTO missing → present (`01-*`), visibility missing → implemented (`02-*`), command policy (`03-*`), responsive layout (`04-*`), absent real component → passing component (`05-*`), missing actual-GUI observation seam (`06-*`), footer overlap → contained grid (`07-*`), explicit finish policy (`09-*`), native font fit (`10-*`) and revealed-history access (`12-*`). New behavior was built in vertical test-first slices; later checks extend the same slice rather than representing every individual rendering line as a separately test-driven function.

Rendered inspection caught a real Unity IMGUI issue: rotating GUI.matrix inside BeginGroup allowed decorative diamonds to escape the illustration clip. Replaced rotation with an axis-aligned original procedural mask. Pixel-level regression checks now reject unique illustration colors outside the art rectangle, with exactly one physical pixel of tolerance for fractional rasterization edges. A corrected-oracle mutation run against the old rotation **still failed** (`08-corrected-oracle-mutant.json`, 4,466 escaping pixels at 1280×720), then the final fixed run passed all four resolutions. Earlier `08-art-bounds-green-*` is intentionally retained as a **failed** intermediate run: it rejected a one-pixel edge before the oracle tolerance was corrected; its filename is not pass evidence. Footer Pause wording/padding was also corrected after rendered clipping was observed.

Unity editor logs contain an unavailable licensing access-token refresh diagnostic followed by successfully resolved entitlements, and some editor teardown Curl callback diagnostics. Builds/assertions completed successfully; these were not compiler/player exceptions. Final graphical player log has no error/exception/warning matches.

## Explicitly not established here

- Parent runtime wiring, real persistence/generation/awards, durable Retry, real process restart/resume, lock behavior or production player build.
- Physical mouse/keyboard/OS focus end-to-end interaction, despite actual button code and semantic component/focus callback assertions.
- Human opening feel, sustained performance, audio playback, supplied artwork or accessibility at tiny window heights.

The parent must integrate the frozen API, run the actual core-connected player/restart/error loop, inspect its own captures and review interactions. This deliverable stays Phase 2 only; it does not authorize Phase 3 or any physical/binder/market/finance expansion.
