using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace VintageCollector.EditorTools
{
    // Read-only admission; no scene, player, directory or save creation here.
    public static class Phase4BuildPolicy
    {
        private static string Full(string path) {
            if(string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))throw new ArgumentException("Absolute project root required");
            string result=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            CheckAncestors(result);return result;
        }
        private static void CheckAncestors(string path) {
            string parent=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(parent) && parent!=path)CheckAncestors(parent);
            try { if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Reparse points refused"); }
            catch(FileNotFoundException){}catch(DirectoryNotFoundException){}
        }
        public static string ValidateProject(string current,string approvedRoot) {
            string root=Full(approvedRoot),project=Full(current);
            if(!string.Equals(project,root,StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(project,Path.Combine(root,".artifacts","phase4-build-project"),StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only approved project or exact owned Phase 4 stage allowed");
            return project;
        }
        public static string Resolve(string projectRoot) {
            string root=Full(projectRoot),output=Path.Combine(root,"Builds","Phase4Windows");CheckAncestors(output);
            if(File.Exists(output))throw new InvalidOperationException("Candidate output is a file");
            if(Directory.Exists(output))foreach(string child in Directory.GetFileSystemEntries(output)) {
                if(!string.Equals(Path.GetFileName(child),"Saves",StringComparison.OrdinalIgnoreCase))continue;
                CheckAncestors(child);
                if(!Directory.Exists(child) || Directory.GetFileSystemEntries(child).Length!=0)
                    throw new InvalidOperationException("Populated candidate Saves blocks rebuild; preserve the entire directory unchanged");
            }
            return output;
        }
        private static bool Excluded(string name) {
            return string.Equals(name,"Saves",StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name,"Logs",StringComparison.OrdinalIgnoreCase);
        }
        public static IDictionary<string,string> ImmutableManifest(string directory) {
            string root=Full(directory);var result=new SortedDictionary<string,string>(StringComparer.Ordinal);
            if(Directory.Exists(root))Walk(root,root,result);return result;
        }
        private static void Walk(string root,string directory,IDictionary<string,string> result) {
            foreach(string path in Directory.GetFileSystemEntries(directory)) {
                // Exclude by name BEFORE attributes, descent or bytes. Never inspect saved progress.
                if(Excluded(Path.GetFileName(path)))continue;
                var attributes=File.GetAttributes(path);
                if((attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Immutable player contains a reparse point");
                if((attributes&FileAttributes.Directory)!=0)Walk(root,path,result);
                else using(var hash=SHA256.Create())using(var file=File.OpenRead(path))
                    result.Add(path.Substring(root.Length+1).Replace('\\','/'),BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant());
            }
        }
    }
}
