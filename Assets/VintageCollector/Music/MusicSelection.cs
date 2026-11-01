namespace VintageCollector.Music
{
    // One-based profile slots. No Unity dependency, storage reads or game commands.
    public static class MusicSelection
    {
        public static int Select(string stage, string printing, bool binderOpen,
            bool shoppingOpen, bool hasGame, bool relaxed)
        {
            if (shoppingOpen) return 6;
            if (binderOpen) return relaxed ? 8 : 5;
            if (!hasGame) return 1;
            if (stage == "results") return 7;
            if (stage != "ready" && stage != "sealed" && stage != "revealing") return 1;
            switch (printing)
            {
                case "base-en-unlimited1999": return 2;
                case "jungle-en-unlimited1999": return 3;
                case "fossil-en-unlimited1999": return 4;
                default: return 0; // Never guess a set from a display label or partial ID.
            }
        }
    }
}
