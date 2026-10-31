using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VintageCollector.EditorTools
{
    public static class MaterialsBuild
    {
        public const string ApprovedRoot="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
        public const string ScenePath="Assets/VintageCollector/Scenes/BootstrapMaterials.unity";
        [Serializable] private sealed class BuildEvidence {
            public string result,outputPath,sourceProject,target,backend,graphicsApi,totalBytes;
            public int errors,warnings;
        }
        [Serializable] private sealed class InputPin {public string path,sha256;}
        [Serializable] private sealed class Inputs {public InputPin[] files;public int fileCount;}
        private static string Project() {return MaterialsBuildPolicy.RequireOwnedStage(Path.GetDirectoryName(Application.dataPath),ApprovedRoot);}
        private static Type RuntimeType(string name) {
            Type type=Type.GetType(name+", Assembly-CSharp");if(type==null)throw new InvalidOperationException("Missing compiled runtime: "+name);return type;
        }
        private static void Admission() {
            Project();MaterialsBuildPolicy.Resolve(ApprovedRoot);
            var known=new HashSet<string>(StringComparer.Ordinal) {ScenePath,"Assets/VintageCollector/Scenes/BootstrapPhase4.unity","Assets/VintageCollector/Scenes/Bootstrap.unity","Assets/VintageCollector/Scenes/BootstrapPhase3.unity"};
            foreach(var scene in EditorBuildSettings.scenes)if(!known.Contains(scene.path))throw new InvalidOperationException("Unrelated build scene present; settings left unchanged");
        }
        [MenuItem("Tools/Vintage Collector/Materials/Create Separate Bootstrap")]
        public static void CreateBootstrap() {
            Admission(); // All admission checks precede scene mutation.
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var catalog=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
            var model=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
            if(catalog==null || model==null)throw new InvalidOperationException("Original catalog/model missing");
            string[] types={"VintageCollector.Presentation.TabletopPresentation","VintageCollector.Runtime.OpeningGame","VintageCollector.Runtime.Phase3MediaHost","VintageCollector.Presentation.BinderPresentation","VintageCollector.Runtime.Phase4CollectionHost","VintageCollector.Verification.Phase4SmokeProbe","VintageCollector.Verification.MaterialsSmokeProbe"};
            var compiled=new Type[types.Length];for(int i=0;i<types.Length;i++)compiled[i]=RuntimeType(types[i]);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(28,39,35,255);cameraObject.AddComponent<AudioListener>();RenderSettings.skybox=null;
            var table=new GameObject("Vintage Card Collector - Materials");
            foreach(Type type in compiled)table.AddComponent(type);
            var serialized=new SerializedObject(table.GetComponent(compiled[1]));
            serialized.FindProperty("catalogJson").objectReferenceValue=catalog;serialized.FindProperty("modelJson").objectReferenceValue=model;
            serialized.FindProperty("allowedProjectRoot").stringValue=ApprovedRoot;serialized.ApplyModifiedPropertiesWithoutUndo();
            var media=new SerializedObject(table.GetComponent(compiled[2]));
            media.FindProperty("manifestRelativePath").stringValue="Assets/Supplied/BaseSetMaterials/Manifest.json";
            media.FindProperty("fullBaseSetBudget").boolValue=true;media.ApplyModifiedPropertiesWithoutUndo();
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Materials bootstrap save failed");
            // Do NOT replace build settings, including unrelated or accepted bootstrap scenes.
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Vintage Collector/Materials/Build Separate Windows Candidate")]
        public static void BuildWindows() {
            Admission();string project=Project(),folder=MaterialsBuildPolicy.Resolve(ApprovedRoot);

            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var components=new List<MonoBehaviour>();foreach(var root in scene.GetRootGameObjects())components.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true));
            var required=new HashSet<string> {"OpeningGame","TabletopPresentation","Phase3MediaHost","Phase4CollectionHost","BinderPresentation","Phase4SmokeProbe","MaterialsSmokeProbe"};
            foreach(var component in components) {
                if(component==null)throw new InvalidOperationException("Bootstrap contains a missing script");
                if(!required.Remove(component.GetType().Name))throw new InvalidOperationException("Unexpected/duplicate bootstrap script: "+component.GetType().Name);
            }
            if(required.Count!=0)throw new InvalidOperationException("Bootstrap runtime closure incomplete");
            string evidence=Path.Combine(ApprovedRoot,".artifacts","materials-build");Directory.CreateDirectory(evidence);
            PlayerSettings.productName="Vintage Card Collector";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            var pins=new List<InputPin>();foreach(string tree in new[]{"Assets","Packages","ProjectSettings"})
                foreach(var pair in MaterialsBuildPolicy.ImmutableManifest(Path.Combine(project,tree)))pins.Add(new InputPin {path=tree+"/"+pair.Key,sha256=pair.Value});
            File.WriteAllText(Path.Combine(evidence,"editor-compiled-inputs.json"),JsonUtility.ToJson(new Inputs {files=pins.ToArray(),fileCount=pins.Count},true));
            Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName=Path.Combine(folder,"VintageCardCollector.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            File.WriteAllText(Path.Combine(evidence,"editor-build-report.json"),JsonUtility.ToJson(new BuildEvidence {
                result=report.summary.result.ToString(),outputPath=report.summary.outputPath,sourceProject=project,target="StandaloneWindows64",backend="Mono",graphicsApi="Direct3D11",
                totalBytes=report.summary.totalSize.ToString(System.Globalization.CultureInfo.InvariantCulture),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
            if(report.summary.result!=BuildResult.Succeeded || report.summary.totalErrors!=0)throw new InvalidOperationException("Materials native build failed");
        }
    }
}
