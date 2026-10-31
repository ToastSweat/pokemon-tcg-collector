using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using VintageCollector.Collections;
using VintageCollector.Domain;
using VintageCollector.Persistence;
using VintageCollector.Presentation;

namespace VintageCollector.Runtime {
    // One synchronous owner: container lease -> session -> physical. Release in reverse order.
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class Phase4CollectionHost : MonoBehaviour {
        public OpeningGame Game { get; private set; }
        public PhysicalSnapshot Physical { get; private set; }
        public CollectionProfiles Profiles { get; private set; }
        public BinderView CurrentBinderView { get; private set; }
        public BinderView View { get { return CurrentBinderView; } }
        public bool IsReady { get; private set; }
        public string StatusMessage { get; private set; } = "Collection awaiting initialization.";
        private PhysicalStore physicalStore;
        private readonly UnityPhysicalSerializer physicalSerializer = new UnityPhysicalSerializer();
        private readonly UnityStateSerializer sessionSerializer = new UnityStateSerializer();
        private Catalog catalog;
        private BinderPresentation binder;
        private TabletopPresentation table;
        private BinderSnapshot projection;
        private OpeningView observedView;
        private ImportPreview reviewed;
        private PhysicalEditData pendingEdit;
        private bool initialized, closed, open, restartRequired, pendingConfirm;
        private string mode="virtual", filter="all", search="", selectedId, condition="unknown", quantity="0", error;
        private int page, backupIndex=-1;
        private string[] backupPaths=new string[0];
        private readonly RuntimeFault physicalFault = new RuntimeFault();
        private readonly RuntimeFault importFault = new RuntimeFault();
        private sealed class RuntimeFault : ISaveFaultInjector {
            public string Stage;
            public void At(string stage) { if (stage == Stage) { Stage=null; throw new IOException("Explicit isolated runtime fault"); } }
        }
        private void Start() { Initialize(Environment.GetCommandLineArgs()); }
        private void Update() {
            if (!IsReady || closed) return;
            // View assignment acknowledges a UI request, so never assign an unchanged DTO every frame.
            if (!ReferenceEquals(observedView, Game.CurrentView)) Publish();
        }
        public void Initialize(string[] args) {
            if (initialized || closed) return;
            initialized=true;
            Game=GetComponent<OpeningGame>();
            if (Game==null) Game=gameObject.AddComponent<OpeningGame>();
            Game.CollectionCommand+=DispatchBinderCommand;
            try {
                var paths=GamePaths.Resolve(Game.allowedProjectRoot,Application.dataPath,Application.isEditor,args);
                Profiles=new CollectionProfiles(paths.BaseSaveDirectory,paths.IsSmokeTest?importFault:null);
                Game.PhysicalTrackingConfigured=true;
                Game.InitializeForProfile(args,Profiles.ActiveProfileId);
                if (Game.CurrentSession==null || Game.HasPendingStorage) throw new InvalidOperationException();
                if (!string.Equals(Game.Paths.SaveDirectory,Profiles.ActiveDirectory,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
                catalog=Catalog.FromData(JsonUtility.FromJson<CatalogData>(Game.catalogJson.text));
                physicalStore=new PhysicalStore(Game.Paths.SaveDirectory,physicalSerializer,catalog,paths.IsSmokeTest?physicalFault:null);
                Physical=physicalStore.Snapshot;
                binder=GetComponent<BinderPresentation>();
                if (binder==null) binder=gameObject.AddComponent<BinderPresentation>();
                binder.Command+=DispatchBinderCommand;
                table=GetComponent<TabletopPresentation>();
                table.CollectionAvailable=true;
                RefreshBackups();
                Game.ShuttingDown+=Shutdown;
                IsReady=true;
                StatusMessage="Collection ready. Virtual revealed pulls and physical ownership are independent.";
                Publish();
            } catch (Exception) {
                // Never expose filesystem paths, pointer contents, or initialize the default collection on failure.
                StatusMessage="Collection startup blocked. Selection or stored data could not be validated. Quit and resolve storage before reopening.";
                error=StatusMessage;
                ReleaseStores();
                Game.BlockInitialization(StatusMessage);
                table=GetComponent<TabletopPresentation>();
                if (table!=null) table.CollectionAvailable=false;
            }
        }
        private void RefreshBackups() {
            backupPaths=Profiles.BackupFiles;
            if (backupIndex>=backupPaths.Length) backupIndex=-1;
        }
        private bool Selected() {
            if (projection==null || string.IsNullOrEmpty(selectedId)) return false;
            foreach(var row in projection.Rows) if(row.CardId==selectedId) return true;
            return false;
        }
        private static bool KnownFilter(string value) { return value=="all" || value=="owned" || value=="missing" || value=="duplicates" || value=="holo"; }
        private static bool KnownCondition(string value) { return value=="unknown" || value=="near-mint" || value=="lightly-played" || value=="moderately-played" || value=="heavily-played" || value=="damaged"; }
        private static bool Index(string value,int limit,out int index) {
            return int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out index) && index>=0 && index<limit && index.ToString(CultureInfo.InvariantCulture)==value;
        }
        private void LoadQuantity() {
            quantity="0";
            if (Physical!=null && selectedId!=null) foreach(var h in Physical.Holdings)
                if(h.CardId==selectedId && h.Condition==condition) quantity=h.Quantity.ToString(CultureInfo.InvariantCulture);
        }
        public void DispatchBinderCommand(string command) {
            if (closed || !IsReady || string.IsNullOrEmpty(command)) return;
            if (command=="quit") { Shutdown(); if(!Application.isEditor) Application.Quit(); return; }
            if (command=="binder-open" || command=="binder-close") {
                open=command=="binder-open"; Publish(); return;
            }
            if (!open || restartRequired) return;
            // Frozen intent survives Back/reopen. No new edit/review/export/cancel can replace uncertain work.
            if (pendingEdit!=null && command!="save-physical") return;
            if (pendingConfirm && command!="import-confirm") return;
            if (reviewed!=null && command!="import-confirm" && command!="import-cancel") return;
            if (!ReferenceEquals(observedView,Game.CurrentView)) Publish();
            try {
                if (command=="mode:virtual" || command=="mode:physical") {
                    mode=command.Substring(5); selectedId=null; page=0; error=null;
                } else if(command.StartsWith("filter:",StringComparison.Ordinal) && KnownFilter(command.Substring(7))) {
                    filter=command.Substring(7); page=0; error=null;
                } else if(command.StartsWith("search:",StringComparison.Ordinal) && command.Length<=263) {
                    search=command.Substring(7); page=0; error=null;
                } else if(command.StartsWith("page:",StringComparison.Ordinal)) {
                    int n;if(!Index(command.Substring(5),Math.Max(1,(projection.Rows.Count+11)/12),out n)) return;page=n;
                } else if(command.StartsWith("select:",StringComparison.Ordinal)) {
                    string id=command.Substring(7); bool found=false;
                    foreach(var row in projection.Rows) if(row.CardId==id) found=true;
                    if(!found) return;selectedId=id;LoadQuantity();error=null;
                } else if(command.StartsWith("condition:",StringComparison.Ordinal)) {
                    if(mode!="physical" || !Selected() || !KnownCondition(command.Substring(10))) return;
                    condition=command.Substring(10);LoadQuantity();error=null;
                } else if(command.StartsWith("quantity:",StringComparison.Ordinal)) {
                    if(mode!="physical" || !Selected() || command.Length>41) return;
                    quantity=command.Substring(9);error=null;
                } else if(command=="save-physical") {
                    if(pendingEdit==null) {
                        int n;
                        if(mode!="physical" || !Selected() || !KnownCondition(condition) ||
                           !int.TryParse(quantity,NumberStyles.None,CultureInfo.InvariantCulture,out n) || n<0 || n>99999 || n.ToString(CultureInfo.InvariantCulture)!=quantity) {
                            error="Quantity must be a canonical whole number from 0 to 99999 for a selected physical printing.";Publish();return;
                        }
                        // Freeze identity, UTC and absolute bucket body BEFORE invoking the durable store.
                        pendingEdit=new PhysicalEditData {transactionId=Guid.NewGuid().ToString("N"),createdUtc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture),cardId=selectedId,condition=condition,quantity=n};
                    }
                    var e=pendingEdit;
                    Physical=physicalStore.SetQuantity(e.transactionId,e.createdUtc,e.cardId,e.condition,e.quantity);
                    pendingEdit=null;error=null;StatusMessage="Physical ownership acknowledged. No virtual holdings changed.";
                } else if(command=="backup") {
                    if(!Acknowledged() || !string.IsNullOrEmpty(error) || reviewed!=null) {
                        StatusMessage="Backup not started. Resolve the opening save or collection error before exporting.";Publish();return;
                    }
                    // The first export must not mistake deleted acknowledged default-store
                    // files for a never-used blank profile. Cached snapshots are not disk proof.
                    if(!Game.ReconcileForCollectionExport()) throw new InvalidOperationException();
                    Physical=physicalStore.Reload();
                    Profiles.CreateBackup(Game.Paths.SaveDirectory,sessionSerializer,physicalSerializer,catalog);
                    RefreshBackups();error=null;StatusMessage="Validated collection backup created explicitly.";
                } else if(command.StartsWith("backup-select:",StringComparison.Ordinal)) {
                    int n;if(!Index(command.Substring(14),backupPaths.Length,out n)) return;backupIndex=n;error=null;
                } else if(command=="import-preview") {
                    if(!Acknowledged() || backupIndex<0 || backupIndex>=backupPaths.Length) {
                        StatusMessage="Preview not started. Resolve the opening save and select an available backup.";Publish();return;
                    }
                    var preview=Profiles.PreviewImport(backupPaths[backupIndex],sessionSerializer,physicalSerializer,catalog);
                    reviewed=preview;error=null;StatusMessage="Review retained. Confirm creates a separate profile for next launch.";
                } else if(command=="import-cancel") {
                    if(reviewed!=null) Profiles.DiscardPreview(reviewed);
                    reviewed=null;error=null;
                } else if(command=="import-confirm") {
                    if(reviewed==null || !Acknowledged()) {
                        StatusMessage="Restore not started. A validated owned review and acknowledged stores are required.";Publish();return;
                    }
                    pendingConfirm=true; // Retain this exact owned preview even when acknowledgement is uncertain.
                    Profiles.ConfirmImport(reviewed);
                    pendingConfirm=false;reviewed=null;restartRequired=true;error=null;
                    StatusMessage="Restart required. Quit / reopen activates the new profile. Current profile unchanged.";
                } else return;
            } catch(Exception) {
                error=pendingEdit!=null?"Physical save acknowledgement uncertain. Retry the SAME ownership edit; draft changes and export/import are locked.":
                    pendingConfirm?"Restore acknowledgement uncertain. Retry the SAME reviewed restore; all state changes remain locked.":
                    "Collection operation failed validation or storage access. Original data and drafts retained. Retry explicitly.";
            }
            Publish();
        }
        private bool Acknowledged() { return pendingEdit==null && !Game.HasPendingStorage && Game.CurrentSession!=null && Physical!=null; }
        private void Publish() {
            if(!IsReady || closed) return;
            observedView=Game.CurrentView;
            projection=BinderProjection.Build(catalog,Game.CurrentSession,Physical,mode,filter,search);
            if(!Selected()) {selectedId=null;quantity="0";}
            page=Math.Max(0,Math.Min(page,Math.Max(0,(projection.Rows.Count-1)/12)));
            var rows=new BinderCardView[projection.Rows.Count];
            for(int i=0;i<rows.Length;i++) {
                var r=projection.Rows[i];rows[i]=new BinderCardView {id=r.CardId,name=r.Name,collectorNumber=r.CollectorNumber,rarity=r.Rarity,finish=r.Finish,kind=r.Kind,quantity=r.Quantity,owned=r.Owned};
            }
            var backups=new string[backupPaths.Length];for(int i=0;i<backups.Length;i++) backups[i]=Path.GetFileName(backupPaths[i]);
            CurrentBinderView=new BinderView {open=open,mode=mode,filter=filter,search=search,page=page,rows=rows,
                targetCount=projection.TargetCount,uniqueOwned=projection.UniqueOwned,totalQuantity=projection.TotalQuantity,completionPercent=projection.CompletionPercent,
                selectedCardId=selectedId,selectedCondition=condition,quantityText=quantity,backups=backups,selectedBackupIndex=backupIndex,
                importSummary=reviewed==null?null:reviewed.Summary,restartRequired=restartRequired,pendingPhysicalEdit=pendingEdit!=null,pendingImportConfirm=pendingConfirm,
                error=error,status=StatusMessage,recoveryNotice=Physical.RecoveryNotice};
            binder.View=CurrentBinderView;
            // Detached positive physical IDs only. Results consume these read-only badges;
            // the normal card11 hold and all earlier reveal faces remain untouched.
            var owned=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach(var h in Physical.Holdings) if(h.Quantity>0) owned.Add(h.CardId);
            table.PhysicalOwnershipReader=id=>owned.Contains(id);
            bool blocked=open || pendingEdit!=null || pendingConfirm || reviewed!=null || restartRequired;
            Game.SetCollectionGate(blocked);
            table.CollectionInputBlocked=blocked;
            table.CollectionOverlayVisible=open;
        }
        // Explicit isolated smoke mode only; production callers cannot arm faults.
        public void FailNextPhysicalCommitForSmoke() { RequireSmoke();physicalFault.Stage="after-prepare"; }
        public void FailNextImportForSmoke() { RequireSmoke();importFault.Stage="before-import-ack"; }
        private void RequireSmoke() { if(!IsReady || Game.Paths==null || !Game.Paths.IsSmokeTest) throw new InvalidOperationException("Explicit isolated smoke mode required"); }
        private void ReleaseStores() {
            if(physicalStore!=null) {physicalStore.Dispose();physicalStore=null;}
            if(Game!=null) Game.Shutdown();
            if(Profiles!=null) {Profiles.Dispose();Profiles=null;}
        }
        public void Shutdown() {
            if(closed) return;closed=true;IsReady=false;
            if(binder!=null) binder.Command-=DispatchBinderCommand;
            if(table!=null) {table.PhysicalOwnershipReader=null;table.CollectionAvailable=false;}
            if(Game!=null) {Game.CollectionCommand-=DispatchBinderCommand;Game.ShuttingDown-=Shutdown;}
            ReleaseStores();
        }
        private void OnDestroy() { Shutdown(); }
        private void OnApplicationQuit() { Shutdown(); }
    }
}
