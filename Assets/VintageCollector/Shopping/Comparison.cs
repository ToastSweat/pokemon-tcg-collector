using System;
using VintageCollector.Domain;
namespace VintageCollector.Shopping {
 public enum ReviewStatus { Unverified=0, Mismatch=1, ManualConfirmed=2 }
 [Serializable] public sealed class ReviewData {public bool manualReviewed;public string language,setId,collectorNumber,edition,finish,condition,grading;public int collectorTotal;}
 [Serializable] public sealed class ComparisonData {public int schemaVersion;public string canonicalCardId,recordId,provider,listingUrl,title,seller,note,observedUtc;public MoneyData rawNearMintUSD;public ReviewData review;}
 public sealed class FrozenComparison {
  readonly ComparisonData snapshot;public ReviewStatus Status{get;private set;}
  internal FrozenComparison(ComparisonData d,ReviewStatus status){snapshot=ComparisonValidator.Copy(d);Status=status;}
  public ComparisonData ToData(){return ComparisonValidator.Copy(snapshot);}
 }
 public static class ComparisonValidator {
  public static System.Collections.ObjectModel.ReadOnlyCollection<FrozenComparison> FreezeMany(ApprovedSetRegistry registry,ComparisonData[] data){
   if(data==null||data.Length>1000)throw new ArgumentException("Comparison batch limit 1000");var inputs=(ComparisonData[])data.Clone();var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);var result=new FrozenComparison[inputs.Length];
   for(int i=0;i<inputs.Length;i++){result[i]=Freeze(registry,inputs[i]);if(!ids.Add(result[i].ToData().recordId))throw new ArgumentException("Duplicate recordId");}return Array.AsReadOnly(result);
  }
  public static FrozenComparison Freeze(ApprovedSetRegistry registry,ComparisonData data){
   if(data==null||data.rawNearMintUSD==null||data.review==null)throw new ArgumentException("Missing comparison graph");var d=Copy(data);
   if(d.schemaVersion!=1)throw new ArgumentException("Unsupported comparison schema");
   Guid id;if(d.recordId==null||!Guid.TryParseExact(d.recordId,"N",out id)||id==Guid.Empty||id.ToString("N")!=d.recordId)throw new ArgumentException("recordId requires nonempty lowercase Guid N");
   var w=WantedCard.FromRegistry(registry,d.canonicalCardId);ShoppingLinks.ValidateListing(d.listingUrl,d.provider);
   Text(d.title,300);Text(d.seller,200);Text(d.note,2000);
   DateTime when;if(!DateTime.TryParseExact(d.observedUtc,"yyyy-MM-dd'T'HH:mm:ss'Z'",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal|System.Globalization.DateTimeStyles.AdjustToUniversal,out when))throw new ArgumentException("observedUtc requires UTC ISO seconds Z");
   Money.Summarize(d.rawNearMintUSD);var r=d.review;bool unknown=false,mismatch=false;
   Compare(r.language,w.Language,ref unknown,ref mismatch);Compare(r.setId,w.SetId,ref unknown,ref mismatch);Compare(r.collectorNumber,w.CollectorNumber,ref unknown,ref mismatch);
   if(r.collectorTotal<0)throw new ArgumentException("Invalid collector total");if(r.collectorTotal==0)unknown=true;else if(r.collectorTotal!=w.CollectorTotal)mismatch=true;
   Compare(r.edition,w.Edition,ref unknown,ref mismatch);Compare(r.finish,w.Finish,ref unknown,ref mismatch);Compare(r.condition,w.Condition,ref unknown,ref mismatch);Compare(r.grading,"raw-ungraded",ref unknown,ref mismatch);
   return new FrozenComparison(d,mismatch?ReviewStatus.Mismatch:unknown||!r.manualReviewed?ReviewStatus.Unverified:ReviewStatus.ManualConfirmed);
  }
  static void Compare(string observed,string wanted,ref bool unknown,ref bool mismatch){Text(observed,100);if(string.IsNullOrEmpty(observed))unknown=true;else if(observed!=wanted)mismatch=true;}
  static void Text(string value,int max){if(value==null)return;if(value.Length>max)throw new ArgumentException("Text exceeds limit");foreach(char c in value)if(char.IsControl(c)||char.IsSurrogate(c))throw new ArgumentException("Unsupported control or surrogate text");}
  internal static ComparisonData Copy(ComparisonData d){var m=d.rawNearMintUSD;var r=d.review;return new ComparisonData{schemaVersion=d.schemaVersion,canonicalCardId=d.canonicalCardId,recordId=d.recordId,provider=d.provider,listingUrl=d.listingUrl,title=d.title,seller=d.seller,note=d.note,observedUtc=d.observedUtc,rawNearMintUSD=new MoneyData{priceKnown=m.priceKnown,shippingKnown=m.shippingKnown,taxKnown=m.taxKnown,feesKnown=m.feesKnown,priceCents=m.priceCents,shippingCents=m.shippingCents,taxCents=m.taxCents,feesCents=m.feesCents},review=new ReviewData{manualReviewed=r.manualReviewed,language=r.language,setId=r.setId,collectorNumber=r.collectorNumber,collectorTotal=r.collectorTotal,edition=r.edition,finish=r.finish,condition=r.condition,grading=r.grading}};}
 }
}
