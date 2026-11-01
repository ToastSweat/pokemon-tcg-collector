# Phase 5 normal Editor save isolation

Status: implemented in **parent source copies**, not live Assets or accepted saves. This is the explicit new Editor default permitted by the runtime contract, separate from the two completed controller fault-fix cycles.

## Reason and exact scope

Legacy `GamePaths.Resolve(..., isEditor:true, ...)` defaults to `Saves/Phase2`. Reusing it in the new Phase5 Editor scene would open legacy development progress. Standalone Phase5 already has its own build-adjacent Saves; this addition protects ordinary Editor Play too.

The parent staged `GamePaths` adds `ResolvePhase5` and a shared private `ResolveCore`. Public legacy Resolve still selects the exact original `Phase2` default. ResolvePhase5 selects fixed `Phase5` for normal Editor only. Standalone paths, explicit smoke scenarios/flags, profile IDs, containment, reparse refusal, validation order and no-directory-creation behavior remain unchanged. No new command-line override or stored field is introduced.

Exactly three existing integration path-call sites use it: Phase5 OpeningGame branch (legacy branch remains old Resolve), Phase5CollectionHost before its container lease, and Phase5MediaHost's path readback. Source maps, originals and hashes are at `.artifacts/phase5-parent-paths/handoff.json`; the exact delta is `source-fix.diff` there. Original controller handoff/Root Assets remain untouched.

## Actual evidence

- `.artifacts/phase5-parent-paths/evidence/editor-red-01`: missing explicit Phase5 path API assertion, actual exit1.
- `editor-green-02`: new API/normal save isolation passes.
- `editor-regression-03`: **6 groups passed**, including legacy and standalone defaults, explicit smoke, profile non-nesting, unknown/override refusal, read-only resolution and refusal to consult/modify a synthetic incompatible legacy destination.
- `routing-red-04`: exact old controller path statement executed with the real path policy selects legacy Editor saves, actual failure.
- `routing-green-05`: exact updated controller/host/media path assignments execute with the real GamePaths and preserve legacy routing. Application/config objects here are explicitly labelled test boundaries; this is not a full native host/store test.
- Parent immutable `.artifacts/phase5-assembled-source-03/compile/results.json`: actual **60 runtime C# files / 31 source overlays** compile against installed Unity assemblies, warnings-as-errors, exit0. No contract doubles in that compile. Earlier snapshots01/02 are preserved, not overwritten.

Tests: `TestsPhase5/RuntimeEditorPathChecks.cs`, `RuntimeEditorRoutingTests.py`. No real legacy save was enumerated or read. Independent path-feature review and actual native ordinary-Editor admission remain separate gates; final player/scene must match the final accepted source snapshot.
