# Phase 2 persistence verification

## Result

**109/109 named checks passed**, real execution exit `0`, using Unity `6000.2.14f1`'s bundled `mono.exe` and `lib/mono/4.5/mcs.exe` with `-warnaserror+`. No installs, Unity launches, Git operations or approved Domain/data modifications were performed by this worker.

- Final machine-readable result: `.artifacts/phase2-persistence/final/results.json` (declared count reconciled with its assertion array).
- Exact compile/run commands, stdout, stderr and exit codes: `final/execution.log`.
- Compiled source SHA-256 manifest: `final/compiled-inputs.json`.
- Protected input before/after digests: `final/protected-inputs.json`.
- Cross-implementation comparison: `.artifacts/phase2-persistence/evidence-summary.json`: all **six** protected Domain `.cs`/approved data `.json` inputs match the first RED run's bytes.
- Actual child exits/output and save fixtures: `final/fixtures/*/process.log` and adjacent session files.

These are pure service/filesystem/process checks, not Unity adapter integration, a native-player launch or physical mouse/UI testing. No tracked work is closed here.

## Reproduce

From the project root:

```bash
python tools/run_phase2_persistence.py --tests --label verify
```

For bracket-containing paths from the available Bash shell:

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG' && python tools/run_phase2_persistence.py --tests --label verify
```

`--filter <substring>` selects named checks; zero selected checks fail. `--label` accepts only safe filename characters. The runner explicitly compiles `TestsPhase2/PersistenceTests.cs`, **not** all `TestsPhase2/*.cs`; parent-owned entrypoints remain separate. It builds the actual unchanged Domain as its own DLL, then compiles the real Persistence sources and the persistence harness against it. Every run directs `TMP`, `TEMP`, `TMPDIR`, binaries, logs and GUID-isolated fixtures under `.artifacts/phase2-persistence/<label>`.

## Executed coverage

| Category | Evidence |
| --- | --- |
| First generation | Startup remains empty; full ordered pack, seed, timestamp, metadata and eleven virtual awards survive Dispose/reopen before wrapper opening |
| Progress/repeated input | Every absolute reveal boundary 0–11 with restart; wrapper requirements and bounds; mid-reveal resume; lower/equal targets; repeated generation/completion; complete old transaction retries; incomplete-current guard |
| Idempotency/collisions | Same exact transaction is a no-op; different timestamp, seed or cards under that ID rejected; separate subsequent transaction awards eleven more exactly once |
| New flags/immutability | Real repeated-Energy generated fixture; only first position new; quantities reconcile; second identical pack all duplicates; IList mutation rejected; returned snapshots stay unchanged; serializer and deserializer aliases cannot mutate authoritative state; candidate-changing serializer rejected before save |
| Validation | Schema, revision, missing/null archive/record, transaction/timestamp/seed, each provenance field, lengths, unknown/#8 IDs, wrong slots, reveal/wrapper, flags, duplicate transaction and incomplete archive prefixes; oversized save rejected |
| Preservation/recovery | Latest full backup repairs corrupted primary and vice versa; damaged bytes preserved exactly under `.corrupt-*`; unsupported schema in primary/backup/pending, unsupported envelope, malformed valid-checksum JSON, both copies corrupt, corrupt pending/anchor and conflicting prepared state all block without rewriting originals |
| Acknowledgement fence | Stale backup cannot replace newer acknowledged history; missing full copies do not create blank state while anchor survives; live store also rejects rollback when all authoritative files are deleted |
| Real OS I/O | Exclusive open of work file prevents writes; deny-delete primary handle prevents actual atomic replacement and blocks Snapshot recovery; closing blocker permits roll-forward; no virtual double award |
| Lifetime lock | Same-process second writer denied; Dispose releases lock and all APIs then reject use; failed constructors release lock; separately launched writer denied; `Process.Kill` of lock owner permits another process to reopen |
| Directory identity | Relative directory is pinned before lock acquisition; changing process CWD cannot redirect saves away from the locked directory |

The 109 named checks comprise **13 scenario/regression checks**, **48 injected-exception cases** (16 stages × generation/wrapper/reveal), and **48 abrupt-child-exit cases** (the same matrix). All use actual files, checksums, flushes and replacements. Child crash mode exits `73` at the named stage without executing `using` cleanup; an unreached stage would exit `74` and fail. Each reopen checks authoritative revision/progress/awards, then retries the exact same transaction or absolute target. This additionally proves process-released locks. Separate hard-kill testing covers a blocked lock-holder process.

## Retained vertical RED/GREEN evidence

All paths below are relative to `.artifacts/phase2-persistence/`. Logs retain their real exit statuses; no unsuccessful log is relabelled as passing evidence.

| Slice | Observed RED | Observed GREEN |
| --- | --- | --- |
| First durable generation/restart | `01-red/execution.log`: missing persistence namespace/API compilation failure (initial API did not exist) | `01-green`: 1 passed |
| Absolute progress/exactly-once | `02-red`: generation replay awarded again | `02-green`: 2 passed |
| Schema/archive/provenance/flags | `03-red`: unsupported schema accepted | `03-green`: 3 passed |
| Flushed prepare/replace/backup and uncertain outcomes | `04-red`: missing fault-stage behavior, 13 failures | `04-green`: 16 passed |
| Lifetime exclusive lock/disposal | `05-red`: second writer was not rejected | `05-green`: 17 passed |
| Acknowledgement rollback fence | `06-red-fixed-test`: stale backup accepted; `06-red-anchor-stages`: 9 missing stage failures | `06-green`: 54 passed |
| Live rollback guard plus actual process/OS checks | `07-red`: live deletion returned blank; other checks, including all child crash cases, passed | `07-green`: 106 passed |
| Unsupported envelope/blank-preservation hardening | `08-red`: unsupported envelope accepted; `08-red-blank`: persisted blank created extra files | `08-green`: 108 passed |
| Directory pinning | `09-red`: CWD redirected relative save to a nonexistent other directory | `09-green`: targeted check passed; `final`: all 109 passed |

`06-red` is also retained: the newly added mutation test initially attempted a direct assignment to read-only collection members and failed to compile. It was corrected to mutate through `IList<T>`; `06-red-fixed-test` then observed the actual missing rollback-fence behavior before production changes. The already-working mutation/backup/progress behaviors received regression coverage without deliberately breaking their implementations.

## Remaining limits/integration notes

The [protocol document](../technical/Phase_2_Persistence.md) names every stage, file and recovery/error rule. A call that throws after preparation can still roll forward. The controller must keep the same pending generation identity/result and absolute reveal target; do not reroll on Retry. `Snapshot` and `Reload` can throw while real recovery I/O remains blocked, rather than exposing unknown state. `InvalidDataException` is **not** an `IOException` on the tested Mono (`BaseType` was separately compiled/reflected as `System.SystemException`); catch it explicitly.

No arbitrary-power-loss guarantee is made: no portable directory-metadata flush, failing physical disk, machine power cut, network filesystem, storage-controller cache, full-device or antivirus scenario was exercised. Recovery-stage fault hooks are not added; real sharing denial exercises blocked recovery. Complete directory removal between launches cannot be distinguished from fresh startup, and a valid anchor cannot reconstruct missing content. Adapter/controller/player integration, presentation advance fencing, native interaction and user opening-feel review remain parent-owned verification.
