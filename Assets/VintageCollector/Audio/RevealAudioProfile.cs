using UnityEngine;

namespace VintageCollector.Audio
{
    [CreateAssetMenu(fileName = "RevealAudioProfile", menuName = "Vintage Collector/Reveal Audio Profile")]
    public sealed class RevealAudioProfile : ScriptableObject, VintageCollector.Media.IRevealSoundMedia
    {
        [Tooltip("One-based reveal positions 1–11. Empty entries use generic reveal Foley.")]
        public AudioClip[] pullSounds = new AudioClip[11];
        [Range(1, 11)] public int rareRevealPosition = 8;
        public AudioClip nonPokemonRare;
        public AudioClip pokemonRare;
        public AudioClip holoRare;

        public AudioClip RevealSound(int oneBasedPosition, string kind, string finish)
        {
            if (oneBasedPosition < 1 || oneBasedPosition > 11) return null;
            // Classification is consulted only for the configured, already-revealed rare slot.
            if (oneBasedPosition == rareRevealPosition)
            {
                AudioClip rare = finish == "holo" ? holoRare :
                    kind == "pokemon" ? pokemonRare : nonPokemonRare;
                if (rare != null) return rare;
            }
            return pullSounds != null && oneBasedPosition <= pullSounds.Length
                ? pullSounds[oneBasedPosition - 1] : null;
        }
    }
}
