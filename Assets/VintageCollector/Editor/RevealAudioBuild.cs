using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace VintageCollector.EditorTools {
 public static class RevealAudioBuild {
  public const string ApprovedRoot="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
  public const string ScenePath="Assets/VintageCollector/Scenes/BootstrapRevealAudio.unity";
  public const string TiltScene="Assets/VintageCollector/Scenes/BootstrapTilt.unity";
  [Serializable] private sealed class InputPin {public string path,sha256;}
  [Serializable] private sealed class Inputs {public InputPin[] files;public int fileCount;}
  [Serializable] private sealed class Evidence {public string result,outputPath,sourceProject,target,backend,graphicsApi,totalBytes;public int errors,warnings;}
  private static string Project(){return RevealAudioBuildPolicy.RequireOwnedStage(Path.GetDirectoryName(Application.dataPath),ApprovedRoot);}
  private static Type RuntimeType(string name){var t=Type.GetType(name+", Assembly-CSharp");if(t==null)throw new InvalidOperationException("Missing compiled runtime "+name);return t;}
  private static void Admission(){
   Project();RevealAudioBuildPolicy.Resolve(ApprovedRoot);
   var known=new HashSet<string>(StringComparer.Ordinal){ScenePath,TiltScene,"Assets/VintageCollector/Scenes/BootstrapPhase4.unity","Assets/VintageCollector/Scenes/Bootstrap.unity","Assets/VintageCollector/Scenes/BootstrapPhase3.unity"};
   foreach(var s in EditorBuildSettings.scenes)if(!known.Contains(s.path))throw new InvalidOperationException("Unrelated build scene; settings left unchanged");
  }
  [MenuItem("Tools/Vintage Collector/Reveal Audio/Create Separate Bootstrap")]
  public static void CreateBootstrap() {
   Admission();
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var hostType=RuntimeType("VintageCollector.Audio.RevealAudioHost");
   var probeType=RuntimeType("VintageCollector.Verification.RevealAudioSmokeProbe");
   RuntimeType("VintageCollector.Audio.RevealAudioProfile");
   RevealAudioNativeChecks.LoadProfile(); // fail before scene mutation if profile/import/API is incomplete
   var scene=EditorSceneManager.OpenScene(TiltScene,OpenSceneMode.Single);
   // OpenScene can unload a previously unreferenced ScriptableObject. Reacquire in the target scene context.
   var profile=RevealAudioNativeChecks.LoadProfile();
   if(profile==null)throw new InvalidOperationException("Profile unloaded while opening accepted scene");
   Component table=null;foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))if(c!=null&&c.GetType().FullName=="VintageCollector.Presentation.TabletopPresentation") {if(table!=null)throw new InvalidOperationException("Duplicate renderer");table=c;}
   if(table==null||table.GetType().GetProperty("RevealSounds")==null)throw new InvalidOperationException("Current renderer RevealSounds API missing");
   if(table.GetComponent(hostType)!=null||table.GetComponent(probeType)!=null)throw new InvalidOperationException("Accepted tilt scene already contains new components");
   var host=table.gameObject.AddComponent(hostType);var serialized=new SerializedObject(host);var field=serialized.FindProperty("profile");
   if(field==null)throw new InvalidOperationException("Serialized public profile missing");field.objectReferenceValue=profile;serialized.ApplyModifiedPropertiesWithoutUndo();
   if(serialized.FindProperty("profile").objectReferenceValue==null)throw new InvalidOperationException("Profile assignment did not survive serialization");
   table.gameObject.AddComponent(probeType);EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("New audio scene save failed");
   AssetDatabase.SaveAssets();RevealAudioNativeChecks.ValidateScene(ScenePath);
  }
  [MenuItem("Tools/Vintage Collector/Reveal Audio/Build Separate Windows Candidate")]
  public static void BuildWindows() {
   Admission();string project=Project(),folder=RevealAudioBuildPolicy.Resolve(ApprovedRoot);
   RevealAudioNativeChecks.ValidateScene(ScenePath);
   string evidence=Path.Combine(ApprovedRoot,".artifacts","reveal-audio-build");Directory.CreateDirectory(evidence);
   PlayerSettings.productName="Vintage Card Collector";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
   PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
   PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
   var pins=new List<InputPin>();foreach(string tree in new[]{"Assets","Packages","ProjectSettings"})foreach(var pair in RevealAudioBuildPolicy.CompilerManifest(Path.Combine(project,tree)))pins.Add(new InputPin{path=tree+"/"+pair.Key,sha256=pair.Value});
   File.WriteAllText(Path.Combine(evidence,"editor-compiled-inputs.json"),JsonUtility.ToJson(new Inputs{files=pins.ToArray(),fileCount=pins.Count},true));
   Directory.CreateDirectory(folder);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(folder,"VintageCardCollector.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   File.WriteAllText(Path.Combine(evidence,"editor-build-report.json"),JsonUtility.ToJson(new Evidence{result=report.summary.result.ToString(),outputPath=report.summary.outputPath,sourceProject=project,target="StandaloneWindows64",backend="Mono",graphicsApi="Direct3D11",totalBytes=report.summary.totalSize.ToString(System.Globalization.CultureInfo.InvariantCulture),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
   if(report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors!=0)throw new InvalidOperationException("Native reveal audio build failed");
  }
 }
}
