// ISOLATED TEST ONLY. Synthetic DTOs, not real physical holdings or services.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using VintageCollector.Presentation;

public sealed class UiRenderFixture : MonoBehaviour
{
    [Serializable] private sealed class CheckRecord { public string name; public bool passed; }
    [Serializable] private sealed class Report { public bool passed; public int checkCount; public CheckRecord[] checks; public string[] screenshots; public string failure; }
    private readonly List<CheckRecord> checks=new List<CheckRecord>();
    private readonly List<string> labels=new List<string>(),commands=new List<string>(),screenshots=new List<string>();
    private BinderPresentation binder;
    private BinderView view;
    private string output,failure;
    private int underlyingClicks;
    private bool longNameFits=true;
    private float reviewTextBottom;
    private readonly Dictionary<string,Rect> controls=new Dictionary<string,Rect>();
    private delegate bool WindowVisitor(IntPtr window,IntPtr state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor,IntPtr state);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll")] private static extern int GetClassName(IntPtr window,StringBuilder name,int capacity);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window,uint message,IntPtr wparam,IntPtr lparam);
    private static IntPtr OwnedWindow() {
        uint pid=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;IntPtr found=IntPtr.Zero;
        EnumWindows((window,state)=> {uint process;GetWindowThreadProcessId(window,out process);var name=new StringBuilder(128);GetClassName(window,name,128);if(process==pid && name.ToString()=="UnityWndClass") {found=window;return false;}return true;},IntPtr.Zero);return found;
    }
    private void OnGUI() { GUI.depth=1000;if(GUI.Button(new Rect(0,0,Screen.width,Screen.height),"",GUIStyle.none))underlyingClicks++; }
    private void Check(bool ok,string name) {checks.Add(new CheckRecord {name=name,passed=ok});}
    private BinderView Fixture(string mode="physical") {
        var rows=new BinderCardView[101];
        for(int i=0;i<rows.Length;i++) {
            int number=i<7?i+1:i+2;
            rows[100-i]=new BinderCardView {id="synthetic:base:"+number.ToString("000")+":unstamped:"+(i%3==0?"holo":"nonholo"),name=i==2?"Long original sample printing name":"Original sample "+number,collectorNumber=number.ToString(),rarity=i%3==0?"Rare":"Common",finish=i%3==0?"holo":"non-holo",kind="Synthetic DTO",quantity=i%4==0?7:i%4==1?1:0,owned=i%4<2};
        }
        return new BinderView {open=true,mode=mode,filter="all",search="",targetCount=101,uniqueOwned=51,totalQuantity=208,completionPercent=51.0/101*100,rows=rows,selectedCardId="synthetic:base:003:unstamped:nonholo",selectedCondition="unknown",quantityText="not validated",backups=new[]{"synthetic-backup-A.zip","synthetic-backup-B.zip","synthetic-backup-C.zip"},selectedBackupIndex=0,status="SYNTHETIC DTO FIXTURE — not an actual physical collection; no saves, awards, or core services."};
    }
    private void Publish(BinderView next) {view=next;binder.View=next;labels.Clear();controls.Clear();}
    private IEnumerator WaitFrames(int count=5) {for(int i=0;i<count;i++)yield return null;}
    private IEnumerator Click(IntPtr window,string command) {
        Rect r;if(!controls.TryGetValue(command,out r)) {Check(false,"native control exists "+command);yield break;}
        yield return ClickAt(window,(int)r.center.x,(int)r.center.y);
    }
    private IEnumerator ClickAt(IntPtr window,int x,int y) {
        var point=new IntPtr((y<<16)|(x&0xffff));PostMessage(window,0x200,IntPtr.Zero,point);PostMessage(window,0x201,new IntPtr(1),point);yield return WaitFrames(3);PostMessage(window,0x202,IntPtr.Zero,point);yield return WaitFrames(4);
    }
    private IEnumerator Key(IntPtr window,int code) {
        PostMessage(window,0x100,new IntPtr(code),new IntPtr(1));yield return WaitFrames(3);PostMessage(window,0x101,new IntPtr(code),new IntPtr(unchecked((int)0xc0000001)));yield return WaitFrames(4);
    }
    private IEnumerator TextInput(IntPtr window,string text) {
        foreach(char c in text)PostMessage(window,0x102,new IntPtr(c),new IntPtr(1));yield return WaitFrames(5);
    }
    private IEnumerator Capture(string name) {
        yield return WaitFrames(4);yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());screenshots.Add(name);
        Check(image.width==Screen.width && image.height==Screen.height,"actual framebuffer "+name);
        var sample=image.GetPixel(4,image.height-5);Check(sample.g>sample.r && sample.g>sample.b,"opaque green overlay fills viewport "+name);Destroy(image);
    }
    private bool Has(string text) {return labels.Exists(s=>s.Contains(text));}
    private IEnumerator Start() {
        Application.runInBackground=true;
        foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--binder-fixture-output="))output=arg.Substring("--binder-fixture-output=".Length);
        if(output==null) {Application.Quit(2);yield break;}Directory.CreateDirectory(output);
        binder=gameObject.AddComponent<BinderPresentation>();
        typeof(BinderPresentation).GetProperty("TextObserver",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(binder,(Action<string>)(s=>labels.Add(s)));
        typeof(BinderPresentation).GetProperty("ControlObserver",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(binder,(Action<string,Rect,bool>)((name,r,enabled)=> {
            // Observer supplies root logical rectangles; explicit scale avoids native nested GUIToScreenPoint inconsistencies.
            float scale=BinderLayoutRules.Geometry(Screen.width,Screen.height).scale;controls[name]=new Rect(r.position*scale,r.size*scale);
        }));
        typeof(BinderPresentation).GetProperty("TextLayoutObserver",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(binder,(Action<string,Rect,float>)((text,r,needed)=> {if(text.Contains("Long original sample printing name"))longNameFits &= needed<=r.height;if(text.StartsWith("SYNTHETIC READ-ONLY REVIEW"))reviewTextBottom=r.yMax*BinderLayoutRules.Geometry(Screen.width,Screen.height).scale;}));
        binder.Command+=command=>commands.Add(command);
        Publish(Fixture());Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return WaitFrames(12);
        var window=OwnedWindow();Check(window!=IntPtr.Zero,"owned fixture HWND only");
        string before=JsonUtility.ToJson(view);yield return Capture("1600x900-physical-synthetic.png");
        Check(Has("PHYSICAL OWNERSHIP") && Has("VIRTUAL REVEALED PULLS"),"both prominent collection modes rendered");
        Check(Has("51 / 101 eligible printings") && Has("208 confirmed cards"),"host unique statistics not quantity denominator");
        Check(Has("EMPTY") && Has("DUP") && Has("holo"),"empty owned duplicate holo states rendered");
        Check(Has("synthetic:base:003:unstamped:nonholo"),"selected full safe stable ID rendered");
        Check(before==JsonUtility.ToJson(view),"real GUI never mutates view");
        Check(longNameFits,"long supplied printing names fit their native rendered labels");
        yield return Click(window,"condition:damaged");Check(commands.Count==1 && commands[0]=="condition:damaged","native click requests condition only");
        Check(view.selectedCondition=="unknown","condition draft remains host owned");
        yield return Click(window,"binder-quantity");yield return TextInput(window,"z");Check(commands.Exists(c=>c.StartsWith("quantity:") && c.Contains("z")),"native text quantity forwards untrusted text");
        Check(view.quantityText=="not validated","untrusted quantity does not mutate DTO");
        Publish(Fixture());commands.Clear();yield return WaitFrames();yield return Click(window,"save-physical");yield return Click(window,"save-physical");
        Check(commands.Count==1 && commands[0]=="save-physical","native explicit ownership confirmation once until host acknowledgement");
        Check(view.rows[0].quantity==7,"confirmation never applies physical edit locally");
        yield return Click(window,"backup");Check(commands.Count==1,"pending ownership request locks backup");
        yield return Capture("1600x900-pending-synthetic.png");Check(Has("REQUEST PENDING"),"pending visible");
        Publish(Fixture());commands.Clear();yield return WaitFrames();yield return Click(window,"mode:virtual");Check(commands.Count==1 && commands[0]=="mode:virtual" && view.mode=="physical","mode click does not change source DTO");
        Publish(Fixture("virtual"));commands.Clear();yield return WaitFrames();
        Check(!controls.ContainsKey("save-physical"),"virtual view has no ownership editor");
        yield return Capture("1600x900-virtual-synthetic.png");Check(Has("REVEALED PULLS / READ ONLY") && Has("never establish physical ownership"),"virtual readonly ownership warning rendered");
        foreach(string filter in BinderLayoutRules.Filters) {yield return Click(window,"filter:"+filter);Check(commands.Contains("filter:"+filter),"native filter command "+filter);}
        yield return Click(window,"binder-search");yield return TextInput(window,"A:b");Check(commands.Exists(c=>c.StartsWith("search:") && c.Contains(":")),"native search preserves arbitrary text separator");
        yield return Click(window,"page:1");Check(commands.Contains("page:1"),"native next page request");
        Publish(Fixture());commands.Clear();yield return WaitFrames();yield return Click(window,"backup-select:1");Check(commands.Contains("backup-select:1") && view.selectedBackupIndex==0,"native backup selection is passive");
        yield return Click(window,"import-preview");Check(commands.Contains("import-preview") && string.IsNullOrEmpty(view.importSummary),"preview explicit request no local import or summary");
        var failed=Fixture();failed.error="SYNTHETIC preview failed. Keep draft and selection.";failed.selectedBackupIndex=1;failed.quantityText="untrusted draft";Publish(failed);yield return WaitFrames();
        Check(view.quantityText=="untrusted draft" && view.selectedBackupIndex==1,"failed preview preserves parent drafts");
        yield return Capture("1600x900-error-synthetic.png");Check(Has("preview failed"),"error reachable while draft retained");
        var review=Fixture();review.importSummary="SYNTHETIC REVIEW ONLY\nVirtual: 208 cards / 51 printings\nPhysical: 19 cards / 12 printings\nReviewed detached archive bytes. No archive was actually read by this UI fixture.";Publish(review);commands.Clear();yield return WaitFrames();
        yield return Capture("1600x900-review-synthetic.png");Check(Has("not a merge or overwrite") && Has("next launch") && Has("NEW PROFILE ONLY"),"review makes clone and restart semantics explicit");
        yield return Click(window,"save-physical");Check(commands.Count==0,"review blocks underlying ownership controls");
        yield return Click(window,"import-cancel");Check(commands.Count==1 && commands[0]=="import-cancel" && view.importSummary==review.importSummary,"explicit cancel never mutates reviewed summary");
        Publish(review);commands.Clear();yield return WaitFrames();yield return Click(window,"import-confirm");yield return Click(window,"import-confirm");Check(commands.Count==1 && commands[0]=="import-confirm","explicit reviewed confirmation emitted once");
        var busy=Fixture();busy.busy=true;busy.error="SYNTHETIC pending acknowledgement error must remain visible";Publish(busy);commands.Clear();yield return WaitFrames();Check(Has("pending acknowledgement error"),"busy state does not hide parent error");yield return Click(window,"save-physical");yield return Click(window,"backup");Check(commands.Count==0,"busy locks dangerous controls");
        yield return Click(window,"binder-close");yield return Click(window,"quit");Check(commands.Contains("binder-close") && commands.Contains("quit"),"busy Back and Quit reachable");
        busy.importSummary="SYNTHETIC reviewed operation still pending";Publish(busy);commands.Clear();yield return WaitFrames();yield return Key(window,0x1b);
        Check(commands.Contains("binder-close") && !commands.Contains("import-cancel"),"busy review Escape requests safe Back rather than blocked cancel");
        var restart=Fixture();restart.restartRequired=true;Publish(restart);commands.Clear();yield return Capture("1600x900-restart-synthetic.png");yield return Click(window,"backup");yield return Click(window,"save-physical");Check(commands.Count==0 && Has("RESTART REQUIRED"),"restart locks changes and displays current-profile preservation");
        Publish(Fixture());commands.Clear();yield return WaitFrames();yield return ClickAt(window,15,15);Check(underlyingClicks==0,"overlay empty space does not click through");
        yield return Key(window,0x1b);Check(commands.Contains("binder-close"),"native Escape requests Back without table action");
        Publish(Fixture());commands.Clear();yield return WaitFrames();yield return Key(window,9);yield return Key(window,0x20);Check(commands.Count>0,"native Tab and Space activate reachable control");
        foreach(var size in new[]{new[]{1280,720},new[]{2560,1080},new[]{1280,480},new[]{1280,360}}) {
            Screen.SetResolution(size[0],size[1],FullScreenMode.Windowed);Publish(new BinderView {open=false});Publish(Fixture());yield return WaitFrames(10);yield return Capture(size[0]+"x"+size[1]+"-physical-synthetic.png");
            Check(controls["binder-close"].yMax<=Screen.height+1 && controls["quit"].yMax<=Screen.height+1,"Back Quit inside viewport "+size[0]+"x"+size[1]);
            commands.Clear();yield return Click(window,"select:synthetic:base:003:unstamped:nonholo");
            Check(commands.Contains("select:synthetic:base:003:unstamped:nonholo"),"native scaled row selection uses full stable ID "+size[0]+"x"+size[1]);
            if(size[1]==720) {
                commands.Clear();yield return Click(window,"backup-select:1");Check(commands.Contains("backup-select:1"),"native scaled nested backup click");
            }
            if(size[1]==480) {
                yield return Click(window,"mode:virtual");for(int t=0;t<29;t++)yield return Key(window,9);
                var g=BinderLayoutRules.Geometry(Screen.width,Screen.height);
                Check(controls["backup"].yMin>=210*g.scale-1 && controls["backup"].yMax<=(210+g.bodyHeight)*g.scale+1,"short-window Tab scrolls offscreen backup control into view");
            }
            var r=Fixture();r.importSummary="SYNTHETIC READ-ONLY REVIEW\nA separate new profile, current collection untouched.";Publish(r);yield return WaitFrames();yield return Capture(size[0]+"x"+size[1]+"-review-synthetic.png");
            Check(controls["import-confirm"].yMax<controls["binder-close"].yMin && controls["import-confirm"].yMin>0,"confirm and Back reachable without overlap "+size[0]+"x"+size[1]);
            Check(reviewTextBottom<controls["import-confirm"].yMin,"review summary never overlaps confirmation "+size[0]+"x"+size[1]+" textBottom="+reviewTextBottom+" confirmTop="+controls["import-confirm"].yMin);
            commands.Clear();yield return Click(window,"import-cancel");Check(commands.Contains("import-cancel"),"native cancel reachable "+size[0]+"x"+size[1]);
        }
        Screen.SetResolution(1280,720,FullScreenMode.Windowed);var empty=Fixture();empty.rows=new BinderCardView[]{null,new BinderCardView()};empty.selectedCardId="HIDDEN-CANARY-NOT-SUPPLIED";Publish(empty);yield return WaitFrames(10);yield return Capture("1280x720-empty-synthetic.png");
        Check(Has("No projected slots") && !Has("HIDDEN-CANARY") && !controls.ContainsKey("save-physical"),"empty invalid rows never invent slots or expose stale selection");
        var unnamed=Fixture();unnamed.rows=new[]{new BinderCardView {id="synthetic:base:102:unstamped:nonholo",collectorNumber="102",owned=false,quantity=0}};unnamed.selectedCardId=unnamed.rows[0].id;unnamed.selectedCondition=null;unnamed.quantityText="";unnamed.backups=new string[0];unnamed.selectedBackupIndex=-1;Publish(unnamed);commands.Clear();yield return WaitFrames();
        Check(Has("Unnamed printing") && Has("synthetic:base:102:unstamped:nonholo"),"unnamed safe printing has readable fallback and exact full identity");
        Check(Has("No backups supplied") && BinderLayoutRules.Condition(view)=="unknown","empty backups and default unknown condition rendered safely");
        foreach(var condition in BinderLayoutRules.Conditions) {yield return Click(window,"condition:"+condition);Check(commands.Contains("condition:"+condition),"native exact physical condition "+condition);}
        yield return Click(window,"import-preview");Check(!commands.Contains("import-preview"),"native preview disabled without selected backup");
        yield return Click(window,"save-physical");Check(commands.Contains("save-physical") && view.quantityText=="" && view.rows[0].quantity==0,"empty quantity stays untrusted host draft on explicit confirmation");
        yield return Capture("1280x720-unnamed-synthetic.png");
        var reviewError=Fixture();reviewError.importSummary="SYNTHETIC retained reviewed summary";reviewError.error="SYNTHETIC reviewed import failure";Publish(reviewError);commands.Clear();yield return WaitFrames();yield return Click(window,"import-confirm");
        Check(commands.Count==0 && Has("reviewed import failure"),"native errored review cannot confirm and keeps summary error visible");
        yield return Click(window,"import-cancel");Check(commands.Contains("import-cancel") && view.importSummary=="SYNTHETIC retained reviewed summary","native errored review remains explicitly cancellable without draft mutation");
        Publish(new BinderView {open=false});labels.Clear();yield return WaitFrames();Check(labels.Count==0 && !binder.IsOpen,"closed overlay emits no content");
        Check(gameObject.GetComponents<AudioSource>().Length==0,"native binder leaves audio absent and unchanged");
        bool passed=checks.Count>0 && checks.TrueForAll(c=>c.passed);
        File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(new Report {passed=passed,checkCount=checks.Count,checks=checks.ToArray(),screenshots=screenshots.ToArray(),failure=failure},true));
        Application.Quit(passed?0:1);
    }
}
