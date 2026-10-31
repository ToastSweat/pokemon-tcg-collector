using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace VintageCollector.EditorTools
{
    public static class Phase3BuildChecks
    {
        [Serializable] private sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public Check[] checks; }
        private static string Digest(string path)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
        public static void Run()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            var checks = new List<Check>();
            try
            {
                string acceptedScene = Path.Combine(root, "Assets/VintageCollector/Scenes/Bootstrap.unity");
                string acceptedHash = Digest(acceptedScene);
                Type builder = Type.GetType("VintageCollector.EditorTools.Phase3Build, Assembly-CSharp-Editor");
                if (builder == null) throw new InvalidOperationException("Phase3Build candidate scene builder is not implemented.");
                builder.GetMethod("CreateBootstrap").Invoke(null, null);
                string path = "Assets/VintageCollector/Scenes/BootstrapPhase3.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new Exception("Separate candidate scene missing.");
                checks.Add(new Check { name = "separate-phase3-scene-saved", passed = true, detail = path });
                if (Digest(acceptedScene) != acceptedHash) throw new Exception("Accepted Phase 2 scene was overwritten.");
                checks.Add(new Check { name = "accepted-source-scene-preserved", passed = true, detail = "Exact original bytes." });
                if (EditorBuildSettings.scenes.Length != 1 || EditorBuildSettings.scenes[0].path != path || !EditorBuildSettings.scenes[0].enabled)
                    throw new Exception("Wrong candidate startup scene.");
                checks.Add(new Check { name = "candidate-startup-enabled", passed = true, detail = "Build index zero." });
                Type host = Type.GetType("VintageCollector.Runtime.Phase3MediaHost, Assembly-CSharp");
                if (host == null || UnityEngine.Object.FindFirstObjectByType(host) == null) throw new Exception("Real media host missing from candidate.");
                Type game = Type.GetType("VintageCollector.Runtime.OpeningGame, Assembly-CSharp");
                var component = UnityEngine.Object.FindFirstObjectByType(game);
                var serialized = new SerializedObject(component);
                if (serialized.FindProperty("catalogJson").objectReferenceValue == null || serialized.FindProperty("modelJson").objectReferenceValue == null)
                    throw new Exception("Frozen data references missing.");
                checks.Add(new Check { name = "media-host-and-original-data-wired", passed = true, detail = "No synthetic default assets." });
            }
            catch (Exception error) { checks.Add(new Check { name = "candidate-scene-failure", passed = false, detail = (error.InnerException ?? error).ToString() }); }
            string folder = Path.Combine(root, ".artifacts", "phase3-build");
            Directory.CreateDirectory(folder);
            bool passed = checks.Count > 0 && checks.TrueForAll(c => c.passed);
            File.WriteAllText(Path.Combine(folder, "scene-checks.json"), JsonUtility.ToJson(new Report { passed = passed, checkCount = checks.Count, checks = checks.ToArray() }, true));
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (!passed) throw new Exception("Candidate scene check failed.");
        }
    }
}
