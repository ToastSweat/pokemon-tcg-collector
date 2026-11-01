using System;
using System.Globalization;
using System.Collections.Generic;
using VintageCollector.Domain;
using VintageCollector.Persistence;
namespace VintageCollector.Collections
{
 public static class BinderProjection
 {
  public static BinderSnapshot Build(Catalog catalog,SessionSnapshot session,PhysicalSnapshot physical,string mode,string filter,string search) {
   var registry=ApprovedSetRegistry.ForLegacyBase(catalog);return Build(registry,catalog.PrintingId,session,physical,mode,filter,search);
  }
  public static BinderSnapshot Build(ApprovedSetRegistry registry,string printingId,SessionSnapshot session,PhysicalSnapshot physical,string mode,string filter,string search) {
   if(registry==null || session==null || physical==null) throw new ArgumentNullException();
   var definition=registry.FindSet(printingId);var catalog=definition.Catalog;
   if(mode!="virtual" && mode!="physical") throw new ArgumentException("Unknown collection mode");
   if(Array.IndexOf(new[]{"all","owned","missing","duplicates","holo"},filter)<0) throw new ArgumentException("Unknown collection filter");
   var counts=new Dictionary<string,int>(StringComparer.Ordinal);
   if(mode=="virtual") {
    // Never consult VirtualHoldings: those include durable but still concealed awards.
    foreach(var pack in session.Packs) for(int i=0;i<pack.RevealedCount;i++) Add(counts,pack.OrderedCardIds[i],1);
   } else foreach(var h in physical.Holdings) Add(counts,h.CardId,h.Quantity);
   var cards=new List<CardPrinting>(); foreach(var c in catalog.Cards) if(registry.IsCompletionEligible(c.Id) && c.PrintingId==printingId) cards.Add(c);
   if(cards.Count!=definition.CompletionTarget) throw new ArgumentException("Approved printing target mismatch");
   cards.Sort((a,b)=>int.Parse(a.CollectorNumber,CultureInfo.InvariantCulture).CompareTo(int.Parse(b.CollectorNumber,CultureInfo.InvariantCulture)));
   int unique=0,total=0; var rows=new List<BinderRow>(); search=search??"";
   foreach(var c in cards) {
    int n; counts.TryGetValue(c.Id,out n); if(n>0) unique++; total=checked(total+n);
    bool match=filter=="all" || filter=="owned"&&n>0 || filter=="missing"&&n==0 || filter=="duplicates"&&n>1 || filter=="holo"&&c.Finish=="holo";
    if(match && (search.Length==0 || c.DisplayName.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0 || c.CollectorNumber==search)) rows.Add(new BinderRow(c,n));
   }
   return new BinderSnapshot(printingId,definition.CompletionTarget,rows.ToArray(),unique,total);
  }
  static void Add(Dictionary<string,int> map,string id,int q) {int n;map.TryGetValue(id,out n);map[id]=checked(n+q);}
 }
}
