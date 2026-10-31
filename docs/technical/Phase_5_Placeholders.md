# Phase 5 — original temporary art (authoring only)

## Current process / delivered status

**Eight original placeholder PNGs and a native-pixel contact sheet are delivered.** They are source-authoring material only, **not bound to a Jungle/Fossil playable game**. No set generation model, card identity, rarity, pull rate, award, completion or mixed-set persistence is implemented by this art work. Existing accepted Base Set presentation/audio remains frozen. No runtime `AssetManifest`, `Assets` member, original supplied file, scene, player/build, save or settings file was changed by this work.

Authorization: `../hermes/Phase_5_Discovery.md` records the user's placeholder-first choice, three independent wrapper designs per set, and deferred real-art intake. This subsystem implements that art subset, not the complete Phase 5 milestone. No downloads, uploads, model/GPU tooling or package installations were used.

## Projected changes / completed checklist

- [x] Original Jungle A/B/C: broad-leaf medallion, rising fern, angular canopy/sun.
- [x] Original Fossil A/B/C: geometric spiral, faceted amber with an abstract inclusion, layered stone/branch impression.
- [x] One identity-neutral generic card-face template per set, with no invented card names, collector numbers, rarity labels or historical subject assignments.
- [x] Readable set + variant labels, `TEMPORARY ART`, cohesive cream/green/stone/amber palette, no official logos or trademark characters.
- [x] Exact native dimensions from read-only supplied PNG metadata; no resampling, crop or copied supplied pixels.
- [x] Deterministic encoder/art/manifest generator, genuine sequential RED→GREEN evidence, independent full actual-file readback and two separate-process reruns.
- [x] Contact sheet inspected as a whole and as native-pixel crops; both card faces inspected individually at native resolution.
- [ ] Later runtime integration and Unity import/player verification — deliberately not part of this authoring checkpoint.
- [ ] Final user-supplied Jungle/Fossil art and human visual acceptance.

## Paths and dimensions

Root: `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG`

All rows below are under **`Authoring/Phase5/Placeholders/`**:

| PNG | Native dimensions | Width:height | Design |
|---|---:|---:|---|
| `jungle-wrapper-A.png` | 260×475 | 52:95 | Broad-leaf medallion |
| `jungle-wrapper-B.png` | 260×475 | 52:95 | Fern |
| `jungle-wrapper-C.png` | 260×475 | 52:95 | Angular canopy/sun |
| `fossil-wrapper-A.png` | 260×475 | 52:95 | Spiral |
| `fossil-wrapper-B.png` | 260×475 | 52:95 | Amber |
| `fossil-wrapper-C.png` | 260×475 | 52:95 | Strata/stone impression |
| `jungle-card-face-template.png` | 600×825 | 8:11 | Generic botanical face |
| `fossil-card-face-template.png` | 600×825 | 8:11 | Generic stone face |
| `ContactSheet.png` | 1540×1842 | — | All eight images at **1:1 native pixels** |

Every PNG is noninterlaced **RGBA8**, filter 0, with valid chunk CRCs, no animation/ancillary metadata and no trailing payload. Alpha is fully opaque. Rounded card corners are cream-backed presentation, not an alpha/holo mask. The nine PNG files have nine distinct SHA-256 hashes; the eight art files have exactly eight, including six distinct wrapper hashes. Native scale avoids arbitrary enlargement of low-resolution source assets; higher-resolution final art is a later intake choice.

### Dimension-only source evidence

The tool reads IHDR dimensions/CRC and hashes each source file for provenance. It never decodes, copies or repaints supplied pixels. The generic image renderer receives sizes, not any source image bytes.

| Read-only reference | Dimensions | Source-byte SHA-256 |
|---|---:|---|
| `Pokemon-Base-Set/base1-001.png` | 600×825 | `cdb5e747c2f20c84ad7b5449d498b450a590bf6ff082abd9fbbea67564e1d9f8` |
| `Pokemon-Base-Set/base1-base-set-booster-charizard-unlimited.png` | 260×475 | `c84f23d8072fab6ffb42ff9ea4fb0bb8e572c2e9e6b598d92f9fb13681138571` |

These are proportions inherited from actual supplied Base Set metadata, **not a historical measurement or character-art assignment claim about Jungle/Fossil**. A changed reference aspect/dimension requires layout review; the generator refuses automatic resizing.

## Authoring manifest and replacement contract

- **`Authoring/Phase5/Placeholders/PlaceholderManifest.json`** is explicitly `Phase5PlaceholderAuthoring`, status `authoring-only-not-runtime-bound`, `runtimeBound: false`, `supportedPlayableSetsAdded: []`.
- It is **not** `Assets/Supplied/AssetManifest.json`, is not written into `Assets`, and is not a compatible/runtime-ready asset-loader manifest.
- Stable IDs are `jungle.wrapper-front.A/B/C`, `fossil.wrapper-front.A/B/C`, `jungle.card-face-template.template`, and `fossil.card-face-template.template` (each slash expression denotes three separate A/B/C IDs, not one literal ID).
- Each row records `setId`, `role`, `variant`, project-root-relative forward-slash `relativePath`, byte SHA-256, dimensions, format and explicit original/procedural provenance; `sourcePixelsReused` and `historicalArtSubjectClaim` are both false. There are no card-printing IDs or invented card identity/rarity fields.
- `Owner.json` is the authoring ownership receipt. The manifest's preview entry separately pins the sheet, including `nativePixelScale: 1`.
- A future explicitly approved runtime adapter can map these stable presentation roles to texture references without changing hitboxes, set rules, pack RNG, reveal audio or collection identity. A/B/C is presentation-only; this generator imports no catalog, pack generator or random state.
- Generic templates intentionally contain no real card details. A later set-aware renderer must obtain all displayed card identity/rarity from a separately validated catalog, or use supplied final card faces. No layout here establishes a real printing.
- Deliberate replacement art should be introduced through reviewed new runtime/media authoring, retaining stable set+variant bindings and provenance. **Do not overwrite this tool's owned files and expect a rerun to adopt them**: changed/foreign files are refused rather than silently repaired.
- No new wrapper backs, card back or foil/metallic masks were requested or generated. These front images do not imply finish/material integration.

## Determinism, ownership and publication

`tools/prepare_phase5_placeholders.py` uses already-installed Pillow **13.0.0** only for primitive raster drawing and decoding its own generated preview inputs. A self-contained 5×7 alphabet avoids installed fonts. The stdlib PNG encoder uses stored DEFLATE blocks, explicit filter-zero scanlines, Adler-32 and chunk CRCs; byte output is not sensitive to a compressor's heuristics. No timestamps, random seeds, network requests or image models are used. The recorded tested interpreter is Python **3.14.9**. Rendering across arbitrary future Pillow/math-toolchain upgrades is not certified; changed bytes fail closed rather than replace an earlier generation silently.

Only the fixed `Authoring/Phase5/Placeholders` destination is published. Fixture roots must remain inside the approved project root. Static ownership checks reject out-of-root paths/traversal, symlinks/junctions/reparse points and regular-file hardlinks before source/output reads. Source references are checked again after rendering; mutation during rendering is refused. Foreign/incomplete destination membership, altered ownership receipts, changed images, changed manifests or changed source receipts are not adopted or overwritten. Complete new output is written into an exclusively allocated sibling staging folder before directory rename. An exception removes only that operation's staging folder. Exact reruns return without changing output bytes **or nanosecond mtimes**.

This is conservative fixed-fileset publication on the tested Windows filesystem, **not a hostile-writer sandbox or a claim of power-loss/crash durability**. There is no general multi-process mutation protocol or auto-repair/recovery. The tool has no cleanup of unknown output folders, accepted players or user files.

## Execution and evidence

From Git Bash, use a quoted `cd` because the repository path contains brackets; the terminal tool rejects that path as its `workdir` argument. Use native forward-slash paths when invoking native programs directly.

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python -B TestsPhase5/PlaceholderAuthoringTests.py
python -B tools/prepare_phase5_placeholders.py
python -B TestsPhase5/PlaceholderReadback.py
```

Actual results:

- **15 authoring tests PASS, zero skips**, final recorded execution 3.048 seconds.
- **1 actual corpus readback/audit PASS**, final recorded execution 0.820 seconds.
- All **9 actual PNGs** pass independent CRC/chunk/IEND/filter-byte validation and full Pillow decode; entire RGBA arrays agree between decoders.
- All **8 native-size contact sheet crops** equal the corresponding original complete pixel arrays.
- **2 separate-process actual generator reruns** preserve byte hashes and mtimes of **11 output files** (8 art + sheet + manifest + owner).
- **116 protected files** retain hashes and mtimes across those actual reruns: all 114 supplied original files plus the old empty runtime manifest and accepted BaseSetMaterials manifest. The latter two also match their independently documented preexisting hashes. No save/build traversal was used for this audit.
- Negative checks include corrupt source IHDR CRC, changed aspect ratio, source mutation during render, later changed-source receipts, foreign output/owner, modified generated art, real source/output hardlinks, a real Windows source-ancestor junction and out-of-scope roots.
- The generator makes no claim of Unity/native texture import or a new playable set. Native image inspection here means inspection of actual unscaled raster pixels, not Unity player execution or user acceptance.

Logs and detailed receipts are under `.artifacts/phase5-placeholders/`:

- `01-codec-red.log` / `01-codec-green.log`: actual encoder missing assertion → RGBA8 roundtrip/CRCs.
- `02-art-red.log` / `02-art-green.log`: missing art renderer → eight distinct original images, motif-region differences and repeat bytes.
- `03-publication-red.log` / `03-publication-green.log`: missing preparation → actual files, manifest, header-derived dimensions and native preview.
- `04-source-race-red.log` / `04-source-race-green.log`: mutation was published → source-change refusal.
- `05-ownership-red.log` / `05-ownership-green.log`: hardlinked source was accepted → physical ownership guard.
- `06-cli-red.log` / `06-cli-green.log`: CLI previously exited without publishing → real child-process CLI.
- `full-green-final.log`, `actual-generation.log`, `actual-readback.log`, **`Actual_Readback.json`**.

Each named RED above exited 1 with the expected assertion failure; GREEN progressed 1→2→3→4→5→15 tests. Later negative assertions exercise existing guards and are not misrepresented as additional RED cycles. All fixtures/logs/staging/output are under Root. No commits were made.

### Actual artifact SHA-256 pins

| File | SHA-256 |
|---|---|
| `jungle-wrapper-A.png` | `dd886ac36f1ccaad51fbf250ec6f6e158b98579e942c082dbe58555c8fbadb29` |
| `jungle-wrapper-B.png` | `af2d644d210b7aa43664c669f8724d96f94d87027b1ea69e57e50982dac569a0` |
| `jungle-wrapper-C.png` | `02c6d9da8baddbccd2359a9f0760d48a8bc78031af8e1436ff242105f9235661` |
| `fossil-wrapper-A.png` | `322109a123100803e284580197e99289648a67699300600408e6772239e36a82` |
| `fossil-wrapper-B.png` | `03af630e58f4b5e50a58f911c5c61b7f81dc9fcc04cfc505dbe97687fd7ee6e4` |
| `fossil-wrapper-C.png` | `89ed32d67606bb0b1ef27cd7917c4cd2a645798c363ed3388796e152226447c4` |
| `jungle-card-face-template.png` | `bdd7124b2f44a946929e5cedb96c8f6cc8ebaf5d6b276b61fb23eca28c50390a` |
| `fossil-card-face-template.png` | `3198afe9a1e99a5cf753020208b5daa3b616b6731d70314c4f24f18efe2b7df7` |
| `ContactSheet.png` | `029405c86398110ee279bc80fa6272b4d5210eb13e86ed9cc98e4552e8a7674c` |
| `PlaceholderManifest.json` | `0bbe868980bb8ca4148cf25c88efeff54913b5f494970dd59c244b3ddfabc722` |

## Visual readback / handoff

The agent loaded the actual 1540×1842 sheet, then inspected native crops containing all three Jungle wrappers and all three Fossil wrappers, and loaded each 600×825 template separately. Set/variant/temporary labels and template labels are visible; no clipping or missing art was observed. The six central motifs differ beyond labels; no branded character or official logo was created. This is agent inspection, not a reported human PASS.

Next responsibility is the parent research/integration gate: independently validate each set, retain explicit unresolved assumptions, and integrate presentation only when separately approved. The final art request is still small and replaceable: three front wrappers per set plus that set's desired card faces, supplied privately with source/provenance. Existing source dimensions are preferred layout references, not a demand to downsample higher-quality supplied art.
