using System;
using UnityEngine;
namespace VintageCollector.Media
{
    [Serializable]
    public sealed class SuppliedCardArtRow
    {
        public string cardId;
        public Texture2D face;
        public Texture2D holoMask;
    }

    // Optional host-side native assets. No catalog/model/save fields or resource ownership.
    [CreateAssetMenu(fileName="SuppliedCardArtProfile", menuName="Vintage Collector/Supplied Card Art Profile")]
    public sealed class SuppliedCardArtProfile : ScriptableObject
    {
        public string printingId;
        public string displayLabel;
        public SuppliedCardArtRow[] cards = new SuppliedCardArtRow[0];
    }
}
