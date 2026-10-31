using UnityEngine;
using VintageCollector.Media;
using VintageCollector.Presentation;

namespace VintageCollector.Audio
{
    // Optional scene wiring only. The presentation owns acknowledgement gates and playback.
    [DisallowMultipleComponent]
    public sealed class RevealAudioHost : MonoBehaviour
    {
        public RevealAudioProfile profile;
        private TabletopPresentation presentation;
        private IRevealSoundMedia bound;

        private void OnEnable()
        {
            presentation = GetComponent<TabletopPresentation>();
            if (presentation == null || profile == null) return;
            bound = profile;
            presentation.RevealSounds = bound;
        }

        private void OnDestroy()
        {
            if (presentation != null && ReferenceEquals(presentation.RevealSounds, bound))
                presentation.RevealSounds = null;
            bound = null;
            presentation = null;
        }
    }
}
