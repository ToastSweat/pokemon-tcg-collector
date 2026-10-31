using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
namespace VintageCollector.Collections
{
 internal static class CollectionEnvelope
 {
  internal static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
  internal static string Hash(byte[] b) {using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
  internal static byte[] Wrap(string kind,string payload) {return Utf8.GetBytes("VTCG-"+kind+"-1\n"+Hash(Utf8.GetBytes(payload))+"\n"+payload);}
  internal static string Unwrap(string kind,byte[] b) {
   string t=Utf8.GetString(b),header="VTCG-"+kind+"-1\n";
   if(!t.StartsWith(header,StringComparison.Ordinal) || t.Length<header.Length+65 || t[header.Length+64]!='\n') throw new InvalidDataException("Unsupported or corrupt collection metadata");
   string p=t.Substring(header.Length+65); if(t.Substring(header.Length,64)!=Hash(Utf8.GetBytes(p))) throw new InvalidDataException("Collection metadata checksum mismatch"); return p;
  }
  internal static byte[] Read(string p,int cap) {
   CollectionFiles.Check(p); using(var s=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.Read)) {
    if(s.Length>cap) throw new InvalidDataException("Collection file exceeds size limit");
    var b=new byte[(int)s.Length];int n=0;while(n<b.Length) {int k=s.Read(b,n,b.Length-n);if(k==0) throw new EndOfStreamException();n+=k;} if(s.ReadByte()!=-1) throw new InvalidDataException("Collection changed while reading");return b;
   }
  }
  internal static void NewFile(string p,byte[] b) {
   CollectionFiles.Check(p);using(var s=new FileStream(p,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {s.Write(b,0,b.Length);s.Flush(true);}
  }
  internal static long Number(string s) {long n;if(!long.TryParse(s,NumberStyles.None,CultureInfo.InvariantCulture,out n)||n<0||n.ToString(CultureInfo.InvariantCulture)!=s) throw new InvalidDataException("Noncanonical metadata number");return n;}
 }
}
