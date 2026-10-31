using System;
using System.Collections.Generic;
using UnityEngine;
using VintageCollector.Domain;
namespace VintageCollector.Media
{
    public sealed class PresentationMediaLibrary : IPresentationMedia, IDisposable
    {
        readonly List<string> warnings=new List<string>();
        readonly Dictionary<string,AudioClip> sounds=new Dictionary<string,AudioClip>(StringComparer.Ordinal);
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>(StringComparer.Ordinal);
        readonly List<MediaEntry> provenance=new List<MediaEntry>();
        public const long TotalTexturePixelLimit=16*1024*1024;
        long texturePixels;
        bool disposed;
        public static PresentationMediaLibrary Load(string projectRoot, Catalog catalog, string manifestRelativePath="Assets/Supplied/AssetManifest.json", bool allowTestInputs=false)
        {
            var library=new PresentationMediaLibrary();
            foreach(string cue in new[]{"generate","open","next","skip"}) {
                AudioClip clip=null;
                try {
                    float[] samples=MediaSamples.Synthesize(cue);
                    clip=AudioClip.Create("Original generic foley: "+cue,samples.Length,1,MediaSamples.SampleRate,false);
                    if(clip==null || !clip.SetData(samples,0)) throw new InvalidOperationException();
                    library.sounds.Add(cue,clip); library.owned.Add(clip);
                } catch(Exception) { DestroyOwned(clip); library.warnings.Add("Original generic cue unavailable: "+cue); }
            }
            try {
                byte[] bytes=MediaFiles.Read(projectRoot,manifestRelativePath,allowTestInputs,MediaFiles.ManifestByteLimit);
                string json=new System.Text.UTF8Encoding(false,true).GetString(bytes);
                List<object> entries=MediaManifest.Parse(json);
                var counts=new Dictionary<string,int>(StringComparer.Ordinal);
                foreach(object raw in entries) {
                    string binding=RawBinding(raw); if(binding==null) continue;
                    int count; counts.TryGetValue(binding,out count); counts[binding]=count+1;
                }
                var valid=new List<KeyValuePair<int,MediaEntry>>();
                for(int i=0;i<entries.Count;i++) {
                    try {
                        string binding=RawBinding(entries[i]);
                        if(binding!=null && counts[binding]>1) throw new System.IO.InvalidDataException();
                        valid.Add(new KeyValuePair<int,MediaEntry>(i,MediaManifest.Validate(entries[i],catalog)));
                    } catch(Exception) { library.warnings.Add("Media entry "+(i+1)+" has invalid schema/identity/provenance or duplicate binding; safe fallback."); }
                }
                // Faces first; masks are never admitted without a successfully decoded matching face.
                foreach(bool masks in new[]{false,true}) foreach(var item in valid) {
                    if((item.Value.Role=="holo-mask")!=masks) continue;
                    try { library.LoadEntry(projectRoot,item.Value,allowTestInputs); }
                    catch(Exception) { library.warnings.Add("Media entry "+(item.Key+1)+" unavailable, unsafe, unsupported, misaligned or hash-invalid; safe fallback."); }
                }
            } catch(Exception) { library.warnings.Add("Media manifest unavailable or invalid; optional art and cues use safe fallbacks."); }
            return library;
        }
        static string RawBinding(object raw)
        {
            var entry=raw as Dictionary<string,object>; if(entry==null) return null;
            object role,id;
            if(!entry.TryGetValue("role",out role) || !(role is string)) return null;
            if(!entry.TryGetValue("cardId",out id) || id==null) id="";
            return id is string?(string)role+"|"+(string)id:null;
        }
        void LoadEntry(string root,MediaEntry entry,bool test)
        {
            bool sound=entry.Role.StartsWith("sound-",StringComparison.Ordinal);
            if(!entry.RelativePath.EndsWith(sound?".wav":".png",StringComparison.OrdinalIgnoreCase)) throw new System.IO.InvalidDataException();
            byte[] bytes=MediaFiles.Read(root,entry.RelativePath,test,MediaFiles.MediaByteLimit);
            MediaFiles.VerifyHash(bytes,entry.Sha256);
            UnityEngine.Object resource=null;
            try {
                if(sound) {
                    DecodedWave wave=MediaWave.Decode(bytes);
                    var clip=AudioClip.Create("Verified optional cue: "+entry.Role.Substring(6),wave.Samples.Length/wave.Channels,wave.Channels,wave.SampleRate,false);
                    resource=clip;
                    if(clip==null || !clip.SetData(wave.Samples,0)) throw new System.IO.InvalidDataException();
                    string cue=entry.Role.Substring(6); AudioClip old;
                    if(sounds.TryGetValue(cue,out old)) { owned.Remove(old); DestroyOwned(old); }
                    sounds[cue]=clip;
                } else {
                    PngDimensions dimensions=MediaPng.Inspect(bytes,entry.Role=="card-face" || entry.Role=="card-back" || entry.Role=="holo-mask");
                    if(entry.Role=="holo-mask") {
                        Texture2D face=CardFace(entry.CardId);
                        if(face==null || face.width!=dimensions.Width || face.height!=dimensions.Height) throw new System.IO.InvalidDataException();
                    }
                    long pixels=(long)dimensions.Width*dimensions.Height;
                    if(texturePixels+pixels>TotalTexturePixelLimit) throw new System.IO.InvalidDataException();
                    var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,entry.Role=="holo-mask");
                    resource=texture;
                    if(!ImageConversion.LoadImage(texture,bytes,false) || texture.width!=dimensions.Width || texture.height!=dimensions.Height) throw new System.IO.InvalidDataException();
                    texture.name="Verified optional texture: "+entry.Role;
                    texture.wrapMode=TextureWrapMode.Clamp; texture.filterMode=FilterMode.Bilinear;
                    textures.Add(entry.Binding,texture); texturePixels+=pixels;
                }
                resource.hideFlags=HideFlags.DontSave;
                owned.Add(resource); provenance.Add(entry);
            } catch { DestroyOwned(resource); throw; }
        }
        public IReadOnlyList<string> Warnings { get { return warnings.AsReadOnly(); } }
        // Full provenance is available to explicit tooling only; never concatenate it into UI or logs.
        public IReadOnlyList<MediaEntry> Provenance { get { return provenance.AsReadOnly(); } }
        public int SuppliedAssetCount { get { return provenance.Count; } }
        public string StatusText { get { return disposed?"Media disposed":SuppliedAssetCount+" supplied assets; generic placeholder art and original generic foley fill missing bindings"; } }
        Texture2D Texture(string role,string id="") { Texture2D result; return !disposed && textures.TryGetValue(role+"|"+id,out result)?result:null; }
        public Texture2D CardFace(string printingId) { return printingId==null?null:Texture("card-face",printingId); }
        public Texture2D HoloMask(string printingId) { return printingId==null?null:Texture("holo-mask",printingId); }
        public Texture2D CardBack { get { return Texture("card-back"); } }
        public Texture2D WrapperFront { get { return Texture("wrapper-front"); } }
        public Texture2D WrapperBack { get { return Texture("wrapper-back"); } }
        public AudioClip Sound(string committedAction) { AudioClip clip; return !disposed && committedAction!=null && sounds.TryGetValue(committedAction,out clip)?clip:null; }
        static void DestroyOwned(UnityEngine.Object resource)
        {
            if(resource==null) return;
            if(Application.isPlaying) UnityEngine.Object.Destroy(resource); else UnityEngine.Object.DestroyImmediate(resource);
        }
        public void Dispose()
        {
            if(disposed) return;
            disposed=true;
            foreach(var resource in owned) DestroyOwned(resource);
            owned.Clear(); sounds.Clear(); textures.Clear(); provenance.Clear();
        }
    }
}
