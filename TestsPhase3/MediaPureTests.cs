using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VintageCollector.Media;

public static class MediaPureTests
{
    static readonly List<string> Checks = new List<string>();
    static int failures;
    static string root, output;
    static void Check(string name, Action action)
    {
        try { action(); Checks.Add("{\"name\":\""+name+"\",\"passed\":true}"); }
        catch (Exception ex) { failures++; Checks.Add("{\"name\":\""+name+"\",\"passed\":false,\"error\":\""+ex.Message.Replace("\\", "/").Replace("\"", "'").Replace("\n", " ").Replace("\r", " ")+"\"}"); }
    }
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    static void Reject(Action action) { bool rejected=false; try { action(); } catch (ArgumentException) { rejected=true; } catch (InvalidDataException) { rejected=true; } Assert(rejected, "Expected rejection"); }
    static void Put(byte[] bytes,int offset,int value) { for(int i=0;i<4;i++) bytes[offset+i]=(byte)(value>>(8*i)); }
    static byte[] Wav(int bits, int channels)
    {
        using(var stream=new MemoryStream()) using(var w=new BinaryWriter(stream)) {
            int size=4*channels*(bits/8);
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36+size); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
            w.Write((short)1); w.Write((short)channels); w.Write(22050); w.Write(22050*channels*(bits/8)); w.Write((short)(channels*(bits/8))); w.Write((short)bits);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(size);
            for(int i=0;i<4*channels;i++) { int v=new[]{-(1<<(bits-1)),0,1<<(bits-2),0}[i%4]; for(int j=0;j<bits/8;j++) w.Write((byte)(v>>(j*8))); }
            return stream.ToArray();
        }
    }
    public static int Main(string[] args)
    {
        root=args[0]; output=args[1];
        Check("foley-four-distinct-deterministic-finite-modest-tapered-cues", () => {
            var signatures=new HashSet<string>();
            foreach (string cue in new[]{"generate","open","next","skip"}) {
                float[] a=MediaSamples.Synthesize(cue), b=MediaSamples.Synthesize(cue);
                Assert(a.Length>=2205 && a.Length<=13230, "Bounded short sample count");
                double energy=0; float peak=0;
                for(int i=0;i<a.Length;i++) { Assert(!float.IsNaN(a[i])&&!float.IsInfinity(a[i])&&Math.Abs(a[i])<=0.25f,"Finite modest peak"); Assert(a[i]==b[i],"Deterministic"); energy+=a[i]*a[i]; peak=Math.Max(peak,Math.Abs(a[i])); }
                Assert(energy>0.01 && peak>0.01,"Non-silent");
                Assert(a[0]==0 && Math.Abs(a[a.Length-1])<0.00001f,"Tapered endpoints");
                signatures.Add(a.Length+":"+a[a.Length/3].ToString("R",CultureInfo.InvariantCulture));
            }
            Assert(signatures.Count==4,"Distinct cues"); Reject(()=>MediaSamples.Synthesize("not-a-committed-action"));
        });
        Check("wav-real-pcm16-and-pcm24-mono-stereo-sample-decoding", () => {
            foreach(int bits in new[]{16,24}) foreach(int channels in new[]{1,2}) {
                byte[] bytes=Wav(bits,channels);
                DecodedWave wave=MediaWave.Decode(bytes);
                Assert(wave.Channels==channels && wave.SampleRate==22050 && wave.Samples.Length==4*channels,"Exact format and sample count");
                for(int i=0;i<wave.Samples.Length;i++) Assert(Math.Abs(wave.Samples[i]-new[]{-1f,0f,0.5f,0f}[i%4])<0.00001f,"Actual signed little-endian PCM samples");
            }
        });
        Check("wav-unsupported-truncated-overlimit-and-inconsistent-format-rejected", () => {
            Reject(()=>MediaWave.Decode(new byte[16]));
            foreach(int offset in new[]{20,22,24,28,32,34,40}) { byte[] w=Wav(16,1); w[offset]=255; Reject(()=>MediaWave.Decode(w)); }
            byte[] tail=Wav(16,1); Array.Resize(ref tail,tail.Length-1); Reject(()=>MediaWave.Decode(tail));
            Reject(()=>MediaWave.Decode(new byte[16*1024*1024+1]));
            byte[] empty=Wav(16,1); Array.Resize(ref empty,44); empty[4]=36; empty[40]=0; Reject(()=>MediaWave.Decode(empty));
        });
        Check("path-canonical-supplied-and-explicit-test-scope-only", () => {
            Assert(MediaFiles.Resolve(root,"Assets/Supplied/AssetManifest.json",false).EndsWith("AssetManifest.json"),"Supplied allowed");
            string rel=output.Substring(root.Length+1).Replace('\\','/')+"/original-synthetic.bin";
            File.WriteAllBytes(Path.Combine(output,"original-synthetic.bin"),new byte[]{1,2,3});
            Assert(MediaFiles.Read(root,rel,true,3).Length==3,"Explicit original test input readable");
            Reject(()=>MediaFiles.Resolve(root,rel,false)); Reject(()=>MediaFiles.Read(root,rel,true,2));
            foreach(string path in new[]{"../Assets/Supplied/a.png","Assets/Supplied/../a.png","Assets/Supplied/./a.png","Assets/Supplied//a.png","Assets/Supplied/a.png:evil","C:/a.png","/Assets/Supplied/a.png","https://example.invalid/a.png","file:///a.png","Assets/SuppliedX/a.png","Saves/state.save",".artifactsX/a","Assets/Supplied/a. ","Assets/Supplied/CON.png","Assets/Supplied/%2e%2e/a","Assets\\Supplied\\a"}) Reject(()=>MediaFiles.Resolve(root,path,true));
        });
        Check("path-existing-reparse-ancestor-is-rejected-before-read", () => {
            string relative=output.Substring(root.Length+1).Replace('\\','/')+"/reparse/original.bin";
            Assert(Directory.Exists(Path.Combine(output,"reparse")),"Real junction fixture exists");
            Reject(()=>MediaFiles.Resolve(root,relative,true));
        });
        Check("sha256-exact-lowercase-required-before-acceptance", () => {
            byte[] bytes={1,2,3};
            string hash=MediaFiles.Sha256(bytes);
            Assert(hash=="039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81","Known SHA256");
            MediaFiles.VerifyHash(bytes,hash);
            foreach(string bad in new[]{null,"",hash.ToUpperInvariant(),new string('0',64),"sha256:"+hash}) Reject(()=>MediaFiles.VerifyHash(bytes,bad));
        });
        Check("png-bounded-header-ratio-pixels-crc-structure-before-native-decode", () => {
            byte[] card=File.ReadAllBytes(Path.Combine(output,"original-synthetic-card.png"));
            PngDimensions dims=MediaPng.Inspect(card,true);
            Assert(dims.Width==126 && dims.Height==176,"Header dimensions");
            MediaPng.Inspect(File.ReadAllBytes(Path.Combine(output,"original-synthetic-wrapper.png")),false);
            Reject(()=>MediaPng.Inspect(File.ReadAllBytes(Path.Combine(output,"original-synthetic-wrapper.png")),true));
            foreach(string file in new[]{"original-synthetic-too-wide.png","original-synthetic-too-many-pixels.png","original-synthetic-not-png.png"}) Reject(()=>MediaPng.Inspect(File.ReadAllBytes(Path.Combine(output,file)),false));
            byte[] corrupt=(byte[])card.Clone(); corrupt[29]^=1; Reject(()=>MediaPng.Inspect(corrupt,false));
            byte[] truncated=(byte[])card.Clone(); Array.Resize(ref truncated,40); Reject(()=>MediaPng.Inspect(truncated,false));
            Reject(()=>MediaPng.Inspect(new byte[16*1024*1024+1],false));
        });
        Check("manifest-strict-schema-json-and-required-provenance", () => {
            Assert(MediaManifest.Parse(File.ReadAllText(Path.Combine(root,"Assets/Supplied/AssetManifest.json"))).Count==0,"Honest production empty manifest");
            string empty="{\"schemaVersion\":1,\"status\":\"test\",\"intendedUse\":\"test\",\"redistributionPermission\":\"none\",\"rightsDefault\":\"unknown\",\"notes\":\"\",\"assets\":[]}";
            foreach(string bad in new[]{"", "{}", empty.Replace("\"schemaVersion\":1","\"schemaVersion\":2"),empty.Replace("\"schemaVersion\":1","\"schemaVersion\":1.0"),empty.Replace("\"schemaVersion\":1","\"schemaVersion\":1,\"schemaVersion\":1"),empty+"x",empty.Replace("\"assets\":[]","\"assets\":null"),empty.Replace("\"notes\":\"\",", ""),empty.Replace("\"assets\":[]","\"assets\":[[[[[[[[[[[[]]]]]]]]]]]]" )}) Reject(()=>MediaManifest.Parse(bad));
            var entry=new Dictionary<string,object>{{"role","card-back"},{"cardId",""},{"relativePath","Assets/Supplied/a.png"},{"source","https://synthetic.invalid/?token=do-not-log"},{"rights","unknown"},{"restrictions",""},{"sha256",new string('a',64)}};
            MediaEntry ok=MediaManifest.Validate(entry,null);
            Assert(ok.Source==(string)entry["source"] && ok.Rights=="unknown","Provenance exact preservation, unknown not clearance");
            foreach(string field in new[]{"source","rights","sha256","role","relativePath","restrictions"}) { var copy=new Dictionary<string,object>(entry); copy.Remove(field); Reject(()=>MediaManifest.Validate(copy,null)); }
            foreach(string role in new[]{"card-face","holo-mask","unknown","sound-resume"}) { var copy=new Dictionary<string,object>(entry); copy["role"]=role; Reject(()=>MediaManifest.Validate(copy,null)); }
            entry["cardId"]="display name"; Reject(()=>MediaManifest.Validate(entry,null));
        });
        Check("manifest-unknown-comma-key-cannot-bypass-exact-schema", () => {
            string json=File.ReadAllText(Path.Combine(root,"Assets/Supplied/AssetManifest.json"));
            json=json.Replace("\"assets\": []","\"status,intendedUse\":\"unknown composite key\",\"assets\": []");
            Reject(()=>MediaManifest.Parse(json));
        });
        Check("wav-rate-duration-boundaries-and-signed-fullscale",()=> {
            foreach(int bits in new[]{16,24}) {
                byte[] wav=Wav(bits,1); int stride=bits/8;
                for(int i=0;i<stride;i++) wav[44+i]=(byte)(((1<<(bits-1))-1)>>(8*i));
                float[] samples=MediaWave.Decode(wav).Samples;
                Assert(samples[0]<1 && samples[0]>0.999f,"Positive full scale normalized below clipping");
            }
            foreach(int rate in new[]{7999,48001}) { byte[] wav=Wav(16,1); Put(wav,24,rate); Put(wav,28,rate*2); Reject(()=>MediaWave.Decode(wav)); }
            foreach(int rate in new[]{8000,48000}) { byte[] wav=Wav(16,1); Put(wav,24,rate); Put(wav,28,rate*2); Assert(MediaWave.Decode(wav).SampleRate==rate,"Inclusive rate boundaries"); }
            byte[] exact=Wav(16,1); int frames=22050*5; Array.Resize(ref exact,44+frames*2); Put(exact,4,exact.Length-8); Put(exact,40,frames*2);
            Assert(MediaWave.Decode(exact).Samples.Length==frames,"Exactly five seconds accepted");
            Array.Resize(ref exact,exact.Length+2); Put(exact,4,exact.Length-8); Put(exact,40,(frames+1)*2); Reject(()=>MediaWave.Decode(exact));
            foreach(int format in new[]{3,6,65534}) { byte[] wav=Wav(16,1); wav[20]=(byte)format; wav[21]=(byte)(format>>8); Reject(()=>MediaWave.Decode(wav)); }
            foreach(int bits in new[]{8,32}) { byte[] wav=Wav(16,1); wav[34]=(byte)bits; Reject(()=>MediaWave.Decode(wav)); }
        });
        Check("bounded-parsers-reject-truncation-and-never-crash-on-deterministic-mutants",()=> {
            byte[] png=File.ReadAllBytes(Path.Combine(output,"original-synthetic-card.png")), wav=Wav(24,2);
            foreach(byte[] valid in new[]{png,wav}) for(int length=0;length<valid.Length;length++) {
                byte[] truncated=new byte[length]; Array.Copy(valid,truncated,length);
                Reject(()=> { if(valid==png) MediaPng.Inspect(truncated,true); else MediaWave.Decode(truncated); });
            }
            var rng=new System.Random(1959);
            for(int i=0;i<500;i++) {
                byte[] bad=(byte[])wav.Clone(); bad[rng.Next(0,44)]=(byte)rng.Next(256);
                try { var decoded=MediaWave.Decode(bad); foreach(float s in decoded.Samples) Assert(!float.IsNaN(s)&&!float.IsInfinity(s)&&s>=-1 && s<=1,"Safe mutant sample range"); }
                catch(InvalidDataException) { }
            }
        });
        Check("project-root-local-drive-gate-blocks-unc-device-uri-and-relative-roots",()=> {
            Assert(MediaFiles.LocalRoot(root)==Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar),"Canonical local project root");
            foreach(string remote in new[]{"\\\\synthetic.invalid\\never-contact","\\\\?\\UNC\\synthetic.invalid\\never-contact","\\\\?\\F:\\device","https://synthetic.invalid/root","file:///F:/root","relative-root","/root"}) Reject(()=>MediaFiles.LocalRoot(remote));
        });
        Check("reparse-metadata-walk-is-root-first-never-descendant-first",()=> {
            string path=Path.GetFullPath(Path.Combine(output,"reparse/original.bin"));
            string[] order=MediaFiles.AncestorsRootFirst(path);
            Assert(order.Length>3 && order[0]==Path.GetPathRoot(path) && order[order.Length-1]==path,"Drive to leaf ordering");
            for(int i=1;i<order.Length;i++) Assert(Path.GetDirectoryName(order[i])==order[i-1],"Every ancestor checked before its child can be traversed");
        });
        Console.WriteLine("{\"testCount\":"+Checks.Count+",\"passed\":"+(failures==0?"true":"false")+",\"checks\":["+string.Join(",",Checks.ToArray())+"]}");
        GC.KeepAlive(root); GC.KeepAlive(output);
        return failures==0?0:1;
    }
}
