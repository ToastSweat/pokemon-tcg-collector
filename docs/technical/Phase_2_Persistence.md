# Phase 2 local persistence

Status: implemented and exercised as pure C# against the unchanged Phase 1 Domain with Unity's bundled Mono. Public types/DTOs match [the Phase 2 contract](Phase_2_Implementation_Contract.md). This subsystem makes no Unity/player/native-input claim.

## Ownership and accounting

`SessionStore` is a synchronous, single-caller service. Keep it alive on the controller's owning thread until `Dispose`; do not call concurrently or reenter it from a serializer/fault injector. Its constructor canonicalizes the directory once, acquires `state.lock` with `FileShare.None` **and** an OS byte-range lock, and holds that stream until disposal. Constructor failures close the lock. The lock file is never unlinked; process death releases the OS handle. Another writer is rejected, including a separately launched process. The parent chooses a project-local save directory; no default/global path is selected here.

`OpenPack` archives the complete result and first-time virtual flags in the same durable operation. That archive, not completion clicks, is the sole award source. `VirtualHoldings` aggregates every archived transaction exactly once, including unopened packs. A repeated Energy is new only at its first position if not already awarded. No physical holdings, money, shopping, binder edits, or completion denominator exist.

The result is persisted before `OpenPack` returns. Wrapper and positive absolute reveals are persisted before their methods return. `RevealTo` is bounded, monotone and idempotent: an old/equal absolute target returns current progress without writing. Positive targets require the wrapper open. The same transaction requires exact timestamp, seed, ordered identities and all model metadata; collisions throw. A new transaction cannot replace an incomplete latest pack. Old completed transaction retries return that old record, without changing `CurrentPack` or awarding again.

Every snapshot and read-only collection owns its data. Serializers receive copies; deserialized DTOs are cloned before adoption. Serialization is round-tripped and compared against the intended candidate before disk I/O. The supplied serializer must faithfully and synchronously map all frozen DTO fields; this is not a sandbox for hostile adapter code. There is no production JSON dependency: the parent supplies its Unity JSON adapter, while CLI tests use Mono's `DataContractJsonSerializer`.

## Supported data and validation

Supported session schema: `1`; supported Phase 2 model: `base-en-unlimited1999-sheet-informed`, `0.1.0`, `approved-provisional`, `xorshift32-v1`, `base-en-unlimited1999`, and the supplied catalog's exact version. A future schema/model needs an explicit reviewed upgrade, not an inferred migration. Historical source/model JSON is never rewritten or generation rerun on load. Stored seed/algorithm are provenance, not a claim that arbitrary caller RNG output can be regenerated from that seed.

Validation checks every authoritative copy before recovery writes: required metadata; bounded/nonblank ordinal transaction IDs (no trim repair/control characters); ISO UTC timestamps with `Z` and zero through seven fractional digits; nonzero seed; known eligible printing identities; exactly eleven IDs/flags; Model A's five common, two basic Energy, rare/holo, three uncommon order and C/U without-replacement policy; explicit #8 exclusion; reveal/wrapper bounds; unique archive transactions; complete archive prefixes; and per-position new flags recomputed from the entire earlier award sequence. Revision must lie between the minimum and maximum distinct state-changing operations capable of producing that archive. Timestamps need not increase, because the local clock can move backwards; archive array order is transaction order.

The in-memory initial state is revision zero with no packs. No blank save is written on startup; a persisted revision-zero blank is unexpected and rejected. Save files are capped at 32 MiB before reading/writing. Invalid schema, metadata, JSON with a valid checksum, unsupported envelope version, conflicting histories, corrupt pending state, corrupt acknowledgement anchor, or missing acknowledged state fail closed with `InvalidDataException`. No empty successful collection replaces these cases.

## On-disk protocol

All names below are in the same save directory/volume. `File.Replace` is required when the destination exists; there is **no delete-then-move fallback**. First publication uses a same-directory `File.Move`.

| File | Meaning |
| --- | --- |
| `state.save` | Current full session |
| `state.backup` | Same full session as primary after acknowledgement; temporarily the previous revision during replacement |
| `state.pending` | Fully prepared next state: a roll-forward obligation, retained until both copies and anchor are verified |
| `state.anchor` | Checksummed acknowledged revision and SHA-256 of the exact complete session envelope; prevents fallback to a stale backup or a blank after both copies disappear |
| `state.writing`, `state.installing`, `state.backup-writing`, `state.anchor-writing` | Unpublished work files, never recovery authorities |
| `*.corrupt-<GUID>` | Byte-for-byte preserved damaged primary/backup copies before repair |
| `state.lock` | Lifetime writer lock, not a liveness timestamp |

Session envelope is UTF-8 without BOM: `VTCG-SAVE-1\n`, lowercase SHA-256 of the UTF-8 payload, `\n`, then serializer text. The anchor uses the same checksummed envelope around `<decimal revision>\n<SHA-256 of complete session envelope>`. SHA-256 detects accidental corruption; it is not authentication against a user editing/recalculating all files.

Commit sequence:

1. Validate a detached candidate and serializer round-trip.
2. Write the whole envelope to `state.writing`, call `FileStream.Flush(true)`, then close it.
3. Rename it to `state.pending`. Before this boundary no action is committed/exposed; after it recovery must preserve that exact transaction and may roll it forward even if the original call threw.
4. Write/flush `state.installing`, then atomically replace `state.save`, receiving the old primary as `state.backup` (or move for first publication). `state.pending` still contains the full candidate.
5. Write/flush `state.backup-writing`, atomically publish it as `state.backup`, and read/validate both full copies against the candidate.
6. Write/flush `state.anchor-writing`, atomically publish `state.anchor`, and read/verify its revision and digest.
7. Delete `state.pending`. Only after these durable operations and read-backs return does the service adopt the candidate in memory and acknowledge success.

Load/recovery validates the anchor and all present primary/backup/pending copies before changing anything. Same-revision states must agree; higher revisions must extend the immutable award archive and never close a wrapper, reduce reveals or change original flags/identity. The newest compatible full copy is selected, subject to the anchor's revision/digest fence and any already-known live state. A good latest backup repairs a checksum-damaged/missing primary; a good primary repairs a checksum-damaged/missing backup. Corrupt source bytes are copied to uniquely named evidence files first. Both copies and the anchor are reverified before deleting pending and returning a snapshot. Unsupported/semantically invalid copies are not silently bypassed, even if another valid copy exists.

An unprepared `state.writing` may remain after an interrupted first attempt; it is retained with a notice and never adopted. A later same-transaction retry can overwrite that unpublished work file. If a pending/anchor/history conflict cannot be resolved, preserve the directory and block for manual recovery; do not delete saves as a retry strategy. Recovery notices describe the selected durable revision/source, or retained unprepared work. Notices persist for the lifetime of that store once observed; they are not stored as award data.

## Errors and controller Retry

- `ArgumentNullException`/`ArgumentException`: invalid command arguments or transaction input collision.
- `InvalidOperationException`: new pack while current incomplete; positive reveal while wrapper closed.
- `KeyNotFoundException`: unknown transaction.
- `InvalidDataException`: corrupt/unsupported/contradictory data or unfaithful serializer. On the tested bundled Mono it derives from `SystemException`, **not** `IOException`; handle it explicitly rather than relying on an I/O catch.
- `IOException`/`UnauthorizedAccessException`: real disk, lock, sharing, replacement or recovery failure; do not advance presentation.
- `ObjectDisposedException`: any store command/snapshot after disposal. `Dispose` itself is idempotent and never attempts a last-minute save.

Before disk work, the store marks its state uncertain. A failed commit does not install the candidate into runtime state. The next `Snapshot`, `Reload`, or command first resolves disk state; if recovery cannot finish, it **throws**, rather than returning a fabricated blank or stale authoritative success. Previously returned immutable snapshots remain unchanged.

The controller must retain the exact pending generation transaction/timestamp/seed/result and retry `OpenPack` with those same arguments. Wrapper retries use the same transaction; reveals use the same absolute target. A thrown call after preparation/replacement can already be durably committed: after the storage issue clears, `Reload`/`Snapshot` reflects the roll-forward, and repeating that same action is a no-op. Never substitute a fresh seed/transaction or increment a target on Retry. Recovery does not call `PackGenerator`.

## Fault stages and tested boundary

`ISaveFaultInjector.At(stage)` is optional, explicit-test-only. Normal commits visit these stages in order:

1. `before-temp-write`
2. `after-temp-write` (before explicit flush)
3. `after-temp-flush`
4. `before-prepare`
5. `after-prepare`
6. `before-replace` (installing copy already flushed)
7. `after-replace`
8. `before-backup`
9. `after-backup-flush`
10. `after-backup-replace`
11. `before-anchor`
12. `after-anchor-flush`
13. `after-anchor-replace`
14. `before-pending-delete`
15. `after-pending-delete`
16. `before-ack`

Every stage is exercised for generation, wrapper opening and absolute reveal, with real filesystem writes both for injected exceptions and abrupt child-process exits. Recovery itself does not invoke these hooks; a real Windows file-sharing denial tests recovery failure/retry independently. See [verification](../verification/Phase_2_Persistence_Verification.md).

## Limits

This is local process-crash recovery, not arbitrary power-loss or disk-failure protection. `Flush(true)` requests file flushing, but portable managed code here does not issue directory metadata flushes or promise storage-controller cache behavior, filesystem atomicity on other volumes/network shares, or simultaneous corruption survival. Complete directory deletion (including the anchor) between launches is indistinguishable from a new directory; a running instance refuses rollback even if all its files are deleted. An anchor prevents stale-copy rollback but cannot reconstruct missing content. Copy the entire closed save directory for manual backup; partial copying is not a supported import mechanism.

Archive writes, validation and aggregate snapshots are proportional to history, with a 32 MiB admission cap. No compaction/export/migration UI is added in Phase 2. Fault tests do not emulate every byte-level torn-sector state, machine power cut, disk-full device, permission policy, antivirus interaction or native Unity player lifecycle. The parent must verify its adapter/controller/UI/build separately.
