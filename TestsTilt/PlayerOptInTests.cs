using System;
using System.IO;
using VintageCollector.Runtime;
using VintageCollector.Verification;
internal static class PlayerOptInTests {
    private const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
    private static int count;
    private static string Validate(GamePaths paths){var type=typeof(GamePaths).Assembly.GetType("VintageCollector.Verification.TiltSmokeProbe");if(type==null)throw new Exception("TiltSmokeProbe missing");try{return (string)type.GetMethod("ValidateOptIn").Invoke(null,new object[]{paths});}catch(System.Reflection.TargetInvocationException e){throw e.InnerException;}}
    private static void Check(string name,bool passed){if(!passed)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
    private static bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}}
    public static int Main(string[] args){try{
        string label="tilt-policy-"+Guid.NewGuid().ToString("N"),run=Path.Combine(Root,".artifacts","phase4-player",label),session=Path.Combine(run,"session"),folder=Path.Combine(run,"input");Directory.CreateDirectory(folder);
        string data=Path.Combine(Root,"Builds","Phase3TiltWindows","VintageCardCollector_Data"),marker=Path.Combine(folder,"tilt-fixture.json");
        var normal=GamePaths.Resolve(Root,data,false,new string[0]);Check("normal-play-inert-no-marker-IO",Validate(normal)==null);Check("null-paths-inert",Validate(null)==null);
        var paths=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+session,"--vcc-artifacts="+folder});
        Check("legacy-phase4-smoke-without-marker-inert",Validate(paths)==null);
        foreach(string mode in new[]{"observe","hover"}){File.WriteAllText(marker,"{\"schemaVersion\":1,\"owner\":\"tilt-player-fixture\",\"mode\":\""+mode+"\"}");Check("exact-bounded-optin-"+mode,Validate(paths)==mode);}
        foreach(string raw in new[]{"{}","{\"schemaVersion\":1,\"owner\":\"tilt-player-fixture\",\"mode\":\"observe\",\"path\":\"C:/outside\"}",new string('x',257),"\ufeff{\"schemaVersion\":1,\"owner\":\"tilt-player-fixture\",\"mode\":\"observe\"}"}){File.WriteAllText(marker,raw);Check("malformed-unknown-oversized-bom-refused-"+count,Reject(()=>Validate(paths)));}
        File.WriteAllText(marker,"{\"schemaVersion\":1,\"owner\":\"tilt-player-fixture\",\"mode\":\"observe\"}");
        var wrongSave=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+Path.Combine(run,"other"),"--vcc-artifacts="+folder});Check("foreign-fresh-save-root-refused",Reject(()=>Validate(wrongSave)));
        var wrongScenario=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=ready","--vcc-save-root="+session,"--vcc-artifacts="+folder});Check("noninput-scenario-refused",Reject(()=>Validate(wrongScenario)));
        string outside=Path.Combine(Root,".artifacts","tilt-build-policy",label);var foreign=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+session,"--vcc-artifacts="+outside});Check("other-scoped-smokes-inert",Validate(foreign)==null);
        var probe=typeof(GamePaths).Assembly.GetType("VintageCollector.Verification.TiltSmokeProbe");var request=probe.GetMethod("ValidateRequest");Check("bounded-request-parser-present",request!=null);
        Func<string,int,string> parse=(raw,last)=>{try{return (string)request.Invoke(null,new object[]{raw,last});}catch(System.Reflection.TargetInvocationException e){throw e.InnerException;}};
        Check("capture-request-exact-next-sequence",parse("{\"schemaVersion\":1,\"sequence\":1,\"action\":\"capture\"}",0)=="capture");
        foreach(string raw in new[]{"{}","{\"schemaVersion\":1,\"sequence\":0,\"action\":\"capture\"}","{\"schemaVersion\":1,\"sequence\":2,\"action\":\"capture\"}","{\"schemaVersion\":1,\"sequence\":1,\"action\":\"pointer:0,0\"}",new string('x',257)})Check("request-rejects-invalid-replay-gap-input-"+count,Reject(()=>parse(raw,0)));
        Check("hover-probe-has-no-pointer-injection-API",probe.GetMethod("SetPointer")==null);
        Check("actual-hover-snapshot-coroutine-present",probe.GetMethod("ObserveHover",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!=null);
        Console.WriteLine("TEST_COUNT "+count);return 0;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}