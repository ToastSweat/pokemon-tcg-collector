using System;
using UnityEngine;
namespace VintageCollector.Media {
 [Serializable] public sealed class NativeBaseCardRow { public string canonicalId; public Texture2D face,mask; }
 // Borrowed references only. Provenance is authoring metadata, not runtime proof of source bytes.
 [CreateAssetMenu(fileName="NativeBaseMediaProfile",menuName="Vintage Collector/Native Base Media")]
 public sealed class NativeBaseMediaProfile:ScriptableObject {
  public string printingId="base-en-unlimited1999",generationId,displayLabel;
  public NativeBaseCardRow[] cards=new NativeBaseCardRow[0];
  public Texture2D sharedCardBack,wrapperBack,wrapperFrontMask,wrapperBackMask;
  // Explicit semantic order: Blastoise, Charizard, Venusaur (accepted filenames are swapped).
  public Texture2D[] wrapperFronts=new Texture2D[3];
 }
}
