# Phase 2 human playtest — pending

Status: NOT TESTED BY USER. Implementation, automated native-player execution and independent review passed; none substitutes for human feel/hardware acceptance. Phase 3 remains unapproved.

## Launch

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase2Windows\VintageCardCollector.exe`

Keep the complete directory together and inside the approved project root. Alternatively open `Assets/VintageCollector/Scenes/Bootstrap.unity` and Play in Unity 6000.2.14f1. Editor/player saves are separate; use one for this checklist.

This build is deliberately silent and uses clearly labelled generic artwork/wrapper placeholders. Please judge function/readability, not final Pokémon art, sound or foil realism.

## One bounded test group

Reply **PASS / FAIL / NOT TESTED** per item (a short description/screenshot for failures is enough):

1. **Opening and inspection:** Generate a Base Set Unlimited pack, Open it, reveal a few cards, inspect a revealed card, and return with Back/Escape. Nothing should require dragging or precision gestures.
2. **Quit/resume:** While only a few cards are revealed, Quit and reopen the same player. The same pack, visible card order and reveal count should resume; no new pack is generated merely by restarting.
3. **Results and awards:** Skip the rest. All eleven positions should appear in original order with sensible NEW/DUPLICATE labels. Virtual quantity increases once per generated pack, not per reveal/inspection/completion. Physical tracking must remain labelled not configured.
4. **Controls and window:** Test Escape/Pause, explicit Resume, switching away/back, and resizing (1280×720 or larger recommended). Back/Resume/Quit must remain reachable. Tiny-height windows use smaller typography.
5. **Feel/readability:** Report whether the placeholder flow is comfortable enough to continue, plus any unwanted delays, missed/double clicks, confusing labels or layout problems. Silence is expected; audio/material polish is Phase 3.

Do not intentionally corrupt/delete your save to test recovery. Automated tests already exercised failure, hard kill and Retry in isolated directories. If an ordinary storage error occurs, use Retry or Quit and report the message without clearing files.

## Record of acceptance

No human item is marked passed yet. A generic scope approval is not evidence that unreported physical subchecks were performed. Record the tested build manifest digest with any future acceptance: `29ed47956943fbb520133e2d5044e41e2102f22e11a5ad182b153c74dc19e271`.

Machamp's collection-target exception remains unanswered and need not block this opening test. No binder, physical editing, marketplace or financial work is part of this checkpoint.
