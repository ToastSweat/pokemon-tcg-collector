# Shopping native verification: retained authority and evidence

## Operational layout

- Reviewed operative helpers: `.artifacts/phase6-shopping-operative-01/`.
- Owned Unity stage: `.artifacts/s6/`.
- Separate candidate: `Builds/Phase6ShoppingWindows/`.
- Native action evidence: `.artifacts/phase6-shopping-build/<fresh-label>/`.
- Isolated player fixtures: `.artifacts/phase6-shopping-player/<fresh-label>/`.
- Parent action receipts/adoption backups: `.artifacts/phase6-parent/`.

Do not rebuild a human-used player. The operational guard refuses any candidate `Saves` entry by name before inspecting its contents. Accepted-player preservation audits prune `Saves` and `Logs` before traversal; validation of a new candidate instead covers its complete unpruned membership and bytes.

## Reproducible action sequence

Only run these against the exact reviewed, promoted source closure. Use a new label for each attempt. The receipt maker verifies independent approvals and actual installed sources before issuing the receipt; do not hand-edit a receipt to make a mismatch pass.

```text
python -B .artifacts/make_shopping_receipt.py --action CreateAssets --label <fresh-label>
python -B .artifacts/phase6-shopping-operative-01/run_shopping_native.py --label <same-label> --authorization <returned-project-local-path> --trusted-sha256 <returned-digest>
python -B .artifacts/adopt_shopping_native.py --label <same-label>
```

Repeat that sequence with `CreateBootstrap`. Adoption is new-only: genuine generated script/folder metadata after asset validation, then the two native scenes and their metadata after bootstrap authoring. Existing source metadata must never be overwritten. Root's original build settings remain unchanged; the stage receives the separately verified two-scene recipe.

Then issue a fresh `BuildWindows` receipt and run the native action without an adoption step. Require actual Editor exit zero, zero native build errors/warnings, complete compiler closure and complete candidate manifest before any player test. Hash the exact successful `results.json` bytes for the driver's trusted-build digest:

```text
python -B .artifacts/phase6-shopping-operative-01/shopping_native_driver.py --run --label <fresh-player-label> --build-evidence <successful-build-results.json> --trusted-build-sha256 <sha256-of-file-bytes>
```

The driver uses isolated fixture saves and fake outbound actions. It is not a keyboard/mouse test of every field, nor a live browser/clipboard or market-price test. Read the native reports, validate declared counts against enumerated checks, validate screenshots, and compare exact notes/session snapshots between processes before publishing a pass.

## Failure lessons

### Distinguish implicit metadata from the wrong importer

Unity 6000.2.14f1 emitted genuinely generated script metadata containing only `fileFormatVersion: 2` and a lowercase GUID. An explicit `MonoImporter` block is not always present. The reviewed guards accept only the exact minimal two-root UTF-8 form or their explicit MonoImporter branch; they retain source-hash, GUID-uniqueness, no-overwrite, role, path and closure checks. Do not synthesize metadata to satisfy a guard, admit another importer role, or weaken folder validation. Folder metadata still requires its exact DefaultImporter roots.

### Keep observed before-state separate from frozen source authority

For preparation, the inherited C# source authority must remain frozen Root inputs. An existing stage can already contain explicitly allowed auto-imported metadata. Save that actual stage-before map as evidence, but do not pass those generated extras as inherited Root authority. For `BuildWindows`, preserve the actual effective stage map instead: its exact two-scene settings differ intentionally from Root. The runner's conditional selects these two meanings explicitly.

### Wait for real UI capabilities, not just a controller command

The collection host refreshes inspection shopping availability during its Update. Dispatching an inspect command and immediately invoking the table request in the same coroutine step can outrun that publication. The opt-in probe yields a frame and waits under a five-second deadline for actual availability, current view identity, matching card/index and no pending storage. It never forces availability, calls a private refresh, or bypasses source authority. Rebuild and rerun after changing the compiled probe.

### Own child cleanup before initializing input

Start cleanup immediately after successful Popen. Input-adapter construction, handshake, HWND discovery, input, exit and report validation all belong inside it. Terminate and bounded-wait on failure; fall back to kill/reap when necessary. Reap normal exits too. Constructor failures and report failures must not leave the owned process behind or become passing runs.

## Retained execution

- `a01`: failed on the overly strict script metadata expectation.
- `a02`: failed on preparation frozen-snapshot selection.
- `a03`: asset validation passed; its 14 genuine metadata files were adopted.
- `b01`: bootstrap authoring passed; four scene/metadata files were adopted.
- `n01`: build passed; first player run `s01` exposed probe inspection timing.
- `n02`: corrected build passed; `s02` passed shopping and independent reopen.

The authoritative raw files and limitations are linked from `docs/verification/Phase_6_Shopping_Execution.json`. Failed evidence remains historical; it must not be relabeled as successful or treated as current after a rebuild.
