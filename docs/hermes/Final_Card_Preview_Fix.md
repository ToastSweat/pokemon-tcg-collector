# Final-card preview — focused Phase 3A fix

Status: implementation, native execution and independent review passed; committed as `51b4a8c`. Focused human retest is pending. Preserve user PASS feedback on the rest of Phase 3A. Do not start branded-material work or Phase 4.

## Current process / cause

User reported that the 11th ordinary reveal skipped the large preview and immediately displayed all eleven results, while the rest passed. Parent confirmed OpeningGame.PublishView maps any fully revealed durable pack to results; no final preview was selected. Save/generation behavior was not defective.

## Projected / implemented smallest change

Only OpeningGame.ExecutePending adds a final ordinary-Next presentation step: after the save acknowledgement, ordinary result publication and existing one-shot cue, set inspection to the saved last index and republish in the same synchronous call. The existing large inspection modal takes focus over the dimmed, disabled results grid. Back/Escape closes it and exposes the usable results view. Skip stays direct. A failed final save shows no unsaved preview; successful Retry shows the same last card and cues once. Completed-pack restart stays at results without replay. No save-schema, generation/model, transaction/archive or media/renderer changes.

Candidate is separate: `Builds/Phase3FinalCardWindows/VintageCardCollector.exe`, with its own new Saves folder. Existing Phase3Windows player and live collection remain untouched. This is a focused test candidate, not migration or data reset.

## Real verification

- Native Unity RED reproduced missing last-preview and final-Retry-preview assertions, with Skip already passing.
- Minimal controller patch then passed **11 named native assertions**: ten-card normal reveal, saved exact card 11 large preview, one final cue, duplicate input/no award, Back/no save, Skip direct, failed-save secrecy, final Retry identity/cue, and completed resume silence.
- Unchanged controller regression: **13 groups /297 assertions passed**.
- Separate Windows x64 Mono/D3D11 build: **0 errors, 0 warnings**.
- Actual owned-window native mouse run: **46 checks passed**, all eleven ordinary Next actions, last inspection, Back to grid, completed process restart, direct Skip, and actual PNG CRC/dimensions. Parent visually inspected the 11/11 modal and subsequent all-eleven grid.
- Evidence: `.artifacts/phase3-build-project/.artifacts/final-card-fix/results.json`, `.artifacts/logs/final-card-{red,green}.log`, `.artifacts/final-card-fix/build-report.json`, `.artifacts/final-card-player/native-01/verification.json`.

The original interactive Editor was not closed; exact-source stage was used. No existing player/save was rebuilt or rewritten. This seven-line behavior patch intentionally does not persist modal presentation state or add a new command/button; it reuses the accepted inspection view.

## Focused human retest after review

1. Reveal normally through card 11: the large 11/11 inspection should remain until Back/Escape.
2. Back shows all eleven results, with no extra quantity/cue/award.
3. Skip still goes straight to results; prior accepted sound/input/resume behavior remains baseline.

Reply PASS / FAIL / NOT TESTED for this focused group. Do not repeat unrelated already-passed checks or intentionally damage saves.
