# Materials media: additive wrapper masks and bounded shared native textures

## Delivered scope and acceptance boundary

The media library is implemented and exercised in **Unity 6000.2.14f1 (589824c1fc31)**, in the dedicated `.artifacts/materials-media-project`. This is native Editor decoding, pixel readback and resource-lifecycle evidence, **not a connected player build, renderer acceptance, human visual/hearing approval or a phase-completion claim**.

Read [Base_Set_Materials_Contract.md](Base_Set_Materials_Contract.md) and [Phase_3_Media.md](Phase_3_Media.md) first. Existing PNG/WAV/path/schema restrictions remain in force except for the two explicitly added wrapper-mask roles and the trusted opt-in aggregate budget below.

The actual prepared `Assets/Supplied/BaseSetMaterials/Manifest.json` was **not present at the single discovery check**. No waiting or polling was performed. Full-set native evidence in this delivery uses clearly labelled original synthetic swatches at the exact required dimensions and catalog coverage. The actual-material command below is ready for the parent after authoring publishes; actual supplied-art hashes, readback, memory and no-fallback acceptance remain a gate, not a claimed result.

## Production API and invariants

Changed production files only:

- `Assets/VintageCollector/Media/PresentationMediaContract.cs`
- `Assets/VintageCollector/Media/MediaManifest.cs`
- `Assets/VintageCollector/Media/PresentationMediaLibrary.cs`

`IPresentationMedia` is unchanged. Old providers need not implement the new, separate optional interface:

```csharp
public interface IWrapperMetallicMedia {
    Texture2D WrapperFrontMask { get; }
    Texture2D WrapperBackMask { get; }
}
```

`PresentationMediaLibrary` implements both interfaces. `WrapperFrontMask` and `WrapperBackMask` return null independently when unavailable. Only exact roles `wrapper-front-mask` / `wrapper-back-mask` are added. Like the existing singleton roles, they reject a nonempty `cardId`; absent/null/empty is accepted. Unknown roles/fields and duplicate declarations still fail closed.

All mask bindings are processed after faces, regardless of manifest order. Before admission **and before every decoded-cache hit**, each mask must have its corresponding successfully decoded face and exactly matching width/height. Wrapper masks cannot borrow the opposite side. Holo masks still require the exact existing holo printing ID and its corresponding card face. No fallback aliases a different role/identity, and missing masks never become full-face metallic effects.

Masks use native linear, readable, non-mipmapped textures; faces remain sRGB. Native texture configuration remains clamp/bilinear. There is no resizing, generation, save call, network access, autoplay, AudioSource creation or RNG change.

### Trusted opt-in budget

```csharp
// Existing calls remain on the 16MiPixel aggregate limit.
PresentationMediaLibrary.Load(root, catalog);

// The NEW host/scene alone explicitly selects this policy and the new manifest.
PresentationMediaLibrary.Load(
    root, catalog,
    manifestRelativePath: "Assets/Supplied/BaseSetMaterials/Manifest.json",
    allowTestInputs: false,
    fullBaseSetBudget: true);
```

The appended optional `bool fullBaseSetBudget=false` is caller authority, never manifest authority:

- `TotalTexturePixelLimit = 16 * 1024 * 1024` (16,777,216 pixels), unchanged.
- `FullBaseSetTexturePixelLimit = 64L * 1024 * 1024` (67,108,864 pixels), exposed constant.
- Read-only `EffectiveTexturePixelLimit`, `UniqueTextureCount`, `DecodedTexturePixels` report the selected cap and retained unique decoded resources.
- The 512-entry schema cap, 16MiB per-file cap, 8MiPixel per-image cap, 4096 dimension cap and existing strict PNG subset are unchanged. Opting in is not unbounded admission. A later small binding can still load after an over-budget one is refused.

### Verified-resource sharing and ownership

Every binding still performs safe bounded file reading, exact lowercase SHA-256 verification and PNG inspection. Only then can it reuse a decoded resource keyed by **verified content hash + linear-mask versus sRGB-face semantics + inspected dimensions**. Each mask's corresponding face is validated before that reuse. A claimed digest does not bypass current path/bytes/hash checks. Compatible linear masks can share across bindings; identical bytes used as a face and mask cannot alias because color-space semantics differ.

Unique admitted pixels are charged once. Failed native decodes do not enter the cache or consume admitted pixels. A failed orphan/misaligned binding cannot authorize or poison a later valid mask. Native resources enter the ownership list once, not once per binding; idempotent disposal destroys them once and clears bindings, provenance and accounting. Editor tests establish real immediate destruction. In play mode the existing deferred `Destroy` behavior remains, to be exercised by the parent player integration.

`SuppliedAssetCount` and `Provenance` continue to count successful **bindings**, not unique textures. Lookups remain cached/no-I/O. Private provenance preserves paths/hashes but is not put into status text, resource names or warnings. Original four generic foley clips remain the fallback.

## Reproduction

Run from the project root in bash. Use fresh labels; evidence is never overwritten. These commands only use the owned isolated media stage and `.artifacts/materials-media` outputs. Do not run two native suites simultaneously: they share this worker's one isolated stage. Pure tests can run concurrently. No install/network/publication step is required; full-set independent image oracles use the already-installed Pillow, whose version is captured in `full-set-oracle.json`.

```bash
python -B tools/run_materials_media.py --label local-pure
python -B tools/run_materials_media_native.py --label local-native --existing
python -B tools/run_materials_media_native.py --label local-synthetic-full --synthetic-full-set
```

**Actual prepared materials, after authoring publishes:**

```bash
python -B tools/run_materials_media_native.py --label actual-materials-full-set --full-set
```

The actual command does not create or modify supplied art. It fails rather than waiting if the new manifest is missing. Its independent oracle checks file hashes, records original dimensions, five RGBA samples per binding, and complete bottom-up RGBA SHA-256 using Pillow. Real Unity then loads the actual manifest with `allowTestInputs:false`, compares `GetPixel` samples and complete `GetPixels32` hashes, proves fifteen holo bindings reference one native mask, checks all 121 bindings/no art fallbacks/accounting, rejects default-budget escalation, and performs two complete reload/dispose cycles with actual object-destruction/no-owned-resource-leak assertions. Synthetic mode follows the same harness but is explicitly labelled and uses `.artifacts` with the explicit test-path flag.

Optional cache-admission mutation regression (expected RED, isolated stage only):

```bash
python -B tools/run_materials_media_native.py --label local-cache-red --filter cache-shared --mutation cache-before-face
python -B tools/run_materials_media_native.py --label local-cache-green --filter cache-shared
```

The mutation moves the cache-hit check ahead of per-binding face validation **only in a copied staged source**. Production source hashes remain unchanged. It establishes that the adversarial test actually catches the forbidden authorization order; it is not mislabelled as a production feature TDD cycle.

## Retained execution evidence

The reconciled private index is `.artifacts/materials-media/evidence-index.json`; regenerate with `python -B tools/run_materials_media_audit.py`. It validates count-matched nonempty reports, actual exits, three feature RED/GREEN pairs, one separate mutation RED/GREEN pair, unchanged protected inputs and current final source pins.

| Suite / retained label | Executed result |
|---|---|
| Existing pure / `16-pure-final` | **13 passed**, real bundled Mono/compiler, warnings-as-errors |
| Existing native / `17-native-regression-final` | **49 passed**, actual Unity PNG/PCM/JSON/path/budget/disposal/RNG checks |
| Materials + synthetic full set / `15-synthetic-full-set-final` | **30 passed**, actual Unity decode/readback/lifecycle, not mocks |

The original `TestsPhase3` files are untouched. The historical native aggregate-budget test declared four byte-identical large images. Under the newly required dedup policy those are intentionally one resource, so the staged copy substitutes four separately hashed synthetic large PNGs while keeping its original assertions and 49-check count. This fixture evolution is explicit; it is not a claim that the old identical-byte aggregate expectation still applies.

New cases include all four wrapper roles in either order, native linear-mask pixels, both sides' duplicate/orphan/opposite-face/misaligned/invalid-face/hash/ID/unknown-role/unsupported-image refusals; unchanged default and bounded full budgets; fifteen-to-one reference equality; same bytes with incompatible color-space semantics; per-ID face validation even on shared hits; failed-first and successful-first mask admission; failed native LoadImage without cache/pixel poisoning; forged cached-digest paths; free shared hits after unique-resource budget refusal; no-I/O lookup after fixture deletion; native destruction/no leaks and no AudioSources.

Three production feature slices have observed native RED then GREEN: separate interface/four wrapper roles (`02`/`03`), trusted budget (`04`/`05`), and unique shared resources (`06`/`07`). Additional negative cases are regression assertions, not falsely counted as additional feature TDD cycles. The separate cache-order mutant fails in `11` and unmodified production passes in `12`.

### Synthetic full-quality native observations

Both final full-set loads observed:

- **101 faces at 600x825**, 15 explicit holo-mask bindings using one native texture, and card-back + wrapper-front/back + wrapper-front/back masks.
- **121 successful bindings, 107 unique native textures**, **51,479,000 unique admitted pixels** under the explicit **67,108,864-pixel** budget. Zero art warnings/fallbacks.
- All 121 binding sample sets and full RGBA readback hashes agreed with independently decoded synthetic PNGs; unique native resources were hashed once per load without weakening per-binding assertions.
- Unity `Profiler.GetRuntimeMemorySizeLong` summed **206,011,872 bytes** for the 107 retained native textures in this `-nographics` Editor run. This is measured texture runtime memory, not a process peak or a GPU/player VRAM claim.
- Managed heap observations were **12,128,256** and **11,624,448 bytes** at the report points. Mono's `Process.WorkingSet64` returned unavailable/zero; the report marks `processWorkingSetAvailable:false`. Do not treat zero as process memory usage.
- Captured texture/clip references compared native-null after disposal, counters returned to zero, and `Resources.FindObjectsOfTypeAll` found no remaining owned textures/clips after each reload. No automatic AudioSource was created.

### Pins / preservation / diagnostics

Current production SHA-256:

```text
PresentationMediaContract.cs  9387ece1843ff2e79e63b380d4024f78d35ac38d295fa4dcb02ecb8ed5412f14
MediaManifest.cs              894ab425269e060e05f6f1754407f7da9111039ba94fde9d11eae6dc743a3c95
PresentationMediaLibrary.cs   1cfd7e620f6ff87886e3a3d1ceee5b9564eb78181fbfac9410d44833a4775c85
```

`compiled-inputs.json`, `staged-inputs.json`, `native-library-pins.json`, `protected-inputs.json`, `execution.log`, `unity.log` and count-matched reports retain exact selected sources/staged configuration/installed Unity native and module pins. The final native runner verifies these bytes before/after execution. `Unity.dll` pin is `59d3aa89ac35e5d84cc8f38a15a0c61b00ad66b5b5e724b07aeed1b224bb8a75`; all six installed-library pins are in the index and per-run report.

Protected domain/catalog and the original empty `Assets/Supplied/AssetManifest.json` remained byte-unchanged. The original manifest SHA-256 remains `f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8`. These runners deliberately **do not traverse accepted builds or live saves**; they import only the legacy fixture function/constants, never its broad protected-build scanner.

Unity logs retain the nonblocking licensing access-token update diagnostic. Run `01` retained a fixture compilation failure from an unavailable Unity property; it was corrected before the genuine feature RED in `02`. Run `14` had all 30 native assertions pass but correctly failed the stricter input-integrity gate because Unity added its revision line to ProjectVersion.txt. The runner now writes the exact installed revision with LF, and full rerun `15` passes with all staged/source/native pins stable. Neither failed run was rewritten or relabelled successful.

## Remaining parent gates

1. Run the actual prepared-material command above and inspect its false/synthetic flag, actual manifest/image/source/native pins, no-fallback counts, full readback and memory observations. This delivery makes no actual supplied-art verification claim.
2. Only the new host/scene may select the new manifest and trusted full budget. Preserve old manifest0 and accepted players.
3. Parent-owned renderer/host/shader integration, source GUID closure, deferred player disposal/playback/pause/mute behavior, actual candidate regression and human visual acceptance remain separate work. No domain, persistence, save, audio-autoplay or RNG changes are authorized by this library delivery.
