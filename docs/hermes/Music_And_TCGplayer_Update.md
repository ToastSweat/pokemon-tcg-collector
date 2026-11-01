# Music and TCGplayer update

## Run

`F:\[WORKSPACE]\[HERMES]\[CHATGPT]\PokemonThrowbackTCG\Builds\Phase6MusicWindows\VintageCardCollector.exe`

Keep the entire player folder together. This is a separate build with its own Saves directory; all twelve earlier used players remain unchanged. Use the existing in-game backup/restore-as-new-profile workflow if you want your current collection in this build. Do not delete or move older saves.

## Search correction

Only the TCGplayer search query changed:

- `Venusaur Pokemon English Base Set Unlimited Holo`
- `Metapod Pokemon English Base Set Unlimited`

TCGplayer no longer receives collector numbers, Near Mint/raw/ungraded filters, Non-Holo text or negative tags. Holo cards retain the useful `Holo` keyword. The eBay URL/query, copied description and listing-URL validation remain unchanged. Eight focused query checks passed using the two owner-supplied examples; no search-result availability guarantee or network test is claimed.

## Supplied soundtrack

| Track | Context |
|---|---|
| 01 — A Fresh Collection | Startup/title loading. The current app has no separate main-menu scene. |
| 02 — Eleven Little Possibilities | Base selection and opening |
| 03 — Something in the Tall Grass | Jungle selection and opening |
| 04 — Buried Wonders | Fossil selection and opening |
| 05 — Every Empty Pocket | Binder and collection management |
| 06 — The Card-Shop Counter | Physical-card shopping panel |
| 07 — One More Page Complete | Explicit pack results |
| 08 — Desk Lamp at Midnight | Optional relaxed binder music |

Use **Music: Day (05)** / **Music: Night (08)** at the binder's bottom-right to switch its theme. This choice lasts for the current session. Shopping temporarily takes precedence; closing it returns to the selected binder theme.

Music loops with short crossfades and follows the existing global volume, pause and focus behavior. It does not restart for each card or alter reveal sounds, card odds, saved awards or physical ownership. The supplied MP3 originals were not modified. Byte-identical asset copies are imported as native streaming audio with preload disabled, rather than loading all song PCM into memory at startup.

## Focused verification

One new native Windows build completed with zero errors/warnings. One isolated player run passed 12 focused checks: set/binder/night/shopping/results routing, same-context playback, mute, pause/resume, one persistent owner and normal shutdown. Actual AudioSource clip identities and advancing sample positions were observed for tracks 02–08. Two complete screenshots were validated, and the new binder control was visually checked at 1280×720.

All eight clips, including 01, passed genuine Unity import/profile binding checks. The heavy-scene probe does not prove startup track 01 playback; human hearing, loudness balance and listening comfort remain for you to judge. The unattended test uses a component-specific synthetic focus callback rather than moving your OS focus. It makes no marketplace or clipboard actions and uses disposable project-local saves.

Evidence: `docs/verification/Music_And_TCGplayer_Execution.json`; raw native reports under `.artifacts/music-native/` and `.artifacts/music-player/s01/`.
