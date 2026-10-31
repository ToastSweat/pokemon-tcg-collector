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
    public sealed class Phase4SmokeProbe : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string name;public bool passed;public string detail;}
        [Serializable] public sealed class Control {public string name;public float x,y,width,height;public bool enabled;}
        [Serializable] public sealed class Pin {public string name,sha256;}
        [Serializable] public sealed class Probe {
            public string stage,profileId,sessionJson,capture,lastCommand,hostStatus,fixtureAck,controlsViewJson,controlsSource;
            public string[] physicalLabels,cumulativeRenderedLabels;
            public bool mediaBound;
            public long revision,physicalRevision;
            public int generatedResultCount,totalVirtualCards,revealedCount,inspectIndex,screenWidth,screenHeight;
            public bool paused,ready;
            public BinderView binder;
            public Control[] controls;
            public Pin[] profilePins;
        }
        [Serializable] public sealed class Report {
            public bool passed;public int checkCount;public string mode;public Check[] checks;public string[] screenshots;public Probe initial,final;
        }
        private Phase4CollectionHost host;private OpeningGame game;private GamePaths paths;
        private BinderPresentation binder;
        private TabletopPresentation table;
        private string fixtureAck="";
        private readonly HashSet<string> cumulativeRenderedLabels=new HashSet<string>(StringComparer.Ordinal);
        private bool active,finished,capturing;private string mode,lastObservation;
        private Probe initial;
        private readonly List<Check> checks=new List<Check>();
        private readonly List<string> screenshots=new List<string>();
        private readonly Dictionary<string,Control> binderControls=new Dictionary<string,Control>(StringComparer.Ordinal);
        private BinderView controlsView;
        private string controlsViewJson;
        private int controlsFrame=-1,controlsWidth,controlsHeight;
        private bool controlsComplete;
        private readonly string[] authoritative={"state.save","state.backup","state.anchor","physical.save","physical.backup","physical.anchor"};
        private IEnumerator Start() {
            yield return null;host=GetComponent<Phase4CollectionHost>();game=GetComponent<OpeningGame>();
            if(game==null || game.Paths==null || !game.Paths.IsSmokeTest)yield break;
            paths=game.Paths;
            try {mode=ValidateFixture(paths);}
            catch(Exception e) {Debug.LogError("Phase 4 fixture rejected: "+e.Message);Application.Quit(2);yield break;}
            active=true;Application.runInBackground=true;binder=GetComponent<BinderPresentation>();
            if(binder!=null)binder.ControlObserver=ObserveControl;
            table=GetComponent<TabletopPresentation>();
            if(table!=null)table.TextObserver=text=>cumulativeRenderedLabels.Add(text);
            initial=Snapshot();
            yield return Guard(Run());
            if(mode!="input")Finish(true);
        }
        private static string ValidateFixture(GamePaths paths) {
            // The approved root is fixed; the controller has already validated its smoke paths.
            string root=Path.GetFullPath("F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/.artifacts/phase4-player");
            string artifacts=Path.GetFullPath(paths.ArtifactDirectory),save=Path.GetFullPath(paths.BaseSaveDirectory);
            if(!artifacts.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Wrong fixture root");
            string[] segments=artifacts.Substring(root.Length+1).Split(Path.DirectorySeparatorChar);
            if(segments.Length!=2 || !System.Text.RegularExpressions.Regex.IsMatch(segments[0],"^[A-Za-z0-9_-]+$") ||
                (segments[1]!="services" && segments[1]!="reopen" && segments[1]!="input"))throw new ArgumentException("Wrong owned fixture directory");
            string expectedSave=Path.Combine(root,segments[0],"session");
            if(!string.Equals(save,expectedSave,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Fixture save is not the owned run session");
            CheckAncestors(artifacts);CheckAncestors(save);
            string file=Path.Combine(artifacts,"phase4-fixture.json");CheckAncestors(file);
            string exact="{\"schemaVersion\":1,\"owner\":\"phase4-player-fixture\",\"mode\":\""+segments[1]+"\"}";
            ReadExactFixture(file,256,exact);
            if(paths.SmokeScenario!="input")throw new ArgumentException("Phase 4 fixture requires supported input smoke mode");
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
        private IEnumerator Run() {
            CheckThat("actual-host-ready",host!=null && host.IsReady && game.CurrentSession!=null,host==null?"Missing host":host.StatusMessage);
            CheckThat("startup-never-generates",game.GeneratedResultCount==0,"Real controller startup");
            CheckThat("actual-media-host-bound",Snapshot().mediaBound,"Actual optional host library bound, not a playback/hearing claim");
            if(mode=="input")yield break;
            if(game.CurrentView.paused)game.DispatchCommand("resume");
            if(mode=="reopen") {
                CheckThat("next-process-activates-imported-profile",!string.IsNullOrEmpty(host.Profiles.ActiveProfileId),"Validated pointer selected on next process");
                CheckThat("reloaded-real-session-history",game.CurrentSession.Packs.Count==1 && game.CurrentSession.TotalVirtualCards==11 && game.CurrentSession.CurrentPack.RevealedCount==11,"Original archived pack, no generation");
                CheckThat("reloaded-physical-condition-buckets",host.Physical.Revision>=2 && ExpectedBuckets(),"Independent persisted condition buckets");
                Command("binder-open");CheckThat("reopen-virtual-eleven",host.CurrentBinderView.totalQuantity==11 && host.CurrentBinderView.targetCount==101,"Strict revealed projection");
                Command("mode:physical");CheckThat("reopen-physical-total-three",host.CurrentBinderView.totalQuantity==(host.Physical.Revision==2?3:4) && host.CurrentBinderView.uniqueOwned==(host.Physical.Revision==2?1:2),"Manual ownership survived actual process restart");
                yield return Capture("reopened-physical");yield break;
            }
            CheckThat("fresh-empty-session",game.CurrentSession.Packs.Count==0 && game.CurrentSession.Revision==0 && host.Physical.Revision==0,"Fresh owned fixture only");
            Command("binder-open");Validate101();CheckThat("initial-empty-virtual",host.CurrentBinderView.totalQuantity==0 && host.CurrentBinderView.uniqueOwned==0,"No startup generation");
            Command("mode:physical");CheckThat("initial-empty-physical",host.CurrentBinderView.totalQuantity==0,"Independent manual ownership");yield return Capture("initial-empty-physical");
            string manual=host.CurrentBinderView.rows[0].id;
            Command("select:"+manual);Command("condition:near-mint");Command("quantity:2");Command("save-physical");
            Command("condition:lightly-played");Command("quantity:1");Command("save-physical");
            CheckThat("manual-before-any-virtual-pulls",host.Physical.Revision==2 && ExpectedBuckets() && game.CurrentSession.Packs.Count==0 && game.GeneratedResultCount==0,"Independent physical condition buckets before generation");
            yield return Capture("manual-before-pulls");
            Command("binder-close");game.GenerateForSmoke(4u);Command("binder-open");Command("mode:virtual");
            CheckThat("generated-hidden-awards-not-projected",game.CurrentSession.TotalVirtualCards==11 && host.CurrentBinderView.totalQuantity==0,"Durable hidden pack must not spoil binder");yield return Capture("sealed-hidden-binder");
            Command("binder-close");game.DispatchCommand("open");for(int i=0;i<3;i++)game.DispatchCommand("next");
            Command("binder-open");CheckThat("only-three-revealed-projected",host.CurrentBinderView.totalQuantity==3,"Actual controller reveal prefix");yield return Capture("three-revealed-binder");Command("binder-close");
            for(int i=3;i<11;i++)game.DispatchCommand("next");
            CheckThat("normal-card-eleven-hold",game.CurrentView.stage=="revealing" && game.CurrentView.revealedCount==11 && game.CurrentView.inspectIndex==-1,"Normal accepted table hold, no implicit results");
            long revision=game.CurrentSession.Revision;string history=SessionJson();Command("binder-open");Validate101();
            CheckThat("completed-eleven-projected",host.CurrentBinderView.totalQuantity==11,"No hidden cards remain");
            CheckThat("physical-not-inferred-from-virtual",host.Physical.Revision==2 && ExpectedBuckets(),"Reveals do not change manual ownership");
            Command("binder-close");CheckThat("binder-back-preserves-final-hold",game.CurrentView.stage=="revealing" && game.CurrentView.inspectIndex==-1 && game.CurrentSession.Revision==revision && SessionJson()==history,"Binder does not change accepted explicit-results gate");yield return Capture("card-eleven-table");
            game.DispatchCommand("results");yield return Capture("explicit-results-physical-missing");
            CheckThat("actual-results-labels-rendered",game.CurrentView.stage=="results" && cumulativeRenderedLabels.Contains("PHYSICAL MISSING"),"Actual native OnGUI text observer and framebuffer");
            game.DispatchCommand("inspect:0");yield return Capture("results-inspection");game.DispatchCommand("back");
            Command("binder-open");Command("mode:physical");Command("select:"+manual);
            CheckThat("independent-condition-buckets",host.Physical.Revision==2 && host.Physical.Holdings.Count==2 && host.CurrentBinderView.totalQuantity==3 && host.CurrentBinderView.uniqueOwned==1,"One unique printing, three confirmed cards");yield return Capture("physical-buckets");
            Command("backup");CheckThat("actual-validated-backup-created",host.CurrentBinderView.backups.Length==1,"Real CollectionProfiles service");
            Command("condition:damaged");Command("quantity:7");string draft=host.CurrentBinderView.quantityText,condition=host.CurrentBinderView.selectedCondition,selected=host.CurrentBinderView.selectedCardId;
            string before=JsonUtility.ToJson(new Pins {files=ProfilePins()});Command("backup-select:0");Command("import-preview");
            CheckThat("readonly-preview-retains-draft",!string.IsNullOrEmpty(host.CurrentBinderView.importSummary) && host.CurrentBinderView.quantityText==draft && host.CurrentBinderView.selectedCondition==condition && host.CurrentBinderView.selectedCardId==selected,"Retained host draft plus actual reviewed archive");
            CheckThat("preview-keeps-old-profile-bytes",before==JsonUtility.ToJson(new Pins {files=ProfilePins()}),"Exact authoritative SHA256 pins");yield return Capture("review-retained-draft");
            Command("import-cancel");CheckThat("cancel-retains-draft",string.IsNullOrEmpty(host.CurrentBinderView.importSummary) && host.CurrentBinderView.quantityText==draft,"No mutation from review cancellation");
            Command("import-preview");Command("import-confirm");
            CheckThat("confirm-requires-next-launch",host.CurrentBinderView.restartRequired && host.Profiles.ActiveProfileId=="" && paths.CollectionProfileId=="","Current live profile unchanged until Quit/reopen");
            CheckThat("confirm-keeps-old-profile-bytes",before==JsonUtility.ToJson(new Pins {files=ProfilePins()}),"Clone, never restore-in-place");
            CheckThat("all-binder-operations-preserve-session",SessionJson()==history && game.GeneratedResultCount==1,"Complete ordered pack/provenance/flags/revision unchanged");yield return Capture("restart-required");
        }
        private bool ExpectedBuckets() {
            if(host.Physical.Holdings.Count<2 || host.Physical.Holdings.Count>3)return false;bool near=false,light=false;string id=null;
            foreach(var h in host.Physical.Holdings)if(h.Condition=="lightly-played" && h.Quantity==1)id=h.CardId;
            foreach(var holding in host.Physical.Holdings) {
                if(holding.CardId!=id) {if(holding.Condition!="near-mint" || holding.Quantity!=1)return false;continue;}
                near|=holding.Condition=="near-mint" && holding.Quantity==2;
                light|=holding.Condition=="lightly-played" && holding.Quantity==1;
            }
            return near && light;
        }
        private void Validate101() {
            var view=host.CurrentBinderView;bool valid=view.targetCount==101 && view.rows.Length==101;
            foreach(var row in view.rows)if(row.collectorNumber=="8" || string.Equals(row.name,"Machamp",StringComparison.OrdinalIgnoreCase))valid=false;
            CheckThat("strict-101-no-machamp-"+checks.Count,valid,"Actual eligible catalog projection");
        }
        private void Command(string command) {host.DispatchBinderCommand(command);CheckThat("command:"+command+":"+checks.Count,host.CurrentBinderView!=null && string.IsNullOrEmpty(host.CurrentBinderView.error),host.CurrentBinderView==null?"Missing view":host.CurrentBinderView.error);}
        [Serializable] private sealed class Pins {public Pin[] files;}
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
            // mode:virtual is the first registered target in each source-pinned binder pass; quit is last
            // without review, import-cancel is last with review. No renderer hook or invented targets.
            if(Event.current==null || Event.current.type!=EventType.Repaint || binder==null)return;
            var current=binder.View;
            if(name=="mode:virtual" || controlsFrame!=Time.frameCount || !ReferenceEquals(controlsView,current)) {
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
        private Probe Snapshot() {
            var controls=new List<Control>();
            if(host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open)controls.AddRange(CurrentBinderControls());
            else {
                var l=PresentationRules.Layout(Screen.width,Screen.height);AddControl(controls,"primary",l.primary,l.scale);AddControl(controls,"resume",l.resume,l.scale);AddControl(controls,"quit",l.quit,l.scale);AddControl(controls,"hero",l.heroCard,l.scale);AddControl(controls,"back",l.back,l.scale);
                // Mirrors the source-pinned real TabletopPresentation footer target; native clicks must acknowledge an actual host change.
                if(game.CurrentView!=null && game.CurrentView.stage=="results")for(int i=0;i<11;i++)AddControl(controls,"result:"+i,l.resultsCards[i],l.scale);
                if(table!=null && table.CollectionAvailable)
                    AddControl(controls,"binder",new PresentationBox(l.width-520,l.height-49,220,34),l.scale);
            }
            var view=game==null?null:game.CurrentView;var session=game==null?null:game.CurrentSession;
            var media=GetComponent<Phase3MediaHost>();var labels=new string[11];for(int i=0;i<11;i++)labels[i]=table==null?null:table.PhysicalResultLabel(i);
            var drawn=new List<string>(cumulativeRenderedLabels);drawn.Sort(StringComparer.Ordinal);
            return new Probe {stage=view==null?"uninitialized":view.stage,profileId=paths==null?"":paths.CollectionProfileId,
                revision=session==null?0:session.Revision,physicalRevision=host==null || host.Physical==null?0:host.Physical.Revision,
                generatedResultCount=game==null?0:game.GeneratedResultCount,totalVirtualCards=session==null?0:session.TotalVirtualCards,
                revealedCount=view==null?0:view.revealedCount,inspectIndex=view==null?-1:view.inspectIndex,paused=view!=null && view.paused,
                screenWidth=Screen.width,screenHeight=Screen.height,lastCommand=game==null?"":game.LastCommand,ready=host!=null && host.IsReady,
                fixtureAck=fixtureAck,physicalLabels=labels,cumulativeRenderedLabels=drawn.ToArray(),mediaBound=media!=null && media.Library!=null && table!=null && ReferenceEquals(table.Media,media.Library),
                controlsSource=host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open?"binder-repaint":"source-pinned-table-layout",controlsViewJson=host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open && controls.Count>0?controlsViewJson:null,
                hostStatus=host==null?"Missing host":host.StatusMessage,binder=host==null?null:host.CurrentBinderView,sessionJson=session==null?"":SessionJson(),controls=controls.ToArray(),profilePins=ProfilePins()};
        }
        private static void AddControl(List<Control> controls,string name,PresentationBox r,float scale) {controls.Add(new Control {name=name,x=r.x*scale,y=r.y*scale,width=r.width*scale,height=r.height*scale,enabled=true});}
        private void Update() {
            if(!active || finished || mode!="input" || capturing)return;
            string commandFile=Path.Combine(paths.ArtifactDirectory,"arm-fault.txt");
            if(File.Exists(commandFile)) {
                CheckAncestors(commandFile);string command=ReadExactFixture(commandFile,256,"physical","import");File.Delete(commandFile);
                if(command=="physical")host.FailNextPhysicalCommitForSmoke();
                else if(command=="import")host.FailNextImportForSmoke();
                else throw new InvalidOperationException("Unknown bounded fixture fault");
                fixtureAck=command;
            }
            string observation=JsonUtility.ToJson(Snapshot());if(observation==lastObservation)return;
            StartCoroutine(Observe(observation));
        }
        private IEnumerator Observe(string observation) {
            capturing=true;yield return Guard(Capture("native-"+screenshots.Count.ToString("D3")));
            var snapshot=Snapshot();snapshot.capture=screenshots.Count==0?"":screenshots[screenshots.Count-1];
            AtomicText(Path.Combine(paths.ArtifactDirectory,"probe.json"),JsonUtility.ToJson(snapshot,true));lastObservation=observation;capturing=false;
        }
        private IEnumerator Capture(string name) {
            yield return null;yield return new WaitForEndOfFrame();string file=Path.Combine(paths.ArtifactDirectory,name+".png");ScreenCapture.CaptureScreenshot(file);
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
            var report=new Report {passed=passed,mode=mode,checkCount=checks.Count,checks=checks.ToArray(),screenshots=screenshots.ToArray(),initial=initial,final=Snapshot()};
            AtomicText(Path.Combine(paths.ArtifactDirectory,"report.json"),JsonUtility.ToJson(report,true));if(quit)Application.Quit(passed?0:1);
        }
        private void OnApplicationQuit() {if(active && mode=="input")Finish(false);}
        private void OnDestroy() {if(binder!=null)binder.ControlObserver=null;if(table!=null)table.TextObserver=null;}
    }
}
