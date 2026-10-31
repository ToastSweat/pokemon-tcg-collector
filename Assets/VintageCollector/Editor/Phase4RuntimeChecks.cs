using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VintageCollector.Collections;
using VintageCollector.Runtime;

namespace VintageCollector.EditorChecks {
 public static class Phase4RuntimeChecks {
  [Serializable] public sealed class Check { public string name; public bool passed; public string error; }
  [Serializable] public sealed class Report { public bool passed; public int checkCount; public Check[] checks; }
  static readonly List<Check> checks=new List<Check>();
  static void Test(string name,Action action) { try {action();checks.Add(new Check {name=name,passed=true});} catch(Exception e) {checks.Add(new Check {name=name,passed=false,error=e.ToString()});} }
  static void Require(bool value,string error) {if(!value) throw new Exception(error);}
  public static void Run() {
   string output=Environment.GetEnvironmentVariable("VCC_RUNTIME_OUTPUT");
   Test("real native JsonUtility physical public-field round trip",()=> {
    var d=new PhysicalStateData {schemaVersion=1,revision=1,edits=new[]{new PhysicalEditData {transactionId="abc",createdUtc="utc",cardId="full-id",condition="near-mint",quantity=9}}};
    var r=JsonUtility.FromJson<PhysicalStateData>(JsonUtility.ToJson(d));
    Require(r.schemaVersion==1 && r.revision==1 && r.edits.Length==1 && r.edits[0].quantity==9 && r.edits[0].cardId=="full-id","Native JSON fields lost");
   });
   var asm=typeof(OpeningGame).Assembly;
   Test("runtime bootstrap public frozen API exists",()=> {
    var t=asm.GetType("VintageCollector.Runtime.Phase4CollectionHost");Require(t!=null,"Missing Phase4CollectionHost");
    foreach(var n in new[]{"Game","Physical","CurrentBinderView","Profiles","IsReady","StatusMessage"}) Require(t.GetProperty(n)!=null,"Missing "+n);
    foreach(var n in new[]{"Initialize","DispatchBinderCommand","Shutdown"}) Require(t.GetMethod(n)!=null,"Missing "+n);
   });
   Test("real native physical serializer adapter exists",()=>Require(asm.GetType("VintageCollector.Runtime.UnityPhysicalSerializer")!=null,"Missing real adapter"));
   Test("OpeningGame profile startup and terminal block seams exist",()=> {
    Require(typeof(OpeningGame).GetMethod("InitializeForProfile")!=null,"Missing profile initializer");
    Require(typeof(OpeningGame).GetMethod("BlockInitialization")!=null,"Missing no-fallback startup guard");
   });
   Test("physical uncertain retry reachable despite error",()=> {
    var v=new VintageCollector.Presentation.BinderView {open=true,mode="physical",pendingPhysicalEdit=true,error="uncertain"};
    Require(VintageCollector.Presentation.BinderLayoutRules.CanEmit(v,"save-physical"),"No reachable retry");
    Require(!VintageCollector.Presentation.BinderLayoutRules.CanEmit(v,"mode:virtual"),"Draft context may change while pending");
   });
   Test("import uncertain retry retains exact review with no cancel",()=> {
    var v=new VintageCollector.Presentation.BinderView {open=true,pendingImportConfirm=true,importSummary="owned review",error="uncertain"};
    Require(VintageCollector.Presentation.BinderLayoutRules.CanEmit(v,"import-confirm"),"No reachable restore retry");
    Require(!VintageCollector.Presentation.BinderLayoutRules.CanEmit(v,"import-cancel"),"May discard uncertain intent");
   });
   // Scenario sources are deliberately outside Assets; only the isolated runner stages them.
   // Reflection keeps the normal project/candidate Editor assembly independently compilable.
   foreach(string fixture in new[]{"RuntimeScenarios","RuntimeNativeNumbers"}) {
    var t=typeof(Phase4RuntimeChecks).Assembly.GetType("VintageCollector.EditorChecks."+fixture);
    if(t==null) {Test("isolated fixture present: "+fixture,()=>Require(false,"Use the exact-source runtime runner"));continue;}
    t.GetMethod("Run").Invoke(null,new object[]{output,(Action<string,Action>)Test});
   }
   var report=new Report {passed=checks.TrueForAll(c=>c.passed),checkCount=checks.Count,checks=checks.ToArray()};
   File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(report,true));
   EditorApplication.Exit(report.passed?0:1);
  }
 }
}
