using System;
using System.Collections.ObjectModel;
namespace VintageCollector.Domain
{
    public sealed class CardPrinting
    {
        public string Id { get; private set; }
        public string SourceId { get; private set; }
        public string DisplayName { get; private set; }
        public string SourceName { get; private set; }
        public string SetId { get; private set; }
        public string CollectorNumber { get; private set; }
        public string Language { get; private set; }
        public string Edition { get; private set; }
        public string PrintingId { get; private set; }
        public string Variant { get; private set; }
        public string Rarity { get; private set; }
        public string Kind { get; private set; }
        public string Finish { get; private set; }
        public string ArtReference { get; private set; }
        public bool BoosterEligible { get; private set; }
        internal CardPrinting(CardData data) { Id = data.id; SourceId = data.sourceId; DisplayName = data.displayName; SourceName = data.sourceName; SetId = data.setId; CollectorNumber = data.collectorNumber; Language = data.language; Edition = data.edition; PrintingId = data.printingId; Variant = data.variant; Rarity = data.rarity; Kind = data.kind; Finish = data.finish; ArtReference = data.artReference; BoosterEligible = data.boosterEligible; }
    }
    public sealed class Catalog
    {
        public ReadOnlyCollection<CardPrinting> Cards { get; private set; }
        public string CatalogVersion { get; private set; }
        public string PrintingId { get; private set; }
        public string CompletionTargetStatus { get; private set; }
        public string SourceResearch { get; private set; }
        private Catalog() { }
        public static Catalog FromData(CatalogData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.schemaVersion != 1) throw new ArgumentException("Unsupported catalog schemaVersion");
            Require(data.catalogVersion, "catalogVersion"); Require(data.printingId, "printingId");
            Require(data.completionTargetStatus, "completionTargetStatus"); Require(data.sourceResearch, "sourceResearch");
            if (data.cards == null || data.cards.Length == 0) throw new ArgumentException("Empty catalog cards");
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var c in data.cards)
            {
                if (c == null) throw new ArgumentException("Null card");
                Require(c.id, "id"); Require(c.sourceId, "sourceId"); Require(c.displayName, "displayName");
                Require(c.sourceName, "sourceName"); Require(c.setId, "setId"); Require(c.language, "language");
                Require(c.edition, "edition"); Require(c.variant, "variant"); Require(c.artReference, "artReference");
                if (!ids.Add(c.id)) throw new ArgumentException("Duplicate card ID: " + c.id);
                if (c.printingId != data.printingId) throw new ArgumentException("Card printing mismatch");
                int number;
                if (!int.TryParse(c.collectorNumber, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out number) || number < 1 || number.ToString(System.Globalization.CultureInfo.InvariantCulture) != c.collectorNumber)
                    throw new ArgumentException("Noncanonical collectorNumber");
                if (c.rarity != "holo" && c.rarity != "rare" && c.rarity != "uncommon" && c.rarity != "common") throw new ArgumentException("Unknown rarity");
                if (c.kind != "pokemon" && c.kind != "trainer" && c.kind != "basic-energy" && c.kind != "special-energy") throw new ArgumentException("Unknown kind");
                if (c.finish != (c.rarity == "holo" ? "holo" : "nonholo")) throw new ArgumentException("Inconsistent finish");
                string expectedId = "pkmn-" + c.printingId + "-" + number.ToString("D3", System.Globalization.CultureInfo.InvariantCulture) + "-" + c.finish + "-" + c.variant;
                if (c.id != expectedId) throw new ArgumentException("Stable ID does not encode printing/number/finish/variant");
            }
            var cards = new CardPrinting[data.cards.Length];
            for (int i = 0; i < cards.Length; i++) cards[i] = new CardPrinting(data.cards[i]);
            return new Catalog { Cards = Array.AsReadOnly(cards), CatalogVersion = data.catalogVersion, PrintingId = data.printingId, CompletionTargetStatus = data.completionTargetStatus, SourceResearch = data.sourceResearch };
        }
        internal static void Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required " + field);
        }
        public CardPrinting Find(string id)
        {
            if (id == null) throw new ArgumentNullException("id");
            foreach (var card in Cards) if (card.Id == id) return card;
            throw new System.Collections.Generic.KeyNotFoundException(id);
        }
    }
}
