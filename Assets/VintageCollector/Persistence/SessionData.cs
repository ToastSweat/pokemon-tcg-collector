using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace VintageCollector.Persistence
{
    public interface IStateSerializer { string Serialize(SessionData data); SessionData Deserialize(string text); }
    public interface ISaveFaultInjector { void At(string stage); }
    [Serializable] public sealed class SessionData { public int schemaVersion; public long revision; public PackRecordData[] packs; }
    [Serializable] public sealed class PackRecordData
    {
        public string transactionId, createdUtc, modelId, modelVersion, catalogVersion, printingId, rngAlgorithm, status;
        public uint seed; public string[] orderedCardIds; public bool[] newToVirtual;
        public int revealedCount; public bool wrapperOpened;
    }
    public sealed class PackSnapshot
    {
        public string TransactionId { get; private set; }
        public string CreatedUtc { get; private set; }
        public string ModelId { get; private set; }
        public string ModelVersion { get; private set; }
        public string CatalogVersion { get; private set; }
        public string PrintingId { get; private set; }
        public string RngAlgorithm { get; private set; }
        public string Status { get; private set; }
        public uint Seed { get; private set; }
        public ReadOnlyCollection<string> OrderedCardIds { get; private set; }
        public ReadOnlyCollection<bool> NewToVirtual { get; private set; }
        public int RevealedCount { get; private set; }
        public bool WrapperOpened { get; private set; }
        public bool IsComplete { get { return RevealedCount == OrderedCardIds.Count; } }
        internal PackSnapshot(PackRecordData p)
        {
            TransactionId=p.transactionId; CreatedUtc=p.createdUtc; ModelId=p.modelId; ModelVersion=p.modelVersion;
            CatalogVersion=p.catalogVersion; PrintingId=p.printingId; RngAlgorithm=p.rngAlgorithm; Status=p.status; Seed=p.seed;
            OrderedCardIds=Array.AsReadOnly((string[])p.orderedCardIds.Clone()); NewToVirtual=Array.AsReadOnly((bool[])p.newToVirtual.Clone());
            RevealedCount=p.revealedCount; WrapperOpened=p.wrapperOpened;
        }
    }
    public sealed class VirtualHolding
    {
        public string CardId { get; private set; } public int Quantity { get; private set; }
        internal VirtualHolding(string id,int quantity) { CardId=id; Quantity=quantity; }
    }
    public sealed class SessionSnapshot
    {
        public long Revision { get; private set; }
        public ReadOnlyCollection<PackSnapshot> Packs { get; private set; }
        public PackSnapshot CurrentPack { get { return Packs.Count==0 ? null : Packs[Packs.Count-1]; } }
        public ReadOnlyCollection<VirtualHolding> VirtualHoldings { get; private set; }
        public int TotalVirtualCards { get; private set; }
        public string RecoveryNotice { get; private set; }
        internal SessionSnapshot(SessionData d,string notice)
        {
            Revision=d.revision; RecoveryNotice=notice;
            var packs=new List<PackSnapshot>(); var quantities=new SortedDictionary<string,int>(StringComparer.Ordinal);
            foreach(var p in d.packs) { packs.Add(new PackSnapshot(p)); foreach(string id in p.orderedCardIds) { int n; quantities.TryGetValue(id,out n); quantities[id]=checked(n+1); TotalVirtualCards=checked(TotalVirtualCards+1); } }
            Packs=packs.AsReadOnly(); var holdings=new List<VirtualHolding>(); foreach(var q in quantities) holdings.Add(new VirtualHolding(q.Key,q.Value)); VirtualHoldings=holdings.AsReadOnly();
        }
    }
    internal static class StateCopy
    {
        internal static PackRecordData Pack(PackRecordData p)
        {
            return new PackRecordData { transactionId=p.transactionId,createdUtc=p.createdUtc,modelId=p.modelId,modelVersion=p.modelVersion,catalogVersion=p.catalogVersion,printingId=p.printingId,rngAlgorithm=p.rngAlgorithm,status=p.status,seed=p.seed,orderedCardIds=(string[])p.orderedCardIds.Clone(),newToVirtual=(bool[])p.newToVirtual.Clone(),revealedCount=p.revealedCount,wrapperOpened=p.wrapperOpened };
        }
        internal static bool SameIdentity(PackRecordData a,PackRecordData b)
        {
            if(a.transactionId!=b.transactionId || a.createdUtc!=b.createdUtc || a.seed!=b.seed || a.modelId!=b.modelId || a.modelVersion!=b.modelVersion || a.catalogVersion!=b.catalogVersion || a.printingId!=b.printingId || a.rngAlgorithm!=b.rngAlgorithm || a.status!=b.status || a.orderedCardIds.Length!=b.orderedCardIds.Length) return false;
            for(int i=0;i<a.orderedCardIds.Length;i++) if(a.orderedCardIds[i]!=b.orderedCardIds[i]) return false;
            return true;
        }
        internal static SessionData Session(SessionData d) { var a=new PackRecordData[d.packs.Length]; for(int i=0;i<a.Length;i++) a[i]=Pack(d.packs[i]); return new SessionData { schemaVersion=d.schemaVersion,revision=d.revision,packs=a }; }
    }
}
