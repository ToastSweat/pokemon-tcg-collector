using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace VintageCollector.EditorTools {
 public static class Phase5NativeChecks {
  [Serializable] public sealed class Check {public string name,detail;public bool passed;}
  [Serializable] public sealed class MediaPin {public string path,guid,name,sha256,kind,sampleSha256;public int samples,channels,frequency,width,height;public float peak;}
  [Serializable] public sealed class Report {public bool passed;public int checkCount,mediaCount;public Check[] checks;public MediaPin[] media;public string[] dependencies;}
  static List<Check> checks;static List<MediaPin> media;
  static void Begin(){Phase5BuildPolicy.RequireOwnedStage(Phase5Build.Project,Phase5BuildPolicy.Root);checks=new List<Check>();media=new List<MediaPin>();}
  static void CheckThat(string name,bool ok,string detail=""){checks.Add(new Check{name=name,passed=ok,detail=detail});if(!ok)throw new InvalidOperationException(name+": "+detail);}
  static MediaPin Pin(string path,string kind){return new MediaPin{path=path,guid=AssetDatabase.AssetPathToGUID(path),sha256=Phase5BuildPolicy.Hash(Path.Combine(Phase5Build.Project,path)),kind=kind};}
  static string ByteHash(byte[] b){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
  static void ApprovedData(){
   string[] names={"jungle-en-unlimited1999.catalog.json","jungle-en-unlimited1999.rules.json","fossil-en-unlimited1999.catalog.json","fossil-en-unlimited1999.rules.json"};
   string[] pins={"60fc385e944778008e284c96d0038411238c0b44607b370537c825b9c88a0355","0d58b66ee91099bf6ad47845d62751d523404bd0dc866a62f1897041972bff12","e724f63927dd66a819c02ab4dd14408ccd0affd2bee6d5a8caa7908ed5f69860","eff5082e6f11efbe638d62cfbd314bda1e2fb8d54b5c7534d3a77956cd10353e"};
   for(int i=0;i<names.Length;i++){string source=Path.Combine(Phase5BuildPolicy.Root,"Authoring/Phase5/ApprovedData",names[i]),path="Assets/VintageCollector/Data/Phase5/"+names[i];CheckThat("approved-source-byte-pin:"+names[i],Phase5BuildPolicy.Hash(source)==pins[i]);var text=AssetDatabase.LoadAssetAtPath<TextAsset>(path);CheckThat("native-textasset:"+names[i],text!=null);CheckThat("TextAsset.bytes-native-source-parity:"+names[i],ByteHash(text.bytes)==pins[i]&&Phase5BuildPolicy.Hash(Path.Combine(Phase5Build.Project,path))==pins[i]);media.Add(Pin(path,"approved-textasset"));}
   CheckThat("approval-record-byte-pin",Phase5BuildPolicy.Hash(Path.Combine(Phase5BuildPolicy.Root,"docs/verification/Phase_5_Model_Approval.json"))=="014260958e10bc3c2d9b28018b66026ca73490732e6f618c7b045233101fed9d");
  }
  static void Profiles(){
   for(int set=0;set<3;set++){
    string profilePath=Phase5Build.ProfilePath(set);var profile=AssetDatabase.LoadAssetAtPath(profilePath,Phase5Build.RuntimeType("Audio.RevealAudioProfile"));CheckThat("native-profile:"+set,profile!=null);var data=new SerializedObject(profile);var pulls=data.FindProperty("pullSounds");CheckThat("profile-eleven-rare8:"+set,pulls!=null&&pulls.arraySize==11&&data.FindProperty("rareRevealPosition").intValue==8);
    var clips=new List<AudioClip>();for(int i=0;i<11;i++)clips.Add(pulls.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip);foreach(string n in new[]{"nonPokemonRare","pokemonRare","holoRare"})clips.Add(data.FindProperty(n).objectReferenceValue as AudioClip);
    CheckThat("profile-fourteen-native-clips:"+set,clips.Count==14&&new HashSet<AudioClip>(clips).Count==14&&!clips.Contains(null));
    foreach(var clip in clips){string path=AssetDatabase.GetAssetPath(clip);CheckThat("native-original-clip:"+set+":"+clip.name,path.StartsWith("Assets/VintageCollector/Audio/Clips/",StringComparison.Ordinal)&&path.EndsWith(".wav",StringComparison.OrdinalIgnoreCase));clip.LoadAudioData();float[] values=new float[clip.samples*clip.channels];CheckThat("pcm-read:"+set+":"+clip.name,values.Length>0&&clip.GetData(values,0));float peak=0;foreach(float v in values){if(float.IsNaN(v)||float.IsInfinity(v)||Math.Abs(v)>1.0001f)throw new InvalidOperationException("Nonfinite/out-of-range imported PCM");peak=Math.Max(peak,Math.Abs(v));}CheckThat("pcm-nonzero:"+set+":"+clip.name,peak>.001f);var pin=Pin(path,"clip-set-"+set);pin.name=clip.name;pin.samples=clip.samples;pin.channels=clip.channels;pin.frequency=clip.frequency;pin.peak=peak;byte[] bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);using(var hash=System.Security.Cryptography.SHA256.Create())pin.sampleSha256=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();media.Add(pin);}
    media.Add(Pin(profilePath,"profile"));
   }
  }
  [Serializable] public sealed class AuthoringAsset {public string setId,role,variant,relativePath,sha256;public int width,height;}
  [Serializable] public sealed class AuthoringManifest {public AuthoringAsset[] assets;}
  static void Textures(){
   string manifestPath=Path.Combine(Phase5BuildPolicy.Root,"Authoring/Phase5/Placeholders/PlaceholderManifest.json");CheckThat("approved-authoring-manifest-pin",Phase5BuildPolicy.Hash(manifestPath)=="0bbe868980bb8ca4148cf25c88efeff54913b5f494970dd59c244b3ddfabc722");var manifest=JsonUtility.FromJson<AuthoringManifest>(File.ReadAllText(manifestPath));CheckThat("exact-eight-authoring-png-roles",manifest!=null&&manifest.assets!=null&&manifest.assets.Length==8);
   var bindings=Phase5Build.ReadBindings();
   for(int i=1;i<3;i++){
    string set=i==1?"jungle":"fossil";var paths=new List<string>{bindings.sets[i].templatePath};paths.AddRange(bindings.sets[i].wrapperPaths);
    for(int k=0;k<paths.Count;k++){
     string path=paths[k],role=k==0?"card-face-template":"wrapper-front",variant=k==0?"template":((char)('A'+k-1)).ToString();AuthoringAsset admitted=null;int matches=0;foreach(var item in manifest.assets)if(item.setId==set&&item.role==role&&item.variant==variant){admitted=item;matches++;}CheckThat("exact-authoring-role:"+set+":"+variant,matches==1);
     var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);CheckThat("native-texture:"+path,tex!=null&&tex.width==admitted.width&&tex.height==admitted.height);CheckThat("authoring-role-native-byte-parity:"+path,Phase5BuildPolicy.Hash(Path.Combine(Phase5Build.Project,path))==admitted.sha256&&Phase5BuildPolicy.Hash(Path.Combine(Phase5BuildPolicy.Root,admitted.relativePath))==admitted.sha256);
     var importer=AssetImporter.GetAtPath(path) as TextureImporter;CheckThat("native-readable-texture:"+path,importer!=null&&importer.isReadable&&importer.npotScale==TextureImporterNPOTScale.None&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed);Color32[] pixels=tex.GetPixels32();CheckThat("native-pixels:"+path,pixels.Length==tex.width*tex.height);bool nonzero=false;byte[] sample=new byte[pixels.Length*4];for(int p=0;p<pixels.Length;p++){var v=pixels[p];if(v.a!=0&&(v.r!=0||v.g!=0||v.b!=0))nonzero=true;sample[p*4]=v.r;sample[p*4+1]=v.g;sample[p*4+2]=v.b;sample[p*4+3]=v.a;}CheckThat("native-pixels-nonzero:"+path,nonzero);var pin=Pin(path,"texture");pin.width=tex.width;pin.height=tex.height;pin.sampleSha256=ByteHash(sample);media.Add(pin);
    }
   }
  }
  public static void ValidateAssets(){Begin();Profiles();Textures();ApprovedData();Publish(new string[0]);}
  static void Publish(string[] dependencies){Phase5Build.WriteReport("native-checks.json",new Report{passed=true,checkCount=checks.Count,checks=checks.ToArray(),mediaCount=media.Count,media=media.ToArray(),dependencies=dependencies});}
  static void ExecutionOrder(MonoBehaviour component,int expected){
   string name=component.GetType().Name;var attribute=(DefaultExecutionOrder)Attribute.GetCustomAttribute(component.GetType(),typeof(DefaultExecutionOrder),false);
   CheckThat("default-execution-order:"+name,attribute!=null&&attribute.order==expected,"required attribute="+expected+", actual="+(attribute==null?"missing":attribute.order.ToString()));
   int order=MonoImporter.GetExecutionOrder(MonoScript.FromMonoBehaviour(component));
   CheckThat("actual-execution-order:"+name,order==0||order==expected,"importer override="+order+", required attribute="+expected);
  }
  static void NativeConfiguration(Component game){
   var method=game.GetType().GetMethod("PreparePhase5Configuration",Type.EmptyTypes);CheckThat("native-config-admission-method",method!=null);var registry=method.Invoke(game,null);CheckThat("native-approved-registry",registry!=null);var sets=registry.GetType().GetProperty("Sets").GetValue(registry,null);CheckThat("native-registry-exact-three",(int)sets.GetType().GetProperty("Count").GetValue(sets,null)==3);
   CheckThat("native-configuration-no-store-side-effects",game.GetType().GetProperty("Paths").GetValue(game,null)==null&&game.GetType().GetProperty("CurrentSession").GetValue(game,null)==null);
   string[] ids={"base-en-unlimited1999","jungle-en-unlimited1999","fossil-en-unlimited1999"};int[] targets={101,64,62};for(int i=0;i<3;i++){var definition=registry.GetType().GetMethod("FindSet",new[]{typeof(string)}).Invoke(registry,new object[]{ids[i]});CheckThat("native-approved-target:"+ids[i],definition!=null&&(int)definition.GetType().GetProperty("CompletionTarget").GetValue(definition,null)==targets[i]);}
  }
  public static void ValidateScene(){
   Begin();var scene=EditorSceneManager.OpenScene(Phase5Build.ScenePath,OpenSceneMode.Single);Profiles();Textures();ApprovedData();var bindings=Phase5Build.ReadBindings();
   var required=new HashSet<string>{"OpeningGame","TabletopPresentation","BinderPresentation","SetSelectorPresentation","Phase5CollectionHost","Phase5MediaHost","Phase5SmokeProbe"};int cameras=0,listeners=0;GameObject table=null;var objects=new List<GameObject>();
   foreach(var root in scene.GetRootGameObjects()){cameras+=root.GetComponentsInChildren<Camera>(true).Length;listeners+=root.GetComponentsInChildren<AudioListener>(true).Length;foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)){
    CheckThat("exact-scene-component:"+(c==null?"missing":c.GetType().Name),c!=null&&required.Remove(c.GetType().Name));objects.Add(c.gameObject);var data=new SerializedObject(c);string name=c.GetType().Name;
    if(name=="TabletopPresentation"){table=c.gameObject;CheckThat("pointerTiltEnabled",data.FindProperty("pointerTiltEnabled").boolValue);}
    if(name=="OpeningGame"){NativeConfiguration(c);var sets=data.FindProperty("phase5Sets");CheckThat("phase5Sets-exact-three",sets!=null&&sets.arraySize==3);for(int i=0;i<3;i++){var e=sets.GetArrayElementAtIndex(i);var b=bindings.sets[i];CheckThat("set-binding:"+i,e.FindPropertyRelative("printingId").stringValue==b.printingId&&e.FindPropertyRelative("catalogJson").objectReferenceValue==AssetDatabase.LoadAssetAtPath<TextAsset>(b.catalogPath)&&e.FindPropertyRelative("modelJson").objectReferenceValue==AssetDatabase.LoadAssetAtPath<TextAsset>(b.modelPath)&&e.FindPropertyRelative("revealAudioProfile").objectReferenceValue!=null&&AssetDatabase.GetAssetPath(e.FindPropertyRelative("revealAudioProfile").objectReferenceValue)==Phase5Build.ProfilePath(i));if(i>0){CheckThat("template-binding:"+i,e.FindPropertyRelative("cardFaceTemplate").objectReferenceValue==AssetDatabase.LoadAssetAtPath<Texture2D>(b.templatePath));var w=e.FindPropertyRelative("wrapperFronts");CheckThat("wrapper-three:"+i,w.arraySize==3);for(int j=0;j<3;j++)CheckThat("wrapper-binding:"+i+":"+j,w.GetArrayElementAtIndex(j).objectReferenceValue==AssetDatabase.LoadAssetAtPath<Texture2D>(b.wrapperPaths[j]));}}}
    if(name=="Phase5MediaHost")CheckThat("accepted-base-media",data.FindProperty("manifestRelativePath").stringValue=="Assets/Supplied/BaseSetMaterials/Manifest.json"&&data.FindProperty("fullBaseSetBudget").boolValue);
    if(name=="Phase5CollectionHost"||name=="Phase5MediaHost"||name=="Phase5SmokeProbe")ExecutionOrder(c,name=="Phase5CollectionHost"?-200:name=="Phase5MediaHost"?-100:-90);
   }}
   CheckThat("complete-scene-components",required.Count==0&&cameras==1&&listeners==1&&table!=null);for(int i=0;i<objects.Count;i++)CheckThat("co-located-component:"+i,objects[i]==table);
   string[] deps=AssetDatabase.GetDependencies(Phase5Build.ScenePath,true);foreach(string path in deps){if(!path.StartsWith("Assets/",StringComparison.Ordinal))continue;string guid=AssetDatabase.AssetPathToGUID(path);CheckThat("native-guid-reference:"+path,guid.Length==32&&AssetDatabase.GUIDToAssetPath(guid)==path&&File.Exists(Path.Combine(Phase5Build.Project,path+".meta")));}
   Publish(deps);
  }
 }
}
