# Mouse-over card / wrapper tilt — implementation contract

## Current process / user steering

User reports the supplied art is present and **everything else functions with PASS**, but holo/metallic response is not perceptible. Requested movement of the mouse over cards/booster so they tilt left/right/up/down and catch a shifting light. Interpret ordinary pointer movement across the surface; no wheel/drag/click is required for tilt. Preserve accepted art/printing/flow; reopen only surface interaction/reflection visibility. This is not Phase5.

Verified cause in source: TabletopPresentation draws supplied art axis-aligned with GUI.DrawTexture. Phase3Foil and WrapperMetallic only sample UV/mask and presentation time; no surface normal, view angle, mouse input or real tilt participates. A subtle sine overlay is not the requested viewing interaction. Existing material player299d060/build35/136immutable files was rechecked unchanged, saves/logs pruned before traversal. Keep that player and acceptedPhase4Windows intact.

Write ONLY inside F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG. All controlled stages/TMP/log/test saves/builds there. No profile/skill/parent/install/network/publication. Main interactive Editor and actual live saves untouched. Candidate separate **Builds/Phase3TiltWindows**, stage **.artifacts/tilt-build-project**. Source originals114, all derivedPNG/manifest/images/metas unchanged. Old empty media manifest0 stays exact; no model/catalog/persistence/collection changes. Preserve user-owned untracked ProjectSettings/PackageManagerSettings.asset.

## Projected behavior

- Mouse position within the ORIGINAL fitted card/pack rectangle controls bounded pitch and yaw; left/right and up/down visibly rock the surface, never flip it. Smooth movement and return to neutral after pointer leaves. No new gesture or progression command.
- Perspective silhouette/texture projection, not just rotating a rectangular outline or scrolling the old sine overlay. Face and its mask use the SAME projected coordinates.
- A physically inspired virtual softbox/key-light model responds to surface normal/view direction, creating clearly visible moving holo/metallic highlights. Gentle ambient sweep may remain, but mouse tilt must visibly drive the effect even with time held constant. Silver wrapper highlights and holo color response stay mask-confined. Matte/nonholo cards tilt but never acquire metallic/rainbow regions.
- Full printed faces retain readable HP/attacks/copyright. Underlying pixel data is unchanged; no full-card paint/glare fallback. No intrusive edition warning. Source scans remain authorized visual references for existing Unlimited identities.
- Stable original hitboxes for inspection/reveal/Back; no accidental drag/open/reveal/save/audio. Pointer in letterboxing/UI must not tilt another surface. Hidden/unrevealed fronts remain hidden. Results cards may tilt on hover, but priority/identity must not leak to an occluded/background card; primary hero/inspection/wrapper proof is required.
- Context keyed per active visible surface (wrapper/hero/back/inspection/results + transaction/index), no pose bleed across cards/views. Bound retained controllers, reset/dispose on teardown. Pause/focus-loss/error/Binder gate freezes current pose/light clock; resume/unmute never replays a cue. Context change/leave returns safely, no persistent state writes.
- New **pointerTiltEnabled** serialized TabletopPresentation field defaults false so historical scene/fixture behavior stays stable. ONLY new tilt scene enables it. New hint outside art: move mouse over card/pack to tilt/catch light. No new buttons/hitbox changes required.

## Frozen helper API / ownership

Surface worker owns NEW Presentation/HoverTiltMotion.cs,Presentation/TiltedSurfaceRenderer.cs,NEWResources/Presentation/TiltedSurface.shader(+actualmetas),TestsTilt/Surface* explicit files,tools/run_tilt_surface*.py,Editor/TiltSurfaceChecks.cs,docs/technical/Tilt_Surface.md. Do NOT edit TabletopPresentation or existing shaders/Media/Runtime/Scene/Builder/Core/Data/Persistence/settings/Git.

Pure `public sealed class HoverTiltMotion` in VintageCollector.Presentation:

```csharp
public float PitchDegrees { get; }
public float YawDegrees { get; }
public float TargetPitchDegrees { get; }
public float TargetYawDegrees { get; }
public const float MaxPitchDegrees = 12f;
public const float MaxYawDegrees = 12f;
public void SetPointer(float u,float v,bool inside); // u,v normalized0..1; top-left v0
public void Advance(float deltaSeconds,bool running);
public void Reset();
```

Map pointer left/right to yaw and up/down to pitch with both signs. Reject/neutralize nonfinite input, clamp bounds, smooth approach/leave without overshoot; Advance(false) freezes exactly. Finite long delta safe. No engine/RNG/DTO/file dependency.

Unity helper `public sealed class TiltedSurfaceRenderer : IDisposable`, `public enum SurfaceFinish { Matte, Holo, Metallic }`:

```csharp
public bool Draw(Rect fittedRect,Texture2D face,Texture2D mask,
 SurfaceFinish finish,float pitchDegrees,float yawDegrees,float presentationClock);
public static Rect ConservativeDrawRect(Rect fittedRect);
```

Draw only at Repaint, own a Resources-loaded material, no face/texture ownership. Return false if unavailable to permit old rendering fallback. Complete face + mask render together under true perspective projection (mesh/homography/ray-plane shader are acceptable); maximum-angle overscan must prevent clipped protruding corners while staying within safe layout. Exact same projectedUV for mask/face. Mask null/misaligned/invalid => matte surface only, never full metallic fallback. Prefer transparent alpha silhouette and normal/light/view-dependent specular/iridescence over a narrow spinning rainbow stripe. Preserve alpha/printed saturation outside masked highlight; matte shading subtle. No _Time-independent clock while paused. Native GPU tests must prove geometry as well as changing colors. Add readonly diagnostics if useful, but do not make tests depend on fabricated states.

Renderer integration worker owns targeted Presentation/TabletopPresentation.cs,TestsTilt/Integration* explicit files,Editor/TiltPresentationChecks.cs,tools/run_tilt_presentation*.py,docs/technical/Tilt_Presentation.md. Do NOT edit surface worker helpers/shader,existingMedia/Core/Runtime/paths/scenes/settings/Git.

Add pointerTiltEnabled defaultfalse; collect a coherent current-render surface set with fixed original fit rectangles and front/role/visibility/context. Read actual OnGUI/client pointer coordinates consistently with layout scale and DPI, select topmost eligible surface, feed normalized position to bounded keyed HoverTiltMotion instances, smooth in Update with existing running gate. Don't publish a historical surface/hitbox union. Render actual supplied faces/backs/wrappers through TiltedSurfaceRenderer only when enabled, with proper finish/mask; fall back old path safely. Ensure instance is disposed without destroying externaltextures. Inspection alone receives hover when modal; Binder/pause/error/focus disabled. Existing known command/hitbox authority stays unchanged. New hint outside art. Retain old clock/cue dedup/shader behavior for defaultfalse and preserve card11 hold.

Add detached smoke-readable `public TiltObservation[] TiltObservations { get; }` on TabletopPresentation, with nested public [Serializable] `TiltObservation` fields `string key; Rect bounds; float pitchDegrees,yawDegrees; bool hovered; int finish,renderFrame;`. Snapshot only CURRENT visible surfaces, never cache history; mutating returned DTOs must not affect motion/render/input. Bounds are original fitted logical rects, finish maps SurfaceFinish. This supports parent-owned actual input/framebuffer proof without synthetic pointer injection.

Parent owns NEW tilt builder/policy/bootstrap/probe/player-driver, metadata adoption/source/compiler/candidate pins, documentation/status/Git and final acceptance. Builder copies trusted newmaterial manifest/fullbudget scene config plus pointerTiltEnabled=true. No rebuilding current materials player or copying live saves.

## Test / proof gates

Use real RED→GREEN vertical slices, not just a missing-class assertion. Pure motion: all4directions/center/outside/clamps/NaN/inf/frame-rate/largeDelta/freeze/reset/noovershoot. GPU: actual supplied holo + nonholo + cardback + wrapper front/back; compare neutral/left/right/up/down at FIXED clock to establish perspective edge displacement and normal-dependent moving highlights, outside-mask print preservation under SAME geometry reference, alpha silhouette/clipping, maskUV alignment at all extremes, failurefallback and material disposal. Show that nonholo matte does not gain metallic response.

Native integration must move pointer by owned-window background messages, never foreground/globalcursor APIs. Observe actual pose/control/framebuffer changes; initial condition false then true, no-op clicks aren't proof. Test leaving returns neutral, hovering buttons doesn'ttilt, inspection modal selection, context reset, results/hero priority, resize/scaling1280x720/1600x900/ultrawide, pause/focus/error/Binder freeze and no save/reveal/cue on hover. Keep fixture saves under .artifacts. PNG completion/CRC/dims, before/after source pins and real counts required. Separate helper/fixture proof from full assembled player and human feel.

Parent must exercise actual new player: real pack, card11 held, all inspection/Back hitboxes, physical-edit/retry/backup/import/restart regressions with art, actual hover over wrapper+matte+holo+inspection across4directions, no generation/award/save changes, masked highlight perceptible. Independently review then request ONLY focused tilt/reflection feedback; retain other user's PASS. No human acceptance inferred from pixel deltas. Stop before any further roadmap phase.
