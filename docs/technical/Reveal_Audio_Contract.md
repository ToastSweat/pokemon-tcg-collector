# Assignable reveal audio — implementation checkpoint

## Current process
- Accepted presentation is tilt build36 / implementation0837c6c; human acceptance e849e12. Preserve that player, original supplied images, old manifests, other accepted players and all live saves.
- `TabletopPresentation.NotifyCommitted` receives acknowledgement-only controller notifications, consumes a transaction/action/revealed-count key before audio suppression, and uses the existing single non-looping Foley AudioSource.
- Base Set Model A has11positions: commons1–5, energy6–7, rare/holo8, uncommons9–11. No card-trick/reordering is requested.

## Projected changes / exact scope
- User explicitly requested11individually assignable reveal-position clips and three rare-slot placeholder musical fanfares: non-Pokémon, Pokémon non-holo, holo. Add this reusable presentation slice before Jungle/Fossil expansion. Continuation approval does not waive research/model gates for new sets.
- Original, locally synthesized placeholders: light growing musical pulses toward position8; distinct escalating rare outcomes; gentler post-rare pulls. No external generation service, downloads, uploads or borrowed soundtrack.
- Unity-native `RevealAudioProfile` ScriptableObject exposes `AudioClip[] pullSounds` (exactly11), `int rareRevealPosition` (default8), `AudioClip nonPokemonRare`, `pokemonRare`, `holoRare`. Rare outcome overrides the position clip; missing rare clip falls back to that position, then existing generic next Foley. Clips can be replaced in the Unity Inspector. No runtime file picker/import/settings UI is required.
- Optional additive interface in `VintageCollector.Media`, leaving IPresentationMedia unchanged: `IRevealSoundMedia { AudioClip RevealSound(int oneBasedPosition, string kind, string finish); }`.
- Profile API type: `VintageCollector.Audio.RevealAudioProfile`; host type `VintageCollector.Audio.RevealAudioHost`, serialized public field `profile`. Host binds the optional provider to the co-located TabletopPresentation, never generates/saves/awards/plays on startup, and detaches only its own binding on destroy.
- TabletopPresentation adds `public IRevealSoundMedia RevealSounds {get;set;}` with StopFoley on provider replacement. Only acknowledged `next` passes already-revealed card at `revealedCount-1` to provider. Holo finish selects holo; otherwise kind `pokemon` selects Pokémon; other catalog kind selects non-Pokémon. Non-rare positions use their assigned clip irrespective of rarity/kind/finish.
- Reuse existing volume, focus, pause, save-error, binder-block, stop and duplicate-consumption gates. Skip remains one generic skip cue, never catches up reveal/fanfare cues. Inspect/back/results/resume/reload/hover never trigger music. Failed save is silent; successful retry cues once.
- No edits to catalog, RNG, model, pack ordering, save schema, collection completion, controller award authority or accepted scene/builds. Existing historical scenes lacking optional host retain generic audio.
- New source scene `Assets/VintageCollector/Scenes/BootstrapRevealAudio.unity`; separate player `Builds/Phase3RevealAudioWindows/VintageCardCollector.exe`, isolated save root. New owned exact-source build stage `.artifacts/reveal-audio-build-project`; preserve canonical pre-mutation admission, source/metadata/compiler closure and owned-window-per-message input guards from tilt tooling.
- All project-controlled files/fixtures/temp/logs remain within `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG`; ordinary Unity-managed licensing/caches/logs already allowed. Leave user-owned untracked ProjectSettings/PackageManagerSettings.asset and `Pokemon Cries/` alone; no cries integration requested in this slice.

## Work ownership
- Audio worker: new Audio source/profile/host, original placeholder generation/assets, additive media interface, own tests/tools/docs. Do not edit TabletopPresentation, old scenes/build tools or controller.
- Build worker: new guarded RevealAudio build policy/tool/Editor bootstrap + source scene, isolated stage/native audio verification/player verifier. Do not edit audio provider implementation or TabletopPresentation. Wait for assembled sources before final build; retain real failure outputs.
- Parent: renderer integration plus focused RED→GREEN tests; overall verification, independent review, preservation/readback, current status/handoff, selective commit. No worker commits.

## Acceptance evidence
- Pure/profile selection, position boundaries, authoritative metadata, missing clips/fallbacks; real imported WAV samples and distinguishable bounded original placeholders.
- Native renderer acknowledgement behavior including actual child AudioSource.isPlaying and sampled signal, all three rare branches, each assigned position, silence/repeat/retry/mute/pause/inspect/results/skip/reload; live native player regression and unchanged ordered results/awards.
- Final build/report/source/compiler/asset references and full immutable player manifest; accepted players and shared original dependencies unchanged with Saves/Logs pruned before traversal.
- Human hearing/musical taste remains pending; automated sample/playback proof is not physical hearing acceptance.
