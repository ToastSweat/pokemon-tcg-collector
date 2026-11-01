using System;
using System.Collections.Generic;
using UnityEngine;
namespace VintageCollector.Media {
 // No PNG read, hash, decode, upload or destruction of borrowed native textures.
 public sealed class NativeBaseMediaProvider:IPresentationMedia,IWrapperMetallicMedia,IDisposable {
  public const long PixelLimit=64L*1024*1024;
  readonly Dictionary<string,Texture2D> faces=new Dictionary<string,Texture2D>(StringComparer.Ordinal),masks=new Dictionary<string,Texture2D>(StringComparer.Ordinal);
  readonly Dictionary<string,AudioClip> sounds=new Dictionary<string,AudioClip>(StringComparer.Ordinal);
  Texture2D back,wrapperBack,frontMask,backMask;Texture2D[] fronts;bool disposed;
  public long TexturePixels {get;private set;} public int UniqueTextureCount {get;private set;}
  public NativeBaseMediaProvider(NativeBaseMediaProfile profile,Func<string,string> printingForCardId,long pixelLimit=PixelLimit) {
   if(profile==null||printingForCardId==null||profile.printingId!="base-en-unlimited1999"||profile.cards==null||profile.cards.Length!=101||profile.wrapperFronts==null||profile.wrapperFronts.Length!=3||pixelLimit<=0||pixelLimit>PixelLimit)throw new ArgumentException("Invalid native Base profile.");
   var refs=new HashSet<Texture2D>();var semantics=new Dictionary<Texture2D,bool>();
   Action<Texture2D,bool,int,int> admit=delegate(Texture2D t,bool linear,int w,int h){
    if(t==null||t.width!=w||t.height!=h||t.isDataSRGB==linear)throw new ArgumentException("Native texture shape/color space mismatch.");
    bool previous;if(semantics.TryGetValue(t,out previous)&&previous!=linear)throw new ArgumentException("Mixed mask/color reference.");semantics[t]=linear;
    if(refs.Add(t)){TexturePixels+=(long)t.width*t.height;if(TexturePixels>pixelLimit)throw new ArgumentException("Native Base pixel budget exceeded.");}
   };
   foreach(var row in profile.cards){
    if(row==null||string.IsNullOrEmpty(row.canonicalId)||Array.IndexOf(NativeBaseBindings.Ids,row.canonicalId)<0||printingForCardId(row.canonicalId)!=profile.printingId||faces.ContainsKey(row.canonicalId))throw new ArgumentException("Invalid native Base canonical identity.");
    admit(row.face,false,600,825);faces.Add(row.canonicalId,row.face);
    if(NativeBaseBindings.NeedsMask(row.canonicalId)){admit(row.mask,true,600,825);masks.Add(row.canonicalId,row.mask);}else if(row.mask!=null)throw new ArgumentException("Unexpected non-holo mask.");
   }
   admit(profile.sharedCardBack,false,600,825);admit(profile.wrapperBack,false,260,475);admit(profile.wrapperFrontMask,true,260,475);admit(profile.wrapperBackMask,true,260,475);
   foreach(var t in profile.wrapperFronts)admit(t,false,260,475);
   UniqueTextureCount=refs.Count;back=profile.sharedCardBack;wrapperBack=profile.wrapperBack;frontMask=profile.wrapperFrontMask;backMask=profile.wrapperBackMask;fronts=(Texture2D[])profile.wrapperFronts.Clone();
   foreach(string cue in new[]{"generate","open","next","skip"}){AudioClip clip=null;try{var samples=MediaSamples.Synthesize(cue);clip=AudioClip.Create("Original generic foley: "+cue,samples.Length,1,MediaSamples.SampleRate,false);if(clip==null||!clip.SetData(samples,0))throw new InvalidOperationException();sounds.Add(cue,clip);}catch(Exception){DestroyOwned(clip);}}
  }
  public Texture2D CardFace(string id){Texture2D t;return !disposed&&id!=null&&faces.TryGetValue(id,out t)?t:null;}
  public Texture2D HoloMask(string id){Texture2D t;return !disposed&&id!=null&&masks.TryGetValue(id,out t)?t:null;}
  public Texture2D CardBack {get{return disposed?null:back;}}
  public Texture2D WrapperFront {get{return WrapperFrontForVariant(1);}}
  public Texture2D WrapperFrontForVariant(int variant){return !disposed&&variant>=0&&variant<3?fronts[variant]:null;}
  public Texture2D WrapperBack {get{return disposed?null:wrapperBack;}}
  public Texture2D WrapperFrontMask {get{return disposed?null:frontMask;}}
  public Texture2D WrapperBackMask {get{return disposed?null:backMask;}}
  public AudioClip Sound(string cue){AudioClip c;return !disposed&&cue!=null&&sounds.TryGetValue(cue,out c)?c:null;}
  public int SuppliedAssetCount {get{return disposed?0:123;}}
  public string StatusText {get{return disposed?"Native Base media disposed":"Preimported Base visual substitutes; borrowed native textures";}}
  static void DestroyOwned(UnityEngine.Object o){if(o==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
  public void Dispose(){if(disposed)return;disposed=true;foreach(var c in sounds.Values)DestroyOwned(c);sounds.Clear();faces.Clear();masks.Clear();back=wrapperBack=frontMask=backMask=null;fronts=null;}
 }
}
