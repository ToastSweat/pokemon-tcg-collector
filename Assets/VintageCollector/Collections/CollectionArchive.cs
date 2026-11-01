using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Globalization;
using VintageCollector.Domain;
using VintageCollector.Persistence;
namespace VintageCollector.Collections
{
 internal sealed class CollectionArchive
 {
  internal const int MemberCap=32*1024*1024, TotalCap=128*1024*1024, ArchiveCap=TotalCap+1024*1024;
  internal static readonly string[] Authoritative={"state.save","state.backup","state.anchor","physical.save","physical.backup","physical.anchor"};
  // Each empty marker replaces a state group; maximum is all twelve state files + version + manifest.
  internal static int MaxMembers {get{return Authoritative.Length+Sidecars.Length+2;}}
  internal static readonly string[] Sidecars={"Shopping/shopping.state.json","Shopping/shopping.backup.json","Shopping/shopping.ack.json","Fund/fund.state.json","Fund/fund.backup.json","Fund/fund.ack.json"};
  internal static bool IsState(string n){return Array.IndexOf(Authoritative,n)>=0||Array.IndexOf(Sidecars,n)>=0;}
  internal static bool Allowed(string n){return IsState(n)||n=="session.empty"||n=="physical.empty"||n=="shopping.empty"||n=="fund.empty"||n=="profile.version"||n=="collection.manifest";}
  internal readonly bool FullProfile;internal readonly VintageCollector.Shopping.ShoppingNotesSnapshot Shopping;internal readonly VintageCollector.Funds.LedgerSnapshot Fund;
  internal readonly Dictionary<string,byte[]> Files; internal readonly SessionSnapshot Session; internal readonly PhysicalSnapshot Physical; internal readonly PhysicalStateData PhysicalData;
  internal CollectionArchive(Dictionary<string,byte[]> files,IStateSerializer ss,IPhysicalSerializer ps,Catalog catalog) : this(files,ss,ps,ApprovedSetRegistry.ForLegacyBase(catalog)) { }
  internal CollectionArchive(Dictionary<string,byte[]> files,IStateSerializer ss,IPhysicalSerializer ps,ApprovedSetRegistry registry) {
   Files=files;foreach(var f in files)if(!Allowed(f.Key)||f.Key=="collection.manifest"||f.Value==null)throw new InvalidDataException("Unknown archive member");
   FullProfile=files.ContainsKey("profile.version");
   if(FullProfile){if(CollectionEnvelope.Utf8.GetString(files["profile.version"])!="VTCG-PROFILE-2\n")throw new InvalidDataException("Unsupported profile archive version");
    var shopping=Group(files,"Shopping/");var fund=Group(files,"Fund/");
    CheckEmpty(files,"shopping.empty","VTCG-EMPTY-SHOPPING-1\n",shopping.Count==0);CheckEmpty(files,"fund.empty","VTCG-EMPTY-FUND-1\n",fund.Count==0);
    Shopping=VintageCollector.Shopping.ShoppingArchive.ValidateArchiveFiles(shopping,registry);if(fund.Count>0)Fund=VintageCollector.Funds.LedgerBackup.ValidateArchiveFiles(fund);
   }else {foreach(string n in Sidecars)if(files.ContainsKey(n))throw new InvalidDataException("Sidecar without profile version");if(files.ContainsKey("shopping.empty")||files.ContainsKey("fund.empty"))throw new InvalidDataException("Versionless sidecar marker");}
   Session=SessionArchiveValidation.Validate(files,ss,registry);
   var protocol=new PhysicalProtocol("",ps,registry,null); PhysicalStateData chosen=null;var copies=new List<KeyValuePair<long,byte[]>>();
   foreach(string n in new[]{"physical.save","physical.backup"}) {byte[] bytes;if(!files.TryGetValue(n,out bytes)) continue;
    var d=protocol.Decode(CollectionEnvelope.Utf8.GetString(bytes));if(d.revision==0) throw new InvalidDataException("Persisted blank physical state");
    if(chosen!=null) PhysicalProtocol.Compatible(chosen,d);copies.Add(new KeyValuePair<long,byte[]>(d.revision,bytes)); if(chosen==null || d.revision>chosen.revision) chosen=d;
   }
   byte[] anchor; if(files.TryGetValue("physical.anchor",out anchor)) {
    if(anchor.Length>256 || chosen==null) throw new InvalidDataException("Physical acknowledgement has no state");
    string[] a=CollectionEnvelope.Unwrap("PHYSICAL",anchor).Split('\n');
    if(a.Length!=2 || CollectionEnvelope.Number(a[0])<1 || chosen.revision<CollectionEnvelope.Number(a[0]) || a[1].Length!=64) throw new InvalidDataException("Physical acknowledgement mismatch");
    foreach(char c in a[1]) if(!(c>='0'&&c<='9'||c>='a'&&c<='f')) throw new InvalidDataException("Invalid physical anchor digest");
    foreach(var copy in copies) if(copy.Key==CollectionEnvelope.Number(a[0]) && CollectionEnvelope.Hash(copy.Value)!=a[1]) throw new InvalidDataException("Physical acknowledgement conflicts with an available copy");
   }
   PhysicalData=PhysicalValidation.Copy(chosen??new PhysicalStateData {schemaVersion=1,revision=0,edits=new PhysicalEditData[0]});Physical=PhysicalValidation.Snapshot(PhysicalData,null);
   CheckEmpty(files,"session.empty","VTCG-EMPTY-SESSION-1\n",Session.Revision==0);
   CheckEmpty(files,"physical.empty","VTCG-EMPTY-PHYSICAL-1\n",Physical.Revision==0);
  }
  internal static Dictionary<string,byte[]> Group(IDictionary<string,byte[]> files,string prefix){var result=new Dictionary<string,byte[]>(StringComparer.Ordinal);foreach(var f in files)if(f.Key.StartsWith(prefix,StringComparison.Ordinal))result.Add(f.Key.Substring(prefix.Length),f.Value);return result;}
  internal static void AddFullProfile(Dictionary<string,byte[]> files,Dictionary<string,byte[]> sidecars){
   if(sidecars==null)throw new InvalidDataException("Sidecar capture unavailable");
   foreach(var f in sidecars){if(Array.IndexOf(Sidecars,f.Key)<0||f.Value==null)throw new InvalidDataException("Invalid sidecar capture membership");files.Add(f.Key,(byte[])f.Value.Clone());}
   files.Add("profile.version",CollectionEnvelope.Utf8.GetBytes("VTCG-PROFILE-2\n"));
   if(Group(files,"Shopping/").Count==0)files.Add("shopping.empty",CollectionEnvelope.Utf8.GetBytes("VTCG-EMPTY-SHOPPING-1\n"));
   if(Group(files,"Fund/").Count==0)files.Add("fund.empty",CollectionEnvelope.Utf8.GetBytes("VTCG-EMPTY-FUND-1\n"));
  }
  static void CheckEmpty(Dictionary<string,byte[]> files,string name,string content,bool empty) {
   byte[] b; bool present=files.TryGetValue(name,out b);if(present!=empty || present&&CollectionEnvelope.Utf8.GetString(b)!=content) throw new InvalidDataException("Missing or conflicting explicit empty marker");
  }
  internal static Dictionary<string,byte[]> Capture(string directory) {
   CollectionFiles.Check(directory);var files=new Dictionary<string,byte[]>(StringComparer.Ordinal);
   foreach(string prefix in new[]{"state.","physical."}) foreach(string work in new[]{"pending","writing","installing","backup-writing","anchor-writing"}) if(CollectionFiles.Exists(Path.Combine(directory,prefix+work))) throw new InvalidDataException("Unacknowledged store work must be recovered before export");
   foreach(string name in Authoritative) {string p=Path.Combine(directory,name);if(CollectionFiles.Exists(p)) files.Add(name,CollectionEnvelope.Read(p,MemberCap));}
   if(!files.ContainsKey("state.save")&&!files.ContainsKey("state.backup")&&!files.ContainsKey("state.anchor")) files.Add("session.empty",CollectionEnvelope.Utf8.GetBytes("VTCG-EMPTY-SESSION-1\n"));
   if(!files.ContainsKey("physical.save")&&!files.ContainsKey("physical.backup")&&!files.ContainsKey("physical.anchor")) files.Add("physical.empty",CollectionEnvelope.Utf8.GetBytes("VTCG-EMPTY-PHYSICAL-1\n"));return files;
  }
  internal static byte[] Manifest(Dictionary<string,byte[]> files) {
   var names=new List<string>(files.Keys);names.Sort(StringComparer.Ordinal);var lines=new List<string>();
   foreach(string n in names) lines.Add(n+" "+files[n].Length.ToString(CultureInfo.InvariantCulture)+" "+CollectionEnvelope.Hash(files[n]));
   return CollectionEnvelope.Wrap("COLLECTION",string.Join("\n",lines.ToArray()));
  }
  internal static byte[] Zip(Dictionary<string,byte[]> files) {
   using(var m=new MemoryStream()) {using(var z=new ZipArchive(m,ZipArchiveMode.Create,true)) {
    foreach(var pair in files) {var e=z.CreateEntry(pair.Key,CompressionLevel.Optimal);using(var s=e.Open()) s.Write(pair.Value,0,pair.Value.Length);}
    byte[] manifest=Manifest(files);using(var s=z.CreateEntry("collection.manifest").Open()) s.Write(manifest,0,manifest.Length);
   } if(m.Length>ArchiveCap) throw new InvalidDataException("Archive exceeds limit");return m.ToArray();}
  }
  internal static CollectionArchive Open(byte[] archive,IStateSerializer ss,IPhysicalSerializer ps,Catalog catalog) {return Open(archive,ss,ps,ApprovedSetRegistry.ForLegacyBase(catalog));}
  internal static CollectionArchive Open(byte[] archive,IStateSerializer ss,IPhysicalSerializer ps,ApprovedSetRegistry registry) {
   if(archive.Length>ArchiveCap) throw new InvalidDataException("Archive exceeds limit");
   var crc=CollectionZipStructure.Check(archive);var files=new Dictionary<string,byte[]>(StringComparer.Ordinal);var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long total=0;
   using(var m=new MemoryStream(archive,false)) using(var zip=new ZipArchive(m,ZipArchiveMode.Read)) {
    if(zip.Entries.Count<3 || zip.Entries.Count>MaxMembers) throw new InvalidDataException("Invalid archive member count");
    foreach(var e in zip.Entries) {
     string n=e.FullName; if(!seen.Add(n) || !Allowed(n) || ((e.ExternalAttributes>>16)&0xf000)==0xa000 || (e.ExternalAttributes&0x410)!=0) throw new InvalidDataException("Invalid archive membership");
     int cap=n=="collection.manifest"?4096:MemberCap; if(e.Length>cap || e.Length<0 || (total+=e.Length)>TotalCap) throw new InvalidDataException("Oversized archive member");
     using(var s=e.Open()) using(var b=new MemoryStream()) {var buffer=new byte[8192];int k;while((k=s.Read(buffer,0,buffer.Length))>0) {if(b.Length+k>cap || b.Length+k>e.Length) throw new InvalidDataException("Member size mismatch");b.Write(buffer,0,k);} if(b.Length!=e.Length) throw new InvalidDataException("Truncated archive member");byte[] decoded=b.ToArray();if(CollectionZipStructure.Crc(decoded)!=crc[n]) throw new InvalidDataException("ZIP CRC mismatch");files.Add(n,decoded);}
    }
   }
   byte[] mf;if(!files.TryGetValue("collection.manifest",out mf)) throw new InvalidDataException("Missing collection manifest");files.Remove("collection.manifest");
   if(CollectionEnvelope.Utf8.GetString(mf)!=CollectionEnvelope.Utf8.GetString(Manifest(files))) throw new InvalidDataException("Archive membership or checksums mismatch");
   return new CollectionArchive(files,ss,ps,registry);
  }
 }
}
