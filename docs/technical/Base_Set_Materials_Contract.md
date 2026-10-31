# Base Set supplied-materials integration contract

## Authorization / current process

Phase4 accepted by direct user “all pass.”, manifest136members rechecked unchanged, acceptance commit a66e0b5. Source implementation d17213d. User offered Pokemon-Base-Set then explicitly chose: **“ignore that the car faces are 1st edition and use them as you would with unlimited. i can edit them later”**. This authorizes replaceable visual references with the existing Unlimited simulation, not new printing identities/rules. Preserve original files, source hashes and the explicit visual approximation in provenance; no intrusive on-card edition warning is required. No Phase5/other sets/marketplace/finance.

Allowed writes ONLY F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG. All controlled stages/TMP/logs/tests/builds inside. No skill/profile/parent/install/network/publication writes. Main interactive Editor and live saves untouched. Candidate is separate Builds/Phase3MaterialsWindows; preserve Phase4Windows and its save root. No tests use real collections. Prune Saves/Logs before traversal/hashing, case-insensitive.

Existing production Assets/Supplied/AssetManifest.json MUST remain byte-unchanged and empty: accepted older binaries read it at runtime, so changing it would change their presentation without changing binary hashes. New material manifest is **Assets/Supplied/BaseSetMaterials/Manifest.json**, selected ONLY by the new scene/host configuration. Do not overwrite originals or accepted builds.

## Asset preparation worker ownership / contract

Own tools/prepare_base_set_materials.py, TestsMaterials/AuthoringTests.py, Assets/Supplied/BaseSetMaterials/** and docs/technical/Materials_Asset_Preparation.md. No Media/Runtime/Presentation/Domain/Data/Persistence/Builder/settings/Git edits.

Sources:102numbered card PNGs, card back, common holo mask, three wrapper fronts, common wrapper back plus front/back metallic masks. Inventory docs/verification/Supplied_Base_Set_Intake.json:114files,110PNG, all decoded. Most cards are indexed/interlaced; derive noninterlaced8-bit RGB/RGBA losslessly at ORIGINAL dimensions (600x825 cards;260x475 wrappers). Preserve decoded RGBA pixels exactly; no crop/repaint/stamp removal/resizing. Keep originals and source/derived hashes. PSD/JPEG not runtime. Exclude Machamp #8 from runtime face/mask mapping even though source exists.

Prepare all101catalog faces keyed to exact existing full catalog IDs,15holo-mask bindings referencing the same derived base1-foil.png, card-back, wrapper-front(default Charizard), wrapper-back, wrapper-front-mask, wrapper-back-mask. Preserve all three derived fronts for later selection; initial manifest has one unambiguous wrapper-front. Do not introduce visual randomization into pack RNG or alter catalog. No alternate editions/cards inferred. Root manifest schema1 unchanged fields per MediaManifest, roles extended below. rights unknown, source local user folder, private-use/no-redistribution restriction; card-face provenance explicitly records user-approved first-edition-scan visual substitute for Unlimited simulation. No runtime on-card warning necessary. Root status user-supplied-private-materials. JSON strict UTF8noBOM, lowercaseSHA256 exact files under Assets/Supplied. Separate inventory/provenance JSON outside runtime manifest may carry source/derived format details; do not add unknown runtime fields.

TDD authoring tests must prove exact101ID coverage/no8, exact15holoeligible masks/no nonholo, role uniqueness, correct names/variants/catalog IDs, original/derived pixel equality + lossless format + source unchanged, originals/read-only discovery, idempotent deterministic rerun, foreign destination refusal/collisions and malformed/truncated input failure without publishing half a manifest. Use owned staged generation plus publication (manifest last), preserve previous valid output on failures; do not claim multi-file crash atomicity. All fixtures under .artifacts/materials-authoring. Commit no supplied binary/source art; parent handles Git ignore/manifest-only policy.

## Shared frozen media API (media worker implements test-first)

Keep IPresentationMedia unchanged for old providers/tests. Add separate optional interface in PresentationMediaContract.cs:

```csharp
public interface IWrapperMetallicMedia {
 Texture2D WrapperFrontMask { get; }
 Texture2D WrapperBackMask { get; }
}
```

Media worker owns Media/** including additive PresentationMediaContract.cs (observe RED before adding the new interface), TestsMaterials/Media* explicit files, tools/run_materials_media*.py, docs/technical/Materials_Media.md. Add roles **wrapper-front-mask / wrapper-back-mask**, no cardId, masks linear and exact aligned to successfully decoded corresponding wrapper face. No face/mask fallback across roles; no unmasked full-face foil. Existing holo-mask rules unchanged. Masks loaded after faces independent of manifest order.

Append optional trusted flag to existing Load API: `Load(root,catalog,manifestRelativePath=...,allowTestInputs=false,bool fullBaseSetBudget=false)`. Old calls/default16MiPixel policy unchanged. Expose FullBaseSetTexturePixelLimit=64*1024*1024 for explicit new host use only; manifest cannot elevate budget. Unique decoded textures share only by verified content hash + color-space/mask role semantics/dimensions; shared mask bindings must each validate matching face first. Charge UNIQUE decoded pixels, own/destroy each native resource once, read-only lookup remains noIO. Provenance/SuppliedAssetCount counts successful bindings, not unique textures. Remain bounded per-file/dimension/native-decode caps; no blanket weakening or interlace decoder changes. New derived images meet old RGB/RGBA gates.

TDD/nativenative JsonUtility/image parity: default budget still refuses overcap, explicit full budget admits all101faces/15alignedmaskbindings/global roles with no fallback, shared mask reference equality and unique accounting, exact supplied PNG pixels sampled/readback, native disposal/no leaks/reload, invalid/misaligned/orphan/nonholo/duplicate/unknown role/hash/path/schema still safe, no startup AudioSource/playback/RNG/generation/save changes. Pure existing media13 and native49 regressions should run; old test contracts kept. Native tests use separate .artifacts/materials-media-project, not main Editor or parent candidate stage. Asset worker source may arrive later; independent synthetic TDD first, then actual prepared manifest when ready; never wait/poll child artifacts.

## Parent integration ownership

Parent owns additive contract, Phase3MediaHost explicit material-manifest/full-budget serialized options (defaults unchanged), targeted TabletopPresentation card/wrapper mask rendering, resource shaders, new builder/policy/bootstrap/opt-in probe and actual candidate. No domain/persistence/collection edits. Preserve normal card11 hold/explicitresults, all controls/hitboxes/inspection/Skip, no spoilers, exactly-once awards, physical quantities, pause/mute/no replay. Binder text view is not being redesigned; opening/inspection/results consume full art through existing provider. Back/wrapperback native binding must be verified; optional visual demonstration may use explicit smoke-only orientation, not hidden gameplay input.

Wrapper metallic reflection must sample supplied mask and preserve printed color/legibility, use fitted texture rectangle, no full-card rainbow/unmasked inference. Card holo stays inside shared illustration mask, normal cards no holo effect. User scans already contain printed sparkle; animated response must remain restrained. No extra purchased/generated pack, save or RNG calls to animate assets.

Build separate candidate with real source GUID closure and exact staged inputs; native supplied-card/back/wrapper/metallic proof plus existing opening→binder/retry/restore regression. Keep accepted manifest0 unchanged even while new files exist. All real art remains local private content, no repository binary commit or uploads. Record exact original/derived/manifest/native-library/source/player pins, independently review and stop for visual user feedback before any further phase.
