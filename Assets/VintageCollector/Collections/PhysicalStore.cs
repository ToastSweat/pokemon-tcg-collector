using System;
using System.IO;
using System.Collections.Generic;
using VintageCollector.Domain;
using VintageCollector.Persistence;
namespace VintageCollector.Collections
{
 public sealed class PhysicalStore : IDisposable
 {
  readonly PhysicalProtocol protocol; readonly Catalog catalog; PhysicalStateData state; FileStream lease; bool disposed,uncertain; string notice;
  public PhysicalStore(string directory,IPhysicalSerializer serializer,Catalog catalog,ISaveFaultInjector faults=null) {
   if(serializer==null || catalog==null || directory==null) throw new ArgumentNullException();
   directory=CollectionFiles.Local(directory); this.catalog=catalog; Directory.CreateDirectory(directory); protocol=new PhysicalProtocol(directory,serializer,catalog,faults);
   try {CollectionFiles.Check(Path.Combine(directory,"physical.lock"));lease=new FileStream(Path.Combine(directory,"physical.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None); lease.Lock(0,1); Reload();} catch {if(lease!=null) lease.Dispose(); throw;}
  }
  void Ready() {if(disposed) throw new ObjectDisposedException("PhysicalStore"); if(uncertain) Reload();}
  public PhysicalSnapshot Snapshot { get {Ready(); return PhysicalValidation.Snapshot(state,notice);} }
  public PhysicalSnapshot Reload() {
   if(disposed) throw new ObjectDisposedException("PhysicalStore"); uncertain=true;
   string n; var d=protocol.Load(state,out n); state=d; uncertain=false; if(n!=null) notice=n; return Snapshot;
  }
  public PhysicalSnapshot SetQuantity(string transactionId,string createdUtc,string cardId,string condition,int quantity) {
   Ready(); var edit=new PhysicalEditData {transactionId=transactionId,createdUtc=createdUtc,cardId=cardId,condition=condition,quantity=quantity}; PhysicalValidation.Edit(edit,catalog);
   foreach(var e in state.edits) if(e.transactionId==transactionId) {if(!PhysicalValidation.Same(e,edit)) throw new ArgumentException("Physical transaction collision"); return Snapshot;}
   var edits=new List<PhysicalEditData>(state.edits); edits.Add(edit); var d=new PhysicalStateData {schemaVersion=1,revision=checked(state.revision+1),edits=edits.ToArray()};
   uncertain=true; protocol.Save(d); state=PhysicalValidation.Copy(d); uncertain=false; return Snapshot;
  }
  public void Dispose() {if(disposed) return; disposed=true; lease.Dispose();}
 }
}
