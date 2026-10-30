# Phase 1 implementation contract

Model A is user-approved; the Machamp completion exception is unanswered and remains unresolved. Runtime booster catalog has 101 eligible printings; full 102-number source checklist stays in research. Do not turn this into a 101-card completion decision. No binder, persistence, marketplace or Phase 2 scope.

## Frozen data contract

UTF-8 JSON files under Assets/VintageCollector/Data: base-en-unlimited1999.catalog.json and base-en-unlimited1999.model-a-v0.1.json. Schema version 1. Source-derived metadata only; shared placeholder art references, no images. Dataset files belong to parent, do not modify without coordination.

Namespace VintageCollector.Domain. Serializable DTOs use public fields with exactly these names, matching both Unity JsonUtility and .NET DataContractJsonSerializer:

- CatalogData: int schemaVersion; string catalogVersion, printingId, completionTargetStatus, sourceResearch; CardData[] cards.
- CardData: string id, sourceId, displayName, sourceName, setId, collectorNumber, language, edition, printingId, variant, rarity, kind, finish, artReference; bool boosterEligible.
- RuleData: int schemaVersion; string modelId, modelVersion, status, printingId, rngAlgorithm, researchRegister, approvalRecord; string[] approximations; PoolData[] pools; SlotData[] slots.
- PoolData: string id; bool uniqueWithinPack; WeightedEntryData[] entries.
- WeightedEntryData: string cardId; int weight.
- SlotData: string poolId, alternativePoolId; int alternativeNumerator, alternativeDenominator. Empty alternativePoolId means no alternative and requires 0/1; otherwise choose alternative when RNG.NextInt(denominator) < numerator. Remaining choice comes from its selected pool.

Rarity values holo/rare/uncommon/common; kind pokemon/trainer/basic-energy/special-energy; finish holo/nonholo. Collector numbers are unpadded canonical decimal strings; stable IDs contain padded numbers and explicit printing/finish/variant, never display names or asset filenames. All runtime entries currently boosterEligible=true. #8 is deliberately absent, not a missing source record. #73 display corrected while sourceName preserves original. #58 explicitly yellow-cheeks. Match all pool references to catalog printingId.

## Frozen callable API

- [Serializable] public DTO classes above, not Unity-dependent.
- Catalog.FromData(CatalogData data) returns a validated immutable Catalog snapshot. Catalog.Cards exposes read-only CardPrinting values; Catalog.Find(string id) resolves a stable ID. CardPrinting exposes Id, DisplayName, CollectorNumber, Rarity, Kind, Finish, PrintingId, etc. as read-only properties.
- public interface IRandomSource { int NextInt(int exclusiveMax); }
- public sealed class SeededRandom : IRandomSource { public SeededRandom(uint seed); } uses xorshift32-v1 plus unbiased bounded rejection. Reject zero seed explicitly; do not silently map it. Freeze shifts (13,17,5), then translate nonzero raw output to v=raw-1, set L=uint.MaxValue-uint.MaxValue%bound, retry while v>=L and return v%bound. This is rejection over 2^32-1 states, not a zero-inclusive 2^32 domain. Bounds of one consume state. Same seed and call sequence have portable reproducibility; raw/bounded/rejection vectors are pinned in Domain_API.md and tests. Changing this mapping requires a new algorithm version.
- public sealed class PackGenerator { public PackGenerator(Catalog catalog, RuleData rule); public PackResult Generate(IRandomSource rng); } Constructor validates and snapshots every mutable input. Pool entry weights must be positive integers; totals checked for overflow; duplicate IDs/pools, wrong printings/ineligible cards/missing references/invalid alt fractions/over-demanded unique pools are errors.
- PackResult exposes ModelId, ModelVersion, CatalogVersion, PrintingId, Status, RngAlgorithm and a read-only OrderedCardIds collection. Freeze independent ordered contents before any presentation. No transactions, awards or persistence in this phase.
- Catalog is immutable; generation state is local to each call. Mutating DTO arrays/fields after construction or later generation/model creation must not change earlier snapshots/results. Validate hostile custom RNG return values and null arguments.

## Validation contract

Use Unity bundled Mono/C# compiler for actual pure-code execution, with no NuGet/test-framework install needed. Keep all test binaries/reports/logs under .artifacts. Test pure generation, weighted boundaries, seeded reproducibility and invalid input, plus complete integrated catalog/model diagnostics. Batch simulation must state N/tolerances and validate Model A including weighted-without-replacement marginal inclusion, not naively sheet fractions. No native rendering/playable claims. A separate parent-owned Unity Editor bridge will parse the same JSON via JsonUtility, construct the same API, generate diagnostics and prove editor compilation/import integration.

## Exclusive ownership

Core implementation worker: Assets/VintageCollector/Domain/**, Tests/**, tools/run_phase1.py, docs/technical/Domain_API.md, docs/verification/Domain_Verification.md. Strict vertical RED/GREEN TDD, retain logs. No data edits, editor scripts, git actions, research document changes, Unity launch, installs, or writes outside allowed root. Parent owns data, Unity Editor bridge, other documentation, integration and Git checkpoints. Reviewer is read-only.
