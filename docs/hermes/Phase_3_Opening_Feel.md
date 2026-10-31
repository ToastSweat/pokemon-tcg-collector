# Phase 3 — Opening feel and supplied materials

Status: AUTHORIZED / INITIAL ASSET INTAKE. User explicitly answered “yes” to the Phase 3 approval request. This authorizes Phase 3 only, not binder/physical tracking, other sets, shopping or finance. No Phase 3 gameplay implementation or candidate build exists yet.

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
- Material readiness/source location is pending user response. Placeholder-first support is a valid alternative, not a reason to fabricate branded assets.

## Tasks

- [x] Record explicit Phase 3 approval and inspect supplied-material inventory.
- [x] Verify/copy accepted player baseline without copying or altering saves.
- [x] Prepare project-local intake folders, empty provenance manifest and representative request.
- [ ] Confirm initial asset readiness/location and inspect any supplied files.
- [ ] Freeze one presentation/media contract, implement with test-first checkpoints.
- [ ] Exercise a real candidate and regress behavior/persistence.
- [ ] Independent review and bounded human Phase 3 feel/material gate.

## Session handoff

Phase 3 is approved, but only intake/setup has been performed. Ask the user for the requested small batch or placeholder-first direction. Assets/Supplied/Phase3/{Cards,Back,Wrapper,Masks,Audio} exists under the allowed root; assets and rights are not invented. No code, model, player or live save was changed. Preserve the user's untracked PackageManagerSettings.asset. All project-controlled writes stay under this project root; the prior Unity-system-files exception remains unchanged.
