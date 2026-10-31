# Phase 5 — approved provisional Jungle/Fossil models

## Current status and scope

**Authoring and pure C# generation proof: complete. Runtime admission: not performed.**

The current authority is `docs/verification/Phase_5_Model_Approval.json`: the user's explicit selection, **“Use the sheet-informed provisional models for both sets”**, approves Model A version `0.1.0`, status `approved-provisional`. Its SHA-256 is `014260958e10bc3c2d9b28018b66026ca73490732e6f618c7b045233101fed9d`.

The older research registers, candidates and weight-supplement reports remain unchanged point-in-time evidence, including their earlier unapproved labels. This document and the new provenance manifest record the later approval; approval does not authenticate factory collation or physical reveal orientation.

**Current process:** the unchanged generic `Catalog.FromData`, `PackGenerator(Catalog, RuleData)` and `Generate(IRandomSource)` APIs consume isolated authoring JSON. Production `Data.cs`, `Catalog.cs`, `Random.cs` and `PackGenerator.cs` are compiled as a separate `Domain.dll`, with `-warnaserror+ -target:library`; the proof executable references that DLL. No Unity API, scene, save, collection award or player is involved.

**Projected integration, not implemented here:** admit these exact model/catalog contracts through a separately authorized runtime selection, serialization and persistence checkpoint. There is still no playable Jungle/Fossil mode in the existing players.

### Delivered authoring contracts

All new data lives under `Authoring/Phase5/ApprovedData/`:

| Printing | Catalog version | Completion status | Model ID | Model version/status |
|---|---|---|---|---|
| `jungle-en-unlimited1999` | `jungle-en-unlimited1999-0.1.0` | `approved-standard-checklist-64` | `jungle-en-unlimited1999-sheet-informed` | `0.1.0` / `approved-provisional` |
| `fossil-en-unlimited1999` | `fossil-en-unlimited1999-0.1.0` | `approved-standard-checklist-62` | `fossil-en-unlimited1999-sheet-informed` | `0.1.0` / `approved-provisional` |

The two `.catalog.json` files are promoted copies of the frozen `Authoring/Phase5/Data/` candidates. Only `catalogVersion` and `completionTargetStatus` change. All cards, list order, stable IDs, source IDs, source names, eligibility and other identity metadata remain exact. `sourceResearch` remains the candidate's declared path:

- Jungle: `docs/research/phase5/jungle/claim-register.md`.
- Fossil: `docs/research/phase5/fossil/Fossil_Research_Register.md`.

The two `.rules.json` files use exactly the public-field `Domain.RuleData` schema, without extra source-table fields masquerading as domain fields. Source-table identity is retained in `approximations` and the separate `Provenance.json` manifest. `researchRegister` points to the respective research register, `approvalRecord` to the actual approval above, and `rngAlgorithm` is `xorshift32-v1`.

## Approved model parameters

| Set/source-qualified table | H weight total | R weight total | U weight total | C weight total |
|---|---:|---:|---:|---:|
| Jungle J1: `jungle-1-removed` | 110 | 121 | **120** | 121 |
| Fossil F1: `fossil-1` | 110 | 121 | 121 | 121 |

The authoritative weight input is `docs/research/phase5/weight-supplement/sheet-weight-supplement.json`, SHA-256 `7ceb3d2b4a429c927301e77be965efcb506d41228a0324bc2ff43e3d012713ef`. Each ordered pool entry copies the matching table row's exact `id` and `multiplicity`; no guessed grouping, averaging, uniform replacement or total-only validation is used.

- Standard English Unlimited numbered holo/nonholo checklists: Jungle 64, Fossil 62. No added error, promo or alternate-print identities.
- Exactly eleven slots: seven C, one R with alternative H at exact probability `1/3`, then three U. H replaces R; there is never a second rare slot.
- C/U use identity-level weighted sampling **without replacement**, `uniqueWithinPack: true`. Removing an identity removes its entire weight. R/H are separate single-draw pools with `uniqueWithinPack: false`.
- No extra Energy slots. Named Trainer cards such as Energy Search retain their actual catalog identity.
- Jungle's removed blank is not a card and does not become U total 121. No Ivy promo, J3 averaging, box quota, pity or factory-strip state.
- Collector number 8 is eligible in both new sets; the Base-only starter exclusion is not generalized.
- Packs are modeled as independent loose packs with fresh pack-scoped uniqueness. Wrapper visuals do not consume generator RNG.
- Rare-or-holo at display position 8 is the approved **provisional order**, not authenticated front/back wrapper orientation.

## Reproducibility contract

The production RNG is unchanged: unsigned xorshift32 shifts `(13,17,5)`, legal seeds `1..UInt32.MaxValue`, zero refused. Raw support is `1..2^32-1`; bounded draws use `v=raw-1`, `M=UInt32.MaxValue`, `L=M-M%bound`, reject `v>=L`, then return `v%bound`. Bound-one calls consume state.

A pack consumes twelve bounded calls: seven C tickets, one branch ticket in `[0,3)`, one ticket from only the selected H/R pool, then three U tickets. Branch ticket 0 selects H, tickets 1 and 2 select R. Ordered source entries and whole-identity removal are part of the versioned contract.

Seven fixed seeds per set (`1`, `2`, `3`, `42`, `0xffffffff`, `0x4a554e47`, `0x464f5353`) are checked against an independent Python oracle. The comparison covers the complete ordered IDs, all provenance/version fields, and every bounded draw's bound and ticket—not merely the holo count or an unordered set.

## Observed RED → GREEN evidence

`.artifacts/phase5-models/` contains the retained `tdd-01` through `tdd-11` logs. These are vertical slices, not a test pile written after implementation:

1. Missing promoted catalogs → exact promoted copies.
2. Missing approved RuleData → complete ordered per-card vectors and slot contract.
3. Missing approved-configuration guard → 30 rejected mutations, including total-preserving weight swaps, entry/slot reorder, probability and uniqueness changes, foreign IDs, source/scope/approval drift, wrong source table, and bool/int/float type substitutions.
4. Missing owned publication → idempotence and refusal of unrelated/changed/escaped outputs.
5. Missing CLI proof → separately compiled production domain and complete fixed diagnostics.
6. Missing executed boundary groups → all integer tickets of every real pool, including each C/U deterministic prefix-removal stage, both branches and all three threshold tickets; detached immutable IDs/results; hostile RNG refusal without subsequent state leakage.
7. Missing numerical oracle → weight-group target-exclusion DP checked against exhaustive ordered enumeration on small unequal-weight populations.
8. Missing actual full sampling → complete diagnostic archives, correct weighted-WOR expectations, statistical checks and separate deterministic replay.
9. Missing execution reconciliation / accepted boolean failed-count → real child-process failure, zero-check, malformed JSON, declared-count and duplicate-seed refusal.
10. Accepted duplicate JSON keys / root identity drift → strict key uniqueness, exact root identity, and an actual Windows junction refusal test.
11. Missing separate review audit → explicit review snapshot, historical metadata drift and during-run immutability accounting.

An unsuccessful early `tdd-03-green.json` is retained with its actual nonzero exit: the first input verifier misread review records as digest strings rather than `{live,index}` objects. The corrected run is `tdd-03-green-fixed-pin-shape.json`. Names containing “green” do not override recorded exits.

The first `tdd-09-red.json` also records an unrelated `.gitattributes` drift refusal; the intended behavioral RED is preserved separately as `tdd-09-red-after-external-attribute-audit.json`.

## Mathematical expectations and sampling policy

Before generation the runner writes `sampling-policy.json` and `expectations-before-generation.json` into the fresh evidence directory.

C/U inclusion is **not** `drawCount * weight / startingWeightTotal`. For each target weight, the DP tracks counts of removed other identities by weight group. The target remains in every denominator. Each state's mass times `targetWeight/remainingTotal` contributes to the next positional marginal; transitions selecting another identity propagate the surviving target-exclusion mass. Summing the target's positional marginals gives its per-pack inclusion probability. The oracle is checked against exhaustive small-population ordered enumeration and conserves both every slot's probability and the requested total number of draws.

H/R expectations use exact branch probability times the actual per-card weight share; the same identity counts are also tested conditionally on the observed branch count. Position checks use DP positional marginals, not inclusion probability divided by the number of slots.

The predeclared regression threshold for each count is:

`abs(actual - n*p) <= 6*sqrt(n*p*(1-p)) + 2`.

Packs are trials for identity inclusion. Position and conditional checks are additional correlated checks, not additional independent samples. This threshold is a deterministic simulation regression guard, not a factory confidence interval or evidence that the historical factory used these probabilities.

## Final exercised results

Primary report: `.artifacts/phase5-models/full-5202c4269df44719b705067e46d64c63/report.json`.

Report SHA-256: `f1b783938554dfd6e7b36672fae27a6ecbd6a6366a855ef3c349e7161dd54edb`.

- Python suite: **16 tests passed**, no skipped tests (`.artifacts/phase5-models/final-tests.json`). Expected negative subprocess compiler/runtime/zero-check diagnostics are deliberate passing refusal tests, not production compilation failures.
- C# primary generation: **14 named proof groups passed**, separately compiled domain and harness, warnings treated as errors.
- Complete primary diagnostic packs: **100,000 per set**, each with eleven ordered IDs, provenance and twelve bounded-draw records. Every line was decompressed, parsed, replayed against its actual weighted tickets and reconciled with all counters/digests.
- Identical second C# execution: another 100,000 per set, explicitly **zero additional statistical trials**. Fixed diagnostics, counts, proof and complete uncompressed archive digests match.
- All **572** declared statistical checks passed. No tolerance was adjusted after observing counts.

| Set | Stream seed | H packs | R packs | Statistical checks | Maximum deviation in sigma units | Exhaustive ticket cases |
|---|---:|---:|---:|---:|---:|---:|
| Jungle | 1247104583 (`0x4a554e47`) | 33454 | 66546 | 289 | 2.3522736834459774 | 1253 |
| Fossil | 1179603795 (`0x464f5353`) | 33360 | 66640 | 283 | 3.456121877947436 | 1254 |

Each set's primary archive contains 1,100,000 cards. Archives are `<printingId>.diagnostics.jsonl.gz` beside the report; rerun archives are under `deterministic-rerun/`. The complete uncompressed ordered-diagnostic SHA-256 values are:

- Jungle: `c4400cd930f2d25bb780f2af4e9e6264c1e285aec1a1bc3176fdfb60d8eddf81`.
- Fossil: `0bb8beccc5d59eb36d4a13f6d4ea28f68b007869921cc13cd28f699986fcf994`.

`mutation-count-proof.json` independently enumerates the 30 actually exercised approved-configuration mutation cases.

### Preservation and input boundaries

The final run pins and rechecks **217** effective data/schema/source/harness inputs. Authoring provenance pins the frozen candidates, actual approval and weight vectors, source PNGs and manual transcription. The independently maintained evidence finalizer, quote-hash metadata and verification proof are deliberately **not generator inputs**; their metadata-only correction does not change the approved numeric model.

The complete root source membership/bytes match `.artifacts/phase5-preservation/before.json`: **480** files under Assets/Packages/ProjectSettings, **8** existing players / **1088** immutable player files, and **416** original material/audio files. Saves/Logs are pruned case-insensitively before stat, descent or read. No player was launched and no save was opened.

All **192** independently reviewed live files are separately snapshotted before/after and unchanged during the final run. There is one explicitly recorded historical difference: an external worker added the weight/ApprovedData text-policy lines to `.gitattributes`, changing its live digest from `8a360efcb87e2290b4f0ff2d867ce08b04532d4d3504915c540cf8e3e78ec2ea` to `76cecba2787dd98a30ca9ba88cdf797e39443d04e2bebad838a58e26a89d379f`. This worker did not edit that file or repin the original review record. Git text policy is audited separately rather than treated as numeric generator authority. See `review-before.json`, `review-after.json`, `reviewHistoricalDrift` and `external-review-drift.json`.

Publication only permits the exact approved authoring directory or model-owned artifact descendants. Existing unrelated, partial or modified authoring directories are refused before writes; matching outputs are returned unchanged, without rewriting their mtimes. Paths are canonicalized and reparse points/junctions refused. No original candidates, production source, staged review member or old research report was edited by this worker.

## MODEL-REPRO-001 correction and current reproduction

The earlier independent verdict remains rejected and unchanged at `.artifacts/phase5-model-independent/review.json`; its retained copy, the actual unmodified CLI failure, and three observed RED → GREEN slices are under `.artifacts/phase5-model-repro-fix/`. This correction addresses only MODEL-REPRO-001, not the nonblocking proof-strictness or crash-atomic-publication suggestions. **Subsequent independent corrected-source rereview passed**, including the real CLI, unchanged five-file bytes/mtimes, numerical proof and exact legacy reflection adapter. See `docs/verification/Phase_5_Models_Independent_Review.json`; the earlier rejection remains historical evidence.

`HISTORICAL_REVIEW_EVIDENCE` explicitly classifies **only** `docs/technical/Phase_5_Data_Preparation.md`. That publication/checkpoint document is not read to derive any catalog, numeric vector, schema or model approval. Its original review digest `fc4685b96b932cc805ceacd81a71b61fa19657c78647a13aec637f4f70f05f5e` remains an exact required member of `source_pins()` and `Provenance.json.inputs`: neither removal nor repinning is accepted. `source_pins()` retains the historical receipt while enforcing all other existing frozen guards; `effective_source_pins()` omits just this inert receipt from current generation/run-input comparison. `build_documents()`, exact typed `validate_documents()`, `read_approved()` and publication still require the original five document payloads. Approval, full weight vectors, candidates, research source bytes, domain/schema/executable guards and source ordering are not exempted. There is no extension-based Markdown exception.

The authoring CLI separately captures `publication_observations()` before/after and fails on `Publication observation drift` if current publication bytes change during the operation. Its JSON exposes both observations. The runner writes `publication-before.json` / `publication-after.json`, requires `publicationObservationsUnchanged`, and additionally retains the original complete review audit. Current publication digest `0e0275d9e14c0d5381ca9558e8ad87659089454441a5c56ed1300e4feadbad63` is an observation, not a replacement frozen receipt. `.gitattributes` retains its existing separate Git text-policy audit and remains outside provenance/numeric authority.

Current unmodified `--check` and `--write` both exit 0 (`verified` / `unchanged`), with identical current publication observations. All five approved files retain their bytes **and nanosecond mtimes**, including provenance SHA-256 `ee2a9a5d575b872b20611a49e1a1dadbf7b29b7cfcf2fd1ac9764b680ba92fed`. Fresh disposable tests cover valid publication-only updates, immutable provenance membership/digest, actual approval/vector/candidate/research/domain drift refusal before publication writes, and a publication mutation between real C# generation and deterministic rerun. The fixture-only runner substitutes the recorded preservation baseline because those small disposable roots contain no live players; the full corrected-source proof separately performs the real preservation scan.

The full Model suite now passes **21 tests, zero failures/errors/skips**; its full-generation case executes 100,000 primary packs per set plus identical replay, all 14 C# groups and all 572 statistical checks. The separately executed fresh proof is `execution/MODEL-REPRO-001-full/report.json`, SHA-256 `b76f8aebae9150b6d6a27f7be23dc95a8cc1047fa16709d249d315bc952c72db`; `fresh-reconciliation.json` additionally parses every primary/replay archive line, independently advances the continuous PRNG stream through every bound/ticket, reconciles every counter and checks the expectations using full identity-subset DP. See `full-model-suite.json`, `suite-result.json` and `execution/latest-full-test.json` in the correction directory. To honor this worker's artifact-only write boundary, `exercise.py` routes only artifact output/read paths (including child runner invocations) into `phase5-model-repro-fix/execution`; no numeric data, source hashes, test assertions, generation code or statistical policy is replaced. The normal CLI defaults are unchanged. Historical counts and reports above remain point-in-time evidence, not rewritten receipts; the corrected runner separates the publication observation from effective inputs and pins the additional regression test.

## Reproduce

Run from Git Bash; choose a fresh evidence label (existing labels are refused):

```bash
cd 'F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG'
python -B tools/prepare_phase5_models.py --check
python -B tools/prepare_phase5_models.py --write  # unchanged/idempotent when already matching
python -B -m unittest discover -s TestsPhase5 -p 'Model*Tests.py' -v
python -B tools/run_phase5_models.py --label approved-models-proof-001 --packs 100000
# Pure fixed-diagnostic/boundary smoke only, not statistical evidence:
python -B tools/run_phase5_models.py --label approved-models-smoke-001 --packs 0
```

The runner uses the installed Unity `6000.2.14f1` bundled Mono compiler/runtime; no download, package installation, artwork/music retrieval or Unity launch is needed. Commands, actual exits/stdout/stderr, policies, expectations, input pins, preservation manifests, C# proof, archive readbacks and statistics are retained inside `.artifacts/phase5-models/`.

## Artifact digests and runtime-contract readiness

| New data file | SHA-256 |
|---|---|
| `jungle-en-unlimited1999.catalog.json` | `60fc385e944778008e284c96d0038411238c0b44607b370537c825b9c88a0355` |
| `jungle-en-unlimited1999.rules.json` | `0d58b66ee91099bf6ad47845d62751d523404bd0dc866a62f1897041972bff12` |
| `fossil-en-unlimited1999.catalog.json` | `e724f63927dd66a819c02ab4dd14408ccd0affd2bee6d5a8caa7908ed5f69860` |
| `fossil-en-unlimited1999.rules.json` | `eff5082e6f11efbe638d62cfbd314bda1e2fb8d54b5c7534d3a77956cd10353e` |
| `Provenance.json` | `ee2a9a5d575b872b20611a49e1a1dadbf7b29b7cfcf2fd1ac9764b680ba92fed` |

**Ready for a separately scoped runtime-contract integration:** preserve the exact printing/model/catalog IDs and versions, entry order, RNG call order, eligibility, approval reference and immutable result provenance. The next gate must exercise the actual native JSON adapter/JsonUtility boundary, explicit model selection, persistence/migration compatibility and player behavior without altering Base Set or live saves. Standalone DataContractJsonSerializer parsing and pure generator proofs do not establish native Unity parsing parity, runtime admission, visual/audio integration or human playtesting. No exact factory collation or authenticated physical order is claimed.
