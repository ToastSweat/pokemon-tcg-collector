using System;
using System.Collections.ObjectModel;
using VintageCollector.Domain;

namespace VintageCollector.Collections
{
    [Serializable] public sealed class PhysicalStateData { public int schemaVersion; public long revision; public PhysicalEditData[] edits; }
    [Serializable] public sealed class PhysicalEditData { public string transactionId, createdUtc, cardId, condition; public int quantity; }
    public interface IPhysicalSerializer { string Serialize(PhysicalStateData data); PhysicalStateData Deserialize(string text); }

    public sealed class PhysicalHolding
    {
        public string CardId { get; private set; }
        public string Condition { get; private set; }
        public int Quantity { get; private set; }
        internal PhysicalHolding(string cardId,string condition,int quantity) { CardId=cardId; Condition=condition; Quantity=quantity; }
    }
    public sealed class PhysicalSnapshot
    {
        public long Revision { get; private set; }
        public ReadOnlyCollection<PhysicalHolding> Holdings { get; private set; }
        public string RecoveryNotice { get; private set; }
        internal PhysicalSnapshot(long revision,PhysicalHolding[] holdings,string recoveryNotice)
        { Revision=revision; Holdings=Array.AsReadOnly((PhysicalHolding[])holdings.Clone()); RecoveryNotice=recoveryNotice; }
    }
    public sealed class BinderRow
    {
        public string CardId { get; private set; }
        public string Name { get; private set; }
        public string CollectorNumber { get; private set; }
        public string Rarity { get; private set; }
        public string Finish { get; private set; }
        public string Kind { get; private set; }
        public int Quantity { get; private set; }
        public bool Owned { get { return Quantity>0; } }
        internal BinderRow(CardPrinting card,int quantity)
        { CardId=card.Id; Name=card.DisplayName; CollectorNumber=card.CollectorNumber; Rarity=card.Rarity; Finish=card.Finish; Kind=card.Kind; Quantity=quantity; }
    }
    public sealed class BinderSnapshot
    {
        public ReadOnlyCollection<BinderRow> Rows { get; private set; }
        public int TargetCount { get { return 101; } }
        public int UniqueOwned { get; private set; }
        public int TotalQuantity { get; private set; }
        public double CompletionPercent { get { return UniqueOwned*100.0/TargetCount; } }
        internal BinderSnapshot(BinderRow[] rows,int uniqueOwned,int totalQuantity)
        { Rows=Array.AsReadOnly((BinderRow[])rows.Clone()); UniqueOwned=uniqueOwned; TotalQuantity=totalQuantity; }
    }
    public sealed class ImportPreview
    {
        public string ReceiptId { get; private set; }
        public string SourceHash { get; private set; }
        public string SourceName { get; private set; }
        public int VirtualCardCount { get; private set; }
        public int PhysicalCardCount { get; private set; }
        public int VirtualUnique { get; private set; }
        public int PhysicalUnique { get; private set; }
        public string Summary { get; private set; }
        internal ImportPreview(string receiptId,string sourceHash,string sourceName,int virtualCardCount,int physicalCardCount,int virtualUnique,int physicalUnique,string summary)
        { ReceiptId=receiptId; SourceHash=sourceHash; SourceName=sourceName; VirtualCardCount=virtualCardCount; PhysicalCardCount=physicalCardCount; VirtualUnique=virtualUnique; PhysicalUnique=physicalUnique; Summary=summary; }
    }
}
