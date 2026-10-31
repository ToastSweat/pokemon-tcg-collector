# Phase 5 runtime integration — source-verified discovery and proposed frozen contract

## Status and scope

**Design only. No Phase 5 runtime, scene or player is implemented by this document.** This is the proposed **FROZEN-RC1** contract for three coding streams; the parent must adopt the decisions in §10 before dispatching conflicting edits. It supports the active approved-model proof work, not a substitute approval checkpoint. No further blanket user approval for the already approved provisional models or temporary wrappers is needed.

Root: `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG`. Discovery read production source, bounded authoring inputs and documentation only; it did not open a store, run Unity, inspect live Saves/Logs, traverse old players, modify implementation, stage changes or commit. All generated discovery evidence is in `.artifacts/phase5-runtime-discovery/`.

Authority: [original handoff](../hermes/Hermes_Vintage_Card_Collector_Handoff.md), especially lines 93–119, 129–137 and 214–220; [explicit model approval](../verification/Phase_5_Model_Approval.json), lines 6–63. The earlier [discovery](../hermes/Phase_5_Discovery.md):7,30 and [data preparation](Phase_5_Data_Preparation.md):5 predate that approval; their “not approved” language is historical, not a new veto. Existing accepted Base presentation/audio, hover, materials and hitboxes remain the reference, not redesign candidates.

### Important observed data boundary

The actual frozen candidate directory is `Authoring/Phase5/Data/`, containing 64 Jungle and 62 Fossil cards. A literal `Authoring/Phase5/Data64J62F/` directory was not found. The candidates still say `proposed-not-approved`, with versions `jungle-en-unlimited1999-candidate-v0.1` and `phase5-fossil-candidate-2026-10-31-v1`. **Do not normalize these in place or admit their tokens as canonical.** The separate model/data worker owns `Authoring/Phase5/ApprovedData`, its model tools and `TestsPhase5/Model*`; this discovery did not read or poll its output. Consume its explicit handoff later. Canonical new catalog versions are `{printingId}-0.1.0`, not either candidate token.

The approval's source-weight SHA-256 was independently recomputed as `7ceb3d2b4a429c927301e77be965efcb506d41228a0324bc2ff43e3d012713ef`. The supplement's own old `UNAPPROVED` status at `docs/research/phase5/weight-supplement/sheet-weight-supplement.json:4` describes the evidence artifact; approval is the later explicit JSON record. This does not authenticate a factory run or authorize its other tables.

## 1. Actual current seams and constraints

All source references below are repository-relative. `VC/` abbreviates **`Assets/VintageCollector/`**, not a real extra directory. Relevant Domain, Persistence, Collections, Runtime controller/host and Presentation seams were read in full, including both halves of `TabletopPresentation.cs`.

| Actual source evidence | Consequence for the smallest integration |
|---|---|
| `VC/Domain/Data.cs:4–34`; `Catalog.cs:32–59` | Catalog/model DTO schema is already versioned. Global IDs already encode printing + number + finish + variant. `Catalog` validates IDs and snapshots cards, but does not establish research approval. |
| `VC/Domain/PackGenerator.cs:25–35,38–83,85–105,113–136` | Generator is already general enough for 7C/RH/3U, weighted draws and C/U without replacement. It snapshots mutable `RuleData`. Do **not** create a new generator or change RNG. Its constructor validates syntactic consistency, **not approval**. |
| `VC/Domain/PackGenerator.cs:57` | Number8 exception is correctly Base-scoped already. Preserve it. |
| `VC/Persistence/SessionData.cs:8–14,15–36,60–72` | Each immutable pack identity already carries model ID/version, catalog version, printing, RNG/status, seed and raw ordered IDs. No new save field is necessary for multi-set dispatch or deterministic wrapper selection. |
| `VC/Persistence/StateValidation.cs:11–30,33–50` | Base-only model/printing, 5C/2E/RH/3U and unconditional number8 exclusion are current barriers. Session-level transaction uniqueness, first-new flags, prefix completion and revision bounds must survive unchanged. |
| `VC/Persistence/SessionStore.cs:9–19,28–42,46–57` | One catalog currently validates every pack. OpenPack freezes awards before wrapper/reveals, has identity-collision idempotence, and disallows another pack while current is incomplete. RevealTo is an absolute target, not an increment. |
| `VC/Persistence/SaveProtocol.cs:31–45,62–71,109–158` | Unsupported semantic data aborts; it is not corruption eligible for blank/fallback. Both copies/pending/anchor and live lineage are checked before recovery. Keep atomic replacement/readback/anchor/fault stages. |
| `VC/Collections/PhysicalStore.cs:10–26`; `PhysicalValidation.cs:10–30`; `PhysicalProtocol.cs:34–40,98–144` | Physical history uses global card IDs and absolute per-card/per-condition edits; schema1 has no per-set field. Validation and every decode/save/recovery path need the same global approved registry. Number8 currently has a second incorrect-for-new-sets exclusion. |
| `VC/Collections/BinderProjection.cs:14–28`; `CollectionContract.cs:39–47` | Binder counts only revealed virtual prefixes (not concealed durable holdings). Eligibility and the target are currently hardcoded101. Filter/search do not redefine the completion denominator. |
| `VC/Runtime/OpeningGame.cs:83–109,124–176,218–248,277–297` | Runtime currently owns one generator/catalog. Generation occurs only on explicit Generate, pending arguments/result freeze before durable write, and cue occurs after acknowledged projection. PublishView resolves card IDs through the single catalog today. |
| `VC/Runtime/OpeningGame.cs:187–199,300–309`; `Phase4CollectionHost.cs:11–19,48–82,147–175,184–225` | Container lease precedes session then physical; release is reverse order. Export reconciles actual leased stores. Import selects next launch; it never swaps live stores. Preserve error gating and frozen physical/import retries. |
| `VC/Presentation/PresentationRules.cs:58–98`; `OpeningGame.cs:241–246` | Generate only ready/results. The eleventh ordinary Next stays on the normal table until explicit Results; completing it is not permission to auto-generate. |
| `VC/Presentation/TabletopPresentation.cs:138–163,200–249` | Media/reveal-provider replacement stops current audio. Same-reference assignment is a no-op. Only committed acknowledgements cue; consumption precedes suppression. No polling setter-based per-frame rebinding. |
| `VC/Presentation/TabletopPresentation.cs:444–449,514,545–557,595–627` | Header/sidebar still say Base; wrapper fronts have a single property. CardFace is passed a **global card ID**, despite the parameter name `printingId`; a supplied full face suppresses all overlay labels. A generic template must not masquerade as a supplied full printing. |
| `VC/Media/PresentationMediaContract.cs:5–27`; `PresentationMediaLibrary.cs:7,21–57,68–109,118–137` | Image interface, optional metallic interface, separate reveal interface, resource ownership and fallback audio are distinct. Library is disposable and owns decoded resources, not imported/native asset references. |
| `VC/Runtime/Phase3MediaHost.cs:32–57,95–104`; `VC/Audio/RevealAudioHost.cs:15–28` | Legacy media host parses only Base TextAsset; audio host assigns one profile. Both detach only their own reference. Do not attach them alongside competing Phase5 owners. |
| `VC/Audio/RevealAudioProfile.cs:8–26` | Existing native profile already provides11 positional clips and non-Pokemon/Pokemon/holo rare overrides, configurable rare position8. No new audio selection engine is needed. |
| `VC/Media/MediaManifest.cs:30–56`; `MediaFiles.cs:25–42,72–98` | Runtime manifest admits only fixed roles + cardID, one wrapper per role; its paths exclude Authoring. Existing authoring placeholder manifest is **not** loadable runtime input. Do not weaken path/identity guards to load it. |
| `VC/Scenes/BootstrapRevealAudio.unity:219–249,288–289` | Accepted scene opts into BaseSetMaterials manifest/full budget, pointer tilt and its native audio profile. New scene must preserve these Base bindings without modifying accepted assets. |

The read-only `Authoring/Phase5/Code/PrintingCatalogIndex.cs:9–29` is useful proof of exact ordinal global lookup, but its namespace is `VintageCollector.Authoring` and it carries **no approval/generation authority**. Do not silently make that authoring file a Unity runtime dependency or duplicate its class under the same namespace.

## 2. FROZEN core registry and model admission contract (stream A)

Use one small pure C# runtime registry, not a configurable engine/framework. Proposed new files:

- `VC/Domain/ApprovedSetRegistry.cs`: public `ApprovedSetRegistry`, public immutable `ApprovedSetDefinition`.
- `VC/Domain/ApprovedSetPolicy.cs`: internal exact allowlist, catalog membership and approved model semantic specification/pins. No public registration or policy mutation API.

```csharp
namespace VintageCollector.Domain {
  public sealed class ApprovedSetRegistry {
    public static ApprovedSetRegistry ForLegacyBase(Catalog baseCatalog);
    public static ApprovedSetRegistry FromApprovedData(Catalog[] catalogs, RuleData[] models);
    public ReadOnlyCollection<ApprovedSetDefinition> Sets { get; }
    public ApprovedSetDefinition FindSet(string printingId);
    public Catalog FindCatalog(string printingId, string catalogVersion);
    public CardPrinting FindCard(string cardId);
    public bool IsCompletionEligible(string cardId);
    public PackGenerator CreateGenerator(string printingId);
  }
  public sealed class ApprovedSetDefinition {
    public string SetId { get; }
    public string PrintingId { get; }
    public string CatalogVersion { get; }
    public string ModelId { get; }
    public string ModelVersion { get; }
    public string Status { get; }
    public string RngAlgorithm { get; }
    public int CompletionTarget { get; }
    public Catalog Catalog { get; }
    // Construction internal. No setter/RuleData exposure.
  }
}
```

`ForLegacyBase` is a **Base-only validation adapter**. It accepts only the canonical Base printing/version/eligible membership and its fixed supported model metadata; it has no admitted RuleData and `CreateGenerator` throws InvalidOperationException. Existing callers already construct their own generator; this preserves their signatures without giving a catalog generation authority. `FromApprovedData` requires exactly the three canonical sets and one approved model for each. It snapshots collections and rules; registry dictionaries use StringComparer.Ordinal; null/empty/duplicate printing/card/model keys fail. Unknown lookup throws; no case folding, suffix repair, first-match, latest-version or cross-set fallback.

### Exact admitted tuples

Registry model key is the **four-field tuple `(printingId, catalogVersion, modelId, modelVersion)`**, not set name, UI index or only modelVersion. Use a value key or unambiguous length-delimited encoding internally; do not concatenate ambiguous user strings. The following are the only admitted tuples:

| printingId | catalogVersion | modelId | modelVersion | completion |
|---|---|---|---|---|
| `base-en-unlimited1999` | `base-en-unlimited1999-0.1.0` | `base-en-unlimited1999-sheet-informed` | `0.1.0` |101, original strict unstamped target excluding Machamp8 |
| `jungle-en-unlimited1999` | `jungle-en-unlimited1999-0.1.0` | `jungle-en-unlimited1999-sheet-informed` | `0.1.0` |64, includes holo8 |
| `fossil-en-unlimited1999` | `fossil-en-unlimited1999-0.1.0` | `fossil-en-unlimited1999-sheet-informed` | `0.1.0` |62, includes holo8 |

All require status `approved-provisional`, RNG `xorshift32-v1`, schema1 catalogs/models. Base sources are `VC/Data/base-en-unlimited1999.catalog.json:2–5` and `base-en-unlimited1999.model-a-v0.1.json:2–16`. J/F canonical versions and model IDs are the integration contract for the approved-data handoff, not a claim the candidate JSON already has those values.

### Admission guards — not just trusting matching strings

1. Bootstrap verifies exact approved source-byte SHA-256 for every catalog/model TextAsset **before deserializing**. Freeze hashes in a newly owned Phase5 input receipt/policy; no caller/import/scene-supplied hash can grant approval. Also pin the independent approval and the explicitly selected supplement table mapping. The final new-data hashes must come from the model worker's explicit handoff and independent parent verification; they are intentionally not invented here.
2. Core `FromApprovedData` independently checks semantic equality to the frozen policy: catalog versions/printing, exact global ID membership, set/language/edition/variant/number/rarity/kind/finish/eligibility; no duplicate global ID or extra variant. A syntactically valid `Catalog.FromData` is not an approval check.
3. Rules must match exact schema/model/version/status/printing/RNG, approved research/approval references and approximation list, exact ordered slots, pool names, ordered entries, positive integer weights and uniqueness flags. Expected semantic policy is compiled/frozen from reviewed canonical data, **not** derived from the same untrusted RuleData being checked. Check it before constructing `PackGenerator`; retain the generator's own validations/snapshot too. Altered weight, reordering, uniformized pool, absent approximation, forged approval label, extra pool, zero-weight entry and cross-printing pool member all fail.
4. Base stays byte/semantic unchanged. J/F:11 cards,7 commons, position8 rare-or-holo,3 uncommons, no energy slots, exact1/3 holo replacement; C/U weighted without replacement within their own pools; rare/holo single draw; independent loose packs with no shared depletion, pity or box quota. Only Jungle `jungle-1-removed` (Table J1) and Fossil `fossil-1` (Table F1) vectors are authorized (`Phase_5_Model_Approval.json:11,17,23–56`). No error/promo table or physical sub-run inference is authorized.
5. Existing global IDs are preserved exactly: `pkmn-{printingId}-{number:D3}-{finish}-{variant}` (`Catalog.cs:54–55`); sourceID/name/collectorNumber alone is not an ownership key. Jungle/Fossil holo/nonholo numbered printings remain separate. Ordinary eligible IDs are also completion-eligible; the **Base-only** Machamp8 exception is applied only to Base, never as a global number exclusion.

## 3. Session, physical and binder interfaces (stream A)

**Add overloads, do not replace signatures.** These canonical Base overloads keep their existing default fault arguments and delegate to `ForLegacyBase(catalog)`. The corresponding registry overload has identical remaining arguments:

```csharp
// Persistence
SessionStore(string directory, IStateSerializer serializer, Catalog catalog,
             ISaveFaultInjector faults = null);                     // keep
SessionStore(string directory, IStateSerializer serializer, ApprovedSetRegistry registry,
             ISaveFaultInjector faults = null);                     // add
SessionArchiveValidation.Validate(IDictionary<string,byte[]> files,
                                 IStateSerializer serializer, Catalog catalog); // keep
SessionArchiveValidation.Validate(IDictionary<string,byte[]> files,
                                 IStateSerializer serializer, ApprovedSetRegistry registry); // add
// Collections
PhysicalStore(string directory, IPhysicalSerializer serializer, Catalog catalog,
              ISaveFaultInjector faults = null);                    // keep
PhysicalStore(string directory, IPhysicalSerializer serializer, ApprovedSetRegistry registry,
              ISaveFaultInjector faults = null);                    // add
BinderProjection.Build(Catalog catalog, SessionSnapshot session, PhysicalSnapshot physical,
                       string mode, string filter, string search);  // keep Base-only
BinderProjection.Build(ApprovedSetRegistry registry, string printingId,
                       SessionSnapshot session, PhysicalSnapshot physical,
                       string mode, string filter, string search);  // add
CollectionProfiles.CreateBackup(string activeDirectory, IStateSerializer ss,
                                IPhysicalSerializer ps, Catalog catalog); // keep
CollectionProfiles.CreateBackup(string activeDirectory, IStateSerializer ss,
                                IPhysicalSerializer ps, ApprovedSetRegistry registry); // add
CollectionProfiles.PreviewImport(string backupPath, IStateSerializer ss,
                                IPhysicalSerializer ps, Catalog catalog); // keep
CollectionProfiles.PreviewImport(string backupPath, IStateSerializer ss,
                                IPhysicalSerializer ps, ApprovedSetRegistry registry); // add
```

`OpenPack`, `OpenWrapper`, `RevealTo`, `SetQuantity`, `Reload`, snapshots, `ConfirmImport(ImportPreview)`, `DiscardPreview`, container constructor and receipt shape stay unchanged. `ImportPreview` preserves its public read-only `ReceiptId`, `SourceHash`, `SourceName`, `VirtualCardCount`, `PhysicalCardCount`, `VirtualUnique`, `PhysicalUnique` and `Summary` fields/properties (`VC/Collections/CollectionContract.cs:49–60`); no caller-created replacement preview is introduced. Existing calls with literal null as the catalog argument may need an explicit `(Catalog)null` in **new tests** because reference-type overloads are ambiguous; do not broaden legacy semantics to resolve overload ambiguity.

Internals: add registry-aware overloads/fields through `StateValidation.Identity/Session`, `SaveProtocol`, `PhysicalValidation.Edit/State`, `PhysicalProtocol`, `CollectionArchive` constructor/Open. All paths, including load/save/readback, pending recovery, archive inspection and staged-store validation, must use the **same immutable registry**. Do not update only the public SessionStore constructor. Review retains that registry in place of one Catalog; confirm/retry does not consult current selection or reload models.

Source-visible guard asymmetry to close within A’s owned SaveProtocol: archive session decoding calls `CollectionJsonShape.Session(payload)` before the serializer (`SessionArchiveValidation.cs:36`), but live `SaveProtocol.Decode` currently deserializes directly (`SaveProtocol.cs:39–44`; `UnityStateSerializer.cs:16–19`). Add that same existing strict shape/range gate before live deserialization; this does not change schema1 and prevents missing/extra fields or numeric coercion from bypassing the intended record identity gate. Exercise it through native JsonUtility and every recovery copy, not only archive preview. Physical live decode already gates shape and its native serializer checks lossless numeric roundtrip (`PhysicalProtocol.cs:34–39`; `UnityPhysicalSerializer.cs:12–21`).

### Per-record validation, with immutable historical contents

- StateValidation resolves **each pack's stored tuple**, finds its exact catalog, checks11 IDs/11 flags, UTC/nonzero seed, wrapper/reveal bounds, exact approved slot membership, booster eligibility and same printing. Base retains5C/2E/RH/3U and repeated-energy allowance; J/F use7C/RH/3U and permit their number8. Use distinct C/U uniqueness groups; no arbitrary global deduplication of legitimate Base energy repeats.
- Preserve whole-session transaction uniqueness, newToVirtual recomputation in original chronological order using full IDs, earlier-pack completion, revision bounds, checked totals and all same-identity/award/monotonic-lineage comparisons. Selection does not reinterpret old flags or completion.
- **Never regenerate a historical pack from seed on load/import**, reroll it under newer data, resort its IDs, recalculate its stored metadata from the selected set, upgrade candidate version strings or “repair” unknown cards. Seed is provenance. New-set raw ordered IDs and metadata freeze before durable write, just like Base. Unknown/unsupported stored tuples fail closed with bytes preserved.
- Physical edits resolve exact global cardID through the registry, then enforce that card's own eligible printing. Keep condition allowlist, absolute quantity0–99999, zero removes a bucket, transaction idempotence/collision, UTC, revision==event-count and exact edit-prefix lineage. Do not key by collector number, set label or UI row index.
- Binder projects one explicitly chosen printing at a time. Target101/64/62 is from the approved set definition; filter/search never changes it. Add read-only `BinderSnapshot.PrintingId`; make TargetCount stored, not hardcoded. Keep existing internal constructor `(rows,unique,total)` delegating to Base101 if needed by old fixtures; add `(string printingId,int targetCount,BinderRow[] rows,int uniqueOwned,int totalQuantity)` for registry projections. Rows are detached/read-only. Virtual counts still use only revealed prefixes, not SessionSnapshot.VirtualHoldings; physical completion sums positive condition buckets per exact printing. No binder operation generates/reveals/awards.

## 4. Verified wire format and archive gates — **retain schema1**

This conclusion follows the actual source, not a speculative JSON manifest design:

| Actual wire component | Current fields / shape and source |
|---|---|
| Session JSON | `schemaVersion, revision, packs`; each pack exactly `transactionId,createdUtc,modelId,modelVersion,catalogVersion,printingId,rngAlgorithm,status,seed,orderedCardIds,newToVirtual,revealedCount,wrapperOpened` (`SessionData.cs:8–14`; `CollectionJsonShape.cs:51–57`). |
| Physical JSON | `schemaVersion,revision,edits`; each edit `transactionId,createdUtc,cardId,condition,quantity` (`CollectionContract.cs:7–9`; `CollectionJsonShape.cs:59–61`). No need to add a printing field: global cardID already supplies it. |
| Store envelopes | Literal `VTCG-SAVE-1\n` / `VTCG-PHYSICAL-1\n`, lowercase SHA256 of UTF8 payload, newline, payload (`SaveProtocol.cs:12,19–20,31–44`; `PhysicalProtocol.cs:13,20–39`). |
| Archive manifest | **Not JSON.** `collection.manifest` is `CollectionEnvelope.Wrap("COLLECTION", payload)` → `VTCG-COLLECTION-1\n` + checksum + newline + sorted lines **`<memberName> <byteLength> <sha256>`** (`CollectionArchive.cs:44–52`; `CollectionEnvelope.cs:10–16`). It has **no printingId, catalogVersion, model list or completion target field**. |
| ZIP members | Six authoritative names `state.save,state.backup,state.anchor,physical.save,physical.backup,physical.anchor`, optional exact `session.empty` / `physical.empty` markers, mandatory manifest. Fixed membership,3–9 entries, member cap32MiB, total128MiB, archive cap128MiB+1MiB; manifest cap4096 (`CollectionArchive.cs:12–13,31–42,55–68`). |
| Anchors/profile metadata | Store anchors hold revision + hash. Import receipt has five lines: receiptID, sourceHash, sessionRevision, physicalRevision, boot pointer context. Owner has receiptID/sourceHash/context; active pointer has receiptID/sourceHash/receiptHash (`CollectionProfiles.cs:75–82,143–166`). These do not encode a set catalog and need no new field. |

**Proposed decision: keep all existing schema numbers, field sets, envelope headers, empty markers, manifest and receipt versions unchanged.** Multi-set support is an extension of the admitted **metadata value allowlist**, not a new wire shape. No wrapper field, selected set, binder set or UI state enters durable records. Do not introduce schema2 just to bypass the Base-only validator. Future identity/schema changes need a separate decision.

### Old-player behavior and Base roundtrip

- Legacy Base-only APIs remain Base-only and reject new-set records, even when they can parse schema1. Existing old `StateValidation` rejects new models/printing (`StateValidation.cs:19–20`); old physical validator cannot find new IDs (`PhysicalValidation.cs:16–17`). Such InvalidDataException is **not** caught as recoverable envelope corruption (`SaveProtocol.cs:51–59`; `PhysicalProtocol.cs:46–54`). One unsupported primary/backup/pending blocks the whole load instead of silently restoring an older Base copy. This is the fail-closed compatibility mechanism; it must be proven with isolated fixtures of the old source/player, never actual old save directories.
- No auto-routing a Phase5 container into an old player; Phase5 gets a separate build-adjacent Saves directory. An empty/Base-only Phase5 container may remain readable by old players because its values are supported; unsupported mixed/new-set values must not be partially loaded, blanked or stripped.
- Base legacy schema1 → preview → confirm as new profile → restart → export → preview remains supported. Archive capture preserves exact member bytes. Import's original source ZIP remains immutable. Staged recovery may create a missing backup/anchor from a valid single-file legacy archive; therefore do **not** claim ZIP byte equality or identical final membership after recovery. Preserve every supplied source member's bytes while copying, every historical identity/order/flag, and every physical edit event; new coherent recovery copies are separate evidence.

### Gates that must not regress during registry conversion

1. Local/reparse/device path checks and whole-container exclusive `collection.lock` lease remain (`CollectionFiles.cs:11–28`; `CollectionProfiles.cs:16–19`). Lock order stays **container → session → physical**, release reverse. A set change does not dispose/reopen a store or acquire a second set-specific container.
2. Export only the actual active directory; reconcile leased opening/physical stores, reject pending/error/unacknowledged files and missing acknowledged content, preserve minimum revisions, validate capture and produced ZIP, publish unique backup and verify final hash (`CollectionProfiles.cs:33–41`; `CollectionArchive.cs:37–42`; `Phase4CollectionHost.cs:147–155`). Never export a per-selected-set subset disguised as the collection.
3. Preview is bounded/read-only, admits strict ZIP dialect/CRC/membership/hash/JSON/semantic lineage, retains owned immutable bytes and the registry snapshot. No current store construction or extraction during preview (`CollectionArchive.cs:55–68`; `CollectionZipStructure.cs:15–34`; `CollectionProfiles.cs:50–54`). Keep strict duplicate/unknown/missing-field/numeric shape checks; don't let JsonUtility's ignored fields grant wire extensions.
4. Confirm requires the **exact same object reference** owned by this service, exact source SHA256 recheck and retained-byte SHA, same pointer context (or exact already-published pointer); keeps source open with read sharing only through publication; extracts owned bytes only (`CollectionProfiles.cs:69–108`). No re-preview, source reread extraction, set-selection-based resolution or replacement of an uncertain review.
5. Stage collision/owner/receipt gates, exact full session identity+ack state, exact physical **history** as well as current buckets, atomic profile/pointer publication/readback, min revisions and restart-required behavior remain (`CollectionProfiles.cs:85–108,118–140,151–166`). Never merge or hot-switch the live profile. Uncertain physical edits and import confirms retain original intent and require same-operation Retry; no new selection/review/export/cancel can replace them.

## 5. FROZEN passive DTOs and set selection (stream B + parent)

Stream B owns these presentation contracts. Existing fields remain; new fields have safe legacy defaults. None are serialized into stores.

```csharp
// namespace VintageCollector.Presentation
public sealed class SetChoiceView { public string printingId, label; }
// additions to OpeningView:
public string selectedPrintingId, activePackPrintingId, setLabel;
public bool canSelectSet;                     // default false in old scenes/fixtures
public SetChoiceView[] supportedSets;
// additions to BinderView:
public string printingId, setLabel;
public bool canSelectSet;                     // binder browsing, not next-pack intent
public SetChoiceView[] supportedSets;
```

- `OpeningGame.SelectedPrintingId` means **next explicit Generate**. `activePackPrintingId` is the stored current pack's printing, including complete results. `setLabel` describes the visible active pack when one exists, otherwise selected preview. A results selector must label itself **“Next pack”**, never relabel old results as the newly selected set.
- Opening selection command is exactly `set:<printingId>`, routed to new parent-owned `bool OpeningGame.TrySelectSet(string printingId)`. It does no generation, RNG, save, award or audio acknowledgement. Known exact IDs only. It republishes selection state only; generation count and session revision must remain unchanged.
- **Opening selection allowed only** at ready or explicit results with acknowledged stores, no pending/error/terminal/restart/review/physical edit/collection gate, not paused or inspecting, and no binder overlay. At sealed, any revealing state (including card11 hold), pause, inspection, error or unresolved work it is refused even if invoked directly. Same selection is idempotent. Parent computes `canSelectSet`; passive policy mirrors it and validates a supported choice, but parent rechecks all authority gates.
- Ready preview is non-generated and uses wrapper A. Explicit Generate freezes the selected printing, model/catalog tuple, transactionID, seed, UTC and PackResult in one PendingAction. After that selection cannot redirect pending work or resumed records. On startup with an active pack, initialize next-pack selection to its admitted printing; on an empty session default Base. No persistence for selection is needed.
- Binder has a separate host-owned `binderPrintingId`, defaulting to Game.SelectedPrintingId on initial host setup. Command **`binder-set:<printingId>`** only changes this browsing scope. It never calls TrySelectSet, Generate or storage. It can browse a different set while a sealed/revealing pack is paused behind the binder, provided opening/physical stores are acknowledged and no physical draft requiring retention, import review, uncertain confirm or restart lock is active. On browse change reset selectedCardID, quantity, page and search/filter to safe initial values; do not reinterpret a retained physical draft. An acknowledged unsaved nondefault ownership draft must be explicitly abandoned/confirmed before switching; simplest UI is to disable switch while such a draft differs from the selected stored bucket.
- Returning from binder does not change the active pack, next-pack selection, wrapper, reveal count or cue history. All table labels/card lookup/media use stored active pack printing, never binderPrintingId.
- B may add `SetSelectorPresentation` as a **passive component** with `OpeningView View`, `event Action<string> Command`, and no Runtime/Domain/store dependency; host assigns the same projection and listens for `set:`. Or render those controls in TabletopPresentation's new opt-in area. Freeze one approach before edits: RC1 chooses the separate `SetSelectorPresentation.cs` component. Add dedicated controls in unused header space without moving accepted card/result/Back/Generate hitboxes; verify geometry at both accepted window sizes. Binder selector is B-owned within its overlay and scroll/focus policy. No combined “choose + generate” control, manual wrapper picker or generator call in either presenter.

## 6. Minimal SetConfig, media/audio routing and lifecycle

Stream B supplies the **passive** serializable scene-config type; parent owns initialization, import/build wiring and lifecycle. No ScriptableObject registry framework is needed.

```csharp
// new VC/Runtime/Phase5SetConfig.cs; namespace VintageCollector.Runtime
[Serializable] public sealed class Phase5SetConfig {
  public string printingId, displayLabel;
  public TextAsset catalogJson, modelJson;
  public RevealAudioProfile revealAudioProfile;
  public Texture2D cardFaceTemplate;             // J/F optional identity-neutral art
  public Texture2D[] wrapperFronts = new Texture2D[3]; // J/F A,B,C, exact order
  public Texture2D cardBack, wrapperBack;        // optional; null -> existing fallback
}
// OpeningGame additions (parent):
public Phase5SetConfig[] phase5Sets;              // absent/empty => unchanged legacy path
public ApprovedSetRegistry ApprovedSets { get; }
public string SelectedPrintingId { get; }
public bool TrySelectSet(string printingId);
```

Phase5 bootstrap must have exactly three explicit printing entries. Base entry references original catalog/model and accepted Base native profile; Base image/wrapper media delegates unchanged to `Assets/Supplied/BaseSetMaterials/Manifest.json` through the old verified library. New entries reference separately admitted catalog/model TextAssets and **separate** Jungle/Fossil native profile assets. Labels are trusted scene display text only, never identity or model authority. Snapshot config arrays at initialization; reject duplicate/mismatched IDs, unknown models and incomplete core data before taking store action. Optional texture/clip absence produces safe fallback, not storage failure.

Native textures are deliberate new scene references copied/imported only into the future owned stage; do not teach MediaFiles to read Authoring, mutate old manifests or pass an authoring manifest to PresentationMediaLibrary.Load. New texture source hashes/provenance come from `Authoring/Phase5/Placeholders/PlaceholderManifest.json`; native importer and scene readback must be proven before claiming images are bound. New-source native metas are generated by Unity in the owned stage, not fabricated by scripts.

### Stable adapter interface (B implements; parent binds once)

```csharp
// namespace VintageCollector.Media
public interface ICardTemplateMedia { Texture2D CardTemplate(string cardId); }
public sealed class Phase5PresentationMedia : IPresentationMedia,
    IWrapperMetallicMedia, IRevealSoundMedia, ICardTemplateMedia {
  public Phase5PresentationMedia(IPresentationMedia baseMedia,
      Phase5SetConfig[] configs, Func<string,string> printingForCardId);
  public void SetContext(string printingId, string transactionId);
  // Existing interface members, no playback, storage, generation or file I/O.
}
// Separate pure helper: promote the already implemented authoring helper unchanged:
// namespace VintageCollector.Presentation
public static class WrapperVariant {
  public static int ForTransaction(string transactionId);
}
```

`printingForCardId` resolves exact IDs through the approved immutable registry (unknown => no texture), **not** string splitting, collectorNumber or hidden-card enumeration. Adapter snapshots bindings. `CardFace`/`HoloMask` still take full cardID: delegate Base exact IDs to baseMedia, return null for J/F until final real per-printing media exists. `CardTemplate` returns the appropriate set template only for admitted new-set IDs. TabletopPresentation's optional template branch draws catalog-derived revealed name/number/kind/rarity/finish over the identity-neutral template, with metadata outside the full supplied-print path; supplied Base faces remain printed unchanged. Templates are matte and do not acquire a holo/metallic mask by inference. Null optional template retains existing generic fallback. No hidden-card texture enumeration, labels or rare classification before reveal.

`Sound(action)` delegates generic original Foley to baseMedia for all sets. WrapperFront/Back/CardBack and metallic masks use current context; J/F fronts are A/B/C, absent backs use generic fallback unless explicitly bound. Base masks delegate through `IWrapperMetallicMedia`; new missing masks are null, never full-face masks or borrowed Base masks. `RevealSound` routes through the context printing's native RevealAudioProfile; null entry/provider yields existing positional/generic fallback. Image media failure must not suppress separately assigned native reveal audio.

Parent owns `Phase5MediaHost` (not Phase3MediaHost+RevealAudioHost together): load one Base optional library once; construct one stable adapter, bind **that exact object** once to `table.Media` and `table.RevealSounds`; configure SetContext synchronously **before** publishing a postcommit View and calling NotifyCommitted. Context is activePackPrintingId/transactionID when a pack exists, otherwise selectedPrintingId/null. Publish/update may call SetContext idempotently, but must not rebuild the adapter/library or reassign providers to fresh wrappers. Changing Next-pack selection on results must not change the active context or stop a currently sounding last-card cue.

Why: `Media` and `RevealSounds` setters stop Foley on replacement (`TabletopPresentation.cs:154–163`). Even same-provider profiles nested in new wrapper objects would interrupt. SetContext itself must not Stop/Play or dispose anything. Native imported clips/textures are borrowed references; never Destroy/Dispose them. Parent detaches only if ReferenceEquals still points to its adapter, then disposes **only its owned decoded Base library** after rendering/playback are shut down. Do not unload/reload scene references on selection. Handle scene unload/reacquisition as the accepted builder does (`RevealAudioBuild.cs:33–36`); Unity equality/destroyed objects and C# interface references require explicit native null guards. Shutdown must not erase another component's replacement provider. Do not emit acknowledgements on initial bind, resume projection, results, inspection/Back, selection or export reconciliation.

New native profiles:11 pull slots each, approved rareRevealPosition **8** for both new models, three rare category fields present (even if a category is unreachable in one current catalog). Preserve Base asset/profile and accepted source clips, do not overwrite them. Missing rare category falls back to position8, then generic Next; do not substitute another category. Only the newly acknowledged card's metadata is classified. Keep Skip one generic skip cue with no per-position catch-up; ack key `(transactionID, command, revealedCount)` dedupes, suppression consumes before mute/focus/pause/binder return, no replay. Card11 remains an ordinary pull on table until Results. Native profile assignment/readback and actual playback evidence are separate from source tests.

### Deterministic wrappers, no generator RNG and no manual choice

**Use the parent’s already implemented helper unchanged**, not a second hash design: `Authoring/Phase5/Code/WrapperVariant.cs:3–14`, namespace **`VintageCollector.Presentation.WrapperVariant`**, API **`public static int ForTransaction(string transactionId)`**. Stream B owns its later unchanged promotion to `VC/Presentation/WrapperVariant.cs` plus Unity-generated meta in the owned stage. No Unity dependency is added to the helper.

- Null/empty transaction returns0=A for idle preview. Nonempty transaction hashes its exact UTF8 bytes using FNV1a32: initial2166136261u, for each byte `unchecked((hash ^ value) * 16777619u)`, then `hash % 3u`. Index0/1/2 selects the current set’s A/B/C array. It is deliberately transaction-only, not printing-prefixed. The same transaction index across different sets is harmless: each set has distinct three bindings.
- Base still uses its unchanged accepted wrapper binding, without introducing Base variants. New-set ready preview usesA without a transaction allocation. The active pack’s stored printing selects the binding array; its immutable transactionID selects the index. Keep both stable through inspection/Back/resume. No persisted wrapper field is needed.
- Do not trim, normalize, case-fold, parse as GUID, change UTF8 encoding, use GetHashCode/clock/seed/frame, sample any RNG, or add a manual wrapper selector. No historical art-subject claim. Future changes cannot silently reassign these transaction appearances.
- Parent reports native Mono5groups passed; this discovery independently read the helper, tests and runner, but did not rerun that separate stream. `TestsPhase5/AppearanceChecks.cs:12–27` tests stable/bounded selection, idleA, independent UTF8 vectors including nonASCII,300 fixtures reaching all3variants and culture independence. `tools/run_phase5_appearance.py:17–36` compiles only appearance test/helper, pins its inputs and reconciles nonzero checks, without Domain/RNG/store or Unity player. It remains **authoring-only, not live runtime integration**.

Existing source-pinned vectors (`AppearanceChecks.cs:15–17`):

| transactionId | index | variant |
|---|---|---|
| `fixture-pack` |2|C|
| `transaction-A` |0|A|
| `caseSensitive` |1|B|
| `Pokémon-交易` |1|B|

The exploratory SHA256+printing wrapper vectors in this discovery’s initial `source-baseline.json` are **superseded design scratch, not the frozen interface or acceptance oracle**. Do not implement them. The final verification receipt independently computes the existing helper’s FNV/UTF8 vectors instead. Assert equal generator RNG call traces and ordered card results with wrapper/media/selection on/off during future integration.

## 7. Exact three-stream file ownership

All paths below are proposed **later edits**, not permission for this discovery worker to edit them. New Assets files/metas must be introduced only through the parent's separately owned Phase5 stage/promotion procedure. No stream touches accepted builds, saves, originals, user PackageManagerSettings, old source tools, Base manifests/material assets, .artifacts/phase5-review/pins.json or commits.

### Stream A — core catalog/model/session/physical/archive/binder

Exclusive existing files:

- `VC/Persistence/StateValidation.cs`, `SessionStore.cs`, `SaveProtocol.cs`, `SessionArchiveValidation.cs`.
- `VC/Collections/CollectionContract.cs`, `BinderProjection.cs`, `PhysicalValidation.cs`, `PhysicalStore.cs`, `PhysicalProtocol.cs`, `CollectionArchive.cs`, `CollectionProfiles.cs`.

Exclusive new files: `VC/Domain/ApprovedSetRegistry.cs`, `VC/Domain/ApprovedSetPolicy.cs`; tests `TestsPhase5/RuntimeCore*`, `TestsPhase5/RuntimeArchive*`, `TestsPhase5/RuntimeBinder*`; new runner `tools/run_phase5_runtime_core.py`; evidence `.artifacts/phase5-runtime-core/`.

Keep Domain/Data.cs, Catalog.cs, PackGenerator.cs, Random.cs and SessionData DTO wire fields unchanged unless parent explicitly reallocates an evidence-backed defect. CollectionFiles/Envelope/JsonShape/ZipStructure remain unchanged by default; A is the sole owner if a required registry-related adapter change is later justified. Do not edit model worker's ApprovedData/tools/Model tests or earlier catalog-index authoring proof. A consumes that handoff and reports exact admitted hashes/spec to parent.

### Stream B — passive UI, templates/wrappers/media/audio routing

Exclusive existing files:

- `VC/Presentation/OpeningView.cs`, `PresentationRules.cs`, `TabletopPresentation.cs`, `BinderView.cs`, `BinderLayoutRules.cs`, `BinderPresentation.cs`.
- `VC/Media/PresentationMediaContract.cs` (only additive ICardTemplateMedia if separate file not chosen).

Exclusive new files: `VC/Presentation/SetSelectorPresentation.cs`, `VC/Runtime/Phase5SetConfig.cs`, `VC/Media/Phase5PresentationMedia.cs`, `VC/Presentation/WrapperVariant.cs` (unchanged promotion from authoring); tests `TestsPhase5/RuntimePresentation*`, `TestsPhase5/RuntimeMedia*`, `TestsPhase5/RuntimeWrapper*`; new runner `tools/run_phase5_runtime_presentation.py`; evidence `.artifacts/phase5-runtime-presentation/`.

Do not edit OpeningGame, store/projection/profile services, old library/parser/file guards, accepted native profiles or shaders/tilt motion/renderers. Pure profile selector API already works; no edits to RevealAudioProfile/RevealAudioHost are required. No Runtime/Domain dependency in presenters; config/adapters may accept passive immutable inputs as specified. Template-label branch must leave Base full-print branch and accepted hitboxes intact.

### Parent stream — OpeningGame, collection/media hosts, bootstrap/import/build integration

Exclusive existing files: `VC/Runtime/OpeningGame.cs`; `GamePaths.cs` only for an **explicit new** Phase5 editor default/save mode if needed, leaving old Resolve path/flags unchanged. Keep old Phase4CollectionHost/Phase3MediaHost/RevealAudioHost source behavior unchanged; introduce separately opted-in new hosts rather than retrofitting old scenes.

Exclusive new files: `VC/Runtime/Phase5CollectionHost.cs`, `Phase5MediaHost.cs`, `Phase5SmokeProbe.cs`; `VC/Editor/Phase5Build.cs`, `Phase5BuildPolicy.cs`, `Phase5NativeChecks.cs`; `VC/Scenes/BootstrapPhase5.unity`; new `VC/Data/Phase5/` admitted data copies/native metas; `VC/Audio/Profiles/Phase5/` separate native new-set profiles; `VC/Media/Phase5/` imported temporary texture copies/native metas or another single explicitly frozen destination. Parent alone creates/binds these assets and generated metas after both workers' code handoffs; no concurrent Unity AssetDatabase writes.

Parent tests `TestsPhase5/RuntimeIntegration*`, `RuntimeNative*`, `RuntimePlayer*`, `RuntimeBuildPolicy*`; new runner `tools/run_phase5_runtime_integration.py`; evidence `.artifacts/phase5-runtime-integration/`. Reserve exact new build stage `.artifacts/phase5-build-project/`, exact separate output `Builds/Phase5Windows/`; final scene path above. No use of old RevealAudio/Tilt/Phase4 build entrypoints, accepted output folders or source ProjectSettings for building. Stage only pinned compiler closure, exclude player Saves/Logs before any traversal, require admission/ownership/source-hash/preservation checks before Unity actions, and reacquire native refs after scene changes. Old players remain immutable; both generated player and human preview run only with separate explicit Phase5-owned test saves.

Parent responsibilities in code:

1. Branch on opt-in `phase5Sets`; retain legacy fields catalogJson/modelJson and initialization signatures unchanged. Snapshot and source-hash-validate Phase5 config/data, construct registry once and approved generators keyed by exact printing before opening stores. No new-set generation via arbitrary RuleData passed from a selection command.
2. Phase5CollectionHost acquires container lease, initializes Game session with the registry/profile, verifies same active directory, then opens global PhysicalStore. It owns binder scope/drafts/backup/review/retry gates and projects new BinderView. It does not replicate protocol logic. It holds the same registry through all export/import operations.
3. OpeningGame resolves pack card IDs globally, freezes generator/model on Generate, republishes detached set-aware views, performs same pending absolute actions, updates media context before commit cue, and preserves card11 hold. No generation on host initialization, configuration, set selection, binder open/close, import, Retry/recovery or resume.
4. Selection is memory-only, profiles are container-global, and successful import remains restart-required. Release physical → session → container exactly once with recursion-safe shutdown, then release optional media only after detaching/stopping its consumer.

## 8. Acceptance and regression oracles (future execution, not reported passes)

Use source-only tests first, native Unity serialization/editor checks next, then a real **separate Phase5 Windows player**. Every runner must have explicit fresh owned output/save roots, nonzero reconciled tests and no writes outside Root other than the already allowed normal Unity/licensing caches. Preserve earlier suites; add tests in reserved filenames instead of letting parallel streams edit old tests.

### A — pure core and archive

- Exact three tuples accepted; forged ID/version/status/RNG/approval, candidate catalog tokens, missing/duplicate catalog/global ID, altered slots/order/weight/uniqueness/provenance and cross-set members rejected. A caller-mutated input array/RuleData cannot change registry/generator output after construction. Legacy Base overloads reject J/F and retain canonical Base operation.
- Base deterministic golden output/RNG call trace unchanged; Base5C/2E/RH/3U, energy repeats allowed, Machamp8 absent,101 target. New-set7C/RH/3U, no energy, exact rare alternative1/3, C/U weighted without replacement, number8 eligible,64/62 targets. Use approved Table J1/F1 ticket boundaries and supplied deterministic vectors, not uniform-pool expectations or marginal assumptions for later without-replacement picks.
- Mixed Base→Jungle→Fossil history opens/reloads/resumes with exact ordered records, metadata, stored new flags and quantities. Unrevealed awards stay out of binder counts; per-set completion/physical buckets never bleed. Filter/search/holo-only view retains whole-set denominator. Same collector numbers across sets remain independent.
- Fault every existing prepare/replace/backup/anchor/before-ack stage; Retry uses the same PackResult/transaction/target, no extra generation/awards. Current or earlier unsupported copy blocks load without fallback. Inconsistent same-revision data, older conflicting acknowledged copies, live rollback, lost acknowledged content and corrupt pending all fail without discarding evidence.
- Mixed raw archive export→preview→confirm→new launch→re-export verifies full record/history and source-member integrity, source hash lease/recheck, registry snapshot and profile pointer/minimum gates. Keep legacy schema1 and empty/single-copy import roundtrips. Old Base-only source/isolated player rejects nonempty new-set session/physical archives and preserves all supplied copies. No schema2 or added fields are emitted.
- Carry forward adversarial ZIP/JSON/path/import ownership gates. Existing regression references: `TestsPhase4/CoreTests.cs:103–138` physical/recovery; `143–205` binder/lease/Base archive/import; `215–274` uncertain import/ZIP/history; `291–305` strict shape/second-writer/junction; `353–390` minimum/source lease/ack-conflict; `403–449` ownership release/numeric ranges; `488–536` CRC/structure/caps/published owner. These are inspected oracles, not tests run by this discovery.

### B — presentation/media helper proofs

- Direct + UI set commands accepted only in enumerated states; unknown/noncanonical IDs refused. Selection changes neither session revision nor generator count/RNG calls. Results remain old active printing while Next-pack selection changes. Binder-only set browse never changes Game selection or pack identity; draft/review/uncertain/restart locks persist through Back/reopen.
- Existing WrapperVariant UTF8/FNV vectors, three distinct actual fronts per new set, fixed A preview and same resume/inspection/Back binding. No manual picker. Full supplied Base face/masks preserved; J/F template identity labels correspond only to revealed catalog data. No hidden face/name/rare-result leakage in actual OnGUI observers.
- Stable Media/RevealSounds reference throughout ordinary view/selection updates; context changes do not Stop/Play current source, no disposal of borrowed native refs, shutdown detaches only owned reference and releases decoded library once. Missing Base optional library still allows native reveal clips; missing template/profile clip uses safe fallback.
- Port accepted audio tests from `TestsRevealAudio/RendererBoundaryChecks.cs:56–159`:11 positions, three rare categories/configured position8/fallback, duplicate ack, muted/focus/pause/binder consumption, error→same-target Retry once, no startup/resume/results/inspect/back/set-selection/reconciliation audio, Skip generic-only, current clip uninterrupted by ordinary projections/selection. Existing profile classification oracles: `TestsRevealAudio/AudioProfileSelectionTests.cs:32–79`; lifecycle reference oracles:80–103.
- Native render/control geometry at1280×720 and1600×900: same hero/result/inspection/Back targets, readable new set labels/selector, binder keyboard/tab/scroll and ownership badges. Accepted hover/tilt/material clocks/shaders not redesigned. Card11 reveal stays normal table; only Results command changes results view.

### Parent — native/player and preservation

- Stage admission refuses main project, sibling stage, reparse path, foreign/preexisting output and any unowned/preserved destination. Editor compiles actual worker closure; every new script/JSON/native profile/texture and scene has native-generated metadata and real serialized ref readback. Profile clips are native imported refs; rare8 configured independently for J/F; Base profile/manifests untouched.
- Native serializer/shape tests reject overflow/coercion/duplicate/unknown/missing fields (`TestsPhase4/RuntimeNativeNumbers.cs:24–58` illustrates current physical and session archive gates). Mixed registry must be exercised through actual JsonUtility, not just test doubles. No numeric or wire defaults change.
- Actual Windows player: start empty without generating; choose each set, Generate/Open/11 Next/Results, inspect/back, Skip, binder per-set physical edits, explicit backup and restore to next launch, restart sealed and mid-reveal, post-prepare fault Retry, no reroll/duplicate awards. Save context/reveal identity and media context match after reload; three fronts appear deterministically across test transactions. Capture player-native audio/render evidence and distinguish it from human acceptance.
- Compare earlier independently frozen preservation manifests without rewriting their192 pins: all eight old players/1,088 immutable members, original480 Assets/Packages/ProjectSettings baseline and416 originals per parent preservation scope. For later allowed source promotion, explicitly enumerate new/changed owned source assets; never claim the original compiler source tree stays identical after approved runtime implementation. Existing old players/originals/shared Base media manifests remain identical. Exclude live Saves/Logs **before** traversal, never after reading them. Do not use this discovery's source-only pins as a substitute for the parent frozen whole-preservation manifest.

## 9. Discovery verification and limits

The only discovery-created files are this document and the bounded `source-baseline.json`, repeatable `verify_discovery.py`, and `mechanical-verification.json` receipt under `.artifacts/phase5-runtime-discovery/`. The baseline pins80 bounded implementation/data/index sources, records actual candidate metadata, verifies the approved supplement hash and records exploratory wrapper vectors (superseded by the parent's existing WrapperVariant helper; see §6). It does not contain or validate live saves, old player membership, approved model worker completion, native import results or a build.

Mechanical verification checks every explicit `VC/` source-path:line reference against the actual file's existence/line range, local documentation links and unchanged hashes for the bounded baseline sources. No runtime tests/builds were executed, so this document reports **no new runtime PASS**. Final catalog/model hashes, native stage creation, implementation, source-level tests, old-player fail-closed fixture execution, playable verification and human acceptance remain work for the assigned streams/parent.

## 10. Parent freeze decisions before coding dispatch

Adopt RC1 or publish precise deltas once; do not turn ordinary integration into another approval-only loop:

1. **Registry identity and approval**: exact class/API/tuple/allowlist above, fixed Base-only overload semantics, no arbitrary registration; independently frozen approved-data byte/semantic pins from the separate worker's handoff.
2. **Wire compatibility**: schema1/headers/manifest/receipts unchanged; metadata allowlist extended only, unsupported old-player records fail closed; no selected-set/wrapper field, migration or historical reroll.
3. **Three nonoverlapping owners**: A core/contracts, B passive DTO/UI/media helper/config type, parent OpeningGame/new hosts/native assets/scene/build. Any shared API delta is parent-mediated before either worker edits another owner's file.
4. **Set state**: next-pack selection separate from active pack and independent binder browse; opening switches ready/results only, binder browse locked for drafts/uncertain/review/restart, no generation except explicit Generate.
5. **Media/audio**: one stable adapter bound once, context before ack, Base media delegated unchanged, templates not full card faces, separate native new-set profiles at rare8, optional media cannot block storage, borrowed native reference lifecycle preserved.
6. **Presentation reproducibility**: existing WrapperVariant.ForTransaction UTF8/FNV policy and A/B/C ordering frozen, no RNG/manual variant selection; accepted hitboxes/hover/lighting/card11/ack-audio semantics retained.
7. **Actual artifact gate**: new exact owned stage + `BootstrapPhase5.unity` + `Builds/Phase5Windows`, source/native/player tests and preservation receipts before claiming a playable Phase5 result. No edits to accepted builds, originals, live saves or old tools.
