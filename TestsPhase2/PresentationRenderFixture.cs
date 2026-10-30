// Synthetic view fixture for ISOLATED verification only. Not imported into the main Assets tree.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using VintageCollector.Presentation;

public sealed class PresentationRenderFixture : MonoBehaviour
{
    [Serializable] private sealed class Report { public bool passed; public int checkCount; public string[] checks; public string failure; }
    private readonly List<string> checks=new List<string>();
    private readonly List<string> labels=new List<string>();
    private string failure;
    private TabletopPresentation presentation;
    private string output;
    private OpeningView view;
    private void Assert(bool ok,string name) { if(!ok) throw new Exception(name); checks.Add(name); }
    private void Awake() { Application.runInBackground=true; }
    private IEnumerator Start()
    {
        output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../presentation/render"));
        Directory.CreateDirectory(output);
        presentation=gameObject.AddComponent<TabletopPresentation>();
        var observer=typeof(TabletopPresentation).GetProperty("TextObserver",BindingFlags.Instance|BindingFlags.NonPublic);
        if(observer==null) { failure="Actual GUI text observation seam missing"; WriteReport(); Application.Quit(1); yield break; }
        observer.SetValue(presentation,(Action<string>)(s=>labels.Add(s)));
        view=new OpeningView {stage="ready",modelLabel="Model A 0.1.0 · provisional",statusLine="Synthetic presentation fixture — no generation, persistence or awards.",totalVirtualCards=22};
        presentation.View=view;
        var cards=new OpeningCardView[11];
        for(int i=0;i<11;i++) cards[i]=new OpeningCardView {id="fixture-"+i,name="Fixture Card "+(i+1),collectorNumber=(i+1)+"/102",rarity=i==10?"Rare":"Common",kind="Generic",finish=i==10?"Holo":"Non-holo",isNew=i%3!=0};
        foreach(var size in new[]{new[]{1280,720},new[]{1600,900},new[]{2560,1080},new[]{1280,360}}) {
            Screen.SetResolution(size[0],size[1],FullScreenMode.Windowed);
            for(int i=0;i<8;i++) yield return null;
            foreach(string stage in new[]{"ready","sealed","revealing","inspection","invalid-inspection","results","pause","error"}) {
                view.stage=stage=="inspection" || stage=="invalid-inspection"?"revealing":stage=="pause"?"results":stage;
                view.cards=cards; view.revealedCount=view.stage=="results"?11:3;
                view.inspectIndex=stage=="inspection"?1:stage=="invalid-inspection"?10:-1;
                view.paused=stage=="pause";
                view.errorMessage="Synthetic disk failure. Retry the same pending action. No save data has been replaced.";
                // Mark unrevealed positions with unique canaries and observe REAL OnGUI label calls.
                for(int i=0;i<11;i++) cards[i].name=((view.stage!="revealing" && view.stage!="results") || i>=view.revealedCount?"HIDDEN-CANARY-":"Fixture Card ")+(i+1);
                labels.Clear();
                yield return new WaitForEndOfFrame();
                try {
                    Assert(labels.Count>0,"Actual GUI labels observed "+stage+" "+size[0]+"x"+size[1]);
                    foreach(string text in labels) Assert(!text.Contains("HIDDEN-CANARY-"),"No hidden canary rendered "+stage+" "+checks.Count);
                    if(stage=="revealing") Assert(labels.Contains("1") && labels.Contains("2") && labels.Contains("3") && !labels.Contains("4"),"Actual history exposes only revealed index buttons "+size[0]+"x"+size[1]);
                    if(stage=="invalid-inspection") Assert(labels.Exists(s=>s.Contains("not available for inspection")),"Invalid inspection shows safe Back state");
                    if(stage=="results") {
                        for(int i=0;i<11;i++) Assert(labels.Exists(s=>s=="Fixture Card "+(i+1)),"All eleven actual result faces "+i+" "+size[0]+"x"+size[1]);
                        var image=ScreenCapture.CaptureScreenshotAsTexture();
                        try { AssertArtBounded(image,PresentationRules.Layout(Screen.width,Screen.height)); }
                        finally { Destroy(image); }
                    }
                    if(stage=="pause") Assert(labels.Contains("Resume") && labels.Contains("Mute") && labels.Contains("Quit"),"Actual pause controls rendered "+size[0]+"x"+size[1]);
                } catch(Exception e) { failure=e.ToString(); }
                string path=Path.Combine(output,size[0]+"x"+size[1]+"-"+stage+".png");
                ScreenCapture.CaptureScreenshot(path);
                for(int i=0;i<4;i++) yield return null;
                if(failure!=null) { WriteReport(); Application.Quit(1); yield break; }
            }
        }
        WriteReport(); Application.Quit(0);
    }
    private void AssertArtBounded(Texture2D image,PresentationLayout layout)
    {
        var pixels=image.GetPixels32(); int escaped=0;
        foreach(var b in layout.resultsCards) {
            float s=layout.scale;
            // Unity rasterizes fractional clip edges to adjacent pixels; allow exactly one physical pixel.
            var art=new Rect((b.x+b.width*0.08f)*s-1,(b.y+b.height*0.17f)*s-1,b.width*0.84f*s+2,b.height*0.37f*s+2);
            // Mountain colors are unique to abstract illustration; they must never cover face text.
            for(int y=(int)(b.y*s);y<(b.y+b.height)*s;y++) for(int x=(int)(b.x*s);x<(b.x+b.width)*s;x++) {
                if(art.Contains(new Vector2(x+0.5f,y+0.5f))) continue;
                var p=pixels[(image.height-1-y)*image.width+x];
                if((Math.Abs(p.r-51)<3 && Math.Abs(p.g-83)<3 && Math.Abs(p.b-73)<3) || (Math.Abs(p.r-73)<3 && Math.Abs(p.g-97)<3 && Math.Abs(p.b-79)<3)) escaped++;
            }
        }
        Assert(escaped==0,"Illustration pixels bounded to art region at "+image.width+"x"+image.height+" escaped="+escaped);
    }
    private void WriteReport()
    {
        File.WriteAllText(Path.Combine(output,"render-report.json"),JsonUtility.ToJson(new Report {passed=failure==null,checkCount=checks.Count,checks=checks.ToArray(),failure=failure},true));
    }
}
