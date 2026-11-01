using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Presentation;
using VintageCollector.Persistence;
namespace VintageCollector.Verification {
 [DefaultExecutionOrder(-80)]
 public sealed class PerformanceSmokeProbe:MonoBehaviour {
  [Serializable] public sealed class Check {public string name;public bool passed;}
  [Serializable] public sealed class Pixel {public string role,id,rgbaSha256;public int width,height;public bool linear,sameNativeReference;}
  [Serializable] public sealed class Operation {public string command,transactionId;public double dispatchMs,ackLatencyMs,maxUpdateGapMs;public int framesDuringBusy,mainThreadId,workerThreadId;public bool savingTextObserved,timingLimited;}
  [Serializable] public sealed class Audio {public string clip;public bool playing;public float sourcePeak;public int mainThreadId;}
  [Serializable] public sealed class Report {public bool passed;public int pid,checkCount,pixelCount,screenshotCount,operationCount,generatedResultCount;public string mode,initialSessionJson,finalSessionJson,failure;public string[] acknowledgedSessionJson;public int snapshotCount;public Check[] checks;public Pixel[] pixels;public string[] screenshots;public Operation[] operations;public Audio[] audio;public int width,height;}
  OpeningGame game;Phase5MediaHost media;TabletopPresentation table;GamePaths paths;string mode,initial;
  readonly List<Check> checks=new List<Check>();readonly List<Pixel> pixels=new List<Pixel>();readonly List<string> shots=new List<string>();readonly List<Operation> operations=new List<Operation>();readonly List<Audio> audioEvidence=new List<Audio>();
  readonly List<string> acknowledgedSnapshots=new List<string>();
  bool saving,savingCaptureRequested;int sequence;int savingWidth,savingHeight;
  public static bool Acknowledged(bool busy,bool error,bool pending,int actual,int expected){return !busy&&!error&&!pending&&actual==expected;}
  static object Private(object owner,string field){if(owner==null)return null;return owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);}
  void Require(string name,bool ok){checks.Add(new Check{name=name,passed=ok});if(!ok)throw new InvalidOperationException(name);}
  IEnumerator Start(){
   game=GetComponent<OpeningGame>();if(game==null)yield break;
   try{paths=PerformanceBootTimingProbe.Admit(game.allowedProjectRoot,out mode);}catch(Exception e){Debug.LogError("Performance gameplay admission refused: "+e.Message);yield break;}
   if(paths==null)yield break;
   yield return Guard(Run());
  }
  IEnumerator Guard(IEnumerator root){var stack=new Stack<IEnumerator>();stack.Push(root);Exception failure=null;while(stack.Count>0){object value=null;bool more=false;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){failure=e;}if(failure!=null)break;if(!more){stack.Pop();continue;}var nested=value as IEnumerator;if(nested!=null)stack.Push(nested);else yield return value;}
   if(failure!=null){Finish(failure.ToString());game.RequestQuit();}
  }
  IEnumerator Run(){
   double until=Time.realtimeSinceStartupAsDouble+180;
   // Unconditional readiness; neither !IsRunning nor scene progress implies this gate.
   while(!StartupLoadingCoordinator.IsReady&&Time.realtimeSinceStartupAsDouble<until)yield return null;
   Require("startup-ready",StartupLoadingCoordinator.IsReady);
   string driver=Path.Combine(paths.ArtifactDirectory,"performance-driver-ready.json");until=Time.realtimeSinceStartupAsDouble+30;
   while(!File.Exists(driver)&&Time.realtimeSinceStartupAsDouble<until)yield return null;
   Require("owned-pid-handshake",PerformanceBootTimingProbe.ReadBounded(driver,256)=="{\"owner\":\"performance-owned-window\",\"pid\":"+System.Diagnostics.Process.GetCurrentProcess().Id+"}");
   media=GetComponent<Phase5MediaHost>();table=GetComponent<TabletopPresentation>();var host=GetComponent<Phase5CollectionHost>();
   Require("native-ready-no-runtime-decode",media!=null&&media.IsReady&&media.NativeBaseProvider!=null&&media.Library==null&&host!=null&&host.IsReady&&table!=null&&table.enabled&&ReferenceEquals(table.Media,media.Adapter)&&GetComponent<SetSelectorPresentation>().enabled&&game.backgroundOpeningStorage);
   table.TextObserver=text=>{if(text!=null&&text.Contains("Saving...")&&Event.current.type==EventType.Repaint&&game.IsStorageBusy){saving=true;if(!savingCaptureRequested){savingCaptureRequested=true;savingWidth=Screen.width;savingHeight=Screen.height;string file=Path.Combine(paths.ArtifactDirectory,"saving.png");PerformanceBootTimingProbe.NoReparse(file);ScreenCapture.CaptureScreenshot(file);}}};
   game.ShutdownCompleted+=delegate{PerformanceBootTimingProbe.Write(Path.Combine(paths.ArtifactDirectory,"shutdown.json"),"{\"released\":"+((table.Media==null&&table.RevealSounds==null)?"true":"false")+",\"workerJoined\":"+((Private(game,"storageWorker")==null&&!game.IsStorageBusy)?"true":"false")+"}");};
   initial=SessionJson();acknowledgedSnapshots.Add(initial);Require("no-startup-generation",game.GeneratedResultCount==0&&!game.IsStorageBusy);
   if(game.CurrentView.paused)game.DispatchCommand("resume");
   var configs=game.GetPhase5PresentationConfiguration();Require("three-configs",configs.Length==3);
   if(mode=="reopen"){
    Require("reopen-nine-packs",game.CurrentSession.Packs.Count==9&&game.CurrentSession.TotalVirtualCards==99);yield return Capture("reopened");
   }else{
    Require("fresh-empty",game.CurrentSession.Packs.Count==0);
    for(int set=0;set<3;set++)for(int variant=0;variant<3;variant++){
     int count=set*3+variant+1;string printing=configs[set].printingId;Require("select-"+set+"-"+variant,game.TrySelectSet(printing));string tx=Transaction(printing,variant);uint seed=(uint)(100+set*3+variant);
     yield return Commit("generate",()=>game.GenerateForSmoke(seed,tx),()=>game.CurrentSession.Packs.Count==count&&game.CurrentSession.CurrentPack.TransactionId==tx&&game.CurrentSession.CurrentPack.Seed==seed&&game.CurrentView.stage=="sealed",false);
     AddPixel("wrapper",printing+":"+variant,table.Media.WrapperFront,set==0?media.nativeBaseMediaProfile.wrapperFronts[variant]:configs[set].wrapperFronts[variant]);yield return Capture("wrapper-"+set+"-"+variant);
     yield return Commit("open",()=>game.DispatchCommand("open"),()=>game.CurrentSession.CurrentPack.WrapperOpened&&game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==0,false);
     if(set==0&&variant==0)yield return Capture("first-pack");
     if(variant==0){
      for(int pos=1;pos<=11;pos++){
       int target=pos;
       if(set==0&&pos==1){
        object pending;game.FailNextCommitForSmoke();yield return Commit("next-fault",()=>game.DispatchCommand("next"),()=>game.CurrentView.stage=="error",true);pending=Private(game,"pending");
        Require("fault-retains-transaction",pending!=null&&game.CurrentSession.CurrentPack.TransactionId==tx&&game.CurrentSession.CurrentPack.Seed==seed&&game.CurrentSession.CurrentPack.RevealedCount==0);
        yield return Commit("retry",()=>game.DispatchCommand("retry"),()=>game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==target,false);
        Require("retry-exact-once",game.CurrentSession.Packs.Count==count&&game.CurrentSession.CurrentPack.TransactionId==tx&&game.CurrentSession.CurrentPack.Seed==seed&&Private(game,"pending")==null);
       }else yield return Commit("next",()=>game.DispatchCommand("next"),()=>game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==target,false);
       if(pos==2)ObserveAudio(configs[set].revealAudioProfile.pullSounds[1]);
      }
      Require("card11-hold-"+set,game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==11);yield return Capture("card11-"+set);
      yield return Commit("results",()=>game.DispatchCommand("results"),()=>game.CurrentView.stage=="results",false);
      game.DispatchCommand("inspect:0");Require("inspect-"+set,game.CurrentView.inspectIndex==0);yield return Capture("inspect-"+set);game.DispatchCommand("close-inspect");Require("close-inspect-"+set,game.CurrentView.inspectIndex==-1);
     }else yield return Commit("skip",()=>game.DispatchCommand("skip"),()=>game.CurrentView.stage=="results"&&game.CurrentView.revealedCount==11,false);
     Require("awards-"+set+"-"+variant,game.CurrentSession.TotalVirtualCards==count*11&&game.GeneratedResultCount==count);
    }
    yield return Capture("complete-nine-packs");
   }
   // Full native readback is deliberately AFTER first-pack timing, not hidden startup work.
   var profile=media.nativeBaseMediaProfile;
   foreach(var row in profile.cards){AddPixel("face",row.canonicalId,media.Adapter.CardFace(row.canonicalId),row.face);if(row.mask!=null)AddPixel("mask",row.canonicalId,media.Adapter.HoloMask(row.canonicalId),row.mask);else Require("nonholo-mask-"+row.canonicalId,media.Adapter.HoloMask(row.canonicalId)==null);yield return null;}
   foreach(var supplied in media.suppliedCardArtProfiles)foreach(var row in supplied.cards){AddPixel("face",row.cardId,media.Adapter.CardFace(row.cardId),row.face);Require("fullcard-no-mask-"+row.cardId,media.Adapter.HoloMask(row.cardId)==null);yield return null;}
   foreach(var config in configs){media.Adapter.SetContext(config.printingId,null);AddPixel("back",config.printingId,media.Adapter.CardBack,profile.sharedCardBack);}
   media.Adapter.SetContext("unknown",null);Require("unknown-back-failclosed",media.Adapter.CardBack==null);
   media.Adapter.SetContext(game.SelectedPrintingId,game.CurrentView.transactionId);
   Require("all227-faces",pixels.FindAll(p=>p.role=="face").Count==227);Require("base15-masks",pixels.FindAll(p=>p.role=="mask").Count==15);
   if(mode=="reopen")Require("reopen-no-mutation",SessionJson()==initial&&game.GeneratedResultCount==0);
   if(savingCaptureRequested){string file=Path.Combine(paths.ArtifactDirectory,"saving.png");Require("saving-framebuffer",PerformanceBootTimingProbe.CompletePng(file,savingWidth,savingHeight));shots.Add("saving.png");}
   Finish(null);game.RequestQuit();
  }
  IEnumerator Commit(string command,Action dispatch,Func<bool> post,bool expectedFault){
   Require("idle-before-"+(sequence++),!game.IsStorageBusy&&StartupLoadingCoordinator.IsReady);
   var op=new Operation{command=command,mainThreadId=Thread.CurrentThread.ManagedThreadId};string before=SessionJson();object pendingBefore=Private(game,"pending");int cue=table.CommittedCueCount;string selected=game.SelectedPrintingId;
   double start=Time.realtimeSinceStartupAsDouble;saving=false;dispatch();op.dispatchMs=(Time.realtimeSinceStartupAsDouble-start)*1000;
   bool queued=game.IsStorageBusy;
   if(queued){if(command=="retry")Require("retry-identical-pending",ReferenceEquals(pendingBefore,Private(game,"pending")));Require("busy-no-early-"+sequence,SessionJson()==before&&table.CommittedCueCount==cue);game.GenerateForSmoke(999);game.DispatchCommand("next");Require("busy-selector-"+sequence,!game.TrySelectSet("unknown")&&game.SelectedPrintingId==selected);Require("busy-duplicate-"+sequence,SessionJson()==before&&table.CommittedCueCount==cue);}
   double last=Time.realtimeSinceStartupAsDouble,until=start+30;
   while(game.IsStorageBusy&&Time.realtimeSinceStartupAsDouble<until){op.framesDuringBusy++;double now=Time.realtimeSinceStartupAsDouble;op.maxUpdateGapMs=Math.Max(op.maxUpdateGapMs,(now-last)*1000);last=now;Require("busy-no-early-frame-"+sequence+"-"+op.framesDuringBusy,SessionJson()==before&&table.CommittedCueCount==cue);yield return null;}
   op.maxUpdateGapMs=Math.Max(op.maxUpdateGapMs,(Time.realtimeSinceStartupAsDouble-last)*1000);op.ackLatencyMs=(Time.realtimeSinceStartupAsDouble-start)*1000;op.savingTextObserved=saving;op.timingLimited=op.framesDuringBusy<2||!saving;
   object worker=Private(game,"storageWorker");var thread=worker==null?null:Private(worker,"thread") as Thread;op.workerThreadId=thread==null?0:thread.ManagedThreadId;op.transactionId=game.CurrentView.transactionId;operations.Add(op);
   Require("ack-"+sequence,expectedFault?!game.IsStorageBusy&&game.CurrentView.stage=="error"&&game.HasPendingStorage&&post():Acknowledged(game.IsStorageBusy,game.CurrentView.stage=="error",game.HasPendingStorage,post()?1:0,1));
   if(queued)Require("actual-worker-"+sequence,op.workerThreadId>0&&op.workerThreadId!=op.mainThreadId);
   acknowledgedSnapshots.Add(SessionJson());
   yield return null;
  }
  void ObserveAudio(AudioClip expected){bool found=false;foreach(var source in table.GetComponentsInChildren<AudioSource>(true)){if(source.clip==null||!source.isPlaying)continue;float[] samples=new float[512];source.GetOutputData(samples,0);float peak=0;foreach(float f in samples)peak=Math.Max(peak,Math.Abs(f));found|=ReferenceEquals(source.clip,expected);audioEvidence.Add(new Audio{clip=source.clip.name,playing=source.isPlaying,sourcePeak=peak,mainThreadId=Thread.CurrentThread.ManagedThreadId});}Require("native-audio-clip-"+game.SelectedPrintingId,found);}
  void AddPixel(string role,string id,Texture2D actual,Texture2D expected){Require("native-ref-"+role+"-"+id,actual!=null&&ReferenceEquals(actual,expected));var p=actual.GetPixels32();byte[] bytes=new byte[p.Length*4];for(int y=0;y<actual.height;y++)for(int x=0;x<actual.width;x++){var c=p[(actual.height-1-y)*actual.width+x];int n=(y*actual.width+x)*4;bytes[n]=c.r;bytes[n+1]=c.g;bytes[n+2]=c.b;bytes[n+3]=c.a;}string hash;using(var h=SHA256.Create())hash=BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();pixels.Add(new Pixel{role=role,id=id,rgbaSha256=hash,width=actual.width,height=actual.height,linear=!actual.isDataSRGB,sameNativeReference=true});}
  static string Transaction(string printing,int variant){for(int n=0;n<1000;n++){string id;using(var h=SHA256.Create()){var digest=h.ComputeHash(System.Text.Encoding.UTF8.GetBytes("performance:"+printing+":"+variant+":"+n));byte[] b=new byte[16];Array.Copy(digest,b,16);id=new Guid(b).ToString("N");}if(WrapperVariant.ForTransaction(id)==variant)return id;}throw new InvalidOperationException("Fixture transaction search");}
  string SessionJson(){var packs=new List<PackRecordData>();foreach(var p in game.CurrentSession.Packs)packs.Add(new PackRecordData{transactionId=p.TransactionId,createdUtc=p.CreatedUtc,modelId=p.ModelId,modelVersion=p.ModelVersion,catalogVersion=p.CatalogVersion,printingId=p.PrintingId,rngAlgorithm=p.RngAlgorithm,status=p.Status,seed=p.Seed,orderedCardIds=new List<string>(p.OrderedCardIds).ToArray(),newToVirtual=new List<bool>(p.NewToVirtual).ToArray(),revealedCount=p.RevealedCount,wrapperOpened=p.WrapperOpened});return JsonUtility.ToJson(new SessionData{schemaVersion=1,revision=game.CurrentSession.Revision,packs=packs.ToArray()});}
  IEnumerator Capture(string name){Require("capture-ready-"+name,StartupLoadingCoordinator.IsReady&&!game.IsStorageBusy);yield return new WaitForEndOfFrame();int w=Screen.width,h=Screen.height;string file=Path.Combine(paths.ArtifactDirectory,name+".png");PerformanceBootTimingProbe.NoReparse(file);ScreenCapture.CaptureScreenshot(file);double until=Time.realtimeSinceStartupAsDouble+8;while(!PerformanceBootTimingProbe.CompletePng(file,w,h)&&Time.realtimeSinceStartupAsDouble<until)yield return null;Require("framebuffer-"+name,PerformanceBootTimingProbe.CompletePng(file,w,h));shots.Add(name+".png");}
  void Finish(string failure){var r=new Report{passed=failure==null&&checks.Count>0,mode=mode,pid=System.Diagnostics.Process.GetCurrentProcess().Id,acknowledgedSessionJson=acknowledgedSnapshots.ToArray(),snapshotCount=acknowledgedSnapshots.Count,initialSessionJson=initial,finalSessionJson=game.CurrentSession==null?null:SessionJson(),failure=failure,checks=checks.ToArray(),checkCount=checks.Count,pixels=pixels.ToArray(),pixelCount=pixels.Count,screenshots=shots.ToArray(),screenshotCount=shots.Count,operations=operations.ToArray(),operationCount=operations.Count,audio=audioEvidence.ToArray(),width=Screen.width,height=Screen.height,generatedResultCount=game.GeneratedResultCount};PerformanceBootTimingProbe.Write(Path.Combine(paths.ArtifactDirectory,"report.json"),JsonUtility.ToJson(r,true));}
  void OnDestroy(){if(table!=null)table.TextObserver=null;}
 }
}
