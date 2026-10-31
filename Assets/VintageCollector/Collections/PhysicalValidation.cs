using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using VintageCollector.Domain;
namespace VintageCollector.Collections
{
 internal static class PhysicalValidation
 {
  internal static readonly string[] Conditions={"unknown","near-mint","lightly-played","moderately-played","heavily-played","damaged"};
  internal static void Edit(PhysicalEditData e,Catalog c) {
   if(e==null || string.IsNullOrWhiteSpace(e.transactionId) || e.transactionId.Length>256 || e.transactionId.Trim()!=e.transactionId) throw new InvalidDataException("Invalid physical transaction");
   foreach(char ch in e.transactionId) if(char.IsControl(ch)) throw new InvalidDataException("Invalid physical transaction");
   DateTime dt; if(e.createdUtc==null || !e.createdUtc.EndsWith("Z",StringComparison.Ordinal) || !DateTime.TryParseExact(e.createdUtc,new[]{"yyyy-MM-dd'T'HH:mm:ss'Z'","yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"},CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out dt)) throw new InvalidDataException("Invalid UTC time");
   if(Array.IndexOf(Conditions,e.condition)<0 || e.quantity<0 || e.quantity>99999) throw new InvalidDataException("Invalid condition or quantity");
   CardPrinting card; try {card=c.Find(e.cardId);} catch(ArgumentNullException) {throw new InvalidDataException("Invalid physical identity");} catch(KeyNotFoundException) {throw new InvalidDataException("Invalid physical identity");}
   if(!card.BoosterEligible || card.CollectorNumber=="8" || card.PrintingId!=c.PrintingId) throw new InvalidDataException("Ineligible physical identity");
  }
  internal static void State(PhysicalStateData d,Catalog c) {
   if(d==null || d.schemaVersion!=1 || d.edits==null || d.revision!=d.edits.LongLength) throw new InvalidDataException("Unsupported or invalid physical state");
   var ids=new HashSet<string>(StringComparer.Ordinal); foreach(var e in d.edits) {Edit(e,c); if(!ids.Add(e.transactionId)) throw new InvalidDataException("Duplicate physical transaction");}
  }
  internal static bool Same(PhysicalEditData a,PhysicalEditData b) { return a.transactionId==b.transactionId && a.createdUtc==b.createdUtc && a.cardId==b.cardId && a.condition==b.condition && a.quantity==b.quantity; }
  internal static PhysicalStateData Copy(PhysicalStateData d) {
   var a=new PhysicalEditData[d.edits.Length]; for(int i=0;i<a.Length;i++) {var e=d.edits[i]; a[i]=new PhysicalEditData {transactionId=e.transactionId,createdUtc=e.createdUtc,cardId=e.cardId,condition=e.condition,quantity=e.quantity};}
   return new PhysicalStateData {schemaVersion=d.schemaVersion,revision=d.revision,edits=a};
  }
  internal static PhysicalSnapshot Snapshot(PhysicalStateData d,string notice) {
   var map=new SortedDictionary<string,PhysicalHolding>(StringComparer.Ordinal); foreach(var e in d.edits) map[e.cardId+"/"+e.condition]=new PhysicalHolding(e.cardId,e.condition,e.quantity);
   var a=new List<PhysicalHolding>(); foreach(var h in map.Values) if(h.Quantity>0) a.Add(h); return new PhysicalSnapshot(d.revision,a.ToArray(),notice);
  }
 }
}
