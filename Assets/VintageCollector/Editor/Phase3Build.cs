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
    public static class Phase3Build
    {
        private const string ApprovedRoot = "F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
        private const string ScenePath = "Assets/VintageCollector/Scenes/BootstrapPhase3.unity";
        [Serializable] private sealed class BuildEvidence
        {
            public string result, outputPath, sourceProject, approvedRoot, target, backend, graphicsApi, totalBytes;
            public int errors, warnings;
        }
        private static string CurrentProject()
        {
            string current = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\', '/');
            string stage = ApprovedRoot + "/.artifacts/phase3-build-project";
            if (!string.Equals(current, ApprovedRoot, StringComparison.OrdinalIgnoreCase) && !string.Equals(current, stage, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Phase 3 builder requires the approved original or owned exact-source stage.");
            return current;
        }
        private static Type RuntimeType(string name)
        {
            var type = Type.GetType(name + ", Assembly-CSharp");
            if (type == null) throw new InvalidOperationException("Missing compiled runtime component: " + name);
            return type;
        }
        [MenuItem("Tools/Vintage Collector/Phase 3/Create Candidate Scene")]
        public static void CreateBootstrap() { CreateScene(); }
        private static bool CreateScene()
        {
            string project = CurrentProject();
            Phase3BuildPolicy.Resolve(ApprovedRoot); // Read-only admission BEFORE scene/output mutation.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            foreach (var item in EditorBuildSettings.scenes)
                if (item.path != ScenePath && item.path != "Assets/VintageCollector/Scenes/Bootstrap.unity")
                    throw new InvalidOperationException("Refusing to replace unrelated scene build settings.");
            var catalog = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
            var model = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
            if (catalog == null || model == null) throw new InvalidOperationException("Original data references missing.");
            var gameType = RuntimeType("VintageCollector.Runtime.OpeningGame");
            var presentationType = RuntimeType("VintageCollector.Presentation.TabletopPresentation");
            var hostType = RuntimeType("VintageCollector.Runtime.Phase3MediaHost");
            var smokeType = RuntimeType("VintageCollector.Verification.Phase2SmokeRunner");
            var probeType = RuntimeType("VintageCollector.Verification.Phase3MediaProbe");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0,0,-10); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(28,39,35,255);
            cameraObject.AddComponent<AudioListener>(); RenderSettings.skybox = null;
            var table = new GameObject("Vintage Card Collector - Phase 3A");
            table.AddComponent(presentationType);
            var game = table.AddComponent(gameType);
            var serialized = new SerializedObject(game);
            serialized.FindProperty("catalogJson").objectReferenceValue = catalog;
            serialized.FindProperty("modelJson").objectReferenceValue = model;
            serialized.FindProperty("allowedProjectRoot").stringValue = ApprovedRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            table.AddComponent(hostType);
            table.AddComponent(smokeType); table.AddComponent(probeType); // Both explicit-smoke guarded.
            Directory.CreateDirectory(Path.Combine(project,"Assets","VintageCollector","Scenes"));
            if (!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Candidate scene save failed.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            AssetDatabase.SaveAssets();
            return true;
        }
        [MenuItem("Tools/Vintage Collector/Phase 3/Build Separate Windows Candidate")]
        public static void BuildWindows()
        {
            string project = CurrentProject();
            string folder = Phase3BuildPolicy.Resolve(ApprovedRoot);
            if (!CreateScene()) return;
            PlayerSettings.productName = "Vintage Card Collector";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[] { GraphicsDeviceType.Direct3D11 });
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath },
                locationPathName = Path.Combine(folder,"VintageCardCollector.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            string evidence = Path.Combine(ApprovedRoot,".artifacts","phase3-build"); Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence,"build-report.json"),JsonUtility.ToJson(new BuildEvidence {
                result = report.summary.result.ToString(), outputPath = report.summary.outputPath,
                sourceProject = project, approvedRoot = ApprovedRoot, target = "StandaloneWindows64", backend = "Mono", graphicsApi = "Direct3D11",
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                totalBytes = report.summary.totalSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },true));
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                throw new InvalidOperationException("Candidate build failed; inspect project-local report.");
        }
    }
}
