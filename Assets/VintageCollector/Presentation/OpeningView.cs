namespace VintageCollector.Presentation
{
    // Read-only-in-practice projection. Runtime owns all authoritative state.
    public sealed class OpeningView
    {
        public string stage, transactionId, errorMessage, recoveryNotice, modelLabel, statusLine, saveLocation;
        public int revealedCount, totalVirtualCards;
        public OpeningCardView[] cards;
        public bool paused;
        public float volume = 0.5f;
        public int inspectIndex = -1;
    }

    public sealed class OpeningCardView
    {
        public string id, name, collectorNumber, rarity, kind, finish;
        public bool isNew;
    }
}
