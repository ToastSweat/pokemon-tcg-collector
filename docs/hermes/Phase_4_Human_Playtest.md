# Phase 4 — focused human review

Status: **ALL FIVE CHECKS PASS — direct user report “all pass.”** Acceptance is recorded in `../verification/Phase_4_Human_Acceptance.json` after rechecking all136immutable player files without traversing saves/logs. The accepted build was not rebuilt or launched for this record. This does not claim the user forcibly interrupted a restore. No Phase5 authorization is inferred.

## Candidate

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase4Windows\VintageCardCollector.exe`

Keep its whole folder together and inside the project. Source scene: `Assets/VintageCollector/Scenes/BootstrapPhase4.unity`, Unity6000.2.14f1. This player uses its own collection under `Builds/Phase4Windows/Saves`. Older players/collections are unchanged; there is no automatic merge or migration from them. Do not manually overwrite a populated collection to import older progress.

Build: `current-controls-candidate-01`, actual Succeeded/errors0/warnings0. Full immutable136-member manifest is in the pinned corrected audit. Core/runtime correction review passed; corrected build/probe re-review is recorded separately. Current materials are original generic placeholders, not supplied/branded faces.

## One review group

Please answer **PASS / FAIL / NOT TESTED** for each item. Describe the exact screen/action for a failure. These checks can use the new candidate collection; you do not need to damage files, kill the process during a write, or inspect technical save contents.

1. **Opening flow preserved.** Generate/open a pack and reveal all11 normally. Card11 stops on the ordinary opening table like the others, not forced fullscreen or directly replaced by results. Open Binder and return; card11 remains held. **View pack results** continues explicitly; result cards remain inspectable and show separate virtual NEW/duplicate and physical OWNED/MISSING labels. Optional inspection/Back and Skip remain usable.
2. **Binder clear and readable.** Use Binder/Tab, both collection modes, search, filters and paging. Both targets are101 unstamped printings with no Machamp slot. Virtual counts include only revealed pulls while a pack is incomplete; quantities and completion should be understandable. Back, Quit, labels and selected mode should remain readable at your usual window size.
3. **Physical ownership independent and persistent.** Select an unowned physical printing, set unknown-condition quantity2 and confirm. Add near-mint quantity1 for that same printing and confirm: total3, still one unique completion slot. Set unknown quantity0: only the near-mint1 remains. Virtual holdings do not change. Quit/reopen: the confirmed physical holding persists.
4. **Backup / review / restore.** Create a backup of that near-mint1. Change its quantity to2 and confirm. Select the earlier backup and review, then Cancel: quantity2 remains. Review again and explicitly confirm. It must explain **new profile / next launch / not a merge or overwrite** and require restart. Quit/reopen: the restored profile has quantity1 and the backed-up pack history/reveal progress. The prior profile's files remain preserved.
5. **Comfort / original behavior.** Mouse and keyboard feel responsive, no trapped panel or accidental table clicks through Binder, and opening/inspection/pause/volume behavior still feels acceptable. Report readability or timing concerns instead of assuming automated screenshots establish comfort.

## Known limitations to review explicitly

- All printed art is still generic/replaceable. Actual supplied/branded material integration remains deferred.
- If the app is forcibly stopped during an unfinished restore, it may refuse startup while preserving the old collection, backup and pending restore evidence. Same-process **Retry SAME reviewed restore** and normal confirmed restart work, but there is no cross-process recovery wizard yet. **Do not delete pending files, profiles or Saves** to get past that screen; ask for scoped recovery assistance. If this limitation is unacceptable, say so before accepting Phase4.
- Restores create new profiles; they are not merges. There is no profile-picker UI or shopping/finance functionality in this phase.
- Completion covers only the selected1999 English Base Set Unlimited unstamped101 printings. Other editions/variants/sets are not silently combined.

## Recording acceptance

No human PASS is inferred from implementation, independent review, normal startup, or synthetic fixtures. Before attaching later acceptance, recheck all136immutable player members and hashes while pruning `Saves`/`Logs` before traversal. Do not rebuild or launch automation against the user's real collection for an acceptance-only update.
