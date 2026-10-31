using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VintageCollector.Domain;
using VintageCollector.Media;

public static class MaterialsMediaNativeChecks
{
    [Serializable] sealed class Result { public string name; public bool passed; public string error; }
    [Serializable] sealed class Report { public int testCount; public bool passed; public Result[] checks; public FullSetMetric[] fullSet; }
    [Serializable] sealed class FullSetMetric { public bool synthetic,processWorkingSetAvailable; public int pass,suppliedBindings,uniqueTextures,fullPixelHashBindings; public long texturePixels,effectiveBudget,nativeTextureRuntimeBytes,managedHeapBytes,processWorkingSetBytes; }
    [Serializable] sealed class PixelSample { public int x,y,r,g,b,a; }
    [Serializable] sealed class ImageBinding { public string role,cardId,relativePath,sha256,rgbaSha256; public int width,height; public bool mask; public PixelSample[] samples; }
    [Serializable] sealed class FullOracle { public bool synthetic; public string manifestRelativePath; public int expectedBindings,expectedUnique; public long expectedPixels; public ImageBinding[] bindings; }
    static readonly List<FullSetMetric> fullMetrics=new List<FullSetMetric>();
    static Texture2D Binding(PresentationMediaLibrary media,ImageBinding binding) {
        switch(binding.role) {
            case "card-face": return media.CardFace(binding.cardId);
            case "holo-mask": return media.HoloMask(binding.cardId);
            case "card-back": return media.CardBack;
            case "wrapper-front": return media.WrapperFront;
            case "wrapper-back": return media.WrapperBack;
            case "wrapper-front-mask": return media.WrapperFrontMask;
            case "wrapper-back-mask": return media.WrapperBackMask;
            default: throw new Exception("Unexpected full-set role");
        }
    }
    static void NoLeaks() {
        foreach(Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>()) Assert(!texture.name.StartsWith("Verified optional texture:"),"No owned texture leaks");
        foreach(AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>()) Assert(!clip.name.StartsWith("Original generic foley:") && !clip.name.StartsWith("Verified optional cue:"),"No owned clip leaks");
        Assert(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length==0,"No automatic AudioSource");
    }
    [Serializable] sealed class Entry { public string role,cardId="",relativePath,source="synthetic private token=never-log",rights="unknown",restrictions="test only",sha256; }
    [Serializable] sealed class Manifest { public int schemaVersion=1; public string status="synthetic",intendedUse="test",redistributionPermission="none",rightsDefault="unknown",notes=""; public Entry[] assets; }
    static readonly List<Result> results=new List<Result>();
    static string root,output,filter; static Catalog catalog;
    static void Assert(bool ok,string message) { if(!ok) throw new Exception(message); }
    static void Check(string name,Action action) { if(!name.StartsWith(filter,StringComparison.Ordinal)) return; try { action(); results.Add(new Result{name=name,passed=true}); } catch(Exception e) { results.Add(new Result{name=name,passed=false,error=e.ToString()}); } }
    static string Relative(string path) { return path.Substring(root.Length+1).Replace('\\','/'); }
    static Entry Asset(string role,string file,string id="") { return new Entry{role=role,cardId=id,relativePath=Relative(Path.Combine(output,file)),sha256=MediaFiles.Sha256(File.ReadAllBytes(Path.Combine(output,file)))}; }
    static string ManifestFile(params Entry[] entries) { string file=Path.Combine(output,"manifest.json"); File.WriteAllText(file,JsonUtility.ToJson(new Manifest{assets=entries},true)); return Relative(file); }
    static Texture2D Mask(PresentationMediaLibrary media,bool front) { var p=media.GetType().GetProperty(front?"WrapperFrontMask":"WrapperBackMask"); Assert(p!=null,"Separate optional wrapper mask API must exist"); return (Texture2D)p.GetValue(media,null); }
    public static void Run()
    {
        root=Environment.GetEnvironmentVariable("PHASE3_MEDIA_ROOT"); output=Environment.GetEnvironmentVariable("PHASE3_MEDIA_OUTPUT"); filter=Environment.GetEnvironmentVariable("MATERIALS_MEDIA_FILTER")??"";
        catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"))));
        Check("wrapper-four-roles-order-linear-exact-native-pixels",()=> {
            var optional=typeof(IPresentationMedia).Assembly.GetType("VintageCollector.Media.IWrapperMetallicMedia");
            Assert(optional!=null && !typeof(IPresentationMedia).GetInterfaces().Contains(optional),"Add separate IWrapperMetallicMedia without changing old provider contract");
            foreach(bool reverse in new[]{false,true}) {
                var entries=new[]{Asset("wrapper-front-mask","wrapper-mask.png"),Asset("wrapper-back-mask","wrapper-mask.png"),Asset("wrapper-front","original-synthetic-wrapper.png"),Asset("wrapper-back","original-synthetic-wrapper.png")};
                if(reverse) Array.Reverse(entries);
                using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries),true)) {
                    Assert(optional.IsInstanceOfType(media),"Library implements optional contract");
                    Assert(media.SuppliedAssetCount==4 && media.Warnings.Count==0,"All four wrapper roles accepted");
                    foreach(bool front in new[]{false,true}) {
                        Texture2D mask=Mask(media,front), face=front?media.WrapperFront:media.WrapperBack;
                        Assert(mask!=null && mask.width==face.width && mask.height==face.height,"Exact native mask alignment");
                        Assert(!mask.isDataSRGB,"Native mask texture must be linear");
                        Color32 pixel=mask.GetPixel(0,0); Assert(pixel.r==128 && pixel.g==128 && pixel.b==128 && pixel.a==255,"Native mask pixel readback");
                        Assert(!mask.mipmapCount.Equals(0) && mask.mipmapCount==1 && mask.isReadable && mask.wrapMode==TextureWrapMode.Clamp && mask.filterMode==FilterMode.Bilinear,"Native texture configuration");
                    }
                }
            }
        });
        Check("budget-trusted-explicit-full-limit-default-preserved",()=> {
            var field=typeof(PresentationMediaLibrary).GetField("FullBaseSetTexturePixelLimit");
            Assert(field!=null && (long)field.GetRawConstantValue()==64L*1024*1024,"Exposed full set limit constant");
            var load=typeof(PresentationMediaLibrary).GetMethod("Load");
            var parameters=load.GetParameters();
            Assert(parameters.Length==5 && parameters[4].Name=="fullBaseSetBudget" && (bool)parameters[4].DefaultValue==false,"Trusted appended optional flag defaults false");
            var entries=new List<Entry>();
            for(int i=0;i<4;i++) entries.Add(Asset("card-face","unique-large-"+i+".png",catalog.Cards[i].Id));
            entries.Add(Asset("wrapper-front","original-synthetic-wrapper.png"));
            string manifest=ManifestFile(entries.ToArray());
            foreach(bool full in new[]{false,true}) using(var media=(PresentationMediaLibrary)load.Invoke(null,new object[]{root,catalog,manifest,true,full})) {
                Assert(media.SuppliedAssetCount==(full?5:3) && media.Warnings.Count==(full?0:2),"Default 16MiPixel versus explicitly trusted 64MiPixel");
                Assert(media.WrapperFront!=null && media.Sound("open")!=null,"Budget rejection independent");
                var effective=media.GetType().GetProperty("EffectiveTexturePixelLimit"); Assert(effective!=null && (long)effective.GetValue(media,null)==(full?64L:16L)*1024*1024,"Read-only effective budget evidence");
            }
            string path=Path.Combine(root,manifest); File.WriteAllText(path,File.ReadAllText(path).Replace("\"schemaVersion\": 1,","\"schemaVersion\": 1, \"fullBaseSetBudget\": true,"));
            using(var media=PresentationMediaLibrary.Load(root,catalog,manifest,true)) Assert(media.SuppliedAssetCount==0 && media.Warnings.Count>0,"Untrusted manifest cannot raise policy");
        });
        Check("cache-fifteen-holo-bindings-one-native-mask-unique-accounting-disposal",()=> {
            var holos=catalog.Cards.Where(c=>c.Finish=="holo").ToArray(); Assert(holos.Length==15,"Frozen fifteen eligible identities");
            var entries=new List<Entry>();
            foreach(var c in holos) { entries.Add(Asset("holo-mask","original-synthetic-mask.png",c.Id)); entries.Add(Asset("card-face","original-synthetic-card.png",c.Id)); }
            string manifest=ManifestFile(entries.ToArray());
            for(int pass=0;pass<2;pass++) {
                var media=PresentationMediaLibrary.Load(root,catalog,manifest,true);
                try {
                    Assert(media.SuppliedAssetCount==30 && media.Warnings.Count==0,"Successful binding count remains thirty");
                    Texture2D face=media.CardFace(holos[0].Id), mask=media.HoloMask(holos[0].Id);
                    foreach(var c in holos) Assert(object.ReferenceEquals(mask,media.HoloMask(c.Id)) && object.ReferenceEquals(face,media.CardFace(c.Id)),"Verified identical bytes share one native resource per semantics");
                    var count=media.GetType().GetProperty("UniqueTextureCount"); var pixels=media.GetType().GetProperty("DecodedTexturePixels");
                    Assert(count!=null && pixels!=null && (int)count.GetValue(media,null)==2 && (long)pixels.GetValue(media,null)==2L*126*176,"Read-only unique resource pixel accounting");
                    media.Dispose(); media.Dispose();
                    Assert(face==null && mask==null && media.SuppliedAssetCount==0 && (int)count.GetValue(media,null)==0 && (long)pixels.GetValue(media,null)==0,"Shared native resources destroyed once and accounting cleared");
                } finally { media.Dispose(); }
            }
        });
        foreach(string role in new[]{"wrapper-front-mask","wrapper-back-mask"}) {
            string current=role, faceRole=role=="wrapper-front-mask"?"wrapper-front":"wrapper-back";
            foreach(string failure in new[]{"orphan","opposite-face-only","misaligned","bad-face","duplicate","with-id","hash-invalid","unsupported-png","unknown-role"}) {
                string mode=failure;
                Check("wrapper-reject-"+current+"-"+mode,()=> {
                    var entries=new List<Entry>();
                    Entry mask=Asset(current,mode=="misaligned"?"wrapper-wrong-mask.png":mode=="unsupported-png"?"original-synthetic-not-png.png":"wrapper-mask.png");
                    Entry face=Asset(faceRole,"original-synthetic-wrapper.png");
                    if(mode=="with-id") mask.cardId=catalog.Cards[0].Id;
                    if(mode=="hash-invalid") mask.sha256=new string('0',64);
                    if(mode=="unknown-role") mask.role=current+"-unknown";
                    if(mode=="bad-face") face.sha256=new string('0',64);
                    entries.Add(mask);
                    if(mode=="duplicate") { Entry duplicate=Asset(current,"wrapper-mask.png"); duplicate.rights=""; entries.Add(duplicate); }
                    if(mode=="opposite-face-only") entries.Add(Asset(faceRole=="wrapper-front"?"wrapper-back":"wrapper-front","original-synthetic-wrapper.png"));
                    else if(mode!="orphan") entries.Add(face);
                    entries.Add(Asset("card-back","original-synthetic-card.png"));
                    using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries.ToArray()),true)) {
                        Assert(Mask(media,current=="wrapper-front-mask")==null,"Invalid wrapper mask must never bind or alias another role");
                        Assert(media.CardBack!=null && media.Sound("skip")!=null && media.Warnings.Count>0,"Safe independent art and generic cue fallbacks");
                        Assert(!string.Join(" ",media.Warnings).Contains("never-log") && !media.StatusText.Contains("never-log"),"No untrusted provenance credentials in diagnostics");
                    }
                });
            }
        }
        Check("cache-shared-hit-validates-every-id-face-even-after-success",()=> {
            var holos=catalog.Cards.Where(c=>c.Finish=="holo").Take(3).ToArray();
            foreach(bool failedFirst in new[]{false,true}) {
                var entries=new List<Entry>{Asset("card-face","original-synthetic-card.png",holos[0].Id),Asset("card-face","original-synthetic-other-mask.png",holos[1].Id)};
                Entry good=Asset("holo-mask","original-synthetic-mask.png",holos[0].Id), mismatch=Asset("holo-mask","original-synthetic-mask.png",holos[1].Id), orphan=Asset("holo-mask","original-synthetic-mask.png",holos[2].Id);
                entries.AddRange(failedFirst?new[]{mismatch,orphan,good}:new[]{good,mismatch,orphan});
                using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries.ToArray()),true)) {
                    Assert(media.HoloMask(holos[0].Id)!=null && media.HoloMask(holos[1].Id)==null && media.HoloMask(holos[2].Id)==null,"Cache hit cannot authorize mismatched or orphan identity; failed admission does not poison later valid mask");
                    Assert(media.SuppliedAssetCount==3 && media.Warnings.Count==2 && media.UniqueTextureCount==3,"Only validated bindings and unique resources charged");
                }
            }
        });
        Check("cache-wrapper-shared-hit-does-not-authorize-missing-opposite-face",()=> {
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("wrapper-front","original-synthetic-wrapper.png"),Asset("wrapper-front-mask","wrapper-mask.png"),Asset("wrapper-back-mask","wrapper-mask.png")),true)) {
                Assert(media.WrapperFrontMask!=null && media.WrapperBackMask==null && media.SuppliedAssetCount==2 && media.Warnings.Count==1,"Corresponding wrapper face required before shared cache access");
            }
        });
        Check("cache-identical-bytes-face-versus-linear-mask-never-alias",()=> {
            string id=catalog.Cards.First(c=>c.Finish=="holo").Id;
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("card-face","original-synthetic-card.png",id),Asset("holo-mask","original-synthetic-card.png",id),Asset("wrapper-front","original-synthetic-card.png"),Asset("wrapper-front-mask","original-synthetic-card.png")),true)) {
                Assert(media.SuppliedAssetCount==4 && media.UniqueTextureCount==2 && media.DecodedTexturePixels==2L*126*176,"Mask semantics separate identical verified bytes from color faces");
                Assert(!object.ReferenceEquals(media.CardFace(id),media.HoloMask(id)) && object.ReferenceEquals(media.HoloMask(id),media.WrapperFrontMask),"Only compatible linear semantics share");
                Assert(media.CardFace(id).isDataSRGB && !media.HoloMask(id).isDataSRGB,"Actual native color-space separation");
            }
        });
        Check("cache-hit-still-verifies-current-path-and-hash",()=> {
            string id=catalog.Cards.First(c=>c.Finish=="holo").Id;
            foreach(string mode in new[]{"hash-mismatch","missing","unsupported","unsafe-path"}) {
                Entry invalid=Asset("wrapper-front","original-synthetic-card.png");
                if(mode=="hash-mismatch") invalid.relativePath=Asset("wrapper-front","original-synthetic-mask.png").relativePath;
                if(mode=="missing") invalid.relativePath=Relative(Path.Combine(output,"absent.png"));
                if(mode=="unsafe-path") invalid.relativePath="Assets/Supplied/../escape.png";
                if(mode=="unsupported") invalid.relativePath=Asset("wrapper-front","original-synthetic-not-png.png").relativePath;
                using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("card-face","original-synthetic-card.png",id),invalid),true)) Assert(media.CardFace(id)!=null && media.WrapperFront==null && media.SuppliedAssetCount==1 && media.UniqueTextureCount==1 && media.Warnings.Count==1,"Cached claimed digest never bypasses bytes/path verification");
            }
        });
        Check("cache-failed-native-mask-decode-never-admits-or-poisons-later-binding",()=> {
            var holos=catalog.Cards.Where(c=>c.Finish=="holo").Take(3).ToArray();
            var entries=new List<Entry>();
            foreach(var card in holos) entries.Add(Asset("card-face","original-synthetic-card.png",card.Id));
            entries.Add(Asset("holo-mask","original-synthetic-bad-zlib.png",holos[0].Id));
            entries.Add(Asset("holo-mask","original-synthetic-bad-zlib.png",holos[1].Id));
            entries.Add(Asset("holo-mask","original-synthetic-mask.png",holos[2].Id));
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries.ToArray()),true)) Assert(media.HoloMask(holos[0].Id)==null && media.HoloMask(holos[1].Id)==null && media.HoloMask(holos[2].Id)!=null && media.SuppliedAssetCount==4 && media.UniqueTextureCount==2 && media.DecodedTexturePixels==2L*126*176 && media.Warnings.Count==2,"Actual native failed LoadImage resources destroyed, not cached or charged; subsequent valid mask independent");
            NoLeaks();
        });
        Check("cache-budget-charges-only-unique-before-admission-hit-free",()=> {
            var entries=new List<Entry>();
            for(int i=0;i<4;i++) entries.Add(Asset("card-face",i==3?"unique-large-0.png":"unique-large-"+i+".png",catalog.Cards[i].Id));
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries.ToArray()),true)) Assert(media.SuppliedAssetCount==3 && media.UniqueTextureCount==2 && media.DecodedTexturePixels==2L*2016*2816 && media.CardFace(catalog.Cards[2].Id)==null && object.ReferenceEquals(media.CardFace(catalog.Cards[0].Id),media.CardFace(catalog.Cards[3].Id)),"Shared hit admitted after unique over-budget rejection, charged once");
        });
        Check("budget-full-sixtyfour-is-bounded-not-unlimited",()=> {
            var entries=new List<Entry>();
            for(int i=0;i<12;i++) entries.Add(Asset("card-face","unique-large-"+i+".png",catalog.Cards[i].Id));
            entries.Add(Asset("wrapper-front","original-synthetic-wrapper.png"));
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries.ToArray()),true,true)) Assert(media.SuppliedAssetCount==12 && media.UniqueTextureCount==12 && media.Warnings.Count==1 && media.CardFace(catalog.Cards[11].Id)==null && media.WrapperFront!=null && media.DecodedTexturePixels<=media.EffectiveTexturePixelLimit,"Explicit full set opt-in still rejects above 64MiPixel without blocking later small resource");
        });
        Check("lookup-no-io-and-native-lifecycle-no-leaks",()=> {
            string file=Path.Combine(output,"lookup-mask.png"); File.Copy(Path.Combine(output,"wrapper-mask.png"),file,true);
            var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("wrapper-front","original-synthetic-wrapper.png"),Asset("wrapper-front-mask","lookup-mask.png")),true);
            try {
                Texture2D face=media.WrapperFront, mask=media.WrapperFrontMask; File.Delete(file);
                Assert(object.ReferenceEquals(face,media.WrapperFront) && object.ReferenceEquals(mask,media.WrapperFrontMask),"Lookups retain decoded resources without filesystem access");
                media.Dispose(); media.Dispose(); Assert(face==null && mask==null,"Native objects actually destroyed");
            } finally { media.Dispose(); }
            foreach(Texture2D t in Resources.FindObjectsOfTypeAll<Texture2D>()) Assert(!t.name.StartsWith("Verified optional texture:"),"No retained native texture after all test scopes");
            foreach(AudioClip c in Resources.FindObjectsOfTypeAll<AudioClip>()) Assert(!c.name.StartsWith("Original generic foley:") && !c.name.StartsWith("Verified optional cue:"),"No retained native audio");
            Assert(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length==0,"No automatic playback source");
        });
        if(Environment.GetEnvironmentVariable("MATERIALS_MEDIA_FULL_SET")=="1") Check("full-set-101-native-full-quality-all-bindings-rgba-hashes-budget-reload-dispose",()=> {
            FullOracle oracle=JsonUtility.FromJson<FullOracle>(File.ReadAllText(Path.Combine(output,"full-set-oracle.json")));
            Assert(oracle.expectedBindings==121 && oracle.bindings.Count(b=>b.role=="card-face")==101 && oracle.bindings.Count(b=>b.role=="holo-mask")==15,"Exact catalog faces and explicit holo mask binding coverage");
            Assert(oracle.bindings.Where(b=>b.role=="card-face").Select(b=>b.cardId).OrderBy(s=>s).SequenceEqual(catalog.Cards.Select(c=>c.Id).OrderBy(s=>s)),"No missing identities or Machamp substitution");
            using(var limited=PresentationMediaLibrary.Load(root,catalog,oracle.manifestRelativePath,oracle.synthetic)) {
                Assert(limited.EffectiveTexturePixelLimit==16L*1024*1024 && limited.DecodedTexturePixels<=limited.EffectiveTexturePixelLimit && limited.Warnings.Count>0,"Default API budget unchanged on complete 101 full-quality set");
                Assert(catalog.Cards.Count(c=>limited.CardFace(c.Id)!=null)<101,"No silent budget escalation");
                foreach(var c in catalog.Cards) { Texture2D face=limited.CardFace(c.Id); if(face!=null) Assert(face.width==600 && face.height==825,"No silent rescale under default budget"); }
            }
            NoLeaks();
            for(int pass=0;pass<2;pass++) {
                var resources=new List<Texture2D>(); var clips=new List<AudioClip>();
                var media=PresentationMediaLibrary.Load(root,catalog,oracle.manifestRelativePath,oracle.synthetic,true);
                try {
                    Assert(media.Warnings.Count==0 && media.SuppliedAssetCount==121 && media.Provenance.Count==121,"Full-set trusted opt-in has no art fallback");
                    Assert(media.EffectiveTexturePixelLimit==64L*1024*1024 && media.UniqueTextureCount==oracle.expectedUnique && media.DecodedTexturePixels==oracle.expectedPixels && media.DecodedTexturePixels<=media.EffectiveTexturePixelLimit,"Exact independent unique-pixel budget accounting");
                    var readbackHashes=new Dictionary<Texture2D,string>();
                    foreach(var binding in oracle.bindings) {
                        Texture2D texture=Binding(media,binding);
                        Assert(texture!=null && texture.width==binding.width && texture.height==binding.height,"Every actual native binding has original dimensions");
                        if(binding.role=="card-face") Assert(texture.width==600 && texture.height==825,"101 original full-quality 600x825 faces");
                        Assert(texture.isDataSRGB!=binding.mask && texture.isReadable && texture.mipmapCount==1,"Native readable linear masks versus sRGB faces");
                        foreach(var sample in binding.samples) { Color32 pixel=texture.GetPixel(sample.x,sample.y); Assert(pixel.r==sample.r && pixel.g==sample.g && pixel.b==sample.b && pixel.a==sample.a,"Actual native GetPixel agrees with independent Pillow RGBA sample"); }
                        string hash;
                        if(!readbackHashes.TryGetValue(texture,out hash)) {
                            Color32[] pixels=texture.GetPixels32(); byte[] rgba=new byte[pixels.Length*4];
                            for(int i=0;i<pixels.Length;i++) { rgba[4*i]=pixels[i].r; rgba[4*i+1]=pixels[i].g; rgba[4*i+2]=pixels[i].b; rgba[4*i+3]=pixels[i].a; }
                            hash=MediaFiles.Sha256(rgba); readbackHashes.Add(texture,hash); resources.Add(texture);
                        }
                        Assert(hash==binding.rgbaSha256,"Complete native RGBA readback hash matches independently decoded source pixels");
                        MediaEntry provenance=media.Provenance.Single(p=>p.Role==binding.role && p.CardId==binding.cardId);
                        Assert(provenance.Sha256==binding.sha256 && provenance.RelativePath==binding.relativePath,"Exact validated per-binding private provenance");
                    }
                    Assert(resources.Count==media.UniqueTextureCount,"Actual distinct native objects match accounting");
                    var holos=catalog.Cards.Where(c=>c.Finish=="holo").ToArray();
                    foreach(var card in holos) Assert(object.ReferenceEquals(media.HoloMask(card.Id),media.HoloMask(holos[0].Id)),"All fifteen holo bindings share exactly one Texture2D");
                    foreach(var card in catalog.Cards.Where(c=>c.Finish!="holo")) Assert(media.HoloMask(card.Id)==null,"Unchanged holo-only identity rules");
                    foreach(string cue in new[]{"generate","open","next","skip"}) { AudioClip clip=media.Sound(cue); Assert(clip!=null && clip.name.StartsWith("Original generic foley:"),"Original generic foley remains fallback"); clips.Add(clip); }
                    long nativeBytes=resources.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t));
                    fullMetrics.Add(new FullSetMetric{synthetic=oracle.synthetic,pass=pass,suppliedBindings=media.SuppliedAssetCount,uniqueTextures=media.UniqueTextureCount,fullPixelHashBindings=oracle.bindings.Length,texturePixels=media.DecodedTexturePixels,effectiveBudget=media.EffectiveTexturePixelLimit,nativeTextureRuntimeBytes=nativeBytes,managedHeapBytes=GC.GetTotalMemory(false),processWorkingSetBytes=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,processWorkingSetAvailable=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64>0});
                    media.Dispose(); media.Dispose();
                    Assert(resources.All(t=>t==null) && clips.All(c=>c==null) && media.UniqueTextureCount==0 && media.DecodedTexturePixels==0 && media.SuppliedAssetCount==0,"All actual native resources destroyed and counters reset");
                } finally { media.Dispose(); }
                NoLeaks();
            }
        });
        bool passed=results.Count>0 && results.All(r=>r.passed);
        File.WriteAllText(Path.Combine(output,"native-results.json"),JsonUtility.ToJson(new Report{testCount=results.Count,passed=passed,checks=results.ToArray(),fullSet=fullMetrics.ToArray()},true));
        EditorApplication.Exit(passed?0:1);
    }
}
