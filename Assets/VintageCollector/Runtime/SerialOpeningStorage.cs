using System;
using System.Threading;
using VintageCollector.Domain;
using VintageCollector.Persistence;

namespace VintageCollector.Runtime
{
    // All members are immutable; PackResult itself owns an immutable copy of its IDs.
    internal sealed class OpeningStorageRequest
    {
        internal readonly string command, transactionId, createdUtc;
        internal readonly uint seed;
        internal readonly PackResult result;
        internal readonly int target;
        internal OpeningStorageRequest(string command,string transactionId=null,string createdUtc=null,uint seed=0,PackResult result=null,int target=0)
        { this.command=command;this.transactionId=transactionId;this.createdUtc=createdUtc;this.seed=seed;this.result=result;this.target=target; }
        internal SessionSnapshot Execute(SessionStore store,bool reload)
        {
            if(reload) store.Reload();
            if(command=="generate") store.OpenPack(transactionId,createdUtc,seed,result);
            else if(command=="open") store.OpenWrapper(transactionId);
            else if(command!="reload") store.RevealTo(transactionId,target);
            return store.Snapshot;
        }
    }

    // One dedicated serial worker, one outstanding request INCLUDING unconsumed results.
    // No Unity callbacks, Unity objects, paths, cancellation, or optimistic acknowledgements.
    internal sealed class SerialOpeningStorage : IDisposable
    {
        internal sealed class Completion
        {
            internal OpeningStorageRequest Request;
            internal SessionSnapshot Snapshot;
            internal Exception Error;
        }
        private readonly object gate=new object();
        private readonly AutoResetEvent wake=new AutoResetEvent(false);
        private readonly Thread thread;
        private readonly SessionStore store;
        private OpeningStorageRequest request;
        private bool reload, stopping;
        private Completion completed;
        internal SerialOpeningStorage(SessionStore store)
        { this.store=store;thread=new Thread(Work);thread.IsBackground=true;thread.Name="Opening durable storage";thread.Start(); }
        internal void Submit(OpeningStorageRequest value,bool reconcile)
        {
            lock(gate) {
                if(stopping || request!=null) throw new InvalidOperationException("Opening storage is occupied or closed");
                request=value;reload=reconcile;wake.Set();
            }
        }
        internal bool TryTake(out Completion value)
        {
            lock(gate) {
                value=completed;
                if(value==null) return false;
                completed=null;request=null;return true;
            }
        }
        private void Work()
        {
            while(true) {
                wake.WaitOne();
                OpeningStorageRequest value;bool reconcile;
                lock(gate) { if(stopping && (request==null || completed!=null)) return;value=request;reconcile=reload; }
                if(value==null) continue;
                var answer=new Completion {Request=value};
                try {answer.Snapshot=value.Execute(store,reconcile);} catch(Exception error) {answer.Error=error;}
                lock(gate) {completed=answer;if(stopping) return;}
            }
        }
        public void Dispose()
        {
            lock(gate) { if(stopping) return;stopping=true;wake.Set(); }
            thread.Join(); // Never cancel a durable operation; joining on exit may block.
            wake.Dispose();
        }
    }
}
