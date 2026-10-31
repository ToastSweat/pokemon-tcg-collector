using UnityEngine;

namespace VintageCollector.Media
{
    // Passive presentation dependency. Implementations never generate, save or award cards.
    public interface IPresentationMedia
    {
        Texture2D CardFace(string printingId);
        Texture2D HoloMask(string printingId);
        Texture2D CardBack { get; }
        Texture2D WrapperFront { get; }
        Texture2D WrapperBack { get; }
        AudioClip Sound(string committedAction);
        string StatusText { get; }
        int SuppliedAssetCount { get; }
    }
}
