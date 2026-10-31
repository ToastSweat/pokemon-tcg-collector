# Acknowledged reveal audio integration

Scope/API: [Reveal_Audio_Contract.md](Reveal_Audio_Contract.md).

## Implemented renderer boundary
`TabletopPresentation` keeps the accepted single, non-looping, non-spatial child Foley AudioSource and existing master volume. The optional `IRevealSoundMedia` provider is separate from the original media interface, so absent providers retain generic action Foley.

Only `NotifyCommitted("next")` with a compatible acknowledged poststate passes `revealedCount` and `cards[revealedCount-1].kind/finish` to the provider. No concealed future card is inspected for music selection. A null optional clip falls back to the existing generic action clip. A provider does not require the optional image library to be available.

The existing transaction/action/revealed-count key is consumed before pause/focus/mute/binder suppression; removing suppression or replacing the provider does not replay a consumed reveal. Provider replacement stops the old cue without clearing acknowledgement history. Both collection input-block and visible-overlay states suppress new cues and stop current playback, including the brief ordering boundary before both flags propagate.

`skip` retains one generic skip sound; no pull/fanfare catch-up. Inspect/Back/explicit results/hover/view publication do not call the reveal provider. Failed write/error is not an acknowledged reveal; a later successful absolute-target Retry can cue once. The final ordinary-table reveal11 remains an ordinary assigned pull, not a forced modal or results sound.

## Test-first evidence (managed boundary, not native playback)
Runner: `tools/run_reveal_audio_renderer.py`. It compiles exact extracted current production acknowledgement/suppression/property members with explicit managed AudioSource/AudioClip boundary doubles under the bundled Unity Mono compiler, warnings as errors. It pins inputs before/after and writes only project-local artifacts. This is deliberately NOT represented as a native Unity engine, audio-device or controller-persistence test.

| Slice | Actual RED | Actual GREEN |
|---|---|---|
| Select already-acknowledged assigned clip | `parent-assigned-red-01`:1check/1failure | `parent-assigned-green-02`:1/0 |
| Binder-block suppression consumes silently | `parent-binder-red-03`:1/1 | `parent-binder-green-04`:2/0 |
| Visible overlay stops ongoing cue | `parent-overlay-red-05`:1/1 | `parent-overlay-green-06`:3/0 |

All labels are under `.artifacts/reveal-audio-renderer/`. Expanded regression `parent-renderer-final-07` passed20named groups: current metadata only; all11positions; duplicate/next target; mute/pause/focus/overlay suppression and no catch-up; generic fallback; legacy absent provider; independent optional media; skip; inspect/back; failed-write then successful retry; provider replacement; silent startup binding; new transaction; invalid projections; master volume/stop. This does not replace full-source native tests or actual player's persistence/reload proof.

## Regressions already executed
- `.artifacts/phase2-persistence/reveal-audio-legacy-persistence-01`:110checks passed, protected domain/data pins unchanged.
- `tools/run_phase4_core.py --label reveal-audio-core-01`:112checks passed.
- `tools/run_phase4_binder_ui_pure.py --label reveal-audio-ui-01`:101checks passed.
- `tools/run_tilt_surface_pure.py --label reveal-audio-surface-01`:215checks passed.

## Assembled evidence and remaining gate
Actual imported profile/clip references, full production renderer native playback, separate Windows player and guarded gameplay regression have been executed; see [Reveal_Audio_Build_Player.md](Reveal_Audio_Build_Player.md) for exact candidate/source/compiler and preservation maps. The independent runtime/audio review passed. Both requested verification corrections were rebuilt and exercised in all five fresh native suites; the corrected build rereview passed with no security or logic findings. Historical rejection is retained separately. Human hearing/musical taste remains the only focused feedback gate, not an automated PASS.
