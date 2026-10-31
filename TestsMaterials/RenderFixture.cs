// ISOLATED TEST ONLY. Exercises actual production private draw paths, no game actions or saves.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VintageCollector.Presentation;
using VintageCollector.Media;
using VintageCollector.Domain;
public sealed class RenderFixture : MonoBehaviour {
    [Serializable] sealed class Report { public bool passed; public int checkCount,screenshotCount; public string[] checks,screenshots; public string failure; }
    readonly List<string> checks=new List<string>(),shots=new List<string>();
    string failure,output; TabletopPresentation ui; Provider provider; string mode="wrapper";
    readonly Rect target=new Rect(100,70,400,620);
    OpeningCardView card=new OpeningCardView {id="synthetic",name="MUST NOT PAINT ON PRINT",finish="Holo",isNew=true,collectorNumber="TEST",kind="TEST",rarity="TEST"};
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    object Call(string n,params object[] args) { return typeof(TabletopPresentation).GetMethod(n,flags).Invoke(ui,args); }
    void OnGUI() {
        if(ui==null) return;
        Call("EnsureResources");
        if(mode=="wrapper") Call("DrawWrapper",target,true);
        else if(mode=="back") Call("DrawBack",target);
        else if(mode=="face") Call("DrawFace",target,card,0);
        else if(mode=="reference") GUI.DrawTexture(target,provider.face,ScaleMode.ScaleToFit,true);
        else {
            Call("OnGUI");
            if(mode=="inspection-reference") {
                var l=PresentationRules.Layout(Screen.width,Screen.height);var p=l.modal;
                var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(l.scale,l.scale,1));
                GUI.DrawTexture(new Rect(p.x+30,p.y+30,620*63f/88,620),provider.face,ScaleMode.ScaleToFit,true);GUI.matrix=old;
            }
        }
    }
    sealed class Provider : IPresentationMedia,IWrapperMetallicMedia {
        public Texture2D face,holo,nonholoFace,front,back,frontMask,backMask,cardBack;
        public bool masks,frontAvailable=true,wrongMask; public IPresentationMedia real;
        public readonly List<Texture2D> owned=new List<Texture2D>();
        public Texture2D CardFace(string id) { return face; }
        public Texture2D HoloMask(string id) { return masks?holo:null; }
        public Texture2D CardBack {get{return cardBack;}}
        public Texture2D WrapperFront {get{return frontAvailable?front:null;}}
        public Texture2D WrapperBack {get{return back;}}
        public Texture2D WrapperFrontMask {get{return masks?(wrongMask?Texture2D.whiteTexture:frontMask):null;}}
        public Texture2D WrapperBackMask {get{return masks?backMask:null;}}
        public AudioClip Sound(string action) {throw new Exception("Drawing must not request playback");}
        public int SuppliedAssetCount {get{return 7;}}
        public string StatusText {get{return real==null?"ORIGINAL SYNTHETIC RENDER FIXTURE":"PRIVATE SUPPLIED MATERIALS RENDER FIXTURE";}}
        public Texture2D Pattern(int w,int h,bool mask,bool wrapper,bool reverse=false) {
            var t=new Texture2D(w,h,TextureFormat.RGBA32,false,mask) {filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var p=new Color32[w*h];
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
                bool region=wrapper?x>w*.16f && x<w*.84f && y>h*.12f && y<h*.88f : x>w*.1f && x<w*.9f && y>h*.55f && y<h*.83f;
                if(mask) p[y*w+x]=region?new Color32(190,190,190,255):new Color32(0,0,0,255);
                else {
                    p[y*w+x]=new Color32((byte)(reverse?65:185),(byte)(75+x%50),(byte)(reverse?180:45),255);
                    if(y%37<4) p[y*w+x]=new Color32(230,223,202,255);
                }
            }
            t.SetPixels32(p); t.Apply(); owned.Add(t); return t;
        }
    }
    void Check(bool b,string name) { checks.Add((b?"PASS ":"FAIL ")+name); if(!b) throw new Exception(name); }
    void Phase(float value) { var m=(Phase3PresentationMotion)typeof(TabletopPresentation).GetField("motion",flags).GetValue(ui); m.Advance(value-m.Clock,true); }
    Rect Fit(Texture2D t) {Rect r=mode=="wrapper"?new Rect(target.x-8,target.y-8,target.width+16,target.height+16):target;float s=Mathf.Min(r.width/t.width,r.height/t.height);return new Rect(r.center.x-t.width*s/2,r.center.y-t.height*s/2,t.width*s,t.height*s);}
    Color Pixel(Texture2D t,int x,int y) {return t.GetPixel(x,t.height-y-1);}
    void Compare(Texture2D a,Texture2D b,Texture2D tex,Texture2D mask,bool changes,string name) {
        Rect r=Fit(tex); int inside=0,outside=0; float largest=0;
        for(int y=(int)target.y+2;y<target.yMax-2;y++) for(int x=(int)target.x+2;x<target.xMax-2;x++) {
            float u=(x+.5f-r.x)/r.width,v=1-(y+.5f-r.y)/r.height;
            Color m=mask==null?Color.black:mask.GetPixelBilinear(u,v);
            Color ac=Pixel(a,x,y),bc=Pixel(b,x,y); float d=Mathf.Max(Mathf.Abs(ac.r-bc.r),Mathf.Abs(ac.g-bc.g),Mathf.Abs(ac.b-bc.b));
            if(d==0) continue;
            // CPU GetPixelBilinear and GPU normalized texel centers differ at transition edges.
            // Classify the one-texel bilinear footprint explicitly; solid black outside it must be exact.
            float coverage=m.r*m.a;
            if(mask!=null) for(int oy=-1;oy<=1;oy++) for(int ox=-1;ox<=1;ox++) {
                Color edge=mask.GetPixelBilinear(u+ox/(float)mask.width,v+oy/(float)mask.height);
                coverage=Mathf.Max(coverage,edge.r*edge.a);
            }
            if(r.Contains(new Vector2(x,y)) && coverage>.001f) { inside++;largest=Mathf.Max(largest,d); } else outside++;
        }
        Check(outside==0,name+" black-mask/letterbox unchanged outside="+outside);
        Check(changes?inside>100:inside==0,name+" visible masked movement pixels="+inside);
        Check(largest<.19f,name+" print contrast/color retained maximum delta="+largest);
    }
    void ComparePrint(Texture2D a,Texture2D b,string name) {
        Rect r=Fit(provider.face);int changed=0;
        for(int y=(int)r.y+2;y<r.yMax-2;y++)for(int x=(int)r.x+2;x<r.xMax-2;x++) {
            Color ac=Pixel(a,x,y),bc=Pixel(b,x,y);
            if(Mathf.Max(Mathf.Abs(ac.r-bc.r),Mathf.Abs(ac.g-bc.g),Mathf.Abs(ac.b-bc.b))>.004f)changed++;
        }
        Check(changed==0,name+" entire fitted print unchanged vs neutral texture reference; HP/attacks/copyright/badge overwrite pixels="+changed);
    }
    IEnumerator Capture(string name,Action<Texture2D> use) {
        yield return new WaitForEndOfFrame(); var t=ScreenCapture.CaptureScreenshotAsTexture();
        try {File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());shots.Add(name+".png");use(t);}catch(Exception e){if(failure==null)failure=e.ToString();} finally{Destroy(t);}
    }
    IEnumerator Step(string name,Action action) { try{action();}catch(Exception e){failure=name+": "+e;} yield return null; }
    IEnumerator Start() {
        Application.runInBackground=true; output=Environment.GetEnvironmentVariable("MATERIALS_RENDER_OUT");
        ui=gameObject.AddComponent<TabletopPresentation>();ui.enabled=false;
        provider=new Provider();
        provider.face=provider.Pattern(600,825,false,false);provider.holo=provider.Pattern(600,825,true,false);
        provider.front=provider.Pattern(260,475,false,true);provider.frontMask=provider.Pattern(260,475,true,true);
        provider.back=provider.Pattern(475,260,false,true,true);provider.backMask=provider.Pattern(475,260,true,true);
        provider.cardBack=provider.Pattern(600,825,false,false,true);ui.Media=provider;
        ui.View=new OpeningView {stage="sealed",transactionId="fixture"};
        for(int i=0;i<8;i++)yield return null;
        Texture2D baseline=null,phaseA=null;
        yield return Capture("synthetic-wrapper-front-baseline",t=>baseline=Instantiate(t));
        provider.masks=true;Phase(2);
        yield return Capture("synthetic-wrapper-front-metallic",t=> {Compare(baseline,t,provider.front,provider.frontMask,true,"Front reflection");phaseA=Instantiate(t);});
        if(failure==null) {
            Phase(5);yield return Capture("synthetic-wrapper-front-moving",t=>Compare(phaseA,t,provider.front,provider.frontMask,true,"Moving neutral front reflection"));
            provider.wrongMask=true;yield return Capture("synthetic-wrapper-wrong-mask",t=>Compare(baseline,t,provider.front,provider.frontMask,false,"Mismatched wrapper mask fails closed"));provider.wrongMask=false;
            var savedMask=provider.frontMask;provider.frontMask=null;
            yield return Capture("synthetic-front-missing-mask",t=>Compare(baseline,t,provider.front,savedMask,false,"Front does not borrow available back mask"));provider.frontMask=savedMask;
            Destroy(baseline);Destroy(phaseA);baseline=null;phaseA=null;
            provider.frontAvailable=false;provider.masks=false;
            yield return Capture("synthetic-wrapper-back-baseline",t=>baseline=Instantiate(t));
            provider.masks=true;Phase(8);
            yield return Capture("synthetic-wrapper-back-metallic",t=>Compare(baseline,t,provider.back,provider.backMask,true,"Back-only fallback reflection matches chosen wide back"));
            Destroy(baseline);baseline=null;mode="reference";provider.masks=false;
            yield return Capture("synthetic-face-neutral-reference",t=>baseline=Instantiate(t));
            mode="face";
            yield return Capture("synthetic-full-face-baseline",t=>ComparePrint(baseline,t,"Synthetic NEW face"));
            card.isNew=false;
            yield return Capture("synthetic-duplicate-clean-face",t=>{ComparePrint(baseline,t,"Synthetic duplicate face");Destroy(baseline);baseline=Instantiate(t);});
            provider.masks=true;Phase(11);
            yield return Capture("synthetic-holo-face",t=>Compare(baseline,t,provider.face,provider.holo,true,"Holo illustration-only"));
            card.finish="Non-holo";Phase(14);
            yield return Capture("synthetic-nonholo-face",t=>Compare(baseline,t,provider.face,provider.holo,false,"Nonholo never foils"));
            Destroy(baseline);baseline=null;
        }
        if(failure==null) yield return Step("Native invariants",()=> {
            Check(Resources.Load<Shader>("Presentation/WrapperMetallic")!=null,"Wrapper shader retained via Resources in release player");
            Check(gameObject.GetComponents<AudioSource>().Length==0 && ui.CommittedCueCount==0,"Rendering creates no AudioSource or automatic cue");
            var m=(Phase3PresentationMotion)typeof(TabletopPresentation).GetField("motion",flags).GetValue(ui);
            foreach(string gate in new[]{"paused","focus","error","binder"}) {
                ui.View=new OpeningView {stage=gate=="error"?"error":"sealed",paused=gate=="paused"};
                if(gate=="focus")Call("OnApplicationFocus",false);if(gate=="binder")ui.CollectionInputBlocked=true;
                float before=m.Clock;Call("Update");Check(m.Clock==before,gate+" freezes existing material clock");
                if(gate=="focus")Call("OnApplicationFocus",true);ui.CollectionInputBlocked=false;
            }
            ui.Media=null;Check(provider.face!=null,"Renderer detach preserves externally owned textures");ui.Media=provider;
        });
        if(failure==null) yield return ActualSupplied();
        if(baseline!=null)Destroy(baseline);if(phaseA!=null)Destroy(phaseA);
        ui.Media=null;
        var ownedMaterial=typeof(TabletopPresentation).GetField("wrapperMetallicMaterial",flags).GetValue(ui) as Material;
        Destroy(ui);yield return null;yield return new WaitForEndOfFrame();
        try {Check(ownedMaterial==null,"Actual deferred renderer destruction disposes owned wrapper material");Check(provider.owned[0]!=null,"Renderer destruction never destroys external supplied textures");}catch(Exception e){if(failure==null)failure=e.ToString();}
        foreach(var t in provider.owned)Destroy(t);
        File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),screenshotCount=shots.Count,screenshots=shots.ToArray(),failure=failure},true));
        Application.Quit(failure==null?0:1);
    }
    IEnumerator ActualSupplied() {
        // Optional actual inputs are staged locally by runner; absence is explicitly reported, not invented.
        string root=Environment.GetEnvironmentVariable("MATERIALS_RENDER_ROOT");
        if(!File.Exists(Path.Combine(root,"Assets/Supplied/BaseSetMaterials/Manifest.json"))) {checks.Add("Actual supplied materials unavailable: synthetic proof only");yield break;}
        PresentationMediaLibrary library=null;
        try {
            var cat=Catalog.FromData(JsonUtility.FromJson<CatalogData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"))));
            var load=typeof(PresentationMediaLibrary).GetMethod("Load");
            Check(load.GetParameters().Length==5,"Actual full-set Load API available");
            library=(PresentationMediaLibrary)load.Invoke(null,new object[]{root,cat,"Assets/Supplied/BaseSetMaterials/Manifest.json",false,true});
            Check(library.Warnings.Count==0,"Actual full-set bindings load without warnings");
            foreach(var c in cat.Cards)if(c.Finish=="holo"){card.id=c.Id;provider.face=library.CardFace(c.Id);provider.holo=library.HoloMask(c.Id);break;}
            foreach(var c in cat.Cards)if(c.Finish!="holo"){provider.nonholoFace=library.CardFace(c.Id);break;}
            var metallic=(IWrapperMetallicMedia)library;
            provider.front=library.WrapperFront;provider.back=library.WrapperBack;provider.frontMask=metallic.WrapperFrontMask;provider.backMask=metallic.WrapperBackMask;provider.cardBack=library.CardBack;provider.real=library;
            Check(provider.face!=null && provider.holo!=null && provider.front!=null && provider.back!=null && provider.cardBack!=null,"Actual supplied representative face/mask/wrappers/back present");
        }catch(Exception e){failure=e.ToString();}
        if(failure==null) {
            Texture2D baseline=null,a=null;
            mode="reference";provider.masks=false;card.finish="Holo";card.isNew=true;
            yield return Capture("actual-face-neutral-reference",t=>baseline=Instantiate(t));
            mode="face";
            yield return Capture("actual-full-face-print-baseline",t=>ComparePrint(baseline,t,"Actual NEW supplied face"));
            card.isNew=false;
            yield return Capture("actual-duplicate-clean-print",t=>{ComparePrint(baseline,t,"Actual duplicate supplied face");Destroy(baseline);baseline=Instantiate(t);});
            provider.masks=true;Phase(17);
            yield return Capture("actual-holo-mask",t=>{Compare(baseline,t,provider.face,provider.holo,true,"Actual holo");a=Instantiate(t);});
            Phase(20);yield return Capture("actual-holo-moving",t=>Compare(a,t,provider.face,provider.holo,true,"Actual holo movement"));
            card.finish="Non-holo";yield return Capture("actual-nonholo-clean-print",t=>Compare(baseline,t,provider.face,provider.holo,false,"Actual nonholo clean print"));
            Destroy(baseline);Destroy(a);baseline=null;a=null;
            var holoFace=provider.face;provider.face=provider.nonholoFace;Check(provider.face!=null,"Actual distinct nonholo catalog face loaded");
            mode="reference";yield return Capture("actual-distinct-nonholo-reference",t=>baseline=Instantiate(t));
            mode="face";yield return Capture("actual-distinct-nonholo-print",t=>{ComparePrint(baseline,t,"Actual distinct nonholo");Destroy(baseline);baseline=Instantiate(t);});
            Phase(22);yield return Capture("actual-distinct-nonholo-later",t=>Compare(baseline,t,provider.face,provider.holo,false,"Actual distinct nonholo no movement"));
            Destroy(baseline);baseline=null;provider.face=holoFace;
            foreach(bool front in new[]{true,false}) {
                mode="wrapper";provider.frontAvailable=front;provider.masks=false;
                string name=front?"front":"back";
                yield return Capture("actual-wrapper-"+name+"-baseline",t=>baseline=Instantiate(t));
                provider.masks=true;Phase(front?23:29);
                yield return Capture("actual-wrapper-"+name+"-metallic",t=>{Compare(baseline,t,front?provider.front:provider.back,front?provider.frontMask:provider.backMask,true,"Actual wrapper "+name);a=Instantiate(t);});
                Phase(front?26:32);
                yield return Capture("actual-wrapper-"+name+"-moving",t=>Compare(a,t,front?provider.front:provider.back,front?provider.frontMask:provider.backMask,true,"Actual moving wrapper "+name));
                Destroy(baseline);Destroy(a);baseline=null;a=null;
            }
            mode="back";yield return Capture("actual-card-back",t=>Check(provider.cardBack.width==600 && provider.cardBack.height==825,"Actual back original dimensions preserved"));
            mode="full";provider.masks=false;provider.frontAvailable=true;
            var cards=new OpeningCardView[11];for(int i=0;i<11;i++)cards[i]=new OpeningCardView {id=card.id,name="Supplied print",finish=i==0?"Holo":"Non-holo",isNew=true};
            ui.View=new OpeningView {stage="revealing",transactionId="fixture",cards=cards,revealedCount=1,inspectIndex=0};
            yield return Capture("actual-production-clean-inspection",t=>{Check(ui.CommittedCueCount==0,"Actual inspection remains passive");baseline=Instantiate(t);});
            mode="inspection-reference";
            yield return Capture("actual-production-inspection-neutral-reference",t=> {
                var l=PresentationRules.Layout(t.width,t.height);var p=l.modal;
                Rect r=new Rect((p.x+30)*l.scale,(p.y+30)*l.scale,620*63f/88*l.scale,620*l.scale);
                float s=Mathf.Min(r.width/provider.face.width,r.height/provider.face.height);float w=provider.face.width*s,h=provider.face.height*s;
                r=new Rect(r.center.x-w/2,r.center.y-h/2,w,h);int changed=0;
                for(int y=(int)r.y+2;y<r.yMax-2;y++)for(int x=(int)r.x+2;x<r.xMax-2;x++) {
                    Color ac=Pixel(baseline,x,y),bc=Pixel(t,x,y);
                    if(Mathf.Max(Mathf.Abs(ac.r-bc.r),Mathf.Abs(ac.g-bc.g),Mathf.Abs(ac.b-bc.b))>.004f)changed++;
                }
                Check(changed==0,"Actual production inspection entire fitted face equals neutral print reference; no HP/attack/copyright overlay pixels="+changed);
            });Destroy(baseline);baseline=null;
            mode="full";provider.masks=true;
            ui.View=new OpeningView {stage="results",transactionId="fixture",cards=cards,revealedCount=11};
            yield return Capture("actual-production-results-fixture",t=>Check(ui.CommittedCueCount==0,"Explicit results fixture remains passive; not a generated pack"));
        }
        ui.Media=null;if(library!=null)library.Dispose();
    }
}
