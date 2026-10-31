# Phase 3A — Safe replaceable media library

## Delivered scope and acceptance boundary

`PresentationMediaLibrary` implements the frozen `IPresentationMedia` contract without modifying the game, catalog, probabilities, persistence, paths, renderer, accepted build or live saves. This is **placeholder-first support**, not acceptance of supplied/branded materials or closure of Phase 3.

The unchanged production `Assets/Supplied/AssetManifest.json` has an empty assets array. Real native verification confirms **0 supplied bindings, no textures and four original generic foley clips**. Synthetic fixtures exist only in `.artifacts`; they are not user-supplied assets. Source/rights metadata does not grant permission to publish anything.

Native evidence below is an **isolated Unity Editor media-library run**. It is not a core-connected player run, renderer/host acceptance, physical-hearing evidence, or an audio-quality judgment. No tracked work was closed.

## API and lifetime

```csharp
using VintageCollector.Domain;
using VintageCollector.Media;

// Unity main thread, after the real catalog is available:
PresentationMediaLibrary media = PresentationMediaLibrary.Load(
    projectRoot, catalog,
    manifestRelativePath: "Assets/Supplied/AssetManifest.json",
    allowTestInputs: false);

IPresentationMedia provider = media;
// provider.CardFace(fullStablePrintingId), provider.HoloMask(fullStablePrintingId)
// provider.CardBack, provider.WrapperFront, provider.WrapperBack
// provider.Sound("generate" | "open" | "next" | "skip")
// provider.StatusText, provider.SuppliedAssetCount
// media.Warnings: IReadOnlyList<string>

// Owner must clear renderer references and stop any sources using its clips first.
media.Dispose();
```

- `Load` reads optional local content, never saves, generates packs or awards cards. Missing/invalid media returns the library with warnings and independent safe fallbacks, not a thrown optional-media error.
- A null texture means use the generic placeholder. Missing face/mask never aliases another card or implicitly enables full-face foil.
- `Sound` returns a cached real `AudioClip`, without playing it. Unknown/null action returns null. The provider never creates an `AudioSource`, plays, queues or schedules anything.
- Lookup methods perform no filesystem I/O. All loading is eager and bounded in `Load`.
- `SuppliedAssetCount` counts successfully decoded bindings, including explicitly bound masks, not manifest declarations or generic clips. Test overrides deliberately exercise the same count, but remain clearly labelled original fixtures in their manifest/provenance and test reports.
- The additional read-only `Provenance` collection contains successful `MediaEntry` records with exact source/rights/restrictions/hash/path/identity. It is for explicit private tooling: **do not concatenate it into UI/logs**. `StatusText`, warnings and native resource names never echo untrusted source URLs, paths or credentials.
- Main-thread `Dispose` is idempotent, destroys owned textures/clips and clears bindings, provenance and supplied count. In edit mode destruction is immediate; while playing it uses Unity's normal deferred `Destroy`. It does not destroy unrelated objects. There is no finalizer or automatic playback. An externally supplied provider is not renderer-owned.

## Manifest v1 validation

The existing contract is unchanged. Root fields are `schemaVersion`, `status`, `intendedUse`, `redistributionPermission`, `rightsDefault`, `notes`, `assets`. Schema version must be the JSON integer `1`, not a string or fractional/exponent number. Root text fields must be present strings; only notes may be empty. Assets must be an array.

Entries use `role`, `cardId`, `relativePath`, `source`, `rights`, `restrictions`, `sha256`:

- Roles: `card-face`, `holo-mask`, `card-back`, `wrapper-front`, `wrapper-back`, `sound-generate`, `sound-open`, `sound-next`, `sound-skip` only.
- Face/mask identity must exactly match an existing full stable `Catalog.Cards[].Id`. A set printing ID, display name, unknown ID, changed variant/finish or missing ID does not qualify. Masks require a holo identity. Other roles require no card ID (absent/null/empty accepted).
- Source and rights must explicitly be nonempty strings. `unknown` rights is accepted as uncertainty, not clearance. Restrictions must be an explicitly present string, possibly empty.
- SHA-256 must be exactly 64 lowercase hexadecimal characters and match the actual bounded bytes **before** any image/audio decode. No prefix or uppercase normalization is accepted.
- Unknown fields and duplicate JSON keys are rejected. All declarations of a duplicate role/card binding are rejected before loading, including when one declaration also has bad provenance. No first/last-wins selection.
- Syntactically valid but schema-invalid entries, including null/boolean/fractional values, produce entry-indexed warnings without discarding unrelated valid entries. Malformed JSON, invalid root schema or excessive parser limits falls back for the manifest as a whole.
- Encoding is strict UTF-8 without BOM. Invalid UTF-8 is not replacement-decoded. Parser caps: 1 MiB manifest, 512 entries, nesting depth 8, 12,000 value nodes, 8,192 UTF-16 characters per JSON string. These are intentionally conservative local import limits.

Warnings are intentionally indexed and bounded rather than echoing attacker-controlled strings or exception messages. They identify manifest failure, schema/identity/provenance/duplicate rejection, or unavailable/unsafe/unsupported/misaligned/hash-invalid content. A bad entry does not invalidate independent valid bindings.

## Local path policy / no network

- The project root itself must be an absolute Windows local-drive path. UNC, device, URI and relative roots are rejected lexically before filesystem metadata access. Mapped network drives are also rejected by drive type; an actual mapped network drive was not needed or contacted in tests.
- Only canonical project-relative forward-slash paths beneath **`Assets/Supplied/`** are permitted by default, for manifest and media alike.
- Explicit `allowTestInputs: true` additionally permits paths beneath **`.artifacts/`**. This is for isolated/smoke inputs only; the parent host must not enable it for normal play. It does not allow `Builds`, live Saves, arbitrary parent paths or sibling roots.
- Reject absolute/UNC/URI paths, drive/ADS colons, traversal and dot components, empty components, backslashes, percent-encoded spellings, control/invalid Windows characters, trailing dot/space components, common Windows device filenames and lookalike root prefixes. Paths are rejected rather than silently repaired or URI-decoded.
- Lexical validation precedes filesystem metadata/open calls. Existing reparse points are rejected along the complete ancestor chain, including the project root. Metadata checks run drive/root-to-leaf, never by statting a descendant through an unchecked ancestor. Real Windows directory-junction fixtures prove this, not simulated flags.
- Before reading file bytes on Windows, the opened handle's `GetFinalPathNameByHandle` identity must equal the validated target; ancestor metadata is rechecked. The read stream permits sharing for readers only, not writers/deletion. This also fences a redirect swapped between initial validation and open. This is application-level containment, not a general OS sandbox.
- Each media file is limited to 16 MiB, checked on the opened stream before allocating its byte buffer. Manifest reads have the smaller 1 MiB cap. Reads must complete the declared bounded length.
- No networking APIs, fetching, DNS lookup or downloading exist in the library. Source URLs are provenance strings only. No branded downloads, public uploads or package installs were performed for this work.

## PNG contract

Only `.png`, signature-verified **8-bit noninterlaced RGB or RGBA PNG** is supported. Native tests decode both subsets and verify original checker pixels through real `Texture2D.GetPixel`. JPEG, palettes/grayscale/16-bit/interlaced images and other codecs are not supported or claimed.

Before invoking Unity's real `ImageConversion.LoadImage`, inspect signature, first IHDR, header fields, dimensions, ratio, chunk lengths/CRCs, critical chunk policy, IDAT presence/ordering and final IEND. PNG inspection alone is not decoding; a CRC-valid/header-valid invalid-zlib fixture reaches the actual decoder and still safely falls back.

Limits:

- At most 4096 in each dimension, at most 8,388,608 pixels per image, file <=16 MiB.
- Total admitted texture pixels <=16,777,216 per library, checked before each native decode. Failed decodes do not consume admitted pixels. This bounds retained readable texture memory as well as individual image size; later small bindings can still load after a large binding exceeds the remaining budget.
- Card face, card back and mask width/height must differ from `63.0 / 88.0` by no more than `0.025` in absolute ratio. Wrappers need only meet dimension/pixel bounds.
- No resize or distortion. Native decoded dimensions must exactly match the inspected IHDR.
- Masks are loaded in a second pass after faces, regardless of manifest order. They require a successfully decoded face of the same exact stable holo ID and exact pixel dimensions. Orphan, bad-face, nonholo and mismatched masks fall back independently; a missing mask never becomes a whole-card mask.
- Textures are readable, non-mipmapped, clamped and bilinear-filtered. Masks are created linear. The renderer determines presentation; this provider does not repaint faces or apply effects.

## WAV contract

Actual signed little-endian sample decoding is implemented in `MediaWave`, not a RIFF-header placeholder:

- RIFF/WAVE with exact RIFF span, one `fmt ` chunk of **exactly 16 bytes**, integer PCM format `1`, one nonempty data chunk.
- PCM16 or PCM24, mono or stereo, 8000–48000 Hz inclusive, at most 5 seconds per channel, <=16 MiB file.
- Validate channel/rate/bit-depth combinations, byte rate, block alignment, data divisibility, all chunk lengths/padding and duration **before** allocating decoded float samples.
- Bounded unknown RIFF metadata chunks are skipped; duplicate/truncated fmt/data, empty data and inconsistent layouts fail closed.
- Sign-extend PCM24 and normalize by 8388608; PCM16 by 32768. Resulting interleaved floats are finite in [-1,1), with frame count equal to scalar samples/channels. Real Unity `AudioClip.Create/SetData/GetData` proves both mono PCM16 and stereo PCM24 round trips.
- This intentionally rejects WAVEFORMATEX/EXTENSIBLE fmt variants, PCM8/32, float/A-law/compressed data, RF64, MP3 and Ogg. A legitimate WAV outside this documented subset warns and uses original generic foley. No other decoder is claimed.

## Original generic cues

The four deterministic one-shots use local xorshift state, filtered noise, a small tonal component and a tapered envelope. They do not use `UnityEngine.Random`, the game's `IRandomSource` or catalog probabilities. Runtime re-generation/calls to `Sound` do not draw from game RNG. The provider creates/caches clips only, never plays them.

Measured by real native Unity PCM readback, 22050 Hz mono:

| Cue | Samples | Approx seconds | Peak | RMS |
|---|---:|---:|---:|---:|
| generate | 6174 | 0.28 | 0.05482127 | 0.01268987 |
| open | 7938 | 0.36 | 0.07650186 | 0.02112205 |
| next | 2646 | 0.12 | 0.05489074 | 0.01275966 |
| skip | 3969 | 0.18 | 0.05863805 | 0.01225265 |

Pure tests additionally assert repeated-call identity, distinct signatures, nonzero energy, finite/modest peaks <=0.25 and tapered endpoints. These are **original generic foley**, not historical recordings, branded sounds, downloaded music or voice. Engine sample readback is not a claim that anyone physically heard a speaker.

## Reproducible verification and retained RED/GREEN

From the project root in bash; no SDK/pip/package installation is needed:

```bash
python -B tools/run_phase3_media.py --label local-pure
python -B tools/run_phase3_media_native.py --label local-native
python -B tools/run_phase3_media_audit.py
```

Use fresh labels to retain previous logs. The first runner uses Unity 6000.2.14f1's bundled Mono/compiler with warnings as errors, explicitly selected production media/domain files and `TestsPhase3/MediaPureTests.cs`. The second copies only media/contract/domain/test files into `.artifacts/phase3-media-project`, runs the installed real Unity Editor and never opens the main project. Controlled temp/log/cache/test/build outputs stay under `.artifacts`. It uses installed built-in Unity modules, not downloaded test packages. No source glob includes another worker's test entrypoint.

Latest verified full reports:

- `.artifacts/phase3-media/19-pure-final/results.json`: **13 named checks passed**. This includes actual PCM16/24 mono/stereo numeric oracles, signed full scale, rate/duration boundaries, truncation/mutant safety, header/CRC/ratio/pixel gates, real junction rejection, exact lowercase hashes and strict JSON/provenance checks.
- `.artifacts/phase3-media/19-native-final/native-results.json`: **49 named checks passed**. This includes actual PNG pixels/RGB+RGBA, actual supplied-test and synthesized PCM/native clip caching, empty/missing/malformed/partial manifests, invalid types, roles/identities/provenance/hashes/paths, actual junctions, unsupported/invalid media, per-file/aggregate caps, aligned/misaligned/orphan masks, independent fallbacks, disposal/no leaks, no automatic AudioSource and unchanged domain/Unity RNG sequences.
- Every run retains `execution.log`, count-matched nonempty JSON results, `compiled-inputs.json` and `protected-inputs.json`; native runs also retain `unity.log`. The synthetic checker/wave fixtures and their explicit original-test-only label stay beside each run.
- `evidence-index.json`, produced by the audit command, verifies **15 actual RED/GREEN slice pairs**, nonempty/count-matched reports, expected failing assertions/real exits, successful GREEN and final source pins. Additional adversarial/regression checks are not falsely labelled extra TDD cycles. Historical RED source hashes are retained, not replaced by current hashes.
- Both final suites verify **162 protected files byte-unchanged before/after**, including Domain/Data/Persistence/GamePaths/OpeningGame, the real manifest and all files in the accepted Phase2 build (including saves). This library has no write API or persistence reference. No accepted build files or live saves were changed.

The native Unity logs contain a non-blocking licensing access-token update diagnostic. The actual assertion runs still returned exit 0 with full reports. Logs were retained as returned, not rewritten to look pristine.

Host attachment, renderer playback/mute/pause behavior and the core-connected candidate were subsequently exercised by the parent; see ../verification/Phase_3A_Corrected_Execution_Summary.json and Phase_3A_Independent_Review.json. The reports above remain point-in-time evidence: earlier media hashing could read/hash save contents without writing them, and the corrected shared runner now prunes save directories before traversal. Later parent observer changes mean historical broad host pins are not all current; host/media implementation pins match, while the corrected player/PDB separately binds current observers. Physical hearing and real supplied-material approval remain pending.
