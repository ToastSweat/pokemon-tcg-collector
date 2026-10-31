using System;
using System.IO;
using System.Reflection;

internal static class BuildPolicyTests
{
    private static Type Policy()
    {
        Type type = Assembly.GetExecutingAssembly().GetType("VintageCollector.EditorTools.Phase3BuildPolicy");
        if (type == null) throw new Exception("Phase3BuildPolicy is not implemented.");
        return type;
    }
    private static string Resolve(string root)
    {
        return (string)Policy().GetMethod("Resolve").Invoke(null, new object[] { root });
    }
    public static int Main(string[] args)
    {
        try
        {
            string root = Path.Combine(args[0], Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string expected = Path.GetFullPath(Path.Combine(root, "Builds", "Phase3Windows"));
            if (Resolve(root) != expected) throw new Exception("Candidate output escaped or reused the Phase 2 folder.");
            if (Directory.Exists(expected)) throw new Exception("Path resolution must not create an output directory.");
            Console.WriteLine("PASS separate_readonly_candidate_path");
            string saves = Path.Combine(expected, "Saves");
            Directory.CreateDirectory(saves);
            string marker = Path.Combine(saves, "state.save");
            File.WriteAllText(marker, "owned-preservation-canary");
            bool rejected = false;
            try { Resolve(root); }
            catch (Exception error) { rejected = (error.InnerException ?? error) is InvalidOperationException; }
            if (!rejected) throw new Exception("Build admission accepted an existing populated player save directory.");
            if (File.ReadAllText(marker) != "owned-preservation-canary") throw new Exception("Build admission altered existing data.");
            Console.WriteLine("PASS populated_candidate_save_is_preserved_and_blocks_rebuild");
            Console.WriteLine("TEST_COUNT 2");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine((error.InnerException ?? error).ToString()); return 1; }
    }
}
