# Phase 3 — Opening feel and supplied materials

Status: PHASE 3A REST PASSED / FINAL-CARD FIX RETEST PENDING. User accepted the rest and reported only the missing eleventh-card large preview. The focused fix passed native execution and review; use Final_Card_Preview_Fix.md for the narrow retest. Preserve accepted audio/visual/input/resume behavior. Real-material Phase 3 and Phase 4 remain deferred.

## Current process

Phase 2 is human-accepted: all five delivered checks passed. Accepted code checkpoint d649e46; acceptance documentation 11285bd. Preserve the normal opening/inspection/resume/input behavior, Model A 0.1.0, card identities and exactly-once persistence. Machamp completion target remains unresolved. No supplied image/audio files were present during initial Phase 3 inspection.

Parent verified and copied all 135 immutable player files to `.artifacts/baselines/phase2-accepted-29ed47956943`, matching manifest digest `29ed47956943fbb520133e2d5044e41e2102f22e11a5ad182b153c74dc19e271`. Save directories were excluded and untouched. The accepted player under Builds/Phase2Windows remains the playable baseline; do not rebuild over it during Phase 3 candidate work.

## Projected changes / action plan

1. Receive a small representative batch or confirm placeholder-first continuation. Follow [asset request](Phase_3_Asset_Request.md); do not request all 102 faces or independently download branded assets.
2. Establish replaceable media bindings by stable printing ID and explicit wrapper/back/sound role. Preserve original files/provenance and unknown rights in Assets/Supplied/AssetManifest.json; supplied files remain separate from original code and generic content.
3. Prove one representative presentation slice before bulk processing: improve controlled wrapper opening, stack/reveal timing, inspection, lighting/finish cues and action-coupled sound. Keep click alternatives, skip/order, pause/focus, readable faces and volume/mute. Do not let missing art prevent generic fallback/asset-pipeline work.
4. Apply holo treatment only to an explicit appropriate mask/region while preserving printed artwork. A missing real-face mask must not cause a full-card rainbow substitution.
5. Keep core generator, model and award/save schema unchanged. Replacement media must not change hitboxes, generation, card identities, reveal order or awards. Verify incomplete/missing/mismatched bindings fail safely to labelled placeholders.
6. Build candidates separately under Builds/Phase3Windows (or an explicitly documented equivalent inside this root), preserving Phase 2 and its live saves. Candidate save continuity/test-copy policy must be explicit before a human handoff; never silently reset, merge or overwrite an existing collection.
7. Test real rendering/audio and regress the accepted loop/persistence/native controls. Inspect captures; distinguish audible/synthetic/native evidence and stop for the user's material/feel/performance review before Phase 4.

Exact rendering/API details will be frozen after initial media readiness is known. This is not authorization for a wholesale engine/render-pipeline rewrite or expensive real-time paper/foil simulation.

## Decision log

- Phase 3 explicitly authorized; Phase 4+ remain gated.
- Accepted Phase 2 build and interaction behavior preserved separately; original model/data/persistence remain authoritative.
- Original/private materials only; no scraping, publication or inferred redistribution rights. Manifest starts empty because no supplied material was present.
- Start with Pikachu #58 yellow-cheeks common, Electabuzz #20 nonholo rare and Alakazam #1 holo, plus one back and one wrapper design; sounds/mask optional. Different substitutions require exact printing verification, not filename inference.
- User chose placeholder-first support and generic polish; branded materials are deferred, not fabricated or scraped. The real supplied manifest remains empty. Concrete API/ownership and evidence gates are frozen in ../technical/Phase_3A_Implementation_Contract.md.

## Tasks

- [x] Record explicit Phase 3 approval and inspect supplied-material inventory.
- [x] Verify/copy accepted player baseline without copying or altering saves.
- [x] Prepare project-local intake folders, empty provenance manifest and representative request.
- [x] Confirm placeholder-first direction; no supplied files exist to inspect yet. Real-material intake stays deferred.
- [x] Freeze shared media/renderer contract and exclusive work ownership.
- [x] Prove separate candidate path/save-rebuild admission policy with observed RED/GREEN cycles: two pure Mono checks passed; populated candidate saves block rebuilding without modification.
- [x] Complete safe media library/original foley and passive renderer polish with test-first evidence.
- [x] Receive completed media and renderer implementations; parent reran 13 pure media checks, 39 motion/layout checks and two build-policy checks. Isolated native/graphics reports remain distinct from the main candidate.
- [x] Build separate Windows candidate with zero errors/warnings and preserve original scene/player/source/save behavior.
- [x] Run real production regression: five suites/111 driver checks/60 validated captures, including native mouse/Escape, hard kill/resume and failed-save Retry.
- [x] Run core-connected media/audio suite: 61 driver checks/34 validated captures, actual cue PCM/DSP, mute/no replay, real-loader synthetic bindings and bad/missing-mask fallback. Pixel checks confirm effects stay in the explicit mask and printed bands remain unchanged.
- [x] Regress 109 persistence checks, accepted generator/distribution and 13 controller groups/297 assertions using the exact-source stage.
- [x] Both cycle-two independent reviews passed; parent rechecked 77 source pins, all 136 candidate files and all 135 accepted Phase 2 files before closing documentation. No reviewed source/build bytes changed afterward.
- [x] Verified local checkpoint `5c4566a` recorded (`[verified] feat: add safe replaceable media and generic opening polish`). Stop for the linked generic Phase 3A human checklist. Branded-material gate remains deferred.

Closing documentation is a separate nonbehavioral checkpoint. The pre-existing untracked PackageManagerSettings.asset remains excluded and unchanged; no remote is configured.

Review record: ../verification/Phase_3A_Independent_Review.json. Historical rejected review and failed intermediate capture/audio attempts are retained, not relabelled as passes. Current corrected manifest digest: `c363cf2242c5e5b91089967f13ed5be67bf66284dc1175e6f8ba0cc659e21db9`.

## Candidate / data separation

Player: `F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/Builds/Phase3Windows/VintageCardCollector.exe`. This candidate has its **own new collection** under its own Saves folder. Accepted Phase2 player/collection remain untouched; no automatic migration or merge. Source scene: Assets/VintageCollector/Scenes/BootstrapPhase3.unity. Editor play continues using the existing separate Editor save location.

Real manifest still contains zero supplied assets. You should see generic art and hear original generic action foley, not branded material. The loaded samples and native DSP were verified, but hearing/quality still needs the user. Full test reproduction and limits: ../verification/Phase_3A_Parent_Verification.md; candidate pins: Phase_3A_Corrected_Player_Manifest.json.

## Session handoff

User passed the six-item group except final ordinary-Next card preview. Keep those passes; don't reopen accepted foley/volume/feel. The minimal OpeningGame presentation patch reuses final inspection after durable acknowledgement/cue; Skip and completed restart stay direct, with unchanged model/schema/awards. It is built separately under Phase3FinalCardWindows and reviewed, with its own new test collection; all previous players/saves remain untouched. Stop for only the focused final-card/Back/Skip retest. Branded material/Machamp target/Phase 4 remain deferred.
