using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VintageCollector.EditorTools {
 public static class RevealAudioNativeChecks {
  [Serializable] public sealed class ClipPin {public string path,guid,name,float32Sha256;public int samples,channels,frequency;public float peak;}
  [Serializable] public sealed class Report {public bool passed;public int clipCount;public string profile,scene;public ClipPin[] clips;}
  public static UnityEngine.Object LoadProfile() {
   RevealAudioBuildPolicy.RequireOwnedStage(Path.GetDirectoryName(Application.dataPath),RevealAudioBuild.ApprovedRoot);
   var type=Type.GetType("VintageCollector.Audio.RevealAudioProfile, Assembly-CSharp");if(type==null)throw new InvalidOperationException("Current RevealAudioProfile component missing");
   string[] guids=AssetDatabase.FindAssets("t:RevealAudioProfile",new[]{"Assets/VintageCollector/Audio"});
   if(guids.Length!=1)throw new InvalidOperationException("Exactly one production audio profile required");
   const string profilePath="Assets/VintageCollector/Audio/DefaultRevealAudioProfile.asset";
   if(AssetDatabase.GUIDToAssetPath(guids[0])!=profilePath)throw new InvalidOperationException("Frozen default profile path required");
   var profile=AssetDatabase.LoadAssetAtPath(profilePath,type);if(profile==null)throw new InvalidOperationException("Production profile import failed");
   var data=new SerializedObject(profile);var pulls=data.FindProperty("pullSounds");
   if(pulls==null||!pulls.isArray||pulls.arraySize!=11||data.FindProperty("rareRevealPosition")==null||data.FindProperty("rareRevealPosition").intValue!=8)throw new InvalidOperationException("Exact profile API: eleven pulls, rare eight");
   var clips=new List<AudioClip>();for(int i=0;i<11;i++)clips.Add(pulls.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip);
   foreach(string field in new[]{"nonPokemonRare","pokemonRare","holoRare"}) {var property=data.FindProperty(field);if(property==null)throw new InvalidOperationException("Profile field missing "+field);clips.Add(property.objectReferenceValue as AudioClip);}
   if(clips.Count!=14||new HashSet<AudioClip>(clips).Count!=14||clips.Contains(null))throw new InvalidOperationException("Fourteen distinct native imported clips required");
   var pins=new List<ClipPin>();foreach(var clip in clips) {
    string path=AssetDatabase.GetAssetPath(clip);if(!path.StartsWith("Assets/VintageCollector/Audio/",StringComparison.Ordinal)||!path.EndsWith(".wav",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Clip must be new locally supplied WAV");
    clip.LoadAudioData();var samples=new float[clip.samples*clip.channels];if(samples.Length==0||!clip.GetData(samples,0))throw new InvalidOperationException("Actual imported samples unreadable");
    float peak=0;foreach(float value in samples){if(float.IsNaN(value)||float.IsInfinity(value)||Math.Abs(value)>1.0001f)throw new InvalidOperationException("Invalid imported sample");peak=Math.Max(peak,Math.Abs(value));}
    if(peak<.001f)throw new InvalidOperationException("Imported clip is silent");
    var bytes=new byte[samples.Length*sizeof(float)];Buffer.BlockCopy(samples,0,bytes,0,bytes.Length);string hash;using(var h=SHA256.Create())hash=BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
    pins.Add(new ClipPin{path=path,guid=AssetDatabase.AssetPathToGUID(path),name=clip.name,samples=clip.samples,frequency=clip.frequency,channels=clip.channels,peak=peak,float32Sha256=hash});
   }
   string folder=Path.Combine(RevealAudioBuild.ApprovedRoot,".artifacts","reveal-audio-build");Directory.CreateDirectory(folder);
   File.WriteAllText(Path.Combine(folder,"editor-audio-profile.json"),JsonUtility.ToJson(new Report{passed=true,clipCount=pins.Count,profile=AssetDatabase.GetAssetPath(profile),clips=pins.ToArray()},true));return profile;
  }
  public static void ValidateScene(string scenePath) {
   RevealAudioBuildPolicy.RequireOwnedStage(Path.GetDirectoryName(Application.dataPath),RevealAudioBuild.ApprovedRoot);
   var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);var profile=LoadProfile();
   if(profile==null)throw new InvalidOperationException("Native profile missing after scene open");
   var required=new HashSet<string>{"OpeningGame","TabletopPresentation","Phase3MediaHost","Phase4CollectionHost","BinderPresentation","Phase4SmokeProbe","MaterialsSmokeProbe","TiltSmokeProbe","RevealAudioHost","RevealAudioSmokeProbe"};
   int cameras=0,listeners=0;GameObject table=null,hostObject=null;
   foreach(var root in scene.GetRootGameObjects()) {
    cameras+=root.GetComponentsInChildren<Camera>(true).Length;listeners+=root.GetComponentsInChildren<AudioListener>(true).Length;
    foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)) {
     if(c==null||!required.Remove(c.GetType().Name))throw new InvalidOperationException("Unexpected, duplicate, or missing scene component");
     var serialized=new SerializedObject(c);
     if(c.GetType().Name=="TabletopPresentation") {table=c.gameObject;if(!serialized.FindProperty("pointerTiltEnabled").boolValue||c.GetType().GetProperty("RevealSounds")==null)throw new InvalidOperationException("Current tilt renderer required");}
     if(c.GetType().Name=="RevealAudioHost") {hostObject=c.gameObject;if(serialized.FindProperty("profile")==null||serialized.FindProperty("profile").objectReferenceValue!=profile)throw new InvalidOperationException("Exact production profile binding missing");}
     if(c.GetType().Name=="Phase3MediaHost"&&(serialized.FindProperty("manifestRelativePath").stringValue!="Assets/Supplied/BaseSetMaterials/Manifest.json"||!serialized.FindProperty("fullBaseSetBudget").boolValue))throw new InvalidOperationException("Full accepted media configuration required");
     if(c.GetType().Name=="OpeningGame"&&(serialized.FindProperty("allowedProjectRoot").stringValue!=RevealAudioBuild.ApprovedRoot||serialized.FindProperty("catalogJson").objectReferenceValue==null||serialized.FindProperty("modelJson").objectReferenceValue==null))throw new InvalidOperationException("Original catalog/model/root closure missing");
    }
   }
   if(required.Count!=0||cameras!=1||listeners!=1||table==null||table!=hostObject)throw new InvalidOperationException("Complete native scene/camera/listener/co-located host closure required");
  }
 }
}
