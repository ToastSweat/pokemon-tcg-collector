using System;
namespace VintageCollector.Domain
{
    [Serializable] public sealed class CatalogData
    {
        public int schemaVersion;
        public string catalogVersion, printingId, completionTargetStatus, sourceResearch;
        public CardData[] cards;
    }
    [Serializable] public sealed class CardData
    {
        public string id, sourceId, displayName, sourceName, setId, collectorNumber, language, edition, printingId, variant, rarity, kind, finish, artReference;
        public bool boosterEligible;
    }
    [Serializable] public sealed class RuleData
    {
        public int schemaVersion;
        public string modelId, modelVersion, status, printingId, rngAlgorithm, researchRegister, approvalRecord;
        public string[] approximations;
        public PoolData[] pools;
        public SlotData[] slots;
    }
    [Serializable] public sealed class PoolData
    {
        public string id;
        public bool uniqueWithinPack;
        public WeightedEntryData[] entries;
    }
    [Serializable] public sealed class WeightedEntryData { public string cardId; public int weight; }
    [Serializable] public sealed class SlotData
    {
        public string poolId, alternativePoolId;
        public int alternativeNumerator, alternativeDenominator;
    }
}
