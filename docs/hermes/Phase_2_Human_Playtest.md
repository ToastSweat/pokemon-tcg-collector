# Phase 2 human playtest — accepted

Status: ALL FIVE ITEMS PASS / USER ACCEPTED. User reported “all PASS” in direct response to the delivered five-item checklist. Acceptance is limited to that checklist and the pinned Phase 2 candidate; it does not establish final art/audio, exact factory collation, measured sustained performance, or unpresented recovery cases. Phase 3 was subsequently authorized in a separate user reply; this does not expand what this playtest accepted.

## Launch

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase2Windows\VintageCardCollector.exe`

Keep the complete directory together and inside the approved project root. Alternatively open `Assets/VintageCollector/Scenes/Bootstrap.unity` and Play in Unity 6000.2.14f1. Editor/player saves are separate; use one for this checklist.

This build is deliberately silent and uses clearly labelled generic artwork/wrapper placeholders. Please judge function/readability, not final Pokémon art, sound or foil realism.

## One bounded test group

The following previously delivered group is accepted as **all PASS**; no retest is requested by this documentation update:

1. **PASS — Opening and inspection:** Generate/Open, partial reveal, inspection and Back/Escape; no precision drag required.
2. **PASS — Quit/resume:** Same incomplete pack/order/reveal count after reopening; no startup regeneration.
3. **PASS — Results and awards:** Eleven ordered results, sensible NEW/DUPLICATE labels and no per-reveal/completion awards; physical tracking stays not configured.
4. **PASS — Controls and window:** Escape/Pause, explicit Resume, switching away/back and resizing; controls remain reachable.
5. **PASS — Feel/readability:** Placeholder timing, click behavior, labels and readability are comfortable enough to continue. Silence remains expected, not audio acceptance.

Do not intentionally corrupt/delete your save to test recovery. Automated tests already exercised failure, hard kill and Retry in isolated directories. If an ordinary storage error occurs, use Retry or Quit and report the message without clearing files.

## Record of acceptance

User report: **“all PASS”**. Recorded 2026-10-30 at 19:27 CDT. Parent rechecked all 135 non-save build files against the tested manifest before binding acceptance; bytes and membership were unchanged. Save folders were excluded and untouched. Accepted implementation checkpoint: `d649e46`; manifest digest: `29ed47956943fbb520133e2d5044e41e2102f22e11a5ad182b153c74dc19e271`.

This accepts the actual preceding five-item group, not a generic scope approval. Keep this candidate as the accepted behavioral baseline. Do not mark final branded visuals, audible sound, exhaustive storage-failure/power-loss guarantees or performance benchmarks human-approved. See ../verification/Phase_2_Human_Acceptance.json for the structured record.

Machamp's collection-target exception remains unanswered and need not block this opening test. No binder, physical editing, marketplace or financial work is part of this checkpoint.
