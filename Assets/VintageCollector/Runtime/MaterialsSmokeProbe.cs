using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using VintageCollector.Domain;
using VintageCollector.Media;
using VintageCollector.Runtime;
using VintageCollector.Presentation;

namespace VintageCollector.Verification
{
    // Inert in ordinary play. An additional exact, bounded fixture opts in alongside the unchanged Phase4 probe.
    [DefaultExecutionOrder(-150)]
    public sealed class MaterialsSmokeProbe : MonoBehaviour
    {
        private const string Root="F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG";
        private const string Manifest="Assets/Supplied/BaseSetMaterials/Manifest.json";
        [Serializable] public sealed class Binding {public string role,cardId,rgbaSha256;public int width,height;}
        [Serializable] public sealed class Oracle {public bool synthetic;public string manifestSha256;public Binding[] bindings;}
        [Serializable] public sealed class Check {public string name,detail;public bool passed;}
        [Serializable] public sealed class Report {public bool passed,synthetic;public int checkCount,suppliedBindings,uniqueTextures;public long texturePixels;public string manifestSha256,mode,failure;public Check[] checks;public string[] images;}
        private readonly List<Check> checks=new List<Check>();private readonly List<string> images=new List<string>();
        private OpeningGame game;private TabletopPresentation table;private Phase3MediaHost host;private string directory,mode,failure;private bool active,complete;
        private Report report;
        private void Assert(string name,bool value,string detail="") {checks.Add(new Check {name=name,passed=value,detail=detail});if(!value)throw new InvalidOperationException(name+": "+detail);}
        private static void Safe(string path) {string p=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(p) && p!=path)Safe(p);try {if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Reparse fixture refused");}catch(FileNotFoundException){}catch(DirectoryNotFoundException){}}
        private static string Bounded(string file,int limit) {Safe(file);using(var stream=File.OpenRead(file)) {var b=new byte[limit+1];int count=0,n;while(count<b.Length && (n=stream.Read(b,count,b.Length-count))>0)count+=n;if(count>limit)throw new ArgumentException("Fixture byte cap");if(count>=3 && b[0]==239 && b[1]==187 && b[2]==191)throw new ArgumentException("Fixture BOM");return new System.Text.UTF8Encoding(false,true).GetString(b,0,count);}}
        public static string ValidateOptIn(GamePaths paths) {
            if(paths==null || !paths.IsSmokeTest)return null;
            string artifacts=Path.GetFullPath(paths.ArtifactDirectory),prefix=Path.GetFullPath(Path.Combine(Root,".artifacts","phase4-player"))+Path.DirectorySeparatorChar;
            if(!artifacts.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))return null;
            string marker=Path.Combine(artifacts,"materials-fixture.json");Safe(marker);if(!File.Exists(marker))return null;
            string[] parts=artifacts.Substring(prefix.Length).Split(Path.DirectorySeparatorChar);
            if(parts.Length!=2 || !System.Text.RegularExpressions.Regex.IsMatch(parts[0],"^[A-Za-z0-9_-]+$") || Array.IndexOf(new[]{"services","input","reopen"},parts[1])<0 || paths.SmokeScenario!="input" ||
                !string.Equals(Path.GetFullPath(paths.BaseSaveDirectory),Path.Combine(prefix,parts[0],"session"),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Materials fixture scope mismatch");
            Safe(paths.BaseSaveDirectory);
            string raw=Bounded(marker,256);
            foreach(string value in new[]{"observe","demo"})if(raw=="{\"schemaVersion\":1,\"owner\":\"materials-player-fixture\",\"mode\":\""+value+"\"}") {if(value=="demo" && parts[1]!="input")throw new ArgumentException("Demo requires isolated input fixture");return value;}
            throw new ArgumentException("Materials fixture must match exact contract");
        }
        private IEnumerator Start() {
            // Host -200 resolves paths first. Fence the legacy probe before its Start
            // can run/quit; normal gameplay and unrelated smoke launches are untouched.
            game=GetComponent<OpeningGame>();
            try {mode=ValidateOptIn(game==null?null:game.Paths);}catch(Exception e){Debug.LogError(e.Message);Application.Quit(2);yield break;}
            if(mode==null)yield break;
            var regression=GetComponent<Phase4SmokeProbe>();bool startRegression=regression!=null && regression.enabled;
            if(startRegression)regression.enabled=false;
            yield return null; // Optional media host +100 now binds before verification.
            directory=game.Paths.ArtifactDirectory;active=true;table=GetComponent<TabletopPresentation>();host=GetComponent<Phase3MediaHost>();
            report=new Report {mode=mode,synthetic=false};
            yield return Guard(Verify());
            if(mode=="demo" && failure==null)yield return Guard(Demo());
            complete=true;Publish();
            // Release real services/input/reopen automation only after the material
            // report is complete; an early legacy Quit cannot truncate native proof.
            if(mode!="demo" && failure==null && startRegression)regression.enabled=true;
            if(mode=="demo" || failure!=null)Application.Quit(failure==null?0:1);
        }
        private IEnumerator Guard(IEnumerator iterator) {
            var stack=new Stack<IEnumerator>();stack.Push(iterator);
            while(stack.Count>0) {bool more=false;object item=null;Exception error=null;try {more=stack.Peek().MoveNext();if(more)item=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){failure=error.ToString();checks.Add(new Check {name="materials-probe-failure",passed=false,detail=failure});yield break;}
                if(!more){stack.Pop();continue;}var nested=item as IEnumerator;if(nested!=null)stack.Push(nested);else yield return item;
            }
        }
        private static string Hash(byte[] bytes) {using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        private static string Pixels(Texture2D texture) {var p=texture.GetPixels32();var bytes=new byte[p.Length*4];for(int n=0;n<p.Length;n++){bytes[n*4]=p[n].r;bytes[n*4+1]=p[n].g;bytes[n*4+2]=p[n].b;bytes[n*4+3]=p[n].a;}return Hash(bytes);}
        private static Texture2D Resolve(PresentationMediaLibrary lib,Binding b) {switch(b.role){case "card-face":return lib.CardFace(b.cardId);case "holo-mask":return lib.HoloMask(b.cardId);case "card-back":return lib.CardBack;case "wrapper-front":return lib.WrapperFront;case "wrapper-back":return lib.WrapperBack;case "wrapper-front-mask":return lib.WrapperFrontMask;case "wrapper-back-mask":return lib.WrapperBackMask;default:throw new ArgumentException("Unknown actual image role");}}
        private IEnumerator Verify() {
            Assert("actual-native-host-bound",host!=null && host.Library!=null && table!=null && ReferenceEquals(table.Media,host.Library));
            Assert("trusted-new-manifest-no-test-override",host.manifestRelativePath==Manifest && host.fullBaseSetBudget && !host.UsesTestManifest);
            var oracle=JsonUtility.FromJson<Oracle>(Bounded(Path.Combine(directory,"materials-oracle.json"),131072));
            string manifestHash=Hash(File.ReadAllBytes(Path.Combine(Root,Manifest)));report.manifestSha256=manifestHash;
            Assert("actual-oracle-no-synthetic-inputs",oracle!=null && !oracle.synthetic && oracle.bindings!=null && oracle.bindings.Length==121 && oracle.manifestSha256==manifestHash);
            Assert("accepted-manifest0-byte-exact",Hash(File.ReadAllBytes(Path.Combine(Root,"Assets/Supplied/AssetManifest.json")))=="f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8");
            var lib=host.Library;report.suppliedBindings=lib.SuppliedAssetCount;report.uniqueTextures=lib.UniqueTextureCount;report.texturePixels=lib.DecodedTexturePixels;
            Assert("121-bindings-107-native-textures-51479000-pixels-no-fallback",lib.SuppliedAssetCount==121 && lib.UniqueTextureCount==107 && lib.DecodedTexturePixels==51479000 && lib.Warnings.Count==0 && lib.EffectiveTexturePixelLimit==PresentationMediaLibrary.FullBaseSetTexturePixelLimit);
            var hashes=new Dictionary<Texture2D,string>();Texture2D common=null;int holos=0;
            foreach(var b in oracle.bindings) {
                var texture=Resolve(lib,b);Assert("actual-dimensions:"+b.role+":"+b.cardId,texture!=null && texture.width==b.width && texture.height==b.height);
                bool mask=b.role.EndsWith("mask",StringComparison.Ordinal);
                Assert("native-linear-readable:"+b.role+":"+b.cardId,texture.isReadable && texture.mipmapCount==1 && texture.isDataSRGB!=mask && texture.wrapMode==TextureWrapMode.Clamp && texture.filterMode==FilterMode.Bilinear);
                string digest;if(!hashes.TryGetValue(texture,out digest)){digest=Pixels(texture);hashes.Add(texture,digest);}
                Assert("actual-full-rgba-readback:"+b.role+":"+b.cardId,digest==b.rgbaSha256);
                if(b.role=="holo-mask") {if(common==null)common=texture;Assert("shared-holo-mask:"+b.cardId,ReferenceEquals(common,texture));holos++;}
            }
            Assert("15-shared-mask-bindings",holos==15 && hashes.Count==107);
            foreach(string action in new[]{"generate","open","next","skip"})Assert("original-generic-foley:"+action,lib.Sound(action)!=null && lib.Sound(action).samples>0);
            ShaderProof("holo",common,"Presentation/Phase3Foil");ShaderProof("wrapper-front",lib.WrapperFrontMask,"Presentation/WrapperMetallic");ShaderProof("wrapper-back",lib.WrapperBackMask,"Presentation/WrapperMetallic");
            var catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(game.catalogJson.text));
            using(var limited=PresentationMediaLibrary.Load(Root,catalog,Manifest))Assert("default16MiPixel-budget-not-elevated",limited.EffectiveTexturePixelLimit==PresentationMediaLibrary.TotalTexturePixelLimit && limited.SuppliedAssetCount<121 && limited.Warnings.Count>0);
            yield return null; // deferred native Destroy from bounded default-budget load
            for(int pass=0;pass<2;pass++) {
                var reload=PresentationMediaLibrary.Load(Root,catalog,Manifest,false,true);var owned=new HashSet<Texture2D>();foreach(var b in oracle.bindings)owned.Add(Resolve(reload,b));var clip=reload.Sound("generate");
                Assert("reload-full-no-fallback:"+pass,reload.SuppliedAssetCount==121 && owned.Count==107 && reload.Warnings.Count==0);
                reload.Dispose();reload.Dispose();yield return null;yield return null;
                bool destroyed=clip==null;foreach(var t in owned)destroyed&=t==null;
                Assert("deferred-native-dispose-idempotent:"+pass,destroyed && reload.UniqueTextureCount==0 && reload.DecodedTexturePixels==0 && reload.SuppliedAssetCount==0);
            }
            Assert("host-original-library-still-bound",ReferenceEquals(table.Media,lib) && lib.SuppliedAssetCount==121);
        }
        private void ShaderProof(string name,Texture2D mask,string resource) {
            var shader=Resources.Load<Shader>(resource);Assert("native-shader-supported:"+name,shader!=null && shader.isSupported);
            var material=new Material(shader);var rt=new RenderTexture(mask.width,mask.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);Texture2D a=null,b=null;var previous=RenderTexture.active;
            try {
                rt.Create();material.SetTexture("_MaskTex",mask);
                a=RenderOverlay(rt,material,0);b=RenderOverlay(rt,material,2.25f);
                var m=mask.GetPixels32();var x=a.GetPixels32();var y=b.GetPixels32();int outside=0,inside=0,changed=0;bool bounded=true;
                for(int n=0;n<m.Length;n++) {int row=n/mask.width,col=n%mask.width;if(row<2 || col<2 || row>=mask.height-2 || col>=mask.width-2)continue;
                    if(m[n].r==0 && m[n].a==255 && m[n-1].r==0 && m[n+1].r==0 && m[n-mask.width].r==0 && m[n+mask.width].r==0){outside++;if(x[n].r>1 || y[n].r>1)bounded=false;}
                    if(m[n].r>128 && m[n].a>128){inside++;if(x[n].r!=y[n].r || x[n].g!=y[n].g || x[n].b!=y[n].b)changed++;}
                }
                Assert("shader-actual-mask-inside-vs-outside:"+name,bounded && outside>0 && inside>0 && changed>0,"outside="+outside+" inside="+inside+" animated="+changed);
                string file="materials-shader-"+name+".png";File.WriteAllBytes(Path.Combine(directory,file),b.EncodeToPNG());images.Add(file);
            } finally {RenderTexture.active=previous;if(a!=null)Destroy(a);if(b!=null)Destroy(b);rt.Release();Destroy(rt);Destroy(material);}
        }
        private static Texture2D RenderOverlay(RenderTexture rt,Material material,float clock) {RenderTexture.active=rt;GL.Clear(true,true,Color.clear);material.SetFloat("_PresentationTime",clock);Graphics.Blit(Texture2D.whiteTexture,rt,material);var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();return t;}
        private float Clock() {var motion=typeof(TabletopPresentation).GetField("motion",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(table);return (float)motion.GetType().GetProperty("Clock").GetValue(motion,null);}
        private IEnumerator Demo() {
            Assert("demo-owned-fresh-start",game.CurrentSession.Packs.Count==0 && game.GeneratedResultCount==0 && table.CommittedCueCount==0);
            if(game.CurrentView.paused)game.DispatchCommand("resume");
            yield return Capture("materials-wrapper-front-preview");
            var original=table.Media;try {table.Media=new BackOnly(original);yield return Capture("materials-wrapper-back-isolated-demo");}finally{table.Media=original;}
            game.GenerateForSmoke(4u);Assert("explicit-seed4-generated-once",game.GeneratedResultCount==1 && game.CurrentSession.TotalVirtualCards==11);yield return Capture("materials-sealed-wrapper");
            game.DispatchCommand("open");Assert("actual-reveal-zero",game.CurrentView.stage=="revealing" && game.CurrentView.revealedCount==0);yield return Capture("materials-card-back-reveal-zero");
            game.FailNextCommitForSmoke();game.DispatchCommand("next");Assert("actual-save-error-no-reveal",game.CurrentView.stage=="error" && game.CurrentView.revealedCount==0);
            yield return null;float frozen=Clock();int cues=table.CommittedCueCount;yield return new WaitForSecondsRealtime(.18f);
            Assert("error-freezes-existing-animation-clock-and-stops-audio",Clock()==frozen && Silent());yield return Capture("materials-save-error");
            game.DispatchCommand("retry");Assert("same-pack-retry-no-reroll",game.CurrentView.revealedCount==1 && game.GeneratedResultCount==1 && game.CurrentSession.TotalVirtualCards==11);
            yield return Capture("materials-partial-nonholo");
            game.DispatchCommand("pause");yield return null;frozen=Clock();cues=table.CommittedCueCount;yield return new WaitForSecondsRealtime(.18f);
            Assert("pause-freezes-existing-animation-clock-and-stops-audio",Clock()==frozen && Silent());yield return Capture("materials-pause");game.DispatchCommand("resume");yield return null;
            Assert("resume-no-auto-audio-replay",table.CommittedCueCount==cues && Silent());
            table.SendMessage("OnApplicationFocus",false);yield return null;frozen=Clock();yield return new WaitForSecondsRealtime(.15f);Assert("focus-loss-pauses-and-freezes-clock",game.CurrentView.paused && Clock()==frozen && Silent());table.SendMessage("OnApplicationFocus",true);game.DispatchCommand("resume");
            game.DispatchCommand("volume:0");game.DispatchCommand("next");cues=table.CommittedCueCount;game.DispatchCommand("volume:0.5");yield return null;Assert("unmute-no-auto-replay",table.CommittedCueCount==cues && Silent());
            for(int n=game.CurrentView.revealedCount;n<11;n++)game.DispatchCommand("next");
            Assert("accepted-card11-hold-before-explicit-results",game.CurrentView.stage=="revealing" && game.CurrentView.revealedCount==11 && game.CurrentView.inspectIndex==-1);yield return Capture("materials-card11-held");
            int holo=-1,plain=-1;for(int n=0;n<11;n++){if(host.Library.HoloMask(game.CurrentView.cards[n].id)!=null)holo=n;else plain=n;}
            Assert("seed4-real-holo-and-nonholo",holo>=0 && plain>=0);
            game.DispatchCommand("inspect:"+holo);yield return Capture("materials-holo-inspection");game.DispatchCommand("close-inspect");game.DispatchCommand("inspect:"+plain);yield return Capture("materials-nonholo-inspection");game.DispatchCommand("close-inspect");
            game.DispatchCommand("binder-open");yield return Capture("materials-binder-complete");game.DispatchCommand("binder-close");Assert("binder-back-keeps-final-card11",game.CurrentView.stage=="revealing" && game.CurrentView.inspectIndex==-1);
            game.DispatchCommand("results");Assert("explicit-results",game.CurrentView.stage=="results");yield return Capture("materials-explicit-results");
        }
        private bool Silent() {foreach(var source in gameObject.GetComponentsInChildren<AudioSource>(true))if(source.isPlaying || source.clip!=null)return false;return true;}
        private IEnumerator Capture(string name) {
            yield return null;yield return new WaitForEndOfFrame();string file=Path.Combine(directory,name+".png");ScreenCapture.CaptureScreenshot(file);float deadline=Time.realtimeSinceStartup+8;
            var method=typeof(Phase4SmokeProbe).GetMethod("CompletePng",BindingFlags.Static|BindingFlags.NonPublic);bool done=false;
            do {yield return null;done=(bool)method.Invoke(null,new object[]{file,Screen.width,Screen.height});}while(!done && Time.realtimeSinceStartup<deadline);
            Assert("actual-framebuffer:"+name,done);images.Add(name+".png");
        }
        private sealed class BackOnly : IPresentationMedia,IWrapperMetallicMedia {
            private readonly IPresentationMedia source;public BackOnly(IPresentationMedia source){this.source=source;}
            public Texture2D CardFace(string id){return source.CardFace(id);}public Texture2D HoloMask(string id){return source.HoloMask(id);}public Texture2D CardBack {get{return source.CardBack;}}public Texture2D WrapperFront {get{return null;}}public Texture2D WrapperBack {get{return source.WrapperBack;}}public Texture2D WrapperFrontMask {get{return null;}}public Texture2D WrapperBackMask {get{return ((IWrapperMetallicMedia)source).WrapperBackMask;}}public AudioClip Sound(string action){return source.Sound(action);}public string StatusText {get{return source.StatusText;}}public int SuppliedAssetCount {get{return source.SuppliedAssetCount;}}
        }
        private void Publish() {if(!active || report==null)return;report.passed=complete && failure==null && checks.Count>0 && checks.TrueForAll(c=>c.passed);report.checkCount=checks.Count;report.checks=checks.ToArray();report.images=images.ToArray();report.failure=failure;string file=Path.Combine(directory,"materials-report.json"),work=file+".writing";File.WriteAllText(work,JsonUtility.ToJson(report,true));if(File.Exists(file))File.Replace(work,file,null);else File.Move(work,file);}
        private void OnApplicationQuit() {if(active && !complete){failure="Application exited before material assertions completed";Publish();}}
    }
}
