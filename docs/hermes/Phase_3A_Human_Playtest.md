# Phase 3A generic-media playtest — pending

Status: PARTIALLY ACCEPTED — user passed the rest and reported one final-card transition defect. The tested checkpoint is 5c4566a. Preserve accepted sound/mute/readability/resume/overall behavior; only the 11th ordinary-reveal preview is reopened. Generic Phase 3A is not fully closed, nor are real materials/Phase 4 approved.

## Use the separate candidate

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase3Windows\VintageCardCollector.exe`

Keep its whole directory together and inside the approved project root. **It uses its own new collection in its own Saves folder.** Your accepted Phase 2 player/collection are unchanged and are not automatically copied or merged. Do not delete or move either save directory for this test.

The real supplied manifest contains zero assets. Expect generic geometric art and original synthesized foley, not authentic card/wrapper artwork or recorded vintage-pack sounds. No branded material is being claimed as ready.

## One bounded group — PASS / FAIL / NOT TESTED

1. **Committed sound:** Generate, Open and reveal a few cards. Do you hear short, comfortable cues once per successful action, with no startup sound or unwanted bursts? Start at modest volume.
2. **Mute/pause:** Change volume and Mute in Pause. Resume and reveal while muted—it should remain silent. Unmute/Resume should not replay missed cues; the next new action should cue normally. Focus loss should pause/stop sound and require explicit Resume.
3. **Visual readability:** Inspect a revealed card and return. Are text, subtle shadows/motion and bounded generic foil cues comfortable and readable? No full-face rainbow, moving hitbox or action delay should interfere.
4. **Accepted flow:** Skip/results still preserve all eleven positions and NEW/DUPLICATE behavior, without another award. Click/Back/Escape/Resize remain usable without precision dragging.
5. **Resume:** Quit partway through a candidate pack, reopen, and check the same pack/order/progress. Reopening should not replay opening sounds, generate a replacement or add awards.
6. **Overall quality:** Report whether generic sound volume/timbre/timing and restrained visual treatment improve or distract from the accepted Phase 2 flow; note missed clicks, clipping, uncomfortable sound or performance issues.

Do not intentionally corrupt saves; automated failure/Retry/hard-kill checks ran only in isolated test folders. If a real storage error appears, Retry/Quit and report its message without clearing files.

## Acceptance record

User reported everything working great, passed the rest, and identified that the 11th card goes directly to the grid without a large preview. Items 1,2,3,5,6 are PASS within the originally presented scope; item4 is PASS except the explicitly reported final-card transition, which remains open. This is not a blanket failure or permission to rework accepted audio/material timing.

Parent rechecked all 136 immutable files against manifest `c363cf2242c5e5b91089967f13ed5be67bf66284dc1175e6f8ba0cc659e21db9` on 2026-10-30 before recording feedback; live saves were neither read nor changed. A separate fix candidate must receive its focused retest, without transferring acceptance automatically. See Final_Card_Preview_Fix.md. Real branded materials remain deferred.
