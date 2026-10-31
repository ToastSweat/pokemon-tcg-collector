using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using VintageCollector.Domain;
using VintageCollector.Media;

public static class MediaNativeChecks
{
    [Serializable] sealed class Result { public string name; public bool passed; public string error; }
    [Serializable] sealed class CueMetric { public string cue; public int sampleRate,channels,samples; public float peak,rms,seconds; }
    static readonly List<CueMetric> metrics=new List<CueMetric>();
    [Serializable] sealed class Report { public int testCount; public bool passed; public string evidence="Native Unity Editor isolated media library, not core-connected player or physical hearing"; public Result[] checks; public CueMetric[] genericCues; }
    static readonly List<Result> results=new List<Result>();
    static string root,output;
    static Catalog catalog;
    static void Assert(bool value,string message) { if(!value) throw new Exception(message); }
    static void Check(string name,Action test) { try { test(); results.Add(new Result{name=name,passed=true}); } catch(Exception ex) { results.Add(new Result{name=name,passed=false,error=ex.ToString()}); } }
    static string Relative(string file) { return file.Substring(root.Length+1).Replace('\\','/'); }
    [Serializable] sealed class Entry { public string role,cardId="",relativePath,source="original synthetic fixture https://synthetic.invalid/?token=never-log",rights="unknown",restrictions="test only",sha256; }
    [Serializable] sealed class Manifest { public int schemaVersion=1; public string status="original-synthetic-fixture",intendedUse="test only",redistributionPermission="not established",rightsDefault="unknown",notes="Not supplied materials"; public Entry[] assets; }
    static Entry Asset(string role,string file,string id="") { return new Entry{role=role,cardId=id,relativePath=Relative(Path.Combine(output,file)),sha256=MediaFiles.Sha256(File.ReadAllBytes(Path.Combine(output,file)))}; }
    static string ManifestFile(params Entry[] entries) { string file=Path.Combine(output,"media-manifest.json"); File.WriteAllText(file,JsonUtility.ToJson(new Manifest{assets=entries},true)); return Relative(file); }
    static void RejectedEntry(string name,Action<Entry> change,string role="card-back",string file="original-synthetic-card.png",string id="")
    {
        Check("reject-entry-"+name,()=> {
            Entry invalid=Asset(role,file,id); change(invalid);
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(invalid,Asset("wrapper-front","original-synthetic-wrapper.png")),true)) {
                Assert(media.SuppliedAssetCount==1 && media.WrapperFront!=null && media.Warnings.Count==1,"Invalid binding rejected independently, only genuine successful fixture counts");
                Assert(media.CardBack==null && media.Sound("open").name.Contains("Original generic"),"Texture and sound fallbacks independent");
                Assert(!string.Join(" ",media.Warnings).Contains("never-log") && !media.StatusText.Contains("never-log"),"Credentials never exposed through status or warnings");
            }
        });
    }
    public static void Run()
    {
        root=Environment.GetEnvironmentVariable("PHASE3_MEDIA_ROOT"); output=Environment.GetEnvironmentVariable("PHASE3_MEDIA_OUTPUT");
        catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(File.ReadAllText(Path.Combine(root,"Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"))));
        Check("production-empty-zero-supplied-null-textures-real-cached-original-clips",()=> {
            using(var media=PresentationMediaLibrary.Load(root,catalog)) {
                Assert(media.SuppliedAssetCount==0,"Empty production manifest is not supplied content");
                Assert(media.Warnings.Count==0,"Empty valid manifest is not an error");
                Assert(media.CardFace(catalog.Cards[0].Id)==null && media.HoloMask(catalog.Cards[0].Id)==null && media.CardBack==null && media.WrapperFront==null && media.WrapperBack==null,"Null textures mean safe placeholders");
                Assert(media.StatusText.Contains("0 supplied") && media.StatusText.Contains("original"),"Honest generic status");
                foreach(string cue in new[]{"generate","open","next","skip"}) {
                    AudioClip clip=media.Sound(cue);
                    float[] expected=MediaSamples.Synthesize(cue), actual=new float[expected.Length];
                    Assert(clip!=null && clip==media.Sound(cue),"Cached real AudioClip");
                    Assert(clip.channels==1 && clip.frequency==MediaSamples.SampleRate && clip.samples==expected.Length,"Native sample metadata");
                    Assert(clip.GetData(actual,0),"Native sample readback");
                    float peak=0; double energy=0;
                    for(int i=0;i<actual.Length;i++) { Assert(Math.Abs(actual[i]-expected[i])<0.000001f,"Native PCM roundtrip"); peak=Math.Max(peak,Math.Abs(actual[i])); energy+=actual[i]*actual[i]; }
                    metrics.Add(new CueMetric{cue=cue,sampleRate=clip.frequency,channels=clip.channels,samples=clip.samples,seconds=clip.length,peak=peak,rms=(float)Math.Sqrt(energy/actual.Length)});
                }
                Assert(media.Sound("invalid")==null && media.Sound(null)==null,"Unknown action never plays anything");
                Assert(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length==0,"Passive provider creates no AudioSource or automatic playback");
            }
        });
        Check("missing-malformed-or-unsafe-manifest-falls-back-with-redacted-warnings",()=> {
            foreach(string relative in new[]{"Assets/Supplied/not-present.json","Assets/Supplied/../escape.json","https://synthetic.invalid/?token=never-log", ".artifacts/phase3-media/not-present.json"}) {
                using(var media=PresentationMediaLibrary.Load(root,catalog,relative,false)) {
                    Assert(media.Warnings.Count>0 && media.SuppliedAssetCount==0 && media.Sound("open")!=null,"Manifest failure never blocks independent original sound");
                    Assert(!string.Join(" ",media.Warnings).Contains("never-log"),"No untrusted source/path in warning");
                }
            }
            string file=Path.Combine(output,"media-manifest.json"), relativeTest=Relative(file);
            foreach(string content in new[]{"{}","{bad json","{\"schemaVersion\":2}","null",""}) {
                File.WriteAllText(file,content);
                using(var media=PresentationMediaLibrary.Load(root,catalog,relativeTest,true)) Assert(media.Warnings.Count>0 && media.SuppliedAssetCount==0 && media.Sound("next")!=null,"Malformed manifest fallback");
            }
            File.WriteAllText(file,"{\"schemaVersion\":1,\"status\":\"original-test\",\"intendedUse\":\"test\",\"redistributionPermission\":\"none\",\"rightsDefault\":\"unknown\",\"notes\":\"\",\"assets\":[]}");
            using(var media=PresentationMediaLibrary.Load(root,catalog,relativeTest,true)) Assert(media.Warnings.Count==0 && media.SuppliedAssetCount==0,"Explicit empty original test manifest accepted");
            using(var media=PresentationMediaLibrary.Load(root,catalog,relativeTest,false)) Assert(media.Warnings.Count>0,"Artifacts never read without explicit opt in");
        });
        Check("valid-original-fixtures-bind-by-exact-role-id-real-native-pixels-and-pcm",()=> {
            string id=catalog.Cards[0].Id;
            var entries=new[]{Asset("card-face","original-synthetic-card.png",id),Asset("card-back","original-synthetic-card.png"),Asset("wrapper-front","original-synthetic-wrapper.png"),Asset("wrapper-back","original-synthetic-wrapper.png"),Asset("sound-open","original-synthetic-pcm16.wav"),Asset("sound-next","original-synthetic-pcm24.wav")};
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries),true)) {
                Assert(media.Warnings.Count==0 && media.SuppliedAssetCount==6,"Count only actual loaded bindings");
                Texture2D face=media.CardFace(id);
                Assert(face!=null && face.width==126 && face.height==176 && face==media.CardFace(id),"Cached native Texture2D and dimensions");
                Color32 pixel=face.GetPixel(0,0); Assert(pixel.r==38 && pixel.g==161 && pixel.b==133 && pixel.a==255,"Actual original checker pixels decoded");
                Assert(media.CardBack!=null && media.WrapperFront.width==128 && media.WrapperBack!=null,"Presentation roles independent");
                Assert(media.CardFace(catalog.Cards[1].Id)==null && media.CardFace("Alakazam")==null && media.HoloMask(id)==null,"No identity aliasing or implicit full-face mask");
                foreach(string cue in new[]{"open","next"}) {
                    string file=cue=="open"?"original-synthetic-pcm16.wav":"original-synthetic-pcm24.wav";
                    DecodedWave expected=MediaWave.Decode(File.ReadAllBytes(Path.Combine(output,file)));
                    AudioClip clip=media.Sound(cue); var actual=new float[expected.Samples.Length];
                    Assert(clip.channels==expected.Channels && clip.samples==2205 && clip.frequency==22050 && clip.GetData(actual,0),"Actual PCM clip metadata/readback");
                    for(int i=0;i<actual.Length;i++) Assert(Math.Abs(actual[i]-expected.Samples[i])<0.000001f,"Supplied test PCM exact native roundtrip");
                }
                Assert(media.Sound("skip").name.Contains("Original generic"),"Independent generic sound fallback");
            }
        });
        Check("duplicate-bindings-reject-all-declarations-never-first-or-last-wins",()=> {
            foreach(string role in new[]{"card-back","sound-open"}) {
                string file=role=="card-back"?"original-synthetic-card.png":"original-synthetic-pcm16.wav";
                Entry a=Asset(role,file), b=Asset(role,file);
                foreach(bool corrupt in new[]{false,true}) {
                    if(corrupt) b.rights="";
                    using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(a,b,Asset("wrapper-front","original-synthetic-wrapper.png")),true)) {
                        Assert(media.SuppliedAssetCount==1 && media.Warnings.Count>=2 && media.WrapperFront!=null,"All duplicate declarations rejected, independent valid binding remains");
                        Assert(media.CardBack==null && media.Sound("open").name.Contains("Original generic"),"No duplicate role replacement");
                    }
                }
            }
        });
        Check("aligned-holo-mask-loads-after-face-regardless-of-manifest-order",()=> {
            string id=null; foreach(var card in catalog.Cards) if(card.Finish=="holo") { id=card.Id; break; }
            foreach(bool reversed in new[]{false,true}) {
                Entry face=Asset("card-face","original-synthetic-card.png",id), mask=Asset("holo-mask","original-synthetic-mask.png",id);
                using(var media=PresentationMediaLibrary.Load(root,catalog,reversed?ManifestFile(mask,face):ManifestFile(face,mask),true)) {
                    Assert(media.SuppliedAssetCount==2 && media.Warnings.Count==0,"Explicit aligned holo-only mask accepted");
                    Assert(media.HoloMask(id)!=null && media.HoloMask(id).width==media.CardFace(id).width && media.HoloMask(id).height==media.CardFace(id).height,"Native aligned mask dimensions");
                }
            }
        });
        Check("orphan-or-mismatched-holo-mask-never-applies-or-counts",()=> {
            string id=null; foreach(var card in catalog.Cards) if(card.Finish=="holo") { id=card.Id; break; }
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("holo-mask","original-synthetic-mask.png",id)),true)) Assert(media.HoloMask(id)==null && media.SuppliedAssetCount==0 && media.Warnings.Count==1,"Orphan mask rejected");
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("holo-mask","original-synthetic-other-mask.png",id),Asset("card-face","original-synthetic-card.png",id)),true)) Assert(media.HoloMask(id)==null && media.CardFace(id)!=null && media.SuppliedAssetCount==1 && media.Warnings.Count==1,"Misaligned mask rejected independently");
        });
        Check("dispose-idempotent-destroys-only-owned-native-resources-and-clears-bindings",()=> {
            var external=new Texture2D(2,2);
            var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("card-back","original-synthetic-card.png"),Asset("sound-open","original-synthetic-pcm16.wav")),true);
            Texture2D owned=media.CardBack; AudioClip supplied=media.Sound("open"), original=media.Sound("skip");
            Assert(media.Provenance.Count==2 && media.Provenance[0].Source.Contains("token=never-log") && media.Provenance[0].Rights=="unknown","Exact private provenance retained, not interpreted as clearance");
            media.Dispose(); media.Dispose();
            Assert(owned==null && supplied==null && original==null && external!=null,"Native resources destroyed, unrelated objects untouched");
            Assert(media.CardBack==null && media.Sound("open")==null && media.SuppliedAssetCount==0 && media.Provenance.Count==0,"Disposed resource bindings cleared");
            Assert(media.StatusText=="Media disposed","Honest disposed status");
            UnityEngine.Object.DestroyImmediate(external);
        });
        Check("aggregate-texture-budget-before-native-decode-keeps-unrelated-small-binding",()=> {
            var entries=new List<Entry>();
            for(int i=0;i<4;i++) entries.Add(Asset("card-face","original-synthetic-large-card.png",catalog.Cards[i].Id));
            entries.Add(Asset("wrapper-front","original-synthetic-wrapper.png"));
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(entries.ToArray()),true)) {
                int admitted=(16*1024*1024)/(2016*2816);
                Assert(media.SuppliedAssetCount==admitted+1 && media.Warnings.Count==4-admitted,"Cumulative pixels bounded, not only per-file bounds");
                Assert(media.CardFace(catalog.Cards[3].Id)==null && media.WrapperFront!=null && media.Sound("skip")!=null,"Budget fallback is binding-specific and never blocks original audio");
            }
        });
        RejectedEntry("missing-file",e=>e.relativePath=Relative(Path.Combine(output,"not-present.png")));
        RejectedEntry("hash-mismatch",e=>e.sha256=new string('0',64));
        RejectedEntry("hash-uppercase",e=>e.sha256=e.sha256.ToUpperInvariant());
        RejectedEntry("hash-missing",e=>e.sha256=null);
        RejectedEntry("source-missing",e=>e.source=null);
        RejectedEntry("rights-empty",e=>e.rights="");
        RejectedEntry("role-unknown",e=>e.role="card-art");
        RejectedEntry("singleton-with-card-id",e=>e.cardId=catalog.Cards[0].Id);
        RejectedEntry("display-name-is-not-id",e=>e.cardId=catalog.Cards[0].DisplayName,"card-face");
        RejectedEntry("wrong-printing-identity",e=>e.cardId=catalog.Cards[0].Id+"-wrong","card-face");
        RejectedEntry("card-face-missing-id",e=>e.cardId="","card-face");
        string nonholo=null; foreach(var card in catalog.Cards) if(card.Finish=="nonholo") { nonholo=card.Id; break; }
        RejectedEntry("nonholo-mask-identity",e=>{},"holo-mask","original-synthetic-mask.png",nonholo);
        foreach(string invalidPath in new[]{"Assets/Supplied/../escape.png","Assets/Supplied/a.png:token=never-log","C:/outside.png","https://synthetic.invalid/token=never-log.png","Assets/SuppliedX/a.png","Saves/a.png",".artifacts/../a.png","Assets/Supplied/a. ",Relative(Path.Combine(output,"reparse/original-synthetic-card.png"))}) {
            string path=invalidPath;
            RejectedEntry("unsafe-path-"+results.Count,e=>e.relativePath=path);
        }
        foreach(string invalidFile in new[]{"original-synthetic-not-png.png","original-synthetic-wrapper.png","original-synthetic-too-wide.png","original-synthetic-too-many-pixels.png","original-synthetic-bad-zlib.png"}) {
            string file=invalidFile; RejectedEntry("unsupported-image-"+file,e=>{},"card-back",file);
        }
        RejectedEntry("png-cannot-be-sound",e=>{},"sound-open","original-synthetic-card.png");
        RejectedEntry("wav-cannot-be-card",e=>{},"card-back","original-synthetic-pcm16.wav");
        Check("partial-manifest-null-entry-does-not-reject-independent-valid-entry",()=> {
            string relative=ManifestFile(Asset("wrapper-front","original-synthetic-wrapper.png")); string path=Path.Combine(root,relative);
            string json=File.ReadAllText(path); json=json.Replace("\"assets\": [","\"assets\": [null,"); File.WriteAllText(path,json);
            using(var media=PresentationMediaLibrary.Load(root,catalog,relative,true)) Assert(media.SuppliedAssetCount==1 && media.Warnings.Count==1 && media.WrapperFront!=null,"Partial entry failure not whole-manifest failure");
        });
        foreach(string badJsonEntry in new[]{"true","1.25","1e10","{\"role\":false}"}) {
            string invalid=badJsonEntry;
            Check("partial-invalid-json-type-"+results.Count,()=> {
                string relative=ManifestFile(Asset("wrapper-front","original-synthetic-wrapper.png")); string path=Path.Combine(root,relative);
                string json=File.ReadAllText(path); json=json.Replace("\"assets\": [","\"assets\": ["+invalid+","); File.WriteAllText(path,json);
                using(var media=PresentationMediaLibrary.Load(root,catalog,relative,true)) Assert(media.SuppliedAssetCount==1 && media.Warnings.Count==1 && media.WrapperFront!=null,"Schema-invalid entry type must not discard unrelated valid JSON entries");
            });
        }
        Check("native-rgb-png-subset-decodes-real-pixels",()=> {
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("card-back","original-synthetic-rgb-card.png")),true)) {
                Assert(media.SuppliedAssetCount==1 && media.Warnings.Count==0 && media.CardBack.width==126,"RGB8 supported native PNG path");
                Color32 pixel=media.CardBack.GetPixel(0,0); Assert(pixel.r==38 && pixel.g==161 && pixel.b==133 && pixel.a==255,"RGB native pixel decode");
            }
        });
        foreach(int format in new[]{3,6,65534}) {
            byte[] wav=File.ReadAllBytes(Path.Combine(output,"original-synthetic-pcm16.wav")); wav[20]=(byte)format; wav[21]=(byte)(format>>8);
            string file="original-synthetic-unsupported-"+format+".wav"; File.WriteAllBytes(Path.Combine(output,file),wav);
            RejectedEntry("unsupported-wav-format-"+format,e=>{},"sound-open",file);
        }
        Check("native-byte-caps-and-invalid-utf8-fall-back-before-decode",()=> {
            string oversized=Path.Combine(output,"original-synthetic-oversized.png");
            using(var stream=new FileStream(oversized,FileMode.Create)) stream.SetLength(MediaFiles.MediaByteLimit+1L);
            var bad=Asset("card-back","original-synthetic-card.png"); bad.relativePath=Relative(oversized);
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(bad),true)) Assert(media.SuppliedAssetCount==0 && media.Warnings.Count==1 && media.Sound("open")!=null,"Oversized image fallback");
            string manifest=Path.Combine(output,"original-synthetic-oversized.json");
            using(var stream=new FileStream(manifest,FileMode.Create)) stream.SetLength(MediaFiles.ManifestByteLimit+1L);
            using(var media=PresentationMediaLibrary.Load(root,catalog,Relative(manifest),true)) Assert(media.SuppliedAssetCount==0 && media.Warnings.Count==1,"Oversized manifest rejected");
            File.WriteAllBytes(manifest,new byte[]{255,255});
            using(var media=PresentationMediaLibrary.Load(root,catalog,Relative(manifest),true)) Assert(media.SuppliedAssetCount==0 && media.Warnings.Count==1,"Malformed UTF8 manifest rejected");
        });
        Check("media-and-foley-do-not-consume-domain-or-unity-random-state",()=> {
            UnityEngine.Random.InitState(1959); int expected=UnityEngine.Random.Range(0,int.MaxValue); UnityEngine.Random.InitState(1959);
            string before=string.Join("|",System.Linq.Enumerable.Select(catalog.Cards,c=>c.Id+":"+c.Finish+":"+c.BoosterEligible));
            var a=new SeededRandom(1959); var b=new SeededRandom(1959);
            using(var media=PresentationMediaLibrary.Load(root,catalog,ManifestFile(Asset("card-back","original-synthetic-card.png")),true)) {
                foreach(string cue in new[]{"generate","open","next","skip"}) {
                    MediaSamples.Synthesize(cue); media.Sound(cue);
                    Assert(a.NextInt(100000)==b.NextInt(100000),"Domain PRNG sequence unchanged");
                }
            }
            Assert(UnityEngine.Random.Range(0,int.MaxValue)==expected,"Unity PRNG not consumed");
            string after=string.Join("|",System.Linq.Enumerable.Select(catalog.Cards,c=>c.Id+":"+c.Finish+":"+c.BoosterEligible)); Assert(before==after,"Catalog identity, finish and eligibility untouched");
        });
        Check("remote-project-roots-are-rejected-lexically-with-generic-fallback",()=> {
            foreach(string invalidRoot in new[]{"\\\\synthetic.invalid\\never-contact","\\\\?\\UNC\\synthetic.invalid\\never-contact","https://synthetic.invalid/root","relative-root"}) {
                using(var media=PresentationMediaLibrary.Load(invalidRoot,catalog)) Assert(media.Warnings.Count==1 && media.SuppliedAssetCount==0 && media.Sound("open")!=null,"No remote root read; original cues remain independent");
            }
        });
        Check("no-native-media-resources-or-automatic-audio-sources-remain",()=> {
            foreach(AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>()) Assert(!clip.name.StartsWith("Original generic foley:")&&!clip.name.StartsWith("Verified optional cue:"),"No leaked owned clips");
            foreach(Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>()) Assert(!texture.name.StartsWith("Verified optional texture:"),"No leaked owned textures");
            Assert(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length==0,"Provider never auto-creates playback");
        });
        bool passed=results.TrueForAll(r=>r.passed);
        var report=new Report{testCount=results.Count,passed=passed,checks=results.ToArray(),genericCues=metrics.ToArray()};
        File.WriteAllText(Path.Combine(output,"native-results.json"),JsonUtility.ToJson(report,true));
        EditorApplication.Exit(passed?0:1);
    }
}
