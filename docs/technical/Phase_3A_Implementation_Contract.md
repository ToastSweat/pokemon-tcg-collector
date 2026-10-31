# Phase 3A — Placeholder-first media/polish implementation contract

User explicitly chose: “Start replaceable-asset support and generic polish; I’ll supply branded materials later.” This authorizes the generic/asset-ready subset now; real-material Phase 3 acceptance remains deferred. Do not mark all Phase 3 complete or begin Phase 4 from this checkpoint.

## Scope / preserved baseline

Only write under F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG. No parent, profile/skill, dependency installs, public uploads or branded scraping. Unity system-managed caches/licensing remain allowed; all controlled temp/log/test/build paths remain project-local. Preserve untracked ProjectSettings/PackageManagerSettings.asset and all live saves. Main generator, Data JSON, Persistence, GamePaths and OpeningGame behavior stay byte-unchanged unless parent explicitly resolves a demonstrated integration need. Model A 0.1.0/printing IDs/order/probabilities/awards/schema are frozen. Phase 2 build remains untouched and has a verified 135-file mirror at .artifacts/baselines/phase2-accepted-29ed47956943.

Separate candidate: Builds/Phase3Windows. Default candidate saves are its own Saves directory; no automatic copy/merge/migration of the accepted player's live collection. Test saves only under .artifacts. Explain this isolated candidate before human use. No binder, physical tracking, completion target, additional sets, marketplace or finance. Machamp decision remains unanswered.

## Frozen shared media API

Parent owns Assets/VintageCollector/Media/PresentationMediaContract.cs. Namespace VintageCollector.Media:

```csharp
public interface IPresentationMedia
{
    Texture2D CardFace(string printingId);
    Texture2D HoloMask(string printingId);
    Texture2D CardBack { get; }
    Texture2D WrapperFront { get; }
    Texture2D WrapperBack { get; }
    AudioClip Sound(string committedAction);
    string StatusText { get; }
    int SuppliedAssetCount { get; }
}
```

`printingId` means the existing full stable card-printing ID (e.g. pkmn-base-en-unlimited1999-001-holo-standard), not the set printingId field or a display name. No texture/sound binding ever changes game identity. Null texture means generic placeholder fallback. Sound returns a verified supplied cue or original synthesized generic foley; it is not a real/historical recording. No automatic playback occurs inside provider methods. Caller owns resource lifetime through the concrete library's IDisposable; renderer must not dispose an externally supplied provider.

Concrete API owned by media worker:

- `public sealed class PresentationMediaLibrary : IPresentationMedia, IDisposable`
- `public static PresentationMediaLibrary Load(string projectRoot, Catalog catalog, string manifestRelativePath = "Assets/Supplied/AssetManifest.json", bool allowTestInputs = false)`.
- Read-only `Warnings` string collection; all interface members above. Missing/malformed/unsupported media yields explicit warnings + safe fallback, never loss of pack/save access. SuppliedAssetCount counts successfully loaded bindings, not invented/merely declared files. Empty manifest yields zero supplied assets and original generic cues.
- Parent host creates/binds the library after OpeningGame initialization, disposes on destroy, clears renderer provider/stops audio before destroying resources. Renderer can receive a fake provider only in explicitly labelled tests.

## Manifest v1 (existing empty manifest remains valid)

Root fields: int schemaVersion; string status, intendedUse, redistributionPermission, rightsDefault, notes; AssetEntry[] assets. Root schemaVersion 1. Entries: string role, cardId, relativePath, source, rights, restrictions, sha256. Empty `assets` is honest current production state; do not insert synthetic examples into the real supplied manifest.

Roles: `card-face`, `holo-mask`, `card-back`, `wrapper-front`, `wrapper-back`, `sound-generate`, `sound-open`, `sound-next`, `sound-skip`. Face/mask require exact supported full cardId; masks only for holo identities. Other roles require no cardId. Duplicate role/card bindings are rejected, not last-wins. Source and rights must be explicitly present; "unknown" rights is valid, not clearance or publication permission. Preserve supplied source strings in provenance, but do not expose tokenized URLs/credentials through UI/logs. No network retrieval.

relativePath is project-relative, inside Assets/Supplied, never an absolute path/URI/ADS/traversal/reparse redirect. Default manifest only under Assets/Supplied. With allowTestInputs=true, .artifacts relative manifest/media paths may be accepted; never live save paths or elsewhere. Parent permits that only for explicit smoke mode. Require exact lowercase SHA-256 before decoding; bad/missing hashes or files warn/fallback. Bound bytes/dimensions/duration before large allocations; enforce sensible whole-card/mask alignment and supported image ratio. Do not silently distort real card faces or apply a missing mask to the whole card.

PNG is the first image contract; document any additional supported formats, but do not claim an untested decoder. Suggested caps: file <=16 MiB, dimensions <=4096 each and pixel cap; card ratio close to63:88, mask matches face dimensions. WAV PCM16/24 mono/stereo with bounded sample rate/duration is preferred; explicitly document supported subset and fallback if unsupported. Tests must prove real decoding, not only RIFF-shaped bytes.

Generic foley: original deterministic synthesis independent of the game RNG. Keep peaks modest, finite samples and short clips; generate/open/next/skip distinct. No downloaded/branded sounds or music, no voice provider. Pure sample/WAV tests and actual Unity clip/source tests are separate from human audibility/quality judgment.

## Renderer changes (presentation worker ownership)

Preserve public OpeningView/OpeningCardView and commands, accepted hitbox/layout positions and visibility/order rules. Add `public IPresentationMedia Media { get; set; }`, `public AudioSource FoleySource { get; }`, `public int CommittedCueCount { get; }`, `public string LastCommittedCue { get; }` to TabletopPresentation. Counter identifies actually requested audible committed cues, not proof of physical speakers; document its exact semantics.

- Full supplied face displayed crisply without repainting printed text, border or whole-face hue. Keep NEW/DUPLICATE/UI labels outside the face or in separate existing info areas so the printed face remains unobscured. Same input hitbox whether media is present or not.
- Real-face holo effect only with explicit aligned provider mask and holo finish. Missing mask disables real-face foil overlay. Generic placeholder effect stays strictly within the known abstract-art rectangle. Keep Resources-referenced shader/material so player stripping cannot remove it; don't use full-card rainbow.
- Improve restrained shadows, highlights, wrapper crimp/tear cue, reveal/inspection motion. Animation purely consumes already committed view; no delay/gesture required for Next/Skip, no hidden names/faces, no generating or awarding. Pause/focus freezes presentation time; shader time cannot run independently while paused.
- AudioSource is 2D, playOnAwake=false, loop=false. Only NotifyCommitted of successfully acknowledged generate/open/next/skip may play action cues; invalid/uncommitted requests do not. Respect volume immediately, stop on mute/pause/error/changed provider/destruction, don't queue bursts on Skip or replay upon Resume. No normal-startup sound. Default volume0.5; no save schema/settings-file change.
- Keep click/Back/Escape/Resume/Quit/short-window controls. Update silent-placeholder copy accurately: distinguish generic original foley and fallback art from supplied/branded assets. Do not claim supplied materials when manifest is empty.
- Presentation remains passive/no filesystem access; provider methods supply resources. Existing null Media remains usable as a silent generic fallback for old component tests and isolated use.

## Parent integration / tests

Parent owns Runtime/Phase3MediaHost.cs, new Phase3 build/integration/smoke tooling and candidate source scene. Main OpeningGame stays untouched; host can use DefaultExecutionOrder after the controller to attach media. Use GamePaths validated root and only permit synthetic override manifest under Paths.ArtifactDirectory/media-manifest.json when Paths.IsSmokeTest and file actually exists. Normal launch reads the empty real manifest, showing fallback media and original foley, not synthetic assets.

Build/capture actual Phase3 player. Regress original opening/persistence/native-input/hard-kill cases with isolated saves. Prove optional replacement visibility using labelled original synthetic PNG/WAV fixtures (not fabricated supplied materials) and test valid/missing/badhash/path/identity/mask cases. Measure native clip samples/peaks, mute/no-startup/pause/error cue behavior; distinguish playback engine evidence from physical audibility. Preview examples must not reroll/award real cards or change probabilities to force a supplied face.

## Exclusive ownership / execution

- Media worker: Media/** EXCEPT parent-owned PresentationMediaContract.cs; uniquely named TestsPhase3/Media*; tools/run_phase3_media*.py; docs/technical/Phase_3_Media.md. No Presentation/Runtime/Editor/build/source settings/Git edits. Pure Mono tests allowed; native Unity only in an isolated project under .artifacts/phase3-media-project if needed, not main while other sources evolve.
- Presentation worker: Presentation/TabletopPresentation.cs, new Presentation/Phase3* helpers, Resources/Presentation/**, Editor/Phase3PresentationChecks.cs, uniquely named TestsPhase3/Presentation* and tools/run_phase3_presentation*.py; docs/technical/Phase_3_Presentation.md. No Media implementation/contract, Runtime/controller, Data/Domain/Persistence, root settings/Git edits. If native Unity tests are needed, isolate under .artifacts/phase3-presentation-project with only owned code and shared interface; do not launch main project while workers edit.
- Parent: shared interface, runtime host, candidate build/scene/native tests, final review/commits and current docs. Workers must retain real vertical RED/GREEN evidence, run their code, and not close tracked work or claim a core-connected player they did not run. All temporary projects/logs/fixtures/binaries under this root.
