using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using VintageCollector.Funds;
using VintageCollector.Persistence;
using VintageCollector.Presentation;
using VintageCollector.Runtime;
using VintageCollector.Shopping;

namespace VintageCollector.Verification {
 // Attached only to BootstrapFinal. Normal play returns before marker IO, hooks or factories.
 [DefaultExecutionOrder(170), DisallowMultipleComponent]
 public sealed class FinalSmokeProbe : MonoBehaviour {
  const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
  const string PackId="final-native-pack-one";
  const string Synthetic="SYNTHETIC FINAL TEST";
  [Serializable] public sealed class Check { public string name,detail; public bool passed; }
  [Serializable] public sealed class FileHash { public string path,sha256; }
  [Serializable] public sealed class Financial {
   public string stage; public long revision,recordedCents,plannedCents,openingCents,suggestionCents;
   public bool enabled; public int entryCount,openingEntryCount; public string[] seenPackIds;
  }
  [Serializable] public sealed class Expected {
   public Financial financial; public string sessionJson,originalProfileId,importedProfileId,originalFundHash,fundHash,shoppingHash,shoppingCardId;
   public long physicalRevision;
  }
  [Serializable] public sealed class Report {
   public bool passed,workflowCompleted,shutdownCompleted;
   public int checkCount,resultCount,financialSnapshotCount,copiedFileHashCount,screenshotCount,generatedResultCount;
   public string mode,sessionJson,failure,activeProfileId;
   public Check[] checks; public string[] results,screenshots;
   public Financial[] financialSnapshots; public FileHash[] copiedFileHashes;
  }
  sealed class Fault : ILedgerFaultInjector {
   public volatile bool Armed; int fired;
   public int Fired {get{return Interlocked.CompareExchange(ref fired,0,0);}}
   public void At(string stage) { if(stage=="afterPrepare"&&Armed&&Interlocked.CompareExchange(ref fired,1,0)==0){Armed=false;throw new IOException(Synthetic+" one-shot afterPrepare");} }
  }
  OpeningGame game; Phase5CollectionHost collection; FundHost fund; FundPresentation panel;
  BinderPresentation binder; Phase6ShoppingHost shopping; ShoppingPresentation shoppingPanel;
  GamePaths paths; string mode,runRoot,sessionAtExit,activeAtExit,failure=""; bool completed,shutdownSeen,fixtureAdmitted;
  readonly Fault fault=new Fault(); readonly List<Check> checks=new List<Check>();
  readonly List<Financial> finances=new List<Financial>(); readonly List<FileHash> hashes=new List<FileHash>();
  readonly List<string> screenshots=new List<string>(); readonly List<string> results=new List<string>();
  int generatedAtExit;
  static object Field(object target,string name) {var f=target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(f==null)throw new MissingFieldException(target.GetType().FullName,name);return f.GetValue(target);}
  static string Full(string path){return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);}
  static bool Same(string x,string y){return string.Equals(Full(x),Full(y),StringComparison.OrdinalIgnoreCase);}
  bool AdmitLayout() {
   // Inspect explicit arguments first: ordinary startup does not even resolve fixture paths.
   string[] args=Environment.GetCommandLineArgs(); bool explicitLayout=false;
   foreach(string arg in args)if(arg=="--vcc-smoke=layout")explicitLayout=true;
   if(!explicitLayout)return false;
   game=GetComponent<OpeningGame>();if(game==null)throw new InvalidOperationException("Final probe requires OpeningGame");
   paths=GamePaths.ResolvePhase5(Root,Application.dataPath,Application.isEditor,args);
   if(!paths.IsSmokeTest||paths.SmokeScenario!="layout")return false;
   string parent=Full(Path.Combine(Root,".artifacts/final-player")),art=Full(paths.ArtifactDirectory);
   if(!art.StartsWith(parent+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return false;
   string[] parts=art.Substring(parent.Length+1).Split(Path.DirectorySeparatorChar);
   if(parts.Length!=2||!System.Text.RegularExpressions.Regex.IsMatch(parts[0],"^[A-Za-z0-9_-]{1,80}$"))throw new InvalidDataException("Final fixture layout");
   mode=parts[1];if(mode!="first"&&mode!="reopen")throw new InvalidDataException("Final fixture mode");
   runRoot=Path.Combine(parent,parts[0]);if(!Same(paths.BaseSaveDirectory,Path.Combine(runRoot,"session")))throw new InvalidDataException("Final fixture shared session root");
   return true;
  }
  IEnumerator Start() {
   bool admitted=false;try{admitted=AdmitLayout();}catch(Exception e){Debug.LogError("Final fixture admission refused: "+e);}
   if(!admitted)yield break;
   yield return Guard(Run());
   // Snapshot while owners are still alive; report success ONLY in ShutdownCompleted.
   try {
    if(game.CurrentSession!=null)sessionAtExit=Session();generatedAtExit=game.GeneratedResultCount;
    if(collection!=null&&collection.Profiles!=null)activeAtExit=collection.Profiles.ActiveProfileId;
    if(fund!=null&&fund.Snapshot!=null)finances.Add(Finance("before-shutdown"));
   }catch(Exception e){Fail("exit-snapshot",e);}
   try {game.ShutdownCompleted+=OnShutdown;game.RequestQuit();}
   catch(Exception e){Fail("quit-cleanup",e);if(shutdownSeen)WriteReport();else Debug.LogError(e);}
  }
  IEnumerator Guard(IEnumerator routine) {
   var stack=new Stack<IEnumerator>();stack.Push(routine);
   while(stack.Count!=0) {
    object value=null;bool more=false;Exception error=null;
    try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){error=e;}
    if(error!=null){Fail("workflow-failure",error);yield break;}
    if(!more){stack.Pop();continue;}var child=value as IEnumerator;if(child!=null)stack.Push(child);else yield return value;
   }
  }
  void Fail(string name,Exception e){failure+=e+"\n";checks.Add(new Check{name=name,passed=false,detail=e.ToString()});Debug.LogError("Final probe: "+e);}
  void CheckThat(string name,bool passed,string detail="actual acknowledged source/API postcondition") {
   foreach(var c in checks)if(c.name==name)throw new InvalidOperationException("Duplicate check "+name);
   checks.Add(new Check{name=name,passed=passed,detail=detail});if(fund!=null&&fund.Snapshot!=null)finances.Add(Finance(name));
   if(!passed)throw new InvalidOperationException(name);
  }
  IEnumerator Wait(Func<bool> ready,string why,float seconds=30) {
   float deadline=Time.realtimeSinceStartup+seconds;
   while(!ready()){if(Time.realtimeSinceStartup>=deadline)throw new TimeoutException(why);yield return null;}
  }
  IEnumerator Settled(){yield return Wait(()=>!game.HasPendingStorage&&fund.Snapshot!=null&&!fund.IsBusy,"actual opening/fund acknowledgements");yield return null;}
  void Cmd(string command){collection.DispatchBinderCommand(command);}
  void OpenFund(){Cmd("binder-open");fund.Open();if(fund.CurrentView==null||!binder.IsOpen)throw new InvalidOperationException("Clean source fund modal not opened");}
  void CloseFund(){panel.Emit("close");Cmd("binder-close");}
  Financial Finance(string stage) {
   var s=fund.Snapshot;int openings=0;foreach(var e in s.Entries)if(e.Kind=="Opening")openings++;
   return new Financial{stage=stage,revision=s.Revision,recordedCents=s.ConfirmedCents,plannedCents=s.PlannedCents,openingCents=s.OpeningBalanceCents,suggestionCents=s.SuggestedContributionCents,enabled=s.Enabled,entryCount=s.Entries.Count,openingEntryCount=openings,seenPackIds=new List<string>(s.SeenPackIds).ToArray()};
  }
  bool MoneyIs(long cash,long planned,long opening,long suggestion){var s=fund.Snapshot;return s!=null&&s.ConfirmedCents==cash&&s.PlannedCents==planned&&s.OpeningBalanceCents==opening&&s.SuggestedContributionCents==suggestion&&s.Enabled;}
  IEnumerator Run() {
   // Canonical bounded marker admits all subsequent fixture writes and optional worker hook.
   ShoppingSmokePolicy.Exact(Path.Combine(paths.ArtifactDirectory,"final-fixture.json"),"{\"owner\":\"final-native-proof-v1\",\"mode\":\""+mode+"\"}");
   ShoppingSmokePolicy.Ancestors(paths.BaseSaveDirectory);fixtureAdmitted=true;Application.runInBackground=true;
   fund=GetComponent<FundHost>();collection=GetComponent<Phase5CollectionHost>();panel=GetComponent<FundPresentation>();binder=GetComponent<BinderPresentation>();shopping=GetComponent<Phase6ShoppingHost>();shoppingPanel=GetComponent<ShoppingPresentation>();
   if(fund==null||collection==null||panel==null||binder==null||shopping==null||shoppingPanel==null)throw new InvalidOperationException("Missing actual final components");
   // Start order 170 precedes FundHost.Update 180, including its first initialization frame.
   if(mode=="first"){if(fund.Snapshot!=null||fund.IsBusy)throw new InvalidOperationException("Factory must precede initialization");var injected=fault;fund.StoreFactory=(p,ids)=>new LedgerStore(p,ids,injected);}
   yield return Wait(()=>game.Paths!=null,"explicit final layout Paths readiness",90);
   if(!game.Paths.IsSmokeTest||game.Paths.SmokeScenario!="layout"||!Same(paths.BaseSaveDirectory,game.Paths.BaseSaveDirectory)||!Same(paths.ArtifactDirectory,game.Paths.ArtifactDirectory))throw new InvalidDataException("Actual Game.Paths mismatch");
   paths=game.Paths;
   yield return Wait(()=>StartupLoadingCoordinator.IsReady&&collection.IsReady&&fund.Snapshot!=null&&!fund.IsBusy&&collection.ShoppingEnabled,"startup and fund adopted acknowledgement",90);
   if(game.CurrentView.paused)game.DispatchCommand("resume");
   if(mode=="first")yield return First();else yield return Reopen();completed=true;
  }
  IEnumerator First() {
   CheckThat("fresh-defaults",game.CurrentSession.Packs.Count==0&&game.GeneratedResultCount==0&&MoneyIs(10000,0,10000,1000)&&Finance("").openingEntryCount==1&&fund.Snapshot.SeenPackIds.Count==0&&collection.Physical.Holdings.Count==0&&collection.Profiles.BackupFiles.Length==0);
   OpenFund();string session=Session(),source=JsonUtility.ToJson(collection.CurrentBinderView);long physical=collection.Physical.Revision;
   Cmd("quantity:9");Cmd("binder-close");game.DispatchCommand("generate");
   CheckThat("fund-modal-fences-underlay",fund.CurrentView!=null&&binder.IsOpen&&Session()==session&&JsonUtility.ToJson(collection.CurrentBinderView)==source&&collection.Physical.Revision==physical);
   yield return Capture("fund-initial");CloseFund();
   game.GenerateForSmoke(7u,PackId);yield return Settled();
   CheckThat("sealed-pack-no-plan",game.GeneratedResultCount==1&&game.CurrentSession.Packs.Count==1&&!game.CurrentSession.CurrentPack.WrapperOpened&&fund.Snapshot.PendingPlans.Count==0&&fund.Snapshot.SeenPackIds.Count==0&&MoneyIs(10000,0,10000,1000));
   game.DispatchCommand("open");yield return Wait(()=>!game.HasPendingStorage&&!fund.IsBusy&&fund.Snapshot.SeenPackIds.Contains(PackId),"opened pack observed once");
   CheckThat("wrapper-open-one-planned-not-cash",game.CurrentSession.CurrentPack.WrapperOpened&&fund.Snapshot.PendingPlans.Count==1&&fund.Snapshot.PendingPlans[0].AmountCents==1000&&fund.Snapshot.SeenPackIds.Count==1&&MoneyIs(10000,1000,10000,1000));
   long ledgerRevision=fund.Snapshot.Revision;game.DispatchCommand("next");yield return Settled();
   CheckThat("reveal-does-not-add-plan-or-popup",game.CurrentSession.CurrentPack.RevealedCount==1&&game.CurrentSession.TotalVirtualCards==11&&fund.Snapshot.Revision==ledgerRevision&&fund.Snapshot.PendingPlans.Count==1&&fund.CurrentView==null&&MoneyIs(10000,1000,10000,1000));
   string afterReveal=Session();OpenFund();fault.Armed=true;panel.Emit("confirm",fund.Snapshot.PendingPlans[0].Id);object exact=Field(fund,"pending");
   yield return Wait(()=>!fund.IsBusy&&fund.CurrentView!=null&&fund.CurrentView.CanRetry,"real afterPrepare failure adopted");
   CheckThat("real-afterPrepare-ack-retained",exact!=null&&ReferenceEquals(exact,Field(fund,"pending"))&&fault.Fired==1&&MoneyIs(10000,1000,10000,1000)&&!fund.CurrentView.CanMutate&&!fund.CurrentView.CanExport);
   yield return Capture("fund-retry");
   panel.Emit("retry");bool reused=ReferenceEquals(exact,Field(fund,"pending"))&&ReferenceEquals(exact,Field(Field(fund,"worker"),"request"));
   yield return Wait(()=>!fund.IsBusy&&Field(fund,"pending")==null,"same real request retry acknowledgement");
   CheckThat("retry-exact-request-once",reused&&fault.Fired==1&&MoneyIs(11000,0,10000,1000)&&fund.Snapshot.PendingPlans.Count==0);
   var draft=fund.CurrentView.Draft.Copy();draft.Amount="25.00";draft.Note=Synthetic+" manual purchase; no actual purchase";panel.SubmitDraft(draft);panel.Emit("purchase");
   yield return Wait(()=>!fund.IsBusy&&fund.Snapshot.ConfirmedCents==8500,"manual purchase acknowledgement");
   CheckThat("purchase-manual-only",collection.Physical.Revision==physical&&Session()==afterReveal&&MoneyIs(8500,0,10000,1000));
   draft=fund.CurrentView.Draft.Copy();draft.Opening="125.00";draft.Suggestion="12.50";draft.Enabled=true;panel.SubmitDraft(draft);panel.Emit("configure");
   yield return Wait(()=>!fund.IsBusy&&fund.Snapshot.OpeningBalanceCents==12500,"configuration correction acknowledgement");
   CheckThat("configuration-appends-correction",MoneyIs(11000,0,12500,1250)&&Finance("").openingEntryCount==1&&collection.Physical.Revision==physical&&Session()==afterReveal);
   panel.Emit("csv");string exports=Path.Combine(paths.SaveDirectory,"Exports");ShoppingSmokePolicy.Ancestors(exports);
   string[] csv=Directory.Exists(exports)?Directory.GetFiles(exports,"*.csv"):new string[0];string text=csv.Length==1?ReadText(csv[0],8*1024*1024):"";
   CheckThat("explicit-real-local-csv",csv.Length==1&&text.Contains("USD")&&text.Contains("Purchase")&&text.Contains(Synthetic)&&Session()==afterReveal&&MoneyIs(11000,0,12500,1250));if(csv.Length==1)RecordHash(csv[0]);panel.Emit("close");
   Cmd("binder-set:base-en-unlimited1999");Cmd("mode:physical");string card=null;foreach(var row in collection.CurrentBinderView.rows)if(row.collectorNumber=="1"&&row.quantity==0&&row.name=="Alakazam")card=row.id;
   if(card==null)throw new InvalidOperationException("Actual Base #1 zero-owned Alakazam missing");Cmd("select:"+card);Cmd("condition:near-mint");
   yield return Wait(()=>binder.ShoppingAvailable,"current source shopping capability",5);
   var request=binder.GetType().GetMethod("RequestShopping",BindingFlags.Instance|BindingFlags.NonPublic);if(request==null)throw new MissingMethodException("BinderPresentation.RequestShopping");request.Invoke(binder,new object[]{card});
   if(shoppingPanel.Current==null)throw new InvalidOperationException("Actual shopping host gate denied source");
   shoppingPanel.Emit("new");var note=shoppingPanel.Current.Draft;note.Data.listingUrl="https://www.ebay.com/itm/123456789012";note.Data.title=Synthetic;note.Data.note=Synthetic+"; no actual listing, search, copy or purchase";note.Data.rawNearMintUSD.priceKnown=true;note.Price="1.00";shoppingPanel.SubmitDraft(note);shoppingPanel.Emit("save");
   var shoppingFiles=shopping.CaptureShoppingBackupFiles();var notes=ShoppingArchive.ValidateArchiveFiles(shoppingFiles,game.ApprovedSets);
   CheckThat("actual-shopping-note-no-ownership-change",notes.Records.Count==1&&notes.Records[0].ToData().title==Synthetic&&notes.Records[0].ToData().rawNearMintUSD.priceCents==100&&collection.Physical.Revision==physical&&Session()==afterReveal);shoppingPanel.Emit("close");
   yield return Settled();var captured=fund.CaptureSidecars(paths.SaveDirectory);Cmd("backup");string[] backups=collection.Profiles.BackupFiles;
   if(backups.Length!=1)throw new InvalidOperationException("Exactly one real complete backup expected");var zip=ReadZip(backups[0]);
   bool exactFiles=true;foreach(var pair in captured)if(!zip.ContainsKey(pair.Key)||Hash(zip[pair.Key])!=Hash(pair.Value))exactFiles=false;
   CheckThat("complete-backup-real-sidecars",zip.ContainsKey("profile.version")&&Encoding.UTF8.GetString(zip["profile.version"])=="VTCG-PROFILE-2\n"&&zip.ContainsKey("Fund/fund.state.json")&&zip.ContainsKey("Shopping/shopping.state.json")&&exactFiles);
   foreach(var pair in captured)hashes.Add(new FileHash{path="backup:"+pair.Key,sha256=Hash(pair.Value)});RecordHash(backups[0]);
   var expected=new Expected{financial=Finance("expected-first"),sessionJson=Session(),physicalRevision=physical,originalProfileId=collection.Profiles.ActiveProfileId,originalFundHash=HashFile(Path.Combine(paths.SaveDirectory,"Fund/fund.state.json")),fundHash=Hash(captured["Fund/fund.state.json"]),shoppingHash=Hash(captured["Shopping/shopping.state.json"]),shoppingCardId=card};
   // Persist own-fixture comparison before import, then append only receipt identity after confirmation.
   SaveExpected(expected);Cmd("backup-select:0");Cmd("import-preview");string review=collection.CurrentBinderView.importSummary;
   CheckThat("review-complete-fund-shopping-summary",!string.IsNullOrEmpty(review)&&review.Contains("complete profile v2")&&review.Contains("1 shopping notes")&&review.Contains("110.00")&&review.Contains("0.00"));
   var preview=(VintageCollector.Collections.ImportPreview)Field(collection,"reviewed");if(preview==null)throw new InvalidOperationException("Actual owned review missing");expected.importedProfileId=preview.ReceiptId;
   Cmd("import-confirm");string pointer=ReadText(Path.Combine(paths.BaseSaveDirectory,"active-profile"),4096);
   CheckThat("restore-new-pointer-no-hot-replacement",collection.CurrentBinderView.restartRequired&&!collection.CurrentBinderView.pendingImportConfirm&&collection.Profiles.ActiveProfileId==expected.originalProfileId&&game.Paths.CollectionProfileId==expected.originalProfileId&&pointer.Contains(expected.importedProfileId)&&HashFile(Path.Combine(paths.SaveDirectory,"Fund/fund.state.json"))==expected.originalFundHash&&MoneyIs(11000,0,12500,1250));
   SaveExpected(expected);Cmd("binder-close");results.Add("first: real fund retry, one opened pack, manual purchase/configuration, local CSV, complete backup and restart-required restore");
  }
  IEnumerator Reopen() {
   var expected=JsonUtility.FromJson<Expected>(ReadText(Path.Combine(runRoot,"expected.json"),1024*1024));if(expected==null||expected.financial==null||string.IsNullOrEmpty(expected.importedProfileId))throw new InvalidDataException("First mode expected receipt missing");
   CheckThat("reopen-imported-active-profile",collection.Profiles.ActiveProfileId==expected.importedProfileId&&game.Paths.CollectionProfileId==expected.importedProfileId&&collection.Profiles.ActiveProfileId.Length!=0);
   var actual=Finance("restored");var prior=expected.financial;
   CheckThat("reopen-exact-fund-no-second-opening",MoneyIs(11000,0,12500,1250)&&actual.openingEntryCount==1&&actual.entryCount==prior.entryCount&&actual.revision==prior.revision&&string.Join("|",actual.seenPackIds)==string.Join("|",prior.seenPackIds)&&HashFile(Path.Combine(paths.SaveDirectory,"Fund/fund.state.json"))==expected.fundHash);
   CheckThat("reopen-no-generation-session-exact",game.GeneratedResultCount==0&&game.CurrentSession.Packs.Count==1&&game.CurrentSession.TotalVirtualCards==11&&game.CurrentSession.CurrentPack.RevealedCount==1&&Session()==expected.sessionJson&&collection.Physical.Revision==expected.physicalRevision);
   var files=shopping.CaptureShoppingBackupFiles();var notes=ShoppingArchive.ValidateArchiveFiles(files,game.ApprovedSets);
   CheckThat("reopen-readonly-shopping-restored",Field(shopping,"store")==null&&notes.Records.Count==1&&notes.Records[0].ToData().canonicalCardId==expected.shoppingCardId&&notes.Records[0].ToData().title==Synthetic&&Hash(files["shopping.state.json"])==expected.shoppingHash);
   foreach(var pair in files)hashes.Add(new FileHash{path="reopen:Shopping/"+pair.Key,sha256=Hash(pair.Value)});
   string original=expected.originalProfileId.Length==0?paths.BaseSaveDirectory:Path.Combine(paths.BaseSaveDirectory,"Profiles",expected.originalProfileId);
   CheckThat("reopen-original-fund-unchanged",HashFile(Path.Combine(original,"Fund/fund.state.json"))==expected.originalFundHash);RecordHash(Path.Combine(original,"Fund/fund.state.json"));RecordHash(Path.Combine(paths.SaveDirectory,"Fund/fund.state.json"));
   OpenFund();if(fund.CurrentView.Available!="110.00"||fund.CurrentView.Planned!="0.00"||fund.CurrentView.Draft.Opening!="125.00"||fund.CurrentView.Draft.Suggestion!="12.50")throw new InvalidOperationException("Restored display values");
   yield return Capture("fund-restored");CloseFund();results.Add("reopen: imported profile exact hashes/history/seen IDs, one genesis, preserved original, zero generation");
  }
  IEnumerator Capture(string name) {
   if(Screen.width!=1600||Screen.height!=900)throw new InvalidOperationException("Parent must launch 1600x900");
   yield return new WaitForEndOfFrame();string path=Path.Combine(paths.ArtifactDirectory,name+".png");ShoppingSmokePolicy.Ancestors(path);if(File.Exists(path))throw new IOException("Refuse stale screenshot");ScreenCapture.CaptureScreenshot(path);
   var validator=typeof(Phase6ShoppingSmokeProbe).GetMethod("Png",BindingFlags.Static|BindingFlags.NonPublic);if(validator==null)throw new MissingMethodException("existing CRC/IEND PNG validator");
   yield return Wait(()=>(bool)validator.Invoke(null,new object[]{path,1600,900}),"complete screenshot PNG CRC/IEND",12);
   screenshots.Add(path);RecordHash(path);
  }
  string Session() {
   var packs=new List<PackRecordData>();foreach(var p in game.CurrentSession.Packs)packs.Add(new PackRecordData{transactionId=p.TransactionId,createdUtc=p.CreatedUtc,modelId=p.ModelId,modelVersion=p.ModelVersion,catalogVersion=p.CatalogVersion,printingId=p.PrintingId,rngAlgorithm=p.RngAlgorithm,status=p.Status,seed=p.Seed,orderedCardIds=new List<string>(p.OrderedCardIds).ToArray(),newToVirtual=new List<bool>(p.NewToVirtual).ToArray(),revealedCount=p.RevealedCount,wrapperOpened=p.WrapperOpened});
   return JsonUtility.ToJson(new SessionData{schemaVersion=1,revision=game.CurrentSession.Revision,packs=packs.ToArray()});
  }
  static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  static byte[] ReadBytes(string path,int cap){ShoppingSmokePolicy.Ancestors(path);using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){if(stream.Length>cap)throw new InvalidDataException("Fixture file exceeds bound");byte[] b=new byte[(int)stream.Length];int n=0;while(n<b.Length){int got=stream.Read(b,n,b.Length-n);if(got==0)throw new EndOfStreamException();n+=got;}return b;}}
  static string HashFile(string path){return Hash(ReadBytes(path,64*1024*1024));}
  static string ReadText(string path,int cap){return new UTF8Encoding(false,true).GetString(ReadBytes(path,cap));}
  void RecordHash(string path){hashes.Add(new FileHash{path=path,sha256=HashFile(path)});}
  static Dictionary<string,byte[]> ReadZip(string path){var files=new Dictionary<string,byte[]>(StringComparer.Ordinal);using(var mem=new MemoryStream(ReadBytes(path,64*1024*1024)))using(var zip=new ZipArchive(mem,ZipArchiveMode.Read)){if(zip.Entries.Count>14)throw new InvalidDataException("Complete archive member bound");foreach(var e in zip.Entries){if(e.Length>8*1024*1024)throw new InvalidDataException("Sidecar bound");using(var input=e.Open())using(var output=new MemoryStream()){input.CopyTo(output);files.Add(e.FullName,output.ToArray());}}}return files;}
  void SaveExpected(Expected expected){string path=Path.Combine(runRoot,"expected.json");ShoppingSmokePolicy.Ancestors(path);File.WriteAllText(path,JsonUtility.ToJson(expected,true),new UTF8Encoding(false,true));}
  void OnShutdown() {
   shutdownSeen=true;game.ShutdownCompleted-=OnShutdown;
   try{CheckThat("shutdown-actual-owners-disposed",Field(fund,"worker")==null&&Field(fund,"lease")==null&&Field(shopping,"store")==null&&Field(game,"store")==null&&fund.CurrentView==null&&shoppingPanel.Current==null);}
   catch(Exception e){Fail("shutdown-verification",e);}WriteReport();
  }
  void WriteReport() {
   if(!fixtureAdmitted){Debug.LogError("Final report refused: canonical fixture marker not admitted");return;}
   bool capturesComplete=mode=="first"
    ? screenshots.Contains(Path.Combine(paths.ArtifactDirectory,"fund-initial.png"))&&screenshots.Contains(Path.Combine(paths.ArtifactDirectory,"fund-retry.png"))
    : screenshots.Contains(Path.Combine(paths.ArtifactDirectory,"fund-restored.png"));
   bool passed=completed&&shutdownSeen&&checks.Count>0&&checks.TrueForAll(c=>c.passed)&&capturesComplete;
   var report=new Report{passed=passed,workflowCompleted=completed,shutdownCompleted=shutdownSeen,mode=mode,checkCount=checks.Count,checks=checks.ToArray(),results=results.ToArray(),resultCount=results.Count,financialSnapshots=finances.ToArray(),financialSnapshotCount=finances.Count,copiedFileHashes=hashes.ToArray(),copiedFileHashCount=hashes.Count,screenshots=screenshots.ToArray(),screenshotCount=screenshots.Count,sessionJson=sessionAtExit,activeProfileId=activeAtExit,generatedResultCount=generatedAtExit,failure=failure};
   string path=Path.Combine(paths.ArtifactDirectory,"final-report.json");ShoppingSmokePolicy.Ancestors(path);File.WriteAllText(path,JsonUtility.ToJson(report,true),new UTF8Encoding(false,true));
  }
 }
}
