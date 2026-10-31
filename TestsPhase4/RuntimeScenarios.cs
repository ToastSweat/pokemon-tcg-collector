using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Collections;
using VintageCollector.Domain;
using VintageCollector.Persistence;
using VintageCollector.Presentation;

namespace VintageCollector.EditorChecks {
 public static class RuntimeScenarios {
  static string output,root; static int sequence;
  static void R(bool value,string why="Invariant failed") { if(!value) throw new Exception(why); }
  static object Field(object value,string name) {return value.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);}
  static void Tick(Phase4CollectionHost h) { typeof(Phase4CollectionHost).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(h,null); }
  sealed class Fixture : IDisposable {
   public readonly GameObject Object; public readonly OpeningGame Game; public readonly Phase4CollectionHost Host; public readonly string Save;
   public Fixture(string existing=null,bool initialize=true) {
    Save=existing??Path.Combine(output,"fixture-"+(++sequence));Directory.CreateDirectory(Save);
    Object=new GameObject("isolated-runtime-check");Game=Object.AddComponent<OpeningGame>();
    Game.allowedProjectRoot=root;
    Game.catalogJson=new TextAsset(File.ReadAllText(Path.Combine(Application.dataPath,"Data/base-en-unlimited1999.catalog.json")));
    Game.modelJson=new TextAsset(File.ReadAllText(Path.Combine(Application.dataPath,"Data/base-en-unlimited1999.model-a-v0.1.json")));
    Host=Object.AddComponent<Phase4CollectionHost>();if(initialize) Init();
   }
   public void Init() {Host.Initialize(new[]{"--vcc-smoke=ready","--vcc-save-root="+Save,"--vcc-artifacts="+Save});}
   public void C(string command) {Host.DispatchBinderCommand(command);}
   public void PhysicalDraft(string qty="4") {C("binder-open");C("mode:physical");C("select:"+Host.View.rows[0].id);C("quantity:"+qty);}
   public void Dispose() { Host.Shutdown();UnityEngine.Object.DestroyImmediate(Game.catalogJson);UnityEngine.Object.DestroyImmediate(Game.modelJson);UnityEngine.Object.DestroyImmediate(Object); }
  }
  public static void Run(string path,Action<string,Action> test) {
   output=path;root=Directory.GetParent(Directory.GetParent(Directory.GetParent(path).FullName).FullName).FullName;
   test("startup native host is ready, target 101, no writes/generation/audio",()=> {using(var f=new Fixture()) {
    R(f.Host.IsReady,f.Host.StatusMessage);R(f.Host.View.targetCount==101 && f.Host.View.rows.Length==101);R(f.Game.GeneratedResultCount==0 && f.Game.CurrentSession.TotalVirtualCards==0 && f.Host.Physical.Revision==0);
    R(!File.Exists(Path.Combine(f.Save,"state.save")) && !File.Exists(Path.Combine(f.Save,"physical.save")) && !Directory.Exists(Path.Combine(f.Save,"Backups")));
    R(f.Object.GetComponents<AudioSource>().Length==0);
   }});
   test("host owns collection/container/session/physical leases until shutdown",()=> {using(var f=new Fixture()) {
    var catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(f.Game.catalogJson.text));bool c=false,s=false,p=false;
    try {using(var x=new CollectionProfiles(f.Save)) {}} catch(IOException) {c=true;}
    try {using(var x=new SessionStore(f.Save,new UnityStateSerializer(),catalog)) {}} catch(IOException) {s=true;}
    try {using(var x=new PhysicalStore(f.Save,new UnityPhysicalSerializer(),catalog)) {}} catch(IOException) {p=true;}
    R(c&&s&&p);f.Host.Shutdown();using(var x=new CollectionProfiles(f.Save)) {}using(var x=new SessionStore(f.Save,new UnityStateSerializer(),catalog)) {}using(var x=new PhysicalStore(f.Save,new UnityPhysicalSerializer(),catalog)) {}
   }});
   test("idle Update retains exact published View reference and UI latch",()=> {using(var f=new Fixture()) {var v=f.Host.View;Tick(f.Host);Tick(f.Host);R(ReferenceEquals(v,f.Host.View));}});
   test("binder opening no reveal/generation/write and closed commands rejected",()=> {using(var f=new Fixture()) {
    long r=f.Game.CurrentSession.Revision;f.C("mode:physical");R(f.Host.View.mode=="virtual");f.C("binder-open");f.Game.DispatchCommand("generate");f.Game.GenerateForSmoke(71);R(f.Game.CurrentSession.Revision==r && f.Game.GeneratedResultCount==0);f.C("binder-close");R(f.Game.CurrentView.stage=="ready");
   }});
   test("binder preserves prior paused state exactly",()=> {using(var f=new Fixture()) {f.Game.DispatchCommand("pause");var v=f.Game.CurrentView;f.C("binder-open");f.C("binder-close");R(ReferenceEquals(v,f.Game.CurrentView)&&f.Game.CurrentView.paused);f.Game.DispatchCommand("resume");R(!f.Game.CurrentView.paused);}});
   test("binder preserves revealed inspection context",()=> {using(var f=new Fixture()) {f.Game.GenerateForSmoke(72);f.Game.DispatchCommand("open");f.Game.DispatchCommand("next");f.Game.DispatchCommand("inspect:0");var v=f.Game.CurrentView;f.C("binder-open");f.C("binder-close");R(ReferenceEquals(v,f.Game.CurrentView)&&v.inspectIndex==0);}});
   test("generate durable awards hidden from binder ownership/filter/search",()=> {using(var f=new Fixture()) {
    f.Game.GenerateForSmoke(4);Tick(f.Host);R(f.Game.CurrentSession.TotalVirtualCards==11 && f.Host.View.totalQuantity==0 && f.Host.Physical.Holdings.Count==0);
    f.Game.DispatchCommand("open");f.Game.DispatchCommand("next");Tick(f.Host);
    f.C("binder-open");f.C("filter:owned");R(f.Host.View.rows.Length==1&&f.Host.View.totalQuantity==1);
    f.C("search:Alakazam");R(f.Host.View.rows.Length==0&&f.Host.View.uniqueOwned==1);
    f.C("search:1");R(f.Host.View.rows.Length==0&&f.Host.View.totalQuantity==1);
    f.C("search:Voltorb");R(f.Host.View.rows.Length==1&&f.Host.View.rows[0].quantity==1);
   }});
   test("actual wrapper/reveal refreshes only revealed-prefix projection",()=> {using(var f=new Fixture()) {
    f.Game.GenerateForSmoke(74);f.Game.DispatchCommand("open");
    for(int n=1;n<=11;n++) {
     f.Game.DispatchCommand("next");Tick(f.Host);R(f.Host.View.totalQuantity==n&&f.Host.Physical.Revision==0);
     foreach(var row in f.Host.View.rows) R(row.quantity==f.Game.CurrentSession.CurrentPack.OrderedCardIds.Take(n).Count(id=>id==row.id),"Per-printing revealed prefix mismatch");
    }
   }});
   test("normal card11 final hold survives binder open close with no new awards",()=> {using(var f=new Fixture()) {
    f.Game.GenerateForSmoke(75);f.Game.DispatchCommand("open");for(int i=0;i<11;i++) f.Game.DispatchCommand("next");R(f.Game.CurrentView.stage=="revealing"&&f.Game.CurrentView.revealedCount==11);
    long revision=f.Game.CurrentSession.Revision;var tx=f.Game.CurrentView.transactionId;f.C("binder-open");f.C("binder-close");f.Game.DispatchCommand("generate");R(f.Game.CurrentView.stage=="revealing"&&f.Game.CurrentSession.Revision==revision&&f.Game.CurrentView.transactionId==tx&&f.Game.GeneratedResultCount==1);
    f.Game.DispatchCommand("results");R(f.Game.CurrentView.stage=="results");
   }});
   test("Skip results preserves exactly once award protocol",()=> {using(var f=new Fixture()) {f.Game.GenerateForSmoke(76);f.Game.DispatchCommand("open");f.Game.DispatchCommand("skip");long r=f.Game.CurrentSession.Revision;f.C("binder-open");f.C("binder-close");f.Game.DispatchCommand("skip");R(f.Game.CurrentView.stage=="results"&&f.Game.CurrentSession.TotalVirtualCards==11&&f.Game.CurrentSession.Revision==r);}});
   foreach(string bad in new[]{"mode:all","filter:secret","page:-1","page:01","page:999999999999","condition:Near-mint","select:1","select:pkmn-fake","backup-select:-1","save-virtual"}) {
    string command=bad;test("host rejects untrusted command "+bad,()=> {using(var f=new Fixture()) {f.PhysicalDraft();var v=f.Host.View;long r=f.Host.Physical.Revision;f.C(command);R(ReferenceEquals(v,f.Host.View)&&f.Host.Physical.Revision==r,command);}});
   }
   test("safe projected selection context cannot be forged through mutable DTO",()=> {using(var f=new Fixture()) {
    f.PhysicalDraft();f.C("filter:missing");f.C("search:no-match");R(f.Host.View.selectedCardId==null);f.Host.View.selectedCardId=f.Game.catalogJson.name;f.Host.View.rows=new[]{new BinderCardView {id="forged"}};f.C("select:forged");f.C("save-physical");R(f.Host.Physical.Revision==0);
   }});
   test("manual physical ownership before virtual pull commits native adapter and reopens",()=> {
    string save;string id;using(var f=new Fixture()) {save=f.Save;f.PhysicalDraft("7");id=f.Host.View.selectedCardId;f.C("condition:near-mint");f.C("quantity:7");f.C("save-physical");R(f.Host.Physical.Revision==1&&f.Host.Physical.Holdings[0].Quantity==7&&f.Game.CurrentSession.TotalVirtualCards==0);}
    using(var f=new Fixture(save)) {R(f.Host.IsReady&&f.Host.Physical.Revision==1&&f.Host.Physical.Holdings[0].CardId==id&&f.Host.Physical.Holdings[0].Condition=="near-mint");}
   });
   foreach(string bad in new[]{"","01","+1","-1"," 1","1 ","1.0","100000","999999999999999999","١"}) {
    string value=bad;test("canonical physical quantity rejects ["+bad+"]",()=> {using(var f=new Fixture()) {f.PhysicalDraft(value);f.C("save-physical");R(f.Host.Physical.Revision==0&&!string.IsNullOrEmpty(f.Host.View.error));}});
   }
   test("condition buckets absolute zero removes only selected bucket",()=> {using(var f=new Fixture()) {
    f.PhysicalDraft("2");f.C("save-physical");f.C("condition:damaged");f.C("quantity:3");f.C("save-physical");R(f.Host.View.totalQuantity==5&&f.Host.View.uniqueOwned==1);f.C("quantity:0");f.C("save-physical");R(f.Host.Physical.Holdings.Count==1&&f.Host.View.totalQuantity==2);
   }});
   test("physical durable preparation failure retains exact identity UTC absolute intent",()=> {using(var f=new Fixture()) {
    f.PhysicalDraft("9");f.Host.FailNextPhysicalCommitForSmoke();f.C("save-physical");R(f.Host.View.pendingPhysicalEdit&&f.Host.Physical.Revision==0);
    var pending=(PhysicalEditData)Field(f.Host,"pendingEdit");string id=pending.transactionId,utc=pending.createdUtc;
    f.C("quantity:5");f.C("mode:virtual");f.C("backup");f.C("import-preview");R(f.Host.View.quantityText=="9"&&f.Host.View.mode=="physical"&&ReferenceEquals(pending,Field(f.Host,"pendingEdit")));
    R(!Directory.Exists(Path.Combine(f.Save,"Backups")));f.C("binder-close");f.Game.DispatchCommand("generate");R(f.Game.GeneratedResultCount==0);f.C("binder-open");R(BinderLayoutRules.CanEmit(f.Host.View,"save-physical"));f.C("save-physical");
    R(!f.Host.View.pendingPhysicalEdit&&f.Host.Physical.Revision==1&&f.Host.Physical.Holdings[0].Quantity==9);
    var json=File.ReadAllText(Path.Combine(f.Save,"physical.save"));R(json.Contains(id)&&json.Contains(utc));
    f.C("binder-close");f.Game.GenerateForSmoke(77);R(f.Game.GeneratedResultCount==1);
   }});
   test("opening error blocks backup until identical runtime Retry acknowledges",()=> {using(var f=new Fixture()) {
    f.Game.FailNextCommitForSmoke();f.Game.GenerateForSmoke(78);R(f.Game.HasPendingStorage);f.C("binder-open");f.C("backup");R(!Directory.Exists(Path.Combine(f.Save,"Backups")));f.C("binder-close");f.Game.DispatchCommand("retry");R(!f.Game.HasPendingStorage&&f.Game.GeneratedResultCount==1&&f.Game.CurrentSession.TotalVirtualCards==11);
   }});
   foreach(string request in new[]{"backup","import-preview"}) {
    string requested=request;test("rejected UI "+request+" acknowledges request latch",()=> {using(var f=new Fixture()) {
     f.C("binder-open");f.C("backup");f.C("backup-select:0");f.C("binder-close");
     f.Game.FailNextCommitForSmoke();f.Game.GenerateForSmoke(78);f.C("binder-open");R(f.Game.HasPendingStorage);
     var renderer=f.Object.GetComponent<BinderPresentation>();var before=f.Host.View;
     typeof(BinderPresentation).GetMethod("RequestCommand",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(renderer,new object[]{requested});
     R(!(bool)Field(renderer,"pending"),"Rejected request remained REQUEST PENDING");
     R(!ReferenceEquals(before,f.Host.View)&&f.Game.HasPendingStorage&&f.Host.View.backups.Length==1);
     f.C("binder-close");f.Game.DispatchCommand("retry");f.C("binder-open");f.C("backup");
     R(f.Host.View.backups.Length==2&&!f.Game.HasPendingStorage);
    }});
   }
   test("forged review UI is rejected with acknowledged renderer latch",()=> {using(var f=new Fixture()) {
    f.C("binder-open");f.Host.View.importSummary="Untrusted review text";
    var renderer=f.Object.GetComponent<BinderPresentation>();
    typeof(BinderPresentation).GetMethod("RequestCommand",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(renderer,new object[]{"import-confirm"});
    R(!(bool)Field(renderer,"pending")&&f.Host.View.importSummary==null&&!Directory.Exists(Path.Combine(f.Save,"Profiles")));
   }});
   test("failed import preview preserves physical draft selection and sanitizes paths",()=> {using(var f=new Fixture(null,false)) {
    Directory.CreateDirectory(Path.Combine(f.Save,"Backups"));File.WriteAllText(Path.Combine(f.Save,"Backups/bad.zip"),"bad");f.Init();f.PhysicalDraft("31");string id=f.Host.View.selectedCardId;f.C("backup-select:0");f.C("import-preview");R(f.Host.View.quantityText=="31"&&f.Host.View.selectedCardId==id&&f.Host.View.selectedBackupIndex==0&&f.Host.View.importSummary==null&&!string.IsNullOrEmpty(f.Host.View.error));R(!f.Host.View.error.Contains(root));
   }});
   test("real schema1 backup preview confirm restart new profile and old bytes preserved",()=> {
    string save,oldSession,oldPhysical,tx;using(var f=new Fixture()) {
     save=f.Save;f.Game.GenerateForSmoke(79);f.Game.DispatchCommand("open");f.Game.DispatchCommand("next");tx=f.Game.CurrentView.transactionId;
     f.PhysicalDraft("6");f.C("save-physical");f.C("backup");R(f.Host.View.backups.Length==1);oldSession=File.ReadAllText(Path.Combine(save,"state.save"));oldPhysical=File.ReadAllText(Path.Combine(save,"physical.save"));
     f.C("backup-select:0");f.C("import-preview");R(!string.IsNullOrEmpty(f.Host.View.importSummary)&&!Directory.Exists(Path.Combine(save,"Profiles")));
     f.C("import-confirm");R(f.Host.View.restartRequired&&f.Game.Paths.CollectionProfileId==""&&f.Host.Profiles.ActiveProfileId=="");f.C("binder-close");long r=f.Game.CurrentSession.Revision;f.Game.DispatchCommand("next");f.Game.DispatchCommand("generate");f.Game.GenerateForSmoke(80);R(f.Game.CurrentSession.Revision==r&&f.Game.GeneratedResultCount==1);
     R(File.ReadAllText(Path.Combine(save,"state.save"))==oldSession&&File.ReadAllText(Path.Combine(save,"physical.save"))==oldPhysical);
    }
    using(var f=new Fixture(save)) {R(f.Host.IsReady&&f.Game.Paths.CollectionProfileId.Length==32&&f.Game.Paths.BaseSaveDirectory==save&&f.Game.CurrentView.transactionId==tx&&f.Game.CurrentView.revealedCount==1&&f.Host.Physical.Holdings[0].Quantity==6);f.Game.DispatchCommand("next");R(f.Game.CurrentView.revealedCount==2);var media=f.Object.AddComponent<Phase3MediaHost>();media.Initialize(f.Game);R(media.Library!=null,media.StatusMessage);media.Shutdown();}
    R(File.ReadAllText(Path.Combine(save,"state.save"))==oldSession&&File.ReadAllText(Path.Combine(save,"physical.save"))==oldPhysical);
   });
   test("cancel releases exact service-owned preview without activation",()=> {using(var f=new Fixture()) {
    f.C("binder-open");f.C("backup");f.C("backup-select:0");f.C("import-preview");
    var preview=(ImportPreview)Field(f.Host,"reviewed");f.C("import-cancel");bool rejected=false;
    try {f.Host.Profiles.ConfirmImport(preview);} catch(ArgumentException) {rejected=true;}
    R(rejected&&f.Host.View.importSummary==null&&!Directory.Exists(Path.Combine(f.Save,"Profiles")),"Cancelled preview remains confirmable in service");
   }});
   test("mutated review summary index and backup list cannot replace owned preview",()=> {
    string save;using(var f=new Fixture()) {
     save=f.Save;f.PhysicalDraft("7");f.C("save-physical");f.C("backup");string a=f.Host.View.backups[0];
     f.C("quantity:9");f.C("save-physical");f.C("backup");int index=Array.IndexOf(f.Host.View.backups,a);R(index>=0&&f.Host.View.backups.Length==2);
     f.C("backup-select:"+index);f.C("import-preview");var preview=(ImportPreview)Field(f.Host,"reviewed");R(preview.PhysicalCardCount==7);
     f.Host.View.importSummary="Untrusted backup B, quantity9";f.Host.View.selectedBackupIndex=1-index;Array.Reverse(f.Host.View.backups);
     f.C("import-confirm");R(f.Host.View.restartRequired&&f.Host.Physical.Holdings[0].Quantity==9);
    }
    using(var f=new Fixture(save)) R(f.Host.IsReady&&f.Host.Physical.Holdings[0].Quantity==7,"Mutable display replaced reviewed archive A");
   });
   test("uncertain import ack retry uses same owned receipt no duplicate profile",()=> {using(var f=new Fixture()) {
    f.PhysicalDraft("8");f.C("save-physical");f.C("backup");f.C("backup-select:0");f.C("import-preview");var preview=Field(f.Host,"reviewed");f.Host.FailNextImportForSmoke();f.C("import-confirm");R(f.Host.View.pendingImportConfirm&&!f.Host.View.restartRequired&&ReferenceEquals(preview,Field(f.Host,"reviewed")));
    f.C("import-cancel");f.C("quantity:22");f.C("import-preview");f.C("backup");R(ReferenceEquals(preview,Field(f.Host,"reviewed"))&&f.Host.View.quantityText=="8");f.C("binder-close");f.Game.DispatchCommand("generate");R(f.Game.GeneratedResultCount==0);f.C("binder-open");R(BinderLayoutRules.CanEmit(f.Host.View,"import-confirm"));f.C("import-confirm");R(f.Host.View.restartRequired&&!f.Host.View.pendingImportConfirm&&Directory.GetDirectories(Path.Combine(f.Save,"Profiles")).Length==1);
   }});
   test("bad pointer startup blocks default Start Initialize and terminal Retry",()=> {using(var f=new Fixture(null,false)) {
    File.WriteAllText(Path.Combine(f.Save,"active-profile"),"corrupt-pointer");f.Init();R(!f.Host.IsReady&&f.Game.CurrentSession==null&&f.Game.CurrentView.stage=="error");
    f.Game.Initialize(new[]{"--vcc-smoke=ready","--vcc-save-root="+f.Save,"--vcc-artifacts="+f.Save});f.Game.DispatchCommand("retry");f.Game.DispatchCommand("generate");R(f.Game.CurrentSession==null&&!File.Exists(Path.Combine(f.Save,"state.save"))&&!File.Exists(Path.Combine(f.Save,"physical.save"))&&!f.Game.CurrentView.errorMessage.Contains(root));
    using(var profilesLease=new FileStream(Path.Combine(f.Save,"collection.lock"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {}
   }});
   test("bad physical store startup never falls back blank and disposes session lease",()=> {using(var f=new Fixture(null,false)) {
    File.WriteAllText(Path.Combine(f.Save,"physical.save"),"corrupt-physical");f.Init();R(!f.Host.IsReady&&f.Game.CurrentView.stage=="error");f.Game.DispatchCommand("retry");R(f.Game.CurrentSession==null||f.Game.CurrentSession.TotalVirtualCards==0);R(File.ReadAllText(Path.Combine(f.Save,"physical.save"))=="corrupt-physical");using(var lease=new FileStream(Path.Combine(f.Save,"state.lock"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {}
   }});
   test("explicit backup reconciliation preserves ordinary final table and committed cues",()=> {using(var f=new Fixture()) {
    f.Game.GenerateForSmoke(86);f.Game.DispatchCommand("open");for(int i=0;i<11;i++)f.Game.DispatchCommand("next");long revision=f.Game.CurrentSession.Revision;string tx=f.Game.CurrentView.transactionId;var table=f.Object.GetComponent<TabletopPresentation>();int cues=table.CommittedCueCount;
    f.C("binder-open");f.C("backup");R(f.Host.View.backups.Length==1);f.C("binder-close");R(f.Game.CurrentView.stage=="revealing"&&f.Game.CurrentView.revealedCount==11&&f.Game.CurrentView.transactionId==tx&&f.Game.CurrentSession.Revision==revision&&f.Game.GeneratedResultCount==1&&table.CommittedCueCount==cues);
   }});
   foreach(string prefix in new[]{"state","physical"}) {
    string kind=prefix;test("TG05 first backup rejects disappeared acknowledged default "+kind+" files",()=> {using(var f=new Fixture()) {
     if(kind=="state") {f.Game.GenerateForSmoke(85);f.Game.DispatchCommand("open");f.Game.DispatchCommand("next");f.C("binder-open");}else {f.PhysicalDraft("4");f.C("save-physical");}
     long sessionRevision=f.Game.CurrentSession.Revision,physicalRevision=f.Host.Physical.Revision;int generated=f.Game.GeneratedResultCount;
     string retained=Path.Combine(output,"tg05-retained-"+kind);Directory.CreateDirectory(retained);
     foreach(string suffix in new[]{"save","backup","anchor"}) {string file=Path.Combine(f.Save,kind+"."+suffix);R(File.Exists(file));File.Copy(file,Path.Combine(retained,kind+"."+suffix));File.Delete(file);}
     string marker=Path.Combine(f.Save,"manual-evidence.txt");File.WriteAllText(marker,"do not alter");f.C("backup");
     R(f.Host.View.backups.Length==0&&(!Directory.Exists(Path.Combine(f.Save,"Backups"))||Directory.GetFiles(Path.Combine(f.Save,"Backups"),"*.zip").Length==0),"Exported blank collection after acknowledged data disappeared");
     R(!string.IsNullOrEmpty(f.Host.View.error));R(f.Game.CurrentSession.Revision==sessionRevision&&f.Host.Physical.Revision==physicalRevision&&f.Game.GeneratedResultCount==generated);R(File.ReadAllText(marker)=="do not alter");
     foreach(string suffix in new[]{"save","backup","anchor"}) R(!File.Exists(Path.Combine(f.Save,kind+"."+suffix)),"Synthesized blank authoritative state");
    }});
   }
   test("all 101 exact eligible IDs selectable and manually complete without virtual awards",()=> {using(var f=new Fixture()) {
    f.PhysicalDraft("1");var rows=(BinderCardView[])f.Host.View.rows.Clone();R(rows.Length==101&&rows.All(r=>r.collectorNumber!="8"&&r.name!="Machamp"));
    foreach(var row in rows) {f.C("select:"+row.id);f.C("quantity:1");f.C("save-physical");}
    R(f.Host.View.uniqueOwned==101&&f.Host.View.totalQuantity==101&&f.Host.View.completionPercent==100&&f.Host.Physical.Revision==101&&f.Game.CurrentSession.TotalVirtualCards==0);
   }});
   test("forged mutable UI review cannot import without service-owned preview",()=> {using(var f=new Fixture()) {
    f.C("binder-open");f.Host.View.importSummary="forged receipt";f.Host.View.selectedBackupIndex=0;f.C("import-confirm");R(!Directory.Exists(Path.Combine(f.Save,"Profiles"))&&!File.Exists(Path.Combine(f.Save,"active-profile")));
   }});
   test("reviewed source replacement blocks confirmation and exact retry reconciles original",()=> {using(var f=new Fixture()) {
    f.PhysicalDraft("3");f.C("save-physical");f.C("backup");f.C("backup-select:0");f.C("import-preview");var preview=Field(f.Host,"reviewed");string backup=f.Host.Profiles.BackupFiles[0];byte[] original=File.ReadAllBytes(backup);File.WriteAllText(backup,"replaced");f.C("import-confirm");R(f.Host.View.pendingImportConfirm&&!f.Host.View.restartRequired&&ReferenceEquals(preview,Field(f.Host,"reviewed"))&&!Directory.Exists(Path.Combine(f.Save,"Profiles")));File.WriteAllBytes(backup,original);f.C("import-confirm");R(f.Host.View.restartRequired&&Directory.GetDirectories(Path.Combine(f.Save,"Profiles")).Length==1);
   }});
   test("review cancel is read-only and keeps physical drafts",()=> {using(var f=new Fixture()) {
    f.PhysicalDraft("14");string id=f.Host.View.selectedCardId;f.C("backup");f.C("backup-select:0");f.C("import-preview");f.C("import-cancel");R(f.Host.View.importSummary==null&&f.Host.View.quantityText=="14"&&f.Host.View.selectedCardId==id&&f.Host.Physical.Revision==0&&!Directory.Exists(Path.Combine(f.Save,"Profiles"))&&!File.Exists(Path.Combine(f.Save,"active-profile")));
   }});
   test("collection runtime session IO errors never leak absolute paths",()=> {using(var f=new Fixture()) {
    using(var locked=new FileStream(Path.Combine(f.Save,"state.writing"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) f.Game.GenerateForSmoke(82);
    R(f.Game.CurrentView.stage=="error");R(!f.Game.CurrentView.errorMessage.Contains(root),"Session IO error leaks absolute path");f.Game.DispatchCommand("retry");R(f.Game.CurrentSession.TotalVirtualCards==11&&f.Game.GeneratedResultCount==1);
   }});
   test("native legacy schema1 single-save wrapped archive restores exact bytes into selected profile",()=> {
    string save,original,tx;using(var f=new Fixture()) {save=f.Save;f.Game.GenerateForSmoke(83);f.Game.DispatchCommand("open");f.Game.DispatchCommand("next");tx=f.Game.CurrentView.transactionId;}
    File.Delete(Path.Combine(save,"state.backup"));File.Delete(Path.Combine(save,"state.anchor"));original=File.ReadAllText(Path.Combine(save,"state.save"));
    var catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(File.ReadAllText(Path.Combine(Application.dataPath,"Data/base-en-unlimited1999.catalog.json"))));
    using(var profiles=new CollectionProfiles(save)) {var backup=profiles.CreateBackup(save,new UnityStateSerializer(),new UnityPhysicalSerializer(),catalog);var preview=profiles.PreviewImport(backup,new UnityStateSerializer(),new UnityPhysicalSerializer(),catalog);R(preview.VirtualCardCount==11&&preview.PhysicalCardCount==0);profiles.ConfirmImport(preview);}
    R(File.ReadAllText(Path.Combine(save,"state.save"))==original&&!File.Exists(Path.Combine(save,"state.anchor")));
    using(var f=new Fixture(save)) {R(f.Host.IsReady&&f.Game.Paths.CollectionProfileId.Length==32&&f.Game.CurrentView.revealedCount==1&&f.Game.CurrentView.transactionId==tx&&f.Host.Physical.Revision==0);R(File.ReadAllText(Path.Combine(f.Game.Paths.SaveDirectory,"state.save"))==original);}
   });
   test("Game direct shutdown releases host physical before session and container leases",()=> {using(var f=new Fixture()) {
    f.Game.Shutdown();R(!f.Host.IsReady);using(var lease=new FileStream(Path.Combine(f.Save,"physical.lock"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {}using(var lease=new FileStream(Path.Combine(f.Save,"state.lock"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {}using(var p=new CollectionProfiles(f.Save)) {}
   }});
   test("previous default OpeningGame Initialize path remains native compatible",()=> {using(var f=new Fixture(null,false)) {
    f.Game.Initialize(new[]{"--vcc-smoke=ready","--vcc-save-root="+f.Save,"--vcc-artifacts="+f.Save});R(f.Game.CurrentView.stage=="ready"&&f.Game.Paths.CollectionProfileId==""&&!f.Game.PhysicalTrackingConfigured);f.Game.GenerateForSmoke(84);f.Game.DispatchCommand("open");f.Game.DispatchCommand("skip");R(f.Game.CurrentView.stage=="results"&&f.Game.CurrentSession.TotalVirtualCards==11);R(f.Object.GetComponent<TabletopPresentation>().PhysicalResultLabel(0)==null);
   }});
   test("physical results labels opt in and never expose before explicit results",()=> {using(var f=new Fixture()) {
    var method=typeof(TabletopPresentation).GetMethod("PhysicalResultLabel");R(method!=null,"Missing read-only physical results labels");
    f.Game.GenerateForSmoke(81);f.Game.DispatchCommand("open");for(int i=0;i<11;i++) f.Game.DispatchCommand("next");var table=f.Object.GetComponent<TabletopPresentation>();R(method.Invoke(table,new object[]{0})==null,"Final hold must not acquire result badges");
    string id=f.Game.CurrentSession.CurrentPack.OrderedCardIds[0];f.PhysicalDraft("1");f.C("select:"+id);f.C("quantity:1");f.C("save-physical");f.C("binder-close");f.Game.DispatchCommand("results");Tick(f.Host);R((string)method.Invoke(table,new object[]{0})=="PHYSICAL OWNED");int missing=-1;for(int i=0;i<11;i++)if(f.Game.CurrentSession.CurrentPack.OrderedCardIds[i]!=id) missing=i;R(missing>=0&&(string)method.Invoke(table,new object[]{missing})=="PHYSICAL MISSING");
   }});
  }
 }
}
