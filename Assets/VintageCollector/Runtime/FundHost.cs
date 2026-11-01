using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using VintageCollector.Funds;
using VintageCollector.Shopping;
using VintageCollector.Presentation;
namespace VintageCollector.Runtime {
 [DefaultExecutionOrder(180), DisallowMultipleComponent]
 public sealed class FundHost : MonoBehaviour {
  OpeningGame game; Phase5CollectionHost collection; BinderPresentation binder; FundPresentation panel;
  FundWorker worker; bool unavailable,uncertain; LedgerSnapshot acknowledged; LedgerRequest pending; string pendingPack,profileRoot,profileId,message="";
  IDisposable lease; bool bound,closed; long version,draftVersion; FundDraft draft=new FundDraft();
  readonly HashSet<string> seen=new HashSet<string>(StringComparer.Ordinal);
  readonly HashSet<string> queued=new HashSet<string>(StringComparer.Ordinal);
  readonly Queue<string> queue=new Queue<string>(); int historyPage,planPage;
  Func<string,Dictionary<string,byte[]>> capture;
  public LedgerSnapshot Snapshot {get{return acknowledged;}}
  public FundView CurrentView {get;private set;}
  public bool IsBusy {get{return worker!=null&&worker.IsBusy;}}
  public Func<string,string[],LedgerStore> StoreFactory {get;set;}
  void Update() {
   if(closed)return;
   if(!bound) { if(!StartupLoadingCoordinator.IsReady)return;var g=GetComponent<OpeningGame>();var c=GetComponent<Phase5CollectionHost>();var p=GetComponent<FundPresentation>();var b=GetComponent<BinderPresentation>();
    if(g!=null&&c!=null&&p!=null&&b!=null&&c.IsReady) Bind(g,c,p,b);return; }
   if(!ProfileMatches()) { Shutdown();return; }
   DrainWorker();ObserveOpened();
   if(lease!=null) {if(!Authority())Close();}
  }
  public void Bind(OpeningGame g,Phase5CollectionHost c,FundPresentation p,BinderPresentation b) {
   if(bound||closed)return;
   if(!StartupLoadingCoordinator.IsReady||g==null||c==null||p==null||b==null||!c.IsReady||c.Game!=g||g.Paths==null||g.CurrentSession==null||c.Profiles==null||g.gameObject!=gameObject||c.gameObject!=gameObject||p.gameObject!=gameObject||b.gameObject!=gameObject)throw new InvalidOperationException("Fund requires ready same-object profile owners.");
   game=g;collection=c;panel=p;binder=b;profileRoot=Path.GetFullPath(g.Paths.SaveDirectory);profileId=c.Profiles.ActiveProfileId;
   if(!ProfileMatches())throw new InvalidOperationException("Fund profile mismatch.");
   var history=new List<string>();foreach(var pack in g.CurrentSession.Packs)if(pack.WrapperOpened)history.Add(pack.TransactionId);
   bound=true; binder.FundRequested+=Open; collection.FundInvalidated+=Close;game.ShuttingDown+=Shutdown;panel.ActionRequested+=Dispatch;panel.DraftEdited+=Edit;panel.Authority=Authority;
   capture=CaptureSidecars;collection.Profiles.SidecarCapture=capture;collection.FundEnabled=true;
   var factory=StoreFactory;worker=new FundWorker(profileRoot,history.ToArray(),factory);message="Loading optional fund on its serial worker. Pack opening remains available.";
  }
  bool ProfileMatches(){return !closed&&game!=null&&collection!=null&&collection.IsReady&&collection.Game==game&&game.Paths!=null&&collection.Profiles!=null&&collection.Profiles.ActiveProfileId==profileId&&SameRoot(game.Paths.SaveDirectory)&&SameRoot(collection.Profiles.ActiveDirectory);}
  bool SameRoot(string path){return string.Equals(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),profileRoot.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);}
  bool Authority(){return bound&&!closed&&lease!=null&&StartupLoadingCoordinator.IsReady&&game.IsAcceptingInput&&ProfileMatches()&&collection.CanUseFund();}
  public void Open(){if(closed||!bound||lease!=null||!ProfileMatches()||!StartupLoadingCoordinator.IsReady)return;IDisposable acquired;if(!collection.TryBeginFund(out acquired))return;lease=acquired;Publish();}
  public void Close(){CurrentView=null;if(panel!=null)panel.Bind(null);var old=lease;lease=null;if(old!=null)old.Dispose();}
  void LocalError(Exception e){message="Fund unavailable ("+e.GetType().Name+"). Files and drafts retained. No money moved; ordinary pack opening is unaffected. Close or Retry explicitly.";}
  void LoadSettings(){if(acknowledged==null)return;draftVersion++;draft.Opening=Usd(acknowledged.OpeningBalanceCents);draft.Suggestion=Usd(acknowledged.SuggestedContributionCents);draft.Enabled=acknowledged.Enabled;}
  public static string Usd(long cents){return (cents/100m).ToString("0.00",CultureInfo.InvariantCulture);}
  public static long ParseAmount(string text,bool signed){long n;bool negative=signed&&text!=null&&text.StartsWith("-",StringComparison.Ordinal);if(!Money.TryParseUsd(negative?text.Substring(1):text,out n))throw new ArgumentException("Use USD digits with at most two decimal places.");return negative?checked(-n):n;}
  void Edit(FundDraft value){if(!Authority()||pending!=null||IsBusy||unavailable||value==null)return;draft=value.Copy();}
  void ObserveOpened(){if(!ProfileMatches()||game.HasPendingStorage||game.CurrentSession==null)return;
   // Initial snapshot SeenPackIds, not current session history, is authority on existing ledgers.
   foreach(var pack in game.CurrentSession.Packs)if(pack.WrapperOpened&&!seen.Contains(pack.TransactionId)&&queued.Add(pack.TransactionId))queue.Enqueue(pack.TransactionId);
   if(worker==null||worker.IsBusy||unavailable||pending!=null||uncertain||acknowledged==null)return;
   while(queue.Count>0&&seen.Contains(queue.Peek())){queued.Remove(queue.Dequeue());}
   if(queue.Count>0){pendingPack=queue.Peek();pending=LedgerRequest.ObservePack(Guid.NewGuid().ToString("N"),pendingPack);ApplyPending();}
  }
  void ApplyPending(){if(pending==null||worker==null||worker.IsBusy||unavailable)return;worker.Begin(pending,false);message="Fund write pending — last acknowledged balances shown. No money moved.";if(lease!=null)Publish();}
  void DrainWorker(){if(worker==null)return;FundWorker.Result result;if(!worker.TryTake(out result))return;
   unavailable=result.Unavailable;uncertain=result.Uncertain;
   if(result.Error!=null){LocalError(result.Error);if(pending!=null&&pendingPack==null&&!result.Uncertain&&!result.Unavailable&&(result.Error is ArgumentException||result.Error is OverflowException||result.Error is InvalidOperationException)){pending=null;pendingPack=null;message="Fund input rejected ("+result.Error.GetType().Name+"). Drafts and last acknowledged balances retained; edit and submit again. No money moved.";}if(pending!=null)message+=" Exact intent retained; Retry only. Complete backup/CSV blocked.";}
   else {bool first=acknowledged==null;acknowledged=result.Snapshot;seen.Clear();foreach(var id in acknowledged.SeenPackIds)seen.Add(id);
    if(pending!=null){pending=null;pendingPack=null;}
    while(queue.Count>0&&seen.Contains(queue.Peek()))queued.Remove(queue.Dequeue());
    if(first)LoadSettings();message="Fund acknowledged locally. Bank not connected; no money moved.";}
   if(lease!=null)Publish();
  }
  bool PlanExists(string id){if(acknowledged==null||string.IsNullOrEmpty(id))return false;foreach(var p in acknowledged.PendingPlans)if(p.Id==id)return true;return false;}
  public void Dispatch(string action,string value=null){
   if(action=="quit"){if(game!=null)game.RequestQuit();return;}if(action=="close"){Close();return;}if(!Authority())return;
   try {
    if(action=="history-prev")historyPage=Math.Max(0,historyPage-1);
    else if(action=="history-next")historyPage++;
    else if(action=="plans-prev")planPage=Math.Max(0,planPage-1);
    else if(action=="plans-next")planPage++;
    else if(action=="retry") {if(worker.IsBusy)message="Fund worker is busy. Close remains available.";else if(pending!=null)ApplyPending();else if(!unavailable){worker.Begin(null,true);message="Reload pending; drafts retained.";}else {worker.Begin(null,false);message="Retrying initialization without resetting any files.";}}
    else if(action=="csv") ExportCsv();
    else if(pending!=null||worker==null||worker.IsBusy||unavailable||uncertain)message="Resolve the retained fund request before changing the ledger.";
    else {
     string id=Guid.NewGuid().ToString("N");LedgerRequest request=null;
     if(action=="configure")request=LedgerRequest.Configure(id,ParseAmount(draft.Opening,false),ParseAmount(draft.Suggestion,false),draft.Enabled);
     else if(action=="confirm"&&PlanExists(value))request=LedgerRequest.Confirm(id,value);
     else if(action=="cancel"&&PlanExists(value))request=LedgerRequest.Cancel(id,value);
     else if(action=="deposit")request=LedgerRequest.Contribution(id,Positive(draft.Amount),RequireNote(draft.Note));
     else if(action=="purchase")request=LedgerRequest.Purchase(id,Positive(draft.Amount),RequireNote(draft.Note));
     else if(action=="adjust") {long n=ParseAmount(draft.Amount,true);if(n==0)throw new ArgumentException("Correction must be nonzero.");request=LedgerRequest.Adjustment(id,n,RequireNote(draft.Note));}
     if(request!=null){pending=request;pendingPack=null;ApplyPending();}
    }
   }catch(ArgumentException e){message="Fund input rejected: "+e.Message+" No money moved.";}catch(Exception e){LocalError(e);}Publish();
  }
  static long Positive(string value){long n=ParseAmount(value,false);if(n<=0)throw new ArgumentException("Amount must be positive.");return n;}
  static string RequireNote(string value){if(string.IsNullOrWhiteSpace(value)||value.Length>500)throw new ArgumentException("Enter an audit note (1–500 characters).");return value;}
  void ExportCsv(){if(worker==null||worker.IsBusy||unavailable||pending!=null||uncertain||queue.Count!=0||acknowledged==null)throw new InvalidOperationException("Only a complete acknowledged fund can be exported.");worker.CaptureIdle();
   string dir=Path.Combine(profileRoot,"Exports");VintageCollector.Collections.CollectionFiles.Check(dir);Directory.CreateDirectory(dir);VintageCollector.Collections.CollectionFiles.Check(dir);
   string path=Path.Combine(dir,"fund-"+Guid.NewGuid().ToString("N")+".csv");byte[] bytes=new UTF8Encoding(false,true).GetBytes(LedgerCsv.Export(acknowledged));VintageCollector.Collections.CollectionFiles.Check(path);using(var f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}message="CSV written locally: "+path;
  }
  public Dictionary<string,byte[]> CaptureSidecars(string requestedRoot){if(!bound||!ProfileMatches()||!SameRoot(requestedRoot)||worker==null||worker.IsBusy||unavailable||uncertain||pending!=null||queue.Count!=0)throw new InvalidOperationException("Complete backup requires acknowledged same-profile fund and shopping data.");
   var result=new Dictionary<string,byte[]>(StringComparer.Ordinal);foreach(var pair in worker.CaptureIdle())AddCaptured(result,"Fund/",pair);
   var shopping=GetComponent<Phase6ShoppingHost>();if(shopping==null)throw new InvalidOperationException("Shopping capture owner unavailable.");foreach(var pair in shopping.CaptureShoppingBackupFiles())AddCaptured(result,"Shopping/",pair);return result;
  }
  static void AddCaptured(Dictionary<string,byte[]> target,string prefix,KeyValuePair<string,byte[]> pair){if(string.IsNullOrEmpty(pair.Key)||pair.Key!=Path.GetFileName(pair.Key)||pair.Key.IndexOfAny(new[]{'/','\\',':'})>=0||pair.Value==null)throw new InvalidOperationException("Invalid sidecar basename.");target.Add(prefix+pair.Key,(byte[])pair.Value.Clone());}
  void Publish(){if(!Authority())return;var plans=new List<FundRow>();var history=new List<FundRow>();
   if(acknowledged!=null){planPage=Math.Min(planPage,Math.Max(0,(acknowledged.PendingPlans.Count-1)/8));historyPage=Math.Min(historyPage,Math.Max(0,(acknowledged.Entries.Count-1)/12));int i=0;foreach(var p in acknowledged.PendingPlans){if(i/8==planPage)plans.Add(Row(p));i++;}for(int j=acknowledged.Entries.Count-1-historyPage*12;j>=0&&history.Count<12;j--)history.Add(Row(acknowledged.Entries[j]));}
   CurrentView=new FundView {Version=++version,DraftVersion=draftVersion,ProfileId=profileId,Message=message,Available=acknowledged==null?"Unavailable":Usd(acknowledged.ConfirmedCents),Planned=acknowledged==null?"Unavailable":Usd(acknowledged.PlannedCents),Busy=IsBusy,CanMutate=worker!=null&&!IsBusy&&!unavailable&&pending==null&&!uncertain,CanRetry=worker!=null&&!IsBusy,CanExport=worker!=null&&!IsBusy&&!unavailable&&pending==null&&!uncertain&&queue.Count==0,Draft=draft.Copy(),Plans=plans.ToArray(),History=history.ToArray(),PlanPage=planPage+1,HistoryPage=historyPage+1};panel.Bind(CurrentView);
  }
  static FundRow Row(LedgerEntry e){return new FundRow {Id=e.Id,Caption=e.Kind+"  USD "+Usd(e.AmountCents)+"  "+e.CreatedUtc+"  "+e.Note};}
  public void Shutdown(){if(closed)return;closed=true;try{Close();}finally{if(bound){binder.FundRequested-=Open;collection.FundInvalidated-=Close;game.ShuttingDown-=Shutdown;panel.ActionRequested-=Dispatch;panel.DraftEdited-=Edit;panel.Authority=null;collection.FundEnabled=false;if(collection.Profiles!=null&&collection.Profiles.SidecarCapture==capture)collection.Profiles.SidecarCapture=null;bound=false;}if(worker!=null){var owned=worker;worker=null;owned.Dispose();}}}

  // Exactly one plain-C# store owner. No Unity objects or callbacks run on this thread.
  sealed class FundWorker : IDisposable {
   internal sealed class Result {internal LedgerSnapshot Snapshot;internal bool Uncertain,Unavailable;internal Exception Error;}
   readonly object gate=new object();readonly Thread thread;readonly string root;readonly string[] history;readonly Func<string,string[],LedgerStore> factory;
   Exception disposeError;LedgerStore store;LedgerRequest request;bool reload,hasJob=true,busy=true,stop;Result completed;
   internal FundWorker(string root,string[] history,Func<string,string[],LedgerStore> factory){this.root=root;this.history=(string[])history.Clone();this.factory=factory;thread=new Thread(Run){IsBackground=true,Name="Collection fund IO"};thread.Start();}
   internal bool IsBusy {get{lock(gate)return busy;}}
   internal void Begin(LedgerRequest exact,bool reload){lock(gate){if(stop||busy)throw new InvalidOperationException("Fund worker busy.");request=exact;this.reload=reload;busy=true;hasJob=true;Monitor.PulseAll(gate);}}
   internal bool TryTake(out Result value){lock(gate){value=completed;if(value==null)return false;completed=null;busy=false;return true;}}
   void Run(){try{while(true){LedgerRequest exact;bool loading;lock(gate){while(!hasJob&&!stop)Monitor.Wait(gate);if(stop&&!hasJob)return;hasJob=false;exact=request;loading=reload;}
     var result=new Result();try{if(store==null)store=factory==null?new LedgerStore(root,history):factory(root,history);result.Snapshot=exact!=null?store.Apply(exact):loading?store.Reload():store.Snapshot;result.Uncertain=store.IsUncertain;}
     catch(Exception e){result.Error=e;result.Unavailable=store==null;result.Uncertain=store!=null&&store.IsUncertain;}
     lock(gate){completed=result;}
    }}finally{try{if(store!=null)store.Dispose();}catch(Exception e){disposeError=e;}}}
   internal Dictionary<string,byte[]> CaptureIdle(){lock(gate){if(stop||busy||hasJob||completed!=null||store==null||store.IsUncertain)throw new InvalidOperationException("Fund worker must be idle and acknowledged.");return store.ExportAcknowledgedFiles();}}
   public void Dispose(){lock(gate){stop=true;Monitor.PulseAll(gate);}thread.Join();if(disposeError!=null)throw new InvalidOperationException("Fund worker close failed.",disposeError);}
  }

  void OnDestroy(){Shutdown();}
 }
}
