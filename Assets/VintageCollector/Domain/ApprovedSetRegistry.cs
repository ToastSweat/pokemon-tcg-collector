using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace VintageCollector.Domain
{
 // Only the public factories can confer approval. Definitions never expose mutable rules.
 public sealed class ApprovedSetRegistry
 {
  readonly Dictionary<string,ApprovedSetDefinition> sets=new Dictionary<string,ApprovedSetDefinition>(StringComparer.Ordinal);
  readonly Dictionary<string,CardPrinting> cards=new Dictionary<string,CardPrinting>(StringComparer.Ordinal);
  public ReadOnlyCollection<ApprovedSetDefinition> Sets { get; private set; }
  internal ApprovedSetRegistry(ApprovedSetDefinition[] definitions) {
   var copy=(ApprovedSetDefinition[])definitions.Clone();
   foreach(var d in copy) {sets.Add(d.PrintingId,d);foreach(var c in d.Catalog.Cards) cards.Add(c.Id,c);}
   Sets=Array.AsReadOnly(copy);
  }
  public static ApprovedSetRegistry ForLegacyBase(Catalog baseCatalog) {
   ApprovedSetPolicy.ValidateLegacyBase(baseCatalog);
   return new ApprovedSetRegistry(new[]{new ApprovedSetDefinition(baseCatalog,null,101)});
  }
  public static ApprovedSetRegistry FromApprovedData(Catalog[] catalogs,RuleData[] models) {
   if(catalogs==null || models==null) throw new ArgumentNullException();
   if(catalogs.Length!=3 || models.Length!=3) throw new ArgumentException("Exactly three approved catalogs and models required");
   var byPrinting=new Dictionary<string,RuleData>(StringComparer.Ordinal);
   foreach(var r in models) {if(r==null || string.IsNullOrEmpty(r.printingId) || byPrinting.ContainsKey(r.printingId)) throw new ArgumentException("Missing or duplicate model printing");byPrinting.Add(r.printingId,r);}
   var defs=new ApprovedSetDefinition[3];var seen=new HashSet<string>(StringComparer.Ordinal);
   for(int i=0;i<3;i++) {
    var c=catalogs[i];RuleData r;
    if(c==null || !seen.Add(c.PrintingId) || !byPrinting.TryGetValue(c.PrintingId,out r)) throw new ArgumentException("Missing or duplicate catalog printing");
    int target=ApprovedSetPolicy.Validate(c,r);defs[i]=new ApprovedSetDefinition(c,r,target);
   }
   return new ApprovedSetRegistry(defs);
  }
  public ApprovedSetDefinition FindSet(string printingId) {if(printingId==null)throw new ArgumentNullException("printingId");return sets[printingId];}
  public Catalog FindCatalog(string printingId,string catalogVersion) {var d=FindSet(printingId);if(d.CatalogVersion!=catalogVersion)throw new KeyNotFoundException("Unknown catalog version");return d.Catalog;}
  public CardPrinting FindCard(string cardId) {if(cardId==null)throw new ArgumentNullException("cardId");return cards[cardId];}
  public bool IsCompletionEligible(string cardId) {var c=FindCard(cardId);return c.BoosterEligible && !(c.PrintingId==ApprovedSetPolicy.BasePrinting && c.CollectorNumber=="8");}
  public PackGenerator CreateGenerator(string printingId) {return FindSet(printingId).CreateGenerator();}
 }
 public sealed class ApprovedSetDefinition
 {
  readonly RuleData rule;
  readonly Dictionary<string,HashSet<string>> pools;
  public string SetId {get;private set;}
  public string PrintingId {get;private set;}
  public string CatalogVersion {get;private set;}
  public string ModelId {get;private set;}
  public string ModelVersion {get;private set;}
  public string Status {get;private set;}
  public string RngAlgorithm {get;private set;}
  public int CompletionTarget {get;private set;}
  public Catalog Catalog {get;private set;}
  internal ApprovedSetDefinition(Catalog catalog,RuleData model,int target) {
   Catalog=catalog;PrintingId=catalog.PrintingId;CatalogVersion=catalog.CatalogVersion;CompletionTarget=target;SetId=catalog.Cards[0].SetId;
   ModelId=PrintingId+"-sheet-informed";ModelVersion="0.1.0";Status="approved-provisional";RngAlgorithm="xorshift32-v1";
   if(model!=null) {
    // This validates and snapshots independently; retain no caller-owned arrays/entries.
    new PackGenerator(catalog,model);rule=Copy(model);
    pools=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal);
    foreach(var p in rule.pools){var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var e in p.entries)ids.Add(e.cardId);pools.Add(p.id,ids);}
   }
  }
  internal PackGenerator CreateGenerator(){if(rule==null)throw new InvalidOperationException("Legacy catalog adapter has no generation authority");return new PackGenerator(Catalog,rule);}
  static RuleData Copy(RuleData r) {
   var result=new RuleData{schemaVersion=r.schemaVersion,modelId=r.modelId,modelVersion=r.modelVersion,status=r.status,printingId=r.printingId,rngAlgorithm=r.rngAlgorithm,researchRegister=r.researchRegister,approvalRecord=r.approvalRecord,approximations=(string[])r.approximations.Clone(),pools=new PoolData[r.pools.Length],slots=new SlotData[r.slots.Length]};
   for(int i=0;i<r.pools.Length;i++){var p=r.pools[i];var q=new PoolData{id=p.id,uniqueWithinPack=p.uniqueWithinPack,entries=new WeightedEntryData[p.entries.Length]};for(int j=0;j<q.entries.Length;j++)q.entries[j]=new WeightedEntryData{cardId=p.entries[j].cardId,weight=p.entries[j].weight};result.pools[i]=q;}
   for(int i=0;i<r.slots.Length;i++){var s=r.slots[i];result.slots[i]=new SlotData{poolId=s.poolId,alternativePoolId=s.alternativePoolId,alternativeNumerator=s.alternativeNumerator,alternativeDenominator=s.alternativeDenominator};}return result;
  }
  public void ValidateOrderedCardIds(string[] orderedCardIds) {
   if(orderedCardIds==null || orderedCardIds.Length!=11)throw new ArgumentException("Expected eleven card identities");
   var used=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal);
   for(int i=0;i<11;i++) {
    CardPrinting c;try{c=Catalog.Find(orderedCardIds[i]);}catch(KeyNotFoundException){throw new ArgumentException("Unknown card");}
    if(!c.BoosterEligible || c.PrintingId!=PrintingId || c.PrintingId==ApprovedSetPolicy.BasePrinting && c.CollectorNumber=="8")throw new ArgumentException("Ineligible card");
    if(rule==null) {
     // Fixed canonical Base validation specification. Energy draws are with replacement.
     bool valid=i<5?c.Rarity=="common"&&c.Kind!="basic-energy":i<7?c.Kind=="basic-energy":i==7?c.Rarity=="rare"||c.Rarity=="holo":c.Rarity=="uncommon";
     if(!valid)throw new ArgumentException("Unsupported Base slot");
     if(i<5 || i>=8){string group=i<5?"common":"uncommon";HashSet<string> ids;if(!used.TryGetValue(group,out ids)){ids=new HashSet<string>(StringComparer.Ordinal);used.Add(group,ids);}if(!ids.Add(c.Id))throw new ArgumentException("Duplicate without-replacement card");}
    } else {
     var slot=rule.slots[i];string selected=slot.poolId;
     if(!pools[selected].Contains(c.Id)) {selected=slot.alternativePoolId;if(string.IsNullOrEmpty(selected)||!pools[selected].Contains(c.Id))throw new ArgumentException("Card outside approved slot pool");}
     PoolData pool=null;foreach(var p in rule.pools)if(p.id==selected)pool=p;
     if(pool.uniqueWithinPack){HashSet<string> ids;if(!used.TryGetValue(selected,out ids)){ids=new HashSet<string>(StringComparer.Ordinal);used.Add(selected,ids);}if(!ids.Add(c.Id))throw new ArgumentException("Duplicate without-replacement card");}
    }
   }
  }
 }
}
