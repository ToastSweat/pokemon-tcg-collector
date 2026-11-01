using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using VintageCollector.Collections;
namespace VintageCollector.Funds {
 public interface ILedgerFaultInjector {void At(string stage);}
 public sealed class LedgerStore:IDisposable {
  const string Zero="0000000000000000000000000000000000000000000000000000000000000000";
  readonly string dir;readonly ILedgerFaultInjector faults;FileStream lease;
  LedgerData current;LedgerSnapshot snapshot;LedgerRequest pending;string pendingExpectedCandidateHash;long known;bool disposed,uncertain;
  public bool IsUncertain{get{return uncertain;}}
  public LedgerSnapshot Snapshot{get{Alive();if(uncertain)throw new InvalidOperationException("Fund save uncertain; retry exact request");return snapshot;}}
  public LedgerStore(string profileRoot,string[] alreadyOpenedPackIds,ILedgerFaultInjector faults=null){
   if(alreadyOpenedPackIds==null)throw new ArgumentNullException("alreadyOpenedPackIds");this.faults=faults;string root=CollectionFiles.Local(profileRoot);if(!Directory.Exists(root))throw new DirectoryNotFoundException("Validated profile root must exist");dir=Path.Combine(root,"Fund");
   Inspect(); // No storage mutation when preexisting content is invalid.
   ValidateCopies();
   if(Directory.Exists(dir)&&Directory.GetFileSystemEntries(dir).Length>0&&!File.Exists(PathFor("state.json"))&&!File.Exists(PathFor("prepare.json")))throw new InvalidDataException("Existing fund evidence cannot become blank genesis");
   var genesis=!File.Exists(PathFor("state.json"))&&!File.Exists(PathFor("prepare.json"))?LedgerRules.Genesis(alreadyOpenedPackIds):null;
   try{Check(dir);Directory.CreateDirectory(dir);string lockPath=PathFor("lock");Check(lockPath);lease=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);Reload();if(current.revision==0){Atomic("prepare.json",genesis);Publish(genesis,Empty());Reload();}}catch{if(lease!=null)lease.Dispose();throw;}
  }
  void Alive(){if(disposed)throw new ObjectDisposedException("LedgerStore");}
  static void Check(string p){CollectionFiles.Check(p);}
  string PathFor(string suffix){return Path.Combine(dir,"fund."+suffix);}
  void Inspect(){Check(dir);if(File.Exists(dir))throw new InvalidDataException("Fund is not a directory");if(!Directory.Exists(dir))return;var allowed=new HashSet<string>(StringComparer.Ordinal){"fund.lock","fund.state.json","fund.backup.json","fund.prepare.json","fund.ack.json","fund.write.tmp"};foreach(string p in Directory.GetFileSystemEntries(dir)){Check(p);if(Directory.Exists(p)||!allowed.Contains(Path.GetFileName(p)))throw new InvalidDataException("Foreign fund path");}}
  LedgerData Empty(){return new LedgerData{revision=0,previousHash=Zero,contentHash=Zero,entries=new LedgerEntryData[0],operations=new LedgerOperationData[0],seenPackIds=new string[0]};}
  static bool Hex(string s){if(s==null||s.Length!=64)return false;foreach(char c in s)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;return true;}
  byte[] Bytes(string path){Check(path);using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){if(f.Length<=0||f.Length>LedgerJsonCodec.MaxBytes)throw new InvalidDataException("Fund file byte limit");byte[] b=new byte[(int)f.Length];int at=0,n;while(at<b.Length&&(n=f.Read(b,at,b.Length-at))>0)at+=n;if(at!=b.Length||f.ReadByte()!=-1)throw new InvalidDataException("Fund file changed or truncated");return b;}}
  LedgerData Read(string suffix){string p=PathFor(suffix);Check(p);if(!File.Exists(p))return null;var e=LedgerJsonCodec.Decode<LedgerData>(Bytes(p));Validate(e);return e;}
  void Validate(LedgerData e){LedgerRules.Validate(e);}
  static void Link(LedgerData child,LedgerData parent){if(parent.revision==0){if(child.revision!=1||child.previousHash!=Zero)throw new InvalidDataException("Genesis lineage");}else LedgerRules.Link(child,parent);}
  LedgerData[] ValidateCopies(){
   Inspect();var state=Read("state.json");var backup=Read("backup.json");var prepare=Read("prepare.json");var ack=Read("ack.json");
   // Even an interrupted staging file is evidence, never silently discard malformed bytes.
   var temp=Read("write.tmp");if(temp!=null){bool match=prepare!=null&&temp.contentHash==prepare.contentHash||state!=null&&temp.contentHash==state.contentHash||backup!=null&&temp.contentHash==backup.contentHash||ack!=null&&temp.contentHash==ack.contentHash;if(!match)throw new InvalidDataException("Unbound fund staging evidence");}
   if(state==null){if(backup!=null||ack!=null||known>0)throw new InvalidDataException("Missing acknowledged fund state");if(prepare!=null)Link(prepare,Empty());}
   else{
    if(state.revision<known)throw new InvalidDataException("Fund revision rollback");if(state.revision==1&&state.previousHash!=Zero)throw new InvalidDataException("Invalid genesis");
    if(backup!=null){if(backup.contentHash!=state.contentHash)Link(state,backup);}else if(state.revision>1)throw new InvalidDataException("Missing fund backup anchor");
    if(ack!=null&&ack.contentHash!=state.contentHash){Link(state,ack);if(prepare==null||prepare.contentHash!=state.contentHash)throw new InvalidDataException("Unacknowledged state lacks prepare");}
    if(ack==null&&prepare==null)throw new InvalidDataException("Fund acknowledgement missing");
    if(prepare!=null&&prepare.contentHash!=state.contentHash)Link(prepare,state);
   }
   // A copied receipt is not proof of the payload. Retain the locally built whole-candidate hash across uncertainty.
   if(pending!=null){string intent=pending.Fingerprint();if(prepare!=null&&(prepare.lastOperationId!=pending.OperationId||prepare.lastOperationFingerprint!=intent||prepare.contentHash!=pendingExpectedCandidateHash))throw new InvalidDataException("Durable prepare differs from retained exact intent");foreach(var copy in new[]{state,backup,ack,temp})if(copy!=null)foreach(var op in copy.operations)if(op.operationId==pending.OperationId&&(op.fingerprint!=intent||copy.contentHash!=pendingExpectedCandidateHash))throw new InvalidDataException("Committed operation differs from retained exact intent");}
   if(current!=null&&current.revision>0)foreach(var copy in new[]{state,backup,prepare,ack,temp})if(copy!=null&&copy.revision==known&&copy.contentHash!=current.contentHash)throw new InvalidDataException("Live acknowledged hash drift");
   return new[]{state,backup,prepare,ack};
  }
  void Cut(string stage){if(faults!=null)faults.At(stage);}
  void Atomic(string suffix,LedgerData e){string temp=PathFor("write.tmp"),target=PathFor(suffix);Check(dir);Check(temp);Check(target);byte[] bytes=LedgerJsonCodec.Encode(e);using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}Check(temp);Check(target);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);if(Read(suffix).contentHash!=e.contentHash)throw new IOException("Fund readback mismatch");}
  void Remove(string suffix){string p=PathFor(suffix);Check(p);if(File.Exists(p))File.Delete(p);}
  void Publish(LedgerData candidate,LedgerData baseline){
   if(baseline.revision>0){Cut("beforeBackup");Atomic("backup.json",baseline);Cut("afterBackup");}
   Cut("beforeReplace");Atomic("state.json",candidate);Cut("afterReplace");Cut("beforeAck");Atomic("ack.json",candidate);Cut("afterAck");Remove("prepare.json");Remove("write.tmp");
  }
  public LedgerSnapshot Reload(){Alive();try{var copies=ValidateCopies();var state=copies[0];var prepare=copies[2];if(prepare!=null){if(state==null||state.contentHash!=prepare.contentHash)Publish(prepare,state??Empty());else {Atomic("ack.json",state);Remove("prepare.json");Remove("write.tmp");}state=prepare;}current=state??Empty();known=Math.Max(known,current.revision);snapshot=current.revision==0?null:new LedgerSnapshot(current);if(pending==null)uncertain=false;return snapshot;}catch{uncertain=true;throw;}}
  public LedgerSnapshot Apply(LedgerRequest request){
   Alive();if(request==null)throw new ArgumentNullException("request");if(pending!=null&&!object.ReferenceEquals(pending,request))throw new InvalidOperationException("Retry the exact retained fund request instance");
   string fingerprint=request.Fingerprint();
   Reload();foreach(var op in current.operations)if(op.operationId==request.OperationId){if(op.fingerprint!=fingerprint)throw new InvalidOperationException("Fund operation ID collision");pending=null;pendingExpectedCandidateHash=null;uncertain=false;return snapshot;}
   if(current.operations.Length>=10000)throw new InvalidOperationException("Fund operation receipt capacity reached; no mutation performed");
   var candidate=LedgerRules.Build(current,request);
   if(pending!=null&&candidate.contentHash!=pendingExpectedCandidateHash)throw new InvalidDataException("Rebuilt fund candidate differs from retained exact intent");
   Cut("beforePrepare");pending=request;pendingExpectedCandidateHash=candidate.contentHash;try{Atomic("prepare.json",candidate);uncertain=true;Cut("afterPrepare");Publish(candidate,current);current=candidate;known=current.revision;snapshot=new LedgerSnapshot(current);pending=null;pendingExpectedCandidateHash=null;uncertain=false;return snapshot;}catch{uncertain=true;throw;}
  }
  public Dictionary<string,byte[]> ExportAcknowledgedFiles(){Alive();if(uncertain||pending!=null)throw new InvalidOperationException("Ledger uncertain");Inspect();if(File.Exists(PathFor("prepare.json"))||File.Exists(PathFor("write.tmp")))throw new InvalidDataException("Pending ledger evidence");var files=new Dictionary<string,byte[]>(StringComparer.Ordinal);foreach(string suffix in new[]{"state.json","backup.json","ack.json"})if(File.Exists(PathFor(suffix)))files.Add("fund."+suffix,Bytes(PathFor(suffix)));var snap=LedgerBackup.ValidateArchiveFiles(files);if(snap.Revision!=known||snap.ToData().contentHash!=current.contentHash)throw new InvalidDataException("Live ledger acknowledgement drift");return files;}
  public void Dispose(){if(disposed)return;disposed=true;if(lease!=null)lease.Dispose();}
 }
}