using System;
using System.IO;
using System.Reflection;
using VintageCollector.Runtime;

public static class EditorPathChecks {
 static void Require(bool pass,string text){if(!pass)throw new Exception(text);}
 static GamePaths Phase5(string root,string data,bool editor,params string[] args){
  var method=typeof(GamePaths).GetMethod("ResolvePhase5",new[]{typeof(string),typeof(string),typeof(bool),typeof(string[])});
  Require(method!=null,"Phase5 Editor needs a separate admitted save root, not legacy Saves/Phase2");
  try{return (GamePaths)method.Invoke(null,new object[]{root,data,editor,args});}catch(TargetInvocationException e){throw e.InnerException;}
 }
 public static int Main(string[] args){
  try{
   string root=Path.Combine(args[0],"project");Directory.CreateDirectory(Path.Combine(root,"Assets"));
   var original=GamePaths.Resolve(root,Path.Combine(root,"Assets"),true,new string[0]);
   var next=Phase5(root,Path.Combine(root,"Assets"),true);
   Require(original.SaveDirectory==Path.Combine(root,"Saves","Phase2"),"Legacy Editor root changes are forbidden");
   Require(next.SaveDirectory==Path.Combine(root,"Saves","Phase5") && next.BaseSaveDirectory==next.SaveDirectory,"Phase5 Editor must have its own container");
   Require(!next.IsSmokeTest && next.ArtifactDirectory=="","Normal Editor may not acquire smoke privileges");
   Require(!Directory.Exists(Path.Combine(root,"Saves")),"Path resolution must not create save directories");
   Action standalone=delegate {
    string data=Path.Combine(root,"Builds","Phase5Windows","VintageCardCollector_Data");
    var old=GamePaths.Resolve(root,data,false,new string[0]);var p5=Phase5(root,data,false);
    Require(old.SaveDirectory==p5.SaveDirectory && p5.SaveDirectory==Path.Combine(root,"Builds","Phase5Windows","Saves"),"Standalone adjacent saves must remain unchanged");
   };
   Action smoke=delegate {
    string saves=Path.Combine(root,".artifacts","session"),art=Path.Combine(root,".artifacts","reports");
    var p5=Phase5(root,Path.Combine(root,"Assets"),true,"--vcc-smoke=input","--vcc-save-root="+saves,"--vcc-artifacts="+art);
    var old=GamePaths.Resolve(root,Path.Combine(root,"Assets"),true,new[]{"--vcc-smoke=input","--vcc-save-root="+saves,"--vcc-artifacts="+art});
    Require(p5.IsSmokeTest && p5.SmokeScenario==old.SmokeScenario && p5.SaveDirectory==old.SaveDirectory && p5.ArtifactDirectory==old.ArtifactDirectory,"Explicit smoke admission may not change");
   };
   Action profiles=delegate {
    string id=new string('1',32);var p5=Phase5(root,Path.Combine(root,"Assets"),true).WithProfile(id);
    Require(p5.BaseSaveDirectory==Path.Combine(root,"Saves","Phase5") && p5.SaveDirectory==Path.Combine(root,"Saves","Phase5","Profiles",id),"Profile must remain inside its own Phase5 container");
    Require(p5.WithProfile(id).SaveDirectory==p5.SaveDirectory,"Profile reapplication may not nest roots");
   };
   Action flags=delegate {
    foreach(string flag in new[]{"--vcc-phase5=anything","--vcc-save-root="+Path.Combine(root,".artifacts","no-smoke")}) {
     bool refused=false;try{Phase5(root,Path.Combine(root,"Assets"),true,flag);}catch(ArgumentException){refused=true;}
     Require(refused,"No new normal-mode override/unknown flag may bypass admission");
    }
   };
   Action legacyNamespace=delegate {
    Directory.CreateDirectory(Path.Combine(root,"Saves"));string file=Path.Combine(root,"Saves","Phase2");File.WriteAllText(file,"owned synthetic legacy canary");
    byte[] before=File.ReadAllBytes(file);long mtime=File.GetLastWriteTimeUtc(file).Ticks;
    Require(Phase5(root,Path.Combine(root,"Assets"),true).SaveDirectory==Path.Combine(root,"Saves","Phase5"),"Phase5 resolution may not inspect/require the legacy Phase2 destination");
    Require(Convert.ToBase64String(before)==Convert.ToBase64String(File.ReadAllBytes(file)) && mtime==File.GetLastWriteTimeUtc(file).Ticks,"Synthetic legacy evidence may not change");
    bool refused=false;try{GamePaths.Resolve(root,Path.Combine(root,"Assets"),true,new string[0]);}catch(ArgumentException){refused=true;}
    Require(refused,"Legacy admission still checks its own destination");
   };
   var tests=new System.Collections.Generic.Dictionary<string,Action> {{"standalone-unchanged",standalone},{"explicit-smoke-unchanged",smoke},{"profiles-scoped",profiles},{"unknown-and-override-refusal",flags},{"legacy-namespace-not-used",legacyNamespace}};
   Console.WriteLine("PASS separate-phase5-editor-default-without-writes");int failed=0;
   foreach(var test in tests)try{test.Value();Console.WriteLine("PASS "+test.Key);}catch(Exception e){failed++;Console.WriteLine("FAIL "+test.Key+": "+e);}
   Console.WriteLine("CHECKS "+(tests.Count+1)+" FAILED "+failed);return failed==0?0:1;
  }catch(Exception e){Console.WriteLine("FAIL "+e);Console.WriteLine("CHECKS 1 FAILED 1");return 1;}
 }
}
