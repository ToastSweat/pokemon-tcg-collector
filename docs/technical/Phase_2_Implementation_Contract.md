# Phase 2 — approved playable-slice contract

User explicitly accepted Phase 1 and authorized Phase 2 with “yes i approve”. Baseline code checkpoint cda1fc1, closing docs c349db4. Approved Model A 0.1.0 and source data must remain byte-unchanged. Machamp completion exception remains unanswered and is not resolved by this approval.

## Boundary

Only write project-controlled files under F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG. No sibling directories, profile skills, installs, remote uploads or new services. Unity-managed global logs/licensing/caches are allowed. Controlled TEMP/TMP/TMPDIR, logs, fixtures, saves and builds stay under this root. Preserve untracked ProjectSettings/PackageManagerSettings.asset; it is not part of this change. Phase 2 only; no binder editor/completion denominator, physical holdings changes, shopping, money, other sets or branded downloads. Use correctly proportioned clearly labelled placeholders.

## Observable loop

1. Ready screen offers English Base Set Unlimited only. No pack is generated automatically on ordinary startup.
2. Choosing Generate creates one immutable result, transaction ID, timestamp and nonzero seed. Persist the full result plus its virtual collection transaction before exposing the sealed wrapper.
3. Opening the wrapper persists wrapperOpened before the placeholder tear. Next reveals the next saved card; persist the absolute target reveal count before displaying it. No per-click rolls.
4. Skip persists the complete reveal count and preserves original order. Results report each card's first-time virtual status and duplicates. Physical ownership is explicitly not configured, not silently marked missing/owned.
5. Restart in any stage resumes the same transaction, card IDs, model metadata, wrapper state and reveal count. Completion/repeated input/retries never award again. New Pack is available only when the current pack is complete.
6. Mouse buttons work without dragging. Click revealed cards for inspection. Escape pauses/back; focus loss pauses, with explicit Resume. Volume supports mute. Quit never depends on a final best-effort save because actions were already persisted.
7. Recoverable disk failure blocks presentation advance and exposes Retry/Quit without overwriting data with an empty collection. Corrupt/unsupported saves never silently become a blank successful collection. Single-writer lock prevents concurrent players sharing a save.

## Pure persistence public API (namespace VintageCollector.Persistence)

The existing pure generator namespace/API is in docs/technical/Domain_API.md; do not modify it.

- public interface IStateSerializer { string Serialize(SessionData data); SessionData Deserialize(string text); }
- public interface ISaveFaultInjector { void At(string stage); } Optional constructor argument for explicit tests only. Name/document actual stages; real disk operations must run in tests.
- [Serializable] public sealed class SessionData { public int schemaVersion; public long revision; public PackRecordData[] packs; }
- [Serializable] public sealed class PackRecordData { public string transactionId, createdUtc, modelId, modelVersion, catalogVersion, printingId, rngAlgorithm, status; public uint seed; public string[] orderedCardIds; public bool[] newToVirtual; public int revealedCount; public bool wrapperOpened; }
- public sealed class SessionStore : IDisposable { public SessionStore(string directory, IStateSerializer serializer, Catalog catalog, ISaveFaultInjector faults = null); public SessionSnapshot Snapshot { get; } public SessionSnapshot Reload(); public PackSnapshot OpenPack(string transactionId, string createdUtc, uint seed, PackResult pack); public PackSnapshot OpenWrapper(string transactionId); public PackSnapshot RevealTo(string transactionId, int targetCount); public void Dispose(); }
- SessionSnapshot read-only properties: long Revision; ReadOnlyCollection<PackSnapshot> Packs; PackSnapshot CurrentPack (latest or null, including completed); ReadOnlyCollection<VirtualHolding> VirtualHoldings; int TotalVirtualCards; string RecoveryNotice.
- VirtualHolding read-only properties: string CardId; int Quantity.
- PackSnapshot read-only properties: string TransactionId, CreatedUtc, ModelId, ModelVersion, CatalogVersion, PrintingId, RngAlgorithm, Status; uint Seed; ReadOnlyCollection<string> OrderedCardIds; ReadOnlyCollection<bool> NewToVirtual; int RevealedCount; bool WrapperOpened, IsComplete.

Snapshot all mutable DTOs; callers cannot mutate authoritative state or previously returned records. OpenPack is idempotent for the same transaction and exact contents/metadata, rejecting collisions. A different transaction cannot replace an incomplete latest pack. RevealTo uses an absolute target (not an increment), is monotone/idempotent, validates bounds and requires wrapper open for positive progress. Completing never adds cards again.

Recommended accounting: durable pack archive is the source of virtual awards; aggregate each transaction once to compute holdings, including the persisted unopened pack. Persist per-position newToVirtual flags based on earlier awards and earlier same-pack positions, so duplicate Energy and replay remain honest. No physical collection writes exist. Validate schema, revision, unique transaction IDs, metadata, card identity/printing, array lengths, reveal bounds, archive ordering and new/duplicate accounting on load. Unsupported schema fails closed and preserves bytes.

Persistence worker owns exact recoverable on-disk protocol, must document it and prove it with real filesystem tests. Use flushed temporary preparation, atomic replacement and backup/recovery (or an equally justified stronger protocol), checksums and an exclusive process-released file lock. A fully persisted prepared state may roll forward after interruption; never reroll that pack. Document recovery notices and limits without claiming arbitrary power-loss protection. Handle uncertain acknowledgement after replacement by read-back/retry of the same transaction/absolute target. Never mutate runtime state to a candidate whose durable outcome is unknown, nor destroy valid recovery copies with a blank save. Original source data/model are not rewritten on load.

## Presentation contract (namespace VintageCollector.Presentation)

Presentation has no persistence/generator dependency; parent runtime controller owns commands and supplies a read-only-in-practice view DTO each update. It must not reveal hidden card faces or generate/award cards itself.

- public sealed class OpeningView { public string stage, transactionId, errorMessage, recoveryNotice, modelLabel, statusLine, saveLocation; public int revealedCount, totalVirtualCards; public OpeningCardView[] cards; public bool paused; public float volume; public int inspectIndex = -1; }
- public sealed class OpeningCardView { public string id, name, collectorNumber, rarity, kind, finish; public bool isNew; }
- public sealed class TabletopPresentation : MonoBehaviour { public OpeningView View { get; set; } public event Action<string> Command; public void NotifyCommitted(string command); }
- Stages: ready, sealed, revealing, results, error. Commands: generate, open, next, skip, retry, pause, resume, quit, inspect:<zero-based index>, close-inspect, volume:<invariant decimal 0..1>.
- Presentation handles mouse controls, Escape/back/focus-pause requests, optional action-coupled generic synthesized sounds, wrapper transition visuals and responsive layout. No branded images/logos/audio. At minimum tabletop, generic wrapper, card back/stack, readable placeholder card faces/inspection and results. Correct card ratio 63:88; any foil cue must be bounded to placeholder illustration region, not a full-card rainbow.
- Only emit sounds/positive action cues after NotifyCommitted from successful runtime action. Pause/volume/inspection are presentation-only. Default volume 0.5; 0 mutes. No audio required for correctness.
- Hide unrevealed names/art in revealing stage. Results may show all persisted complete cards; only revealed indexes are inspectable. Ready scene itself must not generate a pack.
- Design on logical 1600x900, scale by min(width/1600,height/900) and use full available viewport to avoid clipping. Make Back/Resume/Quit reachable at short/wide windows. Clearly state placeholder visuals and provisional rules.

## Parent-owned runtime integration

OpeningGame : MonoBehaviour in namespace VintageCollector.Runtime owns serialized TextAsset catalogJson and modelJson; parse with Unity JsonUtility, construct unchanged domain and SessionStore using a Unity JSON serializer adapter. Parent builds Bootstrap.unity with camera and wired assets/presentation/controller. No Resources copies of data needed.

Default Editor saves: <project>/Saves/Phase2. Default player saves: <executable directory>/Saves, with builds under this project. Never use Application.persistentDataPath or PlayerPrefs. Explicit smoke-test flags may direct saves only to project-local .artifacts fixtures. Runtime freezes pending generation/transaction across recoverable retry rather than generating a new random pack. UI changes only after durable service success; errors retain authoritative/pending identity and show blocking Retry.

## Verification and output

Keep strict vertical RED/GREEN evidence for new logic. Compile pure persistence tests separately with bundled Mono and real data; do not add Phase 2 entrypoints under Tests (Phase 1 runner recursively compiles that directory). Use TestsPhase2 instead. Test first commit/resume, all reveal progress boundaries, idempotency/duplicate inputs, per-card new flags/repeated Energy, recovery from interrupted writes, malformed/unsupported/corrupt saves, lock release and second writer, failed writes before/after durable replacement, and preservation of valid history.

Parent runs actual Unity integration/build and graphics-enabled Windows player. Opt-in smoke automation must be absent on normal startup, use isolated saves, produce assertion arrays/counts, capture rendered Ready/Sealed/Revealing/Inspection/Results/Pause/Error where exercised, and test real restart in another process. Screenshots must be inspected; semantic command calls are not physical mouse playtesting. No claim that successful compilation proves feel/performance/native interaction. Stop after Phase 2 delivery for user review.

## Exclusive ownership

- Persistence worker: Assets/VintageCollector/Persistence/**, TestsPhase2/PersistenceTests.cs (and uniquely named persistence test helpers there), tools/run_phase2_persistence.py, docs/technical/Phase_2_Persistence.md, docs/verification/Phase_2_Persistence_Verification.md. No Domain/model/data/other worker files, Unity launch, git actions or profile changes.
- Presentation worker: Assets/VintageCollector/Presentation/**, Assets/VintageCollector/Editor/Phase2PresentationChecks.cs, docs/technical/Phase_2_Presentation.md and uniquely named presentation tests/tools. No runtime controller, persistence, Domain/data, root settings, git or profile changes. If using Unity before integration, isolate its verification project under .artifacts/phase2-presentation-project and copy only owned presentation/tests plus necessary existing baseline configuration; no main-project Unity launch while core files evolve.
- Parent: Runtime/**, other Editor build/integration/smoke checks, scene, project build settings, controller/adapters, root/status docs, final verification, review and commits. Workers cannot close tracked work or claim a Windows player was tested.
