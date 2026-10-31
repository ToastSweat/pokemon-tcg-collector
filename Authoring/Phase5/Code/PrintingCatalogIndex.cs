using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VintageCollector.Domain;

namespace VintageCollector.Authoring
{
    // Read-only data-preparation component. Does not admit generation rules or touch saves.
    public sealed class PrintingCatalogIndex
    {
        private readonly Dictionary<string,Catalog> byPrinting = new Dictionary<string,Catalog>(StringComparer.Ordinal);
        private readonly Dictionary<string,CardPrinting> byCard = new Dictionary<string,CardPrinting>(StringComparer.Ordinal);
        public ReadOnlyCollection<Catalog> Catalogs {get;private set;}

        public PrintingCatalogIndex(IEnumerable<Catalog> catalogs)
        {
            if(catalogs==null)throw new ArgumentNullException("catalogs");
            var ordered=new List<Catalog>();
            foreach(var catalog in catalogs) {
                if(catalog==null)throw new ArgumentException("Null catalog entry", "catalogs");
                byPrinting.Add(catalog.PrintingId,catalog);
                ordered.Add(catalog);
                foreach(var card in catalog.Cards)byCard.Add(card.Id,card);
            }
            if(ordered.Count==0)throw new ArgumentException("At least one catalog is required", "catalogs");
            Catalogs=ordered.AsReadOnly();
        }
        public Catalog FindCatalog(string printingId) {return byPrinting[printingId];}
        public CardPrinting FindCard(string cardId) {return byCard[cardId];}
    }
}
