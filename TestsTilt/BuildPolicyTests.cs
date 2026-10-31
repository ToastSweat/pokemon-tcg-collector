using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

internal static class TiltBuildPolicyTests
{
    private static int count;
    private static object Call(string name, params object[] args) {
        Type type=Assembly.GetExecutingAssembly().GetType("VintageCollector.EditorTools.TiltBuildPolicy");
        if(type==null) throw new Exception("TiltBuildPolicy missing");
        try { return type.GetMethod(name).Invoke(null,args); }
        catch(TargetInvocationException e) { throw e.InnerException; }
    }
    private static void Check(string name,bool ok) { if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++; }
    private static bool Reject(Action a) {try {a();return false;} catch(ArgumentException){return true;} catch(InvalidOperationException){return true;}}
    public static int Main(string[] args) {
        try {
            string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
            string output=Path.Combine(root,"Builds","Phase3TiltWindows");
            Check("separate_candidate_readonly",(string)Call("Resolve",root)==output && !Directory.Exists(output));
            string save=Path.Combine(output,"sAvEs");Directory.CreateDirectory(save);
            string canary=Path.Combine(save,"state.save");File.WriteAllText(canary,"preserve-exact-canary");
            Check("populated_case_insensitive_saves_refused",Reject(()=>Call("Resolve",root)));
            Check("refusal_keeps_canary_bytes",File.ReadAllText(canary)=="preserve-exact-canary");
            File.Delete(canary);Check("empty_save_admitted",(string)Call("Resolve",root)==output);
            string accepted=Path.Combine(root,"Builds","OlderAccepted");Directory.CreateDirectory(accepted);File.WriteAllText(Path.Combine(accepted,"game.exe"),"accepted");
            Directory.CreateDirectory(Path.Combine(accepted,"SaVeS"));File.WriteAllText(Path.Combine(accepted,"SaVeS","secret"),"must-not-read");
            var files=(IDictionary<string,string>)Call("ImmutableManifest",accepted);
            Check("immutable_manifest_excludes_saves_before_traversal",files.Count==1 && files.ContainsKey("game.exe"));
            Check("immutable_manifest_sha256",files["game.exe"]=="070c160a6299c5438070b1aa737b14fc2992ed49579c14264884886a5876f971");
            Check("approved_original_project",(string)Call("ValidateProject",root,root)==root);
            string stage=Path.Combine(root,".artifacts","tilt-build-project");
            Check("owned_exact_stage",(string)Call("ValidateProject",stage,root)==stage);
            Check("foreign_stage_refused",Reject(()=>Call("ValidateProject",stage+"-other",root)));
            Check("relative_root_refused",Reject(()=>Call("Resolve","relative")));
            Type policy=Assembly.GetExecutingAssembly().GetType("VintageCollector.EditorTools.TiltBuildPolicy");
            Check("stage_only_guard_exists",policy.GetMethod("RequireOwnedStage")!=null);
            Check("stage_only_rejects_main_forward_slashes",Reject(()=>Call("RequireOwnedStage",root.Replace('\\','/'),root.Replace('\\','/'))));
            Check("stage_only_rejects_main_native_slashes",Reject(()=>Call("RequireOwnedStage",root,root.Replace('\\','/'))));
            Check("stage_only_accepts_exact_native_stage",(string)Call("RequireOwnedStage",stage,root.Replace('\\','/'))==stage);
            Check("stage_only_accepts_case_folded_stage",string.Equals((string)Call("RequireOwnedStage",stage.ToUpperInvariant(),root),stage,StringComparison.OrdinalIgnoreCase));
            Check("stage_only_rejects_parent",Reject(()=>Call("RequireOwnedStage",Path.GetDirectoryName(stage),root)));
            Check("stage_only_rejects_sibling_prefix",Reject(()=>Call("RequireOwnedStage",stage+"-foreign",root)));
            Directory.CreateDirectory(Path.Combine(accepted,"LoGs"));File.WriteAllText(Path.Combine(accepted,"LoGs","private.log"),"excluded");
            Check("immutable_manifest_prunes_logs",((IDictionary<string,string>)Call("ImmutableManifest",accepted)).Count==1);
            Console.WriteLine("TEST_COUNT "+count);return 0;
        } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
    }
}