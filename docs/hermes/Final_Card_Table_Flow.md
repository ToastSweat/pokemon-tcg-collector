# Final card — normal opening-table stop (clarified requirement)

Status: native execution and independent review passed; checkpoint `61f6f52`. This supersedes the automatic inspection/modal workaround in 51b4a8c. That workaround was my misunderstanding, not the user's intended flow. The user's conditional continuation permission is now fulfilled by the verified corrected behavior; no unreported human retest is invented.

## Current process / requested behavior

User clarified: the last card should look like every other card on the opening table, with a stop to view it, not an extra fullscreen view. User passed the rest and conditionally authorized continuation after this correction. Do not invent a human pass on newly changed bytes or broaden continuation to unapproved roadmap work.

## Projected / implemented change

- The 11th ordinary Next persists reveal count 11 and leaves the normal `revealing` table, same hero-card rectangle, `inspectIndex=-1`. No automatic modal or grid.
- Primary button becomes **View pack results**. Its presentation-only `results` command clears the transaction-local final-table hold and shows the grid without a save, roll, award or sound.
- Clicking the card to inspect is still optional, exactly like earlier cards; Back returns to the same final table.
- Skip stays direct to results. Last Next still cues once; final save failure exposes no unsaved face, Retry returns to the same normal table.
- No save-schema/model/probability/award changes. The hold is presentation-only for the current runtime transaction; reopening a completed pack still shows results without replay, consistent with the prior completed-resume baseline.

## Real evidence

- Targeted native test observed RED against the modal workaround, then GREEN with **14 named checks**.
- Existing controller regression updated only its stage expectation for the newly authorized final-table stop; **13 groups /297 assertions passed**, with all save/count/order checks retained.
- Separate Windows build: **zero errors and warnings**.
- Actual owned-window native mouse suite: **49 checks passed**; verifies table positions 1–11, no automatic modal/grid on card11, optional inspection/Back stays on table, explicit results, one final cue, unchanged award/revision and completed restart, and direct Skip. PNGs have verified CRC/dimensions.
- Parent visually loaded the normal 11/11 opening table with **View pack results** (no modal/grid).

Candidate: `Builds/Phase3TableFlowWindows/VintageCardCollector.exe`; own isolated collection. Earlier players and live saves are not rewritten/copied/reset. Source scene remains BootstrapPhase3.unity. Exact manifest/execution are ../verification/Final_Table_Flow_Player_Manifest.json and Final_Table_Flow_Execution.json.

Scope stays this corrected transition and necessary presentation gating/button/audio shape. Preserve all prior accepted sound/readability/input/resume behavior. Branded materials remain deferred and the Machamp target unresolved. Any next-phase work must respect the phased roadmap and unresolved actual decisions.
