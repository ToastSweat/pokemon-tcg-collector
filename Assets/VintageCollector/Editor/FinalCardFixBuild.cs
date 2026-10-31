using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using VintageCollector.Runtime;

namespace VintageCollector.EditorTools
{
    public static class FinalCardFixBuild
    {
        private const string Root = "F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
        [Serializable] private sealed class Evidence { public string result, outputPath, sourceProject; public int errors, warnings; }
        [MenuItem("Tools/Vintage Collector/Phase 3/Build Final Card Table Flow Separately")]
        public static void Run()
        {
            string project=Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\','/');
            if(project!=Root && project!=Root+"/.artifacts/phase3-build-project") throw new InvalidOperationException("Unexpected project root.");
            string output=Path.Combine(Root,"Builds","Phase3TableFlowWindows");
            var scoped=GamePaths.Resolve(Root,Path.Combine(output,"VintageCardCollector_Data"),false,new string[0]);
            if(Directory.Exists(scoped.SaveDirectory) && Directory.GetFileSystemEntries(scoped.SaveDirectory).Length>0)
                throw new InvalidOperationException("Existing fix-candidate saves must be preserved; refusing rebuild.");
            string scene="Assets/VintageCollector/Scenes/BootstrapPhase3.unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene)==null)throw new InvalidOperationException("Existing wired Phase3 scene missing.");
            PlayerSettings.productName="Vintage Card Collector";
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.resizableWindow=true; PlayerSettings.runInBackground=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{scene},locationPathName=Path.Combine(output,"VintageCardCollector.exe"),
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            string folder=Path.Combine(Root,".artifacts","final-card-fix"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"build-report.json"),JsonUtility.ToJson(new Evidence { result=report.summary.result.ToString(),
                outputPath=report.summary.outputPath, sourceProject=project, errors=report.summary.totalErrors,warnings=report.summary.totalWarnings },true));
            if(report.summary.result!=BuildResult.Succeeded || report.summary.totalErrors!=0)throw new Exception("Final-card candidate build failed.");
        }
    }
}
