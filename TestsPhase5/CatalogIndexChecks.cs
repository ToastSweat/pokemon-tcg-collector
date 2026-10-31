using System;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using VintageCollector.Domain;

// Pure authoring-only tests with clearly synthetic metadata. No saves or pack generation.
public static class CatalogIndexChecks
{
    static void Require(bool value,string message) {if(!value)throw new Exception(message);}
    static Catalog Fixture(string printing="fixture-base",string version="v1") {
        return Catalog.FromData(new CatalogData {
            schemaVersion=1,catalogVersion=version,printingId=printing,
            completionTargetStatus="synthetic-test-only",sourceResearch="synthetic-test-fixture",
            cards=new[]{new CardData {
                id="pkmn-"+printing+"-001-nonholo-standard",sourceId="fixture-1",
                displayName="Synthetic fixture card",sourceName="Synthetic fixture card",setId=printing,
                collectorNumber="1",language="en",edition="unlimited",printingId=printing,
                variant="standard",rarity="common",kind="pokemon",finish="nonholo",
                artReference="placeholder:card-face",boosterEligible=true
            }}
        });
    }
    static Type IndexType() {
        var type=typeof(Catalog).Assembly.GetType("VintageCollector.Authoring.PrintingCatalogIndex");
        Require(type!=null,"Missing read-only multi-catalog index; new sets cannot be looked up independently");return type;
    }
    static object New(params Catalog[] catalogs) {return Activator.CreateInstance(IndexType(),new object[]{catalogs});}
    static object Invoke(object index,string method,string value) {return IndexType().GetMethod(method).Invoke(index,new object[]{value});}
    static void OnePrintingLookup() {
        var catalog=Fixture();var index=New(catalog);
        Require(ReferenceEquals(Invoke(index,"FindCatalog",catalog.PrintingId),catalog),"Exact printing must resolve to its immutable catalog");
        Require(ReferenceEquals(Invoke(index,"FindCard",catalog.Cards[0].Id),catalog.Cards[0]),"Exact stable card identity must resolve");
    }
    static void RejectEmptyIndex() {
        try {New(new Catalog[0]);throw new Exception("Empty candidate catalog index must be rejected rather than look like successful data admission");}
        catch(TargetInvocationException error) {Require(error.InnerException is ArgumentException,"Empty input must raise a clear argument error");}
    }
    static void RejectNullInputs() {
        try {New((Catalog[])null);throw new Exception("Null catalog sequence must raise ArgumentNullException");}
        catch(TargetInvocationException error) {Require(error.InnerException is ArgumentNullException,"Null sequence must raise ArgumentNullException, not NullReferenceException");}
        try {New(new Catalog[]{null});throw new Exception("Null catalog entry must raise ArgumentException");}
        catch(TargetInvocationException error) {Require(error.InnerException is ArgumentException,"Null entry must raise ArgumentException, not NullReferenceException");}
    }
    static void DistinctPrintingIsolation() {
        var first=Fixture("fixture-jungle");var second=Fixture("fixture-fossil");var index=New(first,second);
        Require(!ReferenceEquals(Invoke(index,"FindCard",first.Cards[0].Id),Invoke(index,"FindCard",second.Cards[0].Id)),"Matching collector numbers across sets must not alias card identities");
        Require(ReferenceEquals(Invoke(index,"FindCatalog",second.PrintingId),second),"Each printing retains its own lookup");
    }
    static void RejectDuplicatePrinting() {
        try {New(Fixture("fixture-jungle","v1"),Fixture("fixture-jungle","v2"));throw new Exception("Two versions cannot silently shadow the same printing");}
        catch(TargetInvocationException error) {Require(error.InnerException is ArgumentException,"Duplicate printing must fail closed");}
    }
    static void ReadOnlyDetachedSequence() {
        var original=Fixture();var input=new[]{original};var index=New(input);input[0]=Fixture("replacement");
        var list=(System.Collections.Generic.IList<Catalog>)IndexType().GetProperty("Catalogs").GetValue(index,null);
        Require(ReferenceEquals(list[0],original),"Caller array mutation must not change admitted catalog list");
        try {list.Add(Fixture("another"));throw new Exception("Public catalog list must be read-only");}
        catch(NotSupportedException) {}
        Require(list[0].CompletionTargetStatus=="synthetic-test-only","Index must not promote a catalog's approval status");
    }
    static void ExactUnknownLookupRefusal() {
        var catalog=Fixture();var index=New(catalog);
        foreach(var method in new[]{"FindCatalog","FindCard"}) {
            string value=method=="FindCatalog"?catalog.PrintingId:catalog.Cards[0].Id;
            try {Invoke(index,method,value.ToUpperInvariant());throw new Exception("Case-changed identity must not match");}
            catch(TargetInvocationException error) {Require(error.InnerException is System.Collections.Generic.KeyNotFoundException,"Unknown lookup must be explicit");}
            try {Invoke(index,method,null);throw new Exception("Null lookup must not match");}
            catch(TargetInvocationException error) {Require(error.InnerException is ArgumentNullException,"Null lookup must be explicit");}
        }
    }
    [Serializable] public sealed class CatalogSummary {
        public string printingId,catalogVersion,completionTargetStatus;
        public int cardCount,holo,rare,uncommon,common;
    }
    [Serializable] public sealed class DataSummary {
        public CatalogSummary[] catalogs;public int totalCards;
        public bool packGenerationAttempted=false,savesOpened=false,nativeUnityPlayerTest=false;
    }
    static void ActualCatalogFiles(string[] paths) {
        var catalogs=new Catalog[paths.Length];var summaries=new CatalogSummary[paths.Length];int total=0;
        for(int i=0;i<paths.Length;i++) {
            CatalogData data;
            using(var stream=File.OpenRead(paths[i]))data=(CatalogData)new DataContractJsonSerializer(typeof(CatalogData)).ReadObject(stream);
            catalogs[i]=Catalog.FromData(data);
        }
        var index=New(catalogs);
        for(int i=0;i<catalogs.Length;i++) {
            var c=catalogs[i];var s=new CatalogSummary {printingId=c.PrintingId,catalogVersion=c.CatalogVersion,completionTargetStatus=c.CompletionTargetStatus,cardCount=c.Cards.Count};
            Require(ReferenceEquals(Invoke(index,"FindCatalog",c.PrintingId),c),"Actual printing must stay distinct");
            foreach(var card in c.Cards) {
                Require(ReferenceEquals(Invoke(index,"FindCard",card.Id),card),"Every actual stable identity must resolve in its own printing");
                if(card.Rarity=="holo")s.holo++;else if(card.Rarity=="rare")s.rare++;else if(card.Rarity=="uncommon")s.uncommon++;else if(card.Rarity=="common")s.common++;
            }
            summaries[i]=s;total+=c.Cards.Count;
        }
        var report=new DataSummary {catalogs=summaries,totalCards=total};
        using(var stream=new MemoryStream()) {
            new DataContractJsonSerializer(typeof(DataSummary)).WriteObject(stream,report);
            Console.WriteLine("CATALOG_DATA "+Encoding.UTF8.GetString(stream.ToArray()));
        }
    }
    public static int Main(string[] args) {
        int failed=0;
        var tests=new System.Collections.Generic.Dictionary<string,Action> {
            {"single-printing-exact-lookup",OnePrintingLookup},{"reject-empty-index",RejectEmptyIndex},{"reject-null-inputs",RejectNullInputs},
            {"distinct-printing-isolation",DistinctPrintingIsolation},{"duplicate-printing-refusal",RejectDuplicatePrinting},
            {"readonly-detached-no-approval-promotion",ReadOnlyDetachedSequence},{"exact-unknown-lookup-refusal",ExactUnknownLookupRefusal}
        };
        if(args.Length>0)tests.Add("actual-catalog-file-identity-and-rarity-readback",()=>ActualCatalogFiles(args));
        foreach(var test in tests) {
            try {test.Value();Console.WriteLine("PASS "+test.Key);}catch(Exception error){failed++;Console.WriteLine("FAIL "+test.Key+": "+error.Message);}
        }
        Console.WriteLine("CHECKS "+tests.Count+" FAILED "+failed);return failed==0?0:1;
    }
}
