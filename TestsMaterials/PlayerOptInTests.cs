using System;
using System.IO;
using VintageCollector.Runtime;
using VintageCollector.Verification;
internal static class PlayerOptInTests {
    private const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
    private static int count;
    private static void Check(string name,bool passed){if(!passed)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
    private static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}}
    public static int Main(string[] args){try{
        string label="materials-policy-"+Guid.NewGuid().ToString("N"),run=Path.Combine(Root,".artifacts","phase4-player",label),session=Path.Combine(run,"session"),folder=Path.Combine(run,"input");Directory.CreateDirectory(folder);
        string data=Path.Combine(Root,"Builds","Phase3MaterialsWindows","VintageCardCollector_Data"),marker=Path.Combine(folder,"materials-fixture.json");
        var normal=GamePaths.Resolve(Root,data,false,new string[0]);Check("normal-play-inert-no-marker-IO",MaterialsSmokeProbe.ValidateOptIn(normal)==null);Check("null-paths-inert",MaterialsSmokeProbe.ValidateOptIn(null)==null);
        var paths=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+session,"--vcc-artifacts="+folder});
        Check("legacy-phase4-smoke-without-marker-inert",MaterialsSmokeProbe.ValidateOptIn(paths)==null);
        foreach(string mode in new[]{"observe","demo"}){File.WriteAllText(marker,"{\"schemaVersion\":1,\"owner\":\"materials-player-fixture\",\"mode\":\""+mode+"\"}");Check("exact-bounded-optin-"+mode,MaterialsSmokeProbe.ValidateOptIn(paths)==mode);}
        foreach(string raw in new[]{"{}","{\"schemaVersion\":1,\"owner\":\"materials-player-fixture\",\"mode\":\"observe\",\"path\":\"C:/outside\"}",new string('x',257),"\ufeff{\"schemaVersion\":1,\"owner\":\"materials-player-fixture\",\"mode\":\"observe\"}"}){File.WriteAllText(marker,raw);Check("malformed-unknown-oversized-bom-refused-"+count,Reject(()=>MaterialsSmokeProbe.ValidateOptIn(paths)));}
        File.WriteAllText(marker,"{\"schemaVersion\":1,\"owner\":\"materials-player-fixture\",\"mode\":\"observe\"}");
        var wrongSave=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+Path.Combine(run,"other"),"--vcc-artifacts="+folder});Check("foreign-fresh-save-root-refused",Reject(()=>MaterialsSmokeProbe.ValidateOptIn(wrongSave)));
        var wrongScenario=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=ready","--vcc-save-root="+session,"--vcc-artifacts="+folder});Check("noninput-scenario-refused",Reject(()=>MaterialsSmokeProbe.ValidateOptIn(wrongScenario)));
        string outside=Path.Combine(Root,".artifacts","materials-build-policy",label);var foreign=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+session,"--vcc-artifacts="+outside});Check("other-scoped-smokes-inert",MaterialsSmokeProbe.ValidateOptIn(foreign)==null);
        Console.WriteLine("TEST_COUNT "+count);return 0;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
