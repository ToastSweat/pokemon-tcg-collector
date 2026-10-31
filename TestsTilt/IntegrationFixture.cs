using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using VintageCollector.Presentation;
using VintageCollector.Media;
using VintageCollector.Domain;

// Explicit synthetic read-only view fixture using actual supplied textures. No runtime/host/save API.
[DefaultExecutionOrder(10000)]
public sealed class IntegrationFixture : MonoBehaviour
{
    [Serializable] public sealed class Request { public int id; public string stage="sealed",transaction="fixture-A",capture; public int count,inspect=-1; public bool enabled,paused,binder,overlayOnly,focus=true,fixedClock=true,mutate,quit,destroyUi; public int width,height; }
    [Serializable] public sealed class State { public int id,frame,width,height,cues,commands,audioSources,bindings,cache; public bool enabled,frozen,detached,hint,teardownPassed; public float clock; public string view,key,failure,controllers,lastCommand; public Vector2 pointer; public TabletopPresentation.TiltObservation[] surfaces; public Rect[] drawBounds; }
    TabletopPresentation ui; PresentationMediaLibrary library; string output,root,lastRequest,lastCommand; Request request=new Request(); int commands,destroyedRequest=-1; bool detached=true,renderedHint,recreating,teardownPassed; string failure;
    static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    object Motion { get {return typeof(TabletopPresentation).GetField("motion",Hidden).GetValue(ui);} }
    object Diagnostic(string name,object fallback) {var p=typeof(TabletopPresentation).GetProperty(name);return p==null?fallback:p.GetValue(ui,null);}
    void Awake()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
        output=Environment.GetEnvironmentVariable("TILT_PRESENTATION_OUT");root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        root=Environment.GetEnvironmentVariable("TILT_PRESENTATION_ROOT")??root;
        ui=gameObject.AddComponent<TabletopPresentation>();ui.Command+=command=>{commands++;lastCommand=command;};
        ui.TextObserver=text=>{if(text=="Move mouse over card/pack to tilt and catch light")renderedHint=true;};
        try {
            var catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"))));
            library=PresentationMediaLibrary.Load(root,catalog,"Assets/Supplied/BaseSetMaterials/Manifest.json",false,true);
            ui.Media=library;
            if(library.SuppliedAssetCount!=121 || library.Warnings.Count!=0) throw new Exception("Actual full library binding failed: "+library.SuppliedAssetCount);
        }catch(Exception e){failure=e.ToString();}
        Apply(request);StartCoroutine(Observe());
    }
    void Apply(Request r)
    {
        request=r;ui.pointerTiltEnabled=r.enabled;ui.CollectionInputBlocked=r.binder;ui.CollectionOverlayVisible=r.binder || r.overlayOnly;
        // Focus gating is separately tested through the actual window message by the driver.
        if(r.width>0 && r.height>0) Screen.SetResolution(r.width,r.height,FullScreenMode.Windowed);
        var cards=new OpeningCardView[11];
        for(int i=0;i<11;i++) cards[i]=new OpeningCardView {id="pkmn-base-en-unlimited1999-"+(i%2==0?"017-nonholo-standard":"001-holo-standard"),name=i%2==0?"Beedrill":"Alakazam",finish=i%2==0?"nonholo":"holo",kind="Pokemon",rarity="Rare",collectorNumber=i%2==0?"17":"1"};
        ui.View=new OpeningView {stage=r.stage,transactionId=r.transaction,revealedCount=r.count,cards=cards,inspectIndex=r.inspect,paused=r.paused,totalVirtualCards=11};
    }
    void LateUpdate()
    {
        renderedHint=false;
        if(recreating)return;
        try {
            string path=Path.Combine(output,"request.json");
            if(File.Exists(path)) {
                string text=File.ReadAllText(path);
                if(text!=lastRequest) {lastRequest=text;Apply(JsonUtility.FromJson<Request>(text));}
            }
            if(request.destroyUi && destroyedRequest!=request.id) {destroyedRequest=request.id;StartCoroutine(Recreate());return;}
            // Test-only clock hold, after production Update and before actual rendering.
            if(request.fixedClock) Motion.GetType().GetProperty("Clock").SetValue(Motion,3f,null);
            if(request.mutate) {
                var before=ui.TiltObservations;
                if(before.Length>0) {
                    string key=before[0].key;Rect rect=before[0].bounds;float pitch=before[0].pitchDegrees;
                    before[0].key="forged";before[0].bounds=new Rect(-999,-999,1,1);before[0].pitchDegrees=999;before[0].hovered=!before[0].hovered;
                    var after=ui.TiltObservations;detached &= after.Length>0 && after[0].key==key && after[0].bounds==rect && after[0].pitchDegrees==pitch;
                }
            }
            if(request.quit)Application.Quit(0);
        }catch(IOException){ /* Atomic mailbox replacement can transiently deny sharing; retry next frame. */ }
        catch(Exception e){failure=e.ToString();}
    }
    IEnumerator Observe()
    {
        while(true) {
            yield return new WaitForEndOfFrame();
            if(recreating || ui==null)continue;
            if(request.capture!=null && Path.GetFileName(request.capture)==request.capture && request.capture.EndsWith(".png")) {
                string file=Path.Combine(output,request.capture);
                if(!File.Exists(file)) {
                    var t=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
                    t.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);t.Apply();File.WriteAllBytes(file,t.EncodeToPNG());Destroy(t);
                }
            }
            var state=new State {id=request.id,frame=Time.frameCount,width=Screen.width,height=Screen.height,enabled=ui.pointerTiltEnabled,cues=ui.CommittedCueCount,commands=commands,audioSources=goAudioCount(),bindings=library==null?0:library.SuppliedAssetCount,clock=(float)Motion.GetType().GetProperty("Clock").GetValue(Motion,null),view=ui.View.stage,key=(string)Diagnostic("TiltContextKey",""),pointer=(Vector2)Diagnostic("TiltPointer",Vector2.zero),frozen=(bool)Diagnostic("TiltFrozen",false),cache=(int)Diagnostic("TiltControllerCount",0),surfaces=ui.TiltObservations,detached=detached,failure=failure,controllers=ControllerFingerprint(),hint=renderedHint,teardownPassed=teardownPassed,lastCommand=lastCommand,drawBounds=DrawBounds()};
            string path=Path.Combine(output,"state.json"),work=path+".writing";
            // Windows readers can transiently deny delete-sharing to File.Replace; retry next actual frame.
            try {File.WriteAllText(work,JsonUtility.ToJson(state,true));if(File.Exists(path)) File.Replace(work,path,null);else File.Move(work,path);}
            catch(IOException) { }
        }
    }
    int goAudioCount(){return gameObject.GetComponents<AudioSource>().Length;}
    Rect[] DrawBounds() {
        var field=typeof(TabletopPresentation).GetField("renderedTiltSurfaces",Hidden);if(field==null)return new Rect[0];
        var bounds=new System.Collections.Generic.List<Rect>();
        foreach(object surface in (System.Collections.IEnumerable)field.GetValue(ui))bounds.Add((Rect)surface.GetType().GetField("drawBounds").GetValue(surface));
        return bounds.ToArray();
    }
    IEnumerator Recreate()
    {
        recreating=true;
        var rendererField=typeof(TabletopPresentation).GetField("tiltedRenderer",Hidden);
        object renderer=rendererField==null?null:rendererField.GetValue(ui);
        Material material=renderer==null?null:renderer.GetType().GetField("material",Hidden).GetValue(renderer) as Material;
        bool hadMaterial=material!=null;
        Texture2D front=library.WrapperFront,back=library.CardBack,face=library.CardFace("pkmn-base-en-unlimited1999-001-holo-standard");
        Destroy(ui);yield return null;yield return new WaitForEndOfFrame();
        teardownPassed=hadMaterial && material==null && front!=null && back!=null && face!=null;
        ui=gameObject.AddComponent<TabletopPresentation>();ui.Command+=command=>{commands++;lastCommand=command;};
        ui.TextObserver=text=>{if(text=="Move mouse over card/pack to tilt and catch light")renderedHint=true;};
        ui.Media=library;Apply(request);recreating=false;
    }
    string ControllerFingerprint()
    {
        var field=typeof(TabletopPresentation).GetField("tiltControllers",Hidden);if(field==null)return "";
        var list=new System.Collections.Generic.List<string>();
        foreach(System.Collections.DictionaryEntry pair in (System.Collections.IDictionary)field.GetValue(ui)) {
            string text=(string)pair.Key;
            foreach(string name in new[]{"PitchDegrees","YawDegrees","TargetPitchDegrees","TargetYawDegrees"}) text+="|"+((float)pair.Value.GetType().GetProperty(name).GetValue(pair.Value,null)).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            list.Add(text);
        }
        list.Sort(StringComparer.Ordinal);return string.Join(";",list.ToArray());
    }
    void OnDestroy(){if(ui!=null)ui.Media=null;if(library!=null)library.Dispose();}
}
