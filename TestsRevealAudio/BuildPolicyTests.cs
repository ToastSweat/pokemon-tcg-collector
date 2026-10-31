using System;
using System.IO;
using System.Reflection;
using VintageCollector.EditorTools;
public static class BuildPolicyTests {
 static int count;
 static void Check(string name,bool ok){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Refuses(string name,Action f){bool refused=false;try{f();}catch(ArgumentException){refused=true;}catch(InvalidOperationException){refused=true;}Check(name,refused);}
 public static int Main(string[] args){try{
  var root=Path.GetFullPath(args[0]);var fixture=Path.Combine(root,".artifacts","reveal-audio-tests","policy-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fixture);
  var before=TiltBuildPolicy.ImmutableManifest(Path.Combine(root,"ProjectSettings"));
  Refuses("main project forward slash admission",()=>RevealAudioBuildPolicy.RequireOwnedStage(root.Replace('\\','/'),root));
  Refuses("main project backslash admission",()=>RevealAudioBuildPolicy.RequireOwnedStage(root.Replace('/','\\'),root));
  Check("no project settings mutation",before.Count==TiltBuildPolicy.ImmutableManifest(Path.Combine(root,"ProjectSettings")).Count);
  var stage=Path.Combine(root,".artifacts","reveal-audio-build-project");
  Check("canonical exact stage",RevealAudioBuildPolicy.RequireOwnedStage(stage,root)==Path.GetFullPath(stage));
  Check("case-insensitive Windows stage",string.Equals(RevealAudioBuildPolicy.RequireOwnedStage(stage.ToUpperInvariant(),root),stage,StringComparison.OrdinalIgnoreCase));
  Refuses("old tilt stage forbidden",()=>RevealAudioBuildPolicy.RequireOwnedStage(Path.Combine(root,".artifacts","tilt-build-project"),root));
  Refuses("sibling stage forbidden",()=>RevealAudioBuildPolicy.RequireOwnedStage(stage+"-other",root));
  Refuses("stage traversal forbidden",()=>RevealAudioBuildPolicy.RequireOwnedStage(Path.Combine(stage,".."),root));
  var output=RevealAudioBuildPolicy.Resolve(fixture);Check("distinct output",output==Path.Combine(fixture,"Builds","Phase3RevealAudioWindows"));
  var saves=Path.Combine(output,"sAvEs");Directory.CreateDirectory(saves);var canary=Path.Combine(saves,"canary");File.WriteAllText(canary,"preserve");
  Refuses("populated casefold saves block",()=>RevealAudioBuildPolicy.Resolve(fixture));Check("canary unchanged",File.ReadAllText(canary)=="preserve");
  var input=Path.Combine(fixture,"Assets","Logs","Editor","Hidden.cs");Directory.CreateDirectory(Path.GetDirectoryName(input));File.WriteAllText(input,"class Hidden {}");
  var compilerManifest=typeof(RevealAudioBuildPolicy).GetMethod("CompilerManifest");Check("complete compiler manifest API",compilerManifest!=null);
  var inputs=(System.Collections.Generic.IDictionary<string,string>)compilerManifest.Invoke(null,new object[]{Path.Combine(fixture,"Assets")});Check("compilable Logs asset never pruned",inputs.ContainsKey("Logs/Editor/Hidden.cs"));
  Directory.Delete(fixture,true);Console.WriteLine("CHECK_COUNT="+count);return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}}
}
