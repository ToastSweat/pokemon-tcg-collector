using System;
using System.IO;
namespace VintageCollector.EditorTools {
 // Read-only canonical admission. Existing tilt policy supplies immutable traversal only.
 public static class RevealAudioBuildPolicy {
  private static string Full(string path) {
   if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new ArgumentException("Absolute path required");
   string result=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);Safe(result);return result;
  }
  private static void Safe(string path) {
   string parent=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(parent)&&parent!=path)Safe(parent);
   try{if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Reparse path refused");}catch(FileNotFoundException){}catch(DirectoryNotFoundException){}
  }
  public static string RequireOwnedStage(string current,string approvedRoot) {
   string project=Full(current),stage=Full(Path.Combine(Full(approvedRoot),".artifacts","reveal-audio-build-project"));
   if(!string.Equals(project,stage,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Reveal audio operations require exact owned stage, never the main project");return project;
  }
  // Complete compiler inputs: unlike immutable player preservation, no Saves/Logs name pruning.
  public static System.Collections.Generic.IDictionary<string,string> CompilerManifest(string directory) {
   string root=Full(directory);var result=new System.Collections.Generic.SortedDictionary<string,string>(StringComparer.Ordinal);
   if(Directory.Exists(root))WalkInputs(root,root,result);return result;
  }
  private static void WalkInputs(string root,string directory,System.Collections.Generic.IDictionary<string,string> result) {
   foreach(string path in Directory.GetFileSystemEntries(directory)) {
    Safe(path);var attrs=File.GetAttributes(path);
    if((attrs&FileAttributes.Directory)!=0)WalkInputs(root,path,result);
    else using(var hash=System.Security.Cryptography.SHA256.Create())using(var file=File.OpenRead(path))result.Add(path.Substring(root.Length+1).Replace('\\','/'),BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant());
   }
  }
  public static string Resolve(string approvedRoot) {
   string output=Path.Combine(Full(approvedRoot),"Builds","Phase3RevealAudioWindows");Safe(output);
   if(File.Exists(output))throw new InvalidOperationException("Candidate output is a file");
   if(Directory.Exists(output))foreach(string child in Directory.GetFileSystemEntries(output)) {
    if(!string.Equals(Path.GetFileName(child),"Saves",StringComparison.OrdinalIgnoreCase))continue;
    Safe(child);if(!Directory.Exists(child)||Directory.GetFileSystemEntries(child).Length!=0)throw new InvalidOperationException("Populated candidate Saves blocks build without mutation");
   }
   return output;
  }
 }
}
