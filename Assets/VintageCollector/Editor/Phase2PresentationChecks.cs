using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VintageCollector.Presentation;

namespace VintageCollector.EditorChecks
{
    // Explicit invocation only. No startup hooks, scene saves, generation or data dependencies.
    public static class Phase2PresentationChecks
    {
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public string[] checks; public string failure; }
        private static readonly List<string> checks = new List<string>();
        private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks.Add(name); }
        private static void Call(Component c, string method, params object[] args)
        {
            var m=c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic);
            if (m==null) throw new Exception("Missing component method "+method);
            m.Invoke(c,args);
        }
        public static void Run()
        {
            checks.Clear(); GameObject go=null; string failure=null;
            try
            {
                Type type=null;
                foreach(var a in AppDomain.CurrentDomain.GetAssemblies()) {
                    type=a.GetType("VintageCollector.Presentation.TabletopPresentation"); if(type!=null) break;
                }
                Check(type!=null,"Real TabletopPresentation component exists");
                go=new GameObject("Presentation assertion fixture");
                var c=go.AddComponent(type);
                var emitted=new List<string>();
                type.GetEvent("Command").AddEventHandler(c,(Action<string>)(s=>emitted.Add(s)));
                var property=type.GetProperty("View");
                Check(emitted.Count==0,"Adding component never generates");
                var v=new OpeningView { stage="ready" };
                property.SetValue(c,v);
                Call(c,"RequestCommand","next");
                Check(emitted.Count==0,"Disconnected next rejected");
                Call(c,"RequestCommand","generate");
                Check(emitted.Count==1 && emitted[0]=="generate","Generate emits exact command");
                Check(v.stage=="ready" && v.cards==null,"Generate cannot invent pack or mutate view");
                emitted.Clear();
                v.stage="revealing"; v.cards=new OpeningCardView[11];
                for(int i=0;i<11;i++) v.cards[i]=new OpeningCardView {id="fixture-"+i,name="SECRET-"+i,isNew=i%2==0};
                v.revealedCount=3;
                Call(c,"RequestCommand","inspect:3");
                Check(emitted.Count==0,"Hidden inspection rejected in component");
                Call(c,"RequestCommand","inspect:2");
                Check(emitted.Count==1 && emitted[0]=="inspect:2","Revealed inspection emits exact index");
                emitted.Clear();
                Call(c,"RequestCommand","next");
                Check(emitted.Count==1 && emitted[0]=="next" && v.revealedCount==3,"Next emits without local reveal");
                emitted.Clear();
                type.GetMethod("NotifyCommitted").Invoke(c,new object[]{"open"});
                Check(emitted.Count==0 && v.revealedCount==3 && v.cards[3].name=="SECRET-3","Commit cue does not advance or reorder cards");
                Check((float)type.GetField("tearRemaining",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c)>0,"Tear starts only on committed open");
                type.GetMethod("NotifyCommitted").Invoke(c,new object[]{"generate"});
                Check(emitted.Count==0,"Commit does not recursively emit");
                Call(c,"OnApplicationFocus",false);
                Check(emitted.Count==1 && emitted[0]=="pause" && !v.paused,"Focus loss requests parent pause");
                emitted.Clear(); v.paused=true;
                Call(c,"RequestCommand","skip"); Call(c,"OnApplicationFocus",true);
                Check(emitted.Count==0,"Paused skip blocked and focus gain never resumes");
                Call(c,"RequestCommand","resume");
                Check(emitted.Count==1 && emitted[0]=="resume","Explicit Resume emits");
                emitted.Clear();
                Call(c,"RequestCommand","volume:0");
                Check(emitted.Count==1 && emitted[0]=="volume:0" && Math.Abs(v.volume-0.5f)<0.001,"Mute is parent-owned command");
                Check(go.GetComponentsInChildren<AudioSource>(true).Length==0,"No synthesized or branded audio in silent placeholder");
                v.paused=false; v.inspectIndex=-1; v.stage="sealed"; emitted.Clear();
                Call(c,"RequestCommand","open");
                Check(emitted.Count==1 && emitted[0]=="open","Open emits exact command");
                v.stage="revealing"; emitted.Clear(); Call(c,"RequestCommand","skip");
                Check(emitted.Count==1 && emitted[0]=="skip" && v.revealedCount==3,"Skip emits without disconnected results");
                v.stage="error"; emitted.Clear(); Call(c,"RequestCommand","retry");
                Check(emitted.Count==1 && emitted[0]=="retry","Retry emits exact command");
                v.inspectIndex=10; emitted.Clear(); Call(c,"RequestCommand","close-inspect");
                Check(emitted.Count==1 && emitted[0]=="close-inspect","Back emits even for rejected inspection");
                emitted.Clear(); Call(c,"RequestCommand","quit");
                Check(emitted.Count==1 && emitted[0]=="quit","Quit emits without persistence dependency");
                // Bounds checked against native Rects too, not only pure arithmetic.
                foreach(var size in new[]{new[]{1280,720},new[]{1600,900},new[]{2560,1080},new[]{1280,360}}) {
                    var l=PresentationRules.Layout(size[0],size[1]);
                    foreach(var b in new[]{l.primary,l.secondary,l.pause,l.quit,l.resume,l.back,l.mute,l.quieter,l.louder}) {
                        var r=new Rect(b.x*l.scale,b.y*l.scale,b.width*l.scale,b.height*l.scale);
                        Check(r.xMin>=0 && r.yMin>=0 && r.xMax<=size[0]+0.01f && r.yMax<=size[1]+0.01f,"Native control bounds "+size[0]+"x"+size[1]+" "+checks.Count);
                    }
                }
                var fit=type.GetMethod("FitTextSize",BindingFlags.Instance|BindingFlags.NonPublic);
                Check(fit!=null,"Native font-fitting policy exists");
                Call(c,"EnsureResources");
                ((GUIStyle)type.GetField("textStyle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c)).fontStyle=FontStyle.Bold;
                var labelBounds=new Rect(0,0,250*63f/88*0.84f,250*0.11f);
                var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                foreach(string name in new[]{"Super Energy Removal","Double Colorless Energy","Pokémon Breeder"}) {
                    int size=(int)fit.Invoke(c,new object[]{labelBounds,name,17});
                    var style=new GUIStyle {font=font,fontSize=size,fontStyle=FontStyle.Bold,wordWrap=true};
                    Check(size>=12 && style.CalcHeight(new GUIContent(name),labelBounds.width)<=labelBounds.height+0.1f,"Long name fits actual result face: "+name);
                }
                Check(font!=null,"Installed engine supplies LegacyRuntime font");
            }
            catch(Exception e) { failure=e.ToString(); Debug.LogError("PRESENTATION ASSERTION FAILED: "+failure); }
            finally
            {
                if(go!=null) UnityEngine.Object.DestroyImmediate(go);
                var report=new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),failure=failure};
                string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../.artifacts/presentation-engine-report.json"));
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output,JsonUtility.ToJson(report,true));
                Debug.Log("PRESENTATION CHECKS "+(failure==null?"PASS":"FAIL")+" count="+checks.Count+" output="+output);
            }
            if(Application.isBatchMode) EditorApplication.Exit(failure==null?0:1);
        }
    }
}
