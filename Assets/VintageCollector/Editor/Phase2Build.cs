using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VintageCollector.EditorTools
{
    public static class Phase2Build
    {
        private const string ScenePath = "Assets/VintageCollector/Scenes/Bootstrap.unity";
        private const string ApprovedRoot = "F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
        [Serializable] private sealed class BuildEvidence
        {
            public string result, outputPath, target, backend, graphicsApi;
            public int errors, warnings;
            public string totalBytes;
        }
        private static string Root()
        {
            string root = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\', '/');
            if (!string.Equals(root, ApprovedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This build helper may write only the approved project root.");
            return root;
        }
        private static Type RuntimeType(string name)
        {
            Type type = Type.GetType(name + ", Assembly-CSharp");
            if (type == null) throw new InvalidOperationException("Required runtime component not compiled: " + name);
            return type;
        }
        [MenuItem("Tools/Vintage Collector/Phase 2/Create Bootstrap Scene")]
        public static void CreateBootstrap()
        {
            CreateScene();
        }
        private static bool CreateScene()
        {
            string root = Root();
            // Cancellation must propagate before any scene mutation or directory write.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            foreach (var item in EditorBuildSettings.scenes)
                if (item.path != ScenePath) throw new InvalidOperationException("Existing non-Phase-2 build scene found; refusing to overwrite its build list.");
            Type controllerType = RuntimeType("VintageCollector.Runtime.OpeningGame");
            Type presentationType = RuntimeType("VintageCollector.Presentation.TabletopPresentation");
            Type smokeType = RuntimeType("VintageCollector.Verification.Phase2SmokeRunner");
            var catalog = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
            var model = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
            if (catalog == null || model == null) throw new InvalidOperationException("Approved catalog/model TextAssets missing.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(28, 39, 35, 255);
            cameraObject.AddComponent<AudioListener>();
            RenderSettings.skybox = null;
            var table = new GameObject("Vintage Card Collector - Phase 2");
            table.AddComponent(presentationType);
            var controller = table.AddComponent(controllerType);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("catalogJson").objectReferenceValue = catalog;
            serialized.FindProperty("modelJson").objectReferenceValue = model;
            serialized.FindProperty("allowedProjectRoot").stringValue = root;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            table.AddComponent(smokeType); // Explicit --vcc-smoke only; ordinary startup is passive.
            Directory.CreateDirectory(Path.Combine(root, "Assets", "VintageCollector", "Scenes"));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Bootstrap scene save failed.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            return true;
        }
        [MenuItem("Tools/Vintage Collector/Phase 2/Build Windows Player")]
        public static void BuildWindows()
        {
            if (!CreateScene()) return;
            string root = Root();
            PlayerSettings.productName = "Vintage Card Collector";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            string executable = Path.Combine(root, "Builds", "Phase2Windows", "VintageCardCollector.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = executable,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            string evidence = Path.Combine(root, ".artifacts", "phase2-build");
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "build-report.json"), JsonUtility.ToJson(new BuildEvidence {
                result = report.summary.result.ToString(), outputPath = report.summary.outputPath,
                target = "StandaloneWindows64", backend = "Mono", graphicsApi = "Direct3D11",
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                totalBytes = report.summary.totalSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }, true));
            Debug.Log("PHASE2_BUILD " + report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                throw new InvalidOperationException("Phase 2 Windows build failed; inspect local build report.");
        }
    }
}
