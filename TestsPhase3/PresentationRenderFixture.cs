// ISOLATED TEST ONLY: original geometric canary faces; not user-supplied/branded materials.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using VintageCollector.Presentation;
using VintageCollector.Media;

public sealed class PresentationRenderFixture : MonoBehaviour
{
    [Serializable] private sealed class Report { public bool passed; public int checkCount, screenshotCount; public string[] checks, screenshots; public string failure; }
    private readonly List<string> checks=new List<string>(), screenshots=new List<string>(), labels=new List<string>();
    private string failure,output;
    private TabletopPresentation presentation;
    private FakeMedia fake;
    private OpeningView view;
    private Vector2 lastPointer;
    private delegate bool WindowVisitor(IntPtr window,IntPtr state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor,IntPtr state);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll")] private static extern int GetClassName(IntPtr window,StringBuilder name,int capacity);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window,uint message,IntPtr wparam,IntPtr lparam);
    private static IntPtr OwnedWindow() {
        uint pid=(uint)System.Diagnostics.Process.GetCurrentProcess().Id; IntPtr found=IntPtr.Zero;
        EnumWindows((window,state)=> { uint process; GetWindowThreadProcessId(window,out process); var name=new StringBuilder(128); GetClassName(window,name,128);
            if(process==pid && name.ToString()=="UnityWndClass") { found=window; return false; } return true;
        },IntPtr.Zero); return found;
    }
    private static void MoveMessage(IntPtr window,int x,int y) { if(window==IntPtr.Zero || !PostMessage(window,0x200,IntPtr.Zero,new IntPtr((y<<16)|(x&0xffff)))) throw new Exception("Owned-window mouse message failed"); }
    private void OnGUI() { lastPointer=Event.current.mousePosition; }
    private sealed class FakeMedia : IPresentationMedia
    {
        public readonly List<string> faces=new List<string>(), masks=new List<string>();
        public readonly Texture2D face,mask,back,wrapper,wrapperBack;
        public bool enableMask;
        public bool frontAvailable=true, enableAudio, misalignedMask;
        private readonly Texture2D wrongMask=new Texture2D(8,8);
        public readonly AudioClip clip;
        public FakeMedia() {
            clip=AudioClip.Create("TEST ONLY original synthetic foley",12000,1,24000,false);
            var samples=new float[12000]; for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*0.09f)*0.07f*(1-i/(float)samples.Length);
            clip.SetData(samples,0);
            face=Image(new Color32(193,38,78,255),true);
            mask=new Texture2D(252,352,TextureFormat.RGBA32,false);
            var pixels=new Color[252*352];
            for(int y=0;y<352;y++) for(int x=0;x<252;x++) pixels[y*252+x]=(x>32 && x<220 && y>170 && y<280)?Color.white:Color.clear;
            mask.SetPixels(pixels); mask.Apply();
            back=Image(new Color32(34,85,215,255),false);
            wrapper=Image(new Color32(217,98,25,255),false);
            wrapperBack=Image(new Color32(107,54,174,255),false);
        }
        private static Texture2D Image(Color32 c,bool print) {
            var t=new Texture2D(252,352,TextureFormat.RGBA32,false) {filterMode=FilterMode.Point,name="TEST ONLY original canary"};
            var p=new Color32[252*352]; for(int y=0;y<352;y++) for(int x=0;x<252;x++) {
                p[y*252+x]=c;
                if(print && x>20 && x<232 && y>170 && y<290) p[y*252+x]=new Color32(28,155,95,255);
                if(print && x>20 && x<232 && ((y>310 && y<324) || (y>33 && y<46))) p[y*252+x]=new Color32(247,240,211,255);
            }
            t.SetPixels32(p); t.Apply(); return t;
        }
        public Texture2D CardFace(string id) { faces.Add(id); return id=="original-synthetic-canary-0"?face:null; }
        public Texture2D HoloMask(string id) { masks.Add(id); return enableMask && id=="original-synthetic-canary-0"?(misalignedMask?wrongMask:mask):null; }
        public Texture2D CardBack { get { return back; } }
        public Texture2D WrapperFront { get { return frontAvailable?wrapper:null; } }
        public Texture2D WrapperBack { get { return wrapperBack; } }
        public AudioClip Sound(string action) { return enableAudio?clip:null; }
        public string StatusText { get { return "TEST ONLY synthetic canaries / no supplied user materials"; } }
        public int SuppliedAssetCount { get { return 5; } }
        public void Cleanup() { UnityEngine.Object.Destroy(wrongMask); UnityEngine.Object.Destroy(clip); UnityEngine.Object.Destroy(face); UnityEngine.Object.Destroy(mask); UnityEngine.Object.Destroy(back); UnityEngine.Object.Destroy(wrapper); UnityEngine.Object.Destroy(wrapperBack); }
    }
    private void Assert(bool ok,string name) { if(!ok) throw new Exception(name); checks.Add(name); }
    private void Awake() { Application.runInBackground=true; }
    private Color Pixel(Texture2D t,float x,float y) { return t.GetPixel(Mathf.Clamp((int)x,0,t.width-1),Mathf.Clamp(t.height-1-(int)y,0,t.height-1)); }
    private bool Near(Color a,Color b) { return Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)<0.065f; }
    private void Save(Texture2D t,string name) { File.WriteAllBytes(Path.Combine(output,name),t.EncodeToPNG()); screenshots.Add(name); }
    private void WriteReport() { File.WriteAllText(Path.Combine(output,"render-report.json"),JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),screenshotCount=screenshots.Count,screenshots=screenshots.ToArray(),failure=failure},true)); }
    private bool SamePixels(Texture2D a,Texture2D b) {
        var ap=a.GetPixels32(); var bp=b.GetPixels32(); if(ap.Length!=bp.Length) return false;
        for(int i=0;i<ap.Length;i++) if(!ap[i].Equals(bp[i])) return false; return true;
    }
    private void CompareFace(Texture2D baseline,Texture2D current,bool expectArtChange,string name)
    {
        var l=PresentationRules.Layout(current.width,current.height); var b=l.heroCard; float s=l.scale;
        var art=new Rect((b.x+b.width*.08f)*s-2,(b.y+b.height*.17f)*s-2,b.width*.84f*s+4,b.height*.37f*s+4);
        int inside=0,outside=0;
        for(int y=(int)((b.y+2)*s);y<(b.y+b.height-2)*s;y++) for(int x=(int)((b.x+2)*s);x<(b.x+b.width-2)*s;x++) {
            if(Near(Pixel(baseline,x,y),Pixel(current,x,y))) continue;
            if(art.Contains(new Vector2(x,y))) inside++; else outside++;
        }
        Assert(outside==0 && (expectArtChange?inside>100:inside==0),name+" changedArt="+inside+" changedPrint="+outside);
    }
    private void CheckNativeAudio(OpeningCardView[] cards)
    {
        fake.enableAudio=true;
        var source=presentation.FoleySource;
        Assert(!source.isPlaying && presentation.CommittedCueCount==0,"Actual player has no startup/uncommitted audio");
        Assert(!source.playOnAwake && !source.loop && source.spatialBlend==0 && gameObject.GetComponentsInChildren<AudioSource>(true).Length==1,"Actual player owns exactly one nonlooping 2D source");
        var samples=new float[fake.clip.samples]; fake.clip.GetData(samples,0); float peak=0; bool finite=true;
        foreach(float sample in samples) { peak=Mathf.Max(peak,Mathf.Abs(sample)); finite &= !float.IsNaN(sample) && !float.IsInfinity(sample); }
        Assert(finite && peak>0.01f && peak<0.1f && fake.clip.samples==12000,"Actual player native clip data finite original nonzero bounded samples");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-a"}; presentation.NotifyCommitted("generate");
        Assert(source.isPlaying && source.clip==fake.clip && presentation.CommittedCueCount==1,"Actual engine reports committed generate playing (not physical audibility)");
        presentation.NotifyCommitted("generate"); presentation.NotifyCommitted("next");
        Assert(presentation.CommittedCueCount==1,"Player duplicate and invalid commit silent");
        presentation.View=new OpeningView {stage="revealing",transactionId="player-a",cards=cards}; presentation.NotifyCommitted("open");
        presentation.View=new OpeningView {stage="revealing",transactionId="player-a",cards=cards,revealedCount=1}; presentation.NotifyCommitted("next");
        presentation.View=new OpeningView {stage="results",transactionId="player-a",cards=cards,revealedCount=11}; presentation.NotifyCommitted("skip"); presentation.NotifyCommitted("skip");
        Assert(presentation.CommittedCueCount==4 && presentation.LastCommittedCue=="skip" && source.isPlaying,"Actual open next skip each one native playback request, no Skip burst");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-b",volume=0}; presentation.NotifyCommitted("generate");
        Assert(!source.isPlaying && source.clip==null && source.volume==0,"Native mute stops immediately");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-b",volume=.8f}; presentation.NotifyCommitted("generate");
        Assert(!source.isPlaying && presentation.CommittedCueCount==4 && source.volume==.8f,"Native unmute applies volume without replay");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-c"}; presentation.NotifyCommitted("generate");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-c",paused=true};
        Assert(!source.isPlaying && source.clip==null,"Native pause stops current clip immediately");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-c"}; presentation.NotifyCommitted("generate");
        Assert(!source.isPlaying && presentation.CommittedCueCount==5,"Native resume never replays");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-d"}; presentation.NotifyCommitted("generate"); presentation.View=new OpeningView {stage="error"};
        Assert(!source.isPlaying && source.clip==null,"Native error stops clip immediately");
        presentation.View=new OpeningView {stage="sealed",transactionId="player-e"}; presentation.NotifyCommitted("generate"); presentation.Media=null;
        Assert(!source.isPlaying && source.clip==null && fake.clip.samples==12000,"Native provider detach stops without destroying externally owned clip");
        presentation.Media=fake; fake.enableAudio=false;
    }
    private IEnumerator NativeClick(IntPtr window,PresentationBox box) {
        float s=PresentationRules.Layout(Screen.width,Screen.height).scale;
        int x=(int)((box.x+box.width/2)*s), y=(int)((box.y+box.height/2)*s); IntPtr point=new IntPtr((y<<16)|(x&0xffff));
        MoveMessage(window,x,y); PostMessage(window,0x201,new IntPtr(1),point);
        for(int j=0;j<3;j++) yield return null;
        PostMessage(window,0x202,IntPtr.Zero,point);
        for(int j=0;j<4;j++) yield return null;
    }
    private IEnumerator NativeEscape(IntPtr window) {
        PostMessage(window,0x100,new IntPtr(0x1b),new IntPtr(1));
        for(int j=0;j<3;j++) yield return null;
        PostMessage(window,0x101,new IntPtr(0x1b),new IntPtr(unchecked((int)0xc0000001)));
        for(int j=0;j<4;j++) yield return null;
    }
    private IEnumerator Start()
    {
        output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../phase3-presentation/render")); Directory.CreateDirectory(output);
        presentation=gameObject.AddComponent<TabletopPresentation>();
        typeof(TabletopPresentation).GetProperty("TextObserver",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(presentation,(Action<string>)(s=>labels.Add(s)));
        fake=new FakeMedia(); presentation.Media=fake;
        var cards=new OpeningCardView[11]; for(int i=0;i<11;i++) cards[i]=new OpeningCardView {id="original-synthetic-canary-"+i,name=i==0?"Original canary":"HIDDEN-CANARY-"+i,finish=i==0?"Holo":"Non-holo",collectorNumber=(i+1)+"/11",kind="Synthetic",rarity="Test",isNew=true};
        view=new OpeningView {stage="revealing",transactionId="test-only",cards=cards,revealedCount=1,statusLine="ISOLATED SYNTHETIC FIXTURE — no game, saves, RNG or awards."}; presentation.View=view;
        Screen.SetResolution(1600,900,FullScreenMode.Windowed); for(int j=0;j<10;j++) yield return null;
        yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        try {
            var b=PresentationRules.Layout(Screen.width,Screen.height).heroCard;
            float s=PresentationRules.Layout(Screen.width,Screen.height).scale;
            Assert(fake.faces.Contains(cards[0].id),"Provider receives exact stable printing ID");
            Assert(!fake.faces.Exists(id=>id!="original-synthetic-canary-0") && !labels.Exists(t=>t.Contains("HIDDEN-CANARY-")),"No hidden face lookup or hidden label");
            Assert(Near(Pixel(image,(b.x+b.width*.5f)*s,(b.y+b.height*.5f)*s),new Color32(28,155,95,255)),"Actual supplied face art pixels visible");
            Assert(Near(Pixel(image,(b.x+b.width*.5f)*s,(b.y+b.height*.90f)*s),new Color32(247,240,211,255)),"Full printed lower band unobscured by NEW badge");
            Save(image,"1600x900-real-face-no-mask.png");
        } catch(Exception e) { failure=e.ToString(); }
        if(failure!=null) { Destroy(image); WriteReport(); presentation.Media=null; fake.Cleanup(); Application.Quit(1); yield break; }
        var nativeWindow=OwnedWindow(); var hoverLayout=PresentationRules.Layout(Screen.width,Screen.height); var hero=hoverLayout.heroCard;
        MoveMessage(nativeWindow,(int)((hero.x+hero.width/2)*hoverLayout.scale),(int)((hero.y+hero.height/2)*hoverLayout.scale));
        for(int j=0;j<5;j++) yield return null;
        yield return new WaitForEndOfFrame();
        var hovered=ScreenCapture.CaptureScreenshotAsTexture();
        try {
            Assert(new Rect(hero.x*hoverLayout.scale,hero.y*hoverLayout.scale,hero.width*hoverLayout.scale,hero.height*hoverLayout.scale).Contains(lastPointer),"Actual owned-window native hover reaches original accepted face hitbox");
            Assert(Near(Pixel(hovered,(hero.x+1)*hoverLayout.scale,(hero.y+hero.height/2)*hoverLayout.scale),new Color32(193,38,78,255)),"Hover affordance never overprints supplied border");
            Save(hovered,"1600x900-hover-print-unobscured.png");
        } catch(Exception e) { failure=e.ToString(); }
        Destroy(hovered); MoveMessage(nativeWindow,8,8);
        if(failure!=null) { Destroy(image); WriteReport(); presentation.Media=null; fake.Cleanup(); Application.Quit(1); yield break; }
        fake.enableMask=true;
        for(int j=0;j<4;j++) yield return null;
        yield return new WaitForEndOfFrame();
        var masked=ScreenCapture.CaptureScreenshotAsTexture();
        try {
            Assert(Resources.Load<Shader>("Presentation/Phase3Foil")!=null,"Foil shader retained in actual built player Resources");
            Assert(fake.masks.Contains(cards[0].id),"Holo requests explicit aligned stable-ID mask");
            CompareFace(image,masked,true,"Aligned supplied mask affects only original synthetic art region");
            Save(masked,"1600x900-real-face-aligned-mask.png");
        } catch(Exception e) { failure=e.ToString(); }
        Destroy(masked);
        var motionField=typeof(TabletopPresentation).GetField("motion",BindingFlags.Instance|BindingFlags.NonPublic);
        if(motionField==null) { failure="Renderer must consume deterministic presentation motion"; WriteReport(); presentation.Media=null; fake.Cleanup(); Application.Quit(1); yield break; }
        var motion=motionField.GetValue(presentation); var clock=motion.GetType().GetProperty("Clock");
        float runningClock=(float)clock.GetValue(motion,null);
        for(int j=0;j<8;j++) yield return null;
        yield return new WaitForEndOfFrame();
        try { Assert((float)clock.GetValue(motion,null)>runningClock,"Native Update advances passive presentation clock"); }
        catch(Exception e) { failure=e.ToString(); }
        view.paused=true; presentation.View=view;
        for(int j=0;j<3;j++) yield return null;
        yield return new WaitForEndOfFrame();
        var paused=ScreenCapture.CaptureScreenshotAsTexture(); float frozen=(float)clock.GetValue(motion,null);
        for(int j=0;j<10;j++) yield return null;
        yield return new WaitForEndOfFrame();
        var pausedLater=ScreenCapture.CaptureScreenshotAsTexture();
        try {
            Assert((float)clock.GetValue(motion,null)==frozen,"Native pause freezes clock at exact phase");
            Assert(SamePixels(paused,pausedLater),"Actual paused GUI and foil pixels remain frozen across frames");
            Save(pausedLater,"1600x900-paused-frozen.png");
        } catch(Exception e) { failure=e.ToString(); }
        Destroy(paused); Destroy(pausedLater); view.paused=false; presentation.View=view;
        cards[0].finish="Non-holo";
        for(int j=0;j<3;j++) yield return null;
        yield return new WaitForEndOfFrame();
        var plain=ScreenCapture.CaptureScreenshotAsTexture();
        try { CompareFace(image,plain,false,"Non-holo supplied face has no mask overlay"); }
        catch(Exception e) { failure=e.ToString(); }
        Destroy(plain);
        cards[0].finish="Holo"; fake.misalignedMask=true;
        for(int j=0;j<3;j++) yield return null;
        yield return new WaitForEndOfFrame();
        var mismatch=ScreenCapture.CaptureScreenshotAsTexture();
        try { CompareFace(image,mismatch,false,"Misaligned provider mask fails closed without overlay"); }
        catch(Exception e) { failure=e.ToString(); }
        fake.misalignedMask=false; Destroy(mismatch); Destroy(image);
        foreach(string role in new[]{"wrapper-front","wrapper-back","card-back"}) {
            view.stage=role=="card-back"?"revealing":"sealed"; view.revealedCount=0; fake.frontAvailable=role!="wrapper-back";
            labels.Clear(); for(int j=0;j<3;j++) yield return null;
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try {
                var l=PresentationRules.Layout(texture.width,texture.height); var b=l.heroCard;
                Color expected=role=="card-back"?new Color32(34,85,215,255):role=="wrapper-front"?new Color32(217,98,25,255):new Color32(107,54,174,255);
                Assert(Near(Pixel(texture,(b.x+b.width*.5f)*l.scale,(b.y+b.height*.5f)*l.scale),expected),"Native optional "+role+" visible without local overprint");
                Assert(labels.Exists(t=>t.Contains("TEST ONLY synthetic canaries")),"Provider status accurately labelled in actual GUI "+role);
                Save(texture,"1600x900-"+role+".png");
            } catch(Exception e) { failure=e.ToString(); }
            Destroy(texture);
            if(failure!=null) break;
        }
        var nativeCommands=new List<string>(); presentation.Command += command=>nativeCommands.Add(command);
        foreach(var size in new[]{new[]{1280,720},new[]{2560,1080},new[]{1280,360}}) {
            Screen.SetResolution(size[0],size[1],FullScreenMode.Windowed); for(int j=0;j<8;j++) yield return null;
            presentation.Media=fake;
            foreach(string state in new[]{"ready","sealed","revealing","inspection","invalid-inspection","results","pause","error"}) {
                view.stage=state=="inspection" || state=="invalid-inspection"?"revealing":state=="pause"?"results":state;
                view.paused=state=="pause"; view.inspectIndex=state=="inspection"?0:state=="invalid-inspection"?10:-1;
                view.revealedCount=view.stage=="results"?11:1;
                view.errorMessage="TEST ONLY simulated storage failure. No saves or awards exist in this fixture.";
                for(int i=0;i<11;i++) cards[i].name=i<PresentationRules.VisibleCount(view)?"Original synthetic "+i:"HIDDEN-CANARY-"+i;
                presentation.View=view; labels.Clear(); fake.faces.Clear();
                for(int j=0;j<4;j++) yield return null;
                yield return new WaitForEndOfFrame();
                var screen=ScreenCapture.CaptureScreenshotAsTexture();
                try {
                    Assert(labels.Count>0 && !labels.Exists(t=>t.Contains("HIDDEN-CANARY-")),"Actual hidden-name policy "+state+" "+size[0]+"x"+size[1]);
                    int visible=PresentationRules.VisibleCount(view); bool hiddenLookup=false;
                    foreach(string id in fake.faces) { int index=int.Parse(id.Substring(id.LastIndexOf('-')+1)); if(index>=visible) hiddenLookup=true; }
                    Assert(!hiddenLookup,"No hidden media lookup "+state+" "+size[0]+"x"+size[1]);
                    if(state=="pause") Assert(labels.Contains("Resume") && labels.Contains("Mute") && labels.Contains("Quit"),"Rendered pause controls reachable "+size[0]+"x"+size[1]);
                    if(state=="inspection" || state=="invalid-inspection") Assert(labels.Contains("Back to table"),"Rendered inspection Back reachable "+size[0]+"x"+size[1]);
                    if(state=="results") Assert(labels.Exists(t=>t.Contains("11 revealed")) && labels.Contains("Original synthetic 10"),"Ordered eleven result positions rendered "+size[0]+"x"+size[1]);
                    var layout=PresentationRules.Layout(screen.width,screen.height);
                    foreach(var box in new[]{layout.back,layout.resume,layout.primary,layout.pause,layout.quit})
                        Assert(box.x*layout.scale>=0 && box.y*layout.scale>=0 && (box.x+box.width)*layout.scale<=screen.width+.01f && (box.y+box.height)*layout.scale<=screen.height+.01f,"Actual window control bounds "+state+" "+size[0]+"x"+size[1]+" "+checks.Count);
                    Save(screen,size[0]+"x"+size[1]+"-"+state+".png");
                } catch(Exception e) { failure=e.ToString(); }
                Destroy(screen);
                if(failure!=null) break;
            }
            if(failure!=null) break;
            presentation.Media=null; view.stage="revealing"; view.revealedCount=1; view.inspectIndex=-1; view.paused=false;
            cards[0].name="Original generic canary"; presentation.View=view; labels.Clear();
            for(int j=0;j<4;j++) yield return null;
            yield return new WaitForEndOfFrame();
            var genericFirst=ScreenCapture.CaptureScreenshotAsTexture();
            motion.GetType().GetMethod("Advance").Invoke(motion,new object[]{2f,true});
            yield return null; yield return new WaitForEndOfFrame();
            var genericSecond=ScreenCapture.CaptureScreenshotAsTexture();
            try { CompareFace(genericFirst,genericSecond,true,"Animated generic foil remains strictly art-bounded "+size[0]+"x"+size[1]); Save(genericSecond,size[0]+"x"+size[1]+"-generic-foil.png"); }
            catch(Exception e) { failure=e.ToString(); }
            Destroy(genericFirst); Destroy(genericSecond);
            if(failure!=null) break;
            var inputLayout=PresentationRules.Layout(Screen.width,Screen.height); nativeCommands.Clear();
            yield return NativeClick(nativeWindow,inputLayout.heroCard);
            try { Assert(nativeCommands.Count==1 && nativeCommands[0]=="inspect:0" && view.inspectIndex==-1,"Native face click emits exact inspection without view mutation "+size[0]+"x"+size[1]); }
            catch(Exception e) { failure=e.ToString(); }
            view.inspectIndex=0; presentation.View=view; nativeCommands.Clear(); yield return NativeClick(nativeWindow,inputLayout.back);
            try { Assert(nativeCommands.Count==1 && nativeCommands[0]=="close-inspect","Native inspection Back remains reachable "+size[0]+"x"+size[1]); }
            catch(Exception e) { failure=e.ToString(); }
            nativeCommands.Clear(); yield return NativeEscape(nativeWindow);
            try { Assert(nativeCommands.Count==1 && nativeCommands[0]=="close-inspect","Native Escape exits inspection "+size[0]+"x"+size[1]); }
            catch(Exception e) { failure=e.ToString(); }
            view.inspectIndex=-1; presentation.View=view; nativeCommands.Clear(); yield return NativeEscape(nativeWindow);
            try { Assert(nativeCommands.Count==1 && nativeCommands[0]=="pause","Native Escape requests pause "+size[0]+"x"+size[1]); }
            catch(Exception e) { failure=e.ToString(); }
            view.paused=true; presentation.View=view; nativeCommands.Clear(); yield return NativeEscape(nativeWindow);
            try { Assert(nativeCommands.Count==0,"Native paused Escape never silently resumes "+size[0]+"x"+size[1]); }
            catch(Exception e) { failure=e.ToString(); }
            yield return NativeClick(nativeWindow,inputLayout.resume);
            try { Assert(nativeCommands.Count==1 && nativeCommands[0]=="resume" && presentation.CommittedCueCount==0,"Native Resume clickable without cue replay "+size[0]+"x"+size[1]); }
            catch(Exception e) { failure=e.ToString(); }
            view.paused=false; presentation.View=view; MoveMessage(nativeWindow,8,8);
            if(failure!=null) break;
        }
        presentation.Media=fake;
        view.paused=false; view.inspectIndex=-1;
        view.stage="results"; view.revealedCount=11;
        for(int i=0;i<11;i++) cards[i].name="Original synthetic "+i;
        labels.Clear(); for(int j=0;j<3;j++) yield return null;
        yield return new WaitForEndOfFrame();
        try {
            Assert(labels.Exists(t=>t.Contains("TEST ONLY synthetic canaries")),"Results retains accurate provider media status");
            CheckNativeAudio(cards);
        } catch(Exception e) { failure=e.ToString(); }
        WriteReport(); presentation.Media=null; fake.Cleanup(); Application.Quit(failure==null?0:1);
    }
}
