using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VintageCollector.Domain;
using VintageCollector.Media;
using VintageCollector.Persistence;
using VintageCollector.Presentation;
using VintageCollector.Runtime;

namespace VintageCollector.EditorChecks
{
    // Explicit -executeMethod only. No startup hooks, packages, player or live-save access.
    public static class Phase3MediaHostChecks
    {
        [Serializable] private sealed class Result { public string name; public bool passed; public string detail; }
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public Result[] checks; public string failure; public string fixtureRoot; public string scope="Original synthetic fixtures, not actual supplied assets; native engine evidence, not physical hearing"; }
        private static readonly List<Result> checks=new List<Result>();
        private static string root, fixtureRoot;
        private static Type hostType;
        private static void Check(bool ok,string name) { checks.Add(new Result {name=name,passed=ok,detail=ok?"passed":"failed"}); if(!ok) throw new Exception(name); }
        private static object Get(object target,string property) { return target.GetType().GetProperty(property).GetValue(target,null); }
        private static void Invoke(object target,string method,params object[] args) {
            try { target.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject go;
            public readonly OpeningGame game;
            public readonly TabletopPresentation ui;
            public Component host;
            public readonly string directory;
            public Fixture(string existing=null,bool initialize=true) {
                directory=existing ?? Path.Combine(fixtureRoot,"synthetic-"+Guid.NewGuid().ToString("N"));
                go=new GameObject("Original synthetic core-connected host fixture");
                game=go.AddComponent<OpeningGame>(); game.allowedProjectRoot=root;
                game.catalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
                game.modelJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
                if(initialize) game.Initialize(new[]{"--vcc-smoke=ready","--vcc-save-root="+Path.Combine(directory,"saves"),"--vcc-artifacts="+directory});
                ui=go.GetComponent<TabletopPresentation>();
            }
            public void Bind() { host=go.AddComponent(hostType); Invoke(host,"Initialize",game); }
            public PresentationMediaLibrary Library { get { return (PresentationMediaLibrary)Get(host,"Library"); } }
            public void Command(string command) { game.DispatchCommand(command); }
            public void Dispose() { if(host!=null) Invoke(host,"Shutdown"); game.Shutdown(); UnityEngine.Object.DestroyImmediate(go); }
        }
        private static void Binding() {
            Check(hostType!=null,"Frozen Phase3MediaHost type exists");
            Check(hostType.IsSealed && typeof(MonoBehaviour).IsAssignableFrom(hostType),"Host is sealed MonoBehaviour");
            var order=(DefaultExecutionOrder)Attribute.GetCustomAttribute(hostType,typeof(DefaultExecutionOrder));
            Check(order!=null && order.order==100 && Attribute.IsDefined(hostType,typeof(DisallowMultipleComponent)),"Host starts after controller and disallows duplicates");
            foreach(string p in new[]{"Library","UsesTestManifest","StatusMessage"}) Check(hostType.GetProperty(p)!=null && hostType.GetProperty(p).GetSetMethod()==null,"Frozen readonly property "+p);
            using(var f=new Fixture()) {
                var before=f.game.CurrentSession;
                f.Bind(); var library=f.Library;
                Check(library!=null && ReferenceEquals(f.ui.Media,library),"Real library bound to controller-created renderer");
                Check(library.SuppliedAssetCount==0 && !(bool)Get(f.host,"UsesTestManifest"),"Default empty production manifest binds zero supplied assets");
                Check(library.Warnings.Count==0,"Valid empty production manifest has no library warnings");
                Check(f.ui.CommittedCueCount==0 && f.go.GetComponents<AudioSource>().Length==0,"Default binding creates no AudioSource or startup sound");
                foreach(string cue in new[]{"generate","open","next","skip"}) {
                    var clip=library.Sound(cue); var data=new float[clip.samples*clip.channels];
                    bool read=clip.GetData(data,0), finite=true; float peak=0;
                    foreach(float x in data) { finite &= !float.IsNaN(x) && !float.IsInfinity(x); peak=Mathf.Max(peak,Mathf.Abs(x)); }
                    Check(read && finite && peak>0 && peak<0.25f && clip.name.StartsWith("Original generic foley"),"Real native original clip samples finite bounded nonzero "+cue);
                }
                Invoke(f.host,"Initialize",f.game); Invoke(f.host,"Start");
                Check(ReferenceEquals(library,f.Library) && ReferenceEquals(before,f.game.CurrentSession) && f.game.GeneratedResultCount==0 && !File.Exists(Path.Combine(f.directory,"saves","state.save")),"Repeat Initialize and Start are passive and idempotent");
                Check(!((string)Get(f.host,"StatusMessage")).Contains("http"),"Host status exposes no source URL");
            }
        }
        [Serializable] private sealed class Manifest { public int schemaVersion=1; public string status="Original synthetic test fixtures, not actual supplied assets", intendedUse="Private integration tests only", redistributionPermission="none", rightsDefault="Original synthetic test only", notes="Not branded or actual supplied media"; public Entry[] assets; }
        [Serializable] private sealed class Entry { public string role,cardId="",relativePath,source="https://synthetic.invalid/private?token=DO-NOT-LEAK",rights="Original synthetic test only",restrictions="Not actual supplied assets",sha256; }
        private static string Hash(byte[] bytes) { using(var sha=System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        private static string Relative(string path) { return path.Substring(root.Length+1).Replace('\\','/'); }
        private static Entry Asset(string directory,string role,string id,byte[] bytes,string extension) {
            Directory.CreateDirectory(directory); string path=Path.Combine(directory,role+extension); File.WriteAllBytes(path,bytes);
            return new Entry {role=role,cardId=id,relativePath=Relative(path),sha256=Hash(bytes)};
        }
        private static byte[] Png(int width=63,int height=88) {
            var t=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try { var pixels=new Color32[width*height]; for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(37,129,211,255); t.SetPixels32(pixels); t.Apply(); return t.EncodeToPNG(); }
            finally { UnityEngine.Object.DestroyImmediate(t); }
        }
        private static byte[] Wave() {
            using(var s=new MemoryStream()) using(var w=new BinaryWriter(s)) {
                int samples=4800; w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36+samples*2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(24000); w.Write(48000); w.Write((short)2); w.Write((short)16); w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(samples*2);
                for(int i=0;i<samples;i++) w.Write((short)(2800*Math.Sin(i*0.13)*(1-i/(double)samples))); return s.ToArray();
            }
        }
        private static void WriteManifest(string directory,params Entry[] entries) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory,"media-manifest.json"),JsonUtility.ToJson(new Manifest {assets=entries})); }
        private static string Holo(Fixture f) { var c=Catalog.FromData(JsonUtility.FromJson<CatalogData>(f.game.catalogJson.text)); foreach(var p in c.Cards) if(p.Finish=="holo") return p.Id; throw new Exception("No holo identity"); }
        private static void Override() {
            using(var f=new Fixture()) {
                string id=Holo(f);
                WriteManifest(f.directory,Asset(f.directory,"card-face",id,Png(),".png"),Asset(f.directory,"holo-mask",id,Png(),".png"),Asset(f.directory,"sound-generate","",Wave(),".wav"));
                f.Bind(); var library=f.Library;
                Check((bool)Get(f.host,"UsesTestManifest") && library.SuppliedAssetCount==3,"Explicit smoke mode uses existing project-local synthetic override only");
                var face=library.CardFace(id); var mask=library.HoloMask(id);
                Check(face!=null && mask!=null && face.width==63 && face.height==88 && Mathf.Abs(face.GetPixel(0,0).b-211/255f)<0.01f,"Stable full identity resolves actually decoded synthetic pixels and aligned mask");
                Check(library.CardFace("1")==null && library.CardFace("Alakazam")==null,"Abbreviated and display identities do not alias full stable ID");
                var clip=library.Sound("generate"); var samples=new float[clip.samples]; bool read=clip.GetData(samples,0); float peak=0; bool finite=true;
                foreach(float x in samples) { finite &= !float.IsNaN(x) && !float.IsInfinity(x); peak=Mathf.Max(peak,Mathf.Abs(x)); }
                Check(read && finite && clip.samples==4800 && peak>0.01f && clip.name.StartsWith("Verified optional cue"),"Actual native synthetic PCM decoded and read back finite nonzero");
                Check(f.ui.CommittedCueCount==0 && f.go.GetComponents<AudioSource>().Length==0,"Synthetic override never starts source playback");
                Check(!((string)Get(f.host,"StatusMessage")).Contains("DO-NOT-LEAK") && !((string)Get(f.host,"StatusMessage")).Contains("https"),"Source token never leaks through host status");
            }
        }
        private static void Lifecycle() {
            using(var f=new Fixture()) {
                f.Bind(); var library=f.Library; var clip=library.Sound("generate");
                f.game.GenerateForSmoke(4); var source=f.ui.FoleySource;
                Check(source.clip==clip && f.ui.CommittedCueCount==1,"Real controller's acknowledged generation assigns owned native clip");
                Invoke(f.host,"Shutdown");
                Check(f.ui.Media==null && source.clip==null && !source.isPlaying && clip==null && f.Library==null,"Shutdown detaches renderer and stops source before owned resources are disposed");
                Invoke(f.host,"Shutdown"); Invoke(f.host,"Initialize",f.game); Invoke(f.host,"Start");
                Check(f.Library==null && f.ui.Media==null,"Shutdown is terminal across repeated Shutdown Initialize and Start");
            }
            using(var f=new Fixture()) {
                f.Bind(); var owned=f.Library; var catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(f.game.catalogJson.text));
                using(var external=PresentationMediaLibrary.Load(root,catalog)) {
                    var externalClip=external.Sound("generate"); f.ui.Media=external;
                    Invoke(f.host,"Shutdown");
                    Check(ReferenceEquals(f.ui.Media,external) && externalClip!=null && external.Sound("generate")==externalClip && owned.Sound("generate")==null,"Shutdown disposes only owned library and never replaces or disposes external provider");
                    f.ui.Media=null;
                }
            }
            using(var f=new Fixture()) {
                f.Bind(); var clip=f.Library.Sound("generate"); f.game.GenerateForSmoke(4);
                // Edit-mode fixtures do not receive normal play lifecycle messages automatically.
                Invoke(f.host,"OnDestroy"); UnityEngine.Object.DestroyImmediate(f.host); f.host=null;
                Check(f.ui.Media==null && f.ui.FoleySource.clip==null && clip==null,"Native OnDestroy callback clears renderer and destroys only owned clips");
            }
        }
        private static void CoreFlow() {
            string directory,tx,order; long revision;
            using(var f=new Fixture()) {
                f.Bind(); var g=f.game;
                f.Command("next"); f.Command("open"); f.Command("skip"); f.ui.NotifyCommitted("invalid");
                Check(f.ui.CommittedCueCount==0 && g.GeneratedResultCount==0,"Rejected pre-pack requests cannot cue or generate");
                // Exercise the real renderer event -> controller -> SessionStore -> acknowledged renderer seam.
                Invoke(f.ui,"RequestCommand","generate");
                Check(g.CurrentView.stage=="sealed" && g.GeneratedResultCount==1 && g.CurrentSession.TotalVirtualCards==11 && f.ui.CommittedCueCount==1 && f.ui.LastCommittedCue=="generate","Actual generate acknowledges exactly eleven saved awards before one eligible cue");
                Check(f.ui.FoleySource.clip==f.Library.Sound("generate") && File.Exists(Path.Combine(f.directory,"saves","state.save")),"Generate cue references real original clip only after durable save exists");
                f.Command("generate"); f.ui.NotifyCommitted("generate");
                Check(g.GeneratedResultCount==1 && f.ui.CommittedCueCount==1,"Rejected generation and repeated acknowledgement never reroll or replay");
                f.Command("open"); f.ui.NotifyCommitted("open");
                Check(g.CurrentView.stage=="revealing" && g.CurrentView.revealedCount==0 && f.ui.CommittedCueCount==2 && f.ui.LastCommittedCue=="open","Actual wrapper open produces one postcommit cue and no reveal");
                for(int i=1;i<=3;i++) { f.Command("next"); f.ui.NotifyCommitted("next"); Check(g.CurrentView.revealedCount==i && f.ui.CommittedCueCount==2+i,"Actual absolute Next acknowledged only once at "+i); }
                f.Command("skip"); f.ui.NotifyCommitted("skip"); f.Command("skip");
                Check(g.CurrentView.stage=="results" && g.CurrentView.revealedCount==11 && f.ui.CommittedCueCount==6 && f.ui.LastCommittedCue=="skip","Actual Skip emits one cue not a per-card burst or duplicate");
                directory=f.directory; tx=g.CurrentSession.CurrentPack.TransactionId; order=string.Join("|",g.CurrentSession.CurrentPack.OrderedCardIds); revision=g.CurrentSession.Revision;
                string bytes=File.ReadAllText(Path.Combine(directory,"saves","state.save"));
                f.Command("pause"); f.Command("resume"); f.Command("volume:0.25");
                Check(File.ReadAllText(Path.Combine(directory,"saves","state.save"))==bytes && g.CurrentSession.TotalVirtualCards==11 && f.ui.CommittedCueCount==6,"Media and settings do not persist schema/settings or award/replay");
            }
            using(var f=new Fixture(directory)) {
                f.Bind();
                Check(f.game.GeneratedResultCount==0 && f.game.CurrentSession.TotalVirtualCards==11 && f.game.CurrentSession.Revision==revision && f.game.CurrentSession.CurrentPack.TransactionId==tx && string.Join("|",f.game.CurrentSession.CurrentPack.OrderedCardIds)==order,"Fresh controller SessionStore resumes exact saved order and awards without generation");
                Check(f.ui.CommittedCueCount==0 && f.go.GetComponents<AudioSource>().Length==0,"Fresh resumed projection and host bind never start sound");
                var catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(f.game.catalogJson.text)); f.game.Shutdown();
                using(var store=new SessionStore(Path.Combine(directory,"saves"),new UnityStateSerializer(),catalog)) Check(store.Snapshot.TotalVirtualCards==11 && store.Snapshot.Packs.Count==1,"Real serializer/store readback retains exactly one pack and eleven awards");
            }
        }
        private static SessionData Prepared(Fixture f) {
            string text=File.ReadAllText(Path.Combine(f.directory,"saves","state.pending"));
            return new UnityStateSerializer().Deserialize(text.Substring(text.IndexOf('\n',text.IndexOf('\n')+1)+1));
        }
        private static void RetryAndSuppression() {
            using(var f=new Fixture()) {
                f.Bind(); var g=f.game; var before=g.CurrentSession;
                g.FailNextCommitForSmoke(); g.GenerateForSmoke(4);
                Check(g.CurrentView.stage=="error" && ReferenceEquals(g.CurrentSession,before) && f.ui.CommittedCueCount==0 && f.go.GetComponents<AudioSource>().Length==0,"Prepared generation write failure produces no cue or source before acknowledgement");
                var prepared=Prepared(f).packs[0];
                f.Command("generate"); g.GenerateForSmoke(7); f.Command("open");
                string pending=Path.Combine(f.directory,"saves","state.pending");
                using(var locked=new FileStream(pending,FileMode.Open,FileAccess.Read,FileShare.None)) f.Command("retry");
                Check(g.CurrentView.stage=="error" && g.GeneratedResultCount==1 && f.ui.CommittedCueCount==0,"Actual locked prepared-file Retry remains silent and retains the single generation");
                f.Command("retry");
                Check(g.CurrentView.stage=="sealed" && g.GeneratedResultCount==1 && g.CurrentSession.TotalVirtualCards==11 && g.CurrentSession.CurrentPack.TransactionId==prepared.transactionId && g.CurrentSession.CurrentPack.Seed==prepared.seed && string.Join("|",g.CurrentSession.CurrentPack.OrderedCardIds)==string.Join("|",prepared.orderedCardIds),"Successful Retry acknowledges exact prepared transaction seed and order without reroll or reaward");
                Check(f.ui.CommittedCueCount==1 && f.ui.LastCommittedCue=="generate" && f.ui.FoleySource.clip==f.Library.Sound("generate"),"Successful generation Retry emits at most one original action cue not retry cue");
                f.Command("retry"); f.ui.NotifyCommitted("generate"); Check(f.ui.CommittedCueCount==1,"Repeated Retry and acknowledgement cannot replay recovered generation");
                int cue=1;
                foreach(string command in new[]{"open","next","skip"}) {
                    before=g.CurrentSession; g.FailNextCommitForSmoke(); f.Command(command);
                    Check(g.CurrentView.stage=="error" && ReferenceEquals(g.CurrentSession,before) && f.ui.CommittedCueCount==cue && f.ui.FoleySource.clip==null && !f.ui.FoleySource.isPlaying,"Prepared "+command+" failure stops old source and never cues before acknowledgement");
                    var p=Prepared(f).packs[0]; f.Command(command); f.Command("retry"); cue++;
                    Check(g.CurrentSession.CurrentPack.TransactionId==p.transactionId && g.CurrentSession.CurrentPack.RevealedCount==p.revealedCount && g.CurrentSession.CurrentPack.WrapperOpened==p.wrapperOpened && g.GeneratedResultCount==1 && g.CurrentSession.TotalVirtualCards==11 && f.ui.CommittedCueCount==cue && f.ui.LastCommittedCue==command,"Retry acknowledges exact original "+command+" once without extra reveal or awards");
                    f.ui.NotifyCommitted(command); Check(f.ui.CommittedCueCount==cue,"Recovered duplicate "+command+" cannot replay");
                }
            }
            using(var f=new Fixture()) {
                f.Bind(); f.Command("volume:0"); f.game.GenerateForSmoke(4);
                Check(f.game.CurrentSession.TotalVirtualCards==11 && f.ui.CommittedCueCount==0 && f.go.GetComponents<AudioSource>().Length==0,"Volume zero suppresses acknowledged generation without blocking durable gameplay");
                f.Command("volume:0.5"); f.ui.NotifyCommitted("generate");
                Check(f.ui.CommittedCueCount==0,"Unmute does not replay consumed muted acknowledgement");
                f.Command("open"); var source=f.ui.FoleySource;
                Check(f.ui.CommittedCueCount==1 && source.clip==f.Library.Sound("open"),"Subsequent eligible unmuted action uses real library cue");
                f.Command("pause"); f.Command("next");
                Check(source.clip==null && !source.isPlaying && f.ui.CommittedCueCount==1 && f.game.CurrentView.revealedCount==0,"Controller pause immediately stops audio and prevents progress/cue");
                f.Command("resume"); f.ui.NotifyCommitted("open");
                Check(source.clip==null && f.ui.CommittedCueCount==1,"Resume and duplicate do not restart sound");
                f.Command("next"); f.Command("volume:0");
                Check(source.clip==null && !source.isPlaying && source.volume==0 && f.ui.CommittedCueCount==2,"Immediate mute stops and clears an already requested native cue");
                f.Command("volume:0.5"); f.Command("next"); f.ui.Media=null;
                Check(source.clip==null && !source.isPlaying && f.Library.Sound("next")!=null,"Provider detach stops audio without disposing host-owned library");
                f.ui.Media=f.Library; f.ui.NotifyCommitted("next"); Check(f.ui.CommittedCueCount==3,"Provider rebind never replays consumed acknowledgement");
            }
        }
        private static void FallbackAndGuards() {
            foreach(string defect in new[]{"path","hash","identity","mask","malformed"}) using(var f=new Fixture()) {
                string id=Holo(f); var face=Asset(f.directory,"card-face",id,Png(),".png");
                var mask=Asset(f.directory,"holo-mask",id,Png(126,176),".png");
                if(defect=="path") face.relativePath=".artifacts/../Saves/not-read.png";
                if(defect=="hash") face.sha256=new string('0',64);
                if(defect=="identity") face.cardId="pkmn-not-an-existing-printing";
                WriteManifest(f.directory,face,mask);
                if(defect=="malformed") File.WriteAllText(Path.Combine(f.directory,"media-manifest.json"),"{broken synthetic fixture");
                f.Bind(); var library=f.Library;
                Check(library!=null && library.Warnings.Count>0 && (bool)Get(f.host,"UsesTestManifest") && library.HoloMask(id)==null && library.SuppliedAssetCount==(defect=="mask"?1:0),"Host preserves safe library warnings and independent fallback for synthetic "+defect);
                Check((defect=="mask")==(library.CardFace(id)!=null) && library.Sound("generate")!=null && library.Sound("generate").name.StartsWith("Original generic foley"),"Bad "+defect+" retains generic original foley without wrong identity or full-card mask");
                f.game.GenerateForSmoke(4); f.Command("open"); f.Command("next"); f.Command("skip");
                Check(f.game.GeneratedResultCount==1 && f.game.CurrentSession.TotalVirtualCards==11 && f.game.CurrentView.stage=="results" && f.ui.CommittedCueCount==4,"Optional "+defect+" error never blocks actual pack flow or causes reroll");
                Check(!((string)Get(f.host,"StatusMessage")).Contains("DO-NOT-LEAK") && !((string)Get(f.host,"StatusMessage")).Contains("https"),"Optional "+defect+" status contains no source token");
            }
            using(var f=new Fixture()) {
                WriteManifest(f.directory,Asset(f.directory,"sound-generate","",Wave(),".wav"));
                var smoke=f.game.Paths;
                // Pure normal policy only, after safe smoke initialization; never initialize/open normal storage.
                var normal=GamePaths.Resolve(root,Application.dataPath,true,new string[0]);
                typeof(OpeningGame).GetProperty("Paths").GetSetMethod(true).Invoke(f.game,new object[]{normal});
                try {
                    f.Bind(); Check(!(bool)Get(f.host,"UsesTestManifest") && f.Library.SuppliedAssetCount==0 && f.Library.Sound("generate").name.StartsWith("Original generic foley"),"Real normal GamePaths ignores existing test override without touching live storage");
                } finally { typeof(OpeningGame).GetProperty("Paths").GetSetMethod(true).Invoke(f.game,new object[]{smoke}); }
            }
            using(var f=new Fixture(null,false)) {
                f.Bind(); Invoke(f.host,"Start"); Invoke(f.host,"Initialize",new object[]{null});
                Check(f.Library==null && f.game.Paths==null && f.game.CurrentSession==null && f.game.GeneratedResultCount==0,"Host Start and null/uninitialized guard never initialize controller or load media");
            }
            using(var f=new Fixture()) using(var other=new Fixture()) {
                f.host=f.go.AddComponent(hostType); Invoke(f.host,"Initialize",other.game);
                Check(f.Library==null && f.ui.Media==null,"Host rejects foreign GameObject controller");
            }
            string directory,saved;
            using(var f=new Fixture()) { f.game.GenerateForSmoke(4); directory=f.directory; saved=File.ReadAllText(Path.Combine(directory,"saves","state.save")); }
            string save=Path.Combine(directory,"saves","state.save"); File.WriteAllText(save,"VTCG-SAVE-999\nunsupported synthetic fixture bytes retained");
            using(var f=new Fixture(directory)) {
                f.Bind(); Check(f.game.CurrentView.stage=="error" && f.game.CurrentSession==null && f.Library==null,"Failed storage initialization blocks optional load until actual usable initialization");
                File.WriteAllText(save,saved); f.Command("retry"); Invoke(f.host,"Initialize",f.game);
                Check(f.Library!=null && f.game.CurrentView.stage=="sealed" && f.game.GeneratedResultCount==0 && f.game.CurrentSession.TotalVirtualCards==11 && f.ui.CommittedCueCount==0,"Explicit host Initialize after real initial Retry binds silently without reroll or reaward");
            }
        }
        private static void ReviewFixPaths() {
            using(var f=new Fixture()) {
                string leaf=Path.Combine(f.directory,"media-manifest.json");
                string target=Path.Combine(fixtureRoot,"original-synthetic-local-target.json");
                File.WriteAllText(target,"{\"assets\":[]}");
                var start=new System.Diagnostics.ProcessStartInfo("cmd.exe","/c mklink \""+leaf+"\" \""+target+"\"") {
                    UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
                };
                using(var process=System.Diagnostics.Process.Start(start)) {
                    string output=process.StandardOutput.ReadToEnd()+process.StandardError.ReadToEnd();
                    process.WaitForExit(); Check(process.ExitCode==0,"Create artifact-only local file symlink: "+output.Trim());
                }
                Check((File.GetAttributes(leaf)&FileAttributes.ReparsePoint)!=0,"Fixture is an actual reparse leaf");
                bool rejected=false;
                try { MediaFiles.Resolve(root,leaf.Substring(root.Length+1).Replace('\\','/'),true); }
                catch(InvalidDataException) { rejected=true; }
                Check(rejected,"Actual resolver rejects local-target leaf symlink");
                f.Bind();
                Check(!(bool)Get(f.host,"UsesTestManifest"),"Unsafe optional override is rejected before selection");
                Check(f.Library!=null && ReferenceEquals(f.Library,f.ui.Media) && f.Library.Warnings.Count==0 && f.Library.SuppliedAssetCount==0 && f.Library.Sound("generate")!=null,"Unsafe override still binds safe empty production manifest and original foley");
                var select=hostType.GetMethod("OptionalManifest",BindingFlags.Static|BindingFlags.NonPublic);
                Check(select!=null,"Private optional selection seam exists without public API change");
                int probes=0;
                Func<string,bool> exists=path=>{probes++; return File.Exists(path);};
                Func<string> selection=()=> (string)select.Invoke(null,new object[]{root,f.directory,exists});
                Check(selection()==null && probes==0,"Unsafe reparse leaf never reaches existence delegate");
                File.Delete(leaf);
                probes=0;
                Check(selection()==null && probes==1,"Missing ordinary override validates then probes and falls back");
                Directory.CreateDirectory(leaf); probes=0;
                Check(selection()==null && probes==1,"Ordinary directory override validates then falls back as not a file");
                Directory.Delete(leaf);
                File.WriteAllText(leaf,"{\"assets\":[]}"); probes=0;
                Check(selection()==leaf.Substring(root.Length+1).Replace('\\','/') && probes==1,"Ordinary override validates then selects existing file");
            }
        }
        private static Fixture playing, recoveryOwner;
        private static AudioClip destroyedClip;
        private static int playStep, playFrame;
        private static double deadline;
        private static bool oldPlayOptionsEnabled;
        private static EnterPlayModeOptions oldPlayOptions;
        private static string resumedDirectory, resumedTransaction;
        private static void BeginPlay() {
            oldPlayOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled; oldPlayOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
            deadline=EditorApplication.timeSinceStartup+60; playStep=0;
            EditorApplication.update+=PlayTick;
            EditorApplication.EnterPlaymode();
        }
        private static void PlayTick() {
            try {
                if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Native Play Mode integration timeout at step "+playStep);
                if(!Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode!=EditorApplication.isPlaying) return;
                if(playStep==0) {
                    Check(Application.isPlaying,"Actual native Editor Play Mode entered without player launch");
                    playing=new Fixture(); playing.go.AddComponent<AudioListener>();
                    playing.host=playing.go.AddComponent(hostType); playFrame=Time.frameCount; playStep=1; return;
                }
                if(Time.frameCount<=playFrame+1) return;
                if(playStep==1) {
                    var f=playing; var g=f.game; var library=f.Library;
                    Check(library!=null && ReferenceEquals(f.ui.Media,library) && f.ui.CommittedCueCount==0 && f.go.GetComponents<AudioSource>().Length==0,"Actual automatic host Start finds initialized same-GO controller and binds with zero startup audio");
                    // An explicit synthetic background-focus callback removes editor-window focus from the engine oracle.
                    Invoke(f.ui,"OnApplicationFocus",true); if(g.CurrentView.paused) f.Command("resume");
                    g.GenerateForSmoke(4); var source=f.ui.FoleySource;
                    Check(source.isPlaying && source.clip==library.Sound("generate") && f.ui.CommittedCueCount==1,"Real native AudioSource isPlaying after actual durable generate acknowledgement");
                    f.Command("pause"); Check(!source.isPlaying && source.clip==null,"Real playing original cue stops immediately on controller pause");
                    f.Command("resume"); Check(!source.isPlaying && f.ui.CommittedCueCount==1,"Actual controller Resume remains silent");
                    f.Command("open"); Check(source.isPlaying && f.ui.CommittedCueCount==2 && source.clip==library.Sound("open"),"Real native AudioSource plays original wrapper cue once");
                    f.Command("next"); Check(source.isPlaying && f.ui.CommittedCueCount==3 && source.clip==library.Sound("next"),"Real native AudioSource plays original Next cue once");
                    g.FailNextCommitForSmoke(); f.Command("skip");
                    Check(!source.isPlaying && source.clip==null && g.CurrentView.stage=="error" && f.ui.CommittedCueCount==3,"Actual prepared Skip failure stops native playback and suppresses unacknowledged cue");
                    f.Command("retry"); f.ui.NotifyCommitted("skip");
                    Check(source.isPlaying && source.clip==library.Sound("skip") && f.ui.CommittedCueCount==4 && g.GeneratedResultCount==1 && g.CurrentSession.TotalVirtualCards==11,"Actual prepared Retry plays exactly one original Skip cue without reroll or reaward");
                    f.Command("volume:0"); Check(!source.isPlaying && source.clip==null && source.volume==0,"Actual mute stops currently playing native original cue");
                    g.GenerateForSmoke(4); Check(!source.isPlaying && f.ui.CommittedCueCount==4 && g.CurrentSession.TotalVirtualCards==22,"Muted subsequent committed generation remains silent while pack flow stays playable");
                    f.Command("volume:0.5"); f.ui.NotifyCommitted("generate"); Check(!source.isPlaying && f.ui.CommittedCueCount==4,"Actual unmute cannot replay muted generation");
                    f.Command("open"); Check(source.isPlaying && f.ui.CommittedCueCount==5,"Unmuted subsequent original Open plays normally");
                    destroyedClip=library.Sound("open");
                    UnityEngine.Object.Destroy(f.host); playFrame=Time.frameCount; playStep=2; return;
                }
                if(playStep==2) {
                    var f=playing;
                    Check(f.host==null && f.ui.Media==null && f.ui.FoleySource.clip==null && !f.ui.FoleySource.isPlaying && destroyedClip==null,"Real deferred component OnDestroy detaches/stops before deferred owned clip destruction");
                    f.host=null; resumedDirectory=f.directory; resumedTransaction=f.game.CurrentSession.CurrentPack.TransactionId;
                    f.Dispose(); playing=new Fixture(resumedDirectory); playing.host=playing.go.AddComponent(hostType);
                    playFrame=Time.frameCount; playStep=3; return;
                }
                if(playStep==3) {
                    var f=playing;
                    Check(f.Library!=null && f.game.CurrentSession.CurrentPack.TransactionId==resumedTransaction && f.game.CurrentSession.TotalVirtualCards==22 && f.game.GeneratedResultCount==0 && f.ui.CommittedCueCount==0 && f.go.GetComponents<AudioSource>().Length==0,"Real Play Mode fresh controller resume and automatic host Start are silent and never reaward");
                    f.Dispose(); recoveryOwner=new Fixture(resumedDirectory); playing=new Fixture(resumedDirectory); playing.host=playing.go.AddComponent(hostType);
                    playFrame=Time.frameCount; playStep=4; return;
                }
                if(playStep==4) {
                    Check(playing.game.CurrentView.stage=="error" && playing.game.CurrentSession==null && playing.Library==null,"Actual initially locked controller and automatic host Start load no optional media");
                    recoveryOwner.Dispose(); recoveryOwner=null; playing.Command("retry");
                    Check(playing.game.CurrentView.stage=="revealing" && playing.game.CurrentSession.TotalVirtualCards==22,"Real initial Retry recovers exact saved session after writer owner releases lock");
                    playFrame=Time.frameCount; playStep=5; return;
                }
                if(playStep==5) {
                    Check(playing.Library!=null && playing.ui.CommittedCueCount==0 && playing.game.GeneratedResultCount==0 && playing.go.GetComponents<AudioSource>().Length==0,"Host automatically attaches silently after initially blocked controller becomes usable");
                    EndPlay(null);
                }
            } catch(Exception e) { Debug.LogError(e); EndPlay(e.ToString()); }
        }
        private static void EndPlay(string failure) {
            EditorApplication.update-=PlayTick;
            if(playing!=null) { playing.Dispose(); playing=null; }
            if(recoveryOwner!=null) { recoveryOwner.Dispose(); recoveryOwner=null; }
            EditorSettings.enterPlayModeOptions=oldPlayOptions; EditorSettings.enterPlayModeOptionsEnabled=oldPlayOptionsEnabled;
            EditorApplication.ExitPlaymode();
            Finish(failure);
        }
        public static void Run() {
            checks.Clear(); string failure=null;
            root=Path.GetDirectoryName(Application.dataPath);
            fixtureRoot=Path.Combine(root,".artifacts","phase3-host","fixtures-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fixtureRoot);
            try {
                hostType=typeof(OpeningGame).Assembly.GetType("VintageCollector.Runtime.Phase3MediaHost");
                if(Environment.GetEnvironmentVariable("VCC_HOST_SLICE")=="play") { BeginPlay(); return; }
                Binding();
                string slice=Environment.GetEnvironmentVariable("VCC_HOST_SLICE");
                if(slice=="review-fix") ReviewFixPaths();
                else if(slice!="binding") Override();
                if(slice=="lifecycle" || slice=="all") Lifecycle();
                if(slice=="all") { CoreFlow(); RetryAndSuppression(); FallbackAndGuards(); }
            } catch(Exception e) { failure=e.ToString(); Debug.LogError(failure); }
            Finish(failure);
        }
        private static void Finish(string failure) {
            string path=Environment.GetEnvironmentVariable("VCC_HOST_REPORT");
            if(string.IsNullOrEmpty(path) || !Path.GetFullPath(path).StartsWith(Path.Combine(root,".artifacts","phase3-host")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new Exception("Explicit project-local report required");
            File.WriteAllText(path,JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),failure=failure,fixtureRoot=fixtureRoot},true));
            Debug.Log("PHASE3_HOST "+(failure==null?"PASS":"FAIL")+" count="+checks.Count);
            if(Application.isBatchMode) EditorApplication.Exit(failure==null?0:1);
            else if(failure!=null) throw new Exception(failure);
        }
    }
}
