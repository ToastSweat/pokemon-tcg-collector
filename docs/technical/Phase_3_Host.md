# Phase 3A — Optional main-runtime media host

## Delivered boundary

`Phase3MediaHost` is a complete optional resource owner for the actual `OpeningGame` / `TabletopPresentation` runtime. It does not change controller logic, generation, awards, probabilities, catalog/model data, save schema, persistence, paths, accepted player or live saves. Production supplied assets remain **empty**. Synthetic PNG/WAV fixtures are explicitly labelled original test fixtures, not actual supplied/branded assets.

This is placeholder-first Phase 3A integration evidence, not full Phase 3 acceptance. Candidate scene attachment/build/player acceptance remains parent-owned. No player was launched, no tracked work closed, and no Git/index mutation or package installation was performed.

## Frozen public API

```csharp
namespace VintageCollector.Runtime
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class Phase3MediaHost : UnityEngine.MonoBehaviour
    {
        public VintageCollector.Media.PresentationMediaLibrary Library { get; }
        public bool UsesTestManifest { get; }
        public string StatusMessage { get; }
        public void Initialize(OpeningGame game);
        public void Shutdown();
    }
}
```

Attach this component to the same GameObject as the configured `OpeningGame`; no component is silently attached by this host. `Start` finds the controller on that object. Execution order 100 places it after the controller's ordinary initialization. It requires non-null validated `Paths`, an acknowledged `CurrentSession`, a usable non-error `CurrentView`, and the configured read-only catalog TextAsset. It never initializes the game, calls its commands, generates, accesses its store or awards cards.

`Initialize` reparses that same catalog TextAsset using real `JsonUtility.FromJson<CatalogData>` and `Catalog.FromData`, so media identity validation uses the actual supported full stable IDs. Pure `GamePaths.Resolve` revalidation checks the configured root against actual Application.dataPath and the existing path policy. Remote/device roots are lexically rejected and mapped network drives rejected before optional manifest existence probing. There are no networking APIs or media downloads.

If startup is initially blocked by storage, `Update` only observes the controller until it becomes usable through its own existing Retry. It then attaches silently. It never performs that Retry or publishes a view. Once a load is attempted, it does not poll files, repeatedly retry an optional load failure or replace a subsequently installed external provider. Repeated Initialize/Start are idempotent.

### Manifest selection

- Normal mode always calls `PresentationMediaLibrary.Load(root, catalog, "Assets/Supplied/AssetManifest.json", false)`.
- Only existing `Paths.IsSmokeTest` authorizes looking for the exact `Paths.ArtifactDirectory/media-manifest.json`.
- The candidate must actually exist and be inside that validated project's `.artifacts` subtree. Only then is its project-relative forward-slash path passed to `Load(..., allowTestInputs: true)`.
- Missing smoke override uses the unchanged normal manifest and `false`; normal mode cannot select an override from command-line residue, environment variables or a nearby synthetic file.
- The media library independently validates manifest/content paths, actual opened handles, hashes, identity, masks and native decoding. Optional manifest/entry failures retain its warnings, independent good bindings and original generic foley where valid. No synthetic production assets are invented.

`UsesTestManifest` reports selection of that explicit test manifest, not that its entries decoded successfully. `StatusMessage` consists only of bounded host text and the library's safe status summary; it never incorporates provenance, paths, source URLs or exception text. Library warnings remain available through `Library.Warnings`. A failed prerequisite or unexpected optional setup error does not alter gameplay and cannot reroll.

### Lifetime

The host owns exactly its created library. Binding assigns `TabletopPresentation.Media` and does not request a source, call `Sound` for playback or acknowledge any action. Only the unchanged real controller's post-save `NotifyCommitted` seam can request cues.

`Shutdown` is **terminal**, including after manual Initialize/Start/Update attempts. If the renderer still references the owned library, it first sets Media to null; that setter stops/clears the old AudioSource **before** the library destroys its clips/textures. The library is then disposed and the property cleared. Calls are idempotent. `OnDestroy` and `OnApplicationQuit` use the same sequence. If another provider was installed externally, Shutdown neither clears nor disposes it. The host does not shut down the game or delete any file. Consumers must not directly dispose the host-owned Library while it is bound.

## Real Unity verification

Reproduce from MSYS Bash with a **fresh label** (old evidence is not overwritten):

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python -B TestsPhase3/HostRun.py --label new-native-integration --slice all
python -B TestsPhase3/HostRun.py --label new-native-play --slice play
python -B TestsPhase3/HostAudit.py
```

`HostAudit.py` reconciles the retained named final reports and RED/GREEN pairs; the first two commands can use fresh arbitrary labels for additional runs. They call the installed editor:

```text
C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe
-batchmode -nographics
-projectPath F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/.artifacts/phase3-host/native-project
-logFile <project>/.artifacts/phase3-host/<label>/unity.log
-executeMethod VintageCollector.EditorChecks.Phase3MediaHostChecks.Run
```

All fixture controllers initialize with the existing explicit scoped arguments:

```text
--vcc-smoke=ready
--vcc-save-root=<native-project>/.artifacts/phase3-host/fixtures-<GUID>/synthetic-<GUID>/saves
--vcc-artifacts=<native-project>/.artifacts/phase3-host/fixtures-<GUID>/synthetic-<GUID>
```

The original interactive main-project Editor was already open. The first batch attempt was correctly refused by Unity's project lock; it was not killed or closed. Native tests therefore run in the project-local exact-source stage above. This is not a mock core: source/staged SHA-256 equality is verified for every staged C#/shader/JSON input, including actual OpeningGame, GamePaths, Domain, Data, Persistence, UnityStateSerializer, renderer, media library and empty production manifest. Built-in modules only; no downloaded test packages. Controlled TMP/TEMP/TMPDIR, reports, fixtures, logs and the stage are under `.artifacts/phase3-host`.

### Final results

| Report | Native mode | Named assertions | Result |
|---|---|---:|---|
| `.artifacts/phase3-host/final-native-integration/results.json` | Real Unity Editor, actual controller/serializer/store/media | 87 | exit 0, all passed |
| `.artifacts/phase3-host/final-native-play/results.json` | Real Unity Editor **Play Mode**, automatic Start/Update and actual deferred destruction | 18 | exit 0, all passed |

Both nonempty reports' declared counts equal their enumerated checks: **105 final assertions**. Each retains `command.json`, `execution.log`, `unity.log`, `source-pins.json`, `staged-inputs.json` and `protected-inputs.json`. `.artifacts/phase3-host/evidence-index.json` reconciles actual exits, counts, failed assertions, report/log hashes, source pins and four vertical RED/GREEN pairs. No made-up engine output is substituted.

Verified behaviors:

- Default real empty manifest: zero supplied bindings, zero startup AudioSource/cues, four real native original generic clips. AudioClip.GetData readbacks are finite, nonzero and modest. Positive synthetic override: real PNG pixels, full stable-ID lookup, aligned mask, actual decoded PCM16 WAV/GetData and no startup playback.
- Real renderer event -> controller Generate -> SessionStore commit -> published view -> eligible cue. Actual Open/Next/Skip acknowledge once, duplicate/rejected acknowledgements cannot replay, and Skip is one cue rather than a per-card burst. Generic clips remain available after optional bad path/hash/ID/mask/malformed-manifest failures, with real playable pack flow and no reroll.
- Normal guard uses a pure real normal `GamePaths` object temporarily on an already smoke-initialized controller; no normal SessionStore is opened. The existing synthetic override is ignored. Original paths are restored before cleanup. Missing/null/uninitialized and foreign-controller guards never initialize the game or load optional media.
- Actual durable after-prepare failures for generation, wrapper, Next and Skip remain silent before acknowledgement. A real prepared-file sharing denial keeps Retry silent. Successful Retry retains exact prepared identity/seed/order/absolute target, invokes generation once, awards eleven once and cues at most one original action. Additional Retry/Notify cannot replay.
- Fresh controller resume reopens the actual serializer/store with exact saved history/revision/order and never generates, reawards or starts audio. Media/settings leave owned fixture save bytes unchanged.
- Play Mode native AudioSource.isPlaying is positively true after real generate/open/next/skip commits, and false with cleared clip after pause, mute, prepared-write error and actual component destruction. Resume/unmute never replay suppressed acknowledgements. Native deferred OnDestroy clears/stops before deferred owned clips disappear.
- Actual automatic host Start binds to the initialized same-GO controller. Initial writer-lock failure loads no library; after the real owner releases its lock and the controller's own Retry succeeds, host Update automatically binds without sound or reaward.
- Shutdown is terminal/idempotent, destroys only owned resources, and leaves an external real provider usable. Editor callback-boundary checks and actual Play Mode lifecycle checks are explicitly distinguished.

**160 protected source/data/manifest/settings/accepted-player files** were hash-identical before/after both final native runs, including the pre-existing untracked PackageManagerSettings.asset. Live save directories, including accepted Phase 2 Saves, were deliberately **excluded from reads/hashes/staging**; tests never read, write, copy, repair or replace them. No stronger claim about live-save contents is inferred from an excluded hash.

### Retained development evidence and diagnostics

Four real vertical pairs: `01-binding-red-native` -> `01-binding-green`; `02-override-red` -> `02-override-green`; `03-lifecycle-red` -> `03-lifecycle-green-native-callback`; `06-initial-recovery-red` -> `06-initial-recovery-green`. Every RED has a nonzero exit and a named expected failed assertion; every paired GREEN has exit 0 and a nonempty count-matched report. The initial main-project-lock attempt has no assertion report and is **not** counted as RED.

Two intermediate fixture issues were retained honestly: `03-lifecycle-green` assumed normal OnDestroy dispatch in nonplaying edit mode (corrected to explicit callback boundary, then independently proved using actual Play Mode destruction); `04-native-integration` used corrupt primary bytes that valid backup recovery legitimately repaired (corrected to an unsupported-version fixture that blocks storage, without changing persistence). Their failures are not claimed as production regressions or extra TDD pairs.

The final Unity logs retain the non-blocking licensing access-token-update diagnostic emitted by the editor; both actual suites still returned exit 0 with full reports. No test claims physical hearing, speaker output, human audio/visual quality, real branded asset readiness, candidate scene attachment or candidate player/build acceptance.

## Source ownership / handoff

Created only these source deliverables (plus generated project-local artifacts):

- `Assets/VintageCollector/Runtime/Phase3MediaHost.cs`
- `Assets/VintageCollector/Editor/Phase3MediaHostChecks.cs`
- `TestsPhase3/HostRun.py`
- `TestsPhase3/HostAudit.py`
- `docs/technical/Phase_3_Host.md`

Current source SHA-256 values are recorded in `evidence-index.json`; final compiled-input pins are in each final report directory. Parent still needs to attach the host in its separate candidate source scene and run the separate Phase3 player/build gates. Do not merge accepted live saves into candidate saves or treat synthetic evidence as real-material acceptance.

This document records the reusable exact-source-stage/native-Play-Mode workflow locally because this worker's write scope explicitly forbids profile skill changes.
