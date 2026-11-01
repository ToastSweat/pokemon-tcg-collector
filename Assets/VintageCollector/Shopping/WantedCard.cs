using System;
using VintageCollector.Domain;
namespace VintageCollector.Shopping {
 public sealed class WantedCard {
  public string CanonicalCardId {get;private set;} public string DisplayName {get;private set;} public string SetId {get;private set;} public string SetName {get;private set;}
  public string Language {get;private set;} public string Edition {get;private set;} public string CollectorNumber {get;private set;} public int CollectorTotal {get;private set;} public string Finish {get;private set;}
  public string Condition {get{return "Near Mint";}} public bool RawUngraded {get{return true;}}
  private WantedCard(){}
  public static WantedCard FromRegistry(ApprovedSetRegistry registry,string canonicalCardId){
   if(registry==null)throw new ArgumentNullException("registry");var c=registry.FindCard(canonicalCardId);
   if(!registry.IsCompletionEligible(canonicalCardId))throw new ArgumentException("Not completion eligible");
   string name;int total;switch(c.PrintingId){case "base-en-unlimited1999":name="Base Set";total=102;break;case "jungle-en-unlimited1999":name="Jungle";total=64;break;case "fossil-en-unlimited1999":name="Fossil";total=62;break;default:throw new ArgumentException("Unsupported printing");}
   if(c.Language!="en"||c.Edition!="unlimited")throw new ArgumentException("Unsupported wanted identity");
   return new WantedCard{CanonicalCardId=c.Id,DisplayName=c.DisplayName,SetId=c.SetId,SetName=name,Language=c.Language,Edition=c.Edition,CollectorNumber=c.CollectorNumber,CollectorTotal=total,Finish=c.Finish};
  }
 }
}
