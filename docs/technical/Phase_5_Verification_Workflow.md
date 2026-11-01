# Phase 5 native verification workflow and pitfalls

These are reusable project-local notes, not permission to modify another Hermes profile or write outside this project.

## Exact-source release cycle

1. Promote only exact-hash reviewed source/data/texture payloads, with backups for existing files. Preserve source MonoScript GUIDs. Native-import missing metadata in an isolated owned project and adopt only genuinely new native files; never overwrite existing metadata to make references pass.
2. Before any mutation, recursively admit the **entire** owned stage/candidate write tree, including Library, Temp, Logs, UserSettings and nested Managed. Reject reparse/nonregular descendants without following targets. Keep populated-candidate-Saves refusal before the scan; prune Saves/Logs before traversing accepted players for preservation.
3. Generate an explicit parent authorization from the current Root compiler map, bindings, tool pins and protected-player hashes. A successful component fixture does not authorize native release work or prove promotion occurred.
4. Prepare native profiles/textures, then the scene, then the release. Validate all references after reopening the saved scene. Freeze the effective on-disk release recipe **before** compiler-manifest admission. In particular, EditorBuildSettings must already serialize the intended scene path and GUID; a BuildPipeline-side flush can otherwise change the disk manifest after the declared-input report.
5. If corrected Root code makes an older owned stage stale, retain the old stage intact and use a fresh canonical stage. Do not weaken stale-code refusal or silently delete evidence. A strict closure refusal remains a failed release receipt even if Unity's internal BuildReport says Succeeded.
6. Reconcile complete Root/stage/compiler/player membership and bytes, not just a source subset or the EXE. The reviewed Phase 5 release is `n07`, with 546 compiler inputs and 138 immutable player files.

## Native assertion semantics

- Require the exact declared `DefaultExecutionOrder`. `MonoImporter.GetExecutionOrder` returns the custom importer override: accept zero or the exact required value, reject mismatched nonzero overrides. Attribute resolution is not actual callback-order evidence; require a player/lifecycle receipt separately.
- `Component.SendMessage` broadcasts to scripts on the same GameObject. It does not mean “invoke only this component.” For an explicitly labelled internal presentation-focus experiment, invoke the exact declared private Tabletop handler, fence its synchronous Pause command, and restore the prior fence in `finally`. Keep the real Next acknowledgement unfenced. Assert real reveal advancement and suppressed cue count, not merely an unchanged count while the game is paused. This is not physical-focus acceptance.
- Source-pinned input geometry must match the current UI stage. The results Generate button is not the opening sidebar primary button. Likewise, unpaused footer Quit bounds are not paused-dialog Quit bounds. Either observe actual current controls or derive the correct exact-built-source target and label that provenance honestly.
- `SetChoiceView` is an in-memory projection, not a JsonUtility transport. Its absence from serialized BinderView must not invalidate three complete current canonical binder-set repaint controls. Still reject missing, duplicate, foreign, disabled-inconsistent or nonpositive controls and stale DTO/render witnesses.
- Treat mutable engine/helper TEMP logs separately from immutable source and verification receipts. A persistent Unity helper can retain an earlier log destination. Disclose drift without restoring bytes to conceal it.

## Verified native commands

From this project directory:

```text
python -B tools/verify_phase5_player.py --label <fresh-label> --build-label n07 --mode input --size 1600x900
python -B tools/verify_phase5_player.py --label <fresh-label> --build-label n07 --mode input --size 1280x720
python -B tools/verify_phase5_player.py --label <fresh-label> --build-label n07 --mode audio --size 1280x720
python -B tools/verify_phase5_player.py --label <fresh-label> --build-label n07 --mode services --size 1600x900
python -B tools/verify_phase5_player.py --label <fresh-label> --build-label n07 --mode crash --size 1280x720
```

Each command refuses a current source/player mismatch and uses fresh isolated fixture stores. Do not reuse labels, point tests at live saves, or run native input against an unowned window. Run GUI jobs serially and keep per-message live-process/HWND/PID checks. Reconcile actual exits, native reports, unique check names, ordered history, acknowledged input postconditions, PNG CRC/IEND/dimensions and full immutable-player hashes afterward.

The final execution index is `docs/verification/Phase_5_Runtime_Execution.json`; exact source/build/driver approval is `Phase_5_Final_Independent_Review.json`. Native counts, native playback, injected background input and human acceptance remain separate evidence layers.
