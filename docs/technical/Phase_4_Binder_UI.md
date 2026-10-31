# Phase 4 — passive binder presentation

## Delivered boundary

`VintageCollector.Presentation.BinderPresentation` is an original dark-green/brass, built-in IMGUI overlay using `LegacyRuntime.ttf`. It reads only the host's `BinderView`. It does not reference `OpeningGame`, collection services, save stores, generation, archive APIs, audio, or file IO. It never applies ownership edits or calls `Application.Quit`. Commands are requests; the host publishes the next view.

Both collection modes are prominent, with the active mode/printing/condition outlined in brass. Supplied rows are numerically ordered by collector number, displayed in 12-slot pages, and show empty/owned/duplicate quantities and supplied rarity/finish. Search and All/Owned/Missing/Duplicates/Holo filters request new projections; they do not interrogate hidden game state or independently re-filter source identities. Completion uses the host's `targetCount`, `uniqueOwned`, `totalQuantity`, and `completionPercent` unchanged. The UI does not invent a denominator or infer physical ownership. The real host must supply strict unstamped 101-printing projections excluding Machamp.

No branded graphics, external packages, precision gestures, sales, cloud accounts, or valuations were added.

## Exact public API

All DTOs are `[Serializable]` public-field classes in `VintageCollector.Presentation`:

```csharp
public sealed class BinderView {
    public bool open, busy, restartRequired;
    public string mode, filter, search, status, error, recoveryNotice;
    public string selectedCardId, selectedCondition, quantityText, importSummary;
    public int targetCount, uniqueOwned, totalQuantity, page;
    public double completionPercent;
    public BinderCardView[] rows;
    public string[] backups;
    public int selectedBackupIndex;
}
public sealed class BinderCardView {
    public string id, name, collectorNumber, rarity, finish, kind;
    public int quantity;
    public bool owned;
}
// BinderPresentation : MonoBehaviour
public BinderView View { get; set; }
public event Action<string> Command;
public bool IsOpen { get; }
```

The UI never writes DTO fields, row fields, or source arrays. It sorts a detached row-reference array, drops null/ID-less entries without manufacturing replacements, and resolves selection only from supplied safe rows. A stale/hidden selected ID cannot reveal a name or enable editing. Missing names use `Unnamed printing`.

### Command protocol

| Request | Meaning / gate |
| --- | --- |
| `binder-open` | Recognized while closed. The host supplies the dedicated table Binder button/opening request; this overlay does not draw while closed. |
| `binder-close` | Back to table; allowed even while busy, pending, errored or restart-required. |
| `mode:virtual`, `mode:physical` | Host rebuilds the appropriate safe projection and drafts. |
| `filter:all`, `filter:owned`, `filter:missing`, `filter:duplicates`, `filter:holo` | Host rebuilds the selected projection and resets/clamps page. |
| `search:<text>` | Exact untrusted text, including empty strings, Unicode and colons; host performs search. |
| `page:<nonnegative int>` | Zero-based page within the supplied safe row count. Host republishes `page`; the renderer defensively clamps stale page display. |
| `select:<fullcardID>` | Exact full stable ID of a supplied safe row, never a numeric index or shortened ID. |
| `condition:<known>` | Physical draft only, selected safe row required. |
| `quantity:<text>` | Physical draft only. No parsing, canonicalization, range validation or ownership change in the UI. |
| `save-physical` | Explicit **Confirm ownership** only; physical mode + safe selection + no error/review/lock. Host validates text and performs the absolute bucket update. |
| `backup` | Explicit backup request, blocked by an error/review/lock. |
| `backup-select:<index>` | Exact in-range supplied list index; no IO or automatic preview. |
| `import-preview` | Explicit request with a selected nonempty supplied backup; errors permit retry. No local summary/archive interpretation. |
| `import-confirm` | Explicit request only when the host supplies a nonblank reviewed summary and there is no error/lock. |
| `import-cancel` | Discard reviewed import through host; never alters local drafts or DTOs. |
| `quit` | Host-owned quit request, always reachable while open. |

Known conditions are exactly `unknown`, `near-mint`, `lightly-played`, `moderately-played`, `heavily-played`, `damaged`. Unknown/unrecognized condition display defaults to `unknown` without changing the DTO. The host must apply the same default and validate quantity text as canonical integer 0..99999 before committing. A zero bucket removes that holding; no such persistence logic is implemented here. No physical edit controls or requests exist in virtual mode.

### Review, errors, and acknowledgement

- A nonblank `importSummary` opens a read-only modal review. Collection-context controls are disabled. It explicitly says restore creates a **new profile**, selects it for **next launch**, preserves the current collection, and is **not a merge or overwrite**.
- Confirm/cancel are distinct buttons. Selecting a backup never previews or confirms automatically. Failed preview cannot clear the parent-owned quantity, condition, card selection, or backup selection.
- `busy` or `restartRequired` locks all commands except Back/Quit. Import confirm also requires an empty error. Recovery/error notices remain available in a scrollable fixed footer; busy/pending/restart messages do not suppress an accompanying error.
- Emitting `save-physical`, `backup`, `import-preview`, or `import-confirm` sets a local unacknowledged-request latch. Repeated dangerous requests are suppressed until **the host assigns `View` again**. Back/Quit remain available. This latch is not a transaction or persistence acknowledgement.
- The host must publish an acknowledged, busy, failed, or restart-required view after handling a request. Do not blindly republish an unchanged nonbusy snapshot every frame during an asynchronous operation: that would acknowledge the latch without acknowledging the operation. The host must independently enforce all service/transaction gates.
- Success after import should clear `importSummary`, publish `restartRequired=true`, and retain the old live collection until Quit/reopen. Failed preview should retain all draft/selection fields and publish a sanitized error.

## Interaction and layout

The logical canvas is 1600×900. The opaque green background fills the entire viewport; the 1600-wide composition is centered on ultrawide screens. Scaling has a 0.75 readability floor. A vertically scrolling body handles short windows while the header, error/status region, Back, and Quit remain fixed. Backup and review contents have independent scrolling. Long printing names wrap across two lines instead of being clipped to a single line.

Tab/Shift-Tab traverse enabled controls; Enter/Space activate native buttons; text fields accept keyboard input. Tab scrolls offscreen body/backup controls into view. Escape cancels an available review, otherwise requests Back, including while busy review cancel is locked. No global `Input` polling or audio changes are used.

The overlay consumes unused pointer, wheel and key events. **The parent must additionally gate the accepted table's Update/input/hitboxes while `IsOpen`**, preserve the previous pause state, and own the dedicated Binder button. IMGUI event consumption alone cannot disable another component's polled input. Parent remains responsible for user volume, audio, profile lifecycle, and the final normal-table card11 hold.

For native test diagnostics, internal `TextObserver`, `TextLayoutObserver`, and `ControlObserver` report only rendered labels/rectangles. Rectangle observers use root logical coordinates, not local nested-scroll coordinates. Tests multiply by the layout scale. Native `GUIUtility.GUIToScreenPoint` was observed to mis-scale nested group origins, so production focus scrolling tracks explicit logical origins instead; native clicks verify both 0.8 and 1.2 scale paths.

## Actual execution and retained evidence

All fixtures are **synthetic DTOs, not the real physical collection**. No session/physical store, archive service, live save, or actual eligibility catalog is loaded by these UI tests. The original interactive Editor was not controlled. Native work uses only `.artifacts/phase4-ui-project` and its own fixture player.

Final count-matched reports:

| Evidence under `.artifacts/phase4-ui/` | Actual result |
| --- | --- |
| `final-pure/results.json` | 101 checks, all pass; Mono compilation with warnings-as-errors. |
| `final-native/results.json` | 12 checks, all pass in isolated Unity Editor. |
| `final-gui-expanded/results.json` | 110 checks, all pass in the isolated native D3D11 fixture player. |
| `final-gui-expanded/png-verification.json` | 16 actual framebuffer PNGs, CRCs/decompression/dimensions verified. |
| `verification-summary.json` | 223 final checks; final compiled source hashes match current UI/test source; passive source policy verified. |

Native execution exercises owned-process HWND messages, actual IMGUI buttons/text fields, exact commands, physical confirmation/pending double-click guards, mode/filter/search/page requests, full-ID selection, backup selection/preview, read-only review/confirm/cancel, failure-draft retention, busy/error/restart lockouts, Back/Quit/Escape, Tab/Space, short-window focus scrolling, empty/null/unnamed row safety, DTO nonmutation, absence of audio sources, and no pointer-through to the synthetic underlying button.

Screenshots cover 1600×900, 1280×720, 2560×1080, 1280×480 and 1280×360. Physical, virtual, pending, error, review, restart and empty states are retained. Final physical/empty/short-review captures were visually inspected; this is not player/human acceptance.

Retained strict vertical RED/GREEN pairs have nonempty JSON with declared counts matching their enumerated checks and equal counts within each pair:

- `s1-command-red-utf8` → `s1-command-green`: 83 checks.
- `s5-visibility-red` → `s5-visibility-green`: 79 checks (long-name clipping, busy hiding errors, short-window focus scrolling).
- `s6-short-review-red-corrected-origins` → `s6-short-review-green`: 95 checks (corrected coordinate instrumentation and tiny-window summary/confirm overlap). A fixture scroll carryover was also isolated and fixed in test setup.
- `s7-busy-back-red` → `s7-busy-back-green`: 96 checks (Escape while busy review cancel is locked).

Earlier failed setup logs are retained, not presented as passing tests: Mono console encoding was made UTF-8; the isolated manifest needed the editor's built-in IMGUI/audio/imageconversion/screencapture/jsonserialize modules. No third-party package install was used. Early geometry/native API-existence RED/GREEN reports are also retained; their test counts grow when the API becomes available and are not the equal-count pairs above.

Reproduction from a shell already rooted in this project (use a fresh label; runners refuse evidence-directory overwrite):

```bash
export TMPDIR="$PWD/.artifacts/phase4-ui" TMP="$PWD/.artifacts/phase4-ui" TEMP="$PWD/.artifacts/phase4-ui" PYTHONDONTWRITEBYTECODE=1
python tools/run_phase4_binder_ui_pure.py --label review-pure
python tools/run_phase4_binder_ui_native.py --label review-native
python tools/run_phase4_binder_ui_native.py --label review-gui --render
```

Do not run the two native commands concurrently: they intentionally share one isolated project. Explicit source lists and SHA-256 manifests are retained per run; native-only staging removes only this worker's stale fixture scripts. All controlled TMP, build, fixture, and evidence paths stay under `.artifacts`.

## Parent integration gates still required

1. Attach `BinderPresentation`, subscribe `Command`, and publish fresh safe DTOs from the actual projection/physical/profile services. The service alone enforces the strict real 101 target, no spoilers, whole-collection completion, independent physical buckets, and validation.
2. Wire accepted table Binder open/close/input gating without changing the pending explicit-results gate; preserve pause and volume/audio behavior. Opening the binder must not reveal, generate, award, or write anything.
3. Resolve condition/quantity drafts on host selection/mode changes. Forward exact full IDs and untrusted quantity text into host validation, never infer physical holdings from virtual rows.
4. Run backup/preview/confirmation through the actual frozen profile APIs. Bind the owned immutable reviewed receipt, not mutable UI text. Publish busy/errors/recovery notices and acknowledge local pending requests correctly.
5. Verify actual candidate opening-to-binder, manual physical edit/reload, backup, read-only preview, clone confirmation, old-profile preservation and next-launch activation. This UI-only fixture proves none of those persistence outcomes.
6. Complete independent review and the user gate. No tracked work was closed and no Phase 5 work was started.
