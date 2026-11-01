using System;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
namespace VintageCollector.Domain
{
 // Frozen semantic policy, independent of caller DTOs. This is not a registration API.
 internal static class ApprovedSetPolicy
 {
  internal const string BasePrinting="base-en-unlimited1999";
  const string BaseCatalogSemantic="fb12ecc093d2a35147e00a4c95fe0a70fad75b9883ee01f54d2bce8db02eb668";
  internal static void ValidateLegacyBase(Catalog c) {
   if(c==null)throw new ArgumentNullException("baseCatalog");
   if(c.PrintingId!=BasePrinting || c.CatalogVersion!=BasePrinting+"-0.1.0" || CatalogFingerprint(c)!=BaseCatalogSemantic)throw new ArgumentException("Catalog is not the frozen canonical Base catalog");
  }
  // Pins independently compiled from reviewer-pinned canonical JSON; see the staged
  // approved-inputs.json and tools/freeze_phase5_core_policy.py. Never derive expected
  // semantics from the caller graph. Source-byte admission remains the bootstrap gate.
  internal static int Validate(Catalog c,RuleData r) {
   if(c==null || r==null)throw new ArgumentNullException();
   string catalogPin,modelPin;int target;
   switch(c.PrintingId) {
    case BasePrinting:catalogPin=BaseCatalogSemantic;modelPin="0da1e53494e3ec0b678e477a956b658049359f14eca5138a2be7d7980ebf9757";target=101;break;
    case "jungle-en-unlimited1999":catalogPin="711311431ff4ea5dd06c1b850218050e40fe57e0064ebf24d83664f38359ff69";modelPin="c901774b769a04574815f65db5d4e5784812f7cf0d9b36c0d40765d2bd7f5c42";target=64;break;
    case "fossil-en-unlimited1999":catalogPin="ee043bc8459df41d1cc4ab009641bf2d0d1a482101c64223945c7918acabc1c6";modelPin="3f1528ee73c28439983579eb07132123c925f62c49528beb15dc5d4de0bfc492";target=62;break;
    default:throw new ArgumentException("Unknown approved printing");
   }
   if(c.CatalogVersion!=c.PrintingId+"-0.1.0" || r.schemaVersion!=1 || r.printingId!=c.PrintingId || r.modelId!=c.PrintingId+"-sheet-informed" || r.modelVersion!="0.1.0" || r.status!="approved-provisional" || r.rngAlgorithm!="xorshift32-v1")throw new ArgumentException("Unsupported exact printing/catalog/model tuple");
   if(CatalogFingerprint(c)!=catalogPin || RuleFingerprint(r)!=modelPin)throw new ArgumentException("Catalog/model differs from the frozen approved semantic specification");
   return target;
  }
  internal static string RuleFingerprint(RuleData r) {
   if(r.approximations==null || r.pools==null || r.slots==null)throw new ArgumentException("Missing approved model arrays");
   var s=new StringBuilder();Field(s,r.schemaVersion.ToString(CultureInfo.InvariantCulture));
   foreach(string f in new[]{r.modelId,r.modelVersion,r.status,r.printingId,r.rngAlgorithm,r.researchRegister,r.approvalRecord})Field(s,f);
   Field(s,r.approximations.Length.ToString(CultureInfo.InvariantCulture));foreach(string a in r.approximations)Field(s,a);
   Field(s,r.pools.Length.ToString(CultureInfo.InvariantCulture));
   foreach(var p in r.pools) {
    if(p==null || p.entries==null)throw new ArgumentException("Missing approved pool");
    Field(s,p.id);Field(s,p.uniqueWithinPack?"true":"false");Field(s,p.entries.Length.ToString(CultureInfo.InvariantCulture));
    foreach(var e in p.entries){if(e==null)throw new ArgumentException("Null approved entry");Field(s,e.cardId);Field(s,e.weight.ToString(CultureInfo.InvariantCulture));}
   }
   Field(s,r.slots.Length.ToString(CultureInfo.InvariantCulture));
   foreach(var slot in r.slots){if(slot==null)throw new ArgumentException("Null approved slot");Field(s,slot.poolId);Field(s,slot.alternativePoolId);Field(s,slot.alternativeNumerator.ToString(CultureInfo.InvariantCulture));Field(s,slot.alternativeDenominator.ToString(CultureInfo.InvariantCulture));}
   return Hash(s);
  }
  static string Hash(StringBuilder s) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false,true).GetBytes(s.ToString()))).Replace("-","").ToLowerInvariant();}
  static void Field(StringBuilder s,string value) {if(value==null)throw new ArgumentException("Missing semantic field");s.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);}
  internal static string CatalogFingerprint(Catalog c) {
   var s=new StringBuilder();Field(s,c.CatalogVersion);Field(s,c.PrintingId);Field(s,c.CompletionTargetStatus);Field(s,c.SourceResearch);Field(s,c.Cards.Count.ToString(CultureInfo.InvariantCulture));
   var cards=new CardPrinting[c.Cards.Count];c.Cards.CopyTo(cards,0);Array.Sort(cards,(a,b)=>StringComparer.Ordinal.Compare(a.Id,b.Id));
   foreach(var x in cards)foreach(string f in new[]{x.Id,x.SourceId,x.DisplayName,x.SourceName,x.SetId,x.CollectorNumber,x.Language,x.Edition,x.PrintingId,x.Variant,x.Rarity,x.Kind,x.Finish,x.ArtReference,x.BoosterEligible?"true":"false"})Field(s,f);
   using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false,true).GetBytes(s.ToString()))).Replace("-","").ToLowerInvariant();
  }
 }
}
