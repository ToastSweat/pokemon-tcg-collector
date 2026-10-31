using System;
using System.IO;
using System.Collections.Generic;
using VintageCollector.Domain;
using VintageCollector.Persistence;
namespace VintageCollector.Collections {
 public sealed class CollectionProfiles : IDisposable {
 readonly string root; readonly ISaveFaultInjector faults; FileStream lease; bool disposed; string activeId="",activeDirectory,bootContext; long minimumSessionRevision,minimumPhysicalRevision;
 sealed class Review {internal byte[] Bytes;internal string Path;internal CollectionArchive Archive;internal IStateSerializer SessionSerializer;internal IPhysicalSerializer PhysicalSerializer;internal Catalog Catalog;internal string Context;internal bool StageOwned,Published,Started,Completed;}
 ImportPreview reviewPreview; Review review;
 string BackupDirectory {get {return Path.Combine(root,"Backups");}}
 string BackupPath(string p) {Ready();string full=CollectionFiles.Local(p);if(Path.GetDirectoryName(full)!=BackupDirectory || Path.GetExtension(full)!=".zip") throw new ArgumentException("Select a backup from this container");CollectionFiles.Check(full);return full;}

 void At(string stage) {if(faults!=null) faults.At(stage);}
 void Ready() {if(disposed) throw new ObjectDisposedException("CollectionProfiles"); CollectionFiles.Check(root);}
 public CollectionProfiles(string baseDirectory) : this(baseDirectory,null) {}
 internal CollectionProfiles(string baseDirectory,ISaveFaultInjector faults) {
 this.faults=faults;root=CollectionFiles.Local(baseDirectory); Directory.CreateDirectory(root);
 try {string p=System.IO.Path.Combine(root,"collection.lock"); CollectionFiles.Check(p); lease=new System.IO.FileStream(p,System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None); lease.Lock(0,1);ReadSelection();} catch {if(lease!=null) lease.Dispose();throw;}
 }

 public string ActiveProfileId {get {Ready();return activeId;}}
 public string ActiveDirectory {get {Ready();return activeDirectory;}}
 public string Notice {get {return null;}}
 public string[] BackupFiles {get {Ready();if(!CollectionFiles.Exists(BackupDirectory)) return new string[0];var a=Directory.GetFiles(BackupDirectory,"*.zip");foreach(string p in a) CollectionFiles.Check(p);Array.Sort(a,StringComparer.Ordinal);return a;}}
 public string CreateBackup(string activeDirectory,IStateSerializer sessionSerializer,IPhysicalSerializer physicalSerializer,Catalog catalog) {
 try {return CreateBackupCore(activeDirectory,sessionSerializer,physicalSerializer,catalog);}
 catch(IOException) {throw new IOException("CreateBackup I/O failed; existing collection, source, and recovery evidence retained. Retry the same operation.");}
 catch(UnauthorizedAccessException) {throw new IOException("CreateBackup access denied; collection and source retained.");}
 catch(InvalidDataException) {throw new InvalidDataException("CreateBackup validation failed: unsupported, changed, missing or corrupt data. Original bytes retained.");}
 catch(ArgumentException) {throw new ArgumentException("CreateBackup rejected invalid input or an unowned preview.");}
 }
 string CreateBackupCore(string activeDirectory,IStateSerializer sessionSerializer,IPhysicalSerializer physicalSerializer,Catalog catalog) {
 Ready();if(CollectionFiles.Local(activeDirectory)!=ActiveDirectory) throw new ArgumentException("Only the active collection may be exported");
 var captured=CollectionArchive.Capture(ActiveDirectory);var inspected=new CollectionArchive(captured,sessionSerializer,physicalSerializer,catalog);
 if(inspected.Session.Revision<minimumSessionRevision || inspected.Physical.Revision<minimumPhysicalRevision) throw new InvalidDataException("Acknowledged collection content is missing; cannot export a blank fallback");
 minimumSessionRevision=inspected.Session.Revision;minimumPhysicalRevision=inspected.Physical.Revision;
 byte[] bytes=CollectionArchive.Zip(captured);CollectionArchive.Open(bytes,sessionSerializer,physicalSerializer,catalog);
 CollectionFiles.Check(BackupDirectory);Directory.CreateDirectory(BackupDirectory);string destination=Path.Combine(BackupDirectory,"collection-"+Guid.NewGuid().ToString("N")+".zip");
 string temp=destination+".writing";CollectionEnvelope.NewFile(temp,bytes);CollectionFiles.Check(destination);File.Move(temp,destination);
 if(CollectionEnvelope.Hash(CollectionEnvelope.Read(destination,CollectionArchive.ArchiveCap))!=CollectionEnvelope.Hash(bytes)) throw new IOException("Backup acknowledgement uncertain; retained for recovery");return destination;
 }
 public ImportPreview PreviewImport(string backupPath,IStateSerializer sessionSerializer,IPhysicalSerializer physicalSerializer,Catalog catalog) {
 try {return PreviewImportCore(backupPath,sessionSerializer,physicalSerializer,catalog);}
 catch(IOException) {throw new IOException("PreviewImport I/O failed; existing collection, source, and recovery evidence retained. Retry the same operation.");}
 catch(UnauthorizedAccessException) {throw new IOException("PreviewImport access denied; collection and source retained.");}
 catch(InvalidDataException) {throw new InvalidDataException("PreviewImport validation failed: unsupported, changed, missing or corrupt data. Original bytes retained.");}
 catch(ArgumentException) {throw new ArgumentException("PreviewImport rejected invalid input or an unowned preview.");}
 }
 ImportPreview PreviewImportCore(string backupPath,IStateSerializer sessionSerializer,IPhysicalSerializer physicalSerializer,Catalog catalog) {
 Ready();if(review!=null && review.Started && !review.Completed) throw new InvalidOperationException("Import confirmation unresolved; retry the same preview before reviewing another");if(PointerToken()!=bootContext) throw new InvalidOperationException("Profile selection changed; reopen before reviewing another import");string p=BackupPath(backupPath);byte[] bytes=CollectionEnvelope.Read(p,CollectionArchive.ArchiveCap);var a=CollectionArchive.Open(bytes,sessionSerializer,physicalSerializer,catalog);
 int physicalCount=0;var pu=new HashSet<string>(StringComparer.Ordinal);foreach(var h in a.Physical.Holdings) {physicalCount=checked(physicalCount+h.Quantity);pu.Add(h.CardId);}
 var v=new ImportPreview(Guid.NewGuid().ToString("N"),CollectionEnvelope.Hash(bytes),Path.GetFileName(p),a.Session.TotalVirtualCards,physicalCount,a.Session.VirtualHoldings.Count,pu.Count,"Reviewed schema1 archive: "+a.Session.TotalVirtualCards+" virtual cards, "+physicalCount+" physical cards. Confirm creates a NEW profile; restart required.");
 review=new Review {Bytes=bytes,Path=p,Archive=a,SessionSerializer=sessionSerializer,PhysicalSerializer=physicalSerializer,Catalog=catalog,Context=PointerToken()};reviewPreview=v;return v;
 }
 // Memory-only cancellation/release; never removes staged, published or recovery evidence.
 public void DiscardPreview(ImportPreview preview) {
 Ready();if(preview==null || !object.ReferenceEquals(preview,reviewPreview)) throw new ArgumentException("Only this service's exact reviewed preview may be discarded");
 if(review.Started && !review.Completed) throw new InvalidOperationException("Import confirmation unresolved; retain and retry the same preview");
 review=null;reviewPreview=null;
 }
 public string ConfirmImport(ImportPreview preview) {
 try {return ConfirmImportCore(preview);}
 catch(IOException) {throw new IOException("ConfirmImport I/O failed; existing collection, source, and recovery evidence retained. Retry the same operation.");}
 catch(UnauthorizedAccessException) {throw new IOException("ConfirmImport access denied; collection and source retained.");}
 catch(InvalidDataException) {throw new InvalidDataException("ConfirmImport validation failed: unsupported, changed, missing or corrupt data. Original bytes retained.");}
 catch(ArgumentException) {throw new ArgumentException("ConfirmImport rejected invalid input or an unowned preview.");}
 }
 string ConfirmImportCore(ImportPreview preview) {
 Ready();if(preview==null || !object.ReferenceEquals(preview,reviewPreview)) throw new ArgumentException("Only this service's exact reviewed preview may be confirmed");Review r=review;
 // Keep the source open without write/delete sharing through pointer publication.
 using(var sourceLease=new FileStream(BackupPath(r.Path),FileMode.Open,FileAccess.Read,FileShare.Read)) {
 // The current source must still be the exact archive reviewed; extraction uses owned bytes only.
 if(CollectionEnvelope.Hash(CollectionEnvelope.Read(BackupPath(r.Path),CollectionArchive.ArchiveCap))!=preview.SourceHash || CollectionEnvelope.Hash(r.Bytes)!=preview.SourceHash) throw new InvalidDataException("Backup changed since preview; review again");
 string profile=Path.Combine(root,"Profiles",preview.ReceiptId),stage=Path.Combine(root,".import-"+preview.ReceiptId);
 string receiptPayload=preview.ReceiptId+"\n"+preview.SourceHash+"\n"+r.Archive.Session.Revision+"\n"+r.Archive.Physical.Revision+"\n"+r.Context;
 byte[] receipt=CollectionEnvelope.Wrap("IMPORT-RECEIPT",receiptPayload);
 byte[] owner=CollectionEnvelope.Wrap("IMPORT-OWNER",preview.ReceiptId+"\n"+preview.SourceHash+"\n"+r.Context);
 byte[] pointer=CollectionEnvelope.Wrap("ACTIVE-PROFILE",preview.ReceiptId+"\n"+preview.SourceHash+"\n"+CollectionEnvelope.Hash(receipt));
 string expected=CollectionEnvelope.Hash(pointer),token=PointerToken();
 if(token!=r.Context && token!=expected) throw new InvalidOperationException("Active profile context changed; import blocked");
 if(token==expected) {ValidatePublished(profile,r,receipt,owner);r.Completed=true;return preview.ReceiptId;}
 if(!r.Published) {
  if(r.StageOwned && !CollectionFiles.Exists(stage) && CollectionFiles.Exists(profile)) {ValidatePublished(profile,r,receipt,owner);r.Published=true;}
  if(!r.StageOwned) {
   if(CollectionFiles.Exists(profile) || CollectionFiles.Exists(stage)) throw new IOException("Import identity collision; existing directory preserved");
   r.Started=true;CollectionFiles.ExclusiveDirectory(stage);r.StageOwned=true;
   CollectionEnvelope.NewFile(Path.Combine(stage,"import.owner"),owner);
  }
  if(!r.Published) RequireExact(Path.Combine(stage,"import.owner"),owner);
  At("after-stage-create");
  if(!r.Published) {
  foreach(var file in r.Archive.Files) if(Array.IndexOf(CollectionArchive.Authoritative,file.Key)>=0) {
   string dest=Path.Combine(stage,file.Key);if(!CollectionFiles.Exists(dest)) CollectionEnvelope.NewFile(dest,file.Value);
  }
  At("after-extract");ValidateStores(stage,r);At("after-store-validation");PutExact(Path.Combine(stage,"import.receipt"),receipt);At("after-receipt");
  string parent=Path.Combine(root,"Profiles");CollectionFiles.Check(parent);Directory.CreateDirectory(parent);CollectionFiles.Check(profile);CollectionFiles.Check(stage);
  Directory.Move(stage,profile);r.Published=true;At("after-profile-publish");
  }
 }
 ValidatePublished(profile,r,receipt,owner);
 string pending=Path.Combine(root,"active-profile.pending");PutExact(pending,pointer);At("after-pointer-prepare");
 CollectionFiles.Check(Path.Combine(root,"active-profile"));CollectionFiles.Check(pending);
 if(CollectionFiles.Exists(Path.Combine(root,"active-profile"))) {CollectionFiles.Check(Path.Combine(root,"active-profile.backup"));File.Replace(pending,Path.Combine(root,"active-profile"),Path.Combine(root,"active-profile.backup"));}
 else File.Move(pending,Path.Combine(root,"active-profile"));
 At("after-pointer-replace");
 if(PointerToken()!=expected) throw new IOException("Activation acknowledgement uncertain; retry the same preview");
 At("before-import-ack");r.Completed=true;return preview.ReceiptId;
 }
 }
 static void PutExact(string p,byte[] bytes) {
  if(CollectionFiles.Exists(p)) {if(CollectionEnvelope.Hash(CollectionEnvelope.Read(p,4096))!=CollectionEnvelope.Hash(bytes)) throw new InvalidDataException("Import receipt or pointer conflict; retained");}
  else CollectionEnvelope.NewFile(p,bytes);
 }
 static void RequireExact(string p,byte[] expected) {
  if(!CollectionFiles.Exists(p) || CollectionEnvelope.Hash(CollectionEnvelope.Read(p,4096))!=CollectionEnvelope.Hash(expected)) throw new InvalidDataException("Import ownership missing or changed; all evidence retained");
 }
 static void ValidatePublished(string profile,Review r,byte[] receipt,byte[] owner) {
  RequireExact(Path.Combine(profile,"import.owner"),owner);
  if(!CollectionFiles.Exists(Path.Combine(profile,"import.receipt")) || CollectionEnvelope.Hash(CollectionEnvelope.Read(Path.Combine(profile,"import.receipt"),4096))!=CollectionEnvelope.Hash(receipt)) throw new InvalidDataException("Imported profile receipt missing or changed");
  ValidateStores(profile,r);
 }
 static void ValidateStores(string directory,Review r) {
  CollectionFiles.Check(directory);foreach(string n in CollectionArchive.Authoritative) CollectionFiles.Check(Path.Combine(directory,n));
  foreach(string prefix in new[]{"state.","physical."}) foreach(string suffix in new[]{"lock","pending","writing","installing","backup-writing","anchor-writing"}) CollectionFiles.Check(Path.Combine(directory,prefix+suffix));
  using(var s=new SessionStore(directory,r.SessionSerializer,r.Catalog)) {if(!SameSession(s.Snapshot,r.Archive.Session)) throw new InvalidDataException("Staged session differs from the reviewed archive");}
  using(var p=new PhysicalStore(directory,r.PhysicalSerializer,r.Catalog)) {if(!SamePhysical(p.Snapshot,r.Archive.Physical)) throw new InvalidDataException("Staged physical state differs from the reviewed archive");}
  var inspected=new CollectionArchive(CollectionArchive.Capture(directory),r.SessionSerializer,r.PhysicalSerializer,r.Catalog);
  if(!PhysicalProtocol.Equal(inspected.PhysicalData,r.Archive.PhysicalData)) throw new InvalidDataException("Staged physical transaction history differs from the reviewed archive");
 }
 static bool SamePhysical(PhysicalSnapshot a,PhysicalSnapshot b) {
  if(a.Revision!=b.Revision || a.Holdings.Count!=b.Holdings.Count) return false;
  for(int i=0;i<a.Holdings.Count;i++) {var x=a.Holdings[i];var y=b.Holdings[i];if(x.CardId!=y.CardId || x.Condition!=y.Condition || x.Quantity!=y.Quantity) return false;}return true;
 }
 static bool SameSession(SessionSnapshot a,SessionSnapshot b) {
  if(a.Revision!=b.Revision || a.Packs.Count!=b.Packs.Count) return false;
  for(int i=0;i<a.Packs.Count;i++) {
   var x=a.Packs[i];var y=b.Packs[i];if(x.TransactionId!=y.TransactionId||x.CreatedUtc!=y.CreatedUtc||x.Seed!=y.Seed||x.ModelId!=y.ModelId||x.ModelVersion!=y.ModelVersion||x.CatalogVersion!=y.CatalogVersion||x.PrintingId!=y.PrintingId||x.RngAlgorithm!=y.RngAlgorithm||x.Status!=y.Status||x.RevealedCount!=y.RevealedCount||x.WrapperOpened!=y.WrapperOpened||x.OrderedCardIds.Count!=y.OrderedCardIds.Count) return false;
   for(int j=0;j<x.OrderedCardIds.Count;j++) if(x.OrderedCardIds[j]!=y.OrderedCardIds[j]||x.NewToVirtual[j]!=y.NewToVirtual[j]) return false;
  }return true;
 }
 string PointerToken() {string p=Path.Combine(root,"active-profile");return CollectionFiles.Exists(p)?CollectionEnvelope.Hash(CollectionEnvelope.Read(p,4096)):"default";}
 static void GuidId(string id) {Guid g;if(id==null||id.Length!=32||!Guid.TryParseExact(id,"N",out g)||g.ToString("N")!=id) throw new InvalidDataException("Invalid collection profile identity");}
 static void CheckAcknowledgedMinimum(string directory,string prefix,string kind,long minimum) {
  if(minimum==0) return;string p=Path.Combine(directory,prefix+".anchor");
  if(!CollectionFiles.Exists(p)) throw new InvalidDataException("Imported collection acknowledgement missing; refusing rollback");
  string[] a=CollectionEnvelope.Unwrap(kind,CollectionEnvelope.Read(p,256)).Split('\n');
  if(a.Length!=2 || CollectionEnvelope.Number(a[0])<minimum || a[1].Length!=64) throw new InvalidDataException("Imported collection acknowledgement predates receipt");
  foreach(char c in a[1]) if(!(c>='0'&&c<='9'||c>='a'&&c<='f')) throw new InvalidDataException("Invalid imported acknowledgement digest");
 }
 void ReadSelection() {
  // An uncommitted pointer is evidence of an interrupted explicit action, not permission to activate it.
  if(CollectionFiles.Exists(Path.Combine(root,"active-profile.pending"))) throw new InvalidDataException("Activation was interrupted; pending metadata retained for explicit recovery");
  bootContext=PointerToken();activeDirectory=root;if(bootContext=="default") {
   if(CollectionFiles.Exists(Path.Combine(root,"active-profile.backup")) || CollectionFiles.Exists(Path.Combine(root,"Profiles"))) throw new InvalidDataException("Profile selection evidence exists but active pointer is missing; refusing default fallback");return;
  }
  string[] p=CollectionEnvelope.Unwrap("ACTIVE-PROFILE",CollectionEnvelope.Read(Path.Combine(root,"active-profile"),4096)).Split('\n');
  if(p.Length!=3) throw new InvalidDataException("Invalid active profile pointer");GuidId(p[0]);
  string dir=Path.Combine(root,"Profiles",p[0]);CollectionFiles.Check(dir);
  byte[] receipt=CollectionEnvelope.Read(Path.Combine(dir,"import.receipt"),4096);if(CollectionEnvelope.Hash(receipt)!=p[2]) throw new InvalidDataException("Active profile receipt does not match pointer");
  string[] r=CollectionEnvelope.Unwrap("IMPORT-RECEIPT",receipt).Split('\n');
  if(r.Length!=5 || r[0]!=p[0] || r[1]!=p[1] || r[1].Length!=64) throw new InvalidDataException("Invalid profile import receipt");
  long virtualRevision=CollectionEnvelope.Number(r[2]),physicalRevision=CollectionEnvelope.Number(r[3]);
  if(virtualRevision>0 && !CollectionFiles.Exists(Path.Combine(dir,"state.save"))&&!CollectionFiles.Exists(Path.Combine(dir,"state.backup")) || physicalRevision>0 && !CollectionFiles.Exists(Path.Combine(dir,"physical.save"))&&!CollectionFiles.Exists(Path.Combine(dir,"physical.backup"))) throw new InvalidDataException("Acknowledged imported profile content is missing; refusing blank fallback");
  CheckAcknowledgedMinimum(dir,"state","SAVE",virtualRevision);CheckAcknowledgedMinimum(dir,"physical","PHYSICAL",physicalRevision);
  minimumSessionRevision=virtualRevision;minimumPhysicalRevision=physicalRevision;activeId=p[0];activeDirectory=dir;
 }
 public void Dispose() {if(disposed) return;disposed=true;review=null;reviewPreview=null;if(lease!=null) lease.Dispose();}
 }
}
