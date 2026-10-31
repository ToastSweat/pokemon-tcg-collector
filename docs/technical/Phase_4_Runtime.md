# Phase 4 runtime integration

## Scope / integration API

`VintageCollector.Runtime.Phase4CollectionHost` is a synchronous, single-threaded runtime owner, marked `[DefaultExecutionOrder(-200)]` and `[DisallowMultipleComponent]`. Attach it beside the configured `OpeningGame` in the Phase 4 bootstrap. It starts before `OpeningGame.Start`, resolves the **base** `GamePaths`, acquires `CollectionProfiles`, calls `OpeningGame.InitializeForProfile(args, Profiles.ActiveProfileId)`, opens the actual `PhysicalStore` using `UnityPhysicalSerializer`, and attaches `BinderPresentation`.

Frozen public API is implemented unchanged:

```csharp
OpeningGame Game { get; }
PhysicalSnapshot Physical { get; }                 // last acknowledged, detached snapshot
BinderView CurrentBinderView { get; }
BinderView View { get; }                           // alias
CollectionProfiles Profiles { get; }
bool IsReady { get; }
string StatusMessage { get; }
void Initialize(string[] args);
void DispatchBinderCommand(string command);
void Shutdown();
```

Startup does **not** generate, save manual holdings, export a backup, or play a cue. Pointer/store/catalog failures release all acquired leases and call the narrow terminal `OpeningGame.BlockInitialization(error)` seam. Neither default `Initialize` nor storage `retry` can initialize another collection after that terminal failure. The displayed error is sanitized. Quit/reopen after resolving storage is the recovery path; there is no blank fallback.

`OpeningGame.Initialize(args)` still means the default profile and remains compatible with earlier phases. New `initialProfileId` is used only through `GamePaths.Resolve(...).WithProfile(initialProfileId)` when opening the session. `GamePaths` itself is parent-owned and was not edited by this worker.

Successful host startup subscribes to `OpeningGame.ShuttingDown`. Both direct Game shutdown and host Quit release **physical store, session store, container lease**, in that order. Idempotent shutdown removes subscriptions; imported profiles never become additional concurrent live stores. Core import validation opens and disposes its own destination stores internally.

## Presentation-only binder gate

OpeningGame's collection-command event routes `binder-open` / `binder-close` to the host before the ordinary table policy. The accepted table has a new dedicated `Binder [Tab]` button in unused footer space, plus Tab while closed. Binder's existing Tab focus navigation and Escape/Back remain unchanged while open.

The gate never changes the game's prior pause, inspection index, volume, or `finalTableTransaction`. Opening and closing an ordinary fully revealed card11 table retains the explicit View pack results gate. Skip, generation, reveal, once-only awards, and normal committed cues keep the original persistence path.

`TabletopPresentation.CollectionOverlayVisible` returns before the table's OnGUI; `CollectionInputBlocked` gates command requests and animation/audio progression. `OpeningGame.SetCollectionGate` independently fences programmatic table requests and `GenerateForSmoke`, not just IMGUI hitboxes. Pending ownership saves, reviewed/uncertain imports, and restart-required state remain fenced after Back. Reopening exposes the retained retry; Quit stays reachable. Existing `PresentationRules` layout/hit rectangles were not edited.

## Independent physical results labels

The host supplies a detached positive-physical-ID reader to `TabletopPresentation.PhysicalOwnershipReader`. Only explicit `results` renders `PHYSICAL OWNED` / `PHYSICAL MISSING` **below** each already revealed card. Names on those cards identify the missing printings; no shopping, inferred ownership, per-reveal advertising, or extra writes exist. Normal card11 hold, inspection, sealed/reveal stages and older phases have no such badges. Existing NEW/duplicate face labels and result card hitboxes are unchanged. The explanatory result footer moves below the new badges only when the reader is configured.

`PhysicalResultLabel(index)` is the same guarded label method consumed by native DrawResults. Native tests bind it to a real generated/revealed pack and a real manually persisted physical edit; the candidate worker still owns actual framebuffer/click acceptance. `OpeningGame.PhysicalTrackingConfigured` opts into accurate status text and sanitized runtime storage errors; its default false preserves older-phase presentation.

## Commands / service trust boundary

The host keeps private mode/filter/search/page/selection/draft state; mutable UI DTOs are never service authority. It validates exact mode/filter/condition strings, canonical bounded page/backup indices, and exact full IDs against its current safe projection. Quantity drafts are text only; Confirm requires canonical integer `0..99999`. Physical selection can precede any virtual pull. All 101 eligible IDs are selectable, #8/Machamp is absent, unique completion is independent of quantity, and virtual rows come from the service's revealed-prefix projection only.

A physical edit freezes GUID-N transaction ID, UTC timestamp, full printing, condition and **absolute** quantity before calling the store. After any failed acknowledgement the exact object is retained. All draft/context/export/import changes are rejected until `save-physical` acknowledges that same edit, or the process quits. Cached physical snapshots are not read from the uncertain store during error publication.

`BinderView.pendingPhysicalEdit` and `pendingImportConfirm` are the only passive DTO additions. Binder policy enables the existing corresponding command despite an error, disables every competing context action/cancel, and labels it `Retry SAME ownership save` or `Retry SAME reviewed restore`. Back/Quit remain available. These fields do not implement transactions; the host and real services do.

Failed preview retains quantity/condition/card/backup selection. A successful preview retains the exact immutable service-owned object privately. Confirmation never reparses `importSummary`, source-name text, or a mutable selected index. Failed/uncertain confirmation retains the same preview even if the active pointer was already published. Retry reconciles the same receipt; it cannot create another profile or cancel uncertainty. Success clears review and publishes restart-required, without switching or mutating the old live profile. All underlying state actions stay locked until Quit/reopen.

The host publishes a new BinderView after an actual command/state/draft change. Update compares `OpeningGame.CurrentView` by reference and does **not** blindly assign an unchanged view each frame, which would release the passive UI's local request latch without an acknowledgement.

## Export acknowledgement barrier (TG-05)

Backup is explicit, synchronous, and requires no pending edit/import, no error, and acknowledged session/physical snapshots. Immediately before **every** CreateBackup (including the first export of the default profile), the host calls the narrow `OpeningGame.ReconcileForCollectionExport()` and `physicalStore.Reload()` against those actual live stores and their known acknowledgements. Missing previously acknowledged authoritative files cannot be reinterpreted as a new blank store merely because the profile service has not exported before.

The session barrier never executes a pending action, generates, reveals, or notifies committed audio. It preserves pause, inspection, final-table transaction, and original pack identity. Reconciliation occurs only on explicit backup, not every frame or on binder-open. Failure publishes a sanitized error and produces no ZIP or synthesized blank state. Retained TG-05 native fixtures include the deleted authoritative bytes outside the synthetic save directory for evidence and verify surviving unrelated evidence is unchanged.

`Phase3MediaHost` reconstructs smoke arguments from `BaseSaveDirectory`, then revalidates `WithProfile(CollectionProfileId)` before comparing actual selected save/artifact paths. Native restored-profile tests exercise a real media-library initialization with this path policy.

## Native JsonUtility finding and adapter hardening

Unity's actual native JsonUtility was observed accepting `quantity:4294967297` as integer 1 and coercing exponent/decimal integer values. A permissive managed JSON test serializer does not expose this behavior. Raw observations are retained in `native-json-observations.json` per expanded run.

The owned `UnityPhysicalSerializer.Deserialize` now invokes the core strict physical JSON shape gate, then verifies a lossless canonical numeric-token round trip through **actual JsonUtility**. Tokens inside quoted/escaped strings are ignored; sorting permits legal property reordering. Native integer wrapping, long overflow, decimal/exponent coercion, missing/duplicate/unknown fields, and leading-zero forms are rejected before physical semantics are applied by the store. The adapter does not change schemas or infer holdings.

**Historical native finding, now resolved:** the retained worker run accepted overflowing schema/seed/reveal tokens. Typed range checks were added in the core inspector before native deserialization. Parent final frozen06 passes85/85 with matching exact input pins; independent corrected-core review also passed. The old77/80 report remains a truthful failure, not a current blocker.

## Reproduction / evidence

From the allowed root, use a **fresh** label:

```bash
TMPDIR="$PWD/.artifacts/phase4-runtime" TMP="$PWD/.artifacts/phase4-runtime" TEMP="$PWD/.artifacts/phase4-runtime" PYTHONDONTWRITEBYTECODE=1 \
  python tools/run_phase4_runtime_native.py --label review-runtime-native
```

The runner uses only the installed Unity 6000.2.14f1, built-in modules and explicit source lists. No network/install/package acquisition or original Editor control is needed. The isolated project is `.artifacts/phase4-runtime/project`; all fixtures, logs, controlled TMP, retained bytes and reports are under `.artifacts/phase4-runtime`. Do not run two instances of this runner concurrently; the candidate build worker uses a different isolated project. Runtime scenario sources are deliberately outside Assets and discovered by reflection only in the isolated Editor checks, so ordinary project/candidate Editor compilation does not require those test scripts.

Each run retains exact Unity command/exit in `execution.log`, actual `results.json`, input SHA-256 `source-pins.json`, and Unity log. Later runs also retain `source-verification.json`; a source changed during execution makes the runner fail rather than claiming current-source verification. Test data is loaded from the pinned staged Assets/Data, not a mutable external fixture copy.

Actual retained vertical checkpoints:

| Run under `.artifacts/phase4-runtime/` | Actual checks / result |
| --- | --- |
| `s1-api-red` -> `s1-api-green` | 4 checks, 3 API failures -> all pass. |
| `s2-retry-ui-red` -> `s2-retry-ui-green` | 6 checks, 2 unreachable-retry failures -> all pass. |
| `s3-host-results-red` -> `s3-host-results-green` | 47 checks, missing results-label API -> all pass. |
| `s4-native-error-red` -> `s4-native-error-green` | 51 checks, real session IO path leak -> all pass. |
| `s5-native-numbers-red` | 73 checks, 12 failures exposing real native coercion (including accepted disk physical / schema1 archive input). |
| `s5-physical-fixed-session-blocker` | 73 checks, physical adapter/store cases pass; 3 schema1 archive alias failures remain. |
| `s6-expanded-native-current` | 77 checks, same 3 cross-owner schema1 alias failures. |
| `s7-tg05-red` -> `s7-tg05-host-fixed` | 79 checks, 2 TG-05 blank-export failures fixed; same 3 schema1 alias failures remain. |
| `s8-runtime-handoff` | **80 checks, 77 passed, 3 failed, process exit 1.** Adds the successful final-table/cue-preserving explicit-export control and stages the subsequently updated bounded-preview CollectionProfiles plus the parent SaveProtocol guard. Still **not an all-green final report** or a final core-source freeze. |

Native scenarios exercise real host methods, actual schema1 SessionStore and PhysicalStore on disk, real serializers, generated packs/reveal prefixes, final card11/Skip, paused/inspection preservation, all 101 manual selections, exact quantity/condition validation, leases/shutdown/reopen, physical lost acknowledgement, source replacement/retry, read-only preview/cancel, normal and legacy single-save wrapped archives, new-profile activation/resume, original-profile byte preservation, restored media policy, terminal startup guards, first-export loss detection and results physical labels. These are not synthetic DTO-only host claims. Every declared check count is verified against the enumerated JSON array by the runner.

## Parent review corrections (after worker handoff)

Independent runtime review retained at `.artifacts/phase4-runtime-review/review.json` rejected a real request-latch bug. Parent reproduced it through the renderer's actual private request path, not by bypassing its latch: `parent-latch-red-01` had83checks/3fail; expanded `parent-review-cancel-red-02` had85checks/4fail. Backup/preview/confirm rejections now publish a nonbusy explanatory view so a rejected request cannot remain REQUEST PENDING. Cancel calls the core's exact `DiscardPreview`; an unresolved confirmation remains uncancellable. Parent added an actual reviewed-A/mutated-summary-index-list-B oracle and name/collector-number hidden-holo searches plus per-printing revealed-prefix checks.

`parent-review-latch-green-03`: **85/85 native checks pass, exit0**, with exact source verification. This includes the formerly failing native schema/seed/reveal overflow cases against the then-current corrected core, not a claim that the historical77/80 report passed.

The runner now pins its own bytes and ProjectVersion configuration and rejects unexpected staged Editor scripts without executing/deleting them. Two real project-local Python tests observed missing-feature RED, then GREEN; logs retained at `.artifacts/phase4-runtime/runner-provenance-{red,green}.log`. Native `parent-runtime-provenance-green-04` and `parent-runtime-frozen-05` have85/85 checks pass but exit3. Parent inspected the complete pin differences: there were no changed content hashes; the two new provenance keys used forward slashes while the final Windows keys used backslashes. This was a runner key-format bug, not verified core source drift. Three runner tests now include canonical key format (actual RED→GREEN), and all current/staged keys use `.as_posix()`. Final **`parent-runtime-frozen-06`:85/85pass, exit0, currentMatchesStaged=true**, including the runner/configuration pins. The earlier drift interpretation is corrected here; failed runs remain retained.

## Remaining gates / no phase-completion claim

1. Corrected core and native runtime are verified and independently re-reviewed. `DiscardPreview` is integrated on explicit cancel, while uncertain confirmation retains its exact preview. The final85-check report pins the corrected current inputs.
2. Candidate build/player worker owns actual accepted-GUI clicks, framebuffer/result-badge inspection, whole-process Quit/reopen, and candidate manifest. The runtime native helper assertions are not graphical acceptance or a human test.
3. Parent must run final cross-owner regression/review, reconcile all final input pins, and obtain the user gate. No tracked task was closed, no live save was read/written, no earlier player directory or scene/build setting was modified here, and no Phase 5 work was started.
