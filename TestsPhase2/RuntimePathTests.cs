using System;
using System.Reflection;

internal static class RuntimePathTests
{
    private const string Root = "F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
    private static Type PathsType()
    {
        var type = Assembly.GetExecutingAssembly().GetType("VintageCollector.Runtime.GamePaths");
        if (type == null) throw new Exception("GamePaths is not implemented: scoped runtime save resolution is missing.");
        return type;
    }
    private static object Resolve(bool editor, params string[] args)
    {
        return PathsType().GetMethod("Resolve").Invoke(null, new object[] {
            Root, editor ? Root + "/Assets" : Root + "/Builds/Phase2Windows/VintageCardCollector_Data", editor, args });
    }
    private static string Read(object value, string property) { return (string)value.GetType().GetProperty(property).GetValue(value, null); }
    private static void EqualPath(string expected, string actual)
    {
        if (System.IO.Path.GetFullPath(expected) != System.IO.Path.GetFullPath(actual)) throw new Exception("Expected " + expected + " got " + actual);
    }
    private static void Rejected(Action action)
    {
        try { action(); }
        catch (Exception error)
        {
            if ((error.InnerException ?? error) is ArgumentException) return;
            throw;
        }
        throw new Exception("Unsafe/ambiguous runtime path arguments were accepted.");
    }
    public static int Main()
    {
        int failures = 0, count = 0;
        Action<string, Action> run = delegate(string name, Action test) {
            count++;
            try { test(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + (error.InnerException ?? error)); }
        };
        run("scoped_default_editor_and_player_paths", delegate {
            EqualPath(Root + "/Saves/Phase2", Read(Resolve(true), "SaveDirectory"));
            EqualPath(Root + "/Builds/Phase2Windows/Saves", Read(Resolve(false), "SaveDirectory"));
        });
        run("smoke_explicitly_uses_isolated_project_artifacts", delegate {
            string save = Root + "/.artifacts/phase2-player/session";
            string output = Root + "/.artifacts/phase2-player/prepare";
            object paths = Resolve(false, "--vcc-smoke=prepare", "--vcc-save-root=" + save, "--vcc-artifacts=" + output);
            EqualPath(save, Read(paths, "SaveDirectory"));
            EqualPath(output, Read(paths, "ArtifactDirectory"));
            if (Read(paths, "SmokeScenario") != "prepare") throw new Exception("Explicit smoke scenario lost.");
        });
        run("unsafe_or_ambiguous_overrides_fail_closed", delegate {
            string save = "--vcc-save-root=" + Root + "/.artifacts/phase2-player/session";
            string output = "--vcc-artifacts=" + Root + "/.artifacts/phase2-player/reports";
            Rejected(delegate { Resolve(false, save, output); });
            Rejected(delegate { Resolve(false, "--vcc-smoke=prepare", "--vcc-save-root=" + Root + "/Saves/Phase2", output); });
            Rejected(delegate { Resolve(false, "--vcc-smoke=prepare", "--vcc-save-root=" + Root + "Sibling/.artifacts/save", output); });
            Rejected(delegate { Resolve(false, "--vcc-smoke=prepare", save, "--vcc-artifacts=" + Root + "/.artifacts/../Saves"); });
            Rejected(delegate { Resolve(false, "--vcc-smoke=typo", save, output); });
            Rejected(delegate { Resolve(false, "--vcc-smoke=prepare", "--vcc-smoke=resume", save, output); });
            Rejected(delegate { Resolve(false, "--vcc-unknown=1"); });
            Rejected(delegate { PathsType().GetMethod("Resolve").Invoke(null, new object[] { Root, "C:/Outside/VintageCardCollector_Data", false, new string[0] }); });
        });
        run("existing_directory_junctions_cannot_redirect_output", delegate {
            string save = "--vcc-save-root=" + Root + "/.artifacts/phase2-paths/junction-fixture/session";
            string output = "--vcc-artifacts=" + Root + "/.artifacts/phase2-paths/reports";
            Rejected(delegate { Resolve(false, "--vcc-smoke=prepare", save, output); });
        });
        run("default_save_path_is_checked_for_directory_redirection", delegate {
            string root = Root + "/.artifacts/phase2-paths/default-root";
            Rejected(delegate { PathsType().GetMethod("Resolve").Invoke(null, new object[] { root, root + "/Assets", true, new string[0] }); });
        });
        run("validated_profile_paths_never_escape_or_nest", delegate {
            object initial = Resolve(false);
            var method = initial.GetType().GetMethod("WithProfile");
            if (method == null) throw new Exception("Validated profile path selection is not implemented.");
            string id = "0123456789abcdef0123456789abcdef";
            object selected = method.Invoke(initial, new object[] { id });
            EqualPath(Root + "/Builds/Phase2Windows/Saves/Profiles/" + id, Read(selected, "SaveDirectory"));
            if (Read(selected, "CollectionProfileId") != id) throw new Exception("Profile identity not retained.");
            object again = method.Invoke(selected, new object[] { id });
            EqualPath(Read(selected, "SaveDirectory"), Read(again, "SaveDirectory"));
            object original = method.Invoke(selected, new object[] { "" });
            EqualPath(Read(initial, "SaveDirectory"), Read(original, "SaveDirectory"));
            Rejected(delegate { method.Invoke(initial, new object[] { "../outside" }); });
            Rejected(delegate { method.Invoke(initial, new object[] { id.ToUpperInvariant() }); });
            Rejected(delegate { method.Invoke(initial, new object[] { "01234567-89ab-cdef-0123-456789abcdef" }); });
        });
        Console.WriteLine("TEST_COUNT " + count);
        return failures == 0 ? 0 : 1;
    }
}
