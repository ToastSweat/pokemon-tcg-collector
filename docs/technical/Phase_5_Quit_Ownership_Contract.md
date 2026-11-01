# Phase 5 Quit ownership — user-authorized implementation contract

## Authority and current process

The user explicitly answered **“Yes—make that fix and continue”** to centralizing Quit ownership, verification and continuation toward the playable three-set build. Authority: `docs/verification/Phase_5_Quit_Ownership_Approval.json`. The earlier unanswered escalation and rejected controller snapshots remain historical evidence; the pause is now lifted for this bounded redesign and previously approved Phase5 integration.

The previous design had two independent process-exit callers: Game and Binder host. Reentrant Binder Quit during Game shutdown could request exit before session/container/media cleanup and duplicate the outer exit. Two narrow fixes did not resolve ownership. Numeric models, catalog identities, stores, art and audio are not the cause and are not reopened.

## Projected behavior — one owner

- `OpeningGame` alone owns and attempts `Application.Quit`.
- Binder Quit forwards intent to Game; it neither owns exit nor treats a nested local Shutdown return as global completion.
- A minimal public `RequestQuit()` seam is permitted. Reentrant Game/Binder requests coalesce into the same intent; an ordinary Shutdown may receive such intent without exiting before its global cleanup finishes.
- Non-editor exit is attempted at most once, **after** all mandatory physical/session/container/media cleanup and completion-handler attempts. Mark the exit attempt before calling Unity so an exception/reentry cannot duplicate it.
- Editors request no process exit. Normal explicit player Quit requests one; repeated/closed callbacks do not repeat it.
- Preserve cleanup order, exactly-once resource attempts, borrowed native-resource ownership, existing legacy nested-host compatibility, and every thrown cleanup/exit failure without masking.
- No generation, reveal, RNG, award, save/schema, export, selection, media-context or cue side effect is added by Quit.
- Preserve the separately approved Editor path scope: normal Phase5 Editor uses `Saves/Phase5`, legacy uses `Saves/Phase2`; standalone/smoke/profile rules remain unchanged.

## Source isolation and proof

Implement only new copies under `.artifacts/phase5-quit-owner-work/`, based on immutable `.artifacts/phase5-assembled-source-03`. Change only Game/controller and Phase5CollectionHost unless a strictly necessary minimal helper is reported. Root Assets and prior snapshots/reviews remain untouched until exact-source review passes. Never overwrite snapshot03; create a new combined source snapshot after the redesign.

Observe a genuine failing reentrant-Quit test before production changes. Exercise both entrypoints/outer Shutdown paths, early/late and throwing lifecycle callbacks, disposal/exit faults, repeated requests, normal/editor cases and side-effect counters. Carry forward original16, targeted18, ordinary/adversarial12+12 and reentry6 boundary groups. Clearly label contract doubles; compile the real combined source against installed Unity assemblies independently.

Final gates remain actual native source-byte/JsonUtility admission, real store/restart/import/media behavior, seven-component scene/profile bindings, actual process exit and separate Phase5 Windows player verification. A source or managed-boundary pass is not a native/player or human acceptance pass. Existing models/core/UI component approvals remain applicable only through unchanged source hashes.
