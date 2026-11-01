using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VintageCollector.Domain;
using VintageCollector.Collections;
namespace VintageCollector.Shopping {
 public interface IShoppingFaultInjector {void At(string stage);}
 public sealed class ShoppingNoteRequest {
  public string OperationId{get;private set;}public string RecordId{get;private set;}public bool IsDelete{get;private set;}public FrozenComparison Comparison{get;private set;}
  ShoppingNoteRequest(){}
  internal static void Id(string s){Guid g;if(s==null||!Guid.TryParseExact(s,"N",out g)||g==Guid.Empty||g.ToString("N")!=s)throw new ArgumentException("Nonempty lowercase Guid N required");}
  public static ShoppingNoteRequest Upsert(string operationId,FrozenComparison comparison){Id(operationId);if(comparison==null)throw new ArgumentNullException("comparison");return new ShoppingNoteRequest{OperationId=operationId,RecordId=comparison.ToData().recordId,Comparison=comparison};}
  public static ShoppingNoteRequest Delete(string operationId,string recordId){Id(operationId);Id(recordId);return new ShoppingNoteRequest{OperationId=operationId,RecordId=recordId,IsDelete=true};}
  internal string Fingerprint(){return ShoppingJsonCodec.Hash(System.Text.Encoding.UTF8.GetBytes((IsDelete?"delete|":"upsert|")+RecordId+"|"+(IsDelete?"":System.Text.Encoding.UTF8.GetString(ShoppingJsonCodec.Encode(Comparison.ToData())))));}
 }
 public sealed class ShoppingNotesSnapshot {
  public long Revision{get;private set;}public ReadOnlyCollection<FrozenComparison> Records{get;private set;}
  internal ShoppingNotesSnapshot(long rev,ReadOnlyCollection<FrozenComparison> records){Revision=rev;Records=records;}
 }
 public sealed class ShoppingNotesStore:IDisposable {
  const string Zero="0000000000000000000000000000000000000000000000000000000000000000";
  readonly string dir;readonly ApprovedSetRegistry registry;readonly IShoppingFaultInjector faults;FileStream lease;
  MetadataEnvelope current;ShoppingNotesSnapshot snapshot;ShoppingNoteRequest pending;string pendingExpectedCandidateHash;long known;bool disposed,uncertain;
  public bool IsUncertain{get{return uncertain;}}
  public ShoppingNotesSnapshot Snapshot{get{Alive();if(uncertain)throw new InvalidOperationException("Shopping save uncertain; retry exact request");return snapshot;}}
  public ShoppingNotesStore(string profileRoot,ApprovedSetRegistry registry,IShoppingFaultInjector faults=null){
   if(registry==null)throw new ArgumentNullException("registry");this.registry=registry;this.faults=faults;string root=CollectionFiles.Local(profileRoot);if(!Directory.Exists(root))throw new DirectoryNotFoundException("Validated profile root must exist");dir=Path.Combine(root,"Shopping");
   Inspect(); // No storage mutation when preexisting content is invalid.
   ValidateCopies();
   try{Check(dir);Directory.CreateDirectory(dir);string lockPath=PathFor("lock");Check(lockPath);lease=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);Reload();}catch{if(lease!=null)lease.Dispose();throw;}
  }
  void Alive(){if(disposed)throw new ObjectDisposedException("ShoppingNotesStore");}
  static void Check(string p){CollectionFiles.Check(p);}
  string PathFor(string suffix){return Path.Combine(dir,"shopping."+suffix);}
  void Inspect(){Check(dir);if(File.Exists(dir))throw new InvalidDataException("Shopping is not a directory");if(!Directory.Exists(dir))return;var allowed=new HashSet<string>(StringComparer.Ordinal){"shopping.lock","shopping.state.json","shopping.backup.json","shopping.prepare.json","shopping.ack.json","shopping.write.tmp"};foreach(string p in Directory.GetFileSystemEntries(dir)){Check(p);if(Directory.Exists(p)||!allowed.Contains(Path.GetFileName(p)))throw new InvalidDataException("Foreign shopping path");}}
  MetadataEnvelope Empty(){return new MetadataEnvelope{revision=0,previousHash=Zero,lastOperationId="",lastOperationFingerprint="",contentHash=Zero,comparisons=new ComparisonData[0],operations=new OperationReceipt[0]};}
  static bool Hex(string s){if(s==null||s.Length!=64)return false;foreach(char c in s)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;return true;}
  byte[] Bytes(string path){Check(path);using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){if(f.Length<=0||f.Length>ShoppingJsonCodec.MaxBytes)throw new InvalidDataException("Shopping file byte limit");byte[] b=new byte[(int)f.Length];int at=0,n;while(at<b.Length&&(n=f.Read(b,at,b.Length-at))>0)at+=n;if(at!=b.Length||f.ReadByte()!=-1)throw new InvalidDataException("Shopping file changed or truncated");return b;}}
  MetadataEnvelope Read(string suffix){string p=PathFor(suffix);Check(p);if(!File.Exists(p))return null;var e=ShoppingJsonCodec.Decode<MetadataEnvelope>(Bytes(p));Validate(e);return e;}
  void Validate(MetadataEnvelope e){
   if(e.schemaVersion!=1||e.revision<1||e.operations.Length!=e.revision||e.operations.Length>10000||!Hex(e.previousHash)||!Hex(e.contentHash)||!Hex(e.lastOperationFingerprint))throw new InvalidDataException("Unsupported shopping schema or lineage");ShoppingNoteRequest.Id(e.lastOperationId);
   var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var op in e.operations){ShoppingNoteRequest.Id(op.operationId);if(!Hex(op.fingerprint)||!ids.Add(op.operationId))throw new InvalidDataException("Invalid operation receipt");}
   var last=e.operations[e.operations.Length-1];if(last.operationId!=e.lastOperationId||last.fingerprint!=e.lastOperationFingerprint)throw new InvalidDataException("Last operation identity mismatch");
   ComparisonValidator.FreezeMany(registry,e.comparisons);string hash=e.contentHash;e.contentHash="";string actual=ShoppingJsonCodec.Hash(ShoppingJsonCodec.Encode(e));e.contentHash=hash;if(hash!=actual)throw new InvalidDataException("Shopping content hash mismatch");
  }
  static void Link(MetadataEnvelope child,MetadataEnvelope parent){if(child.revision!=parent.revision+1||child.previousHash!=parent.contentHash||child.operations.Length!=parent.operations.Length+1)throw new InvalidDataException("Shopping lineage mismatch");for(int i=0;i<parent.operations.Length;i++)if(child.operations[i].operationId!=parent.operations[i].operationId||child.operations[i].fingerprint!=parent.operations[i].fingerprint)throw new InvalidDataException("Shopping operation lineage mismatch");}
  MetadataEnvelope[] ValidateCopies(){
   Inspect();var state=Read("state.json");var backup=Read("backup.json");var prepare=Read("prepare.json");var ack=Read("ack.json");
   // Even an interrupted staging file is evidence, never silently discard malformed bytes.
   var temp=Read("write.tmp");
   // Bind every same-known-revision copy to the retained live acknowledgement before repair or adoption.
   if(current!=null&&known>0)foreach(var copy in new[]{state,backup,prepare,ack,temp})if(copy!=null&&copy.revision==known&&copy.contentHash!=current.contentHash)throw new InvalidDataException("Live acknowledged hash drift");
   if(temp!=null){bool match=prepare!=null&&temp.contentHash==prepare.contentHash||state!=null&&temp.contentHash==state.contentHash||backup!=null&&temp.contentHash==backup.contentHash||ack!=null&&temp.contentHash==ack.contentHash;if(!match)throw new InvalidDataException("Unbound shopping staging evidence");}
   if(state==null){if(backup!=null||ack!=null||known>0)throw new InvalidDataException("Missing acknowledged shopping state");if(prepare!=null)Link(prepare,Empty());}
   else{
    if(state.revision<known)throw new InvalidDataException("Shopping revision rollback");if(state.revision==1&&state.previousHash!=Zero)throw new InvalidDataException("Invalid genesis");
    if(backup!=null){if(backup.contentHash!=state.contentHash)Link(state,backup);}else if(state.revision>1)throw new InvalidDataException("Missing shopping backup anchor");
    if(ack!=null&&ack.contentHash!=state.contentHash){Link(state,ack);if(prepare==null||prepare.contentHash!=state.contentHash)throw new InvalidDataException("Unacknowledged state lacks prepare");}
    if(ack==null&&prepare==null)throw new InvalidDataException("Shopping acknowledgement missing");
    if(prepare!=null&&prepare.contentHash!=state.contentHash)Link(prepare,state);
   }
   // A copied receipt is not proof of the payload. Retain the locally built whole-candidate hash across uncertainty.
   if(pending!=null){string intent=pending.Fingerprint();if(prepare!=null&&(prepare.lastOperationId!=pending.OperationId||prepare.lastOperationFingerprint!=intent||prepare.contentHash!=pendingExpectedCandidateHash))throw new InvalidDataException("Durable prepare differs from retained exact intent");foreach(var copy in new[]{state,backup,ack,temp})if(copy!=null)foreach(var op in copy.operations)if(op.operationId==pending.OperationId&&(op.fingerprint!=intent||copy.contentHash!=pendingExpectedCandidateHash))throw new InvalidDataException("Committed operation differs from retained exact intent");}
   return new[]{state,backup,prepare,ack};
  }
  void Cut(string stage){if(faults!=null)faults.At(stage);}
  void Atomic(string suffix,MetadataEnvelope e){string temp=PathFor("write.tmp"),target=PathFor(suffix);Check(dir);Check(temp);Check(target);byte[] bytes=ShoppingJsonCodec.Encode(e);using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}Check(temp);Check(target);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);if(Read(suffix).contentHash!=e.contentHash)throw new IOException("Shopping readback mismatch");}
  void Remove(string suffix){string p=PathFor(suffix);Check(p);if(File.Exists(p))File.Delete(p);}
  void Publish(MetadataEnvelope candidate,MetadataEnvelope baseline){
   if(baseline.revision>0){Cut("beforeBackup");Atomic("backup.json",baseline);Cut("afterBackup");}
   Cut("beforeReplace");Atomic("state.json",candidate);Cut("afterReplace");Cut("beforeAck");Atomic("ack.json",candidate);Cut("afterAck");Remove("prepare.json");Remove("write.tmp");
  }
  public ShoppingNotesSnapshot Reload(){Alive();try{var copies=ValidateCopies();var state=copies[0];var prepare=copies[2];if(prepare!=null){if(state==null||state.contentHash!=prepare.contentHash)Publish(prepare,state??Empty());else {Atomic("ack.json",state);Remove("prepare.json");Remove("write.tmp");}state=prepare;}current=state??Empty();known=Math.Max(known,current.revision);snapshot=new ShoppingNotesSnapshot(current.revision,ComparisonValidator.FreezeMany(registry,current.comparisons));if(pending==null)uncertain=false;return snapshot;}catch{uncertain=true;throw;}}
  public ShoppingNotesSnapshot Apply(ShoppingNoteRequest request){
   Alive();if(request==null)throw new ArgumentNullException("request");if(pending!=null&&!object.ReferenceEquals(pending,request))throw new InvalidOperationException("Retry the exact retained shopping request instance");
   string fingerprint=request.Fingerprint();if(request.Comparison!=null)ComparisonValidator.Freeze(registry,request.Comparison.ToData());
   Reload();foreach(var op in current.operations)if(op.operationId==request.OperationId){if(op.fingerprint!=fingerprint)throw new InvalidOperationException("Shopping operation ID collision");pending=null;pendingExpectedCandidateHash=null;uncertain=false;return snapshot;}
   if(current.operations.Length>=10000)throw new InvalidOperationException("Shopping operation receipt capacity reached; no mutation performed");
   var records=new List<ComparisonData>();foreach(var item in current.comparisons)if(item.recordId!=request.RecordId)records.Add(item);if(!request.IsDelete)records.Add(request.Comparison.ToData());var frozen=ComparisonValidator.FreezeMany(registry,records.ToArray());
   var ops=new List<OperationReceipt>(current.operations);ops.Add(new OperationReceipt{operationId=request.OperationId,fingerprint=fingerprint});
   var candidate=new MetadataEnvelope{schemaVersion=1,revision=checked(current.revision+1),previousHash=current.contentHash,lastOperationId=request.OperationId,lastOperationFingerprint=fingerprint,contentHash="",comparisons=records.ToArray(),operations=ops.ToArray()};candidate.contentHash=ShoppingJsonCodec.Hash(ShoppingJsonCodec.Encode(candidate));Validate(candidate);
   if(pending!=null&&candidate.contentHash!=pendingExpectedCandidateHash)throw new InvalidDataException("Rebuilt shopping candidate differs from retained exact intent");
   Cut("beforePrepare");pending=request;pendingExpectedCandidateHash=candidate.contentHash;try{Atomic("prepare.json",candidate);uncertain=true;Cut("afterPrepare");Publish(candidate,current);current=candidate;known=current.revision;snapshot=new ShoppingNotesSnapshot(current.revision,frozen);pending=null;pendingExpectedCandidateHash=null;uncertain=false;return snapshot;}catch{uncertain=true;throw;}
  }
  public Dictionary<string,byte[]> ExportAcknowledgedFiles(){
   Alive();if(uncertain||pending!=null)throw new InvalidOperationException("Shopping acknowledgement uncertain; retry exact request before backup");
   var files=ShoppingArchive.CaptureAcknowledgedFiles(Path.GetDirectoryName(dir),registry);
   if(ShoppingArchive.ContentHash(files)!=current.contentHash)throw new InvalidDataException("Shopping disk differs from live acknowledged candidate");return files;
  }
  public void Dispose(){if(disposed)return;disposed=true;if(lease!=null)lease.Dispose();}
 }
}
