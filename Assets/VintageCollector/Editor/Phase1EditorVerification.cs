using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VintageCollector.EditorTools
{
    // Explicit executeMethod only. Does not enter Play mode or attach startup automation.
    public static class Phase1EditorVerification
    {
        [Serializable] private sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public Check[] checks; }
        [Serializable] private sealed class PackView
        {
            public CardView[] orderedCards = null;
            public uint seed = 0;
            public int cardCount = 0, catalogCardCount = 0;
            public string modelId = null, modelVersion = null, catalogVersion = null, printingId = null;
            public string status = null, rngAlgorithm = null, completionTargetStatus = null, warning = null;
            public string[] approximations = null;
        }
        [Serializable] private sealed class CardView
        {
            public string id = null, displayName = null, collectorNumber = null, rarity = null, kind = null, finish = null;
            public int position = 0;
        }

        public static void Run()
        {
            var checks = new List<Check>();
            AddCheck(checks, "unity_json_to_ordered_pack", delegate
            {
                var view = Build(12345u);
                Require(view != null && view.orderedCards != null && view.orderedCards.Length == 11,
                        "Real Unity JSON-to-domain-to-diagnostic path must produce eleven ordered cards.");
            });
            AddCheck(checks, "diagnostic_provenance_and_limitations", delegate
            {
                var view = Build(4u);
                Require(view.seed == 4u && view.cardCount == 11 && view.catalogCardCount == 101, "Diagnostic seed/count metadata missing.");
                Require(view.modelId == "base-en-unlimited1999-sheet-informed" && view.modelVersion == "0.1.0", "Model identity/version missing.");
                Require(view.catalogVersion == "base-en-unlimited1999-0.1.0" && view.printingId == "base-en-unlimited1999", "Catalog/printing provenance missing.");
                Require(view.status == "approved-provisional" && view.rngAlgorithm == "xorshift32-v1", "Provisional/RNG metadata missing.");
                Require(view.completionTargetStatus == "approved-strict-unstamped-101-excluding-machamp", "User-selected strict unstamped101 completion target was lost.");
                Require(view.approximations != null && view.approximations.Length == 6 && !string.IsNullOrWhiteSpace(view.warning), "Limitations must accompany diagnostics.");
            });
            AddCheck(checks, "readable_order_matches_portable_seed_vector", delegate
            {
                var view = Build(4u);
                string[] expected = { "67", "49", "45", "63", "94", "100", "100", "1", "85", "23", "84" };
                for (int i = 0; i < expected.Length; i++)
                {
                    var card = view.orderedCards[i];
                    Require(card.position == i + 1 && card.collectorNumber == expected[i], "Unity/Mono order or readable position mismatch at " + i);
                    Require(!string.IsNullOrWhiteSpace(card.displayName) && !string.IsNullOrWhiteSpace(card.id), "Missing readable card identity.");
                    string pool = i < 7 ? "common" : i == 7 ? "holo" : "uncommon";
                    Require(card.rarity == pool, "Incorrect reported rarity at " + i);
                }
                Require(view.orderedCards[5].id == view.orderedCards[6].id, "Approved Energy repeat demonstration missing.");
                Require(view.orderedCards[7].displayName == "Alakazam" && view.orderedCards[7].finish == "holo", "Holo replacement demonstration missing.");
                Require(JsonUtility.ToJson(view) == JsonUtility.ToJson(Build(4u)), "Same seed must reproduce exact diagnostics.");
            });
            AddCheck(checks, "editor_menu_exports_project_local_diagnostics", delegate
            {
                var method = typeof(Phase1Diagnostics).GetMethod("Run", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                Require(method != null, "Explicit editor diagnostic export entry point is not implemented.");
                var attributes = method.GetCustomAttributes(typeof(MenuItem), false);
                Require(attributes.Length == 1 && ((MenuItem)attributes[0]).menuItem == "Tools/Vintage Collector/Phase 1/Generate Diagnostic Pack", "User-facing editor menu is missing.");
                string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), ".artifacts", "editor");
                string jsonPath = Path.Combine(output, "phase1-diagnostic-pack.json");
                string textPath = Path.Combine(output, "phase1-diagnostic-pack.txt");
                File.Delete(jsonPath);
                File.Delete(textPath);
                method.Invoke(null, null);
                Require(File.Exists(jsonPath) && File.Exists(textPath), "Menu must write both diagnostic artifacts under this project root.");
                var saved = JsonUtility.FromJson<PackView>(File.ReadAllText(jsonPath));
                Require(JsonUtility.ToJson(saved) == JsonUtility.ToJson(Build(4u)), "Saved menu JSON must match the generated pack exactly.");
                string text = File.ReadAllText(textPath);
                Require(text.Contains("Alakazam") && text.Contains("approved-provisional") && text.Contains("11."), "Text export is not readable/complete.");
            });
            bool passed = checks.TrueForAll(check => check.passed);
            var report = new Report { passed = passed, checkCount = checks.Count, checks = checks.ToArray() };
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), ".artifacts", "editor");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "phase1-editor-checks.json"), JsonUtility.ToJson(report, true));
            Debug.Log("PHASE1_EDITOR_CHECKS " + (passed ? "PASS" : "FAIL") + " count=" + checks.Count);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (!passed) throw new InvalidOperationException("Phase 1 editor check failed; see project-local .artifacts/editor report.");
        }

        private static PackView Build(uint seed)
        {
            return JsonUtility.FromJson<PackView>(JsonUtility.ToJson(Phase1Diagnostics.BuildReport(seed)));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AddCheck(List<Check> checks, string name, Action test)
        {
            try { test(); checks.Add(new Check { name = name, passed = true, detail = "PASS" }); }
            catch (Exception error) { checks.Add(new Check { name = name, passed = false, detail = error.ToString() }); }
        }
    }
}
