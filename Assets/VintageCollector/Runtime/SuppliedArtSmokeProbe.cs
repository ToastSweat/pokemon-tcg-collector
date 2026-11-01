using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Presentation;
using VintageCollector.Persistence;

namespace VintageCollector.Verification
{
    // Production bootstrap only. Inert unless BOTH validated GamePaths smoke mode AND exact owned fixture file agree.
    [DefaultExecutionOrder(-90)]
    public sealed class SuppliedArtSmokeProbe : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string name;public bool passed;public string detail;}
        [Serializable] public sealed class Control {public string name,source;public int renderFrame=-1;public float x,y,width,height;public bool enabled;}
        [Serializable] public sealed class Pin {public string name,sha256;}
        [Serializable] public sealed class Probe {
            public string stage,profileId,sessionJson,capture,lastCommand,hostStatus,fixtureAck,controlsViewJson,controlsSource;
            public string[] physicalLabels,cumulativeRenderedLabels;
            public bool mediaBound;public string selectedPrintingId,activePrintingId;public int wrapperIndex,committedCueCount;
            public long revision,physicalRevision;
            public int generatedResultCount,totalVirtualCards,revealedCount,inspectIndex,screenWidth,screenHeight;
            public bool paused,ready,selectorControlsObserved;
            public BinderView binder;
            public Control[] controls;
            public Pin[] profilePins;
        }
        [Serializable] public sealed class WrapperEvidence {public string printingId,transactionId,actualTextureName,expectedTextureName,rgbaSha256;public uint seed;public int index;public bool sameNativeReference;}
        readonly List<WrapperEvidence> wrappers=new List<WrapperEvidence>();
        [Serializable] public sealed class Playback {public string name,printingId,expectedClip,actualClip;public bool childSourcePlaying;public float sourcePeak,listenerPeak;public int samples,frequency;}
        readonly List<Playback> playback=new List<Playback>();readonly HashSet<string> rareBranches=new HashSet<string>();
        [Serializable] public sealed class Report {
            public FaceEvidence[] faces;public long nativeAllocatedBytes;public Playback[] playback;public string[] rareBranches;public WrapperEvidence[] wrappers;public int wrapperCount;
            public bool passed;public int checkCount,seedCount,snapshotCount,screenshotCount;public uint[] seeds;public string mode;public Check[] checks;public string[] screenshots;public Probe initial,final;public Probe[] snapshots;
        }
        private HostFacade host;private OpeningGame game;private GamePaths paths;
        private BinderPresentation binder;
        private TabletopPresentation table;
        private string fixtureAck="";
        private readonly HashSet<string> cumulativeRenderedLabels=new HashSet<string>(StringComparer.Ordinal);
        private bool active,finished,capturing;private string mode,lastObservation;
        private Probe initial;
        private object stableMedia,stableReveal;
        private readonly List<Check> checks=new List<Check>();
        private readonly List<string> screenshots=new List<string>();
        private readonly Dictionary<string,Control> binderControls=new Dictionary<string,Control>(StringComparer.Ordinal);
        private BinderView controlsView;
        private string controlsViewJson;
        private int controlsFrame=-1,controlsWidth,controlsHeight;
        private bool controlsComplete;
        private readonly string[] authoritative={"state.save","state.backup","state.anchor","physical.save","physical.backup","physical.anchor"};
        private IEnumerator Start() {
            game=GetComponent<OpeningGame>();if(game==null)yield break;
            // Resolve flags without taking store action. An opted fixture can wait for the real owners;
            // ordinary startup remains inert, including while a host is temporarily not initialized.
            try{paths=GamePaths.Resolve(game.allowedProjectRoot,Application.dataPath,Application.isEditor,Environment.GetCommandLineArgs());}
            catch(Exception e){Debug.LogError("Phase5 smoke path admission: "+e.Message);yield break;}
            if(!paths.IsSmokeTest)yield break;
            if(!File.Exists(Path.Combine(paths.ArtifactDirectory,"supplied-art-fixture.json")))yield break;
            try {mode=ValidateFixture(paths);}
            catch(Exception e) {Debug.LogError("Phase 5 fixture rejected: "+e.Message);Application.Quit(2);yield break;}
            var hostComponent=GetComponent(Type.GetType("VintageCollector.Runtime.Phase5CollectionHost, Assembly-CSharp"));host=new HostFacade(hostComponent);
            var mediaComponent=GetComponent(Type.GetType("VintageCollector.Runtime.Phase5MediaHost, Assembly-CSharp"));
            foreach(var c in GetComponents<MonoBehaviour>())if(c!=this && Array.IndexOf(new[]{"Phase4SmokeProbe","MaterialsSmokeProbe","TiltSmokeProbe","RevealAudioSmokeProbe"},c.GetType().Name)>=0)c.enabled=false;
            float readinessDeadline=Time.realtimeSinceStartup+30f;
            while((game.Paths==null||!host.IsReady||Get(mediaComponent,"Adapter")==null)&&Time.realtimeSinceStartup<readinessDeadline)yield return null;
            if(game.Paths==null||!host.IsReady||Get(mediaComponent,"Adapter")==null){Debug.LogError("Phase5 opted fixture real host/media readiness deadline");Application.Quit(2);yield break;}
            paths=game.Paths;ValidateFixture(paths);
            CheckThat("actual-ready-after-ordered-hosts",host.IsReady&&ReferenceEquals(GetComponent<TabletopPresentation>().Media,Get(mediaComponent,"Adapter")),"Collection(-200), media(-100), probe(-90); no fabricated ready state");
            active=true;Application.runInBackground=true;binder=GetComponent<BinderPresentation>();
            if(binder!=null)binder.ControlObserver=ObserveControl;
            table=GetComponent<TabletopPresentation>();
            if(table!=null)table.TextObserver=text=>cumulativeRenderedLabels.Add(text);
            stableMedia=table.Media;stableReveal=table.RevealSounds;initial=Snapshot();
            game.ShutdownCompleted+=delegate {bool alive=true;foreach(var profile in GetComponent<Phase5MediaHost>().suppliedCardArtProfiles)foreach(var row in profile.cards)alive&=row.face!=null;AtomicText(Path.Combine(paths.ArtifactDirectory,"shutdown.json"),"{\"released\":"+(table.Media==null&&table.RevealSounds==null?"true":"false")+",\"borrowedAlive\":"+(alive?"true":"false")+"}");};
            yield return Guard(Run());
            Finish(true);
        }
        private static string ValidateFixture(GamePaths paths) {
            // The approved root is fixed; the controller has already validated its smoke paths.
            string root=Path.GetFullPath("F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/.artifacts/phase5-supplied-art-player");
            string artifacts=Path.GetFullPath(paths.ArtifactDirectory),save=Path.GetFullPath(paths.BaseSaveDirectory);
            if(!artifacts.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Wrong fixture root");
            string[] segments=artifacts.Substring(root.Length+1).Split(Path.DirectorySeparatorChar);
            if(segments.Length!=2 || !System.Text.RegularExpressions.Regex.IsMatch(segments[0],"^[A-Za-z0-9_-]+$") ||
                (Array.IndexOf(new[]{"art","reopen"},segments[1])<0))throw new ArgumentException("Wrong owned fixture directory");
            string expectedSave=Path.Combine(root,segments[0],"session");
            if(!string.Equals(save,expectedSave,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Fixture save is not the owned run session");
            CheckAncestors(artifacts);CheckAncestors(save);
            string file=Path.Combine(artifacts,"supplied-art-fixture.json");CheckAncestors(file);
            string exact="{\"schemaVersion\":1,\"owner\":\"supplied-art-player\",\"mode\":\""+segments[1]+"\"}";
            ReadExactFixture(file,256,exact);
            if(paths.SmokeScenario!="input")throw new ArgumentException("Phase 5 fixture requires supported input smoke mode");
            return segments[1];
        }
        private static string ReadExactFixture(string path,int cap,params string[] expected) {
            // One handle, at most cap+1 bytes: no size-check/reopen race or unbounded ReadAllText.
            byte[] bytes=new byte[cap+1];int count=0;
            using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                while(count<bytes.Length) {int n=file.Read(bytes,count,bytes.Length-count);if(n==0)break;count+=n;}
            }
            if(count>cap)throw new ArgumentException("Fixture exceeds byte bound");
            if(count>=3 && bytes[0]==239 && bytes[1]==187 && bytes[2]==191)throw new ArgumentException("Fixture BOM refused");
            string text;
            try {text=new System.Text.UTF8Encoding(false,true).GetString(bytes,0,count);}
            catch(System.Text.DecoderFallbackException e) {throw new ArgumentException("Fixture must be exact UTF8",e);}
            foreach(string item in expected)if(string.Equals(text,item,StringComparison.Ordinal))return text;
            throw new ArgumentException("Fixture must match exact bounded versioned contract");
        }
        private static void CheckAncestors(string path) {
            string parent=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(parent) && parent!=path)CheckAncestors(parent);
            try {if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Fixture reparse path refused");}
            catch(FileNotFoundException){}catch(DirectoryNotFoundException){}
        }
        private IEnumerator Guard(IEnumerator root) {
            var stack=new Stack<IEnumerator>();stack.Push(root);
            while(stack.Count>0) {
                object next=null;bool more=false;Exception failure=null;
                try {more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null) {checks.Add(new Check {name="probe-failure",passed=false,detail=failure.ToString()});yield break;}
                if(!more){stack.Pop();continue;}var nested=next as IEnumerator;if(nested!=null)stack.Push(nested);else yield return next;
            }
        }
        static object Get(object target,string name) {if(target==null)return null;var p=target.GetType().GetProperty(name);if(p!=null)return p.GetValue(target,null);var f=target.GetType().GetField(name);return f==null?null:f.GetValue(target);}
        static object Call(object target,string name,params object[] args) {return target.GetType().GetMethod(name).Invoke(target,args);}
        sealed class HostFacade {
            readonly Component value;public HostFacade(Component c){value=c;}
            public bool IsReady {get{return value!=null && (bool)Get(value,"IsReady");}}
            public string StatusMessage {get{return (string)Get(value,"StatusMessage");}}
            public BinderView CurrentBinderView {get{return (BinderView)Get(value,"CurrentBinderView");}}
            public VintageCollector.Collections.PhysicalSnapshot Physical {get{return (VintageCollector.Collections.PhysicalSnapshot)Get(value,"Physical");}}
            public VintageCollector.Collections.CollectionProfiles Profiles {get{return (VintageCollector.Collections.CollectionProfiles)Get(value,"Profiles");}}
            public void DispatchBinderCommand(string c){Call(value,"DispatchBinderCommand",c);}
            public void FailNextPhysicalCommitForSmoke(){Call(value,"FailNextPhysicalCommitForSmoke");}
            public void FailNextImportForSmoke(){Call(value,"FailNextImportForSmoke");}
        }
        static int Variant(string transaction){var t=Type.GetType("VintageCollector.Presentation.WrapperVariant, Assembly-CSharp");if(t==null)throw new InvalidOperationException("Frozen WrapperVariant missing");return (int)t.GetMethod("ForTransaction").Invoke(null,new object[]{transaction});}
        string FixtureTransaction(string printing,int index){for(int n=0;n<1000;n++){byte[] bytes=System.Text.Encoding.UTF8.GetBytes("phase5-fixture:"+printing+":"+index+":"+n);string id;using(var h=SHA256.Create()){byte[] digest=h.ComputeHash(bytes),guid=new byte[16];Array.Copy(digest,guid,16);id=new Guid(guid).ToString("N");}if(Variant(id)==index)return id;}throw new InvalidOperationException("No deterministic fixture transaction");}
        void Generate(uint seed,string transaction){var m=game.GetType().GetMethod("GenerateForSmoke",new[]{typeof(uint),typeof(string)});if(m==null)throw new InvalidOperationException("Parent-authorized exact-transaction smoke overload missing");m.Invoke(game,new object[]{seed,transaction});}
        [Serializable] public sealed class FaceEvidence {public string cardId,rgbaSha256;}
        readonly List<FaceEvidence> faces=new List<FaceEvidence>();
        static string Pixels(Texture2D t) {
            var p=t.GetPixels32();var bytes=new byte[p.Length*4];
            for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++){var c=p[(t.height-1-y)*t.width+x];int n=(y*t.width+x)*4;bytes[n]=c.r;bytes[n+1]=c.g;bytes[n+2]=c.b;bytes[n+3]=c.a;}
            using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
        }
        IEnumerator Run() {
            string driver=Path.Combine(paths.ArtifactDirectory,"supplied-art-driver-ready.json");float until=Time.realtimeSinceStartup+30f;while(!File.Exists(driver)&&Time.realtimeSinceStartup<until)yield return null;CheckAncestors(driver);ReadExactFixture(driver,128,"{\"owner\":\"supplied-art-owned-window\",\"pid\":"+System.Diagnostics.Process.GetCurrentProcess().Id+"}");
            CheckThat("actual-host-ready",host.IsReady&&game.GeneratedResultCount==0&&Snapshot().mediaBound,"Production hosts, startup never generates");
            var media=GetComponent<Phase5MediaHost>();var configs=game.GetPhase5PresentationConfiguration();
            foreach(var profile in media.suppliedCardArtProfiles)foreach(var row in profile.cards){
                CheckThat("native-face:"+row.cardId,row.face!=null&&row.face.width==600&&row.face.height==825&&media.Adapter.CardFace(row.cardId)==row.face&&media.Adapter.HoloMask(row.cardId)==null,"Real borrowed full face, no overlay mask");
                media.Adapter.SetContext("base-en-unlimited1999",null);CheckThat("cross-context:"+row.cardId,media.Adapter.CardFace(row.cardId)==row.face,"Card identity, not selected context");faces.Add(new FaceEvidence{cardId=row.cardId,rgbaSha256=Pixels(row.face)});yield return null;
            }
            CheckThat("all126-native-faces",faces.Count==126,"Pixel hashes independently compared by driver");
            var fallback=new VintageCollector.Media.Phase5PresentationMedia(media.Library,configs,id=>game.ApprovedSets.FindCard(id).PrintingId);
            foreach(var profile in media.suppliedCardArtProfiles){var row=profile.cards[0];CheckThat("missing-face-fallback:"+profile.printingId,fallback.CardFace(row.cardId)==null&&fallback.CardTemplate(row.cardId)!=null,"Optional missing profile preserves generic template");}
            var malformed=ScriptableObject.CreateInstance<VintageCollector.Media.SuppliedCardArtProfile>();malformed.printingId="invalid";bool refused=false;try{VintageCollector.Media.SuppliedCardArtMap.Create(new[]{malformed},id=>game.ApprovedSets.FindCard(id).PrintingId);}catch(ArgumentException){refused=true;}finally{Destroy(malformed);}CheckThat("invalid-optional-profile-rejected",refused&&table.RevealSounds==media.Adapter,"Live audio binding unaffected");
            foreach(var config in configs){var p=config.revealAudioProfile;var clips=new List<AudioClip>(p.pullSounds);clips.AddRange(new[]{p.nonPokemonRare,p.pokemonRare,p.holoRare});CheckThat("fourteen-audio-bindings:"+config.printingId,clips.Count==14&&new HashSet<AudioClip>(clips).Count==14&&!clips.Contains(null),"Unchanged assigned originals; not full playlist retest");}
            if(game.CurrentView.paused)game.DispatchCommand("resume");
            if(mode=="reopen"){CheckThat("reload-six-packs",game.GeneratedResultCount==0&&game.CurrentSession.Packs.Count==6&&game.CurrentSession.TotalVirtualCards==66,"Separate process, no new awards");yield return Capture("reopened");yield break;}
            CheckThat("fresh-empty-session",game.CurrentSession.Packs.Count==0,"Owned fixture");
            for(int set=1;set<3;set++)for(int variant=0;variant<3;variant++){
                string printing=configs[set].printingId;CheckThat("select:"+set+":"+variant,game.TrySelectSet(printing),"Production selector gate");
                string tx=FixtureTransaction(printing,variant);Generate((uint)(100+set*3+variant),tx);yield return null;
                var actual=table.Media.WrapperFront;var expected=configs[set].wrapperFronts[variant];bool same=actual!=null&&actual==expected;
                wrappers.Add(new WrapperEvidence{printingId=printing,transactionId=tx,index=variant,seed=game.CurrentSession.CurrentPack.Seed,sameNativeReference=same,rgbaSha256=actual==null?null:Pixels(actual),actualTextureName=actual==null?null:actual.name,expectedTextureName=expected.name});CheckThat("wrapper:"+set+":"+variant,same,"Production transaction variant, no RNG change");yield return Capture("wrapper-"+set+"-"+variant);
                game.DispatchCommand("open");
                if(variant==0){for(int pos=1;pos<=11;pos++){int cue=table.CommittedCueCount;game.DispatchCommand("next");CheckThat("reveal:"+set+":"+pos,game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==pos,"Actual commit");if(pos==2)yield return ObservePlayback("ordinary-"+set,printing,configs[set].revealAudioProfile.pullSounds[1],cue);else yield return null;}
                    CheckThat("card11-hold:"+set,game.CurrentView.stage=="revealing"&&game.CurrentView.revealedCount==11,"Results requires explicit command");yield return Capture("card11-"+set);game.DispatchCommand("results");game.DispatchCommand("inspect:0");yield return Capture("inspect-"+set);game.DispatchCommand("close-inspect");
                }else game.DispatchCommand("skip");
                CheckThat("results:"+set+":"+variant,game.CurrentView.stage=="results"&&game.CurrentSession.TotalVirtualCards==game.GeneratedResultCount*11,"No fabricated awards");
            }
            yield return Capture("complete-six-packs");
        }
        IEnumerator RunAudio(){
            string[] ids={"base-en-unlimited1999","jungle-en-unlimited1999","fossil-en-unlimited1999"};var configs=(Array)Get(game,"phase5Sets");CheckThat("audio-three-set-configs",configs!=null&&configs.Length==3,"Actual native scene config");
            for(int set=0;set<3;set++){
                string printing=ids[set];Call(game,"TrySelectSet",printing);var profile=(VintageCollector.Audio.RevealAudioProfile)Get(configs.GetValue(set),"revealAudioProfile");var all=new List<AudioClip>(profile.pullSounds);all.AddRange(new[]{profile.nonPokemonRare,profile.pokemonRare,profile.holoRare});CheckThat("fourteen-assigned-clips:"+printing,all.Count==14&&new HashSet<AudioClip>(all).Count==14&&!all.Contains(null),"Borrowed originals");var found=new HashSet<string>();
                for(uint seed=1;seed<=64&&found.Count<(set==0?3:2);seed++){
                    Generate(seed,FixtureTransaction(printing+":audio:"+seed,(int)(seed%3)));game.DispatchCommand("open");
                    for(int position=1;position<=11;position++){
                        int before=table.CommittedCueCount;game.DispatchCommand("next");var card=game.CurrentView.cards[position-1];string branch=card.finish=="holo"?"holo":card.kind=="pokemon"?"pokemon":"non-pokemon";AudioClip expected=profile.RevealSound(position,card.kind,card.finish);yield return ObservePlayback("position:"+position+":seed:"+seed,printing,expected,before);if(position==8){found.Add(branch);rareBranches.Add(branch);}
                        table.NotifyCommitted("next");CheckThat("audio-dedup:"+set+":"+seed+":"+position,table.CommittedCueCount==before+1,"Actual duplicate ack suppressed");
                    }
                    game.DispatchCommand("results");var source=GetComponentsInChildren<AudioSource>(true)[0];bool playing=source.isPlaying;AudioClip current=source.clip;int cues=table.CommittedCueCount;Call(game,"TrySelectSet",ids[(set+1)%3]);CheckThat("selection-keeps-current-music:"+set+":"+seed,ReferenceEquals(table.RevealSounds,stableReveal)&&source.clip==current&&(!playing||source.isPlaying)&&table.CommittedCueCount==cues,"Next selection does not change active context/stop cue");Call(game,"TrySelectSet",printing);
                }
                CheckThat("reachable-rare-branches:"+printing,found.Contains("pokemon")&&found.Contains("holo")&&(set!=0||found.Contains("non-pokemon")),"Only revealed metadata classifies a cue");yield return Capture("native-audio-set-"+set);
            }
            CheckThat("all-three-rare-categories",rareBranches.SetEquals(new[]{"non-pokemon","pokemon","holo"}),"Real generated, acknowledged cards");
            // Explicit provider-replacement experiment only: clone profile, never mutate/destroy borrowed assets.
            Call(game,"TrySelectSet",ids[0]);var original=(VintageCollector.Audio.RevealAudioProfile)Get(configs.GetValue(0),"revealAudioProfile");var copy=Instantiate(original);copy.nonPokemonRare=null;copy.pokemonRare=null;copy.holoRare=null;table.RevealSounds=copy;
            try{Generate(99,FixtureTransaction("audio-fallback",0));game.DispatchCommand("open");for(int p=1;p<8;p++)game.DispatchCommand("next");int before=table.CommittedCueCount;game.DispatchCommand("next");yield return ObservePlayback("position:8:missing-rare-fallback",ids[0],original.pullSounds[7],before);game.DispatchCommand("skip");}finally{table.RevealSounds=(VintageCollector.Media.IRevealSoundMedia)stableReveal;Destroy(copy);}
            Generate(100,FixtureTransaction("audio-suppression",1));game.DispatchCommand("open");game.DispatchCommand("volume:0");int muted=table.CommittedCueCount;game.DispatchCommand("next");game.DispatchCommand("volume:0.5");table.NotifyCommitted("next");CheckThat("unmute-no-replay",table.CommittedCueCount==muted,"Suppression consumed acknowledgement");
            CheckThat("focus-precondition-unpaused-first-reveal",game.CurrentView.stage=="revealing"&&!game.CurrentView.paused&&game.CurrentView.revealedCount==1&&!table.CollectionInputBlocked&&ReferenceEquals(table.View,game.CurrentView),"Synthetic callback requires an acknowledged first reveal and the actual unfenced projection");
            int focus=table.CommittedCueCount;InvokeTabletopFocus(table,false);
            CheckThat("focus-loss-preserves-game-phase",game.CurrentView.stage=="revealing"&&!game.CurrentView.paused&&game.CurrentView.revealedCount==1&&table.CommittedCueCount==focus&&!table.CollectionInputBlocked,"Internal Tabletop-only focus callback; not physical foreground-input evidence");
            game.DispatchCommand("next");
            CheckThat("focus-suppressed-ack-advances-exactly-once",game.CurrentView.stage=="revealing"&&!game.CurrentView.paused&&game.CurrentView.revealedCount==2&&table.CommittedCueCount==focus,"Real host acknowledged 1->2 while the presentation consumed the inaudible cue");
            InvokeTabletopFocus(table,true);table.NotifyCommitted("next");CheckThat("focus-return-no-replay",game.CurrentView.stage=="revealing"&&!game.CurrentView.paused&&game.CurrentView.revealedCount==2&&table.CommittedCueCount==focus&&!table.CollectionInputBlocked,"Internal focus callback test, not physical foreground-input evidence; duplicate suppressed acknowledgement never replays");
            CheckThat("focus-next-audio-precondition",game.CurrentView.stage=="revealing"&&!game.CurrentView.paused&&game.CurrentView.revealedCount==2&&table.CommittedCueCount==focus,"Next acknowledged reveal must be position three before native ObservePlayback");
            int cue=table.CommittedCueCount;game.DispatchCommand("next");yield return ObservePlayback("before-pause",ids[0],original.pullSounds[2],cue);game.DispatchCommand("pause");CheckThat("pause-stops-audio",Quiet(),"Actual child source");game.DispatchCommand("resume");CheckThat("resume-no-catchup",Quiet(),"No new ack");Command("binder-open");Command("binder-close");CheckThat("binder-back-no-catchup",Quiet(),"No per-position replay");
            cue=table.CommittedCueCount;game.DispatchCommand("skip");yield return ObservePlayback("skip-generic",ids[0],table.Media.Sound("skip"),cue);table.NotifyCommitted("skip");CheckThat("skip-no-music-catchup",table.CommittedCueCount==cue+1&&game.CurrentView.stage=="results","Exactly one generic Skip");CheckThat("audio-media-reference-restored",Snapshot().mediaBound,"Borrowed profile/texture lifecycle intact");yield return Capture("native-audio-complete");
        }
        private static void InvokeTabletopFocus(TabletopPresentation target,bool focused) {
            if(target==null)throw new ArgumentNullException("target");
            var callback=typeof(TabletopPresentation).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly,null,new[]{typeof(bool)},null);
            if(callback==null||callback.ReturnType!=typeof(void)||!callback.IsPrivate)throw new MissingMethodException(typeof(TabletopPresentation).FullName,"OnApplicationFocus(bool)");
            // The real callback also requests Pause. Fence only this synchronous synthetic call;
            // never broadcast Unity messages, alter normal focus policy, or fence the actual Next ack.
            bool blocked=target.CollectionInputBlocked;
            try{target.CollectionInputBlocked=true;callback.Invoke(target,new object[]{focused});}
            finally{target.CollectionInputBlocked=blocked;}
        }
        bool Quiet(){foreach(var a in GetComponentsInChildren<AudioSource>(true))if(a.isPlaying)return false;return true;}
        IEnumerator ObservePlayback(string name,string printing,AudioClip expected,int before){
            var sources=GetComponentsInChildren<AudioSource>(true);CheckThat("source:"+printing+":"+name,sources.Length==1&&!sources[0].loop&&!sources[0].playOnAwake,"Single native child source");var source=sources[0];var row=new Playback{name=name,printingId=printing,expectedClip=expected==null?null:expected.name,actualClip=source.clip==null?null:source.clip.name,childSourcePlaying=source.isPlaying,samples=expected==null?0:expected.samples,frequency=expected==null?0:expected.frequency};
            CheckThat("actual-playback:"+printing+":"+name,expected!=null&&source.clip==expected&&source.isPlaying&&table.CommittedCueCount==before+1,"Acknowledged actual native source");float deadline=Time.realtimeSinceStartup+.6f;float[] samples=new float[512],mixed=new float[512];do{yield return null;source.GetOutputData(samples,0);AudioListener.GetOutputData(mixed,0);foreach(float v in samples)row.sourcePeak=Math.Max(row.sourcePeak,Math.Abs(v));foreach(float v in mixed)row.listenerPeak=Math.Max(row.listenerPeak,Math.Abs(v));}while(Time.realtimeSinceStartup<deadline&&(row.sourcePeak<.00001f||row.listenerPeak<.00001f));playback.Add(row);CheckThat("actual-signal:"+printing+":"+name,row.sourcePeak>.00001f&&row.listenerPeak>.00001f,"Source+listener samples, not human hearing acceptance");
        }
        private void Command(string command) {host.DispatchBinderCommand(command);CheckThat("command-dispatched:"+command+":"+checks.Count,host.CurrentBinderView!=null,"Authoritative postconditions checked independently; expected storage faults retain error");}
        private Pin[] ProfilePins() {
            var list=new List<Pin>();if(paths==null)return list.ToArray();
            foreach(string name in authoritative) {string file=Path.Combine(paths.SaveDirectory,name);CheckAncestors(file);if(File.Exists(file))using(var hash=SHA256.Create())list.Add(new Pin {name=name,sha256=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file))).Replace("-","").ToLowerInvariant()});}return list.ToArray();
        }
        private string SessionJson() {
            var snapshot=game.CurrentSession;var packs=new List<PackRecordData>();
            foreach(var p in snapshot.Packs)packs.Add(new PackRecordData {transactionId=p.TransactionId,createdUtc=p.CreatedUtc,modelId=p.ModelId,modelVersion=p.ModelVersion,catalogVersion=p.CatalogVersion,printingId=p.PrintingId,rngAlgorithm=p.RngAlgorithm,status=p.Status,seed=p.Seed,orderedCardIds=new List<string>(p.OrderedCardIds).ToArray(),newToVirtual=new List<bool>(p.NewToVirtual).ToArray(),revealedCount=p.RevealedCount,wrapperOpened=p.WrapperOpened});
            return JsonUtility.ToJson(new SessionData {schemaVersion=1,revision=snapshot.Revision,packs=packs.ToArray()});
        }
        private void ObserveControl(string name,Rect rect,bool enabled) {
            // Binder calls the observer for every IMGUI pass. Only a complete repaint is render evidence.
            // The first supported binder-set button starts Phase5 repaint; legacy begins at mode:virtual.
            // Quit is last without review; import-cancel is last with review. Never merge repaint passes.
            if(Event.current==null || Event.current.type!=EventType.Repaint || binder==null)return;
            var current=binder.View;var choices=Get(current,"supportedSets") as Array;string beginning=choices!=null&&choices.Length>0?"binder-set:"+(string)Get(choices.GetValue(0),"printingId"):"mode:virtual";
            if(name==beginning || controlsFrame!=Time.frameCount || !ReferenceEquals(controlsView,current)) {
                binderControls.Clear();controlsComplete=false;controlsView=current;
                controlsViewJson=current==null?null:JsonUtility.ToJson(current);
                controlsFrame=Time.frameCount;controlsWidth=Screen.width;controlsHeight=Screen.height;
            }
            float scale=BinderLayoutRules.Geometry(Screen.width,Screen.height).scale;
            binderControls[name]=new Control {name=name,x=rect.x*scale,y=rect.y*scale,width=rect.width*scale,height=rect.height*scale,enabled=enabled};
            if(current!=null && name==(BinderLayoutRules.HasReview(current)?"import-cancel":"quit"))controlsComplete=true;
        }
        private Control[] CurrentBinderControls() {
            var current=host==null?null:host.CurrentBinderView;
            // Update runs before this frame's OnGUI: the immediately preceding complete repaint is usable
            // only while the SAME detached DTO and screen dimensions still apply. Changed/partial/expired
            // observations publish no controls, never an old set attached to a new BinderView.
            if(current==null || !current.open || !controlsComplete || !ReferenceEquals(current,controlsView) ||
                binder==null || !ReferenceEquals(binder.View,current) || controlsViewJson!=JsonUtility.ToJson(current) ||
                controlsFrame<Time.frameCount-1 || controlsFrame>Time.frameCount ||
                controlsWidth!=Screen.width || controlsHeight!=Screen.height)return new Control[0];
            return new List<Control>(binderControls.Values).ToArray();
        }
        private Control[] CurrentSelectorControls(){
            var type=Type.GetType("VintageCollector.Presentation.SetSelectorPresentation, Assembly-CSharp");var selector=type==null?null:GetComponent(type);if(selector==null||!ReferenceEquals(Get(selector,"View"),game.CurrentView))return new Control[0];var raw=Get(selector,"ControlObservations") as Array;if(raw==null||raw.Length!=3)return new Control[0];var result=new List<Control>();var names=new HashSet<string>(StringComparer.Ordinal);int frame=-1;
            foreach(var row in raw){string command=(string)Get(row,"command");int at=(int)Get(row,"renderFrame");var rect=(Rect)Get(row,"bounds");if(Array.IndexOf(new[]{"set:base-en-unlimited1999","set:jungle-en-unlimited1999","set:fossil-en-unlimited1999"},command)<0||!names.Add(command)||at<Time.frameCount-1||at>Time.frameCount||(frame!=-1&&frame!=at)||rect.width<=0||rect.height<=0||rect.x<0||rect.y<0||rect.xMax>Screen.width||rect.yMax>Screen.height)return new Control[0];frame=at;result.Add(new Control{name=command,x=rect.x,y=rect.y,width=rect.width,height=rect.height,enabled=true,source="selector-repaint",renderFrame=at});}return result.ToArray();
        }
        private Probe Snapshot() {
            var controls=new List<Control>();var selectorControls=CurrentSelectorControls();
            if(host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open)controls.AddRange(CurrentBinderControls());
            else {
                controls.AddRange(selectorControls);
                var l=PresentationRules.Layout(Screen.width,Screen.height);AddControl(controls,"primary",PrimaryControlBounds(l,game.CurrentView),l.scale);AddControl(controls,"resume",l.resume,l.scale);AddControl(controls,"quit",l.quit,l.scale);AddControl(controls,"hero",l.heroCard,l.scale);AddControl(controls,"back",l.back,l.scale);
                // Mirrors the source-pinned real TabletopPresentation footer target; native clicks must acknowledge an actual host change.
                if(game.CurrentView!=null && game.CurrentView.stage=="results")for(int i=0;i<11;i++)AddControl(controls,"result:"+i,l.resultsCards[i],l.scale);
                if(table!=null && table.CollectionAvailable)
                    AddControl(controls,"binder",new PresentationBox(l.width-520,l.height-49,220,34),l.scale);
            }
            var view=game==null?null:game.CurrentView;var session=game==null?null:game.CurrentSession;
            var media=GetComponent(Type.GetType("VintageCollector.Runtime.Phase5MediaHost, Assembly-CSharp"));var labels=new string[11];for(int i=0;i<11;i++)labels[i]=table==null?null:table.PhysicalResultLabel(i);
            var drawn=new List<string>(cumulativeRenderedLabels);drawn.Sort(StringComparer.Ordinal);
            return new Probe {stage=view==null?"uninitialized":view.stage, selectedPrintingId=(string)Get(game,"SelectedPrintingId"),activePrintingId=session==null||session.CurrentPack==null?null:session.CurrentPack.PrintingId,wrapperIndex=session==null||session.CurrentPack==null?0:Variant(session.CurrentPack.TransactionId),committedCueCount=table==null?0:table.CommittedCueCount,profileId=paths==null?"":paths.CollectionProfileId,
                revision=session==null?0:session.Revision,physicalRevision=host==null || host.Physical==null?0:host.Physical.Revision,
                generatedResultCount=game==null?0:game.GeneratedResultCount,totalVirtualCards=session==null?0:session.TotalVirtualCards,
                revealedCount=view==null?0:view.revealedCount,inspectIndex=view==null?-1:view.inspectIndex,paused=view!=null && view.paused,
                screenWidth=Screen.width,screenHeight=Screen.height,lastCommand=game==null?"":game.LastCommand,ready=host!=null && host.IsReady,selectorControlsObserved=selectorControls.Length==3,
                fixtureAck=fixtureAck,physicalLabels=labels,cumulativeRenderedLabels=drawn.ToArray(),mediaBound=media!=null && table!=null && table.Media!=null && ReferenceEquals(table.Media,stableMedia) && ReferenceEquals(table.RevealSounds,stableReveal),
                controlsSource=host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open?"binder-repaint":"source-pinned-table-layout",controlsViewJson=host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open && controls.Count>0?controlsViewJson:null,
                hostStatus=host==null?"Missing host":host.StatusMessage,binder=host==null?null:host.CurrentBinderView,sessionJson=session==null?"":SessionJson(),controls=controls.ToArray(),profilePins=ProfilePins()};
        }
        private static PresentationBox PrimaryControlBounds(PresentationLayout layout,OpeningView view) {
            // Source-pinned TabletopPresentation.DrawResults Generate target; all opening stages retain sidebar geometry.
            return view!=null&&view.stage=="results"?new PresentationBox(layout.width-310,140,266,34):layout.primary;
        }
        private static void AddControl(List<Control> controls,string name,PresentationBox r,float scale) {controls.Add(new Control {name=name,x=r.x*scale,y=r.y*scale,width=r.width*scale,height=r.height*scale,enabled=true});}
        private static string ObservationKey(Probe state){var frames=new int[state.controls.Length];for(int i=0;i<frames.Length;i++){frames[i]=state.controls[i].renderFrame;state.controls[i].renderFrame=-1;}try{return JsonUtility.ToJson(state);}finally{for(int i=0;i<frames.Length;i++)state.controls[i].renderFrame=frames[i];}}
        private void Update() {
            if(!active || finished || (mode!="input" && mode!="sealed" && mode!="mid") || capturing)return;
            string commandFile=Path.Combine(paths.ArtifactDirectory,"arm-fault.txt");
            if(File.Exists(commandFile)) {
                CheckAncestors(commandFile);string command=ReadExactFixture(commandFile,256,"physical","import");File.Delete(commandFile);
                if(command=="physical")host.FailNextPhysicalCommitForSmoke();
                else if(command=="import")host.FailNextImportForSmoke();
                else throw new InvalidOperationException("Unknown bounded fixture fault");
                fixtureAck=command;
            }
            string observation=ObservationKey(Snapshot());if(observation==lastObservation)return;
            StartCoroutine(Observe(observation));
        }
        private IEnumerator Observe(string observation) {
            capturing=true;yield return Guard(Capture("native-"+screenshots.Count.ToString("D3")));
            var snapshot=Snapshot();if(ObservationKey(snapshot)!=observation){capturing=false;yield break;}snapshot.capture=screenshots.Count==0?"":screenshots[screenshots.Count-1];
            AtomicText(Path.Combine(paths.ArtifactDirectory,"probe.json"),JsonUtility.ToJson(snapshot,true));lastObservation=observation;capturing=false;
        }
        private IEnumerator Capture(string name) {
            yield return null;yield return new WaitForEndOfFrame();string file=Path.Combine(paths.ArtifactDirectory,name+".png");CheckAncestors(file);ScreenCapture.CaptureScreenshot(file);
            float deadline=Time.realtimeSinceStartup+8f;bool complete=false;
            do {yield return null;complete=CompletePng(file,Screen.width,Screen.height);}while(!complete && Time.realtimeSinceStartup<deadline);
            CheckThat("framebuffer:"+name,complete,"Complete actual PNG IEND/CRC/dimensions");screenshots.Add(name+".png");
        }
        private static uint U32(byte[] b,int p) {return ((uint)b[p]<<24)|((uint)b[p+1]<<16)|((uint)b[p+2]<<8)|b[p+3];}
        private static bool CompletePng(string file,int width,int height) {
            try {
                byte[] b=File.ReadAllBytes(file),signature={137,80,78,71,13,10,26,10};if(b.Length<45)return false;
                for(int i=0;i<8;i++)if(b[i]!=signature[i])return false;
                int p=8;bool header=false,data=false;
                while(p+12<=b.Length) {
                    uint size=U32(b,p);if(size>int.MaxValue || (long)p+12+size>b.Length)return false;int n=(int)size;
                    uint crc=0xffffffff;for(int i=p+4;i<p+8+n;i++){crc^=b[i];for(int bit=0;bit<8;bit++)crc=(crc&1)!=0?0xedb88320^(crc>>1):crc>>1;}
                    if((crc^0xffffffff)!=U32(b,p+8+n))return false;
                    string kind=System.Text.Encoding.ASCII.GetString(b,p+4,4);
                    if(kind=="IHDR") {if(header || p!=8 || n!=13 || U32(b,p+8)!=width || U32(b,p+12)!=height)return false;header=true;}
                    if(kind=="IDAT")data=true;p+=n+12;if(kind=="IEND")return n==0 && p==b.Length && header && data;
                }return false;
            }catch(IOException){return false;}
        }
        private void CheckThat(string name,bool passed,string detail) {checks.Add(new Check {name=name,passed=passed,detail=detail});if(!passed)throw new InvalidOperationException(name+": "+detail);}
        private static void AtomicText(string path,string text) {
            string work=path+".writing";File.WriteAllText(work,text);
            // Windows readers may briefly omit FILE_SHARE_DELETE. Never lose observation on a transient reader lock.
            for(int attempt=0;;attempt++) {
                try {if(File.Exists(path))File.Replace(work,path,null);else File.Move(work,path);return;}
                catch(IOException e) {
                    int code=e.HResult&65535;if(attempt>=100 || (code!=32 && code!=33))throw;
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
        private void Finish(bool quit) {
            if(!active || finished)return;finished=true;
            bool passed=checks.Count>0 && checks.TrueForAll(c=>c.passed);
            var final=Snapshot();var seeds=new List<uint>();int prior=JsonUtility.FromJson<SessionData>(initial.sessionJson).packs.Length;for(int i=prior;i<game.CurrentSession.Packs.Count;i++)seeds.Add(game.CurrentSession.Packs[i].Seed);
            var report=new Report {faces=faces.ToArray(),nativeAllocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),wrappers=wrappers.ToArray(),wrapperCount=wrappers.Count,playback=playback.ToArray(),rareBranches=new List<string>(rareBranches).ToArray(),passed=passed,mode=mode,checkCount=checks.Count,checks=checks.ToArray(),screenshots=screenshots.ToArray(),screenshotCount=screenshots.Count,initial=initial,final=final,snapshots=new[]{initial,final},snapshotCount=2,seeds=seeds.ToArray(),seedCount=seeds.Count};
            AtomicText(Path.Combine(paths.ArtifactDirectory,"report.json"),JsonUtility.ToJson(report,true));if(quit){if(passed)game.DispatchCommand("quit");else Application.Quit(1);}
        }
        private void OnApplicationQuit() {if(active && !finished)Finish(false);}
        private void OnDestroy() {if(binder!=null)binder.ControlObserver=null;if(table!=null)table.TextObserver=null;}
    }
}