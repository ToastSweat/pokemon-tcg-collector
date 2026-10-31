using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VintageCollector.Presentation;

namespace VintageCollector.EditorChecks
{
    public static class Phase4BinderPresentationChecks
    {
        [Serializable] private sealed class CheckRecord { public string name; public bool passed; }
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public CheckRecord[] checks; public string failure; }
        private static readonly List<CheckRecord> checks=new List<CheckRecord>();
        private static void Check(bool ok,string name) { checks.Add(new CheckRecord {name=name,passed=ok}); }
        public static void Run() {
            checks.Clear();string failure=null;GameObject go=null;
            try {
                var type=typeof(BinderView).Assembly.GetType("VintageCollector.Presentation.BinderPresentation");
                Check(type!=null,"native passive binder component exists");
                if(type!=null) {
                    go=new GameObject("SYNTHETIC DTO binder checks - not physical collection");
                    var component=go.AddComponent(type);var property=type.GetProperty("View");
                    Check(property!=null && property.PropertyType==typeof(BinderView),"exact passive DTO property");
                    var v=new BinderView {open=true,mode="physical",filter="all",selectedCardId="synthetic:base:001:unstamped:holo",quantityText="garbage - host validates",selectedCondition="unknown",rows=new[]{new BinderCardView {id="synthetic:base:001:unstamped:holo",name="Original synthetic one",collectorNumber="1"}},backups=new[]{"synthetic-backup.zip"},selectedBackupIndex=0};
                    property.SetValue(component,v);string before=JsonUtility.ToJson(v);
                    var received=new List<string>();type.GetEvent("Command").AddEventHandler(component,(Action<string>)(command=>received.Add(command)));
                    var request=type.GetMethod("RequestCommand",BindingFlags.Instance|BindingFlags.NonPublic);
                    Check(request!=null,"one guarded command dispatch seam");
                    foreach(string command in new[]{"condition:damaged","quantity:garbage - host validates","select:"+v.selectedCardId,"search:synthetic","filter:holo","page:0","backup-select:0"}) request.Invoke(component,new object[]{command});
                    Check(received.Count==7,"native passive dispatch forwards exact parent requests");
                    Check(before==JsonUtility.ToJson(v),"commands do not mutate DTO or drafts");
                    request.Invoke(component,new object[]{"save-physical"});request.Invoke(component,new object[]{"save-physical"});
                    Check(received.Count==8 && received[7]=="save-physical","unacknowledged explicit confirmation emitted once");
                    request.Invoke(component,new object[]{"backup"});Check(received.Count==8,"local pending locks dangerous controls until host publishes view");
                    request.Invoke(component,new object[]{"binder-close"});request.Invoke(component,new object[]{"quit"});Check(received.Count==10,"back quit reachable while pending");
                    property.SetValue(component,v);request.Invoke(component,new object[]{"backup"});Check(received.Count==11,"host republish acknowledges local pending");
                    v.mode="virtual";property.SetValue(component,v);request.Invoke(component,new object[]{"save-physical"});Check(received.Count==11,"virtual physical edits blocked");
                    Check(go.GetComponents<AudioSource>().Length==0,"binder never creates or changes audio sources");
                    v.open=false;property.SetValue(component,v);Check(!(bool)type.GetProperty("IsOpen").GetValue(component),"closed view hides binder");
                }
            } catch(Exception e) {failure=e.ToString();Check(false,"native exception");}
            finally {
                if(go!=null) UnityEngine.Object.DestroyImmediate(go);
                bool passed=failure==null && checks.Count>0 && checks.TrueForAll(c=>c.passed);
                string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../.artifacts/binder-engine-report.json"));Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path,JsonUtility.ToJson(new Report {passed=passed,checkCount=checks.Count,checks=checks.ToArray(),failure=failure},true));
                EditorApplication.Exit(passed?0:1);
            }
        }
    }
}
