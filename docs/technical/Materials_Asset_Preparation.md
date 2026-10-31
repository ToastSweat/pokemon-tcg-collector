# Base Set materials asset preparation

## Delivered output

Run from the repository, using the already-installed Pillow (no installation/network access):

```bash
python -B tools/prepare_base_set_materials.py
python -B TestsMaterials/AuthoringTests.py
python -B .artifacts/materials-authoring/audit_actual_materials.py
```

The new, private runtime manifest is **`Assets/Supplied/BaseSetMaterials/Manifest.json`**. Its `relativePath` values are **project-relative `Assets/Supplied/...` paths**, as required by `MediaFiles.Resolve`; they are not relative to the manifest directory. The older `Assets/Supplied/AssetManifest.json` remains byte-exact and empty. No catalog, simulation, RNG, identity, save, accepted build, runtime/media code, settings or Git edits are part of this work.

The tool owns only its new asset directory. It reads the fixed `Pokemon-Base-Set` source directory and intake/catalog inputs without creating source-side locks, modifying file attributes or rewriting originals. JPEG/PSD files are hashed/preserved but never derived or bound as runtime media.

### Actual prepared counts

| Item | Verified count |
|---|---:|
| Original files, all byte/hash checked | 114 |
| Original PNGs fully decoded | 110 |
| Derived PNGs fully decoded and compared against **entire** original RGBA pixel arrays | 110 |
| Runtime card-face bindings | 101 |
| Eligible holo-mask bindings sharing one `base1-foil.png` | 15 |
| Other bindings: card back, Charizard wrapper front, common wrapper back, front metallic mask, back metallic mask | 5 |
| Total bindings | 121 |
| Unique runtime images, reconciled by content hash + mask/color semantics + dimensions | 107 |
| Unique runtime texture pixels | 51,479,000 |
| RGBA8 base-level texture bytes (not Unity/native/GPU overhead or mipmaps) | 205,916,000 (~196.38 MiB) |
| All derived images, including unbound Machamp and two alternate fronts | 110 |
| All derived image pixels, including the unbound images | 52,221,000 |

All card/back/holo images remain 600×825; all wrapper/front/back/metallic images remain 260×475. All outputs are noninterlaced, 8-bit RGBA PNGs. The supplied PNG codec inventory was one RGB/noninterlaced image, 101 indexed/interlaced images and eight RGBA/noninterlaced images. Conversion changes encoding/ancillary metadata, **not decoded RGBA pixels**. It does not resize, crop, repaint, erase stamps, transform color space or resample.

The 101 exact full printing IDs, names and variants come from the unchanged existing catalog; the tool freezes its canonical semantic hash and refuses identity/variant/name/eligibility drift. Collector #8 has an archival derived copy only, with **no face or mask binding**. Exactly the existing 15 holo identities get masks; nonholos do not. Pikachu retains its existing `yellow-cheeks` ID. The initial wrapper-front binding selects Charizard unambiguously; derived Blastoise and Venusaur fronts remain available, with no pack RNG change.

## Private provenance / runtime schema

Runtime JSON is schema 1, UTF-8 without BOM, and uses only existing known root/entry fields plus the two agreed additive role values `wrapper-front-mask` / `wrapper-back-mask`. It contains lowercase SHA-256 hashes of **derived bytes**, not original-byte hashes. Root status is `user-supplied-private-materials`; rights remain `unknown`, redistribution permission `not-established`, and every binding carries the private-use/no-redistribution restriction.

Card-face `source` provenance records the user's explicit approval of first-edition scans as replaceable visual substitutes for the existing Unlimited simulation. This is not a claim that the scans are Unlimited, a new catalog printing, permission to redistribute, or an intrusive on-card warning. Original source-format metadata and source/derived pins are retained in the generation's separate `Inventory.json`, not added as unknown runtime fields. The historical intake document is read-only and is not rewritten to erase its earlier printing-decision findings.

## Publication / fail-closed behavior

1. Validate fixed paths and every ancestor before use: reject traversal, UNC/device paths, symlinks/junctions/reparse points and regular-file hard links. Source names/counts must match the exact inventory; missing/foreign/duplicate paths are refused before derivation.
2. Acquire an exclusive cooperative `prepare.lock` **inside `.artifacts/materials-authoring`**, never in the source folder. Refuse an existing lock without waiting, polling or assuming it is stale; remove only the operation's verified lock identity/content.
3. Validate all 114 original pins; validate the frozen canonical catalog and strict UTF-8 JSON (no duplicate keys, BOM or nonfinite numbers). Verify PNG chunks/CRCs, header bounds, full decode and IEND; reject malformed/truncated files, animation and trailing payloads.
4. Exclusively allocate an owned staging directory under `.artifacts/materials-authoring`. Encode RGBA8 losslessly and verify every derived decode against the entire original RGBA byte array. File/generation identifiers, entry ordering and JSON formatting are deterministic for identical inputs/toolchain. Pillow, Python and zlib versions are recorded; identical output across arbitrary encoder-version upgrades is not promised.
5. Re-read the source pins and metadata before publication. An existing destination must have the exact ownership marker, complete verified generation receipts and unmodified image/manifest hashes. Refuse foreign destinations, unexpected members, changed outputs or collisions; never silently repair/overwrite foreign files.
6. Publish a complete content-addressed generation directory **before** its manifest. A first manifest uses atomic no-replace hard-link publication; fail closed if unsupported. Updating an owned prior manifest rechecks its expected bytes and atomically replaces only that manifest last. A cooperative lock serializes tool instances. Static path checks and expected-byte checks are not an OS sandbox against an adversarial external writer racing every filesystem operation.
7. Failed preparation never publishes a partial JSON manifest. Publication fault tests retain the previous valid manifest/generation. A completed but unreferenced owned generation can remain after an interrupted last-manifest publication and is validated/reused on retry. Older owned generations are not deleted. This is **manifest-last generation publication, not multi-file crash atomicity**. A process kill may leave a lock/stage requiring explicit inspection; the tool does not delete presumed stale locks.
8. Idempotent reruns revalidate original/derived hashes and pixels and leave destination bytes **and modification times** unchanged. Report/hashpin files are rewritten only when their contents change.

`Owner.json` and `Inventory.json` are authoring receipts, not runtime manifest extensions. Do not remove them or manually replace derived images and expect a rerun to adopt the result. Future deliberate art edits should update reviewed source/intake pins and use the tool to publish another owned generation. Permission to use this folder remains private; runtime images are not repository/public assets. The parent owns ignore/manifest-only Git policy; this worker staged/added no binaries.

## Explicit native Unity sidecar compatibility

Native metadata adoption is **parent-controlled and explicit**. The authoring tool never generates, copies, adopts, rewrites, repairs or deletes `.meta` files. After reviewing exact native-generated sidecars, the parent may place them beside existing owned assets and supply the optional **source-root** ledger `docs/verification/Base_Set_Materials_Unity_Meta_Pins.json`. The ledger is outside `Assets`, so this does not introduce a recursive ledger `.meta` requirement. There is no alternate ledger path, discovery mode or arbitrary foreign-file allowlist.

The exact JSON schema is:

```json
{
  "schemaVersion": 1,
  "originStage": "reviewed native generation stage (provenance only; never traversed)",
  "files": [
    {
      "path": "Assets/Supplied/BaseSetMaterials/Manifest.json.meta",
      "sha256": "<64 lowercase hexadecimal characters>",
      "bytes": 158,
      "guid": "<32 lowercase hexadecimal characters, not all zero>"
    }
  ]
}
```

The placeholders illustrate the schema, not executable pins. Root and row keys must match exactly; duplicate JSON keys, duplicate paths/GUIDs, BOMs, invalid UTF-8, malformed JSON, nonfinite numbers, boolean version/byte values and unknown fields are refused. Limits are 1 MiB per ledger, 4,096 rows, 64 KiB per sidecar, 256 characters per path and 256 non-control characters for a nonempty, trimmed `originStage`. The stage string records provenance only and grants no filesystem access.

Only the following exact sidecar relationships can be pinned:

- `Owner.json.meta` and `Manifest.json.meta` beside their existing owned JSON files.
- `generations.meta` and `generations/<64-lowercase-hex-generation>.meta` beside existing generation folders.
- `generations/<generation>/Inventory.json.meta` and `<exact-owned-PNG-name>.meta` beside their existing owned files.

All paths must be canonical forward-slash source-root-relative paths under `Assets/Supplied/BaseSetMaterials/`. Absolute/UNC paths, backslashes, traversal, foreign PNG/JSON names, `.meta.meta`, `.cs`/`.yaml` and their sidecars cannot be authorized by a ledger entry. Every listed sidecar must exist as a regular file, match its exact byte count and SHA-256, be UTF-8 native metadata with `fileFormatVersion: 2`, and contain exactly one canonical top-level `guid: <ledger GUID>` line. All root/ancestor/leaf reparse/symlink and regular-file hardlink checks precede leaf reads; reads are bounded. Corresponding assets must exist with the correct file/folder type. Exact ownership, complete generation membership, original generation receipts, all derived hashes and manifest bindings are still independently validated; pins never authorize changed art or receipts.

A missing ledger retains the previous strict rejection of every sidecar. Invalid ledgers, missing pinned files, unlisted or mutated sidecars, missing/wrong corresponding assets and unsafe filesystem links fail closed without changing those files. Valid pins are excluded only from directory-member checks; neither `prepare(root)` nor `validate_destination(destination, cards)` changes its call signature. The runtime manifest schema, deterministic generation ID, original/source pins, pixel conversion, publication protocol and default paths are unchanged. These remain static/cooperative checks, not protection against an adversarial writer racing every filesystem operation.

### Compatibility verification

- `meta-compat-01-red.log`: the new pinned-coexistence fixture failed against the prior strict directory-membership rule. `meta-compat-01-green.log` retains an unsuccessful first implementation run (the Inventory JSON filename needed its exact uppercase spelling); `meta-compat-01-green-fixed.log` records the passing corrected run.
- `meta-compat-full-green-final.log`: **30 tests pass, zero skips**, including all 19 previous tests and 11 additive compatibility/security tests. Real file symlinks, hardlinks and an ancestor Windows junction were rejected before the linked metadata leaf was opened. The depth-negative probe also passed immediately; `meta-compat-02-depth-red.log` is retained with its actual exit 0, not claimed as RED evidence.
- After the parent supplied its ledger and all **113 exact native asset sidecars**, `meta-compat-actual.log` records a successful real CLI preparation (exit 0). Before/after hashes and nanosecond mtimes matched for **342 tracked files**: destination assets/sidecars, all 114 originals, the ledger and the old empty production manifest. No native metadata was adopted or edited by this compatibility worker.
- Actual ledger SHA-256: `35f024fec720639da6f51f87f52825d50de5000cb55f2723da24de0ab7c5702c`. Runtime manifest remains `7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be`; generation/source pins below remain unchanged. The rerun reports 121 bindings, 107 content/semantics resources, 110 lossless derived PNGs and 114 preserved originals, retaining the parent's reporting fix.

Rerun command, from the source root, after any future reviewed parent adoption: `python -B tools/prepare_base_set_materials.py`. Authoring compatibility is not strict native compiler-closure/build evidence; the parent must separately rerun that gate against its adopted exact metadata. No main Editor, GUI, save, accepted-build or human-visual acceptance is claimed here.

## Parent reporting correction after independent review

Independent review rejected one reporting-only defect: runtime totals counted distinct filenames instead of the native cache key. Parent added a genuine duplicate-content/different-filename fixture, observed RED, then corrected reports to deduplicate verified derivedSHA + linear-mask/sRGB semantics + dimensions. `distinctRuntimeFileCount` separately reports path count. Full suite now **19tests pass** (`accounting-green.log`), including121bindings with106resources when two face paths contain identical pinned bytes. Parent actual preparation rerun succeeds and keeps the existing manifest unchanged; the actual corpus remains107resources/51479000pixels/205916000RGBA bytes. No derived art, publication protocol, or runtime library change accompanied this correction. Targeted independent re-review remains a separate gate.

## Actual verification and pins

Preparation and independent full-pixel audit both completed successfully. Actual idempotent rerun exited 0; all destination bytes/mtimes remained identical. No original-byte differences were found across all 114 sources.

- Generation: `ac0a72cb65e509f252c73d7692d84c691f9b71718127a5ecf45563db4ab75bbb`
- New runtime manifest SHA-256: `7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be`
- Generation inventory SHA-256: `2b6c8a7def6319819104c37c632660fab5a4ae37ea06152dd07aca903f7a868f`
- Canonical ordered source-pin-list SHA-256: `be22d072b0334b874fec51b79804245618fccf0d20fdf2d895aeca4139e1cdd9`
- Existing catalog byte SHA-256: `0a72cd298d70a2039a9e25b092f76d8d13929117cbe7a2948c2286279122b680`
- Existing empty production manifest SHA-256: `f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8`
- Authoring toolchain: Python 3.14.9, Pillow 13.0.0, zlib `1.3.1.zlib-ng`.

Evidence retained under **`.artifacts/materials-authoring/`**:

- `Prepared_Report.json`: actual manifest/generation paths, counts, memory accounting and toolchain.
- `Source_Hash_Pins.json`: all 114 original byte counts/SHA-256 pins.
- `Actual_Verification.json`: independent reconciliation, source codec counts and 110 per-image source/derived/full-RGBA hashes and full-array equality results.
- `actual-preparation.log`, `actual-rerun.log`, `actual-verification.log`.
- Numbered `01`–`08` RED/GREEN logs: codec, end-to-end preparation, input preflight, publication recovery/idempotence, real runtime path compatibility, safety guards, executable CLI and prior-manifest identity. Unsuccessful/error logs remain honestly retained, including the initial missing-API TypeError before the corrected assertion-based RED.
- `08-publish-identity-green.log`: **18 tests passed**. The suite includes exact coverage, masks, roles/duplicates/order, original dimensions/pixels, readonly source files, old manifest preservation, malformed/truncated input, foreign destination/tampering, late collisions, injected first/update manifest failures, cooperative lock refusal, actual Windows junction/hardlink refusal, source mutation during preparation, strict JSON and a real subprocess CLI run.

All disposable fixtures and child outputs are allocated below `.artifacts/materials-authoring`; the tests do not read live collections or traverse save/build directories. The bracketed repository path prevents the terminal tool's `workdir` option; commands use a quoted `cd` and native-forward-slash script paths instead.

**Scope of evidence:** Pillow fully decoded every original and derived PNG and compared whole RGBA arrays. This does **not** claim Unity/native image decoding, player integration, visual/human acceptance, rights clearance, or a new accepted build. Those remain parent/integration responsibilities.
