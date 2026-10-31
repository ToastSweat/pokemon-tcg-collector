using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
// Minimal exact-method fixture: native Mono runs production method bytes. Unity lifecycle is supplied explicitly;
// this is NOT a framebuffer or production renderer replacement. Real player tests are separate.
public enum EventType { Layout, Repaint, MouseUp }
public sealed class Event { public static Event current=new Event();public EventType type; }
public static class Time { public static int frameCount; }
public static class Screen { public static int width=1600,height=900; }
public struct Rect { public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} }
public sealed class BinderView { public bool open=true;public string key="",importSummary; }
public sealed class Host { public BinderView CurrentBinderView; }
public sealed class Binder { public BinderView View; }
public static class BinderLayoutRules {
 public struct Metrics { public float scale; }
 public static Metrics Geometry(int w,int h){return new Metrics {scale=1};}
 public static bool HasReview(BinderView v){return !string.IsNullOrWhiteSpace(v.importSummary);}
}
public static class JsonUtility { public static string ToJson(BinderView v){return v.key+"/"+v.open+"/"+v.importSummary;} }
public sealed class ExactProbe {
 public sealed class Control { public string name;public float x,y,width,height;public bool enabled; }
 private Host host=new Host();private Binder binder=new Binder();
 /*FIELDS*/
 /*OBSERVER*/
 public Control[] Publish(){/*PUBLICATION*/}
 public void Set(string key,bool review=false){host.CurrentBinderView=binder.View=new BinderView {key=key,importSummary=review?"review":null};}
 public void Draw(params string[] names){Time.frameCount++;Event.current.type=EventType.Repaint;ObserveControl("mode:virtual",new Rect(0,0,1,1),true);foreach(string name in names)ObserveControl(name,new Rect(20,20,30,30),true);ObserveControl("quit",new Rect(0,0,1,1),true);if(BinderLayoutRules.HasReview(binder.View))ObserveControl("import-cancel",new Rect(0,0,1,1),false);}
 public void Partial(){Time.frameCount++;Event.current.type=EventType.Repaint;ObserveControl("mode:virtual",new Rect(0,0,1,1),true);}
 public void NonRepaint(){Time.frameCount++;Event.current.type=EventType.Layout;ObserveControl("select:ghost-layout",new Rect(0,0,1,1),true);}
 public void Mutate(){binder.View.key="mutated";}
 /*BOUNDED*/
 public static string Read(string path,int cap,params string[] values){return ReadExactFixture(path,cap,values);}
}
public static class Tests {
 static int passed,failed;
 static void Test(string name,Action test){try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void Assert(bool ok,string why){if(!ok)throw new Exception(why);}
 static bool Has(ExactProbe p,string name){return Array.Exists(p.Publish(),c=>c.name==name);}
 public static int Main(string[] args){Directory.CreateDirectory(args[0]);
  foreach(string context in new[]{"pending-import-review","pending-physical-retry","page-change","search-change","selection-change"})Test(context+"-evicts-vanished-selector",()=>{var p=new ExactProbe();p.Set("old");p.Draw("select:old");Assert(Has(p,"select:old"),"old render not observed");p.Set(context,context=="pending-import-review");p.Draw("select:current");Assert(!Has(p,"select:old") && Has(p,"select:current"),"historical selector survives current render");});
  Test("new-view-before-repaint-publishes-none",()=>{var p=new ExactProbe();p.Set("old");p.Draw("select:old");p.Set("new");Assert(p.Publish().Length==0,"old controls combined with new view");});
  Test("partial-repaint-publishes-none",()=>{var p=new ExactProbe();p.Set("old");p.Draw("select:old");p.Partial();Assert(p.Publish().Length==0,"incomplete control set published");});
  Test("non-repaint-never-registers-controls",()=>{var p=new ExactProbe();p.Set("old");p.Draw("select:old");p.NonRepaint();Assert(!Has(p,"select:ghost-layout"),"layout accepted as rendered");});
  Test("expired-frame-publishes-none",()=>{var p=new ExactProbe();p.Set("old");p.Draw("select:old");Time.frameCount+=2;Assert(p.Publish().Length==0,"expired frame accepted");});
  Test("screen-resize-invalidates-geometry",()=>{var p=new ExactProbe();p.Set("old");p.Draw("select:old");Screen.width++;Assert(p.Publish().Length==0,"old screen geometry accepted");Screen.width--;});
  Test("in-place-view-change-invalidates-controls",()=>{var p=new ExactProbe();p.Set("old");p.Draw("select:old");p.Mutate();Assert(p.Publish().Length==0,"changed DTO accepted");});
  if(args.Length>1 && args[1]=="controls-only"){Console.WriteLine("TEST_COUNT "+passed);return failed==0?0:1;}
  string file=Path.Combine(args[0],"fixture.txt");
  Test("exact-utf8-fixture-accepted",()=>{File.WriteAllBytes(file,new UTF8Encoding(false).GetBytes("physical"));Assert(ExactProbe.Read(file,256,"physical","import")=="physical","exact rejected");});
  Test("bom-rejected-before-consumption",()=>{File.WriteAllBytes(file,new byte[]{239,187,191,112,104,121,115,105,99,97,108});bool rejected=false;try{ExactProbe.Read(file,256,"physical");}catch(ArgumentException){rejected=true;}Assert(rejected && File.Exists(file),"BOM accepted");});
  Test("trailing-fixture-rejected",()=>{File.WriteAllText(file,"physical\n");bool rejected=false;try{ExactProbe.Read(file,256,"physical");}catch(ArgumentException){rejected=true;}Assert(rejected,"trailing accepted");});
  Test("oversize-rejected-even-when-expected",()=>{string large=new string('x',257);File.WriteAllText(file,large);bool rejected=false;try{ExactProbe.Read(file,256,large);}catch(ArgumentException){rejected=true;}Assert(rejected,"cap not enforced");});
  Test("invalid-utf8-rejected",()=>{File.WriteAllBytes(file,new byte[]{255});bool rejected=false;try{ExactProbe.Read(file,256,"physical");}catch(ArgumentException){rejected=true;}Assert(rejected,"invalid UTF8 accepted");});
  Console.WriteLine("TEST_COUNT "+passed);return failed==0?0:1;
 }
}
