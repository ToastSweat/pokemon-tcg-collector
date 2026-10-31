# Phase 5 runtime implementation contract — FROZEN 1.0

The parent adopts **FROZEN-RC1** in [Phase_5_Runtime_Discovery.md](Phase_5_Runtime_Discovery.md), including its exact public APIs/DTOs and three-stream file ownership, with the binding clarifications below. This is implementation coordination for the already approved Phase5 work, not another request for blanket user approval.

## Authority and immutable inputs

- User model approval: `docs/verification/Phase_5_Model_Approval.json`, SHA256 `014260958e10bc3c2d9b28018b66026ca73490732e6f618c7b045233101fed9d`.
- Approved weighting input: `docs/research/phase5/weight-supplement/sheet-weight-supplement.json`, SHA256 `7ceb3d2b4a429c927301e77be965efcb506d41228a0324bc2ff43e3d012713ef`; Jungle J1 and Fossil F1 only. The later transcript-hash metadata correction does not change this input or approval.
- New canonical data/models are owned by the approved-model authoring stream in `Authoring/Phase5/ApprovedData`. Do not poll partially written outputs or invent final hashes/approximation strings. Parent will deliver the completed, executed handoff and exact pins before final policy admission. Existing frozen research candidates remain unchanged.
- Existing Base catalog/model/RNG and canonical identities remain exact. Approved sets/targets: Base101 excluding only Base Machamp8, Jungle64 and Fossil62, including their own numbered8.

## Workspace isolation before promotion

Coding streams work in **exclusive parent-approved source-copy workspaces**, not live `Assets`, until parent integrates their complete handoffs:

- A: `.artifacts/phase5-core-work/`.
- B: `.artifacts/phase5-presentation-work/`.

Keep production-relative `Assets/VintageCollector/...` paths inside those workspaces. Copy owned originals before targeted edits and pin original bytes. Reject foreign existing workspace/ownership markers, reparse paths and output collisions. No Git commits/staging by workers. Tests/logs/temp also remain in separate owned project-local roots. Native Unity fixtures, when used, must have their own owned project paths and may not operate the open main Editor.

This permits model proof and coding to run in parallel without invalidating the earlier480-source/eight-player preservation baseline. Parent alone promotes reviewed/assembled source changes after those bounded proofs; no accepted player, save, source original or shared Base runtime manifest is overwritten.

## Core API clarification beyond RC1

RC1's immutable `ApprovedSetDefinition` additionally exposes:

```csharp
public void ValidateOrderedCardIds(string[] orderedCardIds);
```

It validates exact11-slot membership/printing/eligibility and approved within-pool duplicate policy against its frozen specification, **without RNG, regeneration, persistence DTO dependencies or exposing mutable RuleData**. It throws an argument/validation error on invalid input. Persistence converts refusal into its existing unsupported semantic-data failure. Stored tuple fields are separately compared to the admitted definition before this call. `ForLegacyBase` has a Base-only fixed validation specification and no generator authority, as RC1 requires.

Keep the public Catalog-based store/profile/archive signatures and add the registry overloads in RC1. Existing test semantic assertions remain unchanged; new tests should explicitly type null arguments where overload resolution requires it. No wire fields/version/header/manifest/receipt shape changes.

### Strict live session JSON

Reuse the existing `Collections/CollectionJsonShape.cs` grammar before live `SaveProtocol` deserialization, rather than implementing a weaker second parser. This grammar has no collection-store dependency. If the standalone legacy persistence harness requires it, A may make **only the necessary explicit source-list addition** of that existing grammar file to a **staged copy** of `tools/run_phase2_persistence.py`, reporting the exact delta. This narrow test-closure adaptation is allowed; no other old-tool rewrites are authorized. Parent will audit/publish it and rerun the original persistence assertions. Archive and physical shape/range/ownership protections remain intact.

## Presentation and media clarification

Adopt RC1's separate passive `SetSelectorPresentation`, DTO fields/commands, `Phase5SetConfig`, additive `ICardTemplateMedia`, stable `Phase5PresentationMedia` adapter and independent binder browsing. Existing accepted card/result/Back/Generate hitboxes, tilt renderer/shaders and ack-audio semantics remain unchanged.

- Promote the already tested `WrapperVariant.ForTransaction` helper byte-for-byte from `Authoring/Phase5/Code/WrapperVariant.cs`; do not replace its UTF8/FNV policy. Base wrapper binding stays accepted/unchanged; new sets use their three A/B/C fronts and immutable active transaction. Idle preview usesA.
- Template art is **not a supplied per-printing face**. The new branch must retain readable already-revealed catalog name/collector/rarity/kind/finish labels. No hidden metadata/face enumeration and no invented printed stats.
- New identity-neutral templates remain matte until appropriate supplied/explicit masks exist; do not infer a full-face holo/metallic mask or borrow Base masks. Holo identity and fanfare classification remain authoritative metadata. This temporary-art limit must be stated in the later player handoff, not misrepresented as verified final foil art.
- Base uses its original media manifest/face/mask behavior. New texture/clip references are borrowed; do not destroy them. Optional image failure cannot suppress independently assigned native reveal audio.
- Parent binds one stable adapter exactly once; SetContext occurs synchronously before postcommit view/cue publication. Selection/binder/results/resume/export do not acknowledge reveals, play/replay clips, rebuild providers, reload media, or stop a valid ongoing cue.
- Parent creates distinct new-set native audio profile assets, initially referencing existing original placeholder clips, without editing the accepted Base profile. Rare position8 is independently configured for both approved new models.

## Parent integration and final verification gate

Parent owns OpeningGame edits, new Phase5CollectionHost/Phase5MediaHost/Phase5SmokeProbe, native asset promotion/metadata, new BootstrapPhase5 scene, exact-owned Phase5 build stage and separate `Builds/Phase5Windows` output. The old hosts/scenes stay functional; old players/shared manifests/originals/saves remain unchanged.

Final implementation must exercise exact approved model admission/mutation refusal, legacy Base overloads, mixed chronological pack/physical histories, all current save-fault/ack/lease/import ownership gates, hidden-prefix binder rules, set-specific targets, direct/UI selection gates, all three new wrapper variants/resume stability and real native audio/serializer/player flows. Preserve source-index/compiler/candidate closure and independent review; source/managed passes alone do not establish a playable Phase5 result. No shopping, finance, model re-research loop or manual wrapper-picker scope is added.
