using System;using VintageCollector.Shopping;
namespace VintageCollector.Presentation {
 public sealed class ShoppingDraft {
  public ComparisonData Data;public string Price="",Shipping="",Tax="",Fees="";
  public ShoppingDraft Copy(){var d=Data;var m=d.rawNearMintUSD;var r=d.review;return new ShoppingDraft{Price=Price,Shipping=Shipping,Tax=Tax,Fees=Fees,Data=new ComparisonData{schemaVersion=d.schemaVersion,canonicalCardId=d.canonicalCardId,recordId=d.recordId,provider=d.provider,listingUrl=d.listingUrl,title=d.title,seller=d.seller,note=d.note,observedUtc=d.observedUtc,rawNearMintUSD=new MoneyData{priceKnown=m.priceKnown,priceCents=m.priceCents,shippingKnown=m.shippingKnown,shippingCents=m.shippingCents,taxKnown=m.taxKnown,taxCents=m.taxCents,feesKnown=m.feesKnown,feesCents=m.feesCents},review=new ReviewData{manualReviewed=r.manualReviewed,language=r.language,setId=r.setId,collectorNumber=r.collectorNumber,collectorTotal=r.collectorTotal,edition=r.edition,finish=r.finish,condition=r.condition,grading=r.grading}}};}
 }
 public sealed class ShoppingRecordView {public string Id{get;private set;}public string Caption{get;private set;}public string Summary{get;private set;}public ReviewStatus Status{get;private set;}public ShoppingRecordView(string id,string caption,string summary,ReviewStatus status){Id=id;Caption=caption;Summary=summary;Status=status;}}
 public sealed class ShoppingView {
  readonly ShoppingDraft draft;readonly ShoppingRecordView[] records;
  public WantedCard Wanted{get;private set;}public long Version{get;private set;}public bool CanAct{get;private set;}public bool CanMutate{get;private set;}public bool CanRetry{get;private set;}public bool ConfirmDelete{get;private set;}public string Message{get;private set;}public string NotesLabel{get;private set;}
  public ShoppingDraft Draft{get{return draft==null?null:draft.Copy();}}public ShoppingRecordView[] Records{get{return (ShoppingRecordView[])records.Clone();}}
  public ShoppingView(WantedCard w,long v,bool act,bool mutate,bool retry,bool confirm,string message,string notes,ShoppingDraft d,ShoppingRecordView[] rs){Wanted=w;Version=v;CanAct=act;CanMutate=mutate;CanRetry=retry;ConfirmDelete=confirm;Message=message;NotesLabel=notes;draft=d==null?null:d.Copy();records=(ShoppingRecordView[])rs.Clone();}
 }
}
