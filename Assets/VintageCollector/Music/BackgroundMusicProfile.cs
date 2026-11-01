using UnityEngine;
namespace VintageCollector.Music
{
    [CreateAssetMenu(menuName = "Vintage Collector/Background Music")]
    public sealed class BackgroundMusicProfile : ScriptableObject
    {
        public AudioClip[] tracks = new AudioClip[8];
        [Range(0f, 1f)] public float gain = 0.35f;
    }
}
