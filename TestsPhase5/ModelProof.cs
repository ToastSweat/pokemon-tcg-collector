// Standalone proof-only harness. Production Domain is compiled into a separate DLL.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.IO.Compression;
using VintageCollector.Domain;

public static class ModelProof
{
    [Serializable] public sealed class Diagnostic {
        public uint seed; public int packIndex;
        public string modelId, modelVersion, catalogVersion, printingId, status, rngAlgorithm;
        public string[] orderedCardIds;
        public Draw[] draws;
    }
    [Serializable] public sealed class Draw { public int bound, ticket; }
    sealed class TraceRandom : IRandomSource {
        readonly SeededRandom rng; public readonly List<Draw> draws = new List<Draw>();
        public TraceRandom(uint seed) { rng = new SeededRandom(seed); }
        public int NextInt(int max) { int t = rng.NextInt(max); draws.Add(new Draw { bound=max, ticket=t }); return t; }
    }
    [Serializable] public sealed class SetProof {
        public string printingId; public bool passed; public int boundaryCases;
        public Diagnostic[] fixedDiagnostics;
        public Sample sample;
    }
    [Serializable] public sealed class CardCount {
        public string cardId, poolId;public int appearances;public int[] positions;
    }
    [Serializable] public sealed class Sample {
        public int packs,cardCount,holoPacks,rarePacks;public uint seed;
        public long uncompressedBytes;
        public string completeOrderedDiagnosticSha256,diagnosticsFile;
        public CardCount[] counts;
    }
    static Sample Simulate(Catalog catalog,RuleData rule,string output,int packs,uint seed) {
        var gen=new PackGenerator(catalog,rule);var rng=new TraceRandom(seed);
        var counts=new Dictionary<string,CardCount>(StringComparer.Ordinal);
        foreach(var p in rule.pools)foreach(var e in p.entries)counts.Add(e.cardId,new CardCount {cardId=e.cardId,poolId=p.id,positions=new int[11]});
        var sample=new Sample {packs=packs,seed=seed,diagnosticsFile=rule.printingId+".diagnostics.jsonl.gz"};
        var serializer=new DataContractJsonSerializer(typeof(Diagnostic));
        using(var hash=SHA256.Create())
        using(var file=new FileStream(Path.Combine(output,sample.diagnosticsFile),FileMode.CreateNew))
        using(var gzip=new GZipStream(file,CompressionMode.Compress))
        using(var stream=new MemoryStream()) {
            for(int i=0;i<packs;i++) {
                rng.draws.Clear();var p=gen.Generate(rng);
                Require(p.OrderedCardIds.Count==11 && rng.draws.Count==12,"Sample shape/draw count");
                Require(p.PrintingId==catalog.PrintingId && p.CatalogVersion==catalog.CatalogVersion && p.ModelId==rule.modelId && p.ModelVersion=="0.1.0" && p.Status=="approved-provisional" && p.RngAlgorithm=="xorshift32-v1","Every sample provenance");
                Require(p.OrderedCardIds.Take(7).Distinct().Count()==7 && p.OrderedCardIds.Skip(8).Distinct().Count()==3,"Every sample C/U uniqueness");
                for(int pos=0;pos<11;pos++) {
                    CardCount c;Require(counts.TryGetValue(p.OrderedCardIds[pos],out c),"Foreign sample ID");
                    Require(c.poolId==(pos<7?"C":pos>7?"U":c.poolId=="H"?"H":"R"),"Every sample slot eligibility");
                    c.appearances++;c.positions[pos]++;sample.cardCount++;
                }
                if(counts[p.OrderedCardIds[7]].poolId=="H")sample.holoPacks++;else sample.rarePacks++;
                var d=DiagnosticFrom(p,seed,i,rng.draws.ToArray());
                stream.SetLength(0);stream.Position=0;serializer.WriteObject(stream,d);stream.WriteByte(10);
                byte[] bytes=stream.ToArray();hash.TransformBlock(bytes,0,bytes.Length,bytes,0);gzip.Write(bytes,0,bytes.Length);sample.uncompressedBytes+=bytes.Length;
            }
            hash.TransformFinalBlock(new byte[0],0,0);sample.completeOrderedDiagnosticSha256=BitConverter.ToString(hash.Hash).Replace("-","").ToLowerInvariant();
        }
        sample.counts=rule.pools.SelectMany(p=>p.entries.Select(e=>counts[e.cardId])).ToArray();
        Require(sample.cardCount==packs*11 && sample.holoPacks+sample.rarePacks==packs,"Sample totals");
        return sample;
    }
    [Serializable] public sealed class Proof {
        public string[] checks; public int checkCount, failedCount;
        public SetProof[] sets;
    }
    static T Read<T>(string path) { using(var s=File.OpenRead(path))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(s); }
    static void Write<T>(string path, T value) { using(var s=File.Create(path))new DataContractJsonSerializer(typeof(T)).WriteObject(s,value); }
    static void Require(bool pass,string message) { if(!pass)throw new Exception(message); }
    static Diagnostic DiagnosticFrom(PackResult p,uint seed,int index,Draw[] draws) {
        return new Diagnostic { seed=seed,packIndex=index,modelId=p.ModelId,modelVersion=p.ModelVersion,catalogVersion=p.CatalogVersion,printingId=p.PrintingId,status=p.Status,rngAlgorithm=p.RngAlgorithm,orderedCardIds=p.OrderedCardIds.ToArray(),draws=draws };
    }
    sealed class TicketRandom : IRandomSource {
        readonly int targetCall,ticket,branch; int call;
        public int targetBound; public int CallCount {get {return call;}}
        public TicketRandom(int targetCall,int ticket,int branch) {this.targetCall=targetCall;this.ticket=ticket;this.branch=branch;}
        public int NextInt(int bound) {
            int at=call++;int value=at==7?branch:at==targetCall?ticket:0;
            if(at==targetCall)targetBound=bound;
            Require(value>=0 && value<bound,"Bad test ticket");return value;
        }
    }
    sealed class HostileRandom : IRandomSource {
        readonly int badCall;readonly bool high;int call;
        public HostileRandom(int badCall,bool high) {this.badCall=badCall;this.high=high;}
        public int NextInt(int bound) {return call++==badCall?(high?bound:-1):0;}
    }
    static int Boundaries(Catalog catalog,RuleData rule) {
        var generator=new PackGenerator(catalog,rule);int cases=0;
        foreach(var pool in rule.pools) {
            int depth=pool.id=="C"?7:pool.id=="U"?3:1;
            for(int removed=0;removed<depth;removed++) {
                var remaining=pool.entries.Skip(removed).ToArray();int total=remaining.Sum(e=>e.weight);
                for(int t=0;t<total;t++) {
                    int offset=0;string expected=null;
                    foreach(var e in remaining) {offset+=e.weight;if(t<offset) {expected=e.cardId;break;}}
                    int targetCall=pool.id=="C"?removed:pool.id=="U"?9+removed:8;
                    int slot=pool.id=="C"?removed:pool.id=="U"?8+removed:7;
                    var rng=new TicketRandom(targetCall,t,pool.id=="H"?0:1);
                    var p=generator.Generate(rng);
                    Require(rng.targetBound==total && rng.CallCount==12,"Removal must discard identity's whole weight and preserve draw order");
                    Require(p.OrderedCardIds[slot]==expected,"Weighted interval boundary mismatch");
                    cases++;
                }
            }
        }
        return cases;
    }
    static void Branches(Catalog catalog,RuleData rule) {
        var gen=new PackGenerator(catalog,rule);
        for(int branch=0;branch<3;branch++) {
            var rng=new TicketRandom(8,0,branch);var p=gen.Generate(rng);
            Require(catalog.Find(p.OrderedCardIds[7]).Rarity==(branch==0?"holo":"rare"),"Alternative threshold incorrect");
            Require(rng.CallCount==12,"Branch must consume one draw, then only selected pool draw");
        }
    }
    static void EightAndEligibility(Catalog catalog,RuleData rule) {
        Require(catalog.Cards.All(c=>c.BoosterEligible && c.Variant=="standard" && c.SetId==catalog.PrintingId.Split('-')[0]),"Standard scope only");
        foreach(var pool in rule.pools)foreach(var e in pool.entries) {
            var c=catalog.Find(e.cardId);string rarity=pool.id=="C"?"common":pool.id=="U"?"uncommon":pool.id=="H"?"holo":"rare";
            Require(c.Rarity==rarity && c.PrintingId==rule.printingId,"Cross-pool/cross-set identity");
        }
        var h=rule.pools.Single(p=>p.id=="H");int ticket=0;
        foreach(var e in h.entries) {if(catalog.Find(e.cardId).CollectorNumber=="8")break;ticket+=e.weight;}
        var pack=new PackGenerator(catalog,rule).Generate(new TicketRandom(8,ticket,0));
        Require(catalog.Find(pack.OrderedCardIds[7]).CollectorNumber=="8","Jungle/Fossil collector 8 must not inherit Base exclusion");
    }
    static void Immutable(string folder,string printing) {
        var d=Read<CatalogData>(Path.Combine(folder,printing+".catalog.json"));
        var r=Read<RuleData>(Path.Combine(folder,printing+".rules.json"));
        var c=Catalog.FromData(d);var gen=new PackGenerator(c,r);
        var previous=gen.Generate(new SeededRandom(42));var expected=previous.OrderedCardIds.ToArray();
        d.cards[0].id="mutated";d.catalogVersion="mutated";d.cards=null;
        r.modelId="mutated";r.approximations[0]="mutated";r.pools[0].entries[0].cardId="mutated";
        r.pools[0].entries[0].weight=999;r.slots[0].poolId="mutated";r.slots=null;r.pools=null;
        Require(gen.Generate(new SeededRandom(42)).OrderedCardIds.SequenceEqual(expected),"Generator must detach nested authoring DTOs");
        Require(previous.OrderedCardIds.SequenceEqual(expected) && previous.ModelId==printing+"-sheet-informed","Prior result immutable");
        bool refused=false;
        try {((IList<string>)previous.OrderedCardIds)[0]="mutated";}catch(NotSupportedException) {refused=true;}
        Require(refused && previous.OrderedCardIds.SequenceEqual(expected),"Result list must be readonly");
        Require(c.Cards[0].Id!="mutated" && c.CatalogVersion==printing+"-0.1.0","Catalog immutable identity");
    }
    static void Hostile(Catalog catalog,RuleData rule) {
        var gen=new PackGenerator(catalog,rule);var expected=gen.Generate(new SeededRandom(42)).OrderedCardIds.ToArray();
        for(int i=0;i<12;i++)foreach(bool high in new[]{false,true}) {
            bool refused=false;try {gen.Generate(new HostileRandom(i,high));}catch(InvalidOperationException) {refused=true;}
            Require(refused,"Hostile random value must be rejected on weighted and branch draws");
            Require(gen.Generate(new SeededRandom(42)).OrderedCardIds.SequenceEqual(expected),"Failure must not deplete future packs");
        }
        bool zero=false;try {new SeededRandom(0);}catch(ArgumentOutOfRangeException) {zero=true;}
        Require(zero,"Zero seed refused");
        bool bound=false;try {new SeededRandom(1).NextInt(0);}catch(ArgumentOutOfRangeException) {bound=true;}
        Require(bound,"Nonpositive bound refused");
    }
    static readonly uint[] Seeds = {1u,2u,3u,42u,0xffffffffu,0x4a554e47u,0x464f5353u};
    public static int Main(string[] args) {
        var checks=new List<string>();var results=new List<SetProof>();int failed=0;
        foreach(var set in new[]{"jungle","fossil"}) {
            string printing=set+"-en-unlimited1999";
            var data=Read<CatalogData>(Path.Combine(args[0],printing+".catalog.json"));
            var rule=Read<RuleData>(Path.Combine(args[0],printing+".rules.json"));
            var catalog=Catalog.FromData(data);var generator=new PackGenerator(catalog,rule);
            var diagnostics=new List<Diagnostic>();
            try {
                foreach(uint seed in Seeds) {
                    var rng=new TraceRandom(seed);var pack=generator.Generate(rng);
                    Require(pack.OrderedCardIds.Count==11,"Approved pack must contain eleven cards");
                    Require(pack.ModelId==printing+"-sheet-informed" && pack.Status=="approved-provisional","Wrong approved provenance");
                    Require(pack.CatalogVersion==printing+"-0.1.0" && pack.ModelVersion=="0.1.0","Wrong version");
                    Require(rng.draws.Count==12,"Exactly twelve bounded draws including rare/holo branch");
                    for(int i=0;i<11;i++) {
                        var card=catalog.Find(pack.OrderedCardIds[i]);
                        Require(card.PrintingId==printing && card.BoosterEligible,"Foreign/ineligible identity");
                        Require(card.Rarity==(i<7?"common":i>7?"uncommon":card.Finish=="holo"?"holo":"rare"),"Slot eligibility");
                    }
                    Require(pack.OrderedCardIds.Take(7).Distinct().Count()==7 && pack.OrderedCardIds.Skip(8).Distinct().Count()==3,"C/U identity uniqueness");
                    diagnostics.Add(DiagnosticFrom(pack,seed,0,rng.draws.ToArray()));
                }
                checks.Add(printing+":fixed-seed-complete-ordered-diagnostics");
                int boundaryCases=Boundaries(catalog,rule);checks.Add(printing+":every-weighted-ticket-after-prefix-removal");
                Branches(catalog,rule);checks.Add(printing+":both-alternative-branches-all-thresholds");
                Immutable(args[0],printing);checks.Add(printing+":immutable-detached-results-and-inputs");
                EightAndEligibility(catalog,rule);checks.Add(printing+":foreign-slot-eligibility-and-collector-eight");
                Hostile(catalog,rule);checks.Add(printing+":hostile-rng-no-generator-state-leak");
                int packs=int.Parse(args[2]);Require(packs==0 || packs==100000,"Only declared sample sizes admitted");
                Sample sample=null;
                if(packs>0) {sample=Simulate(catalog,rule,args[1],packs,set=="jungle"?0x4a554e47u:0x464f5353u);checks.Add(printing+":100000-complete-diagnostic-packs-all-invariants");}
                results.Add(new SetProof {printingId=printing,passed=true,boundaryCases=boundaryCases,fixedDiagnostics=diagnostics.ToArray(),sample=sample});
            }catch(Exception e) {failed++;Console.Error.WriteLine(e);results.Add(new SetProof {printingId=printing,passed=false,fixedDiagnostics=diagnostics.ToArray()});}
        }
        var proof=new Proof {checks=checks.ToArray(),checkCount=checks.Count,failedCount=failed,sets=results.ToArray()};
        Write(Path.Combine(args[1],"csharp-proof.json"),proof);
        Console.WriteLine("CHECKS "+proof.checkCount+" FAILED "+failed);
        return failed==0?0:1;
    }
}
