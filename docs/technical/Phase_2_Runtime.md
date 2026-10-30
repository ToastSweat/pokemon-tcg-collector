# Phase 2 runtime controller and Unity JSON integration

Implemented and verified in the main project with Unity **6000.2.14f1**, native headless Editor execution, the real `JsonUtility`, unchanged Domain generator and real filesystem-backed `SessionStore`. This is controller integration evidence, not Windows-player rendering or physical-input acceptance.

## Frozen callable API

Production namespace: `VintageCollector.Runtime`.

```csharp
public sealed class OpeningGame : UnityEngine.MonoBehaviour
{
    public UnityEngine.TextAsset catalogJson, modelJson;
    public string allowedProjectRoot;
    public GamePaths Paths { get; }
    public VintageCollector.Presentation.OpeningView CurrentView { get; }
    public VintageCollector.Persistence.SessionSnapshot CurrentSession { get; }
    public int GeneratedResultCount { get; }
    public string LastCommand { get; }
    public void Initialize(string[] args);
    public void DispatchCommand(string command);
    public void GenerateForSmoke(uint seed);
    public void FailNextCommitForSmoke();
    public void Shutdown();
}

public sealed class UnityStateSerializer : VintageCollector.Persistence.IStateSerializer
{
    public string Serialize(VintageCollector.Persistence.SessionData data);
    public VintageCollector.Persistence.SessionData Deserialize(string text);
}
```

`Paths` remains null if argument/path validation fails. `CurrentSession` is **only the last successfully acknowledged immutable snapshot**, or null if initialization never succeeded. **It is not authoritative while `CurrentView.stage == "error"`**: a prepared operation can already be durable although this property still describes the prior acknowledgement. Error handling never reads an uncertain `store.Snapshot` to fabricate success. The error view has no card faces; its transaction ID can identify the pending operation without claiming it was acknowledged.

`GeneratedResultCount` increments immediately before each actual call to `PackGenerator.Generate`, not when a generation command is merely requested, rejected, recovered or retried. It is per component, not a persisted statistic. `LastCommand` is the last accepted command, including accepted failed writes and `retry`, presentation-only commands and `quit`. Rejected commands do not change it. Arming a smoke fault is not a command.

## Initialization and integration

- Wire the two existing JSON TextAssets and an absolute approved project root in Bootstrap. There are no Resources copies, generated model replacements or new dependencies.
- `Initialize(args)` attaches a local `TabletopPresentation` if absent, subscribes its `Command` event, freezes command-line arguments/root/source JSON, resolves `GamePaths`, parses with real Unity `JsonUtility`, constructs the unchanged `Catalog`/`PackGenerator`, opens `SessionStore` with `UnityStateSerializer` and projects the saved snapshot.
- Initialization does **not** generate a result or write a blank successful session. The persistence service may create the isolated directory/lock, and may recover an existing durable state.
- Repeated `Initialize` and a subsequent `Start` on an initialized instance cannot reopen/reset the session. If not initialized manually, `Start` uses `Environment.GetCommandLineArgs()` once. No startup smoke automation is included.
- Default save selection is exclusively the parent-owned `GamePaths.Resolve(allowedProjectRoot, Application.dataPath, Application.isEditor, args)`: Editor `<project>/Saves/Phase2`, player `<executable directory>/Saves`. No AppData, `Application.persistentDataPath`, PlayerPrefs or global override is used. This implementation does not modify `GamePaths.cs`.
- `Shutdown` is terminal for that component and idempotent. It unsubscribes commands and disposes the store without a final save. Resume/reinitialization uses a fresh component and the same save directory; tests close/reopen at revealed count **3**. `OnDestroy` and `OnApplicationQuit` also release the lock.
- Accepted `quit` closes the store and calls `Application.Quit()` only outside the Editor. Calling it in the explicit Editor checks never exits the Editor before the test report is written.

The controller is a synchronous main-thread owner, matching `SessionStore`'s single-caller contract. View DTOs are read-only-in-practice projections for the passive presentation, not a public mutation authority.

## Commands, durability and presentation

All commands first pass `PresentationRules.CanEmit`. Unknown commands, wrong-stage commands, hidden/invalid inspect indexes, invalid invariant-decimal volumes, duplicate Open/finished progress, generation before completion and actions blocked by pause/inspection are rejected.

Normal `generate` creates a cryptographic nonzero UInt32 seed using `RandomNumberGenerator`, one GUID transaction ID (`N` format), one UTC round-trip timestamp and one immutable generator result. The exact result and all arguments become a pending action **before** `SessionStore.OpenPack` is called. Wrapper actions cache the transaction ID; Next/Skip cache that same transaction ID and an **absolute target count**, before disk work. Next never rolls cards, and Retry never increments an old target.

Only successful durable operations followed by a successful service snapshot update `CurrentSession`, advance the supplied view and call `NotifyCommitted(originalCommand)`. The wrapper tear therefore starts only after acknowledged Open, including a successful exact Open retry. New/duplicate flags, original order, model metadata, wrapper/reveal progress, results and total virtual cards all come from saved snapshots. No per-reveal or completion awards are added. Physical tracking is always labelled **not configured**.

Pause/Resume, inspection/Back and volume/mute are in-memory presentation state only. Default volume is `0.5`; valid range is `0..1` with invariant decimal syntax. These commands never save, generate or award; they survive durable view refreshes during that component's lifetime. Focus loss is handled by the real passive presentation's pause request; focus gain never resumes automatically. No audio component or playback is added.

## Failure and Retry

`InvalidDataException` is caught **explicitly**, separately from `IOException`, because in the tested Unity Mono it does not derive from `IOException`. I/O, unauthorized-access, security, configuration/argument and invalid-operation failures are also surfaced as blocking error views.

- Failed operations retain the exact pending action and the last acknowledged `CurrentSession`. Repeated Generate/Open/Next/Skip are blocked in error; Retry does not create a new result, seed, ID, timestamp or reveal target.
- Retry opens the original frozen save configuration if initialization failed, otherwise reloads/recovers the existing store. That entire recovery path is inside the error handler too: real sharing denial and corrupt prepared data on Reload are exercised.
- A recovered snapshot is **not** exposed early while a pending action still needs acknowledgement. The controller repeats that exact original operation, takes its successful authoritative snapshot, then publishes it. A prepared action recovered by the service is idempotent on retry.
- Initialization-only failures keep `CurrentSession == null`; they do not fall back to a blank session. Retry reopens the same save, preserving corrupt/unsupported bytes while unresolved. No save deletion, blank replacement or manual-repair automation exists.
- Quit remains available even with a pending failed operation. Closing the writer lock does not discard a durable prepared state; a fresh instance can recover it.

The controller inherits the persistence protocol's documented local crash-recovery limits; it makes no arbitrary power-loss or hostile-file-edit guarantee.

## Explicit smoke API — no automation

Both smoke APIs require `Paths.IsSmokeTest`; calls outside explicit smoke mode throw `InvalidOperationException`. `GenerateForSmoke(0)` throws `ArgumentOutOfRangeException` before invoking the generator. Other supplied nonzero seeds use the same pending-action/durable path as ordinary Generate and the same command gates.

`FailNextCommitForSmoke()` arms one optional injector belonging to that smoke controller. It throws a one-shot **IOException at the actual `SessionStore` `after-prepare` hook** on the next write reaching that hook, then disarms before throwing. Invalid/rejected commands do not consume it. Normal mode constructs/passes no optional fault injector. Recovery itself does not call the hook. Calling Retry after the prepared generation yields the same transaction, timestamp, seed and ordered IDs, exactly one pack/eleven virtual awards and `GeneratedResultCount == 1`.

Parent smoke/build automation is outside these files; no player driver, screenshot capture, scene/build helper or player launch was added here.

## Verification and evidence

Explicit main-project entrypoint:

```text
-executeMethod Phase2RuntimeChecks.Run
```

The static Editor method has no startup hook, does not enter Play mode, writes named JSON results under `.artifacts/phase2-runtime`, and exits **0 only when all nonempty test groups pass**. Failures exit 1. `VCC_RUNTIME_LABEL` selects a filename (default `checks.json`). Reports contain count-matched `checks` and `assertions` arrays with unique names; counts are reconciled externally, not inferred from exit alone.

All storage tests use explicit smoke arguments and unique fixture save directories under `.artifacts/phase2-runtime/fixtures`. No test deletes or writes live saves. The normal-mode test resolves the real default and initializes with a deliberately invalid in-memory model asset, stopping before `SessionStore` can touch normal savedata; it proves default path selection, no normal-mode injector and rejection of both smoke APIs. This guarded test is not a claim that a normal live save was opened during testing.

Observed vertical RED/GREEN checkpoints, retained in `.artifacts/phase2-runtime`:

| Checkpoint | Observed RED | Observed GREEN |
|---|---|---|
| `01-serializer-*` | UnityStateSerializer missing, exit 1 | real JsonUtility roundtrip, 1 group, exit 0 |
| `02-startup-*` | OpeningGame missing, exit 1 | passive startup/lifecycle, 2 groups, exit 0 |
| `03-loop-*` | GenerateForSmoke/DispatchCommand missing, exit 1 | durable full loop and count-3 reopen, 4 groups, exit 0 |
| `04-recovery-*` | missing hook and uncaught real corrupt-save/lock exceptions, exit 1 | prepared identity/absolute-target Retry and init recovery, 8 groups, exit 0 |
| `05-presentation-state-*` | Pause and Quit not handled, exit 1 | validated passive settings and safe Quit, 11 groups, exit 0 |
| `06-full-integration` | supplemental regression coverage | **13 groups / 297 named assertions**, exit 0 |
| `07-delivery` | unchanged-source rerun | **13 groups / 297 named assertions**, exit 0; all names unique and both array counts reconciled |
| `08-delivery-start-covered` | final supplemental automatic-Start branch coverage | **13 groups / 297 named assertions**, exit 0; real process arguments used; counts/names reconciled |

`08-delivery-start-covered.json` contains the final named results. `08-delivery-start-covered-execution.json` records the actual native command, process exit code, reconciled counts and SHA-256 of the three tested source files; `08-delivery-start-covered-unity.log` is the native Editor log.

The final regression adds all eleven Next boundaries, original result order, immutable earlier snapshots, completed restart, actual pre-prepare file sharing failure, exact pending arguments before preparation, immutable initially parsed model version after replacing the serialized asset, a two-pack archive reopen and persistent duplicate flags. Prepared-generation tests also deny recovery reads with a real `FileShare.None` stream and corrupt/restore only their own fixture pending bytes, proving repeated blocked Retry catches both I/O and Mono InvalidDataException without rerolling. Wrapper cue tests verify no positive cue on unacknowledged failure and a cue after successful Retry.

Unity logs show a licensing access-token refresh diagnostic followed by successful entitlement resolution; the final integration run has no compiler errors or unhandled application exceptions. Expected storage failures are caught by the controller and checked as error states.

Reproduce from Bash with **native forward-slash paths**, not MSYS `/f` arguments to Unity:

```bash
ROOT='F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
mkdir -p "$ROOT/.artifacts/phase2-runtime/tmp"
export TEMP="$ROOT/.artifacts/phase2-runtime/tmp"
export TMP="$TEMP" TMPDIR="$TEMP" VCC_RUNTIME_LABEL=rerun
'C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe' \
  -batchmode -nographics -projectPath "$ROOT" \
  -executeMethod Phase2RuntimeChecks.Run \
  -logFile "$ROOT/.artifacts/phase2-runtime/rerun-unity.log"
```

Do not run another Editor concurrently against this project. Test fixtures and evidence are retained rather than deleting possible live data.

## Files and remaining acceptance

Owned source changes are only `Runtime/OpeningGame.cs`, `Runtime/UnityStateSerializer.cs`, `Editor/Phase2RuntimeChecks.cs` under `Assets/VintageCollector`, plus this document. No original Tests entrypoint, Domain/Data/Model A, Persistence, Presentation, GamePaths, Bootstrap, build/smoke driver, package settings, profile skills or Git state is changed by this work. Unity creates ordinary metadata for newly imported scripts.

Still parent-owned/unestablished here: production Bootstrap/build wiring, graphics-enabled Windows player/build, separate-process player restart and smoke captures, physical mouse/keyboard/focus playtesting, human opening feel, sustained performance and any audio/art acceptance. These Editor assertions do not establish those. Phase 2 only; Machamp completion policy remains unanswered and no Phase 3/binder/physical/market/finance behavior is added.
