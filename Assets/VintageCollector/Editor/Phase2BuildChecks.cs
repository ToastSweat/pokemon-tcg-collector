using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VintageCollector.EditorTools
{
    public static class Phase2BuildChecks
    {
        [Serializable] private sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public Check[] checks; }
        public static void Run()
        {
            var checks = new List<Check>();
            try
            {
                Type builder = Type.GetType("VintageCollector.EditorTools.Phase2Build, Assembly-CSharp-Editor");
                if (builder == null) throw new InvalidOperationException("Phase2Build scene builder is not implemented.");
                builder.GetMethod("CreateBootstrap").Invoke(null, null);
                string scenePath = "Assets/VintageCollector/Scenes/Bootstrap.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) throw new Exception("Bootstrap scene was not saved.");
                checks.Add(new Check { name = "bootstrap-scene-saved", passed = true, detail = scenePath });
                if (EditorBuildSettings.scenes.Length != 1 || !EditorBuildSettings.scenes[0].enabled || EditorBuildSettings.scenes[0].path != scenePath)
                    throw new Exception("Bootstrap is not the sole enabled startup scene.");
                checks.Add(new Check { name = "startup-scene-enabled", passed = true, detail = "Build index zero." });
                if (Camera.main == null || UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length != 1)
                    throw new Exception("Camera/AudioListener setup incomplete.");
                checks.Add(new Check { name = "camera-and-listener", passed = true, detail = "Native scene components." });
                Type controllerType = Type.GetType("VintageCollector.Runtime.OpeningGame, Assembly-CSharp");
                var controller = UnityEngine.Object.FindFirstObjectByType(controllerType);
                if (controller == null) throw new Exception("Opening controller missing.");
                var serialized = new SerializedObject(controller);
                foreach (string field in new[] { "catalogJson", "modelJson" })
                    if (serialized.FindProperty(field).objectReferenceValue == null) throw new Exception("Unwired " + field);
                if (serialized.FindProperty("allowedProjectRoot").stringValue != Path.GetDirectoryName(Application.dataPath).Replace('\\', '/'))
                    throw new Exception("Explicit root boundary was not serialized.");
                checks.Add(new Check { name = "controller-assets-and-root-wired", passed = true, detail = "Real TextAssets; no Resources duplicate." });
                if (SceneManager.GetActiveScene().path != scenePath) throw new Exception("Wrong active scene.");
            }
            catch (Exception error)
            {
                checks.Add(new Check { name = "build-scene-check-failure", passed = false, detail = (error.InnerException ?? error).ToString() });
            }
            bool passed = checks.Count > 0 && checks.TrueForAll(c => c.passed);
            string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), ".artifacts", "phase2-build");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "scene-checks.json"), JsonUtility.ToJson(new Report { passed = passed, checkCount = checks.Count, checks = checks.ToArray() }, true));
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else if (!passed) throw new Exception("Phase 2 scene checks failed.");
        }
    }
}
