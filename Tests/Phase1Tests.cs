using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using VintageCollector.Domain;

public sealed class Check { public string name; public bool passed; public string error; }
public sealed class TestReport { public int testCount; public bool passed; public Check[] checks; }
public static class Phase1Tests
{
    static void Assert(bool b, string message) { if (!b) throw new Exception(message); }
    static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    static T Read<T>(string path)
    {
        using (var f = File.OpenRead(path)) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(f);
    }
    static CatalogData CatalogInput() { return Read<CatalogData>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"); }
    static RuleData RuleInput() { return Read<RuleData>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json"); }
    static void Json(object value)
    {
        new DataContractJsonSerializer(value.GetType()).WriteObject(Console.OpenStandardOutput(), value);
        Console.WriteLine();
    }
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "diagnostic")
        {
            Json(Diagnostics.Pack(Catalog.FromData(CatalogInput()), RuleInput(), uint.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture)));
            return 0;
        }
        if (args.Length == 3 && args[0] == "batch")
        {
            Json(Diagnostics.Batch(Catalog.FromData(CatalogInput()), RuleInput(), uint.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture), int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture)));
            return 0;
        }
        if (args.Length == 0 || args[0] != "tests") return 2;
        var checks = new List<Check>();
        foreach (var m in typeof(Phase1Tests).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name.StartsWith("Test_")).OrderBy(m => m.Name))
        {
            if (args.Length > 1 && !m.Name.Contains(args[1])) continue;
            var c = new Check { name = m.Name, passed = true, error = "" };
            try { m.Invoke(null, null); } catch (Exception e) { c.passed = false; c.error = (e.InnerException ?? e).ToString(); }
            checks.Add(c);
        }
        bool passed = checks.Count > 0 && checks.All(c => c.passed);
        Json(new TestReport { testCount = checks.Count, passed = passed, checks = checks.ToArray() });
        return passed ? 0 : 1;
    }
    static void Test_01_CatalogLoadsRealJson()
    {
        var input = CatalogInput();
        var catalog = Catalog.FromData(input);
        Assert(catalog.Cards.Count == 101, "101 booster printings");
        Assert(catalog.Find(input.cards[0].id).DisplayName == "Alakazam", "lookup stable ID");
    }
    static void Test_02_CatalogRejectsMalformedIdentity()
    {
        Throws<ArgumentNullException>(() => Catalog.FromData(null));
        Action<Action<CatalogData>> invalid = mutate => { var d = CatalogInput(); mutate(d); Throws<ArgumentException>(() => Catalog.FromData(d)); };
        invalid(d => d.schemaVersion = 2);
        invalid(d => d.catalogVersion = " ");
        invalid(d => d.printingId = null);
        invalid(d => d.completionTargetStatus = "");
        invalid(d => d.sourceResearch = "");
        invalid(d => d.cards = null);
        invalid(d => d.cards = new CardData[0]);
        invalid(d => d.cards[0] = null);
        invalid(d => d.cards[1].id = d.cards[0].id);
        invalid(d => d.cards[0].id = "");
        invalid(d => d.cards[0].printingId = "other");
        invalid(d => d.cards[0].collectorNumber = "01");
        invalid(d => d.cards[0].collectorNumber = "0");
        invalid(d => d.cards[0].collectorNumber = "-1");
        invalid(d => d.cards[0].collectorNumber = "2147483648");
        foreach (var f in typeof(CardData).GetFields().Where(f => f.FieldType == typeof(string)))
            invalid(d => f.SetValue(d.cards[0], " "));
        invalid(d => d.cards[0].rarity = "mythic");
        invalid(d => d.cards[0].kind = "energy");
        invalid(d => d.cards[0].finish = "foil");
        invalid(d => d.cards[0].finish = "nonholo");
    }

    static void Test_03_CatalogSnapshotsEveryField()
    {
        var input = CatalogInput(); var catalog = Catalog.FromData(input); var card = catalog.Cards[0];
        foreach (var f in typeof(CardData).GetFields())
        {
            string name = char.ToUpperInvariant(f.Name[0]) + f.Name.Substring(1);
            var prop = typeof(CardPrinting).GetProperty(name);
            Assert(prop != null, "Missing snapshot property " + name);
            object before = f.GetValue(input.cards[0]);
            f.SetValue(input.cards[0], f.FieldType == typeof(bool) ? (object)false : "mutated");
            Assert(object.Equals(prop.GetValue(card, null), before), "Aliased " + name);
            Assert(prop.GetSetMethod() == null, "Public setter " + name);
        }
        foreach (string field in new[] { "catalogVersion", "printingId", "completionTargetStatus", "sourceResearch" })
        {
            var f = typeof(CatalogData).GetField(field); object before = f.GetValue(input);
            f.SetValue(input, "mutated");
            var prop = typeof(Catalog).GetProperty(char.ToUpperInvariant(field[0]) + field.Substring(1));
            Assert(prop != null && object.Equals(prop.GetValue(catalog, null), before), "Catalog metadata snapshot " + field);
        }
        input.cards[0] = null; input.cards = null;
        Assert(catalog.Cards.Count == 101 && catalog.Cards[0] == card, "array snapshot");
        Throws<NotSupportedException>(() => ((IList<CardPrinting>)catalog.Cards)[0] = null);
        Throws<ArgumentNullException>(() => catalog.Find(null));
        Throws<KeyNotFoundException>(() => catalog.Find("absent"));
    }

    static void Test_04_PortableNonzeroDomainUnbiasedRng()
    {
        Throws<ArgumentOutOfRangeException>(() => new SeededRandom(0));
        var rng = new SeededRandom(1);
        int[] bounds = { 1, 2, 3, 121, 110, 2147483647, 1073741825, 7 };
        int[] expected = { 0, 0, 2, 28, 82, 745495503, 632435481, 0 };
        for (int i = 0; i < bounds.Length; i++) Assert(rng.NextInt(bounds[i]) == expected[i], "Pinned bounded vector " + i);
        rng = new SeededRandom(1);
        Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
        Throws<ArgumentOutOfRangeException>(() => rng.NextInt(-1));
        Assert(rng.NextInt(2147483647) == 270368, "Invalid bounds must not consume state");
        var a = new SeededRandom(0xffffffff); var b = new SeededRandom(0xffffffff);
        for (int i = 0; i < 10000; i++) { int v = a.NextInt(121); Assert(v >= 0 && v < 121 && v == b.NextInt(121), "range/reproducibility"); }
    }

    static void Test_05_RngRejectionAndRawVector()
    {
        var rng = new SeededRandom(1);
        rng = new SeededRandom(12288);
        int[] expected = { 11334480, 6483128, 398034308, 630358227, 438359828 };
        foreach (int v in expected) Assert(rng.NextInt(1073741825) == v, "Rejection vector");
        rng = new SeededRandom(1);
        uint[] words = { 270369u, 67634689u, 2647435461u, 307599695u, 2398689233u, 745495504u, 632435482u, 435756210u };
        var method = typeof(SeededRandom).GetMethod("NextUInt", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (uint word in words) Assert((uint)method.Invoke(rng, null) == word, "Raw vector");
    }

    sealed class ScriptRandom : IRandomSource
    {
        readonly Queue<int> values; internal readonly List<int> Bounds = new List<int>();
        internal ScriptRandom(params int[] values) { this.values = new Queue<int>(values); }
        public int NextInt(int max) { Bounds.Add(max); if (values.Count == 0) throw new Exception("Unexpected RNG call"); return values.Dequeue(); }
    }
    static RuleData SmallRule()
    {
        var r = RuleInput();
        r.pools = new[] { new PoolData { id = "p", entries = new[] {
            new WeightedEntryData { cardId = CatalogInput().cards[0].id, weight = 2 },
            new WeightedEntryData { cardId = CatalogInput().cards[1].id, weight = 3 } } } };
        r.slots = new[] { new SlotData { poolId = "p", alternativePoolId = "", alternativeDenominator = 1 } };
        return r;
    }
    static void Test_06_WeightedHalfOpenBoundaries()
    {
        var c = Catalog.FromData(CatalogInput()); var r = SmallRule(); var g = new PackGenerator(c, r);
        for (int n = 0; n < 5; n++)
        {
            var rng = new ScriptRandom(n); var result = g.Generate(rng);
            Assert(result.OrderedCardIds.Count == 1 && result.OrderedCardIds[0] == r.pools[0].entries[n < 2 ? 0 : 1].cardId, "half-open interval " + n);
            Assert(rng.Bounds.SequenceEqual(new[] { 5 }), "exact weight total bound");
        }
    }

    static void Test_07_UniquePoolRemovesWholeIdentityAndResetsPerCall()
    {
        var r = SmallRule(); r.pools[0].uniqueWithinPack = true;
        r.slots = new[] { r.slots[0], r.slots[0] };
        var g = new PackGenerator(Catalog.FromData(CatalogInput()), r);
        foreach (int first in new[] { 0, 1, 2, 4 })
        {
            var rng = new ScriptRandom(first, 0); var result = g.Generate(rng);
            Assert(result.OrderedCardIds[0] != result.OrderedCardIds[1], "no repeated identity");
            Assert(rng.Bounds.SequenceEqual(new[] { 5, first < 2 ? 3 : 2 }), "remove entire weight");
        }
        r.pools[0].uniqueWithinPack = false;
        var replacement = new PackGenerator(Catalog.FromData(CatalogInput()), r).Generate(new ScriptRandom(0, 0));
        Assert(replacement.OrderedCardIds[0] == replacement.OrderedCardIds[1], "with replacement permits repeats");
    }

    static void Test_08_AlternativeThresholdAndSlotOrder()
    {
        var r = SmallRule();
        r.pools = new[] { r.pools[0], new PoolData { id = "alt", entries = new[] { new WeightedEntryData { cardId = CatalogInput().cards[2].id, weight = 7 } } } };
        r.slots[0].alternativePoolId = "alt"; r.slots[0].alternativeNumerator = 1; r.slots[0].alternativeDenominator = 3;
        var g = new PackGenerator(Catalog.FromData(CatalogInput()), r);
        foreach (int choice in new[] { 0, 1, 2 })
        {
            var rng = new ScriptRandom(choice, 0); var result = g.Generate(rng);
            Assert(result.OrderedCardIds[0] == r.pools[choice == 0 ? 1 : 0].entries[0].cardId, "alt threshold " + choice);
            Assert(rng.Bounds.SequenceEqual(new[] { 3, choice == 0 ? 7 : 5 }), "choice before card selection");
        }
        var actual = RuleInput(); var catalog = Catalog.FromData(CatalogInput());
        g = new PackGenerator(catalog, actual);
        var pack = g.Generate(new SeededRandom(12345));
        Assert(pack.OrderedCardIds.Count == 11, "11 cards");
        for (int i = 0; i < 11; i++)
        {
            var card = catalog.Find(pack.OrderedCardIds[i]);
            Assert(card.BoosterEligible && card.CollectorNumber != "8", "eligible, never Machamp");
            Assert(i < 5 ? card.Rarity == "common" && card.Kind != "basic-energy" : i < 7 ? card.Kind == "basic-energy" : i == 7 ? card.Rarity == "rare" || card.Rarity == "holo" : card.Rarity == "uncommon", "slot " + i);
        }
        Assert(pack.OrderedCardIds.Take(5).Distinct().Count() == 5 && pack.OrderedCardIds.Skip(8).Distinct().Count() == 3, "C/U identity exclusion");
    }

    static void Test_09_InvalidRulesFailAtConstruction()
    {
        var c = Catalog.FromData(CatalogInput());
        Throws<ArgumentNullException>(() => new PackGenerator(null, SmallRule()));
        Throws<ArgumentNullException>(() => new PackGenerator(c, null));
        Action<Action<RuleData>> invalid = mutate => { var r = SmallRule(); mutate(r); Throws<ArgumentException>(() => new PackGenerator(c, r)); };
        invalid(r => r.schemaVersion = 2);
        foreach (string name in new[] { "modelId", "modelVersion", "status", "printingId", "rngAlgorithm", "researchRegister", "approvalRecord" })
            invalid(r => typeof(RuleData).GetField(name).SetValue(r, " "));
        invalid(r => r.printingId = "other"); invalid(r => r.rngAlgorithm = "unknown");
        invalid(r => r.approximations = null); invalid(r => r.approximations = new[] { "" });
        invalid(r => r.pools = null); invalid(r => r.pools = new PoolData[0]); invalid(r => r.pools[0] = null);
        invalid(r => r.pools = new[] { r.pools[0], r.pools[0] }); invalid(r => r.pools[0].id = "");
        invalid(r => r.pools[0].entries = null); invalid(r => r.pools[0].entries = new WeightedEntryData[0]);
        invalid(r => r.pools[0].entries[0] = null); invalid(r => r.pools[0].entries[0].cardId = null);
        invalid(r => r.pools[0].entries[0].weight = 0); invalid(r => r.pools[0].entries[0].weight = -1);
        invalid(r => r.pools[0].entries[0].weight = int.MaxValue);
        invalid(r => r.pools[0].entries[1].cardId = r.pools[0].entries[0].cardId);
        invalid(r => r.pools[0].entries[0].cardId = "missing");
        invalid(r => r.slots = null); invalid(r => r.slots = new SlotData[0]); invalid(r => r.slots[0] = null);
        invalid(r => r.slots[0].poolId = "missing"); invalid(r => r.slots[0].poolId = null);
        invalid(r => r.slots[0].alternativePoolId = "missing");
        invalid(r => r.slots[0].alternativeNumerator = 1); invalid(r => r.slots[0].alternativeDenominator = 2);
        invalid(r => { r.slots[0].alternativePoolId = "p"; r.slots[0].alternativeDenominator = 0; });
        invalid(r => { r.slots[0].alternativePoolId = "p"; r.slots[0].alternativeNumerator = -1; });
        invalid(r => { r.slots[0].alternativePoolId = "p"; r.slots[0].alternativeNumerator = 2; });
        var d = CatalogInput(); d.cards[0].boosterEligible = false;
        Throws<ArgumentException>(() => new PackGenerator(Catalog.FromData(d), SmallRule()));
    }

    static void Test_10_ReachableUniqueSlotDemandValidatedExactly()
    {
        var c = Catalog.FromData(CatalogInput()); var r = SmallRule(); r.pools[0].uniqueWithinPack = true;
        r.slots = new[] { r.slots[0], r.slots[0], r.slots[0] };
        Throws<ArgumentException>(() => new PackGenerator(c, r));
        r = SmallRule(); r.pools[0].uniqueWithinPack = true;
        r.pools = new[] { r.pools[0], new PoolData { id = "q", entries = new[] { new WeightedEntryData { cardId = c.Cards[2].Id, weight = 1 } } } };
        var s = new SlotData { poolId = "q", alternativePoolId = "p", alternativeNumerator = 1, alternativeDenominator = 3 };
        r.slots = new[] { s, s, s };
        Throws<ArgumentException>(() => new PackGenerator(c, r));
        s.alternativeNumerator = 0; new PackGenerator(c, r); // unreachable alternative cannot consume p
        s.poolId = "p"; s.alternativePoolId = "q"; s.alternativeNumerator = 3;
        new PackGenerator(c, r); // unreachable primary cannot consume p
        r.slots = new[] { new SlotData { poolId = "p", alternativePoolId = "p", alternativeNumerator = 1, alternativeDenominator = 2 }, new SlotData { poolId = "p", alternativePoolId = "", alternativeDenominator = 1 } };
        new PackGenerator(c, r).Generate(new ScriptRandom(0, 0, 0)); // same pool alternative counts once per slot
    }

    static void Test_11_HostileRngRejectedAndFailureDoesNotDepleteGenerator()
    {
        var c = Catalog.FromData(CatalogInput()); var g = new PackGenerator(c, SmallRule());
        Throws<ArgumentNullException>(() => g.Generate(null));
        foreach (int bad in new[] { -1, 5, int.MinValue, int.MaxValue })
            Throws<InvalidOperationException>(() => g.Generate(new ScriptRandom(bad)));
        var r = RuleInput(); g = new PackGenerator(c, r);
        Throws<InvalidOperationException>(() => g.Generate(new ScriptRandom(0, 0, 0, 0, 0, 0, 0, -1)));
        Throws<InvalidOperationException>(() => g.Generate(new ScriptRandom(0, 0, 0, 0, 0, 0, 0, 3)));
        var fresh = new PackGenerator(c, RuleInput()).Generate(new SeededRandom(99));
        Assert(g.Generate(new SeededRandom(99)).OrderedCardIds.SequenceEqual(fresh.OrderedCardIds), "failure cannot mutate shared state");
    }

    static void Test_12_GeneratorAndResultsFreezeVersionsAndNestedRuleData()
    {
        var c = Catalog.FromData(CatalogInput()); var r = RuleInput(); var g = new PackGenerator(c, r);
        var before = g.Generate(new SeededRandom(123)); var expectedIds = before.OrderedCardIds.ToArray();
        Assert(before.ModelId == r.modelId && before.ModelVersion == r.modelVersion && before.PrintingId == r.printingId && before.CatalogVersion == c.CatalogVersion && before.Status == r.status && before.RngAlgorithm == r.rngAlgorithm, "frozen provenance metadata");
        foreach (var p in r.pools) { foreach (var e in p.entries) { e.weight = 0; e.cardId = "mutated"; } p.id = "mutated"; p.uniqueWithinPack = !p.uniqueWithinPack; p.entries[0] = null; p.entries = null; }
        foreach (var s in r.slots) { s.poolId = "mutated"; s.alternativePoolId = "mutated"; s.alternativeNumerator = -1; s.alternativeDenominator = 0; }
        r.pools = null; r.slots = null; r.approximations[0] = "mutated"; r.approximations = null;
        foreach (var f in typeof(RuleData).GetFields().Where(f => f.FieldType == typeof(string))) f.SetValue(r, "mutated");
        r.schemaVersion = -1;
        var after = g.Generate(new SeededRandom(123));
        Assert(after.OrderedCardIds.SequenceEqual(expectedIds) && after.ModelVersion == before.ModelVersion, "generator snapshot survives hostile DTO mutation");
        var next = RuleInput(); next.modelVersion = "future";
        new PackGenerator(c, next).Generate(new SeededRandom(123));
        Assert(before.ModelVersion == "0.1.0" && before.OrderedCardIds.SequenceEqual(expectedIds), "future model/result does not rewrite past");
        Throws<NotSupportedException>(() => ((IList<string>)before.OrderedCardIds)[0] = "mutated");
        Throws<NotSupportedException>(() => ((IList<string>)before.OrderedCardIds).Add("mutated"));
        Assert(!object.ReferenceEquals(before.OrderedCardIds, after.OrderedCardIds), "independently frozen result collection");
        foreach (var p in typeof(PackResult).GetProperties()) Assert(p.GetSetMethod() == null, "result public setter");
    }

    static void Test_13_PrintingIdsMustEncodeCanonicalPrintingFinishVariant()
    {
        Action<string> invalid = id => { var d = CatalogInput(); d.cards[0].id = id; Throws<ArgumentException>(() => Catalog.FromData(d)); };
        invalid("Alakazam"); invalid("pkmn-base-en-unlimited1999-1-holo-standard");
        invalid("pkmn-base-en-unlimited1999-001-nonholo-standard"); invalid("pkmn-other-001-holo-standard");
        invalid("pkmn-base-en-unlimited1999-001-holo-other");
        var data = CatalogInput();
        data.cards[0].collectorNumber = "8"; data.cards[0].id = "pkmn-base-en-unlimited1999-008-holo-standard";
        var r = SmallRule(); r.pools[0].entries[0].cardId = data.cards[0].id;
        Throws<ArgumentException>(() => new PackGenerator(Catalog.FromData(data), r));
    }

    static void Test_14_DiagnosticContainsActualOrderedIdsNamesAndWarnings()
    {
        var c = Catalog.FromData(CatalogInput()); var r = RuleInput();
        var d = Diagnostics.Pack(c, r, 1); var p = new PackGenerator(c, r).Generate(new SeededRandom(1));
        Assert(d.cardCount == d.cards.Length && d.cardCount == 11, "diagnostic declared count");
        Assert(d.modelVersion == p.ModelVersion && d.modelId == p.ModelId && d.seed == 1 && d.rngAlgorithm == "xorshift32-v1", "diagnostic metadata");
        Assert(d.warning.Contains("provisional") && d.warning.Contains("not exact factory collation") && d.warning.Contains("unresolved"), "explicit limitations");
        for (int i = 0; i < d.cards.Length; i++) Assert(d.cards[i].position == i+1 && d.cards[i].id == p.OrderedCardIds[i] && d.cards[i].displayName == c.Find(p.OrderedCardIds[i]).DisplayName, "ordered diagnostic card " + i);
    }

    static void Test_15_BatchCountsReconcileWithActualPacks()
    {
        var c = Catalog.FromData(CatalogInput()); var r = RuleInput(); var b = Diagnostics.Batch(c, r, 1, 50);
        Assert(b.sampleSize == 50 && b.cardCount == 550 && b.invariantViolations == 0, "batch sample/card/invariant counts");
        Assert(b.countCount == b.counts.Length && b.counts.Length == 101 && b.energyPairCount == b.energyPairs.Length && b.energyPairs.Length == 36, "declared arrays");
        Assert(b.counts.Sum(x => x.count) == 550 && b.energyPairs.Sum(x => x.count) == 50 && b.firstEnergyCounts.Sum(x => x.count) == 50 && b.secondEnergyCounts.Sum(x => x.count) == 50, "all counters reconcile");
        var g = new PackGenerator(c, r); var rng = new SeededRandom(1); var ids = new List<string>(); int holos = 0, repeats = 0;
        for (int i = 0; i < 50; i++) { var p = g.Generate(rng); ids.AddRange(p.OrderedCardIds); if (c.Find(p.OrderedCardIds[7]).Rarity == "holo") holos++; if (p.OrderedCardIds[5] == p.OrderedCardIds[6]) repeats++; }
        foreach (var count in b.counts) Assert(count.count == ids.Count(id => id == count.id), "independent counter " + count.id);
        Assert(b.holoCount == holos && b.energyRepeatCount == repeats, "actual holo/repeat counts");
        Throws<ArgumentOutOfRangeException>(() => Diagnostics.Batch(c, r, 1, 0));
        Throws<ArgumentOutOfRangeException>(() => Diagnostics.Batch(c, r, 1, -1));
    }

    static void Test_16_CliRequiresExplicitGenerationMode()
    {
        var exe = Assembly.GetExecutingAssembly().Location;
        var start = new System.Diagnostics.ProcessStartInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, "\"" + exe + "\" diagnostic 1") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        using (var process = System.Diagnostics.Process.Start(start))
        {
            string output = process.StandardOutput.ReadToEnd(); string error = process.StandardError.ReadToEnd(); process.WaitForExit();
            Assert(process.ExitCode == 0 && error == "" && output.Contains("displayName") && output.Contains("provisional") && output.Contains("cardCount"), "opt-in diagnostic CLI must emit real JSON, exit=" + process.ExitCode);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(output)))
            {
                var d = (DiagnosticPack)new DataContractJsonSerializer(typeof(DiagnosticPack)).ReadObject(stream);
                Assert(d.cards.Length == d.cardCount && d.seed == 1, "CLI parseable diagnostic contract");
            }
        }
    }

    static void Test_17_BatchCliReturnsReconciledJson()
    {
        var exe = Assembly.GetExecutingAssembly().Location;
        var start = new System.Diagnostics.ProcessStartInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, "\"" + exe + "\" batch 1 10") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        using (var process = System.Diagnostics.Process.Start(start))
        {
            string output = process.StandardOutput.ReadToEnd(); string error = process.StandardError.ReadToEnd(); process.WaitForExit();
            Assert(process.ExitCode == 0 && error == "", "batch CLI exit=" + process.ExitCode);
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(output)))
            {
                var d = (BatchReport)new DataContractJsonSerializer(typeof(BatchReport)).ReadObject(stream);
                Assert(d.sampleSize == 10 && d.cardCount == 110 && d.countCount == d.counts.Length && d.energyPairCount == d.energyPairs.Length, "CLI actual counters");
            }
        }
    }

    static void Test_18_AllFrozenPoolWeightBoundaries()
    {
        var c = Catalog.FromData(CatalogInput()); var r = RuleInput();
        foreach (var pool in r.pools)
        {
            r.slots = new[] { new SlotData { poolId = pool.id, alternativePoolId = "", alternativeDenominator = 1 } };
            var g = new PackGenerator(c, r); int ticket = 0;
            foreach (var e in pool.entries)
                for (int j = 0; j < e.weight; j++, ticket++)
                {
                    var rng = new ScriptRandom(ticket);
                    Assert(g.Generate(rng).OrderedCardIds[0] == e.cardId, "real pool boundary " + pool.id + " ticket " + ticket);
                    Assert(rng.Bounds[0] == pool.entries.Sum(x => x.weight), "real pool total");
                }
        }
        var max = SmallRule(); max.pools[0].entries[0].weight = int.MaxValue - 3;
        var big = new PackGenerator(c, max);
        Assert(big.Generate(new ScriptRandom(int.MaxValue-4)).OrderedCardIds[0] == max.pools[0].entries[0].cardId, "Int32 maximum valid total last first bucket");
        Assert(big.Generate(new ScriptRandom(int.MaxValue-3)).OrderedCardIds[0] == max.pools[0].entries[1].cardId, "Int32 maximum valid total next bucket");
    }
    static void Test_19_RealCatalogProvenanceAndSourceCorrections()
    {
        var c = Catalog.FromData(CatalogInput());
        Assert(c.CompletionTargetStatus == "unresolved-machamp-exception", "completion unresolved, not a denominator");
        Assert(c.Cards.Select(x => x.CollectorNumber).Distinct().Count() == 101 && !c.Cards.Any(x => x.CollectorNumber == "8"), "101 booster identities, source #8 absent intentionally");
        Assert(c.Cards.All(x => x.BoosterEligible && x.ArtReference == "placeholder:card-face" && x.Language == "en" && x.Edition == "unlimited"), "scope and placeholder only");
        Assert(c.Cards.Single(x => x.CollectorNumber == "58").Variant == "yellow-cheeks", "explicit Pikachu variant");
        var corrected = c.Cards.Single(x => x.CollectorNumber == "73");
        Assert(corrected.DisplayName == "Imposter Professor Oak" && corrected.SourceName != corrected.DisplayName, "corrected display while preserving source");
        foreach (var type in new[] { typeof(CatalogData), typeof(CardData), typeof(RuleData), typeof(PoolData), typeof(WeightedEntryData), typeof(SlotData) })
            Assert(type.IsSerializable && type.GetProperties().Length == 0, "serializable field-only frozen DTO " + type.Name);
    }
    static void Test_20_GeneratorSupportsIndependentConcurrentAndReentrantCalls()
    {
        var g = new PackGenerator(Catalog.FromData(CatalogInput()), RuleInput());
        var expected = g.Generate(new SeededRandom(77)).OrderedCardIds.ToArray();
        var jobs = new System.Threading.Tasks.Task[8];
        for (int i = 0; i < jobs.Length; i++) jobs[i] = System.Threading.Tasks.Task.Run(() => Assert(g.Generate(new SeededRandom(77)).OrderedCardIds.SequenceEqual(expected), "concurrent local generation state"));
        System.Threading.Tasks.Task.WaitAll(jobs);
        var rng = new ReentrantRandom(g);
        Assert(g.Generate(rng).OrderedCardIds.Count == 11 && rng.Calls == 12, "reentrant call leaves outer pack intact");
    }
    sealed class ReentrantRandom : IRandomSource
    {
        readonly PackGenerator generator; internal int Calls;
        internal ReentrantRandom(PackGenerator generator) { this.generator = generator; }
        public int NextInt(int max)
        {
            if (Calls++ == 0) Assert(generator.Generate(new SeededRandom(10)).OrderedCardIds.Count == 11, "inner generation");
            return 0;
        }
    }

    static void Test_21_DomainCompilesAsSeparatePureAssembly()
    {
        var assembly = typeof(Catalog).Assembly;
        Assert(assembly.GetName().Name == "VintageCollector.Domain", "domain must compile separately from diagnostic/tests");
        Assert(!assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Unity") || a.Name == "System.Runtime.Serialization"), "no Unity/serializer dependency in pure domain");
    }

    // NEXT_TEST
}
