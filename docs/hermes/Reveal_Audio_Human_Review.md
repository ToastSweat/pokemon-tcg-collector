# Reveal audio — focused listening review

Status: **ACCEPTED / COMPLETE / FROZEN.** Direct user feedback: **“sounds perfect. workds great”**, responding to the delivered buildup/rare-outcome listening handoff. This is overall audio acceptance, not an invented separately itemized physical-control checklist. Implementation `e1d78d4`, corrected `native-build-fixed-14`; all137immutable player members were rechecked unchanged without launching, rebuilding or inspecting live saves. See `../verification/Reveal_Audio_Human_Acceptance.json`. Earlier accepted art, tilt, lighting, opening flow and binder passes remain accepted. The guide below is retained for reference.

## Start

Run the complete separate player folder:

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase3RevealAudioWindows\VintageCardCollector.exe`

Keep its dependencies beside the EXE. Normal play uses this candidate's own `Saves` folder; no earlier collection has been automatically copied, merged or migrated. Existing accepted players and saves remain unchanged.

## What changed

- Eleven independently assignable pull-position sounds. Original short musical placeholders rise toward the rare slot, then relax for the remaining pulls.
- Three distinct rare overrides: non-Pokémon, Pokémon non-holo, holo. The existing Base Set Model A physical order still places the rare at **position8 of11**; nothing has been moved to the end.
- Existing master volume/mute, pause and focus controls apply. A new cue replaces the previous one rather than layering an unbounded stack; the game does not make you wait for music.
- Skip retains one generic cue, not eleven fanfares. No startup/reload/inspect/Back/results replay. Save failure is silent; an acknowledged Retry can play the reveal once.

## Quick comparison without hunting random rares

`.artifacts/reveal-audio-preview/Rare_Fanfare_Preview.wav` concatenates the actual three source clips with quiet gaps, in this order:

1. Non-Pokémon rare — compact chime answer.
2. Pokémon non-holo rare — brighter rising gesture.
3. Holo rare — wider flourish and sustained tone.

This comparison file is not substituted for native playback evidence; the actual player separately played all14imported clips, including all three real generated rare outcomes. No guaranteed-rare/debug controls were added to ordinary play.

## Focused feedback

Please use **PASS / FAIL / NOT TESTED**; only these new sound checks are open:

1. **Progression:** ordinary pulls build anticipation toward reveal8 without becoming grating or obscuring the card-opening feel.
2. **Rare distinction:** the three stingers are recognizable and the holo feels like the largest payoff. Placeholder composition/timbre can be replaced later.
3. **Control:** overall volume is comfortable; mute/pause work and inspection/Back do not repeat the fanfare. Advancing quickly may intentionally replace the current one-shot.

## Assign your own sounds in Unity

Select `Assets/VintageCollector/Audio/DefaultRevealAudioProfile.asset` in the Inspector. **Pull Sounds** elements0–10 correspond to reveal positions1–11. The rare-specific fields override element7 at position8. Clearing a rare override falls back to its position clip; clearing that falls back to the existing generic reveal Foley.

Import replacement AudioClips into a project-local `Assets` folder and drag them into the relevant fields. `BootstrapRevealAudio.unity` is already wired to this profile. Save and rebuild to include changes in a standalone player; this is not a runtime loose-file/music-import UI. Detailed authoring and provenance: `../technical/Reveal_Audio_Profile.md`.

## Next checkpoint

Continuation approval is recorded. The next planned expansion checkpoint is research for **English Jungle Unlimited and English Fossil Unlimited** under the original handoff. Their probabilities, ordering, identities, variant completion and mixed-set persistence still require independent research/validation; Base Set assumptions are not automatically approved for those sets. No shopping, marketplace or finance scope is opened.
