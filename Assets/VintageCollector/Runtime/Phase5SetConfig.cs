using System;
using UnityEngine;
using VintageCollector.Audio;
namespace VintageCollector.Runtime
{
    // Passive scene bindings only; admission, initialization and lifecycle belong to the host.
    [Serializable] public sealed class Phase5SetConfig
    {
        public string printingId, displayLabel;
        public TextAsset catalogJson, modelJson;
        public RevealAudioProfile revealAudioProfile;
        public Texture2D cardFaceTemplate;
        public Texture2D[] wrapperFronts = new Texture2D[3];
        public Texture2D cardBack, wrapperBack;
    }
}
