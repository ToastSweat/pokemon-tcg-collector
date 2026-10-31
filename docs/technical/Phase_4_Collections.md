# Phase 4 collection core — implementation and integration

## Scope and verified result

Core services only; this is **not a Phase 4 completion claim**. Native adapter/runtime integration re-verification against these exact core bytes, candidate player, graphical/native opening-to-binder flow, independent review and the user gate remain parent-owned.

Run from any directory using the literal native path:

```bash
python 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/tools/run_phase4_core.py' --label fix-core-review-final-01
```

Choose a new label for every run. The runner rejects reused evidence labels rather than using stale fixtures. It uses Unity 6000.2.14f1's bundled Mono/mcs, warnings as errors, explicit source files and one test entrypoint. No SDK, NuGet, network, Unity Editor or live save directory is needed. Only the harness uses `System.Web.Extensions` for its real JSON adapter; production uses injected serializers and platform libraries.

Final retained run: `.artifacts/phase4-core/fix-core-review-final-01/` — **112 named checks, 112 passed, 112 unique names, compiler and test process exits 0**. `results.json` count is reconciled with the named check array; `compiled-inputs.json` and `source-pins.json` contain SHA-256 input pins, including the current approved-strict-unstamped-101-excluding-machamp catalog/rules. All compiled inputs were unchanged during the run. `execution.log` retains exact compiler/execution commands and exit codes. This fix worker did not edit `SaveProtocol.cs`: the parent's intentional analogous legacy anchor guard is included, pinned as `fa265fce59ecdf9e70a85d41e2c6acc8723887857d75c111c429e336850e2b05`. Session schema/IDs/model and the shared contract remain unchanged. All fixtures, child binaries, logs, TMP/TEMP/TMPDIR and real junction targets are under the selected synthetic run directory.

## Physical store

`PhysicalStore(directory, physicalSerializer, catalog, faults = null)` implements the exact shared API. No changes to `CollectionContract.cs`, `SessionData`, `SessionStore`, `StateValidation` or the schema1 award/reveal representation were needed. The parent-owned legacy anchor guard is intentionally included in the final compile.

- Sidecar DTO schema version 1; revision equals the append-only event count.
- Each transaction records an **absolute** quantity for one exact catalog printing ID / condition bucket. Zero removes the visible holding but retains its event. Different buckets aggregate only in the physical binder projection.
- Conditions are exactly `unknown`, `near-mint`, `lightly-played`, `moderately-played`, `heavily-played`, `damaged`; quantities are integers 0..99999. Transaction and UTC identity are validated. No inferred physical ownership, virtual edit, generation, grading or valuation exists.
- Reusing an ID with the exact original timestamp/card/condition/quantity is idempotent. Any changed field is a collision. Snapshots and their holdings are detached immutable objects/read-only collections.
- A held `physical.lock` stream uses `FileShare.None` plus an OS byte lock. Same-process and real second-process writers are excluded. Dispose releases the lease; never delete a lock file to steal a lease.
- Local paths and existing ancestors/leaves are checked root-first for reparse points. URI, UNC/device/relative/traversal/ADS/DOS-device paths are rejected. This is not an ACL sandbox against a malicious process that can replace ancestors between checks.

### Physical publication protocol

Authoritative files are `physical.save`, `physical.backup`, `physical.anchor`; prepared recovery input is `physical.pending`. Work files are `physical.writing`, `physical.installing`, `physical.backup-writing`, `physical.anchor-writing`.

Save and backup envelopes are strict UTF-8:

```text
VTCG-PHYSICAL-1

<lowercase SHA-256 of the UTF-8 JSON payload>

<schema1 PhysicalStateData JSON>
```

The anchor uses the same envelope; its payload is `<revision>\n<SHA-256 of the complete save-envelope bytes>`.

Publication is: validate and serializer-round-trip check; write/flush(true) temporary; rename it to pending (the recoverable prepare point); install by `File.Replace` or first-create `File.Move`; write/flush/atomically replace the mirrored backup; read both back; write/flush/replace/read the anchor; delete pending; acknowledge. There is **no delete-then-move fallback**. Unsupported atomic replace/filesystem behavior blocks the action rather than weakening durability.

An exception marks the in-memory store uncertain. Its next snapshot/mutation reloads before accepting more work. Before prepare, only the previous state is committed. From prepare onward, the validated prepared revision can be recovered and the identical transaction retry acknowledged without applying any delta twice. Tests exercise 16 actual file-protocol cut points, including post-anchor and post-pending-delete acknowledgement loss. A real child exits without Dispose immediately after durable prepare, and the parent reloads exactly that edit once.

All authoritative copies are validated before recovery writes. Prefix lineage, same-revision collisions, schema/printing/condition semantics and the acknowledged anchor are checked. Every available valid primary, backup and pending copy **at the anchor revision** must match the anchor hash, even if another copy is newer. A newer B2 plus its B1 prefix cannot hide an independently contradictory A1 acknowledgement. Read-only session and physical archive inspection apply the same rule to both archive copies. Matching-anchor roll-forward remains supported; no matching older copy is invented when only newer data remains. Unsupported schemas/envelopes and semantic corruption never silently fall back. Envelope-damaged primary copies can recover from a valid compatible backup and are preserved under `.corrupt-<GUID>` names. A damaged pending record, a damaged anchor, no valid recovery copy with acknowledgement evidence, or a live-session rollback fails closed with evidence retained. An unprepared `.writing` alone is not a commit.

## Binder projection

`BinderProjection.Build(catalog, session, physical, mode, filter, search)` is a pure read-only projection.

- Modes `virtual` / `physical`; filters `all`, `owned`, `missing`, `duplicates`, `holo`.
- Rows are numerically ordered by collector number; the strict eligible catalog target must contain 101 printings and excludes #8/Machamp. Completion is positive unique printings /101. Total quantity and unique/completion statistics describe the entire selected collection, not filtered rows.
- Search is ordinal case-insensitive name substring or exact canonical collector-number string (`1`, not `01`). Ordinary unowned checklist names are catalog metadata, not evidence of a hidden award.
- Virtual rows **never consult `SessionSnapshot.VirtualHoldings`**, which includes concealed durable awards. They count each pack's revealed prefix only. Production validation guarantees all prior packs are complete. An incomplete current pack contributes only its revealed prefix; concealed cards cannot affect quantity, ownership, duplicates, filters or completion.
- Physical rows aggregate confirmed condition buckets and remain independent of virtual openings. Tests include full 101-unique completion with quantity 202, readonly rows/holdings, and a hidden holo searched through the owned filter.

## Collection container lifecycle

Construct and retain one `CollectionProfiles(baseSaveDirectory)` **before** opening either active store. It holds `collection.lock` with exclusive file sharing and a byte lock for the entire runtime lifetime. Resolve `ActiveProfileId` / `ActiveDirectory`, then initialize the existing runtime for that validated profile. Empty ID means the existing default directory itself; resolution creates no session/physical state and performs no migration/copy. Healthy legacy schema1 data remains in place and unchanged. The session schema/award/reveal protocol remains unchanged; the parent's explicit anchor-conflict hardening is included, not reverted.

Imported profiles are exclusively operation-owned staged directories `.import-<GUID-N>`, then atomically renamed to `Profiles/<same GUID-N>`. Native Windows `CreateDirectoryW` supplies exclusive directory creation. A pre-existing stage or profile is a collision, never adopted or overwritten. The checksummed `import.owner` and `import.receipt` bind the operation to reviewed source/context. Before any retry extraction/store-validation/receipt write, the retained stage must contain the **exact expected owner envelope** for receipt ID, archive hash and original pointer context. Missing/damaged/different ownership fails closed before writes. Published-profile retries also verify owner before store recovery. A RAM `StageOwned` flag alone is not authority; replacing a retained stage with a foreign stage is rejected even if its recognized save bytes match. No overwrite/merge of the current directory occurs.

`active-profile` is a checksummed `VTCG-ACTIVE-PROFILE-1` envelope containing the profile GUID, source archive hash and receipt hash. `import.receipt` is a `VTCG-IMPORT-RECEIPT-1` envelope containing profile GUID, source hash, imported session revision, imported physical revision, and the previous pointer token. These receipts are provenance/minimum-revision evidence, not mutable current-save mirrors. Later normal user actions may update the selected collection without rewriting receipts. Startup checks pointer/receipt identity and imported acknowledgement minima, then the owning runtime must open the actual stores for full catalog/protocol validation.

Confirmation changes **next-start selection only**. The service's current `ActiveProfileId` / `ActiveDirectory` do not switch underneath an open SessionStore/PhysicalStore. Set the UI to restart-required on success; do not live-reinitialize or overwrite the current controller. Dispose stores first, then the container lease, during Quit. On metadata/selection failure **do not initialize the default profile as a fallback**.

## Export format and user-triggered backup

Call `CreateBackup(activeDirectory, sessionSerializer, physicalSerializer, catalog)` synchronously on the owning thread only after both store snapshots are acknowledged and no mutation/error is pending. The core checks active-directory identity, validates captured copies read-only and refuses store work/pending files. It cannot infer the caller's in-flight UI state, so the host must fence that state.

A fresh GUID-named ZIP is written/flush(true) to `<base>/Backups/*.zip.writing`, moved without overwrite to `.zip`, then read back. Preview lists only direct `.zip` children of this container's Backups. Cross-container archives must first be deliberately placed there; arbitrary paths/URLs/raw saves are not accepted.

ZIP membership is an exact subset of:

- `state.save`, `state.backup`, `state.anchor` (original schema1 bytes)
- `physical.save`, `physical.backup`, `physical.anchor`
- `session.empty` only when no virtual state exists, exact bytes `VTCG-EMPTY-SESSION-1\n`
- `physical.empty` only when no physical state exists, exact bytes `VTCG-EMPTY-PHYSICAL-1\n`
- required `collection.manifest`

No locks, work files, corrupt copies, imported receipts/provenance, assets, media or credentials are exported. An anchor without valid content is an error, not an empty profile. Imported receipt minima and this service's prior-export-observed revision minima prevent rollback below that evidence. For the default profile, this service learns its process minima only from successful export validation, **not automatically from a live SessionStore/PhysicalStore acknowledgement**. The host must independently reconcile its acknowledged snapshots before first export; this harness does not establish that parent integration guarantee after every durable file disappears.

The manifest is a checksummed `VTCG-COLLECTION-1` envelope. Its payload is ordinal-name-sorted newline-separated `<member-name> <canonical-byte-length> <lowercase-SHA256>` lines, excluding itself. Exact member names, contents, manifest bytes and hashes are validated; unknown collection formats fail closed. Legacy schema1 single-save archives are supported **inside this versioned ZIP wrapper**, without reserializing/reinterpreting the source. The actual original SessionStore may create a missing backup/anchor only in the new imported profile.

Bounds: 32 MiB per data member, 4 KiB manifest, 128 MiB aggregate uncompressed, 129 MiB complete archive, 3..9 entries (the exact legal membership is narrower). Accepted ZIP dialect is classic single-disk stored/deflated, ASCII filenames, matching local/central names/flags/methods/lengths/CRC, contiguous non-overlapping local members, and no ZIP64, encryption, descriptors, extras, comments, preamble or trailing bytes. This deliberate narrowness is part of format version 1. A different ZIP packaging dialect requires explicit format work, not silently widening import.

## Preview / confirm trust boundary

`PreviewImport` performs bounded reads only: no directories, profile files, selection changes or recovery writes. It checks source membership/structure, duplicate names (case-insensitive too), symlink/reparse metadata, slip/absolute/ADS paths, lengths, aggregate limits, CRC, manifest hashes, envelope/schema versions, strict required JSON shape (including duplicate/unknown/missing properties and numeric coercion), original pack/slot/printing identities, transaction lineage and original new-to-virtual flags. `SessionArchiveValidation` is a narrow read-only helper using the unchanged `StateValidation`, `StateCopy` and `SaveProtocol.Equal`; core/helper compile together for internal access, never reflection.

The immutable public preview is only a detached display/result object. A **single private review slot**, not an accumulating dictionary, retains owned original archive bytes, decoded review snapshots, source hash/path and active-pointer context. Confirmation accepts only that exact service-owned object, not an equivalent DTO or edited UI text. Archive counts describe reviewed stored awards, not binder visibility.

### Bounded ownership / host API change

- `PreviewImport(...)` replaces the one previous **unstarted** review only after the new archive has validated successfully. Replaced preview objects become unowned; confirming them throws `ArgumentException`. A failed replacement leaves the current review valid. Persistent retention is one review; replacement can temporarily allocate a second bounded archive while validating it. This is not a promise of a small fixed process-memory ceiling.
- New public **`void CollectionProfiles.DiscardPreview(ImportPreview preview)`** explicitly cancels an unstarted review or releases an acknowledged successful review. Only the exact currently owned object is accepted; null, forged, replaced or already discarded references throw `ArgumentException`. It clears memory only; it never deletes any file, stage, receipt or recovery evidence.
- Publication is fenced before exclusive stage creation is attempted. From then until successful confirmation acknowledgement, both replacement and `DiscardPreview` throw `InvalidOperationException`. A source/context failure before publication, or a pre-existing stage/profile collision detected before attempting creation, may still be canceled. Once publication starts, later failures are conservatively unresolved, even if no pointer was installed.
- The host must **retain the same preview during uncertain confirmation**, keep cancel/re-preview disabled, and retry `ConfirmImport` with that exact object. Do not clear it in an error handler or `finally`; no equivalent reconstructed preview can retry it. Successful retry validates/reconciles the same receipt/profile, never duplicates it. Ordinary successful confirmation still requires Quit/reopen; the current active store does not switch.
- After `ConfirmImport` returns successfully the host may call `DiscardPreview` and clear its display reference. If it keeps the preview, repeated confirmation remains idempotent. Cancel should clear the host reference **only after** `DiscardPreview` succeeds. `Dispose` clears retained review memory and releases the container lease, without deleting unresolved disk evidence; disposal is not a recovery action.
- Calls remain synchronous and owning-thread serialized. No new multi-threaded or cross-process recovery API is introduced.

`CollectionJsonShape.Session` now range-checks canonical unsigned tokens **before calling the injected/native deserializer**: `schemaVersion` and `revealedCount` fit nonnegative Int32, `seed` fits UInt32, and `revision` fits nonnegative Int64. Production semantic validation still enforces schema 1, legal reveal counts and revision/history relationships. Tests cover exact maxima, overflow and wrap aliases from `RuntimeNativeNumbers.cs` (4294967297 schema, 4294967419 seed, 4294967296 reveal, 18446744073709551617 revision), plus a sentinel proving the adapter is not called on rejection. The fresh native Unity 80-check rerun is parent-owned; a pure gate pass is not a native rerun claim.

`ConfirmImport` rechecks archive hash and context before creating a stage and keeps a read handle denying archive writes/deletes through publication. Extraction uses the owned bytes, never reparsed UI fields. It exclusively creates files, validates the destination with **actual `SessionStore` and `PhysicalStore`**, compares complete session identity/flags/reveals and full physical transaction history (not just aggregate final buckets), publishes the exclusive receipt, then moves the stage into its new profile directory. Only then does it prepare and atomically install/read back the active pointer. Replacing an existing pointer retains `active-profile.backup`.

Public import/export I/O/validation errors omit raw source names/absolute paths and serializer/filesystem inner errors. Original collection, source backup and useful recovery evidence are retained. Partial extraction remains a non-executable stage, never an active profile. Retry the same preview after a transient failure: existing operation-owned files are reconciled/validated, a completed profile is not duplicated, and a committed pointer after lost acknowledgement is verified rather than re-imported. Missing/corrupt receipt or acknowledged files are not synthesized into a successful empty restore.

## Crash/recovery limits (intentional, not hidden atomicity claims)

1. Physical and schema1 session stores are separately committed. A backup is a validated synchronous capture, **not** a cross-store transaction. The host must serialize actions and refuse backup while either side is uncertain.
2. New-profile files are sequentially published while inactive. Only the final directory rename and pointer replace are individual atomic filesystem operations. This is **not** a multi-file crash-atomic restore-in-place.
3. In-process uncertain acknowledgement is recoverable by retrying the identical retained preview. Public preview ownership does not survive process loss. Orphan stages/profiles/receipts/backups are retained; they are never blindly garbage-collected or adopted by a new preview.
4. A process crash after `active-profile.pending` is prepared but before pointer installation deliberately blocks subsequent startup, retaining old state/source/new profile/pending selection. The frozen API has no cross-process manual-recovery wizard. A future explicit recovery workflow/operator must validate this evidence and choose a selection; do not delete evidence or silently initialize blank. A crash after pointer installation can reopen the confirmed profile after pointer/receipt/store validation. Both boundaries are exercised with real child-process exit.
5. If the pointer is missing but a prior pointer backup or Profiles directory remains, startup refuses default fallback. Unknown/corrupt pointer or receipt also blocks startup. Imported receipt minima prevent coherent rollback to files older than that import.
6. Anchors and duplicate copies detect partial loss/corruption and in-process rollback. **No local-file protocol can distinguish a truly never-used default store from deletion/replacement of every file containing acknowledgement/history evidence**, or detect a coordinated rollback of all files and all independent proof to an internally consistent older state. Checksums are corruption detection, not signatures/authorization. No guarantee against a malicious account with write access, faulty storage ignoring flush, hardware power-loss lies, or directory fsync durability is claimed. Keep independent backups.
7. Filesystem path checks and leases constrain cooperating processes and reject existing reparses. They are not an OS security boundary against adversarial ancestor replacement races. Import staging/receipt ownership is conservative; no foreign partial directory is removed. Exact checksummed owner content detects accidental/cooperating-process stage replacement, not an adversary copying the expected owner envelope; these hashes are not authentication.

## Review-fix RED/GREEN evidence

All labels below are under `.artifacts/phase4-core/`; each has real results, compiler/execution logs and exact source pins. Commands use the runner above with the listed fresh `--label`.

| Slice | RED evidence (passed / named checks) | GREEN evidence (passed / named checks) |
| --- | --- | --- |
| CORE-01 older acknowledged conflict | `fix-core01-red-01` 56/60 (4 expected failures) | `fix-core01-green-01` 60/60 |
| CORE-02 repeated replacement | `fix-core02-replacement-red-01` 60/61 | `fix-core02-replacement-green-01` 61/61 |
| CORE-02 unresolved retry retention | `fix-core02-uncertain-red-01` 55/61 | `fix-core02-uncertain-green-01` 61/61 |
| CORE-02 explicit release/cancel API | `fix-core02-cancel-red-01` 53/62 | `fix-core02-cancel-green-01` 62/62 |
| TG-06 exact retry-stage owner | `fix-tg06-owner-red-01` 63/69 | `fix-tg06-owner-green-01` 69/69 |
| Native-demonstrated session numeric wrapping | `fix-session-numbers-red-02` 70/74 | `fix-session-numbers-green-01` 74/74 |
| Final expanded full core | — | `fix-core-review-final-01` **112/112** |

`fix-session-numbers-red-01` is a retained compiler-error attempt (extra parenthesis), not RED behavior evidence. The corrected `-red-02` observed four genuine overflow-gate failures before the production fix. A terminal workdir containing brackets was rejected before execution; running the literal absolute script path from `C:/Users/pada1` avoided that tool limitation without moving the project.

Expanded tests independently exercise valid-envelope physical prefix forks and same-revision event collisions; damaged/ahead/same-revision-conflicting anchors; corrupt/unsupported/semantically invalid pending copies; byte-preserving rejection; older acknowledged copies in primary/backup/pending; matching-anchor roll-forward; 64 replacements and 64 cancel cycles; all eight existing publication uncertainty cuts retaining the exact preview; pre-existing and renamed/replaced foreign stages; owner receipt/source/context/checksum/missing variants; and published-owner validation before uncertain retry.

ZIP additions start from a successfully opened valid ZIP before mutation. Coverage includes matching local/central CRC fields with wrong decoded-payload CRC; encrypted/descriptor/unsupported flags; too few/many entries; overlong names; disk and ZIP64 fields; local/central extras and entry/archive comments; offset gaps/overlap/local gaps; compressed payload bounds; preamble/trailing bytes; the complete-archive cap before ZIP parsing; and aggregate uncompressed overflow with individually in-bound members. No broader ZIP dialect was added.

Additional unverified boundaries remain: short/partial exclusive-file writes, native sharing conflicts during stage/receipt/pointer writes, and native process-loss while replacing an already-selected pointer. A partial/corrupt owned file is retained and may require explicit recovery rather than being automatically repairable. No new recovery wizard or expanded recovery guarantee is claimed.

## Evidence and remaining gates

Tracer bullets retain real RED/GREEN logs under `01-*` through `15-*`, plus expanded native/crash verification and the final run. Unsuccessful compiler/oracle/write attempts remain recorded with their real exits; labels containing “green” alone are not proof. The corrected JSON-shape RED removes old anchor/backup evidence so it actually exercises permissive JSON decoding rather than failing only because an unrelated anchor changed. Final counts are generated by real execution, not synthesized responses.

Coverage includes physical absolute commits/reload/conditions/zero/idempotency/collision, 16 protocol failures, corruption/unsupported/rollback, immutable snapshots; binder revealed-prefix/no-spoiler filtering/numeric ordering/101 completion; exclusive container/native second writers; validated exports/explicit empty markers; immutable read-only preview; strict malicious ZIP/JSON/schema/pack/flag cases; source replacement/context drift/forged previews; new-profile legacy restore/byte preservation; partial-stage and profile collision handling; eight in-process import publication cuts; actual process crashes before/after pointer publication; missing/corrupt acknowledgements/receipts; subsequent user edits and second-profile restore; real junction refusal; and source leasing during confirmation.

Remaining parent gates: rerun this harness if core inputs change; rerun isolated native Unity with the real JsonUtility adapters against these exact final bytes; integrate ProfilePaths/OpeningGame/Phase4CollectionHost and lifetime/uncertainty fences; exercise real UI/physical drafts/backup/preview/confirm/restart/opening-to-binder in the separate candidate player; independent review; preserve prior player directories and live saves; obtain the user gate. Do not report all Phase 4 complete or proceed to Phase 5 from this core result.
