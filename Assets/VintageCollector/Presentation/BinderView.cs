using System;

namespace VintageCollector.Presentation
{
    // Host-owned, privacy-filtered projection. Presentation never modifies these fields.
    [Serializable]
    public sealed class BinderView
    {
        public string printingId, setLabel;
        public bool canSelectSet;
        public SetChoiceView[] supportedSets;
        public bool open, busy, restartRequired;
        public bool pendingPhysicalEdit, pendingImportConfirm;
        public string mode, filter, search, status, error, recoveryNotice;
        public string selectedCardId, selectedCondition, quantityText, importSummary;
        public int targetCount, uniqueOwned, totalQuantity, page;
        public double completionPercent;
        public BinderCardView[] rows;
        public string[] backups;
        public int selectedBackupIndex;
    }

    [Serializable]
    public sealed class BinderCardView
    {
        public string id, name, collectorNumber, rarity, finish, kind;
        public int quantity;
        public bool owned;
    }
}
