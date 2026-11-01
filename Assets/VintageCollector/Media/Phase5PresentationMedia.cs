using System;
using System.Collections.Generic;
using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Presentation;
namespace VintageCollector.Media
{
    // Stable borrowed-reference adapter: no playback, decoding, I/O or resource ownership.
    public sealed class Phase5PresentationMedia : IPresentationMedia, IWrapperMetallicMedia, IRevealSoundMedia, ICardTemplateMedia
    {
        private const string BasePrinting = "base-en-unlimited1999";
        private sealed class Binding
        {
            public Texture2D template, cardBack, wrapperBack;
            public Texture2D[] fronts;
            public AudioClip[] pulls;
            public AudioClip nonPokemon, pokemon, holo;
            public int rarePosition;
        }
        private readonly IPresentationMedia baseMedia;
        private readonly Func<string,string> printingForCardId;
        private readonly Dictionary<string,Binding> bindings = new Dictionary<string,Binding>(StringComparer.Ordinal);
        private string context = BasePrinting;
        private int variant;
        private readonly SuppliedCardArtMap suppliedArt;
        public Phase5PresentationMedia(IPresentationMedia baseMedia, Phase5SetConfig[] configs, Func<string,string> printingForCardId)
            : this(baseMedia,configs,printingForCardId,null) { }
        public Phase5PresentationMedia(IPresentationMedia baseMedia, Phase5SetConfig[] configs, Func<string,string> printingForCardId, SuppliedCardArtMap suppliedArt)
        {
            this.suppliedArt=suppliedArt;
            if (configs == null) throw new ArgumentNullException("configs");
            if (printingForCardId == null) throw new ArgumentNullException("printingForCardId");
            this.baseMedia=baseMedia; this.printingForCardId=printingForCardId;
            foreach (var row in configs)
            {
                if (row == null || (row.printingId != BasePrinting && row.printingId != "jungle-en-unlimited1999" && row.printingId != "fossil-en-unlimited1999"))
                    throw new ArgumentException("Unknown or null presentation binding.","configs");
                if (bindings.ContainsKey(row.printingId)) throw new ArgumentException("Duplicate presentation binding.","configs");
                var b=new Binding {template=row.cardFaceTemplate,cardBack=row.cardBack,wrapperBack=row.wrapperBack,
                    fronts=row.wrapperFronts==null?new Texture2D[3]:(Texture2D[])row.wrapperFronts.Clone()};
                if (b.fronts.Length != 3) throw new ArgumentException("Wrapper fronts must have exactly A/B/C slots.","configs");
                var p=row.revealAudioProfile;
                if (p != null) {
                    b.pulls=p.pullSounds==null?null:(AudioClip[])p.pullSounds.Clone(); b.rarePosition=p.rareRevealPosition;
                    b.nonPokemon=p.nonPokemonRare; b.pokemon=p.pokemonRare; b.holo=p.holoRare;
                }
                bindings.Add(row.printingId,b);
            }
        }
        public void SetContext(string printingId,string transactionId)
        {
            // Unknown context fails closed; never silently borrows another set's images/profile.
            context=printingId; variant=WrapperVariant.ForTransaction(transactionId);
        }
        private Binding Current { get { Binding b;return context!=null && bindings.TryGetValue(context,out b)?b:null; } }
        private string Printing(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            try { return printingForCardId(cardId); }
            catch (KeyNotFoundException) { return null; }
            catch (ArgumentException) { return null; }
        }
        public Texture2D CardFace(string cardId)
        {
            string printing=Printing(cardId); Texture2D face;
            if(printing==BasePrinting) return baseMedia==null?null:baseMedia.CardFace(cardId);
            return suppliedArt!=null && suppliedArt.TryGetFace(cardId,printing,out face)?face:null;
        }
        public Texture2D HoloMask(string cardId) { return Printing(cardId)==BasePrinting && baseMedia!=null?baseMedia.HoloMask(cardId):null; }
        public Texture2D CardTemplate(string cardId)
        {
            string printing=Printing(cardId);Binding b;
            Texture2D face;
            if(suppliedArt!=null && suppliedArt.TryGetFace(cardId,printing,out face)) return null;
            return printing!=null && printing!=BasePrinting && bindings.TryGetValue(printing,out b)?b.template:null;
        }
        public Texture2D CardBack { get {
            // The supplied Base back is shared by all admitted printings, never unknown contexts.
            if(context!=BasePrinting && context!="jungle-en-unlimited1999" && context!="fossil-en-unlimited1999") return null;
            var shared=baseMedia==null?null:baseMedia.CardBack;
            return shared!=null?shared:context==BasePrinting?null:Current==null?null:Current.cardBack;
        } }
        public Texture2D WrapperFront { get {
            var b=Current;
            if(context==BasePrinting) return b!=null && b.fronts[variant]!=null?b.fronts[variant]:baseMedia==null?null:baseMedia.WrapperFront;
            return b==null?null:b.fronts[variant];
        } }
        public Texture2D WrapperBack { get { return context==BasePrinting?(baseMedia==null?null:baseMedia.WrapperBack):(Current==null?null:Current.wrapperBack); } }
        public Texture2D WrapperFrontMask { get { var m=baseMedia as IWrapperMetallicMedia;return context==BasePrinting && m!=null?m.WrapperFrontMask:null; } }
        public Texture2D WrapperBackMask { get { var m=baseMedia as IWrapperMetallicMedia;return context==BasePrinting && m!=null?m.WrapperBackMask:null; } }
        public AudioClip Sound(string committedAction) { return baseMedia==null?null:baseMedia.Sound(committedAction); }
        public AudioClip RevealSound(int oneBasedPosition,string kind,string finish)
        {
            var b=Current;if (b==null || oneBasedPosition<1 || oneBasedPosition>11) return null;
            if (oneBasedPosition==b.rarePosition) {
                var rare=finish=="holo"?b.holo:kind=="pokemon"?b.pokemon:b.nonPokemon;
                if (rare!=null) return rare;
            }
            return b.pulls!=null && oneBasedPosition<=b.pulls.Length?b.pulls[oneBasedPosition-1]:null;
        }
        public string StatusText { get {
            if(context==BasePrinting) return baseMedia==null?"Generic fallback art":baseMedia.StatusText;
            return suppliedArt!=null && suppliedArt.FaceCountForPrinting(context)>0
                ?"Supplied First Edition-stamped visual substitutes / Unlimited identities; missing faces use templates"
                :"Temporary identity-neutral art / matte templates";
        } }
        public int SuppliedAssetCount { get { return (baseMedia==null?0:baseMedia.SuppliedAssetCount)+(suppliedArt==null?0:suppliedArt.FaceCount); } }
    }
}
