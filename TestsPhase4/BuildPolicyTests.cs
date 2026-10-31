using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

internal static class Phase4BuildPolicyTests
{
    private static int count;
    private static object Call(string name, params object[] args) {
        Type type=Assembly.GetExecutingAssembly().GetType("VintageCollector.EditorTools.Phase4BuildPolicy");
        if(type==null) throw new Exception("Phase4BuildPolicy missing");
        try { return type.GetMethod(name).Invoke(null,args); }
        catch(TargetInvocationException e) { throw e.InnerException; }
    }
    private static void Check(string name,bool ok) { if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++; }
    private static bool Reject(Action a) {try {a();return false;} catch(ArgumentException){return true;} catch(InvalidOperationException){return true;}}
    public static int Main(string[] args) {
        try {
            string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
            string output=Path.Combine(root,"Builds","Phase4Windows");
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
            string stage=Path.Combine(root,".artifacts","phase4-build-project");
            Check("owned_exact_stage",(string)Call("ValidateProject",stage,root)==stage);
            Check("foreign_stage_refused",Reject(()=>Call("ValidateProject",stage+"-other",root)));
            Check("relative_root_refused",Reject(()=>Call("Resolve","relative")));
            Console.WriteLine("TEST_COUNT "+count);return 0;
        } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
    }
}
