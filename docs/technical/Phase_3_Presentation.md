# Phase 3A presentation — generic, replaceable-media slice

## Scope and acceptance boundary

This implements the user-approved **generic polish / asset-ready subset**, not full Phase 3 acceptance with real supplied materials. No branded media was obtained. All graphical canaries and audio fixtures are labelled original synthetic test assets, created only in the isolated fixture at runtime. The production supplied manifest was not modified.

Only the renderer/helper/shader, owned checks/runners and this document were changed. OpeningView, OpeningCardView, PresentationRules, OpeningGame, Domain, Data, Persistence, GamePaths, settings, scenes, saves and accepted Phase 2 player were not edited by this worker. A read-only `git diff --name-only` check of frozen source paths returned no differences. Parent integration/build work is separate; this worker did not launch Unity against the main project or claim a core-connected Phase 3 candidate.

## Source ownership / deliverable

Production files:

- `Assets/VintageCollector/Presentation/TabletopPresentation.cs`: passive IMGUI renderer and audio adapter.
- `Assets/VintageCollector/Presentation/Phase3PresentationMotion.cs`: pure deterministic decoration clock/easing; no engine, game, persistence or random dependencies.
- `Assets/VintageCollector/Resources/Presentation/Phase3Foil.shader`: player-retained, explicitly masked foil shader. No independent shader `_Time` input.

Verification files:

- `Assets/VintageCollector/Editor/Phase3PresentationChecks.cs`: explicit native assertion entry point, not a startup hook.
- `TestsPhase3/PresentationMotionTests.cs`: package-free pure harness.
- `TestsPhase3/PresentationRenderFixture.cs`: synthetic views/media, native source/clip assertions, screenshots and owned-window native input. Never import into the main runtime.
- `TestsPhase3/PresentationFixtureBuild.cs`: isolated scene/player builder, never import into the main runtime.
- `tools/run_phase3_presentation_pure.py`
- `tools/run_phase3_presentation_editor.py`
- `tools/run_phase3_presentation_render.py`

Shared dependency, **not owned or edited**: `Assets/VintageCollector/Media/PresentationMediaContract.cs`. Media implementation belongs to the media worker; host, source scene, candidate build, controller integration and final acceptance belong to the parent. No package/dependency installs were used. All controlled projects, builds, logs, fixtures and capture outputs are under `.artifacts`.

## Frozen public API

Namespace `VintageCollector.Presentation`, class `TabletopPresentation`:

```csharp
public OpeningView View { get; set; }              // Existing DTO, never mutated here.
public event Action<string> Command;              // Existing exact commands.
public IPresentationMedia Media { get; set; }     // VintageCollector.Media contract.
public AudioSource FoleySource { get; }           // Lazily created, renderer-owned component.
public int CommittedCueCount { get; }             // Private setter.
public string LastCommittedCue { get; }           // Private setter; null before first cue.
public void NotifyCommitted(string command);      // Existing host acknowledgement seam.
```

`CommittedCueCount` increments exactly when this renderer requests `AudioSource.Play()` with a non-null provider clip, positive effective volume and an eligible acknowledged postcommit projection. `LastCommittedCue` records that action (`generate`, `open`, `next` or `skip`). These are **requested playback diagnostics**, not proof that speakers emitted sound, a human heard it, or a clip completed. Suppressed/null/invalid/unacknowledged actions do not increment them; provider changes do not reset them.

The source is 2D (`spatialBlend=0`), `playOnAwake=false`, `loop=false`, default volume 0.5. It is lazy so old null-provider component usage remains silent and creates no AudioSource unless the diagnostic getter is explicitly requested. Binding a provider does not play anything. No provider method is called for automatic startup audio.

## Parent integration

1. Keep the shared interface unchanged. After the real OpeningGame initializes, the parent host owns `PresentationMediaLibrary.Load(...)` and assigns the resulting `IPresentationMedia` to `presentation.Media`.
2. Continue using the existing controller sequence: persist/acknowledge the action, publish its new `OpeningView`, then call `NotifyCommitted(action.command)`. Do not call NotifyCommitted on clicks, startup/recovery view publication, errors, inspect, pause, resume, volume, or failed writes. Retry success naturally acknowledges the original pending action, not a new `retry` sound.
3. Publish views through `View` to apply pause/error/volume changes immediately; Update also observes in-place legacy changes on the next frame. No save schema or settings file was added.
4. Before destroying/replacing library resources, clear `presentation.Media`. Detach stops and clears the current source. The host then disposes the library it owns. The renderer never disposes a provider or destroys a provider-owned face, mask, back, wrapper or clip.
5. Preserve the Resources shader in the source/candidate. It was loaded successfully from an actual stripped/release Mono Windows fixture player; no Shader.Find/always-included setting edit is needed.
6. Build the separate `Builds/Phase3Windows` candidate using the parent's tooling. Explain its own isolated Saves directory before human use; do not merge or copy accepted Phase 2 live saves.

Host/media implementations must not depend on fixture fake types. No filesystem, network, manifest parsing, loading arbitrary paths, generation, awards or save authority was added to the renderer.

## Rendering / input guarantees

- Faces are requested only for positions the unchanged PresentationRules permits to be visible. Providers receive the existing full `OpeningCardView.id`, not a name, set ID, list index or abbreviated ID. Hidden identities are not looked up for media.
- A non-null face is drawn whole with preserved aspect ratio and neutral GUI tint. It bypasses all local face painting: no local printed title, border, hue, rarity, number or NEW/DUPLICATE badge is layered over it. Existing sidebar/inspection information carries collection status separately.
- The hover affordance is outside the printed rectangle; the original `GUI.Button` rectangle is unchanged. A real owned-window mouse message exposed the old inner-border overpaint; the fix has retained RED/GREEN pixel evidence.
- Null face/back/wrapper bindings retain the original abstract fallback. Optional card back is drawn whole. Wrapper front is preferred; a wrapper-back-only binding is drawn whole as its fallback. No new flip command, gesture or hitbox was introduced.
- Supplied-face foil requires explicit holo finish and a non-null same-dimension mask. Missing/misaligned masks and non-holo finish disable this overlay. The shader samples **red times alpha** of the aligned mask, and affects only its marked pixels. It does not infer an art mask or tint a whole real face. The provider is responsible for semantic mask alignment/provenance.
- Generic foil is drawn only into the known abstract-art rectangle. GPU pixels were compared across changing phases at normal, short and ultrawide sizes; no animated pixels escaped into print areas.
- Restrained layered contact shadows/table edge highlights add depth. Acknowledged Next/Skip creates a short shadow-lift/settle decoration; inspection transitions settle their panel shadow. The printed face and accepted hit rectangles remain stationary. There is no action callback, gesture requirement, input delay, queued reveal, RNG or award in animation code.
- The existing crimped generic wrapper and edge-only tear cue remain available, with the legacy `tearRemaining` and `TearDuration` fields intact. The legacy visual-only NotifyCommitted("open") fixture on an already partially revealed projection remains compatible; audio still requires a valid exact postcommit shape.
- One explicit presentation clock drives foil and easing. Pause/focus loss/error freeze it; provider swaps do not randomize or reset its phase. Repeated observations of an unchanged inspection index do not restart it. Focus loss requests the original parent pause; focus gain never requests resume.
- Accepted click areas, Escape policy, Back, Resume, Quit, volume controls, history positions and result order/layout remain unchanged. Old pure and native Phase 2 checks pass unchanged.

## Audio gate and stopping semantics

Only `NotifyCommitted` can request playback. Compatible poststates are:

- generate: nonempty transaction ID, sealed, zero revealed;
- open: revealing, exactly eleven projected cards, zero revealed;
- next: eleven projected cards, revealing with 1–10 revealed, or results with eleven;
- skip: results with eleven projected/revealed cards.

Inspection, paused, unfocused, error, zero-volume, null-provider and null-clip states cannot play. Unknown or inconsistent projections cannot cue. Within the current transaction, the `(action, revealedCount)` acknowledgement is consumed once, before mute/pause/missing-provider suppression. Unmute/Resume/rebind cannot replay a suppressed acknowledgement. The host remains acknowledgement authority; a compatible view alone never plays anything.

Each new cue stops the previous one before assigning/playing the clip. Skip requests one cue, not a per-card burst. There is no clip queue and no Resume replay. The source stops/clears on mute, pause, error, focus loss, provider change and destruction. Volume is immediately clamped to 0..1 (NaN falls back to 0.5). The renderer does not synthesize foley: real supplied recordings or original generic foley come from the provider. Production media status is shown through provider StatusText, including in results, so an empty manifest is not described as containing supplied art or historical recordings. Recovery notices retain priority in the footer.

## Reproducible verification

Use a quoted native forward-slash root under this machine's MSYS Bash:

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python tools/run_phase3_presentation_pure.py --label new-motion-label
python tools/run_phase3_presentation_editor.py --label new-native-label
python tools/run_phase3_presentation_editor.py --legacy --label new-legacy-label
python tools/run_phase3_presentation_render.py --label new-graphics-label
```

Use new labels to retain evidence. Do not run two Unity commands simultaneously against the same isolated project. The stage contains only presentation sources, shared interface, owned/unchanged legacy checks, baseline copied settings and explicit test fixtures. It does not copy the game/controller, media implementation, saves or production data. Project: `.artifacts/phase3-presentation-project`; graphical fixture player: `.artifacts/phase3-presentation-player/PresentationFixture.exe` (test utility only, not the candidate).

Current completed evidence under `.artifacts/phase3-presentation`:

| Verification | Report | Count |
|---|---|---:|
| Pure deterministic motion/layout | `final-motion.json` | 39 |
| Native media/audio component | `final-native-engine.json` | 29 |
| Unchanged Phase 2 native checks against new renderer | `final-phase2-native-engine.json` | 62 |
| Real graphics/player/owned-window input/audio | `14-native-input-render.json` | 234 |
| Unchanged Phase 2 pure DTO/visibility/commands/layout/foil | `../presentation/phase3-final-baseline.json` | 5 |

All reports pass with nonzero declared counts exactly matching their enumerated checks. `final-evidence.json` reconciles these reports, validates retained PNG membership/CRCs and records SHA-256 values for byte-exact staged production/interface/DTO sources plus unchanged tracked frozen sources. Graphical run retained **34 PNGs**, each checked for PNG signature, dimensions and every chunk CRC; screenshots are enumerated in the report and retained under `14-native-input-captures/`. The isolated release fixture build reported zero errors and zero warnings.

Native graphics used 1600x900 canary face/mask/hover/wrapper/back/frozen-pause cases plus eight states at 1280x720, 2560x1080 and 1280x360, and generic foil bounds captures. Actual background Win32 messages to only the owned player window verified face inspection, inspection Back, Escape from inspection, Escape pause, paused Escape not resuming, and explicit Resume at all three additional sizes. The fixture only observes emitted commands and manually publishes labelled views; this is not game/persistence integration or physical human input.

Original synthetic canary faces prove actual optional-texture pixels and unobscured print bands. Comparison checks cover missing/aligned/misaligned masks, non-holo no-overlay, animated generic-art bounds and byte-identical paused GUI/foil frames. Actual native AudioClip.GetData samples and AudioSource.isPlaying after committed Play prove engine data/playback gating separately from physical audibility. Native tests cover no startup/unacknowledged cue, one cue per generate/open/next/skip, duplicate/invalid rejection, mute, volume, pause, error, detach and no replay. Destruction/focus stopping and external-resource lifetime also have native Editor checks.

Vertical RED/GREEN logs were retained for API, committed cue, duplicate/lifecycle gates, actual full-face rendering, shader/mask, wrapper/back/status bindings, pure clock, native clock freeze, results status, default source volume, inconsistent poststates and hover print protection. Labels `01`–`09`, `11`, `12`, `13` identify these pairs; actual RED exits are nonzero and actual GREEN exits are zero. Later `10`, `14` and `final-*` runs are expanded regressions, not relabelled REDs. The earlier `10` fixture used a revealed canary with a stale hidden-marker display name in its generic-only screenshots; this was a fixture naming defect, corrected in later retained runs, not a hidden-identity lookup or production visibility change.

## Untested / parent-owned gates

- Parent host binding to the real media library and core-connected candidate player; normal empty-manifest startup with original generic production foley.
- Full new-candidate persistence, hard-kill, failed-write Retry, once-only awards, source archive and immutable candidate manifest acceptance. No accepted player/save was exercised or changed by this worker.
- Real user-supplied face/back/wrapper/mask/recording materials, rights/redistribution, and human visual/audio quality approval. These remain deferred; synthetic evidence is not real-material acceptance.
- Physical speakers/hearing, physical mouse/keyboard comfort, sustained performance and other graphics backends/platforms. Tested graphical player backend is Windows D3D11, Built-in pipeline, Unity 6000.2.14f1.

No tracked work was closed, no Git/index mutation or commit was performed, and no global/profile/skill/install content was modified. This project-local document records the reusable isolated TDD/graphics workflow because this worker's write scope forbids profile skill changes.
