using System;
using System.IO;
using System.Reflection;
using VintageCollector.Runtime;
using VintageCollector.Verification;
public static class RuntimePlayerPureChecks {
 static int count;static Type Probe=typeof(Phase5SmokeProbe);
 static object Invoke(string name,params object[] args){return Probe.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
 static void Check(string n,bool ok){count++;Console.WriteLine((ok?"PASS ":"FAIL ")+n);if(!ok)throw new Exception(n);}
 static bool Refuses(Action f){try{f();return false;}catch(TargetInvocationException e){return e.InnerException is ArgumentException;}}
 public static int Main(string[] args){
  string root=Path.GetFullPath(args[0]),label="pure-policy-"+Guid.NewGuid().ToString("N"),folder=Path.Combine(root,".artifacts","phase5-player",label,"services"),save=Path.Combine(root,".artifacts","phase5-player",label,"session");Directory.CreateDirectory(folder);
  string marker=Path.Combine(folder,"phase5-fixture.json"),text="{\"schemaVersion\":1,\"owner\":\"phase5-player-fixture\",\"mode\":\"services\"}";
  try{
   File.WriteAllText(marker,text);var paths=GamePaths.Resolve(root,Path.Combine(root,"Builds","Phase5Windows","VintageCardCollector_Data"),false,new[]{"--vcc-smoke=input","--vcc-save-root="+save,"--vcc-artifacts="+folder});
   Check("exact-validated-phase5-fixture-admitted",(string)Invoke("ValidateFixture",paths)=="services");
   File.WriteAllText(marker,text+" ");Check("marker-whitespace-refused",Refuses(()=>Invoke("ValidateFixture",paths)));
   File.WriteAllText(marker,new string('x',257));Check("marker-byte-cap-refused",Refuses(()=>Invoke("ValidateFixture",paths)));
   File.WriteAllBytes(marker,new byte[]{239,187,191,123,125});Check("marker-bom-refused",Refuses(()=>Invoke("ValidateFixture",paths)));
   File.WriteAllBytes(marker,new byte[]{0xff});Check("marker-invalid-utf8-refused",Refuses(()=>Invoke("ValidateFixture",paths)));
   File.WriteAllText(marker,text.Replace("services","input"));Check("marker-mode-mismatch-refused",Refuses(()=>Invoke("ValidateFixture",paths)));
   Check("complete-synthetic-png",(bool)Invoke("CompletePng",args[1],2,1));Check("wrong-png-dimensions-refused",!(bool)Invoke("CompletePng",args[1],3,1));
   string partial=Path.Combine(folder,"partial.png");byte[] bytes=File.ReadAllBytes(args[1]);byte[] truncated=new byte[bytes.Length-1];Array.Copy(bytes,truncated,truncated.Length);File.WriteAllBytes(partial,truncated);Check("truncated-iend-refused",!(bool)Invoke("CompletePng",partial,2,1));
   bytes[bytes.Length-1]^=1;File.WriteAllBytes(partial,bytes);Check("bad-crc-refused",!(bool)Invoke("CompletePng",partial,2,1));
  }finally{Directory.Delete(Path.GetDirectoryName(folder),true);}
  Console.WriteLine("CHECKS "+count);return 0;
 }
}
