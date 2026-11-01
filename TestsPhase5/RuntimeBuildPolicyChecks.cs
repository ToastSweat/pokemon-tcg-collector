using System;
using System.IO;
using VintageCollector.EditorTools;
public static class RuntimeBuildPolicyChecks {
 static int count;
 static void Check(string name,bool ok){count++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)throw new Exception(name);}
 static bool Refuses(Action a){try{a();return false;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}}
 public static int Main(string[] args){
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
  Check("main-root-refused",Refuses(()=>Phase5BuildPolicy.RequireOwnedStage(root,root)));
  Check("cousin-refused",Refuses(()=>Phase5BuildPolicy.RequireOwnedStage(Path.Combine(root,".artifacts","phase5-build-project-cousin"),root)));
  string stage=Path.Combine(root,".artifacts","phase5-build-project");
  Check("unowned-exact-stage-refused",Refuses(()=>Phase5BuildPolicy.RequireOwnedStage(stage,root)));
  string save=Path.Combine(root,"Builds","Phase5Windows","sAvEs");Directory.CreateDirectory(save);File.WriteAllText(Path.Combine(save,"canary"),"progress");
  Check("populated-save-refused",Refuses(()=>Phase5BuildPolicy.Resolve(root)));
  Check("save-byte-canary",File.ReadAllText(Path.Combine(save,"canary"))=="progress");
  string hidden=Path.Combine(root,"Assets","Logs","Editor","code.cs");Directory.CreateDirectory(Path.GetDirectoryName(hidden));File.WriteAllText(hidden,"compile me");
  Check("compiler-never-prunes-logs",Phase5BuildPolicy.CompilerManifest(Path.Combine(root,"Assets")).ContainsKey("Logs/Editor/code.cs"));
  Console.WriteLine("CHECKS "+count);return 0;
 }
}
