// Dedicated isolated GPU player, never attached to the main project scene.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VintageCollector.Presentation;
public sealed class SurfaceGpuFixture : MonoBehaviour {
    [Serializable] public sealed class Asset {public string role,cardId,relativePath;}
    [Serializable] public sealed class Manifest {public Asset[] assets;}
    [Serializable] public sealed class Report {public bool passed;public int checkCount,screenshotCount;public string[] checks,screenshots;public string failure,gpu,api;}
    readonly List<string> checks=new List<string>(),shots=new List<string>();
    readonly List<Texture2D> textures=new List<Texture2D>();
    TiltedSurfaceRenderer surfaceRenderer;Texture2D face,mask;SurfaceFinish finish;float pitch,yaw;Rect rect;
    static readonly Color32 background=new Color32(20,25,30,255);
    string output,root,failure;bool drew;bool ready,sawNonRepaint,expectedDraw=true;float clock=7.25f;
    void OnGUI() {
        if(!ready)return;
        if(Event.current.type==EventType.Repaint)drew=surfaceRenderer.Draw(rect,face,mask,finish,pitch,yaw,clock);
        else {sawNonRepaint=true;if(surfaceRenderer.Draw(rect,face,mask,finish,pitch,yaw,clock))failure="Draw unexpectedly accepted non-Repaint event";}
    }
    void Check(bool b,string name) {checks.Add((b?"PASS ":"FAIL ")+name);if(!b&&failure==null)failure=name;}
    Texture2D Load(Manifest manifest,string role,string id=null) {
        foreach(var a in manifest.assets)if(a.role==role&&(id==null||a.cardId==id)) {
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false,role.Contains("mask"));
            if(!t.LoadImage(File.ReadAllBytes(Path.Combine(root,a.relativePath))))throw new Exception("PNG decode "+a.relativePath);
            t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;textures.Add(t);return t;
        }
        throw new Exception("Missing actual "+role+" "+id);
    }
    Rect Fit(Texture2D texture) {float s=520f/Mathf.Max(texture.width,texture.height);return new Rect((Screen.width-texture.width*s)/2,(Screen.height-texture.height*s)/2,texture.width*s,texture.height*s);}
    Color Pixel(Texture2D t,int x,int y) {return t.GetPixel(x,t.height-y-1);}
    static float Delta(Color a,Color b) {return Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b));}
    bool UV(float x,float y,out Vector2 uv) {
        float size=Mathf.Max(rect.width,rect.height);Vector3 camera=new Vector3(0,0,2.4f);
        Vector3 ray=new Vector3((x-rect.center.x)/size,(rect.center.y-y)/size,-2.4f);
        Quaternion rotation=Quaternion.Euler(0,yaw,0)*Quaternion.Euler(pitch,0,0);
        Vector3 n=rotation*Vector3.forward;
        Vector3 p=camera+ray*(-Vector3.Dot(n,camera)/Vector3.Dot(n,ray));
        Vector3 local=Quaternion.Inverse(rotation)*p;
        uv=new Vector2(local.x/(rect.width/size)+.5f,local.y/(rect.height/size)+.5f);
        return uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1;
    }
    void Geometry(Texture2D image,string name) {
        int bad=0,edge=0,sampled=0,wrongPrint=0;Rect bounds=TiltedSurfaceRenderer.ConservativeDrawRect(rect);
        for(int y=5;y<image.height-5;y+=2)for(int x=5;x<image.width-5;x+=2) {
            Vector2 uv;bool hit=UV(x+.5f,y+.5f,out uv);Color c=Pixel(image,x,y);bool painted=Delta(c,(Color)background)>.035f;
            float margin=Mathf.Min(uv.x,1-uv.x,uv.y,1-uv.y);
            if(Mathf.Abs(margin)<.008f)continue;
            bool opaque=hit&&face.GetPixelBilinear(uv.x,uv.y).a>.99f;
            // Dark source ink can equal the background; do not mistake it for missing coverage.
            if(!hit&&painted)bad++;
            if(hit&&!bounds.Contains(new Vector2(x+.5f,y+.5f)))edge++;
            if(opaque&&margin>.05f&&x%17<2&&y%19<2) {
                sampled++;
                // CPU bilinear uses endpoint centers, GPU normalized sampling uses u*width-.5.
                float best=1f;
                for(int sy=-1;sy<=1;sy++)for(int sx=-1;sx<=1;sx++) {
                    float tu=(uv.x*face.width-.5f+sx*.5f)/(face.width-1);
                    float tv=(uv.y*face.height-.5f+sy*.5f)/(face.height-1);
                    best=Mathf.Min(best,Delta(c,face.GetPixelBilinear(tu,tv)));
                }
                if(best>.13f)wrongPrint++;
            }
        }
        int cornerMiss=0;float size=Mathf.Max(rect.width,rect.height);
        Quaternion rotation=Quaternion.Euler(0,yaw,0)*Quaternion.Euler(pitch,0,0);
        foreach(float u in new[]{.018f,.982f})foreach(float v in new[]{.018f,.982f}) {
            Vector3 p=rotation*new Vector3((u-.5f)*rect.width/size,(v-.5f)*rect.height/size,0);
            float zoom=2.4f/(2.4f-p.z);int x=Mathf.RoundToInt(rect.center.x+p.x*zoom*size),y=Mathf.RoundToInt(rect.center.y-p.y*zoom*size);
            Color expected=face.GetPixelBilinear(u,v);
            if(expected.a>.99f&&Delta(expected,(Color)background)>.15f&&Delta(Pixel(image,x,y),(Color)background)<.02f)cornerMiss++;
        }
        Check(cornerMiss==0,name+" GPU opaque near-corners retained="+cornerMiss);
        Check(bad<18,name+" perspective alpha silhouette mismatch="+bad);
        Check(edge==0,name+" conservative bounds no corner clipping="+edge);
        Check(sampled>20&&wrongPrint<sampled*.1f,name+" perspective texture landmarks samples="+sampled+" wrong="+wrongPrint);
    }
    IEnumerator Capture(string name,Action<Texture2D> use) {
        yield return new WaitForEndOfFrame();Texture2D image=ScreenCapture.CaptureScreenshotAsTexture();
        try {File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());shots.Add(name+".png");Check(drew==expectedDraw,name+" Repaint Draw="+drew+" expected="+expectedDraw);use(image);}catch(Exception e){if(failure==null)failure=e.ToString();}finally{Destroy(image);}
    }
    float[] Reflection(Texture2D matte,Texture2D coated,string name) {
        int outside=0,inside=0,neutralViolations=0;float largest=0;var bins=new float[16*16];var counts=new int[bins.Length];
        for(int y=0;y<coated.height;y+=2)for(int x=0;x<coated.width;x+=2) {
            Color a=Pixel(matte,x,y),b=Pixel(coated,x,y);float d=Delta(a,b);Vector2 uv;bool hit=UV(x+.5f,y+.5f,out uv);float coverage=0;
            if(hit && mask!=null)for(int oy=-2;oy<=2;oy++)for(int ox=-2;ox<=2;ox++) {
                Color m=mask.GetPixelBilinear(uv.x+ox/(float)mask.width,uv.y+oy/(float)mask.height);coverage=Mathf.Max(coverage,m.r*m.a);
            }
            if(d>0f) {
                if(coverage>.001f){inside++;largest=Mathf.Max(largest,d);}else outside++;
                if(finish==SurfaceFinish.Metallic&&Mathf.Max(Mathf.Abs((b.r-a.r)-(b.g-a.g)),Mathf.Abs((b.g-a.g)-(b.b-a.b)))>3f/255f)neutralViolations++;
            }
            if(hit&&coverage>.05f) {int bin=Mathf.Clamp((int)(uv.y*16),0,15)*16+Mathf.Clamp((int)(uv.x*16),0,15);bins[bin]+=d;counts[bin]++;}
        }
        for(int i=0;i<bins.Length;i++)if(counts[i]>0)bins[i]/=counts[i];
        Check(outside==0,name+" same-projection black-mask/background unchanged="+outside);
        Check(inside>100&&largest>.07f,name+" clearly visible masked reflection pixels="+inside+" peak="+largest);
        Check(neutralViolations==0,name+" wrapper neutral silver not rainbow="+neutralViolations);
        return bins;
    }
    void Equal(Texture2D a,Texture2D b,string name) {var aa=a.GetPixels32();var bb=b.GetPixels32();int changed=0;for(int i=0;i<aa.Length;i++)if(!aa[i].Equals(bb[i]))changed++;Check(changed==0,name+" exact same-geometry pixels changed="+changed);}
    IEnumerator Full(Manifest manifest) {
        var holo=face;var holoMask=mask;
        var nonholo=Load(manifest,"card-face","pkmn-base-en-unlimited1999-017-nonholo-standard");
        var cardback=Load(manifest,"card-back");var front=Load(manifest,"wrapper-front");var back=Load(manifest,"wrapper-back");
        var fm=Load(manifest,"wrapper-front-mask");var bm=Load(manifest,"wrapper-back-mask");
        Texture2D[] faces={holo,nonholo,cardback,front,back};Texture2D[] masks={holoMask,null,null,fm,bm};
        string[] names={"holo","nonholo","cardback","wrapper-front","wrapper-back"};
        Vector2[] poses={Vector2.zero,new Vector2(0,-12),new Vector2(0,12),new Vector2(12,0),new Vector2(-12,0),new Vector2(12,12),new Vector2(-12,-12),new Vector2(12,-12),new Vector2(-12,12)};
        string[] labels={"neutral","left","right","up","down","diagonal-a","diagonal-b","diagonal-c","diagonal-d"};
        for(int material=0;material<faces.Length && failure==null;material++) {
            face=faces[material];mask=masks[material];rect=Fit(face);float[] neutral=null;
            for(int pose=0;pose<poses.Length && failure==null;pose++) {
                pitch=poses[pose].x;yaw=poses[pose].y;finish=SurfaceFinish.Matte;Texture2D baseline=null;string name="actual-"+names[material]+"-"+labels[pose];
                yield return Capture(name+"-matte",t=>{Geometry(t,name);baseline=Instantiate(t);});
                if(failure!=null){if(baseline!=null)Destroy(baseline);break;}
                if(mask!=null) {
                    finish=material==0?SurfaceFinish.Holo:SurfaceFinish.Metallic;
                    float[] response=null;yield return Capture(name+"-finish",t=>response=Reflection(baseline,t,name));
                    if(failure==null) {
                        if(pose==0)neutral=response;
                        else {float total=0;for(int i=0;i<response.Length;i++)total+=Mathf.Abs(response[i]-neutral[i]);Check(total>.3f,name+" fixed-clock surface-UV reflection moves versus neutral L1="+total);}
                    }
                }else {
                    clock=999f;yield return Capture(name+"-later-matte",t=>Equal(baseline,t,name+" matte never acquires metallic/rainbow"));clock=7.25f;
                }
                Destroy(baseline);
            }
        }
        if(failure==null) {
            face=holo;mask=holoMask;rect=Fit(face);pitch=12;yaw=-12;finish=SurfaceFinish.Matte;Texture2D baseline=null;
            yield return Capture("fallback-matte-reference",t=>baseline=Instantiate(t));
            finish=SurfaceFinish.Holo;mask=null;yield return Capture("fallback-null-mask",t=>Equal(baseline,t,"null mask matte fallback"));
            mask=Texture2D.whiteTexture;yield return Capture("fallback-wrong-mask",t=>Equal(baseline,t,"wrong dimensions matte fallback"));
            finish=SurfaceFinish.Matte;mask=holoMask;pitch=10000;yaw=-10000;yield return Capture("clamp-extreme-angles",t=>Equal(baseline,t,"clamped to 12 degrees"));
            Destroy(baseline);pitch=yaw=0;yield return Capture("finite-neutral-reference",t=>baseline=Instantiate(t));
            pitch=float.NaN;yaw=float.PositiveInfinity;clock=float.NaN;yield return Capture("finite-bad-inputs",t=>Equal(baseline,t,"nonfinite pose/clock neutralized"));Destroy(baseline);
            pitch=yaw=0;clock=7.25f;
            Check(sawNonRepaint,"actual Layout/input events return false");
            Check(TiltedSurfaceRenderer.ConservativeDrawRect(new Rect(float.NaN,0,1,1))==new Rect(),"invalid rect conservative empty");
        }
    }
    void BackgroundOnly(Texture2D image,string name) {int changed=0;foreach(var c in image.GetPixels32())if(!c.Equals(background))changed++;Check(changed==0,name+" untouched background pixels="+changed);}
    IEnumerator Safety() {
        var goodFace=face;Rect goodRect=rect;expectedDraw=false;
        foreach(Rect bad in new[]{new Rect(float.NaN,0,100,100),new Rect(0,float.PositiveInfinity,100,100),new Rect(0,0,-1,100),new Rect(0,0,100,float.PositiveInfinity),new Rect(0,0,0,100)}) {
            rect=bad;yield return Capture("invalid-rect-"+shots.Count,t=>BackgroundOnly(t,"invalid rect no drawing"));
        }
        rect=goodRect;face=null;yield return Capture("null-face",t=>BackgroundOnly(t,"null face no drawing"));face=goodFace;expectedDraw=true;
        // Synthetic alpha is separate from the supplied opaque scans: exercise actual texture alpha.
        var cutout=new Texture2D(128,192,TextureFormat.RGBA32,false);var pixels=new Color32[128*192];
        for(int y=0;y<192;y++)for(int x=0;x<128;x++)pixels[y*128+x]=new Color32(130,170,210,(byte)(x<25||x>100||y<30||y>160?0:255));
        cutout.SetPixels32(pixels);cutout.Apply();cutout.wrapMode=TextureWrapMode.Clamp;textures.Add(cutout);face=cutout;mask=null;finish=SurfaceFinish.Matte;rect=Fit(face);pitch=-12;yaw=12;
        yield return Capture("synthetic-alpha-projected",t=> {
            int transparent=0,bad=0;for(int y=0;y<t.height;y+=2)for(int x=0;x<t.width;x+=2){Vector2 uv;if(!UV(x+.5f,y+.5f,out uv))continue;float alpha=0;for(int oy=-2;oy<=2;oy++)for(int ox=-2;ox<=2;ox++)alpha=Mathf.Max(alpha,face.GetPixelBilinear(uv.x+ox/(float)face.width,uv.y+oy/(float)face.height).a);if(alpha<.001f){transparent++;if(Delta(Pixel(t,x,y),(Color)background)>1f/255f)bad++;}}
            Check(transparent>1000&&bad==0,"projected internal source alpha holes remain background="+bad);
        });
        face=goodFace;rect=goodRect;pitch=yaw=0;finish=SurfaceFinish.Matte;
        var owned=typeof(TiltedSurfaceRenderer).GetField("material",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(surfaceRenderer) as Material;
        Check(owned!=null,"renderer owns a native material before disposal");
        surfaceRenderer.Dispose();surfaceRenderer.Dispose();expectedDraw=false;yield return null;
        yield return Capture("disposed-renderer",t=>{BackgroundOnly(t,"disposed renderer cannot resurrect");Check(owned==null,"deferred GPU material destruction completed");Check(face!=null&&goodFace.GetPixel(0,0).a>=0,"renderer disposal preserves provider textures");});
    }
    IEnumerator Start() {
        Application.runInBackground=true;output=Environment.GetEnvironmentVariable("TILT_SURFACE_OUT");root=Environment.GetEnvironmentVariable("TILT_SURFACE_ROOT");
        surfaceRenderer=new TiltedSurfaceRenderer();Manifest manifest=null;
        try {
            manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root,"Assets/Supplied/BaseSetMaterials/Manifest.json")));
            face=Load(manifest,"card-face","pkmn-base-en-unlimited1999-001-holo-standard");mask=Load(manifest,"holo-mask","pkmn-base-en-unlimited1999-001-holo-standard");rect=Fit(face);finish=SurfaceFinish.Matte;ready=true;
        }catch(Exception e){failure=e.ToString();}
        for(int i=0;i<5;i++)yield return null;
        if(failure==null) {
            string slice=Environment.GetEnvironmentVariable("TILT_SURFACE_SLICE");
            if(slice=="faults") {expectedDraw=false;yield return Capture("unavailable-shader",t=>BackgroundOnly(t,"unavailable/unsupported shader fails closed"));}
            else {
                yield return Capture("tracer-neutral-matte",t=>Geometry(t,"neutral"));
                yaw=-12;
                if(failure==null)yield return Capture("tracer-left-matte",t=>Geometry(t,"left"));
                if(failure==null && slice!="geometry" && slice!="safety")yield return Full(manifest);
                if(failure==null && slice!="geometry")yield return Safety();
            }
        }
        ready=false;surfaceRenderer.Dispose();foreach(var t in textures)Destroy(t);
        File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),screenshotCount=shots.Count,screenshots=shots.ToArray(),failure=failure,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString()},true));
        Application.Quit(failure==null?0:1);
    }
}
