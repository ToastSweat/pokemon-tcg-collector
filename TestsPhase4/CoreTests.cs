using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using VintageCollector.Domain;
using VintageCollector.Persistence;
using VintageCollector.Collections;
public static class CoreTests
{
 static readonly JavaScriptSerializer Json=new JavaScriptSerializer {MaxJsonLength=64*1024*1024};
 sealed class SessionJson : IStateSerializer { public string Serialize(SessionData d) { return Json.Serialize(d); } public SessionData Deserialize(string t) { return Json.Deserialize<SessionData>(t); } }
 sealed class PhysicalJson : IPhysicalSerializer { public string Serialize(PhysicalStateData d) { return Json.Serialize(d); } public PhysicalStateData Deserialize(string t) { return Json.Deserialize<PhysicalStateData>(t); } }
 static IPhysicalSerializer pj=new PhysicalJson();
 static Catalog catalog; static string output,root; static List<object> checks=new List<object>(); static int failures;
 static string Dir(string name) { string d=Path.Combine(output,name); Directory.CreateDirectory(d); return d; }
 static void Assert(bool b,string m) { if(!b) throw new Exception(m); }
 static void Reject(Action a) { bool bad=false; try { a(); } catch(ArgumentException) {bad=true;} catch(IOException) {bad=true;} catch(InvalidDataException) {bad=true;} catch(InvalidOperationException) {bad=true;} Assert(bad,"Expected rejection"); }
 static void Test(string name,Action a) { try {a(); checks.Add(new {name=name,passed=true});} catch(Exception e) {failures++;checks.Add(new {name=name,passed=false,error=e.ToString()});} }
 sealed class CallbackFault : ISaveFaultInjector {readonly Action<string> action; public CallbackFault(Action<string> a) {action=a;}public void At(string s) {action(s);} }
 sealed class Fault : ISaveFaultInjector { readonly string stage; public bool Fired; public Fault(string s) {stage=s;} public void At(string s) {if(s==stage&&!Fired) {Fired=true;throw new IOException("Synthetic cut "+s);}} }
 static PackResult Pack() {return new PackGenerator(catalog,Json.Deserialize<RuleData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json")))).Generate(new SeededRandom(4));}
 static Dictionary<string,string> Tree(string d) {var x=new Dictionary<string,string>(); foreach(string p in Directory.GetFiles(d,"*",SearchOption.AllDirectories)) if(!p.EndsWith(".lock")) x[p.Substring(d.Length)]=Convert.ToBase64String(File.ReadAllBytes(p)); return x;}
 static bool SameTree(Dictionary<string,string> a,Dictionary<string,string> b) {return Json.Serialize(a)==Json.Serialize(b);}
 static Dictionary<string,byte[]> EmptyMembers() {return new Dictionary<string,byte[]> {{"session.empty",Encoding.UTF8.GetBytes("VTCG-EMPTY-SESSION-1\n")},{"physical.empty",Encoding.UTF8.GetBytes("VTCG-EMPTY-PHYSICAL-1\n")}};}
 static byte[] CustomZip(Dictionary<string,byte[]> files,string extra=null,int attributes=0,bool duplicate=false) {
 using(var m=new MemoryStream()) {using(var z=new ZipArchive(m,ZipArchiveMode.Create,true)) {
 foreach(var f in files) {var e=z.CreateEntry(f.Key);using(var s=e.Open()) s.Write(f.Value,0,f.Value.Length);}
 byte[] mf=CollectionArchive.Manifest(files);using(var s=z.CreateEntry("collection.manifest").Open()) s.Write(mf,0,mf.Length);
 if(extra!=null) {var e=z.CreateEntry(extra);e.ExternalAttributes=attributes;using(var s=e.Open()) s.WriteByte(1);}
 if(duplicate) {var e=z.CreateEntry("session.empty");using(var s=e.Open()) s.WriteByte(1);}
 }return m.ToArray();}
 }
 static string PutBackup(string d,string name,byte[] b) {string dir=Path.Combine(d,"Backups");Directory.CreateDirectory(dir);string p=Path.Combine(dir,name+".zip");File.WriteAllBytes(p,b);return p;}
 static Dictionary<string,byte[]> ReviewPhysicalAnchorConflict() {
 var b1=new PhysicalStateData {schemaVersion=1,revision=1,edits=new[]{new PhysicalEditData {transactionId="B1",createdUtc="2026-10-30T12:00:00Z",cardId=Id,condition="unknown",quantity=1}}};
 byte[] older=CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(b1));
 b1.edits=new[]{b1.edits[0],new PhysicalEditData {transactionId="B2",createdUtc="2026-10-30T12:01:00Z",cardId=Id,condition="unknown",quantity=2}};b1.revision=2;
 byte[] newer=CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(b1));
 b1.edits=new[]{new PhysicalEditData {transactionId="A1",createdUtc="2026-10-30T12:00:00Z",cardId=Id,condition="unknown",quantity=9}};b1.revision=1;
 byte[] foreign=CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(b1));
 return new Dictionary<string,byte[]> {{"session.empty",Encoding.UTF8.GetBytes("VTCG-EMPTY-SESSION-1\n")},{"physical.save",newer},{"physical.backup",older},{"physical.anchor",CollectionEnvelope.Wrap("PHYSICAL","1\n"+CollectionEnvelope.Hash(foreign))}};
 }
 static Dictionary<string,byte[]> ReviewSessionAnchorConflict(string fixture="anchor-session") {
 string d=Dir(fixture+"-B"),a=Dir(fixture+"-A");byte[] older,newer,anchor;
 using(var s=new SessionStore(d,new SessionJson(),catalog)) {s.OpenPack("B1","2026-10-30T12:00:00Z",4,Pack());older=File.ReadAllBytes(Path.Combine(d,"state.save"));s.OpenWrapper("B1");newer=File.ReadAllBytes(Path.Combine(d,"state.save"));}
 using(var s=new SessionStore(a,new SessionJson(),catalog)) {s.OpenPack("A1","2026-10-30T12:00:00Z",4,Pack());anchor=File.ReadAllBytes(Path.Combine(a,"state.anchor"));}
 return new Dictionary<string,byte[]> {{"physical.empty",Encoding.UTF8.GetBytes("VTCG-EMPTY-PHYSICAL-1\n")},{"state.save",newer},{"state.backup",older},{"state.anchor",anchor}};
 }
 sealed class NeverSessionDecode : IStateSerializer {
 internal bool Called;
 public string Serialize(SessionData d) {throw new Exception("Unexpected serialization");}
 public SessionData Deserialize(string s) {Called=true;throw new InvalidDataException("Adapter must not see overflow");}
 }
 static string NumericSessionPayload() {
 var d=new SessionJson().Deserialize(CollectionEnvelope.Unwrap("SAVE",File.ReadAllBytes(Path.Combine(output,"zip-semantic-source","state.save"))));
 d.packs[0].seed=123;return new SessionJson().Serialize(d);
 }
 static void Discard(CollectionProfiles p,ImportPreview v) {
 var method=typeof(CollectionProfiles).GetMethod("DiscardPreview",new[]{typeof(ImportPreview)});Assert(method!=null,"Explicit DiscardPreview API missing");
 try {method.Invoke(p,new object[]{v});} catch(TargetInvocationException e) {throw e.InnerException;}
 }
 static int U16(byte[] b,int p) {return BitConverter.ToUInt16(b,p);}
 static int U32(byte[] b,int p) {return checked((int)BitConverter.ToUInt32(b,p));}
 static void W16(byte[] b,int p,int n) {byte[] bytes=BitConverter.GetBytes((ushort)n);Buffer.BlockCopy(bytes,0,b,p,2);}
 static void W32(byte[] b,int p,uint n) {byte[] bytes=BitConverter.GetBytes(n);Buffer.BlockCopy(bytes,0,b,p,4);}
 static void InvalidMessage(Action a,string message) {bool bad=false;try {a();}catch(InvalidDataException e) {bad=true;Assert(e.Message==message,"Wrong rejection branch: "+e.Message);}Assert(bad,"Expected invalid data");}
 static int RetainedReviews(CollectionProfiles p) {
 var old=typeof(CollectionProfiles).GetField("reviews",BindingFlags.NonPublic|BindingFlags.Instance);
 if(old!=null) return ((System.Collections.IDictionary)old.GetValue(p)).Count;
 return typeof(CollectionProfiles).GetField("review",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(p)==null?0:1;
 }
 static string Id {get {return catalog.Cards[0].Id;}}
 static int Child(string action,string d) {
 string mono=Process.GetCurrentProcess().MainModule.FileName,exe=Assembly.GetExecutingAssembly().Location;
 var start=new ProcessStartInfo(mono,"\""+exe+"\" --child "+action+" \""+d+"\"") {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
 using(var p=Process.Start(start)) {if(!p.WaitForExit(15000)) {p.Kill();p.WaitForExit();throw new Exception("Child timeout");}string o=p.StandardOutput.ReadToEnd()+p.StandardError.ReadToEnd();Assert(o.Length==0,"Child unexpected output: "+o);return p.ExitCode;}
 }
 sealed class ExitFault : ISaveFaultInjector {public void At(string stage) {if(stage=="after-prepare") Environment.Exit(77);}}
 public static int Main(string[] args) {
 if(args.Length==3 && args[0]=="--child") {
  string action=args[1],d=args[2];
  if(action=="physical-lock") {try {using(var p=new PhysicalStore(d,new PhysicalJson(),Catalog.FromData(Json.Deserialize<CatalogData>(File.ReadAllText(Environment.GetEnvironmentVariable("PHASE4_CATALOG")))))) {return 2;}} catch(IOException) {return 0;}}
  if(action=="profile-lock") {try {using(var p=new CollectionProfiles(d)) {return 2;}} catch(IOException) {return 0;}}
  if(action=="crash") {
   string catalogPath=Environment.GetEnvironmentVariable("PHASE4_CATALOG");catalog=Catalog.FromData(Json.Deserialize<CatalogData>(File.ReadAllText(catalogPath)));
   using(var p=new PhysicalStore(d,new PhysicalJson(),catalog,new ExitFault())) p.SetQuantity("crashed","2026-10-30T12:00:00Z",catalog.Cards[0].Id,"unknown",9);return 3;
  }
  if(action=="import-crash-prepare"||action=="import-crash-ack") {
   catalog=Catalog.FromData(Json.Deserialize<CatalogData>(File.ReadAllText(Environment.GetEnvironmentVariable("PHASE4_CATALOG"))));
   string cut=action=="import-crash-prepare"?"after-pointer-prepare":"after-pointer-replace";
   using(var profiles=new CollectionProfiles(d,new CallbackFault(stage=> {if(stage==cut) Environment.Exit(77);}))) {
    string backup=profiles.CreateBackup(d,new SessionJson(),new PhysicalJson(),catalog);profiles.ConfirmImport(profiles.PreviewImport(backup,new SessionJson(),new PhysicalJson(),catalog));
   }return 3;
  }
  return 4;
 }

 root=args[0]; output=args[1]; catalog=Catalog.FromData(Json.Deserialize<CatalogData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"))));
 Test("physical-real-commit-reload-absolute-immutable",()=> {
 string d=Dir("physical-basic"); PhysicalSnapshot old;
 using(var store=new PhysicalStore(d,pj,catalog)) { old=store.Snapshot; var s=store.SetQuantity("first","2026-10-30T12:00:00Z",Id,"unknown",3); Assert(s.Revision==1 && s.Holdings[0].Quantity==3,"Committed bucket"); Assert(old.Holdings.Count==0,"Detached old snapshot"); }
 using(var store=new PhysicalStore(d,pj,catalog)) { Assert(store.Snapshot.Revision==1 && store.Snapshot.Holdings[0].Quantity==3,"Actual file reload"); store.SetQuantity("second","2026-10-30T12:01:00Z",Id,"unknown",1); Assert(store.Snapshot.Holdings[0].Quantity==1,"Absolute not delta"); }
 });

 Test("physical-validation-idempotent-collision-conditions-zero-and-lock",()=> {
 string d=Dir("physical-validation");
 using(var s=new PhysicalStore(d,pj,catalog)) {
 Reject(()=>new PhysicalStore(d,pj,catalog));
 foreach(string cond in new[]{"unknown","near-mint","lightly-played","moderately-played","heavily-played","damaged"}) s.SetQuantity(cond,"2026-10-30T12:00:00Z",Id,cond,99999);
 var snap=s.Snapshot; Assert(snap.Holdings.Count==6,"Separate condition buckets");
 Assert(s.SetQuantity("unknown","2026-10-30T12:00:00Z",Id,"unknown",99999).Revision==6,"Idempotent exact body");
 Reject(()=>s.SetQuantity("unknown","2026-10-30T12:00:00Z",Id,"unknown",2));
 foreach(int q in new[]{-1,100000}) Reject(()=>s.SetQuantity("bad"+q,"2026-10-30T12:00:00Z",Id,"unknown",q));
 Reject(()=>s.SetQuantity("bad","not UTC",Id,"unknown",1)); Reject(()=>s.SetQuantity("bad","2026-10-30T12:00:00Z",Id,"graded",1));
 Reject(()=>s.SetQuantity("bad","2026-10-30T12:00:00Z",Id+"-variant","unknown",1));
 s.SetQuantity("remove","2026-10-30T12:00:00Z",Id,"unknown",0); Assert(s.Snapshot.Holdings.Count==5 && snap.Holdings.Count==6,"Zero removes holding; immutable old snapshot");
 } });

 foreach(string stage in new[]{"before-temp-write","after-temp-write","after-temp-flush","before-prepare","after-prepare","before-replace","after-replace","before-backup","after-backup-flush","after-backup-replace","before-anchor","after-anchor-flush","after-anchor-replace","before-pending-delete","after-pending-delete","before-ack"}) {
 string cut=stage; Test("physical-fault-"+cut,()=> {
 string d=Dir("fault-"+cut); using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("old","2026-10-30T12:00:00Z",Id,"unknown",2);
 var fault=new Fault(cut); using(var s=new PhysicalStore(d,pj,catalog,fault)) {
 Reject(()=>s.SetQuantity("new","2026-10-30T12:00:00Z",Id,"unknown",9)); Assert(fault.Fired,"Real protocol cut reached");
 bool prepared=Array.IndexOf(new[]{"before-temp-write","after-temp-write","after-temp-flush","before-prepare"},cut)<0;
 Assert(s.Snapshot.Holdings[0].Quantity==(prepared?9:2),"Recover prepared only");
 Assert(s.SetQuantity("new","2026-10-30T12:00:00Z",Id,"unknown",9).Revision==2,"Idempotent uncertain retry"); }
 using(var s=new PhysicalStore(d,pj,catalog)) Assert(s.Snapshot.Holdings[0].Quantity==9 && s.Snapshot.Revision==2,"Durable after restart");
 }); }
 Test("physical-corruption-recovery-and-acknowledged-loss-fails-closed",()=> {
 string d=Dir("corrupt"); using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("first","2026-10-30T12:00:00Z",Id,"unknown",4);
 File.WriteAllText(Path.Combine(d,"physical.save"),"corrupt"); using(var s=new PhysicalStore(d,pj,catalog)) Assert(s.Snapshot.Holdings[0].Quantity==4 && s.Snapshot.RecoveryNotice!=null,"Backup recovery");
 File.Delete(Path.Combine(d,"physical.save")); File.Delete(Path.Combine(d,"physical.backup")); Reject(()=>new PhysicalStore(d,pj,catalog));
 });
 Test("physical-unsupported-preserved-not-fallback",()=> {
 string d=Dir("unsupported"); using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("a","2026-10-30T12:00:00Z",Id,"unknown",1);
 string p=Path.Combine(d,"physical.save"),bad="VTCG-PHYSICAL-99\nopaque"; File.WriteAllText(p,bad); Reject(()=>new PhysicalStore(d,pj,catalog)); Assert(File.ReadAllText(p)==bad,"Unsupported bytes unchanged");
 });

 Test("binder-revealed-prefix-only-search-filter-numeric-101",()=> {
 string d=Dir("binder"); using(var s=new SessionStore(d,new SessionJson(),catalog)) using(var p=new PhysicalStore(d,pj,catalog)) {
 var rules=Json.Deserialize<RuleData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json")));
 var pack=new PackGenerator(catalog,rules).Generate(new SeededRandom(4)); s.OpenPack("pack","2026-10-30T12:00:00Z",4,pack);
 var hidden=BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"virtual","owned",""); Assert(hidden.Rows.Count==0 && hidden.UniqueOwned==0 && hidden.TotalQuantity==0,"No hidden awards or filter spoilers");
 s.OpenWrapper("pack"); s.RevealTo("pack",3);
 var v=BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"virtual","all",""); Assert(v.TargetCount==101 && v.Rows.Count==101 && v.TotalQuantity==3 && v.UniqueOwned==3,"Only prefix counted");
 int last=0; foreach(var row in v.Rows) {int n=int.Parse(row.CollectorNumber); Assert(n>last && n!=8,"Numeric ascending excludes 8"); last=n;}
 string concealed=pack.OrderedCardIds[7]; var hc=catalog.Find(concealed);
 var search=BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"virtual","owned",hc.DisplayName); Assert(search.Rows.Count==0 && search.TotalQuantity==3,"Search cannot expose hidden ownership");
 p.SetQuantity("manual","2026-10-30T12:00:00Z",concealed,"near-mint",5); p.SetQuantity("manual2","2026-10-30T12:00:00Z",concealed,"damaged",2);
 var phys=BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"physical","duplicates",hc.CollectorNumber); Assert(phys.Rows.Count==1 && phys.TotalQuantity==7 && phys.UniqueOwned==1 && phys.Rows[0].Quantity==7,"Independent physical buckets; whole stats");
 Assert(BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"physical","missing","not present").TotalQuantity==7,"Filters do not reduce stats");
 Assert(BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"physical","all","0"+hc.CollectorNumber).Rows.Count==0,"Canonical number search only");
 s.RevealTo("pack",11); s.OpenPack("pack2","2026-10-30T12:01:00Z",4,pack);
 Assert(BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"virtual","all","").TotalQuantity==11,"Completed history plus zero new reveals");
 Assert(BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"physical","all","").TotalQuantity==7,"Opening never changes physical");
 Reject(()=>BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"unknown","all","")); Reject(()=>BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"virtual","bad",""));
 } });

 Test("profiles-default-legacy-readonly-and-exclusive-container-lease",()=> {
 string d=Dir("profile-root"); File.WriteAllText(Path.Combine(d,"legacy-untouched"),"untouched");
 using(var p=new CollectionProfiles(d)) {Assert(p.ActiveProfileId=="" && p.ActiveDirectory==Path.GetFullPath(d),"Default in place"); Assert(!File.Exists(Path.Combine(d,"state.save")),"No fresh state writes"); Reject(()=>new CollectionProfiles(d)); Assert(p.BackupFiles.Length==0,"Empty backup list");}
 using(var p=new CollectionProfiles(d)) Assert(p.ActiveProfileId=="","Lock released");
 foreach(string bad in new[]{"relative","https://invalid/never","\\\\server\\never","F:/bad/../escape"}) Reject(()=>new CollectionProfiles(bad));
 Assert(File.ReadAllText(Path.Combine(d,"legacy-untouched"))=="untouched","Legacy bytes unchanged");
 });

 Test("backup-preview-detached-readonly-legacy-schema1-exact",()=> {
 string d=Dir("export"); using(var s=new SessionStore(d,new SessionJson(),catalog)) {s.OpenPack("legacy","2026-10-30T12:00:00Z",4,Pack()); s.OpenWrapper("legacy");s.RevealTo("legacy",3);}
 using(var p=new PhysicalStore(d,pj,catalog)) p.SetQuantity("manual","2026-10-30T12:00:00Z",Id,"damaged",8);
 using(var profiles=new CollectionProfiles(d)) {
 string backup=profiles.CreateBackup(d,new SessionJson(),pj,catalog); Assert(Path.GetDirectoryName(backup)==Path.Combine(d,"Backups"),"Only container backups");
 var before=Tree(d); var preview=profiles.PreviewImport(backup,new SessionJson(),pj,catalog);
 Assert(preview.VirtualCardCount==11 && preview.PhysicalCardCount==8 && preview.VirtualUnique>0 && preview.PhysicalUnique==1,"Reviewed stored counts not rerolled");
 Assert(preview.SourceHash.Length==64 && preview.SourceName==Path.GetFileName(backup) && preview.ReceiptId.Length==32,"Detached identity");
 Assert(SameTree(before,Tree(d)),"Preview writes no bytes"); Assert(!Directory.Exists(Path.Combine(d,"Profiles")) && profiles.ActiveProfileId=="","No premature profiles or activation");
 Reject(()=>profiles.PreviewImport(Path.Combine(d,"state.save"),new SessionJson(),pj,catalog));
 } });
 Test("backup-empty-explicit-marker-never-corrupt-blank",()=> {
 string d=Dir("empty-export"); using(var p=new CollectionProfiles(d)) {string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(b,new SessionJson(),pj,catalog);Assert(v.VirtualCardCount==0&&v.PhysicalCardCount==0,"Explicit empty valid");
 File.WriteAllText(Path.Combine(d,"state.anchor"),"lost acknowledgement"); Reject(()=>p.CreateBackup(d,new SessionJson(),pj,catalog));}
 });

 Test("confirm-new-profile-explicit-next-start-exact-schema1-preserved",()=> {
 string d=Dir("restore");using(var s=new SessionStore(d,new SessionJson(),catalog)) {s.OpenPack("legacy","2026-10-30T12:00:00Z",4,Pack());s.OpenWrapper("legacy");s.RevealTo("legacy",2);}
 using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("old-manual","2026-10-30T12:00:00Z",Id,"unknown",3);
 string original=File.ReadAllText(Path.Combine(d,"state.save")),physical=File.ReadAllText(Path.Combine(d,"physical.save")),newId;
 using(var p=new CollectionProfiles(d)) {
 string backup=p.CreateBackup(d,new SessionJson(),pj,catalog); var v=p.PreviewImport(backup,new SessionJson(),pj,catalog);
 Assert(!Directory.Exists(Path.Combine(d,"Profiles")),"Preview does not create target");
 newId=p.ConfirmImport(v); Assert(newId==v.ReceiptId && p.ConfirmImport(v)==newId,"Same receipt never creates another profile");
 Assert(p.ActiveProfileId=="" && p.ActiveDirectory==Path.GetFullPath(d),"Running profile never changes");
 Assert(File.ReadAllText(Path.Combine(d,"Profiles",newId,"state.save"))==original,"Exact schema1 bytes preserved");
 Assert(File.ReadAllText(Path.Combine(d,"state.save"))==original && File.ReadAllText(Path.Combine(d,"physical.save"))==physical,"Old collection preserved");
 Assert(File.Exists(backup),"Original backup preserved");
 }
 using(var p=new CollectionProfiles(d)) {Assert(p.ActiveProfileId==newId && p.ActiveDirectory==Path.Combine(d,"Profiles",newId),"Explicit confirmation activates only on reopening");
 using(var s=new SessionStore(p.ActiveDirectory,new SessionJson(),catalog)) Assert(s.Snapshot.TotalVirtualCards==11 && s.Snapshot.CurrentPack.RevealedCount==2,"Real session restored unchanged");
 using(var s=new PhysicalStore(p.ActiveDirectory,pj,catalog)) Assert(s.Snapshot.Holdings[0].Quantity==3,"Real physical restored");
 }
 });
 Test("confirm-owned-preview-only-source-replacement-and-pointer-drift",()=> {
 string d=Dir("confirm-guards"); using(var p=new CollectionProfiles(d)) {
 string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(b,new SessionJson(),pj,catalog);
 var forged=new ImportPreview(v.ReceiptId,v.SourceHash,v.SourceName,0,0,0,0,v.Summary);Reject(()=>p.ConfirmImport(forged));
 byte[] old=File.ReadAllBytes(b);File.WriteAllBytes(b,new byte[]{1,2,3});Reject(()=>p.ConfirmImport(v)); Assert(!Directory.Exists(Path.Combine(d,"Profiles")),"Source replacement creates nothing");File.WriteAllBytes(b,old);
 File.WriteAllText(Path.Combine(d,"active-profile"),"changed");Reject(()=>p.ConfirmImport(v));Assert(!Directory.Exists(Path.Combine(d,"Profiles")),"Context drift creates nothing");
 }
 });

 foreach(string stage in new[]{"after-stage-create","after-extract","after-store-validation","after-receipt","after-profile-publish","after-pointer-prepare","after-pointer-replace","before-import-ack"}) {
 string cut=stage;Test("import-uncertain-"+cut,()=> {
 string d=Dir("import-fault-"+cut);using(var s=new SessionStore(d,new SessionJson(),catalog)) s.OpenPack("old","2026-10-30T12:00:00Z",4,Pack());
 string old=File.ReadAllText(Path.Combine(d,"state.save"));var f=new Fault(cut);
 using(var p=new CollectionProfiles(d,f)) {
 string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(b,new SessionJson(),pj,catalog);Reject(()=>p.ConfirmImport(v)); Assert(f.Fired,"Fault boundary reached");
 Assert(File.ReadAllText(Path.Combine(d,"state.save"))==old && p.ActiveProfileId=="","Old runtime untouched on failure");
 var evidence=Tree(d);Reject(()=>p.PreviewImport(b,new SessionJson(),pj,catalog));Reject(()=>Discard(p,v));
 Assert(RetainedReviews(p)==1 && SameTree(evidence,Tree(d)),"Unresolved operation cannot be evicted or mutate evidence");
 string id=p.ConfirmImport(v); Assert(id==v.ReceiptId && p.ConfirmImport(v)==id,"Same preview reconciles once"); Assert(Directory.GetDirectories(Path.Combine(d,"Profiles")).Length==1,"One profile only");
 }
 using(var p=new CollectionProfiles(d)) using(var s=new SessionStore(p.ActiveDirectory,new SessionJson(),catalog)) Assert(s.Snapshot.TotalVirtualCards==11,"Restart selected actual imported store");
 });}
 Test("import-profile-guid-collision-never-overwrites",()=> {
 string d=Dir("import-collision"); using(var p=new CollectionProfiles(d)) {
 var v=p.PreviewImport(p.CreateBackup(d,new SessionJson(),pj,catalog),new SessionJson(),pj,catalog); string target=Path.Combine(d,"Profiles",v.ReceiptId);Directory.CreateDirectory(target);File.WriteAllText(Path.Combine(target,"keep"),"mine");
 Reject(()=>p.ConfirmImport(v));Assert(File.ReadAllText(Path.Combine(target,"keep"))=="mine"&&!File.Exists(Path.Combine(d,"active-profile")),"Foreign collision preserved, no activation");
 } });
 Test("missing-active-pointer-with-backup-not-fresh-default",()=> {
 string d=Dir("lost-pointer");File.WriteAllText(Path.Combine(d,"active-profile.backup"),"older acknowledged selection");Reject(()=>new CollectionProfiles(d));
 });

 Test("zip-local-central-name-mismatch-is-not-reviewed",()=> {
 string d=Dir("zip-local");byte[] b=CustomZip(EmptyMembers());Assert(b[0]==80&&b[1]==75,"Local header");b[30]=(byte)'X';
 using(var p=new CollectionProfiles(d)) Reject(()=>p.PreviewImport(PutBackup(d,"mismatch",b),new SessionJson(),pj,catalog));
 });
 Test("zip-slip-duplicates-symlink-unlisted-and-bomb-are-readonly-rejected",()=> {
 string d=Dir("zip-membership");using(var p=new CollectionProfiles(d)) {
 int i=0;foreach(string name in new[]{"../escape","/absolute","state\\save","state.save:ads","https://invalid","sub/","STATE.SAVE","secret.txt"}) {
 string b=PutBackup(d,"invalid"+(i++),CustomZip(EmptyMembers(),name));var before=Tree(d);Reject(()=>p.PreviewImport(b,new SessionJson(),pj,catalog));Assert(SameTree(before,Tree(d)),"Bad preview changes nothing");}
 Reject(()=>p.PreviewImport(PutBackup(d,"duplicate",CustomZip(EmptyMembers(),null,0,true)),new SessionJson(),pj,catalog));
 var symlink=EmptyMembers();symlink.Remove("session.empty"); // recognized member with Unix symlink metadata
 byte[] link; using(var m=new MemoryStream()) {using(var z=new ZipArchive(m,ZipArchiveMode.Create,true)) {foreach(var f in EmptyMembers()) {var e=z.CreateEntry(f.Key);if(f.Key=="session.empty") e.ExternalAttributes=unchecked((int)0xa1ff0000);using(var stream=e.Open()) stream.Write(f.Value,0,f.Value.Length);} byte[] mf=CollectionArchive.Manifest(EmptyMembers());using(var stream=z.CreateEntry("collection.manifest").Open()) stream.Write(mf,0,mf.Length);}link=m.ToArray();}
 Reject(()=>p.PreviewImport(PutBackup(d,"link",link),new SessionJson(),pj,catalog));
 var bomb=EmptyMembers();bomb["physical.save"]=new byte[CollectionArchive.MemberCap+1];Reject(()=>p.PreviewImport(PutBackup(d,"bomb",CustomZip(bomb)),new SessionJson(),pj,catalog));
 } });
 Test("zip-hash-schema-identity-and-newflags-corrupt-rejected",()=> {
 string d=Dir("zip-semantic"),fixture=Dir("zip-semantic-source");using(var s=new SessionStore(fixture,new SessionJson(),catalog)) s.OpenPack("legacy","2026-10-30T12:00:00Z",4,Pack());
 var original=CollectionArchive.Capture(fixture);using(var p=new CollectionProfiles(d)) {
 int i=0;foreach(string mutation in new[]{"schema","printing","flags","unknown-id","revision","checksum","version"}) {
 var f=new Dictionary<string,byte[]>(original);string raw=Encoding.UTF8.GetString(f["state.save"]);
 if(mutation=="checksum") raw=raw+"broken"; else if(mutation=="version") raw=raw.Replace("VTCG-SAVE-1","VTCG-SAVE-2"); else {
 var data=new SessionJson().Deserialize(CollectionEnvelope.Unwrap("SAVE",f["state.save"]));
 if(mutation=="schema") data.schemaVersion=2; if(mutation=="printing") data.packs[0].printingId="other"; if(mutation=="flags") data.packs[0].newToVirtual[0]=false; if(mutation=="unknown-id") data.packs[0].orderedCardIds[0]=Id+"-foreign"; if(mutation=="revision") data.revision=0;
 raw=Encoding.UTF8.GetString(CollectionEnvelope.Wrap("SAVE",new SessionJson().Serialize(data)));
 }
 f["state.save"]=Encoding.UTF8.GetBytes(raw);Reject(()=>p.PreviewImport(PutBackup(d,"semantic"+(i++),CustomZip(f)),new SessionJson(),pj,catalog));
 }
 } });

 Test("staged-physical-history-not-just-final-buckets-is-validated",()=> {
 string d=Dir("physical-audit");using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("original-event","2026-10-30T12:00:00Z",Id,"damaged",3);
 bool altered=false;var f=new CallbackFault(stage=> {if(stage!="after-extract"||altered) return;altered=true;
 string target=Directory.GetDirectories(d,".import-*")[0];
 var data=pj.Deserialize(CollectionEnvelope.Unwrap("PHYSICAL",File.ReadAllBytes(Path.Combine(target,"physical.save"))));data.edits[0].transactionId="different-event";
 byte[] bytes=CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(data)); File.WriteAllBytes(Path.Combine(target,"physical.save"),bytes);File.WriteAllBytes(Path.Combine(target,"physical.backup"),bytes);
 File.WriteAllBytes(Path.Combine(target,"physical.anchor"),CollectionEnvelope.Wrap("PHYSICAL","1\n"+CollectionEnvelope.Hash(bytes)));
 });
 using(var p=new CollectionProfiles(d,f)) {string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(b,new SessionJson(),pj,catalog);Reject(()=>p.ConfirmImport(v));Assert(!File.Exists(Path.Combine(d,"active-profile")),"Changed audit must not activate");Assert(File.Exists(b)&&File.Exists(Path.Combine(d,"physical.save")),"Source and old profile retained");}
 });
 Test("import-missing-acknowledged-content-never-successful-blank",()=> {
 string d=Dir("import-missing");using(var s=new SessionStore(d,new SessionJson(),catalog)) s.OpenPack("old","2026-10-30T12:00:00Z",4,Pack());
 string id;using(var p=new CollectionProfiles(d)) {var v=p.PreviewImport(p.CreateBackup(d,new SessionJson(),pj,catalog),new SessionJson(),pj,catalog);id=p.ConfirmImport(v);
 string target=Path.Combine(d,"Profiles",id);foreach(string n in new[]{"state.save","state.backup","state.anchor"}) File.Delete(Path.Combine(target,n));Reject(()=>p.ConfirmImport(v));}
 Reject(()=>new CollectionProfiles(d));
 });
 Test("backup-after-deleting-acknowledged-imported-state-is-not-empty",()=> {
 string d=Dir("export-missing");using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("old","2026-10-30T12:00:00Z",Id,"unknown",2);
 using(var p=new CollectionProfiles(d)) p.ConfirmImport(p.PreviewImport(p.CreateBackup(d,new SessionJson(),pj,catalog),new SessionJson(),pj,catalog));
 using(var p=new CollectionProfiles(d)) {foreach(string n in new[]{"physical.save","physical.backup","physical.anchor"}) File.Delete(Path.Combine(p.ActiveDirectory,n));Reject(()=>p.CreateBackup(p.ActiveDirectory,new SessionJson(),pj,catalog));}
 });
 Test("preview-errors-do-not-expose-source-path",()=> {
 string d=Dir("private-errors");using(var p=new CollectionProfiles(d)) {
 string path=Path.Combine(d,"Backups","secret-missing.zip");Directory.CreateDirectory(Path.GetDirectoryName(path));
 try {p.PreviewImport(path,new SessionJson(),pj,catalog);throw new Exception("Expected missing-source failure");} catch(IOException e) {Assert(!e.ToString().Contains(d) && !e.ToString().Contains("secret-missing"),"Safe UI failure text");}
 } });

 Test("archive-json-shape-duplicate-unknown-missing-and-noncanonical-numbers",()=> {
 string d=Dir("json-shape");string source=Dir("json-shape-source");using(var s=new SessionStore(source,new SessionJson(),catalog)) s.OpenPack("legacy","2026-10-30T12:00:00Z",4,Pack());
 var original=CollectionArchive.Capture(source);original.Remove("state.backup");original.Remove("state.anchor");string payload=CollectionEnvelope.Unwrap("SAVE",original["state.save"]);
 int i=0;using(var p=new CollectionProfiles(d)) foreach(string bad in new[]{payload.Replace("\"schemaVersion\":1","\"schemaVersion\":99,\"schemaVersion\":1"),payload.Replace("\"schemaVersion\":1","\"schemaVersion\":1,\"foreign\":true"),payload.Replace(",\"wrapperOpened\":false",""),payload.Replace("\"revision\":1","\"revision\":1.0")}) {
 var files=new Dictionary<string,byte[]>(original);files["state.save"]=CollectionEnvelope.Wrap("SAVE",bad);Reject(()=>p.PreviewImport(PutBackup(d,"shape"+(i++),CustomZip(files)),new SessionJson(),pj,catalog));
 }
 });

 Test("native-child-second-writer-and-crash-recovery",()=> {
 string d=Dir("native-lock");using(var p=new CollectionProfiles(d)) Assert(Child("profile-lock",d)==0,"Other process root lease excluded");
 using(var p=new PhysicalStore(d,pj,catalog)) {p.SetQuantity("old","2026-10-30T12:00:00Z",Id,"unknown",2);Assert(Child("physical-lock",d)==0,"Other process physical writer excluded");}
 Assert(Child("crash",d)==77,"Native child exited at durable prepare without Dispose");
 using(var p=new PhysicalStore(d,pj,catalog)) {Assert(p.Snapshot.Holdings[0].Quantity==9&&p.Snapshot.Revision==2,"Crash recovers real prepared bytes");Assert(p.SetQuantity("crashed","2026-10-30T12:00:00Z",Id,"unknown",9).Revision==2,"Crash retry idempotent");}
 });
 Test("real-junction-paths-rejected-before-file-access",()=> {
 Reject(()=>new CollectionProfiles(Path.Combine(output,"reparse")));
 Reject(()=>new PhysicalStore(Path.Combine(output,"reparse"),pj,catalog));
 Reject(()=>new PhysicalStore(Path.Combine(output,"reparse-files"),pj,catalog));
 using(var p=new CollectionProfiles(Path.Combine(output,"reparse-export"))) {Reject(()=>p.CreateBackup(p.ActiveDirectory,new SessionJson(),pj,catalog));Reject(()=>p.PreviewImport(Path.Combine(p.ActiveDirectory,"Backups","any.zip"),new SessionJson(),pj,catalog));}
 Assert(Directory.GetFiles(Path.Combine(output,"junction-target")).Length==0,"Junction destination was never read or written");
 });

 Test("physical-lineage-collision-semantic-corrupt-and-live-rollback-rejected",()=> {
 string d=Dir("physical-lineage");byte[] old;
 using(var p=new PhysicalStore(d,pj,catalog)) {p.SetQuantity("one","2026-10-30T12:00:00Z",Id,"unknown",1);old=File.ReadAllBytes(Path.Combine(d,"physical.save"));p.SetQuantity("two","2026-10-30T12:00:00Z",Id,"unknown",2);
 File.WriteAllBytes(Path.Combine(d,"physical.save"),old);File.WriteAllBytes(Path.Combine(d,"physical.backup"),old);File.Delete(Path.Combine(d,"physical.anchor"));Reject(()=>p.Reload());}
 string corrupt=Dir("physical-semantic");using(var p=new PhysicalStore(corrupt,pj,catalog)) p.SetQuantity("one","2026-10-30T12:00:00Z",Id,"unknown",1);
 var data=pj.Deserialize(CollectionEnvelope.Unwrap("PHYSICAL",File.ReadAllBytes(Path.Combine(corrupt,"physical.save"))));data.edits[0].condition="graded";File.WriteAllBytes(Path.Combine(corrupt,"physical.save"),CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(data)));Reject(()=>new PhysicalStore(corrupt,pj,catalog));
 });
 Test("restore-legacy-single-schema1-file-recovers-only-new-profile",()=> {
 string d=Dir("legacy-single"),source=Dir("legacy-single-source");using(var s=new SessionStore(source,new SessionJson(),catalog)) {s.OpenPack("legacy-single","2026-10-30T12:00:00Z",4,Pack());s.OpenWrapper("legacy-single");s.RevealTo("legacy-single",5);}
 byte[] original=File.ReadAllBytes(Path.Combine(source,"state.save"));var files=EmptyMembers();files.Remove("session.empty");files["state.save"]=original;
 using(var p=new CollectionProfiles(d)) {string b=PutBackup(d,"legacy-schema1",CustomZip(files));var v=p.PreviewImport(b,new SessionJson(),pj,catalog);var before=Tree(source);string id=p.ConfirmImport(v),target=Path.Combine(d,"Profiles",id);
 Assert(File.Exists(Path.Combine(target,"state.backup"))&&File.Exists(Path.Combine(target,"state.anchor")),"Actual legacy protocol recovers destination");Assert(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(target,"state.save")))==Convert.ToBase64String(original),"Original schema1 identity and flags preserved byte-for-byte");Assert(SameTree(before,Tree(source)),"Legacy source untouched");}
 });
 Test("preview-physical-unknown-schema-and-duplicate-event-rejected",()=> {
 string d=Dir("physical-archive"),source=Dir("physical-archive-source");using(var s=new PhysicalStore(source,pj,catalog)) s.SetQuantity("one","2026-10-30T12:00:00Z",Id,"unknown",1);
 var original=CollectionArchive.Capture(source);original.Remove("physical.backup");original.Remove("physical.anchor");int i=0;
 foreach(string change in new[]{"schema","duplicate","quantity","identity"}) {
 var data=pj.Deserialize(CollectionEnvelope.Unwrap("PHYSICAL",original["physical.save"]));if(change=="schema") data.schemaVersion=2;if(change=="duplicate") {data.edits=new[]{data.edits[0],data.edits[0]};data.revision=2;}if(change=="quantity") data.edits[0].quantity=100000;if(change=="identity") data.edits[0].cardId=Id+"-stamped";
 var f=new Dictionary<string,byte[]>(original);f["physical.save"]=CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(data));using(var p=new CollectionProfiles(d)) Reject(()=>p.PreviewImport(PutBackup(d,"physical"+(i++),CustomZip(f)),new SessionJson(),pj,catalog));
 }
 });
 Test("binder-full-101-unique-completion-and-readonly-collections",()=> {
 string d=Dir("binder-all");using(var p=new PhysicalStore(d,pj,catalog)) using(var s=new SessionStore(d,new SessionJson(),catalog)) {
 int count=0;foreach(var c in catalog.Cards) if(c.BoosterEligible&&c.CollectorNumber!="8") p.SetQuantity("card-"+(count++),"2026-10-30T12:00:00Z",c.Id,"unknown",2);
 var b=BinderProjection.Build(catalog,s.Snapshot,p.Snapshot,"physical","holo","");Assert(b.UniqueOwned==101&&b.TotalQuantity==202&&b.CompletionPercent==100.0,"Unique denominator not quantity");Assert(b.Rows.Count==15,"Strict target holo rows excludes Machamp");
 bool immutable=false;try {((IList<BinderRow>)b.Rows).Clear();}catch(NotSupportedException) {immutable=true;}Assert(immutable,"Rows readonly");immutable=false;try {((IList<PhysicalHolding>)p.Snapshot.Holdings).Clear();}catch(NotSupportedException) {immutable=true;}Assert(immutable,"Holdings readonly");
 }
 });
 Test("second-restore-replaces-pointer-atomically-preserving-first-profile",()=> {
 string d=Dir("second-restore");string first,second;
 using(var p=new CollectionProfiles(d)) {first=p.ConfirmImport(p.PreviewImport(p.CreateBackup(d,new SessionJson(),pj,catalog),new SessionJson(),pj,catalog));}
 using(var p=new CollectionProfiles(d)) {Assert(p.ActiveProfileId==first,"First selected");using(var s=new PhysicalStore(p.ActiveDirectory,pj,catalog)) s.SetQuantity("later-user-action","2026-10-30T12:00:00Z",Id,"unknown",6);
 string b=p.CreateBackup(p.ActiveDirectory,new SessionJson(),pj,catalog);second=p.ConfirmImport(p.PreviewImport(b,new SessionJson(),pj,catalog));Assert(first!=second&&p.ActiveProfileId==first,"Only future activation");}
 Assert(File.Exists(Path.Combine(d,"active-profile.backup")),"Prior checksummed pointer retained");using(var p=new CollectionProfiles(d)) using(var s=new PhysicalStore(p.ActiveDirectory,pj,catalog)) {Assert(p.ActiveProfileId==second&&s.Snapshot.Holdings[0].Quantity==6,"New backup includes normal user changes");}
 Assert(Directory.Exists(Path.Combine(d,"Profiles",first)),"Old imported profile preserved");
 });
 Test("path-device-names-are-rejected-without-opening-devices",()=> {
 foreach(string name in new[]{"CON.zip","aux.zip","COM1.zip","lpt9.zip","NUL.zip","part. /file.zip"}) Reject(()=>CollectionFiles.Local(Path.Combine(output,"Backups",name)));
 });

 Test("import-receipt-minimum-revision-prevents-coherent-rollback",()=> {
 string d=Dir("receipt-rollback");byte[] oldSave,oldBackup,oldAnchor;
 using(var s=new PhysicalStore(d,pj,catalog)) {s.SetQuantity("one","2026-10-30T12:00:00Z",Id,"unknown",1);oldSave=File.ReadAllBytes(Path.Combine(d,"physical.save"));oldBackup=File.ReadAllBytes(Path.Combine(d,"physical.backup"));oldAnchor=File.ReadAllBytes(Path.Combine(d,"physical.anchor"));s.SetQuantity("two","2026-10-30T12:00:00Z",Id,"unknown",2);}
 string id;using(var p=new CollectionProfiles(d)) id=p.ConfirmImport(p.PreviewImport(p.CreateBackup(d,new SessionJson(),pj,catalog),new SessionJson(),pj,catalog));
 string target=Path.Combine(d,"Profiles",id);File.WriteAllBytes(Path.Combine(target,"physical.save"),oldSave);File.WriteAllBytes(Path.Combine(target,"physical.backup"),oldBackup);File.WriteAllBytes(Path.Combine(target,"physical.anchor"),oldAnchor);
 Reject(()=>new CollectionProfiles(d));
 });
 Test("active-pointer-corrupt-unsupported-missing-receipt-failclosed",()=> {
 foreach(string mutation in new[]{"pointer-corrupt","pointer-version","receipt-corrupt","receipt-missing"}) {
 string d=Dir(mutation),id;using(var p=new CollectionProfiles(d)) id=p.ConfirmImport(p.PreviewImport(p.CreateBackup(d,new SessionJson(),pj,catalog),new SessionJson(),pj,catalog));
 string ptr=Path.Combine(d,"active-profile"),rec=Path.Combine(d,"Profiles",id,"import.receipt");if(mutation=="pointer-corrupt") File.WriteAllText(ptr,"broken");if(mutation=="pointer-version") File.WriteAllText(ptr,File.ReadAllText(ptr).Replace("VTCG-ACTIVE-PROFILE-1","VTCG-ACTIVE-PROFILE-99"));if(mutation=="receipt-corrupt") File.WriteAllText(rec,"broken");if(mutation=="receipt-missing") File.Delete(rec);
 var before=Tree(d);Reject(()=>new CollectionProfiles(d));Assert(SameTree(before,Tree(d)),"Metadata rejection does not repair/rewrite data");
 }
 });

 Test("confirm-keeps-reviewed-source-leased-through-publication",()=> {
 string d=Dir("source-lease"),backup=null;bool writerBlocked=false;
 var f=new CallbackFault(stage=> {if(stage!="after-extract") return;try {File.WriteAllBytes(backup,new byte[]{1,2,3});}catch(IOException) {writerBlocked=true;}});
 using(var p=new CollectionProfiles(d,f)) {backup=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(backup,new SessionJson(),pj,catalog);string id=p.ConfirmImport(v);Assert(writerBlocked&&id==v.ReceiptId,"Mutable source cannot change during confirmation");Assert(CollectionEnvelope.Hash(File.ReadAllBytes(backup))==v.SourceHash,"Reviewed archive retained exactly");}
 });

 Test("native-process-import-crash-before-and-after-pointer-publication",()=> {
 foreach(string action in new[]{"import-crash-prepare","import-crash-ack"}) {
 string d=Dir(action);using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("old","2026-10-30T12:00:00Z",Id,"damaged",7);
 string original=File.ReadAllText(Path.Combine(d,"physical.save"));Assert(Child(action,d)==77,"Child process exited at real import publication cut");Assert(File.ReadAllText(Path.Combine(d,"physical.save"))==original,"Old profile preserved across actual process loss");
 Assert(Directory.GetFiles(Path.Combine(d,"Backups"),"*.zip").Length==1 && Directory.GetDirectories(Path.Combine(d,"Profiles")).Length==1,"Source backup and single operation-owned profile retained");
 if(action=="import-crash-prepare") {Assert(!File.Exists(Path.Combine(d,"active-profile"))&&File.Exists(Path.Combine(d,"active-profile.pending")),"No premature pointer activation");Reject(()=>new CollectionProfiles(d));}
 else using(var profiles=new CollectionProfiles(d)) using(var s=new PhysicalStore(profiles.ActiveDirectory,pj,catalog)) Assert(s.Snapshot.Holdings[0].Quantity==7,"Committed confirmed pointer survives acknowledgement loss");
 }
 });
 // CORE-01: identical construction to retained independent ReadOnlyAnchorProbe.cs,
 // using this fresh run's synthetic fixtures instead of depending on older evidence.
 Test("review-probe-physical-older-anchor-conflict-readonly",()=> {
 var files=ReviewPhysicalAnchorConflict();
 foreach(bool swap in new[]{false,true}) {if(swap) {var older=files["physical.backup"];files["physical.backup"]=files["physical.save"];files["physical.save"]=older;}
 var before=Json.Serialize(files);Reject(()=>new CollectionArchive(files,new SessionJson(),pj,catalog));Assert(before==Json.Serialize(files),"Read-only inspector preserved inputs regardless of copy order");}
 });
 Test("review-probe-session-older-anchor-conflict-readonly",()=> {
 var files=ReviewSessionAnchorConflict();var before=Json.Serialize(files);
 Assert(CollectionEnvelope.Hash(files["state.backup"])!=CollectionEnvelope.Unwrap("SAVE",files["state.anchor"]).Split('\n')[1],"Independent A1 anchor contradicts B1");
 Reject(()=>new CollectionArchive(files,new SessionJson(),pj,catalog));Assert(before==Json.Serialize(files),"Read-only inspector preserved inputs");
 var older=files["state.backup"];files["state.backup"]=files["state.save"];files["state.save"]=older;before=Json.Serialize(files);
 Reject(()=>new CollectionArchive(files,new SessionJson(),pj,catalog));Assert(before==Json.Serialize(files),"Older primary cannot be hidden by newer backup");
 });
 foreach(string olderName in new[]{"physical.save","physical.backup","physical.pending"}) {string name=olderName;
 Test("review-probe-physical-older-anchor-conflict-disk-"+name,()=> {
 string d=Dir("anchor-conflict-"+name);var files=ReviewPhysicalAnchorConflict();if(name=="physical.save") {var old=files["physical.backup"];files["physical.backup"]=files["physical.save"];files["physical.save"]=old;}if(name=="physical.pending") {files[name]=files["physical.backup"];files.Remove("physical.backup");}
 foreach(var f in files) if(f.Key.StartsWith("physical.")) File.WriteAllBytes(Path.Combine(d,f.Key),f.Value);
 var before=Tree(d);Reject(()=>new PhysicalStore(d,pj,catalog));Assert(SameTree(before,Tree(d)),"Disk rejection preserved every input byte");
 });}
 Test("preview-replacement-bounds-retained-archive-ownership",()=> {
 string d=Dir("preview-replacement");using(var p=new CollectionProfiles(d)) {
 string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var before=Tree(d);ImportPreview prior=null,current=null;
 for(int i=0;i<64;i++) {current=p.PreviewImport(b,new SessionJson(),pj,catalog);Assert(RetainedReviews(p)==1,"One retained review, not every previous archive");if(prior!=null) Reject(()=>p.ConfirmImport(prior));prior=current;}
 Reject(()=>p.PreviewImport(Path.Combine(d,"Backups","absent.zip"),new SessionJson(),pj,catalog));
 Assert(RetainedReviews(p)==1 && SameTree(before,Tree(d)),"Failed replacement retains current review without writes");
 Assert(p.ConfirmImport(current)==current.ReceiptId,"Newest exact preview remains confirmable");
 } });
 Test("preview-cancel-release-and-dispose-own-no-archive",()=> {
 string d=Dir("preview-cancel");var p=new CollectionProfiles(d);try {
 string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var before=Tree(d);
 for(int i=0;i<64;i++) {var v=p.PreviewImport(b,new SessionJson(),pj,catalog);var forged=new ImportPreview(v.ReceiptId,v.SourceHash,v.SourceName,0,0,0,0,v.Summary);Reject(()=>Discard(p,forged));
 Discard(p,v);Assert(RetainedReviews(p)==0,"Cancel releases archive and decoded members");Reject(()=>p.ConfirmImport(v));Reject(()=>Discard(p,v));}
 var current=p.PreviewImport(b,new SessionJson(),pj,catalog);Assert(SameTree(before,Tree(d)),"Cancel is memory only, never deletes evidence");
 p.Dispose();Assert(RetainedReviews(p)==0,"Dispose releases review memory");Reject(()=>p.ConfirmImport(current));
 }finally {p.Dispose();}
 });
 Test("import-preexisting-stage-collision-preserves-foreign-bytes",()=> {
 string d=Dir("stage-collision");using(var p=new CollectionProfiles(d)) {
 var v=p.PreviewImport(p.CreateBackup(d,new SessionJson(),pj,catalog),new SessionJson(),pj,catalog);
 string stage=Path.Combine(d,".import-"+v.ReceiptId);Directory.CreateDirectory(stage);File.WriteAllText(Path.Combine(stage,"keep"),"foreign stage");
 var before=Tree(d);Reject(()=>p.ConfirmImport(v));Assert(SameTree(before,Tree(d)) && !File.Exists(Path.Combine(d,"active-profile")),"Preexisting stage is never adopted");
 Discard(p,v);Assert(RetainedReviews(p)==0,"Collision before publication may be canceled");
 } });
 foreach(string mutation in new[]{"replaced-foreign","missing","checksum","receipt","source","context"}) {string change=mutation;
 Test("import-retry-requires-exact-stage-owner-"+change,()=> {
 string d=Dir("stage-owner-"+change);using(var s=new PhysicalStore(d,pj,catalog)) s.SetQuantity("old","2026-10-30T12:00:00Z",Id,"unknown",3);
 var fault=new Fault("after-stage-create");using(var p=new CollectionProfiles(d,fault)) {
 string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(b,new SessionJson(),pj,catalog);Reject(()=>p.ConfirmImport(v));Assert(fault.Fired,"Stage actually created before loss");
 string stage=Path.Combine(d,".import-"+v.ReceiptId),owner=Path.Combine(stage,"import.owner");string payload=CollectionEnvelope.Unwrap("IMPORT-OWNER",File.ReadAllBytes(owner));
 if(change=="replaced-foreign") {Directory.Move(stage,stage+"-retained-original");Directory.CreateDirectory(stage);File.WriteAllText(Path.Combine(stage,"foreign-evidence"),"preserve");
 foreach(var f in CollectionArchive.Capture(d)) if(Array.IndexOf(CollectionArchive.Authoritative,f.Key)>=0) File.WriteAllBytes(Path.Combine(stage,f.Key),f.Value);
 File.WriteAllBytes(owner,CollectionEnvelope.Wrap("IMPORT-OWNER","foreign\n"+v.SourceHash+"\ndefault"));}
 if(change=="missing") File.Delete(owner);
 if(change=="checksum") File.WriteAllText(owner,"broken owner");
 if(change=="receipt" || change=="source" || change=="context") {string[] fields=payload.Split('\n');fields[change=="receipt"?0:change=="source"?1:2]="foreign";File.WriteAllBytes(owner,CollectionEnvelope.Wrap("IMPORT-OWNER",string.Join("\n",fields)));}
 var before=Tree(d);Reject(()=>p.ConfirmImport(v));Assert(SameTree(before,Tree(d)),"Foreign or damaged owner rejection must precede every write");
 Assert(!File.Exists(Path.Combine(d,"active-profile")) && !Directory.Exists(Path.Combine(d,"Profiles")),"Foreign stage never activated or published");
 Reject(()=>Discard(p,v));Reject(()=>p.PreviewImport(b,new SessionJson(),pj,catalog));
 } });}
 Test("session-json-shape-typed-numeric-boundaries-accepted",()=> {
 string payload=NumericSessionPayload();CollectionJsonShape.Session(payload);
 CollectionJsonShape.Session(payload.Replace("\"schemaVersion\":1","\"schemaVersion\":2147483647").Replace("\"revision\":1","\"revision\":9223372036854775807").Replace("\"seed\":123","\"seed\":4294967295").Replace("\"revealedCount\":0","\"revealedCount\":2147483647"));
 CollectionJsonShape.Session(payload.Replace("\"schemaVersion\":1","\"schemaVersion\":0").Replace("\"revision\":1","\"revision\":0").Replace("\"seed\":123","\"seed\":0"));
 });
 foreach(string field in new[]{"schemaVersion","revision","seed","revealedCount"}) {string name=field;
 Test("session-json-shape-rejects-overflow-before-adapter-"+name,()=> {
 string payload=NumericSessionPayload(),old="\""+name+"\":"+(name=="seed"?"123":name=="revealedCount"?"0":"1");
 string[] invalid=name=="revision"?new[]{"9223372036854775808","18446744073709551617"}:name=="seed"?new[]{"4294967296","4294967419","18446744073709551617"}:new[]{"2147483648","4294967296","4294967297","18446744073709551617"};
 foreach(string token in invalid) {string bad=payload.Replace(old,"\""+name+"\":"+token);Assert(bad!=payload,"Mutation applied");Reject(()=>CollectionJsonShape.Session(bad));
 var adapter=new NeverSessionDecode();var files=new Dictionary<string,byte[]> {{"state.save",CollectionEnvelope.Wrap("SAVE",bad)}};var before=Json.Serialize(files);
 Reject(()=>SessionArchiveValidation.Validate(files,adapter,catalog));Assert(!adapter.Called && before==Json.Serialize(files),"Reject before native adapter and preserve bytes");}
 });}
 Test("archives-matching-older-anchor-allow-readonly-rollforward",()=> {
 var physical=ReviewPhysicalAnchorConflict();physical["physical.anchor"]=CollectionEnvelope.Wrap("PHYSICAL","1\n"+CollectionEnvelope.Hash(physical["physical.backup"]));
 var session=ReviewSessionAnchorConflict("anchor-session-matching");session["state.anchor"]=CollectionEnvelope.Wrap("SAVE","1\n"+CollectionEnvelope.Hash(session["state.backup"]));
 foreach(var f in new[]{physical,session}) {var before=Json.Serialize(f);var a=new CollectionArchive(f,new SessionJson(),pj,catalog);Assert(a.Session.Revision==2 || a.Physical.Revision==2,"Compatible newer revision accepted");Assert(before==Json.Serialize(f),"Matching archive inspection is read only");}
 });
 foreach(string olderName in new[]{"physical.save","physical.backup","physical.pending"}) {string name=olderName;
 Test("physical-matching-older-anchor-rollforward-"+name,()=> {
 string d=Dir("anchor-matching-"+name);var files=ReviewPhysicalAnchorConflict();byte[] older=files["physical.backup"],newer=files["physical.save"];
 File.WriteAllBytes(Path.Combine(d,name),older);File.WriteAllBytes(Path.Combine(d,name=="physical.save"?"physical.pending":"physical.save"),newer);
 File.WriteAllBytes(Path.Combine(d,"physical.anchor"),CollectionEnvelope.Wrap("PHYSICAL","1\n"+CollectionEnvelope.Hash(older)));
 using(var s=new PhysicalStore(d,pj,catalog)) Assert(s.Snapshot.Revision==2 && s.Snapshot.Holdings[0].Quantity==2,"Matching anchor rolls forward to B2");
 Assert(CollectionEnvelope.Hash(File.ReadAllBytes(Path.Combine(d,"physical.save")))==CollectionEnvelope.Hash(newer) && CollectionEnvelope.Hash(File.ReadAllBytes(Path.Combine(d,"physical.backup")))==CollectionEnvelope.Hash(newer),"Recovered pair exact B2 bytes");
 Assert(CollectionEnvelope.Unwrap("PHYSICAL",File.ReadAllBytes(Path.Combine(d,"physical.anchor")))=="2\n"+CollectionEnvelope.Hash(newer) && !File.Exists(Path.Combine(d,"physical.pending")),"New anchor and prepared cleanup acknowledged");
 });}
 foreach(string mutation in new[]{"prefix","same-revision","anchor-checksum","anchor-ahead","anchor-same-revision","pending-checksum","pending-unsupported","pending-semantic"}) {string change=mutation;
 Test("physical-independent-corruption-preserves-all-inputs-"+change,()=> {
 string d=Dir("physical-independent-"+change);var f=ReviewPhysicalAnchorConflict();f.Remove("session.empty");f.Remove("physical.anchor");
 var protocol=new PhysicalProtocol("",pj,catalog,null);protocol.Decode(Encoding.UTF8.GetString(f["physical.save"]));protocol.Decode(Encoding.UTF8.GetString(f["physical.backup"]));
 if(change=="prefix" || change=="same-revision") {var body=pj.Deserialize(CollectionEnvelope.Unwrap("PHYSICAL",f[change=="prefix"?"physical.backup":"physical.save"]));body.edits[0].transactionId="foreign-valid-event";f["physical.backup"]=CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(body));protocol.Decode(Encoding.UTF8.GetString(f["physical.backup"]));}
 if(change=="anchor-checksum") f["physical.anchor"]=Encoding.UTF8.GetBytes("damaged acknowledgement");
 if(change=="anchor-ahead") f["physical.anchor"]=CollectionEnvelope.Wrap("PHYSICAL","3\n"+CollectionEnvelope.Hash(f["physical.save"]));
 if(change=="anchor-same-revision") f["physical.anchor"]=CollectionEnvelope.Wrap("PHYSICAL","2\n"+CollectionEnvelope.Hash(f["physical.backup"]));
 if(change=="pending-checksum") f["physical.pending"]=Encoding.UTF8.GetBytes("damaged prepared state");
 if(change=="pending-unsupported") f["physical.pending"]=Encoding.UTF8.GetBytes("VTCG-PHYSICAL-99\nopaque");
 if(change=="pending-semantic") {var body=pj.Deserialize(CollectionEnvelope.Unwrap("PHYSICAL",f["physical.save"]));body.edits[0].condition="graded";f["physical.pending"]=CollectionEnvelope.Wrap("PHYSICAL",pj.Serialize(body));}
 if(!change.StartsWith("pending-")) {var archive=new Dictionary<string,byte[]>(f);archive["session.empty"]=Encoding.UTF8.GetBytes("VTCG-EMPTY-SESSION-1\n");var before=Json.Serialize(archive);Reject(()=>new CollectionArchive(archive,new SessionJson(),pj,catalog));Assert(before==Json.Serialize(archive),"Archive rejection is read only");}
 foreach(var file in f) File.WriteAllBytes(Path.Combine(d,file.Key),file.Value);var tree=Tree(d);Reject(()=>new PhysicalStore(d,pj,catalog));Assert(SameTree(tree,Tree(d)),"No repair writes on independent corrupt evidence");
 });}
 Test("session-json-shape-rejects-noncanonical-tokens-and-surrogates",()=> {
 string payload=NumericSessionPayload();foreach(string token in new[]{"-1","-0","123e0","123.0","0123","\"123\"","null"}) Reject(()=>CollectionJsonShape.Session(payload.Replace("\"seed\":123","\"seed\":"+token)));
 Reject(()=>CollectionJsonShape.Session(payload.Replace("\"transactionId\":\"legacy\"","\"transactionId\":\"\\uD800\"")));
 });
 Test("zip-crc-payload-mismatch-with-matching-headers-rejected",()=> {
 byte[] b=CollectionArchive.Zip(EmptyMembers());CollectionArchive.Open(b,new SessionJson(),pj,catalog);
 int cd=U32(b,b.Length-6);uint wrong=BitConverter.ToUInt32(b,cd+16)^1u;W32(b,cd+16,wrong);W32(b,14,wrong);
 CollectionZipStructure.Check(b); // CRC fields match: only computing the decoded payload CRC can reject.
 string d=Dir("zip-crc");using(var p=new CollectionProfiles(d)) {string path=PutBackup(d,"crc",b);var before=Tree(d);Reject(()=>p.PreviewImport(path,new SessionJson(),pj,catalog));Assert(SameTree(before,Tree(d)),"CRC rejection never writes");}
 InvalidMessage(()=>CollectionArchive.Open(b,new SessionJson(),pj,catalog),"ZIP CRC mismatch");
 });
 foreach(string mutation in new[]{"encryption","descriptor","unsupported-flags","too-few","too-many","overlong-name","disk","central-disk","zip64","central-extra","local-extra","entry-comment","archive-comment","offset-gap","overlap","payload-bounds","local-gap","preamble","trailing"}) {string change=mutation;
 Test("zip-strict-structure-rejects-"+change,()=> {
 byte[] b=CollectionArchive.Zip(EmptyMembers());CollectionArchive.Open(b,new SessionJson(),pj,catalog);int end=b.Length-22,cd=U32(b,end+16),second=cd+46+U16(b,cd+28);
 if(change=="encryption" || change=="descriptor" || change=="unsupported-flags") {int flags=change=="encryption"?1:change=="descriptor"?8:16;W16(b,6,flags);W16(b,cd+8,flags);}
 if(change=="too-few" || change=="too-many") {int count=change=="too-few"?2:10;W16(b,end+8,count);W16(b,end+10,count);}
 if(change=="overlong-name") W16(b,cd+28,257);
 if(change=="disk") W16(b,end+4,1);if(change=="central-disk") W16(b,cd+34,1);
 if(change=="zip64") W32(b,end+16,uint.MaxValue);
 if(change=="central-extra") W16(b,cd+30,1);if(change=="local-extra") W16(b,28,1);
 if(change=="entry-comment") W16(b,cd+32,1);if(change=="archive-comment") W16(b,end+20,1);
 if(change=="offset-gap") W32(b,cd+42,1);if(change=="overlap") W32(b,second+42,0);
 if(change=="payload-bounds") {W32(b,18,(uint)cd);W32(b,cd+20,(uint)cd);}
 if(change=="local-gap") {uint size=(uint)(U32(b,18)-1);W32(b,18,size);W32(b,cd+20,size);}
 if(change=="preamble") {var padded=new byte[b.Length+1];Buffer.BlockCopy(b,0,padded,1,b.Length);b=padded;}
 if(change=="trailing") {var padded=new byte[b.Length+1];Buffer.BlockCopy(b,0,padded,0,b.Length);b=padded;}
 string d=Dir("zip-structure-"+change);using(var p=new CollectionProfiles(d)) {string path=PutBackup(d,"invalid",b);var before=Tree(d);Reject(()=>p.PreviewImport(path,new SessionJson(),pj,catalog));Assert(SameTree(before,Tree(d)),"Structural rejection preserved input bytes");}
 });}
 Test("zip-complete-archive-cap-is-enforced-before-structure",()=> {
 string d=Dir("zip-archive-cap");using(var p=new CollectionProfiles(d)) {
 string path=PutBackup(d,"oversize",new byte[0]);using(var file=new FileStream(path,FileMode.Open,FileAccess.Write)) file.SetLength((long)CollectionArchive.ArchiveCap+1);
 long length=new FileInfo(path).Length;DateTime timestamp=File.GetLastWriteTimeUtc(path);Reject(()=>p.PreviewImport(path,new SessionJson(),pj,catalog));Assert(new FileInfo(path).Length==length && File.GetLastWriteTimeUtc(path)==timestamp && !Directory.Exists(Path.Combine(d,"Profiles")),"Over-cap source untouched, no stage");
 InvalidMessage(()=>CollectionArchive.Open(new byte[CollectionArchive.ArchiveCap+1],new SessionJson(),pj,catalog),"Archive exceeds limit");
 } });
 Test("zip-aggregate-cap-with-individually-in-bound-members",()=> {
 var f=new Dictionary<string,byte[]>();var block=new byte[CollectionArchive.MemberCap];foreach(string n in new[]{"state.save","state.backup","physical.save","physical.backup","state.anchor"}) f[n]=block;
 byte[] b=CustomZip(f);Assert(b.Length<CollectionArchive.ArchiveCap,"Compressed fixture itself in bound");CollectionZipStructure.Check(b);
 InvalidMessage(()=>CollectionArchive.Open(b,new SessionJson(),pj,catalog),"Oversized archive member");
 });
 Test("preview-release-after-success-and-prepublication-source-failure",()=> {
 string d=Dir("preview-release-success");using(var p=new CollectionProfiles(d)) {
 string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(b,new SessionJson(),pj,catalog);byte[] bytes=File.ReadAllBytes(b);File.WriteAllText(b,"replaced");Reject(()=>p.ConfirmImport(v));Discard(p,v);Assert(RetainedReviews(p)==0,"Source check failure before publication can be canceled");File.WriteAllBytes(b,bytes);
 v=p.PreviewImport(b,new SessionJson(),pj,catalog);p.ConfirmImport(v);var before=Tree(d);Discard(p,v);Assert(RetainedReviews(p)==0 && SameTree(before,Tree(d)),"Acknowledged operation release changes memory only");Reject(()=>p.ConfirmImport(v));
 } });
 Test("published-profile-owner-must-match-before-uncertain-retry",()=> {
 string d=Dir("published-owner");var fault=new Fault("after-pointer-replace");using(var p=new CollectionProfiles(d,fault)) {
 string b=p.CreateBackup(d,new SessionJson(),pj,catalog);var v=p.PreviewImport(b,new SessionJson(),pj,catalog);Reject(()=>p.ConfirmImport(v));Assert(fault.Fired,"Actual committed-pointer acknowledgement loss");
 string owner=Path.Combine(d,"Profiles",v.ReceiptId,"import.owner");byte[] original=File.ReadAllBytes(owner);File.WriteAllBytes(owner,CollectionEnvelope.Wrap("IMPORT-OWNER","foreign"));
 var before=Tree(d);Reject(()=>p.ConfirmImport(v));Assert(SameTree(before,Tree(d)),"Published ownership failure precedes store recovery or pointer writes");Reject(()=>Discard(p,v));
 File.WriteAllBytes(owner,original);Assert(p.ConfirmImport(v)==v.ReceiptId,"Exact original owner enables same-operation retry");
 } });
 Console.WriteLine(Json.Serialize(new {testCount=checks.Count,passed=failures==0,checks=checks})); return failures==0?0:1;
 }
}
