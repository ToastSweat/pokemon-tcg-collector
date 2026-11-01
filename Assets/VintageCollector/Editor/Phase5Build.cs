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
 public static class Phase5Build {
  public const string ScenePath="Assets/VintageCollector/Scenes/BootstrapPhase5.unity";
  public const string BaseScene="Assets/VintageCollector/Scenes/BootstrapRevealAudio.unity";
  public const string BaseProfile="Assets/VintageCollector/Audio/DefaultRevealAudioProfile.asset";
  [Serializable] public sealed class Binding {public string printingId,displayLabel,catalogPath,modelPath,templatePath;public string[] wrapperPaths;}
  [Serializable] public sealed class Bindings {public Binding[] sets;}
  [Serializable] public sealed class Pin {public string path,sha256;}
  [Serializable] public sealed class Inputs {public Pin[] files;public int fileCount;}
  [Serializable] public sealed class Evidence {public string result,outputPath,sourceProject,target,backend,graphicsApi;public int errors,warnings;public bool development;}
  public static string Project {get{return Path.GetDirectoryName(Application.dataPath);}}
  public static Type RuntimeType(string name){var t=Type.GetType("VintageCollector."+name+", Assembly-CSharp");if(t==null)throw new InvalidOperationException("Missing assembled runtime "+name);return t;}
  public static string ProfilePath(int i){return i==0?BaseProfile:"Assets/VintageCollector/Audio/Profiles/Phase5/"+(i==1?"Jungle":"Fossil")+"RevealAudioProfile.asset";}
  public static Bindings ReadBindings(){string path=Path.Combine(Project,".phase5-bindings.json");Phase5BuildPolicy.Safe(path);var b=JsonUtility.FromJson<Bindings>(File.ReadAllText(path));string[] ids={"base-en-unlimited1999","jungle-en-unlimited1999","fossil-en-unlimited1999"};if(b==null||b.sets==null||b.sets.Length!=3)throw new InvalidOperationException("Exactly three frozen binding entries required");for(int i=0;i<3;i++){var s=b.sets[i];if(s==null||s.printingId!=ids[i]||string.IsNullOrEmpty(s.displayLabel))throw new InvalidOperationException("Canonical ordered bindings required");RequireAsset(s.catalogPath,i==0?"Assets/VintageCollector/Data/":"Assets/VintageCollector/Data/Phase5/",".json");RequireAsset(s.modelPath,i==0?"Assets/VintageCollector/Data/":"Assets/VintageCollector/Data/Phase5/",".json");if(i>0){if(s.catalogPath!="Assets/VintageCollector/Data/Phase5/"+ids[i]+".catalog.json"||s.modelPath!="Assets/VintageCollector/Data/Phase5/"+ids[i]+".rules.json")throw new InvalidOperationException("Canonical approved .catalog/.rules role paths required");RequireAsset(s.templatePath,"Assets/VintageCollector/Media/Phase5/",".png");if(s.wrapperPaths==null||s.wrapperPaths.Length!=3||new HashSet<string>(s.wrapperPaths).Count!=3)throw new InvalidOperationException("Three distinct ordered wrapper paths required");foreach(string w in s.wrapperPaths)RequireAsset(w,"Assets/VintageCollector/Media/Phase5/",".png");}}return b;}
  static void RequireAsset(string path,string prefix,string suffix){if(path==null||!path.StartsWith(prefix,StringComparison.Ordinal)||!path.EndsWith(suffix,StringComparison.Ordinal)||path.Contains("..")||path.Contains("\\")||!File.Exists(Path.Combine(Project,path)))throw new InvalidOperationException("Missing/foreign native role: "+path);}
  public static void WriteReport(string name,object report){string d=Path.Combine(Project,".phase5-native-reports");Phase5BuildPolicy.Safe(d);Directory.CreateDirectory(d);string target=Path.Combine(d,name);Phase5BuildPolicy.Safe(target);File.WriteAllText(target,JsonUtility.ToJson(report,true));}
  static bool Prompt(){return Application.isBatchMode||EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();}
  static void AssetsCore(){
   for(int i=1;i<3;i++)if(File.Exists(Path.Combine(Project,ProfilePath(i)))||File.Exists(Path.Combine(Project,ProfilePath(i)+".meta")))throw new InvalidOperationException("Native new profile collision; no importer mutation performed");
   ReadBindings();foreach(var b in ReadBindings().sets){if(b.printingId=="base-en-unlimited1999")continue;var paths=new List<string>{b.templatePath};paths.AddRange(b.wrapperPaths);foreach(string path in paths){var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Native texture importer missing");importer.isReadable=true;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}}var baseProfile=AssetDatabase.LoadAssetAtPath(BaseProfile,RuntimeType("Audio.RevealAudioProfile"));if(baseProfile==null)throw new InvalidOperationException("Accepted Base native profile missing");
   foreach(int i in new[]{1,2}){string path=ProfilePath(i);string absolute=Path.Combine(Project,path);Phase5BuildPolicy.Safe(absolute);if(File.Exists(absolute))throw new InvalidOperationException("CreateAssets never overwrites an existing native profile");Directory.CreateDirectory(Path.GetDirectoryName(absolute));AssetDatabase.Refresh();var clone=UnityEngine.Object.Instantiate(baseProfile);clone.name=i==1?"JungleRevealAudioProfile":"FossilRevealAudioProfile";var so=new SerializedObject(clone);so.FindProperty("rareRevealPosition").intValue=8;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(clone,path);}
   AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  }
  [MenuItem("Tools/Vintage Collector/Phase 5/Create Separate Native Assets")]
  public static void CreateAssets(){Phase5BuildPolicy.Admit(Project,"CreateAssets");if(!Prompt())return;AssetsCore();Phase5NativeChecks.ValidateAssets();}
  [MenuItem("Tools/Vintage Collector/Phase 5/Create Separate Bootstrap")]
  public static void CreateBootstrap(){
   Phase5BuildPolicy.Admit(Project,"CreateBootstrap");if(!Prompt())return;
   if(File.Exists(Path.Combine(Project,ScenePath)))throw new InvalidOperationException("CreateBootstrap never overwrites an existing scene");
   ReadBindings();Phase5NativeChecks.ValidateAssets();ConfigureRelease();
   var scene=EditorSceneManager.OpenScene(BaseScene,OpenSceneMode.Single);
   Component table=null;foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))if(c!=null&&c.GetType().Name=="TabletopPresentation"){if(table!=null)throw new InvalidOperationException("Duplicate table");table=c;}
   if(table==null)throw new InvalidOperationException("Accepted renderer absent");var go=table.gameObject;
   var keep=new HashSet<string>{"OpeningGame","TabletopPresentation","BinderPresentation"};
   foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))if(c==null)throw new InvalidOperationException("Missing accepted script");else if(!keep.Contains(c.GetType().Name))UnityEngine.Object.DestroyImmediate(c);
   foreach(string name in new[]{"Presentation.SetSelectorPresentation","Runtime.Phase5CollectionHost","Runtime.Phase5MediaHost","Verification.Phase5SmokeProbe"})go.AddComponent(RuntimeType(name));
   var game=go.GetComponent(RuntimeType("Runtime.OpeningGame"));var data=new SerializedObject(game);var sets=data.FindProperty("phase5Sets");if(sets==null)throw new InvalidOperationException("Assembled Phase5 config absent");sets.arraySize=3;var bindings=ReadBindings();
   for(int i=0;i<3;i++){var b=bindings.sets[i];var e=sets.GetArrayElementAtIndex(i);e.FindPropertyRelative("printingId").stringValue=b.printingId;e.FindPropertyRelative("displayLabel").stringValue=b.displayLabel;e.FindPropertyRelative("catalogJson").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(b.catalogPath);e.FindPropertyRelative("modelJson").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(b.modelPath);e.FindPropertyRelative("revealAudioProfile").objectReferenceValue=AssetDatabase.LoadAssetAtPath(ProfilePath(i),RuntimeType("Audio.RevealAudioProfile"));e.FindPropertyRelative("cardFaceTemplate").objectReferenceValue=i==0?null:AssetDatabase.LoadAssetAtPath<Texture2D>(b.templatePath);var w=e.FindPropertyRelative("wrapperFronts");w.arraySize=3;for(int j=0;j<3;j++)w.GetArrayElementAtIndex(j).objectReferenceValue=i==0?null:AssetDatabase.LoadAssetAtPath<Texture2D>(b.wrapperPaths[j]);e.FindPropertyRelative("cardBack").objectReferenceValue=null;e.FindPropertyRelative("wrapperBack").objectReferenceValue=null;}
   data.FindProperty("allowedProjectRoot").stringValue=Phase5BuildPolicy.Root;data.ApplyModifiedPropertiesWithoutUndo();
   var media=new SerializedObject(go.GetComponent(RuntimeType("Runtime.Phase5MediaHost")));var manifest=media.FindProperty("manifestRelativePath");var budget=media.FindProperty("fullBaseSetBudget");if(manifest==null||budget==null)throw new InvalidOperationException("Frozen host serialized Base media configuration absent");manifest.stringValue="Assets/Supplied/BaseSetMaterials/Manifest.json";budget.boolValue=true;media.ApplyModifiedPropertiesWithoutUndo();
   var tilt=new SerializedObject(table);tilt.FindProperty("pointerTiltEnabled").boolValue=true;tilt.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("New Phase5 scene save failed");AssetDatabase.SaveAssets();Phase5NativeChecks.ValidateScene();
  }
  static void ConfigureRelease(){
   PlayerSettings.productName="Vintage Card Collector";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
   PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});EditorUserBuildSettings.development=false;EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
  }
  [MenuItem("Tools/Vintage Collector/Phase 5/Build Separate Release Windows Candidate")]
  public static void BuildWindows(){
   string project=Phase5BuildPolicy.Admit(Project,"BuildWindows"),folder=Phase5BuildPolicy.Resolve(Phase5BuildPolicy.Root);if(!Prompt())return;Phase5NativeChecks.ValidateScene();
   ConfigureRelease();
   var pins=new List<Pin>();foreach(var pair in Phase5BuildPolicy.AllInputs(project))pins.Add(new Pin{path=pair.Key,sha256=pair.Value});WriteReport("compiled-inputs.json",new Inputs{files=pins.ToArray(),fileCount=pins.Count});
   Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,".phase5-candidate-owner.txt"),"phase5-candidate-v1\n"+Phase5BuildPolicy.Root+"\n");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(folder,"VintageCardCollector.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   WriteReport("build-report.json",new Evidence{result=report.summary.result.ToString(),outputPath=report.summary.outputPath,sourceProject=project,target="StandaloneWindows64",backend="Mono",graphicsApi="Direct3D11",development=false,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings});
   if(report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors!=0||report.summary.totalWarnings!=0)throw new InvalidOperationException("Native Phase5 release build requires zero errors and warnings");
  }
 }
}
