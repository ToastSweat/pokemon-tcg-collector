using System;
using System.IO;
using UnityEngine;
using VintageCollector.Runtime;
namespace VintageCollector.Verification {
    // No marker IO in ordinary play. The exact fixture is additional to GamePaths smoke admission.
    [DefaultExecutionOrder(-175)]
    public sealed class TiltSmokeProbe : MonoBehaviour {
        private const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
        public static string ValidateRequest(string raw,int lastSequence) {
            if(raw==null || raw.Length>256 || lastSequence<0 || lastSequence>=10000)throw new ArgumentException("Bounded tilt request sequence");
            foreach(string action in new[]{"capture","seed4","fault"}) {
                string expected="{\"schemaVersion\":1,\"sequence\":"+(lastSequence+1).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"action\":\""+action+"\"}";
                if(raw==expected)return action;
            }
            throw new ArgumentException("Exact bounded next-sequence request required; pointer injection is not supported");
        }
        private static void Safe(string path) {string p=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(p) && p!=path)Safe(p);try {if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Reparse fixture refused");}catch(FileNotFoundException){}catch(DirectoryNotFoundException){}}
        private static string Bounded(string file,int limit) {Safe(file);using(var stream=File.OpenRead(file)) {var b=new byte[limit+1];int count=0,n;while(count<b.Length && (n=stream.Read(b,count,b.Length-count))>0)count+=n;if(count>limit)throw new ArgumentException("Fixture byte cap");if(count>=3 && b[0]==239 && b[1]==187 && b[2]==191)throw new ArgumentException("Fixture BOM");return new System.Text.UTF8Encoding(false,true).GetString(b,0,count);}}
        public static string ValidateOptIn(GamePaths paths) {
            if(paths==null || !paths.IsSmokeTest)return null;
            string artifacts=Path.GetFullPath(paths.ArtifactDirectory),prefix=Path.GetFullPath(Path.Combine(Root,".artifacts","phase4-player"))+Path.DirectorySeparatorChar;
            if(!artifacts.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))return null;
            string marker=Path.Combine(artifacts,"tilt-fixture.json");Safe(marker);if(!File.Exists(marker))return null;
            string[] parts=artifacts.Substring(prefix.Length).Split(Path.DirectorySeparatorChar);
            if(parts.Length!=2 || !System.Text.RegularExpressions.Regex.IsMatch(parts[0],"^[A-Za-z0-9_-]+$") || Array.IndexOf(new[]{"services","input","reopen"},parts[1])<0 || paths.SmokeScenario!="input" ||
                !string.Equals(Path.GetFullPath(paths.BaseSaveDirectory),Path.Combine(prefix,parts[0],"session"),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Tilt fixture scope mismatch");
            Safe(paths.BaseSaveDirectory);
            string raw=Bounded(marker,256);
            foreach(string value in new[]{"observe","hover"})if(raw=="{\"schemaVersion\":1,\"owner\":\"tilt-player-fixture\",\"mode\":\""+value+"\"}") {if(value=="hover" && parts[1]!="input")throw new ArgumentException("Hover requires isolated input fixture");return value;}
            throw new ArgumentException("Tilt fixture must match exact contract");
        }

        [Serializable] public sealed class Observation {
            public string key;public Rect bounds;public float pitchDegrees,yawDegrees;public bool hovered;public int finish,renderFrame;
        }
        [Serializable] public sealed class Pin {public string name,sha256;}
        [Serializable] public sealed class State {
            public string stage,capture,failure,viewJson,controlsSource;public int frame,sequence,screenWidth,screenHeight,generatedResultCount,totalVirtualCards,revealedCount,inspectIndex,cues;
            public long revision;public float scale,clock,pointerX,pointerY,inputX,inputY;public bool paused,binderOpen,tiltEnabled,ready,silent;
            public Observation[] observations;public Pin[] profilePins;public Phase4SmokeProbe.Control[] controls;
        }
        private OpeningGame game;private VintageCollector.Presentation.TabletopPresentation table;
        private Phase4CollectionHost host;private bool hover;private int sequence;private string directory,lastRequest,capture="",failure;
        private Vector2 eventPointer;
        private System.Collections.IEnumerator Start() {
            game=GetComponent<OpeningGame>();string mode;
            try {mode=ValidateOptIn(game==null?null:game.Paths);}catch(Exception e){Debug.LogError(e);Application.Quit(2);yield break;}
            if(mode!="hover")yield break; // observe mode leaves Materials(-150)/Phase4 Start ordering unchanged.
            // Dedicated hover mode is mutually exclusive with legacy auto-generation/quit.
            var legacy=GetComponent<Phase4SmokeProbe>();if(legacy!=null)legacy.enabled=false;
            var materials=GetComponent<MaterialsSmokeProbe>();if(materials!=null)materials.enabled=false;
            directory=game.Paths.ArtifactDirectory;hover=true;Application.runInBackground=true;
            table=GetComponent<VintageCollector.Presentation.TabletopPresentation>();host=GetComponent<Phase4CollectionHost>();
            yield return null; // actual host/media Start completes before any observation
            yield return ObserveHover();
        }
        private void OnGUI() {if(hover && Event.current!=null)eventPointer=Event.current.mousePosition;}
        private Observation[] Observations() {
            var property=table.GetType().GetProperty("TiltObservations");
            if(property==null)throw new InvalidOperationException("Actual tilt observation API unavailable");
            var source=property.GetValue(table,null) as Array;if(source==null)throw new InvalidOperationException("Observation array missing");
            var result=new System.Collections.Generic.List<Observation>();
            foreach(var item in source) {
                Type t=item.GetType();
                result.Add(new Observation {key=(string)t.GetField("key").GetValue(item),bounds=(Rect)t.GetField("bounds").GetValue(item),
                    pitchDegrees=(float)t.GetField("pitchDegrees").GetValue(item),yawDegrees=(float)t.GetField("yawDegrees").GetValue(item),
                    hovered=(bool)t.GetField("hovered").GetValue(item),finish=(int)t.GetField("finish").GetValue(item),renderFrame=(int)t.GetField("renderFrame").GetValue(item)});
            }
            return result.ToArray();
        }
        private State Snapshot() {
            var view=game.CurrentView;var l=VintageCollector.Presentation.PresentationRules.Layout(Screen.width,Screen.height);
            var controls=new System.Collections.Generic.List<Phase4SmokeProbe.Control>();
            Action<string,VintageCollector.Presentation.PresentationBox> add=(name,r)=>controls.Add(new Phase4SmokeProbe.Control {name=name,x=r.x*l.scale,y=r.y*l.scale,width=r.width*l.scale,height=r.height*l.scale,enabled=true});
            if(view.stage=="results")add("primary",new VintageCollector.Presentation.PresentationBox(l.width-310,140,266,34));
            else add("primary",l.primary);
            add("skip",l.secondary);add("resume",l.resume);add("quit",l.quit);add("hero",l.heroCard);add("back",l.back);
            add("binder",new VintageCollector.Presentation.PresentationBox(l.width-520,l.height-49,220,34));
            if(view.stage=="results")for(int n=0;n<11;n++)add("result:"+n,l.resultsCards[n]);
            var pins=new System.Collections.Generic.List<Pin>();
            foreach(string name in new[]{"state.save","state.backup","state.anchor","physical.save","physical.backup","physical.anchor"}) {
                string file=Path.Combine(game.Paths.SaveDirectory,name);Safe(file);
                if(File.Exists(file))using(var h=System.Security.Cryptography.SHA256.Create())using(var f=File.OpenRead(file))pins.Add(new Pin {name=name,sha256=BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant()});
            }
            var motion=table.GetType().GetField("motion",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(table);
            bool silent=true;foreach(var audio in GetComponentsInChildren<AudioSource>(true))silent &= !audio.isPlaying && audio.clip==null;
            return new State {controlsSource="source-pinned-table-layout-not-observed-registrations",stage=view.stage,viewJson=JsonUtility.ToJson(view),ready=host!=null && host.IsReady,frame=Time.frameCount,sequence=sequence,capture=capture,failure=failure,
                screenWidth=Screen.width,screenHeight=Screen.height,scale=l.scale,clock=(float)motion.GetType().GetProperty("Clock").GetValue(motion,null),
                pointerX=eventPointer.x,pointerY=eventPointer.y,inputX=Input.mousePosition.x,inputY=Screen.height-Input.mousePosition.y,
                revision=game.CurrentSession.Revision,generatedResultCount=game.GeneratedResultCount,totalVirtualCards=game.CurrentSession.TotalVirtualCards,
                revealedCount=view.revealedCount,inspectIndex=view.inspectIndex,paused=view.paused,binderOpen=host!=null && host.CurrentBinderView!=null && host.CurrentBinderView.open,
                cues=table.CommittedCueCount,silent=silent,tiltEnabled=(bool)table.GetType().GetField("pointerTiltEnabled").GetValue(table),observations=Observations(),profilePins=pins.ToArray(),controls=controls.ToArray()};
        }
        private System.Collections.IEnumerator ObserveHover() {
            while(hover) {
                yield return new WaitForEndOfFrame();
                try {
                    string request=Path.Combine(directory,"tilt-request.json");Safe(request);
                    if(File.Exists(request)) {
                        string raw=Bounded(request,256);
                        if(raw!=lastRequest) {
                            string action=ValidateRequest(raw,sequence);
                            if(action=="seed4") {if(game.GeneratedResultCount!=0 || game.CurrentSession.Packs.Count!=0)throw new ArgumentException("Seed setup requires fresh isolated session");game.GenerateForSmoke(4u);}
                            else if(action=="fault")game.FailNextCommitForSmoke();
                            else {
                                string name="tilt-"+(sequence+1).ToString("D4",System.Globalization.CultureInfo.InvariantCulture)+".png";
                                var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
                                try {texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,name),texture.EncodeToPNG());}finally{Destroy(texture);}
                                capture=name;
                            }
                            sequence++;lastRequest=raw;
                        }
                    }
                    var state=Snapshot();string json=JsonUtility.ToJson(state,true);
                    Atomic(Path.Combine(directory,"tilt-state.json"),json);
                    if(state.sequence>0 && state.capture.Length>0 && state.sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)!=lastCapturedSequence && lastRequest.EndsWith("\"capture\"}",StringComparison.Ordinal)) {
                        Atomic(Path.Combine(directory,Path.GetFileNameWithoutExtension(state.capture)+".json"),json);lastCapturedSequence=state.sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }catch(Exception e) {
                    failure=e.ToString();Atomic(Path.Combine(directory,"tilt-failure.json"),JsonUtility.ToJson(new Failure {failure=failure}));Debug.LogError(e);Application.Quit(2);yield break;
                }
                yield return new WaitForSecondsRealtime(.035f);
            }
        }
        private string lastCapturedSequence="";
        [Serializable] private sealed class Failure {public string failure;}
        private static void Atomic(string path,string text) {
            string work=path+".writing";File.WriteAllText(work,text);
            for(int attempt=0;;attempt++)try {if(File.Exists(path))File.Replace(work,path,null);else File.Move(work,path);return;}catch(IOException e){int code=e.HResult&65535;if(attempt>=50 || (code!=32 && code!=33))throw;System.Threading.Thread.Sleep(10);}
        }
    }
}
