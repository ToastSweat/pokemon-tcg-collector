using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using VintageCollector.Domain;

namespace VintageCollector.EditorTools
{
    /// <summary>Explicit editor-only JSON/domain integration. No runtime startup or collection awards.</summary>
    public static class Phase1Diagnostics
    {
        [Serializable] public sealed class DiagnosticCard
        {
            public int position;
            public string id, displayName, collectorNumber, rarity, kind, finish;
        }
        [Serializable] public sealed class DiagnosticPack
        {
            public uint seed;
            public int cardCount, catalogCardCount;
            public string modelId, modelVersion, catalogVersion, printingId, status, rngAlgorithm;
            public string completionTargetStatus, warning;
            public string[] approximations;
            public DiagnosticCard[] orderedCards;
        }

        public static DiagnosticPack BuildReport(uint seed)
        {
            string folder = Path.Combine(Application.dataPath, "VintageCollector", "Data");
            var catalogData = JsonUtility.FromJson<CatalogData>(File.ReadAllText(Path.Combine(folder, "base-en-unlimited1999.catalog.json")));
            var rule = JsonUtility.FromJson<RuleData>(File.ReadAllText(Path.Combine(folder, "base-en-unlimited1999.model-a-v0.1.json")));
            Catalog catalog = Catalog.FromData(catalogData);
            var result = new PackGenerator(catalog, rule).Generate(new SeededRandom(seed));
            var cards = new List<DiagnosticCard>();
            foreach (string id in result.OrderedCardIds)
            {
                CardPrinting printing = catalog.Find(id);
                cards.Add(new DiagnosticCard {
                    position = cards.Count + 1, id = id, displayName = printing.DisplayName,
                    collectorNumber = printing.CollectorNumber, rarity = printing.Rarity,
                    kind = printing.Kind, finish = printing.Finish
                });
            }
            return new DiagnosticPack {
                seed = seed, cardCount = cards.Count, catalogCardCount = catalog.Cards.Count,
                modelId = result.ModelId, modelVersion = result.ModelVersion, catalogVersion = result.CatalogVersion,
                printingId = result.PrintingId, status = result.Status, rngAlgorithm = result.RngAlgorithm,
                completionTargetStatus = catalog.CompletionTargetStatus,
                warning = "Provisional single-pack simulation, not exact factory collation. Machamp completion target unresolved. No collection awards or persistence.",
                approximations = (string[])rule.approximations.Clone(), orderedCards = cards.ToArray()
            };
        }

        [MenuItem("Tools/Vintage Collector/Phase 1/Generate Diagnostic Pack")]
        public static void Run()
        {
            DiagnosticPack report = BuildReport(4u);
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), ".artifacts", "editor");
            Directory.CreateDirectory(folder);
            var text = new StringBuilder();
            text.AppendLine("Vintage Card Collector — Phase 1 diagnostic pack");
            text.AppendLine(report.modelId + " / " + report.modelVersion + " / " + report.status);
            text.AppendLine("Seed " + report.seed + " / " + report.rngAlgorithm + " / " + report.catalogVersion);
            text.AppendLine(report.warning);
            text.AppendLine();
            foreach (var card in report.orderedCards)
                text.AppendLine(card.position + ". " + card.displayName + " (#" + card.collectorNumber + "/102, " + card.rarity + ") — " + card.id);
            text.AppendLine();
            foreach (string approximation in report.approximations) text.AppendLine("- " + approximation);
            File.WriteAllText(Path.Combine(folder, "phase1-diagnostic-pack.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(folder, "phase1-diagnostic-pack.txt"), text.ToString());
            Debug.Log(text.ToString() + "\nDiagnostics exported to " + folder);
        }
    }
}
