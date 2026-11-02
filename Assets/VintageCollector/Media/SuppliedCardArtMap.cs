using System;
using System.Collections.Generic;
using UnityEngine;
namespace VintageCollector.Media
{
    // Detached identity snapshot. Textures are borrowed; never decoded, edited or destroyed here.
    public sealed class SuppliedCardArtMap
    {
        public const string JunglePrinting = "jungle-en-unlimited1999";
        public const string FossilPrinting = "fossil-en-unlimited1999";
        public const long PerSetPixelLimit = 32L * 1024 * 1024;
        public const long CombinedPixelLimit = 64L * 1024 * 1024;
        public const long MaskPixelLimit = 2L * 1024 * 1024;
        private sealed class FaceBinding { public string printing; public Texture2D face, mask; }
        private readonly Dictionary<string,FaceBinding> faces = new Dictionary<string,FaceBinding>(StringComparer.Ordinal);
        private readonly Dictionary<string,int> counts = new Dictionary<string,int>(StringComparer.Ordinal);
        private SuppliedCardArtMap() { }
        public int FaceCount { get; private set; }
        public int MaskCount { get; private set; }
        public static SuppliedCardArtMap Create(SuppliedCardArtProfile[] profiles, Func<string,string> printingForCardId)
        {
            return Create(profiles,printingForCardId,null);
        }
        public static SuppliedCardArtMap Create(SuppliedCardArtProfile[] profiles, Func<string,string> printingForCardId, Func<string,bool> isHolo)
        {
            if (printingForCardId == null) throw new ArgumentNullException("printingForCardId");
            var result = new SuppliedCardArtMap();
            if (profiles == null) return result;
            if (profiles.Length > 2) throw new ArgumentException("Only Jungle and Fossil profiles are admitted.","profiles");
            long combinedPixels = 0, maskPixels = 0;
            var uniqueMasks = new HashSet<Texture2D>();
            foreach (var profile in profiles)
            {
                if (profile == null) throw new ArgumentException("Null supplied art profile.","profiles");
                string printing = profile.printingId;
                int rowLimit = printing == JunglePrinting ? 64 : printing == FossilPrinting ? 62 : 0;
                if (rowLimit == 0 || result.counts.ContainsKey(printing)) throw new ArgumentException("Unknown or duplicate supplied art printing.","profiles");
                var rows = profile.cards;
                if (rows == null || rows.Length > rowLimit) throw new ArgumentException("Invalid supplied art row count.","profiles");
                long pixels = 0; int count = 0;
                foreach (var row in rows)
                {
                    if (row == null || string.IsNullOrEmpty(row.cardId)) throw new ArgumentException("Missing supplied art identity.","profiles");
                    string canonicalPrinting;
                    try { canonicalPrinting = printingForCardId(row.cardId); }
                    catch (KeyNotFoundException e) { throw new ArgumentException("Unknown supplied art identity.","profiles",e); }
                    catch (ArgumentException e) { throw new ArgumentException("Invalid supplied art identity.","profiles",e); }
                    if (!string.Equals(canonicalPrinting,printing,StringComparison.Ordinal) || result.faces.ContainsKey(row.cardId))
                        throw new ArgumentException("Unknown, wrong-printing or duplicate supplied art identity.","profiles");
                    Texture2D face = row.face;
                    // Unity missing native references compare null; retain the admitted ID for duplicate checks.
                    if (face != null)
                    {
                        int width = face.width, height = face.height;
                        long facePixels = (long)width * height;
                        if (width <= 0 || height <= 0 || facePixels > PerSetPixelLimit || pixels > PerSetPixelLimit - facePixels)
                            throw new ArgumentException("Supplied art pixel budget exceeded.","profiles");
                        pixels += facePixels; count++;
                    }
                    // Optional masks fail independently; identity and face admission stay unchanged.
                    Texture2D mask = null, candidate = row.holoMask;
                    bool eligible = false;
                    if (face != null && candidate != null && isHolo != null)
                    {
                        try { eligible = isHolo(row.cardId); }
                        catch (Exception) { eligible = false; }
                    }
                    if (eligible && candidate.width == face.width && candidate.height == face.height)
                    {
                        long size = (long)candidate.width * candidate.height;
                        if (size > 0 && (uniqueMasks.Contains(candidate) || size <= MaskPixelLimit - maskPixels))
                        {
                            if (uniqueMasks.Add(candidate)) maskPixels += size;
                            mask = candidate; result.MaskCount++;
                        }
                    }
                    result.faces.Add(row.cardId,new FaceBinding { printing=printing, face=face, mask=mask });
                }
                if (combinedPixels > CombinedPixelLimit - pixels) throw new ArgumentException("Combined supplied art pixel budget exceeded.","profiles");
                combinedPixels += pixels; result.counts.Add(printing,count); result.FaceCount += count;
            }
            return result;
        }
        public bool TryGetFace(string cardId,string printingId,out Texture2D face)
        {
            face = null; FaceBinding binding;
            if (cardId == null || printingId == null || !faces.TryGetValue(cardId,out binding) || !string.Equals(binding.printing,printingId,StringComparison.Ordinal)) return false;
            face = binding.face; return face != null;
        }
        public bool TryGetHoloMask(string cardId,string printingId,out Texture2D mask)
        {
            mask = null; FaceBinding binding;
            if (cardId == null || printingId == null || !faces.TryGetValue(cardId,out binding) || !string.Equals(binding.printing,printingId,StringComparison.Ordinal)) return false;
            if (binding.face == null || binding.mask == null || binding.mask.width != binding.face.width || binding.mask.height != binding.face.height) return false;
            mask = binding.mask; return true;
        }
        public int FaceCountForPrinting(string printingId)
        {
            int count; return printingId != null && counts.TryGetValue(printingId,out count) ? count : 0;
        }
    }
}
