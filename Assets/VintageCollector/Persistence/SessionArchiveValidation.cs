using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Generic;
using VintageCollector.Domain;
namespace VintageCollector.Persistence
{
 // Read-only archive inspection. Uses the production schema/identity validator and equality;
 // never constructs a store, repairs a file, regenerates a pack, or changes schema1 bytes.
 public static class SessionArchiveValidation
 {
  static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
  static string Hash(string s) {using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Utf8.GetBytes(s))).Replace("-","").ToLowerInvariant();}
  static string Payload(byte[] bytes) {
   if(bytes.Length>32*1024*1024) throw new InvalidDataException("Oversized session member");
   string t=Utf8.GetString(bytes),head="VTCG-SAVE-1\n";
   if(!t.StartsWith(head,StringComparison.Ordinal) || t.Length<head.Length+65 || t[head.Length+64]!='\n') throw new InvalidDataException("Invalid or unsupported session envelope");
   string p=t.Substring(head.Length+65); if(t.Substring(head.Length,64)!=Hash(p)) throw new InvalidDataException("Session checksum mismatch"); return p;
  }
  static void Compatible(SessionData a,SessionData b) {
   if(a.revision>b.revision) {Compatible(b,a);return;}
   if(a.revision==b.revision) {if(!SaveProtocol.Equal(a,b)) throw new InvalidDataException("Session revision collision");return;}
   if(a.packs.Length>b.packs.Length) throw new InvalidDataException("Session archive shrank");
   for(int i=0;i<a.packs.Length;i++) {
    var x=a.packs[i];var y=b.packs[i];
    if(!StateCopy.SameIdentity(x,y) || x.wrapperOpened&&!y.wrapperOpened || x.revealedCount>y.revealedCount) throw new InvalidDataException("Session lineage conflict");
    for(int j=0;j<x.newToVirtual.Length;j++) if(x.newToVirtual[j]!=y.newToVirtual[j]) throw new InvalidDataException("Session award conflict");
   }
  }
  public static SessionSnapshot Validate(IDictionary<string,byte[]> files,IStateSerializer serializer,Catalog catalog) {return Validate(files,serializer,ApprovedSetRegistry.ForLegacyBase(catalog));}
  public static SessionSnapshot Validate(IDictionary<string,byte[]> files,IStateSerializer serializer,ApprovedSetRegistry registry) {
   if(files==null || serializer==null || registry==null)throw new ArgumentNullException();
   SessionData chosen=null;var copies=new List<KeyValuePair<long,byte[]>>();
   foreach(string n in new[]{"state.save","state.backup"}) {
    byte[] bytes; if(!files.TryGetValue(n,out bytes)) continue;
    SessionData d; try {string payload=Payload(bytes);VintageCollector.Collections.CollectionJsonShape.Session(payload);d=serializer.Deserialize(payload);} catch(Exception e) {throw new InvalidDataException("Session archive cannot be decoded",e);}
    StateValidation.Session(d,registry); if(d.revision==0) throw new InvalidDataException("Persisted blank session is not a legacy archive");
    if(chosen!=null) Compatible(chosen,d);
    copies.Add(new KeyValuePair<long,byte[]>(d.revision,bytes));
    if(chosen==null || d.revision>chosen.revision) chosen=StateCopy.Session(d);
   }
   byte[] anchor;
   if(files.TryGetValue("state.anchor",out anchor)) {
    if(anchor.Length>256 || chosen==null) throw new InvalidDataException("Session acknowledgement has no archive");
    string[] fields=Payload(anchor).Split('\n');long r;
    if(fields.Length!=2 || !long.TryParse(fields[0],NumberStyles.None,CultureInfo.InvariantCulture,out r) || r<1 || fields[0]!=r.ToString(CultureInfo.InvariantCulture) || fields[1].Length!=64 || chosen.revision<r) throw new InvalidDataException("Session acknowledgement mismatch");
    foreach(char c in fields[1]) if(!(c>='0'&&c<='9'||c>='a'&&c<='f')) throw new InvalidDataException("Invalid session anchor digest");
    // A newer candidate cannot erase contradictory evidence at the acknowledged revision.
    foreach(var copy in copies) if(copy.Key==r && Hash(Utf8.GetString(copy.Value))!=fields[1]) throw new InvalidDataException("Session acknowledgement conflicts with an available copy");
   }
   return new SessionSnapshot(chosen??new SessionData {schemaVersion=1,revision=0,packs=new PackRecordData[0]},null);
  }
 }
}
