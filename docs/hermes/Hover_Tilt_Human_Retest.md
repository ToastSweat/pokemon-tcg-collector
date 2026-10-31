# Hover tilt / catching-light correction — focused human retest

Status: **APPROVED — direct user feedback: “good shit. looks great thank you. i approve of this”.** The focused booster/card/holo tilt and feel gate is accepted for build36/commit0837c6c. All136immutable player members were rechecked unchanged without save/log traversal, rebuild or player launch; see `../verification/Hover_Tilt_Human_Acceptance.json`. Prior supplied-art and other functionality PASS remain preserved. No Phase5 scope is inferred.

## Separate player

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase3TiltWindows\VintageCardCollector.exe`

Keep its whole dependency folder under this project. It uses its own collection/save root; previous Phase4/materials players and collections remain preserved. Source scene: `Assets/VintageCollector/Scenes/BootstrapTilt.unity`. Normal pointer movement is the gesture—**no click, drag or scroll wheel is needed** to tilt.

## Three focused checks — PASS / FAIL / NOT TESTED

1. **Booster:** Move the pointer across the pack left/right and up/down. It should visibly rock in perspective and catch silver light in the supplied metallic regions. Moving away eases it back toward neutral, without generating/opening anything.
2. **Cards / holo:** Move across an ordinary revealed card and an inspected holo. All cards tilt; ordinary cards stay matte, while the holo illustration catches a moving color/light response tied to the angle. Printed HP/attacks/copyright stay readable, and the effect does not cover unmasked text/borders. If no holo has been observed, mark that part NOT TESTED rather than a failure.
3. **Feel / unchanged control:** Motion is smooth and comfortable, returns on leave, and does not interfere with clicking to inspect, Back, revealing or the normal card11 table stop. Pause/focus-loss/Binder freezes it and resume does not replay sound. Report excessive strength, weak reflection, jitter or clipping specifically.

This is a focused retest, not a request to redo accepted ownership/import functionality. Automated actual-player checks separately exercise those regressions. The effect uses a perspective-projected plane and normal/view-dependent virtual softboxes; it is not only the old time-driven overlay. Old supplied images, metadata, IDs, odds and101targets stay unchanged.

Current candidate build: `parent-tilt-build-36`; full current manifests/reports are `../verification/Tilt_Player_Manifest.json` and `Tilt_Player_Execution.json`. Implementation/native tests and independent reviews do not substitute for this human perception/feel gate.

The prior documented restore limitation still applies; no profile-picker/recovery wizard is added in this presentation correction. Do not delete save/pending/profile files to reset an error. Optional collection transfer should use an exported ZIP and explicit reviewed new-profile restore, not raw-save overwrite.
