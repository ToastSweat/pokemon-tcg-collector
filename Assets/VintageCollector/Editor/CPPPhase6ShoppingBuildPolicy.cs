using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace VintageCollector.EditorTools {
 // Guard component only. No Unity dependency, launch, creation, deletion or old admission.
 public static class CPPPhase6ShoppingBuildPolicy {
  public const string Root=Phase5BuildPolicy.Root;
  public const string StageRelative=".artifacts/s6";
  public const string CandidateRelative="Builds/Phase6ShoppingWindows";
  static string Full(string p){if(!Path.IsPathRooted(p))throw new ArgumentException("Absolute required");Phase5BuildPolicy.Safe(p);return Path.GetFullPath(p).TrimEnd('\\','/');}
  public static void Boundaries(string current){BoundariesCore(Root,current);}
  // Semantic exact-root injection is exclusively a directly owned offline fixture.
  public static void FixtureBoundaries(string root,string current,string ownedWorkspace){string fixtures=Full(Path.Combine(ownedWorkspace,"fixtures"));if(!string.Equals(Path.GetDirectoryName(Full(root)),fixtures,StringComparison.OrdinalIgnoreCase)||!Full(ownedWorkspace).Equals(Full(Path.Combine(Root,".artifacts/phase6-shopping-native-build-work")),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Not owned fixture");BoundariesCore(root,current);}
  static void BoundariesCore(string root,string current){root=Full(root);string stage=Full(Path.Combine(root,StageRelative));if(!string.Equals(Full(current),stage,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Exact NEW stage required, never Root/old/cousin");string output=Full(Path.Combine(root,CandidateRelative));if(File.Exists(output))throw new InvalidOperationException("Candidate file");if(Directory.Exists(output))foreach(string p in Directory.GetFileSystemEntries(output))if(string.Equals(Path.GetFileName(p),"Saves",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Candidate Saves refused by name before attributes or reads");Phase5BuildPolicy.RequireRegularTree(stage);Phase5BuildPolicy.RequireRegularTree(output);if(Directory.Exists(stage)){string owned=Path.Combine(stage,".shopping-owned-stage.json");if(!File.Exists(owned)||new FileInfo(owned).Length>=4096||File.ReadAllText(owned)!="{\"owner\": \"shopping-exact-source-v1\", \"root\": \""+root.Replace('\\','/')+"\"}")throw new InvalidOperationException("Exact owned stage marker required before headers");}if(Directory.Exists(output)&&Directory.GetFileSystemEntries(output).Length>0){string marker=Path.Combine(output,".shopping-candidate-owner.txt");if(!File.Exists(marker)||File.ReadAllText(marker)!="shopping-exact-source-v1\n"+root.Replace('\\','/')+"\n")throw new InvalidOperationException("Foreign candidate");}}
  public static void ExactClosure(string project,IDictionary<string,string> expected){foreach(string n in expected.Keys)if(Path.Combine(project,n).Length>=260)throw new InvalidOperationException("Mono path budget");var actual=Phase5BuildPolicy.AllInputs(project);if(actual.Count!=expected.Count)throw new InvalidOperationException("Full closure membership");foreach(var pin in expected){string d;if(!actual.TryGetValue(pin.Key,out d)||pin.Value!=d)throw new InvalidOperationException("Full closure bytes");}RequireUniqueGuids(project,actual);}
  public static void RequireUniqueGuids(string project,IDictionary<string,string> all){var seen=new HashSet<string>(StringComparer.Ordinal);foreach(string n in all.Keys)if(n.EndsWith(".meta",StringComparison.Ordinal)){string path=Path.Combine(project,n);Phase5BuildPolicy.Safe(path);if(new FileInfo(path).Length>65536)throw new InvalidOperationException("Meta bound");string text=File.ReadAllText(path);var matches=Regex.Matches(text,"(?m)^guid: ([0-9a-f]{32})\\r?$");if(matches.Count!=1||!Regex.IsMatch(text,"(?m)^fileFormatVersion: 2\\r?$")||!seen.Add(matches[0].Groups[1].Value))throw new InvalidOperationException("Invalid/duplicate GUID");}}
  public static void NativeMetaAllowance(string root,string stage,IDictionary<string,string> source,IEnumerable<string> assets){ExactSource(root,source);RequireUniqueGuids(stage,Phase5BuildPolicy.AllInputs(stage));var seen=new HashSet<string>(StringComparer.Ordinal);foreach(string n in assets){if(!seen.Add(n)||!n.StartsWith("Assets/",StringComparison.Ordinal)||n.EndsWith(".meta",StringComparison.Ordinal)||n.Contains("..")||n.Contains("\\")||n.Contains(":")||!source.ContainsKey(n)||source.ContainsKey(n+".meta")||File.Exists(Path.Combine(root,n+".meta")))throw new InvalidOperationException("Unknown/duplicate/existing metadata allowance");string meta=Path.Combine(stage,n+".meta");if(!n.EndsWith(".cs",StringComparison.Ordinal)||!File.Exists(meta)||new FileInfo(meta).Length>65536||Phase5BuildPolicy.Hash(Path.Combine(stage,n))!=source[n])throw new InvalidOperationException("Stage source/meta mismatch");string text=new UTF8Encoding(false,true).GetString(File.ReadAllBytes(meta));bool minimal=Regex.IsMatch(text,@"\AfileFormatVersion: 2\r?\nguid: [0-9a-f]{32}(?:\r?\n)?\z");bool explicitImporter=Regex.Matches(text,"(?m)^MonoImporter:").Count==1&&!Regex.IsMatch(text,"(?m)^(?:folderAsset|TextureImporter|DefaultImporter):");if(!minimal&&!explicitImporter)throw new InvalidOperationException("Stage script metadata role mismatch");}}

  public const string Generation="f1d3132f59f170d06554aa8e108110a516ffc4697f2c11eaa7255dc70a219149";
  public const string Scene="Assets/VintageCollector/Scenes/BootstrapShopping.unity";
  public const string Startup="Assets/VintageCollector/Scenes/StartupShopping.unity";
  public static readonly string[] Scenes={Startup,Scene};
  public const string Settings="ProjectSettings/EditorBuildSettings.asset";
  public static byte[] RecipeBytes(string root,string stage){
   byte[] raw=File.ReadAllBytes(Path.Combine(root,Settings));string old=new UTF8Encoding(false,true).GetString(raw);
   var matches=Regex.Matches(old,@"(?m)^  - enabled: 1\r?\n    path: Assets/VintageCollector/Scenes/Bootstrap\.unity\r?\n    guid: [0-9a-f]{32}\r?\n");
   if(matches.Count!=1||Regex.Matches(old,@"(?m)^  - enabled:").Count!=1||Regex.Matches(old,@"(?m)^    path:").Count!=1||Regex.Matches(old,@"(?m)^    guid:").Count!=1)throw new InvalidOperationException("Exact single old scene block");
   string nl=matches[0].Value.Contains("\r\n")?"\r\n":"\n";var replacement=new StringBuilder();
   foreach(string scene in Scenes){string meta=File.ReadAllText(Path.Combine(stage,scene+".meta"));var guid=Regex.Matches(meta,@"(?m)^guid: ([0-9a-f]{32})\r?$");if(guid.Count!=1)throw new InvalidOperationException("Scene GUID");replacement.Append("  - enabled: 1"+nl+"    path: "+scene+nl+"    guid: "+guid[0].Groups[1].Value+nl);}
   var m=matches[0];return new UTF8Encoding(false,true).GetBytes(old.Substring(0,m.Index)+replacement+old.Substring(m.Index+m.Length));
  }
  static void RequireFolderMeta(string path){
   Phase5BuildPolicy.Safe(path);if(new FileInfo(path).Length>65536)throw new InvalidOperationException("Folder metadata bound");
   var seen=new HashSet<string>(StringComparer.Ordinal);string current=null;
   foreach(string raw in File.ReadAllText(path,new UTF8Encoding(false,true)).Split('\n')){
    string line=raw.TrimEnd('\r');if(string.IsNullOrWhiteSpace(line))continue;
    if(line.StartsWith(" ",StringComparison.Ordinal)){if(current!="DefaultImporter")throw new InvalidOperationException("Folder nested fields require DefaultImporter");continue;}
    var match=Regex.Match(line,@"\A(?:(fileFormatVersion): 2|(guid): [0-9a-f]{32}|(folderAsset): yes|(DefaultImporter):)\z");
    if(!match.Success)throw new InvalidOperationException("Unexpected folder root role");
    current=match.Groups.Cast<Group>().Skip(1).Single(g=>g.Success).Value;
    if(!seen.Add(current))throw new InvalidOperationException("Duplicate folder root role");
   }
   if(!seen.SetEquals(new[]{"fileFormatVersion","guid","folderAsset","DefaultImporter"}))throw new InvalidOperationException("Exact folder DefaultImporter roots required");
  }
  public const string ShoppingFolder="Assets/VintageCollector/Shopping";
  public static readonly string[] ShoppingSources={"WantedCard.cs","ShoppingLinks.cs","Money.cs","Comparison.cs","ShoppingNotesStore.cs","ShoppingJsonCodec.cs"};
  public static void NativeFolderAllowance(string root,string stage,IDictionary<string,string> source,IEnumerable<string> folders){
   var owned=new HashSet<string>(ShoppingSources.Select(n=>ShoppingFolder+"/"+n),StringComparer.Ordinal);
   if(!owned.SetEquals(source.Keys.Where(n=>n.StartsWith(ShoppingFolder+"/",StringComparison.Ordinal)&&!n.EndsWith(".meta",StringComparison.Ordinal))))throw new InvalidOperationException("Exact SIX Shopping source ownership");
   var missing=new HashSet<string>(StringComparer.Ordinal);if(!File.Exists(Path.Combine(root,ShoppingFolder+".meta")))missing.Add(ShoppingFolder);
   var list=folders.ToArray();if(list.Length!=list.Distinct(StringComparer.Ordinal).Count()||!missing.SetEquals(list))throw new InvalidOperationException("Exact Shopping folder allowance");
   foreach(string n in list){Phase5BuildPolicy.Safe(Path.Combine(root,n));Phase5BuildPolicy.Safe(Path.Combine(stage,n));if(!Directory.Exists(Path.Combine(root,n))||!Directory.Exists(Path.Combine(stage,n))||source.ContainsKey(n))throw new InvalidOperationException("Owned folder required");RequireFolderMeta(Path.Combine(stage,n+".meta"));}
   foreach(string n in source.Keys.Where(n=>n.StartsWith("Assets/",StringComparison.Ordinal))){string dir=Path.GetDirectoryName(n);while(!string.IsNullOrEmpty(dir)&&dir.Replace('\\','/')!="Assets"){if(dir.Replace('\\','/')!=ShoppingFolder&&!File.Exists(Path.Combine(root,dir+".meta")))throw new InvalidOperationException("Other folder metadata must exist");dir=Path.GetDirectoryName(dir);}}
  }
  // Actual production admission helper: inherited frozen pins remain authoritative; only validated new metadata can extend them.
  public static void AdmitClosure(string root,string stage,IDictionary<string,string> source,IDictionary<string,string> frozen,IEnumerable<string> assets,IEnumerable<string> folders,string action){
   ExactSource(root,source);if(action=="BuildWindows"&&!File.ReadAllBytes(Path.Combine(stage,Settings)).SequenceEqual(RecipeBytes(root,stage)))throw new InvalidOperationException("Exact effective recipe required");var actual=Phase5BuildPolicy.AllInputs(stage);var files=assets.ToArray();var dirs=folders.ToArray();var missing=new HashSet<string>(source.Keys.Where(n=>n.StartsWith("Assets/",StringComparison.Ordinal)&&!n.EndsWith(".meta",StringComparison.Ordinal)&&!source.ContainsKey(n+".meta")),StringComparer.Ordinal);if(files.Length!=files.Distinct(StringComparer.Ordinal).Count()||!missing.SetEquals(files))throw new InvalidOperationException("Every missing file meta required");
   NativeMetaAllowance(root,stage,source,files);NativeFolderAllowance(root,stage,source,dirs);var extras=new HashSet<string>(files.Concat(dirs).Select(n=>n+".meta"),StringComparer.Ordinal);
   foreach(var p in frozen){string d;if(!actual.TryGetValue(p.Key,out d)||d!=p.Value)throw new InvalidOperationException("Frozen stage drift");}
   if(actual.Keys.Any(n=>!frozen.ContainsKey(n)&&!extras.Contains(n)))throw new InvalidOperationException("Unapproved import extra");
   foreach(var p in source){string d;if(!frozen.TryGetValue(p.Key,out d))throw new InvalidOperationException("Missing source");if(d!=p.Value){if(action!="BuildWindows"||p.Key!=Settings||!File.ReadAllBytes(Path.Combine(stage,Settings)).SequenceEqual(RecipeBytes(root,stage)))throw new InvalidOperationException("Source drift");}}
   if(frozen.Keys.Any(n=>!source.ContainsKey(n)))throw new InvalidOperationException("Unapproved frozen extra");if(action=="BuildWindows"&&(files.Length!=0||dirs.Length!=0))throw new InvalidOperationException("Adopt metadata before build");RequireUniqueGuids(stage,actual);
  }
  public static void ExactSource(string root,IDictionary<string,string> source){var actual=Phase5BuildPolicy.AllInputs(root);if(actual.Count!=source.Count)throw new InvalidOperationException("Root membership");foreach(var pin in source){string d;if(!actual.TryGetValue(pin.Key,out d)||d!=pin.Value)throw new InvalidOperationException("Root byte mismatch");}}
 }
}
