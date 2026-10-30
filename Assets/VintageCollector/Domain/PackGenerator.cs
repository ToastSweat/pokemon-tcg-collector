using System;
using System.Collections.ObjectModel;
namespace VintageCollector.Domain
{
    public sealed class PackResult
    {
        public string ModelId { get; private set; }
        public string ModelVersion { get; private set; }
        public string CatalogVersion { get; private set; }
        public string PrintingId { get; private set; }
        public string Status { get; private set; }
        public string RngAlgorithm { get; private set; }
        public ReadOnlyCollection<string> OrderedCardIds { get; private set; }
        internal PackResult(string[] ids, RuleData rule, string catalogVersion)
        {
            ModelId = rule.modelId; ModelVersion = rule.modelVersion; CatalogVersion = catalogVersion;
            PrintingId = rule.printingId; Status = rule.status; RngAlgorithm = rule.rngAlgorithm;
            OrderedCardIds = Array.AsReadOnly((string[])ids.Clone());
        }
    }
    public sealed class PackGenerator
    {
        private readonly RuleData rule;
        private readonly string catalogVersion;
        public PackGenerator(Catalog catalog, RuleData rule)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (rule == null) throw new ArgumentNullException("rule");
            if (rule.schemaVersion != 1) throw new ArgumentException("Unsupported rule schemaVersion");
            Catalog.Require(rule.modelId, "modelId"); Catalog.Require(rule.modelVersion, "modelVersion");
            Catalog.Require(rule.status, "status"); Catalog.Require(rule.researchRegister, "researchRegister"); Catalog.Require(rule.approvalRecord, "approvalRecord");
            if (rule.printingId != catalog.PrintingId) throw new ArgumentException("Rule printing mismatch");
            if (rule.rngAlgorithm != "xorshift32-v1") throw new ArgumentException("Unsupported rngAlgorithm");
            if (rule.approximations == null) throw new ArgumentException("Missing approximations");
            foreach (var a in rule.approximations) Catalog.Require(a, "approximation");
            if (rule.pools == null || rule.pools.Length == 0) throw new ArgumentException("Empty pools");
            var pools = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var p in rule.pools)
            {
                if (p == null) throw new ArgumentException("Null pool");
                Catalog.Require(p.id, "pool id");
                if (!pools.Add(p.id)) throw new ArgumentException("Duplicate pool: " + p.id);
                if (p.entries == null || p.entries.Length == 0) throw new ArgumentException("Empty entries");
                var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                long total = 0;
                foreach (var e in p.entries)
                {
                    if (e == null) throw new ArgumentException("Null weighted entry");
                    Catalog.Require(e.cardId, "cardId");
                    if (!ids.Add(e.cardId)) throw new ArgumentException("Duplicate pool card ID: " + e.cardId);
                    if (e.weight <= 0) throw new ArgumentException("Nonpositive weight");
                    total += e.weight;
                    if (total > int.MaxValue) throw new ArgumentException("Pool total exceeds Int32");
                    CardPrinting c;
                    try { c = catalog.Find(e.cardId); }
                    catch (System.Collections.Generic.KeyNotFoundException) { throw new ArgumentException("Missing card ID: " + e.cardId); }
                    if (!c.BoosterEligible || c.PrintingId != rule.printingId || (c.PrintingId == "base-en-unlimited1999" && c.CollectorNumber == "8")) throw new ArgumentException("Ineligible/wrong-printing/starter-only pool card");
                }
            }
            if (rule.slots == null || rule.slots.Length == 0) throw new ArgumentException("Empty slots");
            foreach (var s in rule.slots)
            {
                if (s == null) throw new ArgumentException("Null slot");
                if (s.poolId == null || !pools.Contains(s.poolId)) throw new ArgumentException("Missing primary pool");
                if (string.IsNullOrEmpty(s.alternativePoolId))
                {
                    if (s.alternativeNumerator != 0 || s.alternativeDenominator != 1) throw new ArgumentException("No alternative requires 0/1");
                }
                else if (!pools.Contains(s.alternativePoolId) || s.alternativeDenominator <= 0 || s.alternativeNumerator < 0 || s.alternativeNumerator > s.alternativeDenominator)
                    throw new ArgumentException("Invalid alternative fraction/reference");
            }
            foreach (var p in rule.pools)
            {
                if (!p.uniqueWithinPack) continue;
                int demand = 0;
                foreach (var s in rule.slots)
                {
                    bool hasAlt = !string.IsNullOrEmpty(s.alternativePoolId);
                    bool primaryReachable = !hasAlt || s.alternativeNumerator < s.alternativeDenominator;
                    bool altReachable = hasAlt && s.alternativeNumerator > 0;
                    if ((primaryReachable && s.poolId == p.id) || (altReachable && s.alternativePoolId == p.id)) demand++;
                }
                if (demand > p.entries.Length) throw new ArgumentException("Over-demanded unique pool: " + p.id);
            }
            catalogVersion = catalog.CatalogVersion;
            var snapshot = new RuleData {
                schemaVersion = rule.schemaVersion, modelId = rule.modelId, modelVersion = rule.modelVersion,
                status = rule.status, printingId = rule.printingId, rngAlgorithm = rule.rngAlgorithm,
                researchRegister = rule.researchRegister, approvalRecord = rule.approvalRecord,
                approximations = (string[])rule.approximations.Clone(),
                pools = new PoolData[rule.pools.Length], slots = new SlotData[rule.slots.Length]
            };
            for (int i = 0; i < snapshot.pools.Length; i++)
            {
                var p = rule.pools[i];
                var entries = new WeightedEntryData[p.entries.Length];
                for (int j = 0; j < entries.Length; j++) entries[j] = new WeightedEntryData { cardId = p.entries[j].cardId, weight = p.entries[j].weight };
                snapshot.pools[i] = new PoolData { id = p.id, uniqueWithinPack = p.uniqueWithinPack, entries = entries };
            }
            for (int i = 0; i < snapshot.slots.Length; i++)
            {
                var s = rule.slots[i];
                snapshot.slots[i] = new SlotData { poolId = s.poolId, alternativePoolId = s.alternativePoolId, alternativeNumerator = s.alternativeNumerator, alternativeDenominator = s.alternativeDenominator };
            }
            this.rule = snapshot;
        }
        private static int Next(IRandomSource rng, int bound)
        {
            int value = rng.NextInt(bound);
            if (value < 0 || value >= bound) throw new InvalidOperationException("IRandomSource returned out-of-range value");
            return value;
        }
        public PackResult Generate(IRandomSource rng)
        {
            if (rng == null) throw new ArgumentNullException("rng");
            var ids = new string[rule.slots.Length];
            var used = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>(StringComparer.Ordinal);
            foreach (var p in rule.pools) used.Add(p.id, new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal));
            for (int i = 0; i < ids.Length; i++)
            {
                PoolData pool = null;
                var slot = rule.slots[i];
                string selected = slot.poolId;
                if (!string.IsNullOrEmpty(slot.alternativePoolId) && Next(rng, slot.alternativeDenominator) < slot.alternativeNumerator) selected = slot.alternativePoolId;
                foreach (var p in rule.pools) if (p.id == selected) pool = p;
                int total = 0;
                foreach (var e in pool.entries) if (!used[pool.id].Contains(e.cardId)) total += e.weight;
                int ticket = Next(rng, total);
                foreach (var e in pool.entries)
                {
                    if (used[pool.id].Contains(e.cardId)) continue;
                    if (ticket < e.weight) { ids[i] = e.cardId; if (pool.uniqueWithinPack) used[pool.id].Add(e.cardId); break; }
                    ticket -= e.weight;
                }
            }
            return new PackResult(ids, rule, catalogVersion);
        }
    }
}
