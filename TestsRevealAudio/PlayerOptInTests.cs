using System;
using System.IO;
using System.Reflection;
using VintageCollector.Runtime;
public static class PlayerOptInTests {
 const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";static int count;
 static void Check(string n,bool ok){if(!ok)throw new Exception(n);Console.WriteLine("PASS "+n);count++;}
 static string Validate(GamePaths paths){var t=typeof(GamePaths).Assembly.GetType("VintageCollector.Verification.RevealAudioSmokeProbe");if(t==null)throw new Exception("RevealAudioSmokeProbe opt-in missing");try{return (string)t.GetMethod("ValidateOptIn").Invoke(null,new object[]{paths});}catch(TargetInvocationException e){throw e.InnerException;}}
 static bool Refused(Action f){try{f();return false;}catch(ArgumentException){return true;}}
 public static int Main(){try{
  var run=Path.Combine(Root,".artifacts","reveal-audio-player","optin-fixture");var folder=Path.Combine(run,"audio");Directory.CreateDirectory(folder);var marker=Path.Combine(folder,"reveal-audio-fixture.json");
  var data=Path.Combine(Root,"Builds","Phase3RevealAudioWindows","VintageCardCollector_Data");
  Check("null opt-in inert",Validate(null)==null);
  Check("normal startup no marker IO",Validate(GamePaths.Resolve(Root,data,false,new string[0]))==null);
  var paths=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+Path.Combine(run,"session"),"--vcc-artifacts="+folder});
  Check("no marker inert",Validate(paths)==null);
  File.WriteAllText(marker,"{\"schemaVersion\":1,\"owner\":\"reveal-audio-player-fixture\",\"mode\":\"audio\"}");Check("exact scoped fixture",Validate(paths)=="audio");
  foreach(var raw in new[]{"{}",new string('x',257),"\ufeff{\"schemaVersion\":1,\"owner\":\"reveal-audio-player-fixture\",\"mode\":\"audio\"}","{\"schemaVersion\":1,\"owner\":\"reveal-audio-player-fixture\",\"mode\":\"audio\",\"other\":0}"}){File.WriteAllText(marker,raw);Check("malformed bounded fixture "+count,Refused(()=>Validate(paths)));}
  File.WriteAllText(marker,"{\"schemaVersion\":1,\"owner\":\"reveal-audio-player-fixture\",\"mode\":\"audio\"}");
  var wrong=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+Path.Combine(run,"other"),"--vcc-artifacts="+folder});Check("foreign save refused",Refused(()=>Validate(wrong)));
  wrong=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=ready","--vcc-save-root="+Path.Combine(run,"session"),"--vcc-artifacts="+folder});Check("wrong smoke scenario refused",Refused(()=>Validate(wrong)));
  Directory.Delete(run,true);
  var legacyRun=Path.Combine(Root,".artifacts","reveal-audio-legacy","optin-fixture");var legacyFolder=Path.Combine(legacyRun,"before-kill");Directory.CreateDirectory(legacyFolder);
  File.WriteAllText(Path.Combine(legacyFolder,"reveal-audio-legacy-fixture.json"),"{\"schemaVersion\":1,\"owner\":\"reveal-audio-legacy-fixture\",\"case\":\"before-kill\"}");
  var legacy=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+Path.Combine(legacyRun,"crash-session"),"--vcc-artifacts="+legacyFolder});Check("legacy exact isolated crash fixture",Validate(legacy)=="legacy");
  var foreignLegacy=GamePaths.Resolve(Root,data,false,new[]{"--vcc-smoke=input","--vcc-save-root="+Path.Combine(legacyRun,"session"),"--vcc-artifacts="+legacyFolder});Check("legacy foreign save rejected",Refused(()=>Validate(foreignLegacy)));
  Directory.Delete(legacyRun,true);Console.WriteLine("TEST_COUNT "+count);return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
