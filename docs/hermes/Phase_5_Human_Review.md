# Jungle / Fossil — focused player review

## Launch

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase5Windows\VintageCardCollector.exe`

Keep the entire `Phase5Windows` folder together and inside the project. Editable scene: `Assets/VintageCollector/Scenes/BootstrapPhase5.unity` (Unity 6000.2.14f1).

This candidate starts its own collection under `Builds/Phase5Windows/Saves`. Earlier players and collections are preserved; there is no automatic import or merge. Do not delete saves to recover an error or copy raw save files between players. Deliberate transfer uses **Create backup → Review restore → confirm a new profile → Quit/reopen**, preserving the prior profile.

## What changed

- English Base Unlimited, Jungle Unlimited and Fossil Unlimited are selectable before generating a pack, including from the results screen. Selection alone does not generate cards or replace the current pack.
- Jungle and Fossil use the explicitly approved provisional J1/F1 models: eleven cards, seven commons, rare/holo at position eight, three uncommons, no Energy, one-third holo probability and weighted sampling without replacement. These are independent loose-pack models, not authenticated factory/box collation claims. Base generation is unchanged.
- Jungle/Fossil have temporary face templates and **three wrapper-front variants per set**. Appearance is transaction-stable and does not consume pack RNG. Variants are not promised to cycle every three packs.
- Both binder modes distinguish the three printings, with completion targets **Base 101 / Jungle 64 / Fossil 62**. Base still excludes Machamp. Physical ownership remains manual and independent of virtual pulls.
- Accepted Base art, pointer tilt, lighting and reveal audio remain in place. New-set profiles are `Assets/VintageCollector/Audio/Profiles/Phase5/JungleRevealAudioProfile.asset` and `FossilRevealAudioProfile.asset`; Base still uses `Audio/DefaultRevealAudioProfile.asset`. They initially reference the existing fourteen clips and are separately assignable. Position eight remains the rare cue.

## Three focused checks

1. **Selection and packs:** choose Jungle, generate/open a pack, then choose Fossil from results and generate another. Confirm the active set and temporary wrapper/face are correct. Selecting another set must not alter already revealed cards.
2. **Normal reveal flow:** reveal all eleven cards. Card eleven stays on the ordinary table until **View pack results** is clicked. Inspect/Back and generating the next pack should remain usable.
3. **Binder:** open Binder and switch Base/Jungle/Fossil. Confirm the 101/64/62 targets and separate virtual/physical views. A manual physical edit must not change virtual ownership.

Please report any failed check or unexpected behavior. Automated native input, playback, restart and crash tests passed; they are not a substitute for your physical-input/listening acceptance. Previously accepted presentation features do not need blanket reapproval.

Final card and booster scans are optional replacements later; temporary art is the authorized current presentation. No additional approval of the pack models is pending.

## Evidence

- Native release and five complete player runs: `docs/verification/Phase_5_Runtime_Execution.json` (build `n07`).
- Normal/faulting/reentrant headless native Quit: `docs/verification/Phase_5_Quit_Native_Execution.json`.
- Previous failed test-harness attempts and superseded snapshots remain under `.artifacts`; they have not been rewritten as passing runs.

Human acceptance is **PASS** for all three focused checks, explicitly reported by the user. The138 immutable `n07` player files were rechecked unchanged without launching/rebuilding or reading saves. See `docs/verification/Phase_5_Human_Acceptance.json`. Phase 6 shopping discovery is authorized; newly supplied Jungle/Fossil art remains separate from this accepted candidate until integrated and verified.
