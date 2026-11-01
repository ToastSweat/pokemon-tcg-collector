using System;using System.IO;using System.Collections.Generic;using VintageCollector.Domain;using VintageCollector.Collections;
namespace VintageCollector.Shopping {
 // Pure validation: never opens a store, repairs a file or acquires a writer lease.
 public static class ShoppingArchive {
  static readonly string Zero=new string('0',64);
  internal static readonly string[] Names={"shopping.state.json","shopping.backup.json","shopping.ack.json"};
  static bool Hex(string s){if(s==null||s.Length!=64)return false;foreach(char c in s)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;return true;}
  static MetadataEnvelope Decode(byte[] bytes,ApprovedSetRegistry registry){
   var e=ShoppingJsonCodec.Decode<MetadataEnvelope>(bytes);
   if(e.schemaVersion!=1||e.revision<1||e.operations==null||e.operations.Length!=e.revision||e.operations.Length>10000||!Hex(e.previousHash)||!Hex(e.contentHash)||!Hex(e.lastOperationFingerprint))throw new InvalidDataException("Invalid shopping envelope");
   ShoppingNoteRequest.Id(e.lastOperationId);var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(var op in e.operations){if(op==null)throw new InvalidDataException("Missing operation");ShoppingNoteRequest.Id(op.operationId);if(!Hex(op.fingerprint)||!ids.Add(op.operationId))throw new InvalidDataException("Invalid shopping operation");}
   var last=e.operations[e.operations.Length-1];if(last.operationId!=e.lastOperationId||last.fingerprint!=e.lastOperationFingerprint)throw new InvalidDataException("Shopping last operation mismatch");
   ComparisonValidator.FreezeMany(registry,e.comparisons);string hash=e.contentHash;e.contentHash="";string actual=ShoppingJsonCodec.Hash(ShoppingJsonCodec.Encode(e));e.contentHash=hash;if(actual!=hash||e.revision==1&&e.previousHash!=Zero)throw new InvalidDataException("Shopping content hash/genesis mismatch");return e;
  }
  public static ShoppingNotesSnapshot ValidateArchiveFiles(IDictionary<string,byte[]> files,ApprovedSetRegistry registry){
   if(files==null||registry==null)throw new ArgumentNullException();foreach(var f in files)if(Array.IndexOf(Names,f.Key)<0)throw new InvalidDataException("Foreign shopping archive member");
   if(files.Count==0)return new ShoppingNotesSnapshot(0,ComparisonValidator.FreezeMany(registry,new ComparisonData[0]));
   byte[] state,ack,backup;if(!files.TryGetValue(Names[0],out state)||!files.TryGetValue(Names[2],out ack))throw new InvalidDataException("Missing acknowledged shopping state");
   var s=Decode(state,registry);var a=Decode(ack,registry);if(s.contentHash!=a.contentHash)throw new InvalidDataException("Unacknowledged shopping state");
   if(files.TryGetValue(Names[1],out backup)){var b=Decode(backup,registry);if(b.contentHash!=s.contentHash){if(s.revision!=b.revision+1||s.previousHash!=b.contentHash||s.operations.Length!=b.operations.Length+1)throw new InvalidDataException("Shopping backup lineage mismatch");for(int i=0;i<b.operations.Length;i++)if(s.operations[i].operationId!=b.operations[i].operationId||s.operations[i].fingerprint!=b.operations[i].fingerprint)throw new InvalidDataException("Shopping receipt lineage mismatch");}}
   else if(s.revision>1)throw new InvalidDataException("Missing shopping backup");
   return new ShoppingNotesSnapshot(s.revision,ComparisonValidator.FreezeMany(registry,s.comparisons));
  }
  internal static string ContentHash(IDictionary<string,byte[]> files){byte[] b;return files.TryGetValue(Names[0],out b)?ShoppingJsonCodec.Decode<MetadataEnvelope>(b).contentHash:Zero;}
  public static Dictionary<string,byte[]> CaptureAcknowledgedFiles(string profileRoot,ApprovedSetRegistry registry){
   string root=CollectionFiles.Local(profileRoot);if(!Directory.Exists(root))throw new DirectoryNotFoundException();string dir=Path.Combine(root,"Shopping");CollectionFiles.Check(dir);var result=new Dictionary<string,byte[]>(StringComparer.Ordinal);
   if(File.Exists(dir))throw new InvalidDataException("Shopping is not a directory");if(!Directory.Exists(dir))return result;
   foreach(string p in Directory.GetFileSystemEntries(dir)){CollectionFiles.Check(p);string n=Path.GetFileName(p);if(Directory.Exists(p)||n!="shopping.lock"&&Array.IndexOf(Names,n)<0)throw new InvalidDataException("Pending or foreign shopping evidence");if(n!="shopping.lock")result.Add(n,CollectionEnvelope.Read(p,ShoppingJsonCodec.MaxBytes));}
   ValidateArchiveFiles(result,registry);return result;
  }
 }
}
