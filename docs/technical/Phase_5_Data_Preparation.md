# Phase 5 data-preparation checkpoint

## Current process / projected changes

The accepted game remains Base Set only. Independent Jungle/Fossil research and data authoring are now active, alongside six temporary wrapper-front variants and two card-face templates. Missing final art is not a blocker. No new probability/order model has been silently adopted.

Work is isolated under `Authoring/Phase5`, `TestsPhase5`, set-scoped `docs/research/phase5` directories and explicit tools. Existing `Assets`, `Packages`, `ProjectSettings`, eight players, originals and saves are preserved. These authoring outputs are not described as an already integrated Jungle/Fossil Windows player.

## Implemented and executed read-only catalog index

`Authoring/Phase5/Code/PrintingCatalogIndex.cs` indexes immutable existing `Catalog` instances by exact printing ID and exact stable card ID using ordinal matching. It snapshots the input list, exposes a read-only sequence, rejects missing/empty/duplicate input and does not promote catalog approval status. It has no generator, save/store or presentation authority.

The standalone harness compiles the actual existing `Domain/Data.cs` and `Domain/Catalog.cs` with the new index. Neither `PackGenerator` nor the RNG is compiled into this proof. Managed compilation uses Unity's bundled Mono and warnings as errors; this is **not native Unity serialization/player proof**.

Real vertical-slice evidence under `.artifacts/phase5-catalog-index/`:

| Slice | RED | GREEN |
|---|---|---|
| Exact printing/card lookup | `single-lookup-red-01`:missing index assertion | `single-lookup-green-02`:1 group |
| Refuse empty index | `empty-index-red-03`:empty input accepted | `empty-index-green-04`:2 groups |
| Clear null-input refusal | `null-inputs-red-05`:NullReferenceException instead of argument error | `null-inputs-green-06`:3 groups |

`index-regression-07` passed seven groups, also checking cross-printing collector-number isolation, duplicate printing-version shadow refusal, detached/read-only input, unmodified approval status and case-sensitive/unknown lookup refusal.

`actual-base-readonly-08` passed eight groups and read the actual accepted Base Set catalog through the same domain validator and index: **101 cards**, **15 holo / 16 rare / 32 uncommon / 38 common**. Every actual stable ID resolved to its own immutable card. The emitted report explicitly records `packGenerationAttempted=false`, `savesOpened=false`, `nativeUnityPlayerTest=false`.

Actual three-catalog execution **`actual-three-catalogs-09`** passed eight groups after both independent source cross-checks completed: Base101, Jungle64, Fossil62, **227 exact global identities**. The unchanged production catalog validator accepted both candidates; every card resolved through the read-only index without cross-set collector-number aliasing. This remains pure authoring/data proof, not a native player or live model-admission claim. Later explicit model approval is recorded separately in `../verification/Phase_5_Model_Approval.json`; frozen candidate status/version tokens were not rewritten.

Data/index, placeholder-generator and corrected numerical-weight/appearance independent reviews all passed, with their reports under `docs/verification/Phase_5_*_Independent_Review.json`. The original WEIGHTS-001 transcript-hash rejection and its observed correction remain separate historical evidence; no quantitative weights or approval changed.

```bash
python -B tools/run_phase5_catalog_index.py --label fresh-label \
  --catalog-file 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json'
```

Repeat `--catalog-file` for each researched candidate. The runner admits only project-local JSON paths, rejects reparse paths, uses fresh owned output directories, pins sources/data/helper before and after, and requires nonzero reconciled test output.

## Verified integration barriers, not permission to weaken validation

- `Persistence/StateValidation.cs:19–30` deliberately admits only the accepted Base Set model/version/printing, its five-common/two-energy/rare/three-uncommon order and the existing duplicate policy. It also excludes collector8 within that Base-only validator. New sets require exact versioned model/printing dispatch; simply dropping new JSON into the old scene will correctly fail.
- `Persistence/SessionStore.cs:9–11,36` currently owns one Catalog and validates archived entries through that catalog. Mixed histories require per-pack catalog/model lookup while preserving old ordered IDs, flags, acknowledgements and absolute-target Retry behavior.
- `Collections/BinderProjection.cs:19–20` currently excludes collector8 and demands101rows; `CollectionContract.cs:42` returns101as the target. Those are current Base Set rules, **not** reusable Jungle/Fossil completion or collector-number rules. Future set-scoped targets must preserve Base Set's explicitly approved101/Machamp decision.
- `Domain/PackGenerator.cs:57` already scopes its starter-only collector8 exception to the Base printing. Preserve that pattern instead of globally banning number8 in new sets.
- Existing archives/physical tracking/import/export need complete mixed-set validation and legacy preservation before live integration; no speculative schema bump, save rewrite or migration has been performed at this checkpoint.

## Preservation evidence

`.artifacts/phase5-preservation/before.json` records **8 players / 1,088 immutable player members**, all **480 Assets/Packages/ProjectSettings members**, and **416 original art/cry files**. Player Saves/Logs were pruned before traversal or reading. This pre-work baseline was independently rechecked unchanged in `.artifacts/phase5-preservation/after-data-art.json`, including **480 source members / 8 players / 1,088 immutable player members / 416 originals**. That bounded authoring checkpoint preservation is distinct from later explicitly permitted staged runtime source changes; accepted old player/original/shared-media bytes must remain protected throughout integration.
