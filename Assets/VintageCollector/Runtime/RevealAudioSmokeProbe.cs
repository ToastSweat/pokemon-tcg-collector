using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Presentation;
using VintageCollector.Persistence;
namespace VintageCollector.Verification {
 // Native adapter/gameplay observations only. Inert without both validated smoke paths and exact marker.
 // Host(-200) initializes Paths synchronously; fence Tilt(-175)/Materials(-150) before their Start.
 [DefaultExecutionOrder(-180)]
 public sealed class RevealAudioSmokeProbe : MonoBehaviour {
  const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
  [Serializable] public sealed class Check {public string name;public bool passed;public string detail;}
  [Serializable] public sealed class Playback {public string name,expectedClip,actualClip;public bool childSourcePlaying;public float sourcePeak,listenerPeak;public int samples,frequency;}
  [Serializable] public sealed class Report {public bool passed;public int checkCount,generatedResultCount,totalVirtualCards;public string mode,failure,initialSession,finalSession;public Check[] checks;public Playback[] playback;public string[] screenshots,rareBranches;}
  public static string ValidateOptIn(GamePaths paths) {
   if(paths==null||!paths.IsSmokeTest)return null;
   string legacyPrefix=Path.GetFullPath(Path.Combine(Root,".artifacts","reveal-audio-legacy"))+Path.DirectorySeparatorChar;
   string legacyArtifacts=Path.GetFullPath(paths.ArtifactDirectory);
   if(legacyArtifacts.StartsWith(legacyPrefix,StringComparison.OrdinalIgnoreCase)) {
    string file=Path.Combine(legacyArtifacts,"reveal-audio-legacy-fixture.json");Safe(file);if(!File.Exists(file))return null;
    string[] bits=legacyArtifacts.Substring(legacyPrefix.Length).Split(Path.DirectorySeparatorChar);
    if(bits.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(bits[0],"^[A-Za-z0-9_-]+$"))throw new ArgumentException("Legacy fixture path mismatch");
    string scenario,save;
    if(bits[1]=="prepare"||bits[1]=="resume"){scenario=bits[1];save="session";}
    else if(bits[1]=="error"){scenario="error";save="error-session";}
    else if(bits[1]=="before-kill"||bits[1]=="after-kill"){scenario="input";save="crash-session";}
    else throw new ArgumentException("Unknown bounded legacy case");
    if(paths.SmokeScenario!=scenario||!string.Equals(Path.GetFullPath(paths.BaseSaveDirectory),Path.Combine(legacyPrefix,bits[0],save),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Legacy fixture save/scenario mismatch");
    Safe(paths.BaseSaveDirectory);byte[] b=new byte[257];int size=0;using(var f=File.OpenRead(file)){int n;while(size<b.Length&&(n=f.Read(b,size,b.Length-size))>0)size+=n;}
    string expected="{\"schemaVersion\":1,\"owner\":\"reveal-audio-legacy-fixture\",\"case\":\""+bits[1]+"\"}";
    if(size>256||new System.Text.UTF8Encoding(false,true).GetString(b,0,size)!=expected)throw new ArgumentException("Exact bounded legacy marker required");
    return "legacy";
   }
   string prefix=Path.GetFullPath(Path.Combine(Root,".artifacts","reveal-audio-player"))+Path.DirectorySeparatorChar;
   string artifacts=Path.GetFullPath(paths.ArtifactDirectory);if(!artifacts.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))return null;
   string marker=Path.Combine(artifacts,"reveal-audio-fixture.json");Safe(marker);if(!File.Exists(marker))return null;
   string[] parts=artifacts.Substring(prefix.Length).Split(Path.DirectorySeparatorChar);
   if(parts.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(parts[0],"^[A-Za-z0-9_-]+$")||Array.IndexOf(new[]{"audio","reopen"},parts[1])<0||paths.SmokeScenario!="input"||!string.Equals(Path.GetFullPath(paths.BaseSaveDirectory),Path.Combine(prefix,parts[0],"session"),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Reveal audio fixture scope mismatch");
   Safe(paths.BaseSaveDirectory);
   byte[] bytes=new byte[257];int count=0;using(var file=File.OpenRead(marker)){int n;while(count<bytes.Length&&(n=file.Read(bytes,count,bytes.Length-count))>0)count+=n;}
   if(count>256)throw new ArgumentException("Reveal audio fixture byte cap");
   string raw=new System.Text.UTF8Encoding(false,true).GetString(bytes,0,count);
   string exact="{\"schemaVersion\":1,\"owner\":\"reveal-audio-player-fixture\",\"mode\":\""+parts[1]+"\"}";
   if(raw!=exact)throw new ArgumentException("Exact bounded BOM-free audio fixture required");return parts[1];
  }
  static void Safe(string path){string parent=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(parent)&&parent!=path)Safe(parent);try{if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Reparse fixture refused");}catch(FileNotFoundException){}catch(DirectoryNotFoundException){}}
  OpeningGame game;TabletopPresentation table;Phase4CollectionHost collections;UnityEngine.Object profile;
  readonly List<Check> checks=new List<Check>();readonly List<Playback> playback=new List<Playback>();readonly List<string> screenshots=new List<string>();readonly HashSet<string> branches=new HashSet<string>();
  string directory,mode,failure,initialSession;AudioClip[] pulls;AudioClip nonPokemon,pokemon,holo;
  void CheckThat(string name,bool ok,string detail=""){checks.Add(new Check{name=name,passed=ok,detail=detail});if(!ok)throw new InvalidOperationException(name+": "+detail);}
  bool Quiet(){foreach(var a in GetComponentsInChildren<AudioSource>(true))if(a.isPlaying)return false;return true;}
  AudioClip Field(string name){return (AudioClip)profile.GetType().GetField(name).GetValue(profile);}
  IEnumerator Start(){
   game=GetComponent<OpeningGame>();try{mode=ValidateOptIn(game==null?null:game.Paths);}catch(Exception e){Debug.LogError(e);Application.Quit(2);yield break;}
   if(mode==null)yield break;
   foreach(var c in GetComponents<MonoBehaviour>())if(c!=this&&Array.IndexOf(new[]{"Phase4SmokeProbe","MaterialsSmokeProbe","TiltSmokeProbe"},c.GetType().Name)>=0)c.enabled=false;
   directory=game.Paths.ArtifactDirectory;Application.runInBackground=true;
   if(mode=="legacy"){gameObject.AddComponent<Phase2SmokeRunner>();yield break;}
   yield return null;yield return null;
   table=GetComponent<TabletopPresentation>();collections=GetComponent<Phase4CollectionHost>();initialSession=SessionJson();
   yield return Guard(Run());
   var report=new Report{passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),playback=playback.ToArray(),mode=mode,failure=failure,initialSession=initialSession,finalSession=SessionJson(),generatedResultCount=game.GeneratedResultCount,totalVirtualCards=game.CurrentSession==null?-1:game.CurrentSession.TotalVirtualCards,screenshots=screenshots.ToArray(),rareBranches=new List<string>(branches).ToArray()};
   File.WriteAllText(Path.Combine(directory,"audio-report.json"),JsonUtility.ToJson(report,true));game.DispatchCommand("quit");Application.Quit(report.passed?0:2);
  }
  IEnumerator Guard(IEnumerator root){var stack=new Stack<IEnumerator>();stack.Push(root);while(stack.Count>0){object next=null;bool more=false;Exception error=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){failure=error.ToString();checks.Add(new Check{name="native-audio-failure",passed=false,detail=failure});Debug.LogError(error);yield break;}if(!more){stack.Pop();continue;}var nested=next as IEnumerator;if(nested!=null)stack.Push(nested);else yield return next;}}
  IEnumerator Run(){
   // The driver publishes this only after live-PID-guarded activation messages to our own HWND.
   string ready=Path.Combine(directory,"audio-driver-ready.json");float readyDeadline=Time.realtimeSinceStartup+25f;
   while(!File.Exists(ready)&&Time.realtimeSinceStartup<readyDeadline)yield return null;
   Safe(ready);CheckThat("owned-native-window-driver-ready-file",File.Exists(ready));
   string expectedReady="{\"owner\":\"reveal-audio-owned-window\",\"pid\":"+System.Diagnostics.Process.GetCurrentProcess().Id.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}";
   var readyBytes=new byte[129];int readyCount=0;using(var f=File.OpenRead(ready)){int n;while(readyCount<readyBytes.Length&&(n=f.Read(readyBytes,readyCount,readyBytes.Length-readyCount))>0)readyCount+=n;}
   CheckThat("owned-native-window-driver-ready",readyCount<=128&&new System.Text.UTF8Encoding(false,true).GetString(readyBytes,0,readyCount)==expectedReady);
   CheckThat("real-controller-collection-ready",game.CurrentSession!=null&&collections!=null&&collections.IsReady);
   CheckThat("startup-zero-generations",game.GeneratedResultCount==0);
   CheckThat("startup-child-sources-silent",Quiet());
   var hostType=Type.GetType("VintageCollector.Audio.RevealAudioHost, Assembly-CSharp");CheckThat("production-profile-host-type",hostType!=null);
   var host=GetComponent(hostType);CheckThat("production-host-attached",host!=null);
   profile=hostType.GetField("profile").GetValue(host) as UnityEngine.Object;CheckThat("production-host-profile-reference",profile!=null);
   pulls=(AudioClip[])profile.GetType().GetField("pullSounds").GetValue(profile);nonPokemon=Field("nonPokemonRare");pokemon=Field("pokemonRare");holo=Field("holoRare");
   var all=new List<AudioClip>(pulls);all.AddRange(new[]{nonPokemon,pokemon,holo});CheckThat("fourteen-distinct-imported-profile-clips",pulls.Length==11&&all.Count==14&&new HashSet<AudioClip>(all).Count==14&&!all.Contains(null));
   CheckThat("rare-position-eight",(int)profile.GetType().GetField("rareRevealPosition").GetValue(profile)==8);
   CheckThat("actual-provider-bound",typeof(TabletopPresentation).GetProperty("RevealSounds").GetValue(table,null)!=null);
   foreach(var clip in all){var samples=new float[clip.samples*clip.channels];CheckThat("imported-pcm-readable:"+clip.name,clip.samples>0&&clip.GetData(samples,0));float peak=0;foreach(float v in samples)peak=Math.Max(peak,Math.Abs(v));CheckThat("imported-pcm-nonzero:"+clip.name,peak>.001f,peak.ToString("R"));}
   if(mode=="reopen"){CheckThat("reload-no-generation-no-cue",game.GeneratedResultCount==0&&table.CommittedCueCount==0&&Quiet());yield return Capture("audio-reload");yield break;}
   CheckThat("fresh-isolated-audio-session",game.CurrentSession.Packs.Count==0&&game.CurrentSession.TotalVirtualCards==0);yield return Capture("audio-startup");
   if(game.CurrentView.paused)game.DispatchCommand("resume");
   // Frozen generator, actual authoritative acknowledgements. Branch discovered only from revealed metadata.
   for(uint seed=1;seed<=64&&branches.Count<3;seed++){
    game.GenerateForSmoke(seed);CheckThat("seed-generated:"+seed,game.CurrentView.stage=="sealed");game.DispatchCommand("open");CheckThat("actual-open:"+seed,game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==0);
    for(int position=1;position<=11;position++){
     int before=table.CommittedCueCount;
     if(seed==1&&position==1){game.FailNextCommitForSmoke();game.DispatchCommand("next");CheckThat("failed-save-no-cue",game.CurrentView.stage=="error"&&table.CommittedCueCount==before&&Quiet());yield return Capture("audio-save-error");game.DispatchCommand("retry");}
     else game.DispatchCommand("next");
     CheckThat("authoritative-reveal:"+seed+":"+position,game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==position);
     var card=game.CurrentView.cards[position-1];string branch=card.finish=="holo"?"holo":card.kind=="pokemon"?"pokemon":"non-pokemon";
     AudioClip expected=position==8?(branch=="holo"?holo:branch=="pokemon"?pokemon:nonPokemon):pulls[position-1];
     yield return ObservePlayback("position:"+position+":seed:"+seed+(position==8?":"+branch:""),expected,before);
     table.NotifyCommitted("next");CheckThat("duplicate-ack-consumed:"+seed+":"+position,table.CommittedCueCount==before+1);
     if(seed==1&&position==1){game.DispatchCommand("retry");CheckThat("retry-not-replayed",table.CommittedCueCount==before+1);}
     if(position==8){branches.Add(branch);yield return Capture("audio-rare-"+branch+"-seed-"+seed);}
    }
    int atResults=table.CommittedCueCount;yield return new WaitForSecondsRealtime(pulls[10].length+.08f);
    game.DispatchCommand("results");CheckThat("explicit-results-no-music:"+seed,game.CurrentView.stage=="results"&&table.CommittedCueCount==atResults&&Quiet());
    game.DispatchCommand("inspect:0");CheckThat("inspect-no-music:"+seed,game.CurrentView.inspectIndex==0&&table.CommittedCueCount==atResults&&Quiet());game.DispatchCommand("close-inspect");CheckThat("back-no-music:"+seed,game.CurrentView.inspectIndex==-1&&table.CommittedCueCount==atResults&&Quiet());
   }
   CheckThat("all-three-real-rare-branches",branches.SetEquals(new[]{"non-pokemon","pokemon","holo"}));
   yield return Capture("audio-results");
   // Clone only the new profile for fallback/assignability checks; never edit the imported asset.
   var copy=Instantiate(profile);var provider=typeof(TabletopPresentation).GetProperty("RevealSounds");
   foreach(string field in new[]{"nonPokemonRare","pokemonRare","holoRare"})copy.GetType().GetField(field).SetValue(copy,null);
   provider.SetValue(table,copy,null);CheckThat("provider-replacement-stops-native-source",Quiet());
   game.GenerateForSmoke(99u);game.DispatchCommand("open");for(int i=1;i<8;i++)game.DispatchCommand("next");
   int beforeFallback=table.CommittedCueCount;game.DispatchCommand("next");yield return ObservePlayback("position:8:missing-rare-fallback",pulls[7],beforeFallback);
   provider.SetValue(table,profile,null);Destroy(copy);game.DispatchCommand("skip");
   game.GenerateForSmoke(100u);game.DispatchCommand("open");game.DispatchCommand("volume:0");int muted=table.CommittedCueCount;
   game.DispatchCommand("next");CheckThat("muted-reveal-silent-consumed",game.CurrentView.revealedCount==1&&table.CommittedCueCount==muted&&Quiet());game.DispatchCommand("volume:0.5");table.NotifyCommitted("next");CheckThat("unmute-does-not-replay",table.CommittedCueCount==muted&&Quiet());
   int beforePause=table.CommittedCueCount;game.DispatchCommand("next");yield return ObservePlayback("before-pause",pulls[1],beforePause);
   game.DispatchCommand("pause");CheckThat("pause-stops-audio",game.CurrentView.paused&&Quiet());int paused=table.CommittedCueCount;game.DispatchCommand("next");CheckThat("paused-next-does-not-reveal-or-cue",game.CurrentView.revealedCount==2&&table.CommittedCueCount==paused&&Quiet());yield return Capture("audio-pause");game.DispatchCommand("resume");CheckThat("resume-no-replay",table.CommittedCueCount==paused&&Quiet());
   game.DispatchCommand("binder-open");CheckThat("binder-silent",table.CollectionInputBlocked&&Quiet());game.DispatchCommand("binder-close");CheckThat("binder-back-no-replay",table.CommittedCueCount==paused&&Quiet());
   int beforeSkip=table.CommittedCueCount;game.DispatchCommand("skip");CheckThat("skip-direct-results",game.CurrentView.stage=="results"&&game.CurrentView.revealedCount==11);yield return ObservePlayback("one-generic-skip-not-catchup",table.Media.Sound("skip"),beforeSkip);table.NotifyCommitted("skip");CheckThat("duplicate-skip-no-replay",table.CommittedCueCount==beforeSkip+1);
   CheckThat("once-only-eleven-awards-per-real-generated-pack",game.CurrentSession.TotalVirtualCards==game.GeneratedResultCount*11);
   yield return new WaitForSecondsRealtime(table.Media.Sound("skip").length+.08f);yield return Capture("audio-skipped");
  }
  IEnumerator ObservePlayback(string name,AudioClip expected,int before){
   var sources=GetComponentsInChildren<AudioSource>(true);CheckThat(name+":single-nonloop-child-source",sources.Length==1&&!sources[0].loop&&!sources[0].playOnAwake);
   var source=sources[0];var record=new Playback{name=name,expectedClip=expected==null?null:expected.name,actualClip=source.clip==null?null:source.clip.name,childSourcePlaying=source.isPlaying,samples=expected==null?0:expected.samples,frequency=expected==null?0:expected.frequency};
   CheckThat(name+":ack-once-source-playing-correct-clip",expected!=null&&source.clip==expected&&source.isPlaying&&table.CommittedCueCount==before+1);
   float until=Time.realtimeSinceStartup+.45f;var sourceSamples=new float[512];var mixed=new float[512];
   while(Time.realtimeSinceStartup<until&&(record.sourcePeak<.00001f||record.listenerPeak<.00001f)){
    yield return null;source.GetOutputData(sourceSamples,0);AudioListener.GetOutputData(mixed,0);foreach(float v in sourceSamples)record.sourcePeak=Math.Max(record.sourcePeak,Math.Abs(v));foreach(float v in mixed)record.listenerPeak=Math.Max(record.listenerPeak,Math.Abs(v));
   }
   playback.Add(record);CheckThat(name+":sampled-native-child-output-nonzero",record.sourcePeak>.00001f,record.sourcePeak.ToString("R"));CheckThat(name+":sampled-listener-mix-nonzero",record.listenerPeak>.00001f,record.listenerPeak.ToString("R"));
  }
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);try{texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());screenshots.Add(name+".png");}finally{Destroy(texture);}}
  string SessionJson(){if(game==null||game.CurrentSession==null)return null;var packs=new List<PackRecordData>();foreach(var p in game.CurrentSession.Packs)packs.Add(new PackRecordData{transactionId=p.TransactionId,createdUtc=p.CreatedUtc,modelId=p.ModelId,modelVersion=p.ModelVersion,catalogVersion=p.CatalogVersion,printingId=p.PrintingId,rngAlgorithm=p.RngAlgorithm,status=p.Status,seed=p.Seed,orderedCardIds=new List<string>(p.OrderedCardIds).ToArray(),newToVirtual=new List<bool>(p.NewToVirtual).ToArray(),revealedCount=p.RevealedCount,wrapperOpened=p.WrapperOpened});return JsonUtility.ToJson(new SessionData{schemaVersion=1,revision=game.CurrentSession.Revision,packs=packs.ToArray()});}
 }
}
