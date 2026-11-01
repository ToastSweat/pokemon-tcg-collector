using System;using System.Collections.Generic;using System.Globalization;using UnityEngine;using VintageCollector.Shopping;using VintageCollector.Presentation;
namespace VintageCollector.Runtime {
 [DefaultExecutionOrder(150)][DisallowMultipleComponent]
 public sealed class Phase6ShoppingHost:MonoBehaviour {
  OpeningGame game;Phase5CollectionHost collection;ShoppingPresentation panel;BinderPresentation binder;TabletopPresentation table;ShoppingNotesStore store;ShoppingNotesSnapshot lastAcknowledgedSnapshot;IDisposable lease;WantedCard wanted;string profileRoot,cardId,message="",deleteId;bool origin,closed,bound,opening;long version;
  readonly Dictionary<string,ShoppingDraft> drafts=new Dictionary<string,ShoppingDraft>(StringComparer.Ordinal);ShoppingDraft draft;ShoppingNoteRequest pending;string pendingCard;
  public IShoppingUserActions UserActions{get;set;}
  public Func<string,VintageCollector.Domain.ApprovedSetRegistry,ShoppingNotesStore> StoreFactory{get;set;}
  void Update(){if(closed)return;if(!bound){if(!StartupLoadingCoordinator.IsReady)return;var g=GetComponent<OpeningGame>();var c=GetComponent<Phase5CollectionHost>();var p=GetComponent<ShoppingPresentation>();var b=GetComponent<BinderPresentation>();var t=GetComponent<TabletopPresentation>();if(g!=null&&c!=null&&p!=null&&b!=null&&t!=null&&c.IsReady)Bind(g,c,p,b,t);}else if(lease!=null){if(Authority())Publish();else Close();}}
  public void Bind(OpeningGame g,Phase5CollectionHost c,ShoppingPresentation p,BinderPresentation b,TabletopPresentation t){
   if(bound||closed)return;if(g==null||c==null||p==null||b==null||t==null||!c.IsReady||c.Game!=g||g.ApprovedSets==null||g.Paths==null||g.gameObject!=gameObject||c.gameObject!=gameObject||p.gameObject!=gameObject||b.gameObject!=gameObject||t.gameObject!=gameObject)throw new InvalidOperationException("Shopping requires ready same-object hosts.");
   game=g;collection=c;panel=p;binder=b;table=t;profileRoot=g.Paths.SaveDirectory;UserActions=UserActions??new ShoppingUserActions();binder.ShoppingRequested+=FromBinder;table.ShoppingRequested+=FromInspection;collection.ShoppingInvalidated+=Close;game.ShuttingDown+=Shutdown;panel.ActionRequested+=Dispatch;panel.DraftEdited+=Edit;panel.Authority=Authority;bound=true;collection.ShoppingEnabled=true;
  }
  public Dictionary<string,byte[]> CaptureShoppingBackupFiles(){
   if(closed||!bound||game==null||game.Paths==null||game.Paths.SaveDirectory!=profileRoot||game.ApprovedSets==null)throw new InvalidOperationException("Shopping backup requires the bound active profile");
   if(pending!=null)throw new InvalidOperationException("Shopping acknowledgement uncertain; retry before backup");
   return store!=null?store.ExportAcknowledgedFiles():ShoppingArchive.CaptureAcknowledgedFiles(profileRoot,game.ApprovedSets);
  }
  void FromBinder(string id){Open(id,true);}void FromInspection(string id){Open(id,false);}
  bool Authority(){bool valid=!closed&&bound&&lease!=null&&StartupLoadingCoordinator.IsReady&&game.IsAcceptingInput&&game.Paths.SaveDirectory==profileRoot&&collection.CanUseShopping(cardId,origin);if(!valid&&lease!=null)Close();return valid;}
  void Open(string id,bool fromBinder){if(closed||!bound||opening||lease!=null||!StartupLoadingCoordinator.IsReady||!game.IsAcceptingInput||game.Paths.SaveDirectory!=profileRoot||!collection.CanUseShopping(id,fromBinder))return;opening=true;try{var w=WantedCard.FromRegistry(game.ApprovedSets,id);IDisposable acquired;if(!collection.TryBeginShopping(id,fromBinder,out acquired))return;lease=acquired;wanted=w;cardId=id;origin=fromBinder;drafts.TryGetValue(id,out draft);deleteId=null;
    if(store==null){try{store=StoreFactory==null?new ShoppingNotesStore(profileRoot,game.ApprovedSets):StoreFactory(profileRoot,game.ApprovedSets);lastAcknowledgedSnapshot=store.Snapshot;message="";}catch(Exception e){LocalError(e);}}
    Publish();}catch(Exception e){LocalError(e);Close();}finally{opening=false;}}
  void LocalError(Exception e){message="Shopping notes/action unavailable ("+e.GetType().Name+"). Your collection is unaffected. Close or retry; existing files are not reset.";}
  public void Close(){if(draft!=null&&cardId!=null)drafts[cardId]=draft.Copy();deleteId=null;if(panel!=null)panel.Bind(null);var l=lease;lease=null;wanted=null;cardId=null;try{if(l!=null)l.Dispose();}catch(Exception e){LocalError(e);if(closed)throw;}}
  void Edit(ShoppingDraft value){if(!Authority()){Close();return;}if(pending!=null||value==null||draft==null)return;try{var copy=value.Copy();if(copy.Data.recordId!=draft.Data.recordId||copy.Data.canonicalCardId!=cardId)return;draft=copy;drafts[cardId]=draft.Copy();deleteId=null;}catch(Exception e){LocalError(e);}Publish();}
  static string Amount(long cents){return (cents/100).ToString(CultureInfo.InvariantCulture)+"."+(cents%100).ToString("D2",CultureInfo.InvariantCulture);}
  void Publish(){if(!Authority()){Close();return;}var rows=new List<ShoppingRecordView>();if(lastAcknowledgedSnapshot!=null)foreach(var f in lastAcknowledgedSnapshot.Records){var d=f.ToData();if(d.canonicalCardId!=cardId)continue;var m=Money.Summarize(d.rawNearMintUSD);rows.Add(new ShoppingRecordView(d.recordId,(d.title??"(untitled)")+" — "+d.provider,(m.FullTotalKnown?"Full total USD "+Amount(m.FullTotalCents):"Known subtotal USD "+Amount(m.KnownSubtotalCents)+"; full total unknown"),f.Status));}
   panel.Bind(new ShoppingView(wanted,++version,true,store!=null&&pending==null,pending!=null&&pendingCard==cardId,deleteId!=null,message,lastAcknowledgedSnapshot==null?"Notes not loaded":(pending!=null?"Last saved notes (write acknowledgement uncertain)":"Saved notes; revision "+lastAcknowledgedSnapshot.Revision),draft,rows.ToArray()));}
  FrozenComparison Find(string id){if(lastAcknowledgedSnapshot!=null)foreach(var r in lastAcknowledgedSnapshot.Records)if(r.ToData().recordId==id&&r.ToData().canonicalCardId==cardId)return r;return null;}
  void New(){draft=new ShoppingDraft{Data=new ComparisonData{schemaVersion=1,canonicalCardId=cardId,recordId=Guid.NewGuid().ToString("N"),provider="ebay",listingUrl="",observedUtc=DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ",CultureInfo.InvariantCulture),rawNearMintUSD=new MoneyData(),review=new ReviewData()}};drafts[cardId]=draft.Copy();deleteId=null;message="New manual observation; enter a listing URL before saving.";}
  static long Parse(bool known,string text){long v;if(!known)return 0;if(!Money.TryParseUsd(text,out v))throw new ArgumentException("Invalid USD amount");return v;}
  void ApplyPending(){if(!Authority()||pending==null||pendingCard!=cardId)return;try{lastAcknowledgedSnapshot=store.Apply(pending);if(pending.IsDelete){drafts.Remove(pendingCard);if(draft!=null&&draft.Data.recordId==pending.RecordId)draft=null;}pending=null;pendingCard=null;deleteId=null;message="Saved locally. No purchase or ownership change.";}catch(Exception e){LocalError(e);if(!store.IsUncertain){pending=null;pendingCard=null;}else message="Write acknowledgement uncertain. Retained exact request; Retry only on this card. Other note changes blocked. Last saved list may be stale.";}}
  void Dispatch(string action,string value){
   if(action=="quit"){if(game!=null)game.RequestQuit();return;}if(action=="close"){Close();return;}
   if(!Authority()){Close();return;}
   try{
    if(action=="search"){ShoppingLinks.Search(wanted,value);UserActions.Search(wanted,value);}
    else if(action=="copy")UserActions.CopyDescription(wanted);
    else if(action=="listing"){if(draft==null)return;var d=draft.Copy().Data;UserActions.OpenListing(d.provider,ShoppingLinks.ValidateListing(d.listingUrl,d.provider));}
    else if(action=="retry"){ApplyPending();}
    else if(pending!=null){message="Resolve the retained request on its original card before other note changes.";}
    else if(action=="reload"){if(store==null){store=StoreFactory==null?new ShoppingNotesStore(profileRoot,game.ApprovedSets):StoreFactory(profileRoot,game.ApprovedSets);}lastAcknowledgedSnapshot=store.Reload();message="Notes loaded.";}
    else if(store==null){message="Notes not loaded. Search and copy remain available.";}
    else if(action=="new")New();
    else if(action=="select"){var f=Find(value);if(f!=null){var d=f.ToData();draft=new ShoppingDraft{Data=d,Price=Amount(d.rawNearMintUSD.priceCents),Shipping=Amount(d.rawNearMintUSD.shippingCents),Tax=Amount(d.rawNearMintUSD.taxCents),Fees=Amount(d.rawNearMintUSD.feesCents)};drafts[cardId]=draft.Copy();deleteId=null;}}
    else if(action=="save"&&draft!=null){var d=draft.Copy().Data;var m=d.rawNearMintUSD;m.priceCents=Parse(m.priceKnown,draft.Price);m.shippingCents=Parse(m.shippingKnown,draft.Shipping);m.taxCents=Parse(m.taxKnown,draft.Tax);m.feesCents=Parse(m.feesKnown,draft.Fees);var frozen=ComparisonValidator.Freeze(game.ApprovedSets,d);pending=ShoppingNoteRequest.Upsert(Guid.NewGuid().ToString("N"),frozen);pendingCard=cardId;ApplyPending();}
    else if(action=="delete"&&draft!=null&&Find(draft.Data.recordId)!=null){deleteId=draft.Data.recordId;message="Confirm deletion of this saved comparison only.";}
    else if(action=="delete-confirm"&&draft!=null&&deleteId==draft.Data.recordId&&Find(deleteId)!=null){pending=ShoppingNoteRequest.Delete(Guid.NewGuid().ToString("N"),deleteId);pendingCard=cardId;ApplyPending();}
    else if(action=="delete-cancel")deleteId=null;
   }catch(Exception e){LocalError(e);}Publish();
  }
  public void Shutdown(){if(closed)return;closed=true;var failures=new List<Exception>();try{Close();}catch(Exception e){failures.Add(e);}try{if(store!=null)store.Dispose();}catch(Exception e){failures.Add(e);}finally{store=null;}
   if(bound){binder.ShoppingRequested-=FromBinder;table.ShoppingRequested-=FromInspection;collection.ShoppingInvalidated-=Close;game.ShuttingDown-=Shutdown;panel.ActionRequested-=Dispatch;panel.DraftEdited-=Edit;panel.Authority=null;collection.ShoppingEnabled=false;bound=false;}if(failures.Count>0)throw new AggregateException(failures);
  }
  void OnDestroy(){Shutdown();}
 }
}
