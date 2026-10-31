# Assignable reveal audio profile

## Outcome and scope

- **Implemented:** optional Inspector-configured profile/host, 11 original pull-position WAVs, three original rare stingers, and the additive reveal-sound interface.
- **Native asset:** `Assets/VintageCollector/Audio/DefaultRevealAudioProfile.asset`.
- **Native validation:** isolated Unity **6000.2.14f1**, real Play Mode activation/destruction callbacks, imported sample readback, no main-project Editor launch. This is not human hearing acceptance or the assembled player/acknowledgement verification; those remain separate integration evidence.
- No catalog, RNG, pack ordering, controller, save schema, persistence or supplied media changes in this workstream. No Pokémon cries integration, remote generation, downloads/uploads, runtime import UI or settings UI.

The frozen [Reveal Audio Contract](Reveal_Audio_Contract.md) remains authoritative. Renderer integration belongs to the parent workstream; new source scene/player construction belongs to the build workstream.

## Inspector assignment

1. Open **`Assets/VintageCollector/Audio/DefaultRevealAudioProfile.asset`** in the Unity Inspector. To make a separate profile, duplicate this asset or use **Assets → Create → Vintage Collector → Reveal Audio Profile**.
2. Keep **Pull Sounds → Size = 11**. Array indices are zero-based in the Inspector; reveal positions are one-based:

   | Inspector element | Reveal position | Base Set Model A slot | Default clip |
   |---|---|---|---|
   | 0–4 | 1–5 | Commons | `Clips/Pull_01.wav`–`Pull_05.wav` |
   | 5–6 | 6–7 | Energy | `Clips/Pull_06.wav`–`Pull_07.wav` |
   | 7 | 8 | Rare/holo position fallback | `Clips/Pull_08.wav` |
   | 8–10 | 9–11 | Uncommons | `Clips/Pull_09.wav`–`Pull_11.wav` |

3. Leave **Rare Reveal Position = 8** for this pack order. It is configurable presentation data, not a card-trick or pack-order setting.
4. Assign the three optional **AudioClip** overrides:
   - **Non Pokemon Rare:** `Clips/Rare_NonPokemon.wav`.
   - **Pokemon Rare:** `Clips/Rare_Pokemon.wav` (Pokémon non-holo).
   - **Holo Rare:** `Clips/Rare_Holo.wav`.
5. On the **same GameObject as `TabletopPresentation`**, add **`RevealAudioHost`** and assign its public **Profile** field to this asset. The new `BootstrapRevealAudio` scene/build workstream does this wiring; historical scenes without a host continue using generic Foley.
6. Replace any referenced clip by importing your own appropriately licensed audio into Unity and dragging its **AudioClip** onto that field. No filename convention is required for replacements. Prefer short one-shots with quiet endpoints. The authoring generator intentionally refuses to overwrite differing source bytes; it is not the replacement workflow.

### Selection and fallback

- On ordinary positions, only that position's assigned clip is consulted. Kind/finish cannot reveal an upcoming rare outcome.
- At the configured rare position, exact authoritative `finish == "holo"` has priority; otherwise exact `kind == "pokemon"` selects Pokémon non-holo; other catalog kinds select non-Pokémon.
- **Missing selected rare override → that position's clip → null → renderer's existing generic next Foley.** A missing holo never substitutes the Pokémon/non-Pokémon stinger. Missing Pokémon does not substitute non-Pokémon.
- Positions outside 1–11 return null. Null/short arrays fail safely for unassigned ordinary positions; an assigned rare override still works. Array entries beyond 11 are never selected.
- The provider does not play anything itself. Only the renderer's acknowledged `next` path calls it using the already-revealed card, subject to its existing duplicate, volume, focus, pause, save-error and collection gates. Skip does not catch up the music.

## Exact public API

Additive interface in `Assets/VintageCollector/Media/PresentationMediaContract.cs`; `IPresentationMedia` is unchanged:

```csharp
namespace VintageCollector.Media
{
    public interface IRevealSoundMedia
    {
        UnityEngine.AudioClip RevealSound(int oneBasedPosition, string kind, string finish);
    }
}
```

```csharp
namespace VintageCollector.Audio
{
    public sealed class RevealAudioProfile : UnityEngine.ScriptableObject,
        VintageCollector.Media.IRevealSoundMedia
    {
        public UnityEngine.AudioClip[] pullSounds; // initialized to eleven entries
        public int rareRevealPosition;             // initialized to 8
        public UnityEngine.AudioClip nonPokemonRare;
        public UnityEngine.AudioClip pokemonRare;
        public UnityEngine.AudioClip holoRare;
        public UnityEngine.AudioClip RevealSound(int oneBasedPosition, string kind, string finish);
    }
    public sealed class RevealAudioHost : UnityEngine.MonoBehaviour
    {
        public RevealAudioProfile profile;
    }
}
```

The host's `OnEnable` binds the assigned profile to the co-located renderer's `public IRevealSoundMedia RevealSounds { get; set; }`. Missing profile/renderer is harmless. Startup does not create an AudioSource, play clips, read card state or modify saves. `OnDestroy` clears only the exact provider it bound; a later replacement survives. The host never owns/destroys shared imported AudioClips.

## Original placeholder composition

`tools/prepare_reveal_audio.py` uses **Python stdlib only**, additive sine harmonics and tapered per-note envelopes. The music was composed for this slice; no recordings, borrowed themes, soundfonts, trademark melody, external generation service or supplied copyrighted art are inputs. Synthesis is deterministic and does not access gameplay RNG/state.

- All **14 files** are playable RIFF/WAVE, mono, 44,100 Hz, signed 16-bit PCM.
- Positions 1–8 rise through D-major/pentatonic pitches with increasing measured RMS/peak and durations from 0.220 to approximately 0.444 seconds; positions 9–11 relax to shorter, quieter 0.300-second gestures.
- Distinct rare gestures: compact D/A answer (**0.740 s**), rising D/F♯/A/B (**1.060 s**), wider chime flourish over a sustained dyad (**1.540 s**).
- Peaks have headroom: the loudest imported sample is approximately **0.640** full scale. Per-note attack/release and global endpoint fades eliminate hard starts/stops; tests check finite audible PCM, maximum adjacent-sample differences and short endpoint windows.
- `Clips/OriginalRevealAudio.manifest.json` records provenance, per-file SHA-256, frame counts and duration. These are original placeholder assets, not supplied-user-asset entries.

Authoring/check commands from the project root:

```bash
python tools/prepare_reveal_audio.py --check
python tools/prepare_reveal_audio.py --write
```

`--write` creates missing originals and does nothing to identical files. It preflights the whole batch and refuses differing assets before writing anything; existing `.meta` files are never touched. Output is confined to this workstream's source or fixture directories.

## Native asset identity/provenance

Unity created `DefaultRevealAudioProfile.asset` through `AssetDatabase.CreateAsset` in the isolated `.artifacts/reveal-audio-profile/native-project`. Audio import settings are PCM, Decompress On Load, preserved sample rate and preloaded data. The actual Unity-generated script/WAV/folder/manifest/profile metas and asset were adopted into source **only if absent or already byte-identical**, never overwriting an existing identity.

Adoption mapping and hashes: `.artifacts/reveal-audio-profile/10-native-profile-green-4/native-asset-adoption.json`. Source/staged input pins and post-run drift checks are alongside it. The native profile's serialized MonoScript GUID and all 14 clip GUIDs point to delivered source metas, not a manually invented YAML identity.

## Vertical-slice TDD evidence

Evidence directory: **`.artifacts/reveal-audio-profile/`**. Each runner preserves the actual exit; a filename containing `green` is not itself a pass.

| Slice | Observed RED | Actual GREEN |
|---|---|---|
| One-based Inspector pull slots | `01-slots-red.log`: missing profile assertion, exit 1 | `01-slots-green.log`: 6 checks, exit 0 |
| Optional interface / non-Pokémon rare / fallback | `02-rare-interface-red.log`: missing optional interface assertion | `02-rare-interface-green.log`: 10 checks |
| Pokémon rare / category-safe fallback | `03-pokemon-red.log`: Pokémon branch assertion | `03-pokemon-green.log`: 12 checks |
| Authoritative holo priority | `04-holo-red.log`: holo priority assertion | `04-holo-green.log`: 15 checks |
| Array/position resilience / nonspoiling metadata matrix | `05-boundaries-red.log`: rare suppressed by null array | `05-boundaries-green.log`: 149 checks |
| Optional host / owned detach | `06-host-red.log`: missing host assertion | `06-host-green.log`: **155 checks**, exit 0 |
| Eleven crescendo WAVs / actual PCM | `07-position-pcm-red.log`: generator absent assertion | `07-position-pcm-green.log`: one Python test |
| Three distinct rare musical stingers | `08-rare-pcm-red.log`: missing rare compositions assertion | `08-rare-pcm-green-2.log`: two Python tests |
| Safe authoring CLI / asset-meta preservation | `09-authoring-cli-red.log`: CLI generated zero assets assertion | `09-authoring-cli-green.log`: **three Python tests**, exit 0 |
| Native asset / import / real lifecycle | `10-native-profile-red/results.json`: missing native profile assertion | `10-native-profile-green-4/results.json`: **147 native checks**, exit 0 |

### Retained unsuccessful verification attempts

- `08-rare-pcm-green.log` is a **real failed run**: highest holo pitch exceeded the adjacent-sample safety bound. The original composition was lowered for headroom; `green-2` is the passing result. The earlier log remains unchanged.
- `10-native-profile-green/unity.log`: test authoring harness used obsolete `AudioImporter.preloadAudioData`; Unity 6000 requires `AudioImporterSampleSettings.preloadAudioData`. Corrected the harness, not production behavior.
- `10-native-profile-green-2` and `green-3`: Edit Mode fixture assumptions did not deliver inactive `SendMessage`/ordinary MonoBehaviour destruction callbacks. The final harness enters **real Play Mode** and proves automatic activation/OnDestroy, rather than adding `ExecuteInEditMode` to production or faking native callback results.
- Final native logs retain cached-license signature/token diagnostics despite resolved entitlements, and Mono shutdown abort-thread diagnostics. The process exited 0 with a complete reconciled passing report; the run is not described as warning-free.

### Reproduction

```bash
python TestsRevealAudio/AudioProfileRunSelection.py verification-selection-unique-label
python TestsRevealAudio/AudioProfileRunSynthesis.py verification-pcm-unique-label
python TestsRevealAudio/AudioProfileRunNative.py --label verification-native-unique-label
```

Use fresh evidence labels: runners refuse to overwrite existing logs/reports. Managed selection tests use explicit Unity-boundary doubles and compile the **real production profile/host/contract** with Unity's bundled Mono and warnings-as-errors. Native tests stage real production presentation/media/domain/profile/host sources and source metas, reject unexpected staged scripts and check source/staged hashes after execution. Native sample readback matched source PCM exactly for every imported clip in the passing run. A second native run **`11-native-delivered-source/results.json`** restaged the delivered asset and all source metas, passed the same **147 checks**, and reported zero PCM error across **14 clips**. Final Python regression evidence is `11-final-pcm.log` (three tests, exit 0). `source-asset-closure.json` records all **15** profile references (MonoScript plus 14 AudioClips), source GUID closure and the profile asset SHA-256.

**Evidence boundary:** assembled-scene/player acknowledgement playback, stop/mute/pause/retry/reload/skip and final source/asset/compiler closure are recorded separately in [Reveal_Audio_Build_Player.md](Reveal_Audio_Build_Player.md). The initial actual player suite and the parent's independent rerun played all14clips, observed36nonzero source/listener playbacks, and passed279audio plus40reload checks; independent build-review corrections and their latest evidence belong to that build handoff. Component/profile tests here do not replace player execution. Human hearing, musical preference and physical interaction remain pending.

The independent runtime/audio review passed with no security or logic findings. Its nonblocking fixture-isolation suggestion is deferred: run the profile verification helpers **serially**, using fresh evidence labels, because their selection binary/native fixture project paths are shared rather than label-specific.
