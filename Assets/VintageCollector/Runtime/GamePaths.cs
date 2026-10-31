using System;
using System.IO;

namespace VintageCollector.Runtime
{
    // Pure path/argument policy; resolving paths never creates a file or directory.
    public sealed class GamePaths
    {
        public string SaveDirectory { get; private set; }
        public string ArtifactDirectory { get; private set; }
        public string SmokeScenario { get; private set; }
        public string BaseSaveDirectory { get; private set; }
        public string CollectionProfileId { get; private set; } = "";
        public bool IsSmokeTest { get { return !string.IsNullOrEmpty(SmokeScenario); } }

        public static GamePaths Resolve(string allowedRoot, string applicationDataPath, bool isEditor, string[] args)
        {
            if (args == null) throw new ArgumentNullException("args");
            string root = FullPath(allowedRoot);
            string dataPath = FullPath(applicationDataPath);
            RequireInside(root, dataPath);
            string appFolder = Path.GetDirectoryName(dataPath);
            var flags = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string argument in args)
            {
                if (argument == null) throw new ArgumentException("Null command-line argument");
                if (!argument.StartsWith("--vcc-", StringComparison.Ordinal)) continue;
                int equals = argument.IndexOf('=');
                if (equals < 0) throw new ArgumentException("Project flags require explicit =value");
                string key = argument.Substring(0, equals), value = argument.Substring(equals + 1);
                if (key != "--vcc-smoke" && key != "--vcc-save-root" && key != "--vcc-artifacts") throw new ArgumentException("Unknown project flag: " + key);
                if (flags.ContainsKey(key) || string.IsNullOrEmpty(value)) throw new ArgumentException("Duplicate/empty project flag: " + key);
                flags.Add(key, value);
            }
            if (!flags.ContainsKey("--vcc-smoke"))
            {
                if (flags.Count != 0) throw new ArgumentException("Save/artifact overrides require explicit smoke mode");
                string defaultSave = isEditor ? Path.Combine(root, "Saves", "Phase2") : Path.Combine(appFolder, "Saves");
                RequireInside(root, defaultSave);
                return new GamePaths { SaveDirectory = defaultSave, BaseSaveDirectory = defaultSave, SmokeScenario = "", ArtifactDirectory = "" };
            }
            string scenario = flags["--vcc-smoke"];
            if (Array.IndexOf(new[] { "ready", "prepare", "resume", "error", "input", "layout" }, scenario) < 0) throw new ArgumentException("Unknown smoke scenario");
            if (!flags.ContainsKey("--vcc-save-root") || !flags.ContainsKey("--vcc-artifacts")) throw new ArgumentException("Smoke mode requires isolated save and artifact roots");
            string save = FullPath(flags["--vcc-save-root"]), artifacts = FullPath(flags["--vcc-artifacts"]);
            string generatedRoot = Path.Combine(root, ".artifacts");
            RequireInside(generatedRoot, save);
            RequireInside(generatedRoot, artifacts);
            return new GamePaths { SaveDirectory = save, BaseSaveDirectory = save, ArtifactDirectory = artifacts, SmokeScenario = scenario };
        }

        public GamePaths WithProfile(string profileId)
        {
            if (profileId == null) throw new ArgumentNullException("profileId");
            string directory = BaseSaveDirectory;
            if (profileId.Length != 0) {
                Guid parsed;
                if (!Guid.TryParseExact(profileId,"N",out parsed) || parsed.ToString("N") != profileId)
                    throw new ArgumentException("Profile must be an exact lowercase GUID-N identity");
                directory = Path.Combine(BaseSaveDirectory,"Profiles",profileId);
                RequireInside(BaseSaveDirectory,directory);
            }
            return new GamePaths { SaveDirectory=directory, BaseSaveDirectory=BaseSaveDirectory,
                CollectionProfileId=profileId, ArtifactDirectory=ArtifactDirectory, SmokeScenario=SmokeScenario };
        }

        private static string FullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) throw new ArgumentException("An absolute project-local path is required");
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void RequireInside(string root, string path)
        {
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Path is outside its approved project directory: " + path);
            string current = root;
            CheckDirectory(current);
            foreach (string segment in path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar))
            {
                current = Path.Combine(current, segment);
                CheckDirectory(current);
            }
        }

        private static void CheckDirectory(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Reparse points cannot redirect project-controlled paths: " + path);
                if ((attributes & FileAttributes.Directory) == 0) throw new ArgumentException("Expected directory, found file: " + path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
}
