using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using VintageCollector.Domain;
using VintageCollector.Persistence;

public sealed class PersistenceJson : IStateSerializer
{
    public SessionData Last;
    public string Serialize(SessionData d) { Last = d; return Json(d); }
    public SessionData Deserialize(string s) { Last = Parse<SessionData>(s); return Last; }
    public static string Json<T>(T d) { using(var m = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(m,d); return Encoding.UTF8.GetString(m.ToArray()); } }
    public static T Parse<T>(string s) { using(var m = new MemoryStream(Encoding.UTF8.GetBytes(s))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(m); }
}
public sealed class PersistencePoisonJson : IStateSerializer
{
    public Action<SessionData> Change; public bool OnWrite;
    public string Serialize(SessionData d) { if(OnWrite) Change(d); return PersistenceJson.Json(d); }
    public SessionData Deserialize(string s) { var d=PersistenceJson.Parse<SessionData>(s); if(!OnWrite) Change(d); return d; }
}
public sealed class PersistenceFault : ISaveFaultInjector
{
    public string Stage; public bool Fired, Crash;
    public void At(string stage) { if(stage==Stage && !Fired) { Fired=true; if(Crash) Environment.Exit(73); throw new IOException("Injected " + stage); } }
}
[Serializable] public sealed class PersistenceCheck { public string name, error; public bool passed; }
[Serializable] public sealed class PersistenceReport { public int testCount; public bool passed; public PersistenceCheck[] checks; }
public static class PersistenceTests
{
    static string root, output;
    static Catalog catalog;
    static PackGenerator generator;
    const string Time = "2026-10-30T12:34:56.0000000Z";
    static void Assert(bool b, string message) { if(!b) throw new Exception(message); }
    static string Fixture(string name) { string d = Path.Combine(output, "fixtures", name + "-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(d); return d; }
    static PackResult Pack(uint seed) { return generator.Generate(new SeededRandom(seed)); }
    static SessionStore Store(string d, ISaveFaultInjector f = null, PersistenceJson j = null) { return new SessionStore(d,j ?? new PersistenceJson(),catalog,f); }
    static void FirstGeneration()
    {
        string d = Fixture("first"); var p = Pack(1);
        using(var s = Store(d)) {
            Assert(s.Snapshot.CurrentPack == null && s.Snapshot.TotalVirtualCards == 0, "Startup generated a pack");
            var saved = s.OpenPack("tx-one",Time,1,p);
            Assert(saved.TransactionId == "tx-one" && saved.Seed == 1 && saved.CreatedUtc == Time, "Identity lost");
            Assert(saved.OrderedCardIds.Count == 11 && !saved.WrapperOpened && saved.RevealedCount == 0, "Not sealed");
            Assert(s.Snapshot.Revision == 1 && s.Snapshot.TotalVirtualCards == 11, "Awards not committed with generation");
        }
        using(var s = Store(d)) {
            var saved = s.Snapshot.CurrentPack;
            Assert(saved != null && saved.TransactionId == "tx-one" && saved.OrderedCardIds.Count == 11, "Missing restart");
            for(int i=0;i<11;i++) Assert(saved.OrderedCardIds[i] == p.OrderedCardIds[i],"Rerolled");
            Assert(saved.ModelId == p.ModelId && saved.ModelVersion == p.ModelVersion && saved.CatalogVersion == p.CatalogVersion && saved.PrintingId == p.PrintingId && saved.Status == p.Status && saved.RngAlgorithm == p.RngAlgorithm,"Metadata lost");
            Assert(s.Snapshot.TotalVirtualCards == 11 && s.Reload().Revision == 1,"Restart awards duplicated");
        }
    }
    static void Throws<T>(Action a) where T:Exception { try { a(); } catch(T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    static void Progress()
    {
        string d=Fixture("progress"); var p=Pack(4);
        using(var s=Store(d)) {
            s.OpenPack("tx",Time,4,p);
            Assert(s.OpenPack("tx",Time,4,p).TransactionId=="tx" && s.Snapshot.Revision==1 && s.Snapshot.TotalVirtualCards==11,"Generation replay awards again");
            Throws<InvalidOperationException>(()=>s.OpenPack("other",Time,4,p));
            Throws<ArgumentException>(()=>s.OpenPack("tx",Time,5,p));
            Throws<ArgumentException>(()=>s.OpenPack("tx",Time,4,Pack(1)));
            Throws<ArgumentException>(()=>s.OpenPack("tx","2026-10-31T12:34:56.0000000Z",4,p));
            Throws<ArgumentException>(()=>s.RevealTo("tx",-1)); Throws<ArgumentException>(()=>s.RevealTo("tx",12));
            Throws<InvalidOperationException>(()=>s.RevealTo("tx",1));
            Assert(s.RevealTo("tx",0).RevealedCount==0 && s.Snapshot.Revision==1,"Zero reveal writes");
            s.OpenWrapper("tx"); s.OpenWrapper("tx"); Assert(s.Snapshot.Revision==2,"Repeated wrapper writes");
            for(int i=1;i<=5;i++) { s.RevealTo("tx",i); s.RevealTo("tx",i); s.RevealTo("tx",0); Assert(s.Snapshot.Revision==i+2,"Nonabsolute reveal"); }
        }
        using(var s=Store(d)) {
            Assert(s.Snapshot.CurrentPack.RevealedCount==5 && s.Snapshot.CurrentPack.WrapperOpened,"Mid-reveal restart");
            s.RevealTo("tx",11); long revision=s.Snapshot.Revision;
            for(int i=0;i<20;i++) { s.RevealTo("tx",11); s.OpenWrapper("tx"); s.OpenPack("tx",Time,4,p); }
            Assert(s.Snapshot.Revision==revision && s.Snapshot.TotalVirtualCards==11 && s.Snapshot.CurrentPack.IsComplete,"Completion awards again");
            s.OpenPack("second",Time,4,p); Assert(s.Snapshot.TotalVirtualCards==22,"Second pack not awarded");
            foreach(bool flag in s.Snapshot.CurrentPack.NewToVirtual) Assert(!flag,"Existing card marked new");
            s.RevealTo("tx",11); Assert(s.Snapshot.CurrentPack.TransactionId=="second","Old retry replaced latest");
            Throws<KeyNotFoundException>(()=>s.OpenWrapper("unknown"));
        }
    }
    static void Validation()
    {
        string d=Fixture("validation"); using(var s=Store(d)) { s.OpenPack("tx",Time,1,Pack(1)); }
        byte[] original=File.ReadAllBytes(Path.Combine(d,"state.save"));
        var changes=new Dictionary<string,Action<SessionData>> {
            {"unsupported-schema", x=>x.schemaVersion=2}, {"negative-revision",x=>x.revision=-1}, {"unearned-revision",x=>x.revision=2},
            {"missing-archive",x=>x.packs=null}, {"null-pack",x=>x.packs[0]=null}, {"blank-transaction",x=>x.packs[0].transactionId=" "},
            {"invalid-time",x=>x.packs[0].createdUtc="not-utc"}, {"zero-seed",x=>x.packs[0].seed=0},
            {"wrong-model",x=>x.packs[0].modelId="unknown"}, {"wrong-version",x=>x.packs[0].modelVersion="99.0.0"},
            {"wrong-catalog",x=>x.packs[0].catalogVersion="unknown"}, {"wrong-printing",x=>x.packs[0].printingId="other"},
            {"wrong-rng",x=>x.packs[0].rngAlgorithm="random"}, {"wrong-status",x=>x.packs[0].status="historically-exact"},
            {"missing-cards",x=>x.packs[0].orderedCardIds=null}, {"short-pack",x=>Array.Resize(ref x.packs[0].orderedCardIds,10)},
            {"missing-flags",x=>x.packs[0].newToVirtual=null}, {"short-flags",x=>Array.Resize(ref x.packs[0].newToVirtual,10)},
            {"incorrect-new",x=>x.packs[0].newToVirtual[0]=false}, {"unknown-card",x=>x.packs[0].orderedCardIds[0]="unknown"},
            {"machamp-never-booster",x=>x.packs[0].orderedCardIds[0]="pkmn-base-en-unlimited1999-008-holo-standard"},
            {"wrong-slot",x=>{var a=x.packs[0].orderedCardIds; var t=a[0]; a[0]=a[5]; a[5]=t;}},
            {"negative-reveal",x=>x.packs[0].revealedCount=-1}, {"excess-reveal",x=>x.packs[0].revealedCount=12},
            {"closed-reveal",x=>x.packs[0].revealedCount=1},
            {"duplicate-transaction",x=>{x.revision=2; x.packs=new[]{x.packs[0],x.packs[0]};}},
            {"incomplete-archive-prefix",x=>{x.revision=2; var b=PersistenceJson.Parse<PackRecordData>(PersistenceJson.Json(x.packs[0])); b.transactionId="other"; x.packs=new[]{x.packs[0],b};}}
        };
        foreach(var change in changes) {
            bool rejected=false;
            try { using(var s=new SessionStore(d,new PersistencePoisonJson {Change=change.Value},catalog)) { } } catch(InvalidDataException) { rejected=true; }
            Assert(rejected,"Accepted invalid save: "+change.Key);
            Assert(Convert.ToBase64String(original)==Convert.ToBase64String(File.ReadAllBytes(Path.Combine(d,"state.save"))),"Changed invalid save: "+change.Key);
        }
        using(var s=Store(Fixture("bad-input"))) {
            Throws<ArgumentException>(()=>s.OpenPack(" ",Time,1,Pack(1))); Throws<ArgumentException>(()=>s.OpenPack("tx","yesterday",1,Pack(1)));
            Throws<ArgumentException>(()=>s.OpenPack("tx",Time,0,Pack(1))); Throws<ArgumentNullException>(()=>s.OpenPack("tx",Time,1,null));
            Assert(s.Snapshot.Revision==0,"Invalid input committed");
        }
    }
    static readonly string[] Stages={"before-temp-write","after-temp-write","after-temp-flush","before-prepare","after-prepare","before-replace","after-replace","before-backup","after-backup-flush","after-backup-replace","before-anchor","after-anchor-flush","after-anchor-replace","before-pending-delete","after-pending-delete","before-ack"};
    static void FaultGeneration(string stage)
    {
        string d=Fixture("fault-"+stage); var f=new PersistenceFault {Stage=stage};
        bool prepared=Array.IndexOf(Stages,stage)>=Array.IndexOf(Stages,"after-prepare");
        using(var s=Store(d,f)) {
            Throws<IOException>(()=>s.OpenPack("tx",Time,1,Pack(1))); Assert(f.Fired,"Stage never exercised: "+stage);
            Assert(s.Snapshot.TotalVirtualCards==(prepared?11:0),"Snapshot does not reflect recovered durability at "+stage);
            Assert(s.Reload().TotalVirtualCards==(prepared?11:0),"Reload differs at "+stage);
            s.OpenPack("tx",Time,1,Pack(1)); Assert(s.Snapshot.TotalVirtualCards==11 && s.Snapshot.Revision==1,"Retry double award at "+stage);
        }
        using(var s=Store(d)) Assert(s.Snapshot.TotalVirtualCards==11 && s.Snapshot.Revision==1,"Reopen differs at "+stage);
        Assert(File.Exists(Path.Combine(d,"state.backup")),"No checksummed backup");
    }
    static void LockLifetime()
    {
        string d=Fixture("lock"); var s=Store(d);
        try { Throws<IOException>(()=>{ using(var other=Store(d)) {} }); s.OpenPack("tx",Time,1,Pack(1)); }
        finally { s.Dispose(); }
        s.Dispose();
        Throws<ObjectDisposedException>(()=>{ var ignored=s.Snapshot; }); Throws<ObjectDisposedException>(()=>s.Reload());
        Throws<ObjectDisposedException>(()=>s.OpenPack("tx",Time,1,Pack(1))); Throws<ObjectDisposedException>(()=>s.OpenWrapper("tx")); Throws<ObjectDisposedException>(()=>s.RevealTo("tx",1));
        using(var other=Store(d)) Assert(other.Snapshot.TotalVirtualCards==11,"Dispose lost data or lock");
        string bad=Fixture("lock-bad"); File.WriteAllText(Path.Combine(bad,"state.save"),"broken");
        for(int i=0;i<2;i++) Throws<InvalidDataException>(()=>{ using(var other=Store(bad)) {} });
    }
    static void MissingAcknowledgedData()
    {
        string d=Fixture("missing-acknowledged");
        using(var s=Store(d)) { s.OpenPack("tx",Time,1,Pack(1)); }
        string stale=File.ReadAllText(Path.Combine(d,"state.backup"));
        using(var s=Store(d)) { s.OpenWrapper("tx"); s.RevealTo("tx",11); s.OpenPack("second",Time,4,Pack(4)); }
        File.WriteAllText(Path.Combine(d,"state.save"),"corrupt primary"); File.WriteAllText(Path.Combine(d,"state.backup"),stale);
        Throws<InvalidDataException>(()=>{using(var s=Store(d)) {}});
        string empty=Fixture("deleted-data"); using(var s=Store(empty)) {s.OpenPack("tx",Time,1,Pack(1));}
        File.Delete(Path.Combine(empty,"state.save")); File.Delete(Path.Combine(empty,"state.backup"));
        Throws<InvalidDataException>(()=>{using(var s=Store(empty)) {}});
    }
    static void DuplicateEnergyAndMutationSafety()
    {
        uint seed=1; PackResult p;
        do { p=Pack(seed++); } while(p.OrderedCardIds[5]!=p.OrderedCardIds[6] && seed<10000);
        Assert(p.OrderedCardIds[5]==p.OrderedCardIds[6],"No repeated Energy fixture"); seed--;
        string d=Fixture("mutation"); var j=new PersistenceJson();
        using(var s=Store(d,null,j)) {
            var original=s.Snapshot; var pack=s.OpenPack("tx",Time,seed,p); var snapshot=s.Snapshot;
            Assert(pack.NewToVirtual[5] && !pack.NewToVirtual[6],"Repeated Energy has dishonest new flags");
            int quantity=0,sum=0; foreach(var h in snapshot.VirtualHoldings) {sum+=h.Quantity; if(h.CardId==p.OrderedCardIds[5]) quantity=h.Quantity;}
            Assert(quantity==2 && sum==11,"Wrong virtual quantities");
            Throws<NotSupportedException>(()=>((IList<string>)pack.OrderedCardIds)[0]="changed"); Throws<NotSupportedException>(()=>((IList<bool>)pack.NewToVirtual)[0]=false);
            Throws<NotSupportedException>(()=>((IList<PackSnapshot>)snapshot.Packs).Clear()); Throws<NotSupportedException>(()=>((IList<VirtualHolding>)snapshot.VirtualHoldings).Clear());
            j.Last.packs[0].orderedCardIds[0]="changed"; j.Last.packs[0].newToVirtual[0]=false; j.Last.revision=999;
            Assert(s.Snapshot.CurrentPack.OrderedCardIds[0]==p.OrderedCardIds[0] && s.Snapshot.Revision==1,"Serializer alias leaked");
            s.OpenWrapper("tx"); s.RevealTo("tx",11);
            Assert(!pack.WrapperOpened && pack.RevealedCount==0 && snapshot.Revision==1 && original.Packs.Count==0,"Old snapshot mutated");
            s.Reload(); j.Last.packs[0].transactionId="changed";
            Assert(s.Snapshot.CurrentPack.TransactionId=="tx","Deserializer alias leaked");
        }
        using(var s=new SessionStore(Fixture("mutating-serializer"),new PersistencePoisonJson {OnWrite=true,Change=x=>x.packs[0].seed=99},catalog)) {
            Throws<InvalidDataException>(()=>s.OpenPack("tx",Time,1,Pack(1))); Assert(s.Snapshot.TotalVirtualCards==0,"Serializer silently changed identity");
        }
    }
    static void FaultProgress(string stage,bool wrapper)
    {
        string d=Fixture((wrapper?"wrapper-":"reveal-")+stage); using(var s=Store(d)) {s.OpenPack("tx",Time,1,Pack(1)); if(!wrapper) {s.OpenWrapper("tx"); s.RevealTo("tx",3);}}
        var f=new PersistenceFault {Stage=stage}; bool prepared=Array.IndexOf(Stages,stage)>=Array.IndexOf(Stages,"after-prepare");
        using(var s=Store(d,f)) {
            long before=s.Snapshot.Revision;
            Throws<IOException>(()=>{if(wrapper) s.OpenWrapper("tx"); else s.RevealTo("tx",11);}); Assert(f.Fired,"Missing progress stage");
            var recovered=s.Snapshot; Assert(recovered.TotalVirtualCards==11,"Failure changed awards");
            Assert(wrapper ? recovered.CurrentPack.WrapperOpened==prepared : recovered.CurrentPack.RevealedCount==(prepared?11:3),"Uncertain progress not recovered");
            if(wrapper) s.OpenWrapper("tx"); else s.RevealTo("tx",11);
            Assert(s.Snapshot.Revision==before+1 && s.Snapshot.TotalVirtualCards==11,"Retry advanced/awarded twice");
        }
        using(var s=Store(d)) Assert(s.Snapshot.TotalVirtualCards==11 && (wrapper?s.Snapshot.CurrentPack.WrapperOpened:s.Snapshot.CurrentPack.IsComplete),"Progress restart failed");
    }
    static string Wire(string payload)
    {
        using(var sha=SHA256.Create()) return "VTCG-SAVE-1\n"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant()+"\n"+payload;
    }
    static void CorruptionRecovery()
    {
        foreach(string target in new[]{"state.save","state.backup"}) {
            string d=Fixture("recover-"+target); using(var s=Store(d)) {s.OpenPack("tx",Time,1,Pack(1));s.OpenWrapper("tx");s.RevealTo("tx",5);}
            byte[] corrupt=Encoding.UTF8.GetBytes("broken checksum and envelope"); File.WriteAllBytes(Path.Combine(d,target),corrupt);
            using(var s=Store(d)) { Assert(s.Snapshot.CurrentPack.RevealedCount==5 && s.Snapshot.TotalVirtualCards==11,"Backup lost latest progress"); Assert(!string.IsNullOrEmpty(s.Snapshot.RecoveryNotice),"Silent recovery"); }
            string[] preserved=Directory.GetFiles(d,target+".corrupt-*"); Assert(preserved.Length==1 && Convert.ToBase64String(File.ReadAllBytes(preserved[0]))==Convert.ToBase64String(corrupt),"Corrupt evidence lost");
            Assert(File.ReadAllText(Path.Combine(d,"state.save"))==File.ReadAllText(Path.Combine(d,"state.backup")),"Recovery copies differ");
        }
        foreach(string target in new[]{"state.save","state.backup","state.pending"}) {
            string d=Fixture("schema-"+target); using(var s=Store(d)) s.OpenPack("tx",Time,1,Pack(1));
            string wire=File.ReadAllText(Path.Combine(d,"state.save")); string payload=wire.Substring(wire.IndexOf('\n')+66);
            var data=PersistenceJson.Parse<SessionData>(payload); data.schemaVersion=200;
            File.WriteAllText(Path.Combine(d,target),Wire(PersistenceJson.Json(data)));
            var before=new Dictionary<string,string>(); foreach(string f in Directory.GetFiles(d)) before[f]=Convert.ToBase64String(File.ReadAllBytes(f));
            Throws<InvalidDataException>(()=>{using(var s=Store(d)) {}});
            foreach(var f in before) Assert(Convert.ToBase64String(File.ReadAllBytes(f.Key))==f.Value,"Unsupported schema bytes changed");
        }
        foreach(string mode in new[]{"both","pending","anchor","malformed-payload","conflicting-pending"}) {
            string d=Fixture("corrupt-"+mode); using(var s=Store(d)) s.OpenPack("tx",Time,1,Pack(1));
            if(mode=="both") {File.WriteAllText(Path.Combine(d,"state.save"),"bad"); File.WriteAllText(Path.Combine(d,"state.backup"),"bad too");}
            if(mode=="pending") File.WriteAllText(Path.Combine(d,"state.pending"),"incomplete prepared data");
            if(mode=="anchor") File.WriteAllText(Path.Combine(d,"state.anchor"),"bad anchor");
            if(mode=="malformed-payload") File.WriteAllText(Path.Combine(d,"state.save"),Wire("{broken json"));
            if(mode=="conflicting-pending") {string w=File.ReadAllText(Path.Combine(d,"state.save"));var x=PersistenceJson.Parse<SessionData>(w.Substring(w.IndexOf('\n')+66));x.packs[0].seed=99;File.WriteAllText(Path.Combine(d,"state.pending"),Wire(PersistenceJson.Json(x)));}
            var before=new Dictionary<string,string>(); foreach(string f in Directory.GetFiles(d)) before[f]=Convert.ToBase64String(File.ReadAllBytes(f));
            Throws<InvalidDataException>(()=>{using(var s=Store(d)) {}});
            foreach(var f in before) Assert(Convert.ToBase64String(File.ReadAllBytes(f.Key))==f.Value,"Corrupt bytes changed: "+mode);
        }
    }
    static void RealFilesystemFailures()
    {
        string d=Fixture("real-io"); using(var s=Store(d)) {
            s.OpenPack("tx",Time,1,Pack(1)); var before=s.Snapshot;
            using(var block=new FileStream(Path.Combine(d,"state.writing"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
                Throws<IOException>(()=>s.OpenWrapper("tx")); Assert(!s.Snapshot.CurrentPack.WrapperOpened,"Failed disk write advanced wrapper");
            }
            s.OpenWrapper("tx");
            using(var block=new FileStream(Path.Combine(d,"state.save"),FileMode.Open,FileAccess.Read,FileShare.Read)) {
                Throws<IOException>(()=>s.RevealTo("tx",11));
                Throws<IOException>(()=>{var ignored=s.Snapshot;});
            }
            Assert(s.Reload().CurrentPack.IsComplete && s.Snapshot.TotalVirtualCards==11,"Blocked atomic replace did not roll forward");
            s.RevealTo("tx",11); Assert(s.Snapshot.Revision==3 && !before.CurrentPack.WrapperOpened,"Retry changed revision/snapshot");
        }
    }
    static void LiveMissingState()
    {
        string d=Fixture("live-missing"); using(var s=Store(d)) {
            s.OpenPack("tx",Time,1,Pack(1));
            File.Delete(Path.Combine(d,"state.save"));File.Delete(Path.Combine(d,"state.backup"));File.Delete(Path.Combine(d,"state.anchor"));
            Throws<InvalidDataException>(()=>s.Reload()); Throws<InvalidDataException>(()=>{var ignored=s.Snapshot;});
        }
    }
    static string Quote(string value) { return "\""+value.Replace("\"","\\\"")+"\""; }
    static Process Child(string mode,string d,string stage="",string action="")
    {
        string mono="C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge/bin/mono.exe";
        var start=new ProcessStartInfo(mono,Quote(typeof(PersistenceTests).Assembly.Location)+" "+Quote(root)+" "+Quote(output)+" "+Quote(mode)+" "+Quote(d)+" "+Quote(stage)+" "+Quote(action));
        start.UseShellExecute=false;start.RedirectStandardOutput=true;start.RedirectStandardError=true;start.RedirectStandardInput=true;start.CreateNoWindow=true;
        return Process.Start(start);
    }
    static string Finish(Process p,string d,int expected)
    {
        if(!p.WaitForExit(20000)) {p.Kill();p.WaitForExit();throw new Exception("Child timeout");}
        string stdout=p.StandardOutput.ReadToEnd(),stderr=p.StandardError.ReadToEnd();
        File.AppendAllText(Path.Combine(d,"process.log"),"EXIT "+p.ExitCode+" EXPECTED "+expected+"\n"+stdout+stderr+"\n");
        Assert(p.ExitCode==expected,"Child exit "+p.ExitCode+": "+stderr+stdout); return stdout;
    }
    static void ProcessLock()
    {
        string d=Fixture("process-lock");
        using(var s=Store(d)) using(var p=Child("child-probe",d)) Assert(Finish(p,d,23).Contains("DENIED"),"Second process not denied");
        using(var p=Child("child-hold",d)) {
            try { var line=p.StandardOutput.ReadLineAsync();Assert(line.Wait(15000) && line.Result=="LOCKED","Child failed to acquire lock"); using(var probe=Child("child-probe",d)) Finish(probe,d,23); p.Kill(); Assert(p.WaitForExit(15000),"Killed lock holder did not exit"); }
            finally { if(!p.HasExited) {p.Kill();p.WaitForExit();} }
        }
        using(var p=Child("child-probe",d)) Assert(Finish(p,d,0).Contains("OPEN"),"Crash did not release OS lock");
        using(var s=Store(d)) Assert(s.Snapshot.TotalVirtualCards==0,"Lock probe generated pack");
    }
    static void CrashRecovery(string stage,string action)
    {
        string d=Fixture("crash-"+action+"-"+stage); long prior=0;
        if(action!="generation") using(var s=Store(d)) {s.OpenPack("tx",Time,1,Pack(1));if(action=="reveal") {s.OpenWrapper("tx");s.RevealTo("tx",3);}prior=s.Snapshot.Revision;}
        using(var p=Child("child-crash",d,stage,action)) Finish(p,d,73);
        bool prepared=Array.IndexOf(Stages,stage)>=Array.IndexOf(Stages,"after-prepare");
        using(var s=Store(d)) {
            var snap=s.Snapshot;
            Assert(snap.Revision==prior+(prepared?1:0),"Crash revision mismatch");
            if(action=="generation") Assert(snap.TotalVirtualCards==(prepared?11:0),"Crash generation visibility");
            if(action=="wrapper") Assert(snap.CurrentPack.WrapperOpened==prepared && snap.TotalVirtualCards==11,"Crash wrapper visibility");
            if(action=="reveal") Assert(snap.CurrentPack.RevealedCount==(prepared?11:3) && snap.TotalVirtualCards==11,"Crash reveal visibility");
            if(action=="generation") s.OpenPack("tx",Time,1,Pack(1)); else if(action=="wrapper") s.OpenWrapper("tx"); else s.RevealTo("tx",11);
            Assert(s.Snapshot.Revision==prior+1 && s.Snapshot.TotalVirtualCards==11 && s.Snapshot.CurrentPack.Seed==1,"Crash retry changed identity/awards/revision");
        }
    }
    static void UnsupportedEnvelopeAndBlankPreservation()
    {
        foreach(string mode in new[]{"envelope","blank"}) {
            string d=Fixture("unsupported-"+mode); using(var s=Store(d)) s.OpenPack("tx",Time,1,Pack(1));
            if(mode=="envelope") {string wire=File.ReadAllText(Path.Combine(d,"state.save"));File.WriteAllText(Path.Combine(d,"state.save"),wire.Replace("VTCG-SAVE-1","VTCG-SAVE-99"));}
            else {File.Delete(Path.Combine(d,"state.anchor"));string wire=Wire(PersistenceJson.Json(new SessionData {schemaVersion=1,revision=0,packs=new PackRecordData[0]}));File.WriteAllText(Path.Combine(d,"state.save"),wire);File.WriteAllText(Path.Combine(d,"state.backup"),wire);}
            var before=new Dictionary<string,string>(); foreach(string f in Directory.GetFiles(d)) before[f]=Convert.ToBase64String(File.ReadAllBytes(f));
            Throws<InvalidDataException>(()=>{using(var s=Store(d)) {}});
            foreach(var f in before) Assert(Convert.ToBase64String(File.ReadAllBytes(f.Key))==f.Value,"Unsupported/blank save changed existing bytes");
            Assert(Directory.GetFiles(d).Length==before.Count,"Unsupported/blank save created data files");
        }
    }
    static void EveryRevealBoundary()
    {
        string d=Fixture("every-reveal"); var p=Pack(1);
        using(var s=Store(d)) {s.OpenPack("tx",Time,1,p);s.OpenWrapper("tx");}
        for(int target=0;target<=11;target++) {
            using(var s=Store(d)) {s.RevealTo("tx",target);Assert(s.Snapshot.CurrentPack.RevealedCount==target,"Lost boundary"); Assert(s.Snapshot.TotalVirtualCards==11 && s.Snapshot.Revision==target+2,"Boundary awarded again");}
            using(var s=Store(d)) {Assert(s.Snapshot.CurrentPack.RevealedCount==target && s.Snapshot.CurrentPack.IsComplete==(target==11),"Boundary restart"); for(int i=0;i<11;i++) Assert(s.Snapshot.CurrentPack.OrderedCardIds[i]==p.OrderedCardIds[i],"Boundary reordered");}
        }
        string huge=Fixture("oversize"); using(var f=new FileStream(Path.Combine(huge,"state.save"),FileMode.Create)) f.SetLength(32L*1024*1024+1);
        Throws<InvalidDataException>(()=>{using(var s=Store(huge)) {}}); Assert(new FileInfo(Path.Combine(huge,"state.save")).Length==32L*1024*1024+1,"Oversize save changed");
    }
    static void RelativeDirectoryPinned()
    {
        string cwd=Environment.CurrentDirectory,home=Fixture("relative-home"),other=Fixture("relative-other");
        try {
            Environment.CurrentDirectory=home;
            using(var s=Store("save")) {Environment.CurrentDirectory=other;s.OpenPack("tx",Time,1,Pack(1));Assert(s.Reload().TotalVirtualCards==11,"Relative store changed directory");}
            Assert(File.Exists(Path.Combine(home,"save/state.save")) && !Directory.Exists(Path.Combine(other,"save")),"Store followed process cwd");
        } finally {Environment.CurrentDirectory=cwd;}
    }
    static void OlderAnchorConflict()
    {
        string a=Fixture("anchor-history-a"),b=Fixture("anchor-history-b");
        using(var s=Store(a)) s.OpenPack("tx-a",Time,1,Pack(1));
        string foreignAnchor=File.ReadAllText(Path.Combine(a,"state.anchor")),older,olderAnchor,newer;
        using(var s=Store(b)) {
            s.OpenPack("tx-b",Time,4,Pack(4));
            older=File.ReadAllText(Path.Combine(b,"state.save"));
            olderAnchor=File.ReadAllText(Path.Combine(b,"state.anchor"));
            s.OpenWrapper("tx-b");
            newer=File.ReadAllText(Path.Combine(b,"state.save"));
        }
        foreach(string name in new[]{"state.save","state.pending"}) {
            string d=Fixture("conflicting-old-anchor");
            File.WriteAllText(Path.Combine(d,"state.save"),older);
            File.WriteAllText(Path.Combine(d,"state.backup"),older);
            File.WriteAllText(Path.Combine(d,name),newer);
            File.WriteAllText(Path.Combine(d,"state.anchor"),foreignAnchor);
            var before=new Dictionary<string,string>();
            foreach(string path in Directory.GetFiles(d)) before[path]=Convert.ToBase64String(File.ReadAllBytes(path));
            Throws<InvalidDataException>(()=>{using(var s=Store(d)) {}});
            foreach(var file in before) Assert(Convert.ToBase64String(File.ReadAllBytes(file.Key))==file.Value,"Conflicting older acknowledgement was modified");
        }
        string valid=Fixture("compatible-old-anchor");
        File.WriteAllText(Path.Combine(valid,"state.save"),newer);
        File.WriteAllText(Path.Combine(valid,"state.backup"),older);
        File.WriteAllText(Path.Combine(valid,"state.anchor"),olderAnchor);
        using(var s=Store(valid)) Assert(s.Snapshot.Revision==2 && s.Snapshot.CurrentPack.WrapperOpened,"Valid prepared roll-forward was blocked");
    }
    public static int Main(string[] args)
    {
        root = args[0]; output = args[1];
        catalog = Catalog.FromData(PersistenceJson.Parse<CatalogData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"))));
        generator = new PackGenerator(catalog,PersistenceJson.Parse<RuleData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json"))));
        if(args.Length>2 && args[2].StartsWith("child-",StringComparison.Ordinal)) {
            string mode=args[2],d=args[3];
            if(mode=="child-probe") {try {using(var s=Store(d)) {Console.WriteLine("OPEN");return 0;}} catch(IOException) {Console.WriteLine("DENIED");return 23;}}
            if(mode=="child-hold") {using(var s=Store(d)) {Console.WriteLine("LOCKED");Console.Out.Flush();Console.ReadLine();return 0;}}
            if(mode=="child-crash") {using(var s=Store(d,new PersistenceFault {Stage=args[4],Crash=true})) {if(args[5]=="generation") s.OpenPack("tx",Time,1,Pack(1));else if(args[5]=="wrapper") s.OpenWrapper("tx");else s.RevealTo("tx",11);}return 74;}
            return 75;
        }
        var tests = new Dictionary<string,Action> { {"first-generation-durable-restart",FirstGeneration}, {"absolute-progress-retry-exactly-once",Progress}, {"schema-archive-metadata-new-flags-validation",Validation}, {"exclusive-lock-dispose-failed-constructor",LockLifetime}, {"missing-acknowledged-state-never-rolls-back",MissingAcknowledgedData}, {"duplicate-energy-immutable-snapshots-serializer-safety",DuplicateEnergyAndMutationSafety}, {"corruption-backup-unsupported-schema-byte-preservation",CorruptionRecovery}, {"real-filesystem-sharing-failures-block-advance",RealFilesystemFailures}, {"live-missing-state-fails-closed",LiveMissingState}, {"multiprocess-lock-denial-kill-release",ProcessLock}, {"unsupported-envelope-blank-save-preserved",UnsupportedEnvelopeAndBlankPreservation}, {"every-reveal-boundary-restart-oversize-rejection",EveryRevealBoundary}, {"relative-directory-pinned-for-lock-and-save",RelativeDirectoryPinned} };
        tests.Add("older-anchor-conflict-not-hidden-by-newer-copy",OlderAnchorConflict);
        foreach(string stage in Stages) { string captured=stage; tests.Add("generation-fault-"+captured,()=>FaultGeneration(captured)); tests.Add("wrapper-fault-"+captured,()=>FaultProgress(captured,true)); tests.Add("reveal-fault-"+captured,()=>FaultProgress(captured,false)); }
        foreach(string stage in Stages) foreach(string action in new[]{"generation","wrapper","reveal"}) {string st=stage,ac=action;tests.Add("process-crash-"+ac+"-"+st,()=>CrashRecovery(st,ac));}
        var checks = new List<PersistenceCheck>(); bool passed = true;
        foreach(var t in tests) { if(args.Length>2 && args[2].Length>0 && !t.Key.Contains(args[2])) continue; var c=new PersistenceCheck {name=t.Key}; try { t.Value(); c.passed=true; } catch(Exception e) { c.error=e.ToString(); passed=false; } checks.Add(c); }
        Console.WriteLine(PersistenceJson.Json(new PersistenceReport { testCount=checks.Count,passed=passed,checks=checks.ToArray() }));
        return passed && checks.Count>0 ? 0 : 1;
    }
}
