using System;
using System.IO;
using System.Collections.Generic;
using VintageCollector.Domain;
namespace VintageCollector.Persistence
{
    public sealed class SessionStore : IDisposable
    {
        readonly Catalog catalog; readonly SaveProtocol protocol;
        SessionData state; bool uncertain,disposed; string recoveryNotice; FileStream writerLock;
        public SessionStore(string directory,IStateSerializer serializer,Catalog catalog,ISaveFaultInjector faults=null)
        {
            if(directory==null) throw new ArgumentNullException("directory");
            if(serializer==null) throw new ArgumentNullException("serializer");
            if(catalog==null) throw new ArgumentNullException("catalog");
            directory=Path.GetFullPath(directory);
            this.catalog=catalog; Directory.CreateDirectory(directory); protocol=new SaveProtocol(directory,serializer,catalog,faults);
            try { writerLock=new FileStream(Path.Combine(directory,"state.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None); writerLock.Lock(0,1); Reload(); }
            catch { if(writerLock!=null) writerLock.Dispose(); throw; }
        }
        public SessionSnapshot Snapshot { get { EnsureReady(); return new SessionSnapshot(state,recoveryNotice); } }
        void CheckDisposed() { if(disposed) throw new ObjectDisposedException("SessionStore"); }
        void EnsureReady() { CheckDisposed(); if(uncertain) Reload(); }
        public SessionSnapshot Reload()
        {
            CheckDisposed(); uncertain=true; string notice; var loaded=protocol.Load(state,out notice); state=loaded; uncertain=false; if(notice!=null) recoveryNotice=notice; return Snapshot;
        }
        public PackSnapshot OpenPack(string transactionId,string createdUtc,uint seed,PackResult pack)
        {
            EnsureReady(); if(pack==null) throw new ArgumentNullException("pack");
            var ids=new string[pack.OrderedCardIds.Count]; pack.OrderedCardIds.CopyTo(ids,0);
            var flags=new bool[ids.Length]; var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var old in state.packs) foreach(string id in old.orderedCardIds) seen.Add(id);
            for(int i=0;i<ids.Length;i++) flags[i]=seen.Add(ids[i]);
            var p=new PackRecordData {transactionId=transactionId,createdUtc=createdUtc,seed=seed,modelId=pack.ModelId,modelVersion=pack.ModelVersion,catalogVersion=pack.CatalogVersion,printingId=pack.PrintingId,rngAlgorithm=pack.RngAlgorithm,status=pack.Status,orderedCardIds=ids,newToVirtual=flags};
            try { StateValidation.Identity(p,catalog); } catch(InvalidDataException e) { throw new ArgumentException(e.Message,e); }
            foreach(var old in state.packs) if(old.transactionId==transactionId) {
                if(!StateCopy.SameIdentity(old,p)) throw new ArgumentException("Transaction input collision");
                return new PackSnapshot(old);
            }
            if(state.packs.Length>0 && !Snapshot.CurrentPack.IsComplete) throw new InvalidOperationException("Current pack is incomplete");
            var d=StateCopy.Session(state); Array.Resize(ref d.packs,d.packs.Length+1); d.packs[d.packs.Length-1]=p; d.revision=checked(d.revision+1); Commit(d); return Snapshot.CurrentPack;
        }
        void Commit(SessionData d) { uncertain=true; protocol.Save(d); state=StateCopy.Session(d); uncertain=false; }
        int Find(string transactionId) { EnsureReady(); for(int i=0;i<state.packs.Length;i++) if(state.packs[i].transactionId==transactionId) return i; throw new KeyNotFoundException("Unknown transaction"); }
        public PackSnapshot OpenWrapper(string transactionId)
        {
            int i=Find(transactionId); if(state.packs[i].wrapperOpened) return new PackSnapshot(state.packs[i]);
            var d=StateCopy.Session(state); d.packs[i].wrapperOpened=true; d.revision=checked(d.revision+1); Commit(d); return new PackSnapshot(state.packs[i]);
        }
        public PackSnapshot RevealTo(string transactionId,int targetCount)
        {
            int i=Find(transactionId); var p=state.packs[i];
            if(targetCount<0 || targetCount>p.orderedCardIds.Length) throw new ArgumentException("Reveal target out of bounds");
            if(targetCount>0 && !p.wrapperOpened) throw new InvalidOperationException("Wrapper is closed");
            if(targetCount<=p.revealedCount) return new PackSnapshot(p);
            var d=StateCopy.Session(state); d.packs[i].revealedCount=targetCount; d.revision=checked(d.revision+1); Commit(d); return new PackSnapshot(state.packs[i]);
        }
        public void Dispose() { if(disposed) return; disposed=true; if(writerLock!=null) writerLock.Dispose(); }
    }
}
