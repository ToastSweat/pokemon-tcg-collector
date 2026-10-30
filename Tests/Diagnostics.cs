using System;
using System.Linq;
using System.Collections.Generic;
using VintageCollector.Domain;
public sealed class DiagnosticCard { public int position; public string id, displayName, collectorNumber, rarity, kind, finish; }
public sealed class DiagnosticPack
{
    public string modelId, modelVersion, catalogVersion, printingId, status, rngAlgorithm, warning;
    public uint seed;
    public int cardCount;
    public DiagnosticCard[] cards;
}
public sealed class CardCount { public string id, poolId; public int count; }
public sealed class EnergyPair { public string firstId, secondId; public int count; }
public sealed class BatchReport
{
    public int sampleSize, cardCount, holoCount, energyRepeatCount, invariantViolations;
    public uint seed;
    public string modelId, modelVersion, catalogVersion, printingId, status, rngAlgorithm, warning;
    public CardCount[] counts, firstEnergyCounts, secondEnergyCounts;
    public EnergyPair[] energyPairs;
    public int countCount, energyPairCount;
}
public static class Diagnostics
{
    public static BatchReport Batch(Catalog catalog, RuleData rule, uint seed, int sampleSize)
    {
        if (sampleSize <= 0 || sampleSize > int.MaxValue / 11) throw new ArgumentOutOfRangeException("sampleSize");
        var g = new PackGenerator(catalog, rule); var rng = new SeededRandom(seed);
        var counts = new Dictionary<string, CardCount>(StringComparer.Ordinal);
        foreach (var pool in rule.pools) foreach (var entry in pool.entries) counts.Add(entry.cardId, new CardCount { id = entry.cardId, poolId = pool.id });
        var energy = rule.pools.Single(p => p.id == "energy").entries;
        var first = energy.Select(e => new CardCount { id = e.cardId, poolId = "energy" }).ToArray();
        var second = energy.Select(e => new CardCount { id = e.cardId, poolId = "energy" }).ToArray();
        var pairs = (from a in energy from b in energy select new EnergyPair { firstId = a.cardId, secondId = b.cardId }).ToArray();
        var byId = catalog.Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
        int holos = 0, repeats = 0, violations = 0;
        PackResult last = null;
        for (int n = 0; n < sampleSize; n++)
        {
            last = g.Generate(rng); var ids = last.OrderedCardIds;
            bool valid = ids.Count == 11;
            if (ids.Count != 11) { violations++; continue; }
            for (int i = 0; i < ids.Count; i++)
            {
                var c = byId[ids[i]]; counts[c.Id].count++;
                valid &= c.BoosterEligible && c.PrintingId == rule.printingId && c.CollectorNumber != "8";
                valid &= i < 5 ? c.Rarity == "common" && c.Kind != "basic-energy" : i < 7 ? c.Kind == "basic-energy" : i == 7 ? c.Rarity == "rare" || c.Rarity == "holo" : c.Rarity == "uncommon";
            }
            valid &= ids.Take(5).Distinct().Count() == 5 && ids.Skip(8).Distinct().Count() == 3;
            if (!valid) violations++;
            if (byId[ids[7]].Rarity == "holo") holos++;
            if (ids[5] == ids[6]) repeats++;
            first.Single(c => c.id == ids[5]).count++;
            second.Single(c => c.id == ids[6]).count++;
            pairs.Single(p => p.firstId == ids[5] && p.secondId == ids[6]).count++;
        }
        return new BatchReport { sampleSize = sampleSize, cardCount = counts.Values.Sum(c => c.count),
            holoCount = holos, energyRepeatCount = repeats, invariantViolations = violations, seed = seed,
            modelId = last.ModelId, modelVersion = last.ModelVersion, catalogVersion = last.CatalogVersion,
            printingId = last.PrintingId, status = last.Status, rngAlgorithm = last.RngAlgorithm, warning = Warning,
            counts = counts.Values.OrderBy(c => c.id).ToArray(), countCount = counts.Count,
            firstEnergyCounts = first, secondEnergyCounts = second, energyPairs = pairs, energyPairCount = pairs.Length };
    }
    public const string Warning = "approved-provisional single-pack simulation, not exact factory collation; validates implementation, not history. Machamp completion exception unresolved; no completion calculation. No box collation, persistence, awards or playable loop.";
    public static DiagnosticPack Pack(Catalog catalog, RuleData rule, uint seed)
    {
        var pack = new PackGenerator(catalog, rule).Generate(new SeededRandom(seed));
        var cards = new DiagnosticCard[pack.OrderedCardIds.Count];
        for (int i = 0; i < cards.Length; i++)
        {
            var c = catalog.Find(pack.OrderedCardIds[i]);
            cards[i] = new DiagnosticCard { position = i+1, id = c.Id, displayName = c.DisplayName, collectorNumber = c.CollectorNumber, rarity = c.Rarity, kind = c.Kind, finish = c.Finish };
        }
        return new DiagnosticPack { modelId = pack.ModelId, modelVersion = pack.ModelVersion, catalogVersion = pack.CatalogVersion, printingId = pack.PrintingId, status = pack.Status, rngAlgorithm = pack.RngAlgorithm, seed = seed, warning = Warning, cardCount = cards.Length, cards = cards };
    }
}
