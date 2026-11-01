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
    public sealed class Phase5CollectionHost : MonoBehaviour {
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
        private ApprovedSetRegistry registry;
        private string binderPrintingId;
        private BinderPresentation binder;
        private TabletopPresentation table;
        private BinderSnapshot projection;
        private OpeningView observedView;
        private ImportPreview reviewed;
        private PhysicalEditData pendingEdit;
        private bool initialized, closed, open, restartRequired, pendingConfirm;
        private string mode="virtual", filter="all", search="", selectedId, condition="unknown", quantity="0", error;
        private string openingReconciliationError;
        private int page, backupIndex=-1;
        private string[] backupPaths=new string[0];
        private readonly RuntimeFault physicalFault = new RuntimeFault();
        private readonly RuntimeFault importFault = new RuntimeFault();
        private sealed class RuntimeFault : ISaveFaultInjector {
            public string Stage;
            public void At(string stage) { if (stage == Stage) { Stage=null; throw new IOException("Explicit isolated runtime fault"); } }
        }
        // Opt-in only: existing scenes never acquire shopping capabilities.
        public bool ShoppingEnabled { get; set; }
        public bool FundEnabled { get; set; }
        public event Action FundInvalidated;
        private FundLease fundLease;
        private sealed class FundLease : IDisposable {
            Phase5CollectionHost owner;
            internal FundLease(Phase5CollectionHost owner) {this.owner=owner;}
            public void Dispose(){var h=owner;if(h==null)return;owner=null;if(!ReferenceEquals(h.fundLease,this))return;h.fundLease=null;if(!h.closed&&h.IsReady)h.Publish();else if(h.binder!=null)h.binder.ExternalOverlayVisible=false;}
        }
        public bool CanUseFund() {
            CaptureDraft();
            return FundEnabled && !closed && IsReady && StartupLoadingCoordinator.IsReady && open && shoppingLease==null &&
                Game!=null && Game.IsAcceptingInput && !Game.HasPendingStorage && Game.CurrentSession!=null && Physical!=null &&
                pendingEdit==null && !pendingConfirm && reviewed==null && !restartRequired && !DirtyDraft() &&
                string.IsNullOrEmpty(error) && string.IsNullOrEmpty(openingReconciliationError);
        }
        public bool TryBeginFund(out IDisposable overlayLease) {
            overlayLease=null;if(fundLease!=null || !CanUseFund())return false;
            fundLease=new FundLease(this);overlayLease=fundLease;Publish();return true;
        }
        private void InvalidateFund() {
            if(fundLease==null)return;fundLease.Dispose();var h=FundInvalidated;
            if(h!=null)foreach(Action subscriber in h.GetInvocationList())try{subscriber();}catch(Exception){}
        }
        public event Action ShoppingInvalidated;
        private ShoppingLease shoppingLease;
        private bool invalidatingShopping;
        private sealed class ShoppingLease : IDisposable {
            private Phase5CollectionHost owner;
            internal readonly string CardId;
            internal readonly bool FromBinder;
            internal readonly int InspectIndex;
            internal readonly string TransactionId;
            internal ShoppingLease(Phase5CollectionHost owner,string id,bool source) {this.owner=owner;CardId=id;FromBinder=source;InspectIndex=owner.Game.CurrentView.inspectIndex;TransactionId=owner.Game.CurrentView.transactionId;}
            public void Dispose() {var host=owner;if(ReferenceEquals(host,null)) return;owner=null;host.EndShopping(this);}
        }
        public bool CanUseShopping(string cardId,bool fromBinder) {
            CaptureDraft();
            if(fundLease!=null || !ShoppingEnabled || closed || !IsReady || !StartupLoadingCoordinator.IsReady || Game==null ||
                !Game.IsAcceptingInput || Game.HasPendingStorage || Game.CurrentSession==null || Physical==null || registry==null ||
                pendingEdit!=null || pendingConfirm || reviewed!=null || restartRequired || !string.IsNullOrEmpty(error) ||
                !string.IsNullOrEmpty(openingReconciliationError) || DirtyDraft() || string.IsNullOrEmpty(cardId)) return false;
            if(shoppingLease!=null && (shoppingLease.CardId!=cardId || shoppingLease.FromBinder!=fromBinder)) return false;
            CardPrinting card;
            try {card=registry.FindCard(cardId);if(!registry.IsCompletionEligible(cardId)) return false;}
            catch(System.Collections.Generic.KeyNotFoundException) {return false;}
            foreach(var holding in Physical.Holdings) if(holding.CardId==cardId && holding.Quantity>0) return false;
            var current=Game.CurrentView;
            if(current==null || current.paused || current.storageBusy || !string.IsNullOrEmpty(current.errorMessage) || current.stage=="error") return false;
            if(fromBinder) {
                if(!open || mode!="physical" || selectedId!=cardId || binderPrintingId!=card.PrintingId) return false;
                // Reproject current authoritative snapshots; the mutable DTO and prior projection are not authority.
                var latest=BinderProjection.Build(registry,binderPrintingId,Game.CurrentSession,Physical,mode,filter,search);
                foreach(var row in latest.Rows) if(row.CardId==cardId) return true;
                return false;
            }
            int i=current.inspectIndex;
            var pack=Game.CurrentSession.CurrentPack;
            return !open && PresentationRules.CanInspect(current,i) && current.cards[i].id==cardId &&
                pack!=null && pack.WrapperOpened && i<pack.RevealedCount && i<pack.OrderedCardIds.Count &&
                pack.OrderedCardIds[i]==cardId && pack.TransactionId==current.transactionId &&
                (shoppingLease==null || (shoppingLease.InspectIndex==i && shoppingLease.TransactionId==current.transactionId));
        }
        public bool TryBeginShopping(string cardId,bool fromBinder,out IDisposable overlayLease) {
            overlayLease=null;
            if(shoppingLease!=null || invalidatingShopping || !CanUseShopping(cardId,fromBinder)) return false;
            var lease=new ShoppingLease(this,cardId,fromBinder);shoppingLease=lease;overlayLease=lease;
            Publish();return true;
        }
        private void EndShopping(ShoppingLease lease) {
            if(!ReferenceEquals(shoppingLease,lease)) return;
            shoppingLease=null;
            if(!closed && IsReady) Publish();
            else {if(binder!=null) {binder.ExternalOverlayVisible=false;binder.ShoppingAvailable=false;}
                if(table!=null) {table.ShoppingAvailable=false;table.CollectionOverlayVisible=false;}}
        }
        private void InvalidateShopping() {
            if(shoppingLease==null || invalidatingShopping) return;
            invalidatingShopping=true;
            try {
                shoppingLease.Dispose();
                var handler=ShoppingInvalidated;
                if(handler!=null) foreach(Action subscriber in handler.GetInvocationList()) {try {subscriber();} catch(Exception) {/* One observer cannot prevent owner release. */}}
            } finally {invalidatingShopping=false;}
        }
        private void RefreshShopping() {
            if(shoppingLease!=null && !CanUseShopping(shoppingLease.CardId,shoppingLease.FromBinder)) InvalidateShopping();
            if(binder!=null) binder.ShoppingAvailable=shoppingLease==null && CanUseShopping(selectedId,true);
            var current=Game==null?null:Game.CurrentView;
            string id=current!=null && PresentationRules.CanInspect(current,current.inspectIndex)?current.cards[current.inspectIndex].id:null;
            if(table!=null) table.ShoppingAvailable=shoppingLease==null && CanUseShopping(id,false);
        }
        private void Start() { Initialize(Environment.GetCommandLineArgs()); }
        private void Update() {
            if (!IsReady || closed) return;
            // View assignment acknowledges a UI request, so never assign an unchanged DTO every frame.
            if (!ReferenceEquals(observedView, Game.CurrentView)) { CaptureDraft(); Publish(); }
            RefreshShopping();
            if(fundLease!=null && !CanUseFund()) InvalidateFund();
            if(binder!=null)binder.FundAvailable=fundLease==null && CanUseFund();
        }
        public void Initialize(string[] args) {
            if (initialized || closed) return;
            initialized=true;
            Game=GetComponent<OpeningGame>();
            if (Game==null) Game=gameObject.AddComponent<OpeningGame>();
            Game.CollectionCommand+=DispatchBinderCommand;
            try {
                registry=Game.PreparePhase5Configuration();
                if(registry==null) throw new InvalidOperationException("Phase5 requires three approved sets");
                var paths=GamePaths.ResolvePhase5(Game.allowedProjectRoot,Application.dataPath,Application.isEditor,args);
                Profiles=new CollectionProfiles(paths.BaseSaveDirectory,paths.IsSmokeTest?importFault:null);
                Game.PhysicalTrackingConfigured=true;
                Game.InitializeForProfile(args,Profiles.ActiveProfileId);
                if (Game.CurrentSession==null || Game.HasPendingStorage) throw new InvalidOperationException();
                if (!string.Equals(Game.Paths.SaveDirectory,Profiles.ActiveDirectory,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
                binderPrintingId=Game.SelectedPrintingId;
                physicalStore=new PhysicalStore(Game.Paths.SaveDirectory,physicalSerializer,registry,paths.IsSmokeTest?physicalFault:null);
                Physical=physicalStore.Snapshot;
                binder=GetComponent<BinderPresentation>();
                if (binder==null) binder=gameObject.AddComponent<BinderPresentation>();
                binder.Command+=DispatchBinderCommand;
                table=GetComponent<TabletopPresentation>();
                table.CollectionAvailable=true;
                RefreshBackups();
                Game.ShuttingDown+=OnGameShuttingDown;
                Game.ShutdownCompleted+=OnGameShutdownCompleted;
                Game.ViewPublished+=OnOpeningViewPublished;
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
        private string StoredQuantity() {
            if (Physical!=null && selectedId!=null) foreach(var h in Physical.Holdings)
                if(h.CardId==selectedId && h.Condition==condition) return h.Quantity.ToString(CultureInfo.InvariantCulture);
            return "0";
        }
        private void CaptureDraft() {
            if(pendingEdit==null && !pendingConfirm && open && mode=="physical" && selectedId!=null && binder!=null && CurrentBinderView!=null) quantity=binder.QuantityText;
        }
        private bool DirtyDraft() {
            if(mode!="physical" || selectedId==null) return false;
            // Capture UI text once at command/update boundaries. Permission and DTO publication
            // must use that same effective draft, never the previous UI projection.
            return !string.Equals(quantity,StoredQuantity(),StringComparison.Ordinal);
        }
        private void Refuse(string message) {StatusMessage=message;Publish();}
        private void LoadQuantity() {
            quantity="0";
            if (Physical!=null && selectedId!=null) foreach(var h in Physical.Holdings)
                if(h.CardId==selectedId && h.Condition==condition) quantity=h.Quantity.ToString(CultureInfo.InvariantCulture);
        }
        public void DispatchBinderCommand(string command) {
            // Quit is intent only, including callbacks after this host closed its own input.
            if (command=="quit") { if(Game!=null) Game.RequestQuit(); return; }
            if (closed || !IsReady || shoppingLease!=null || fundLease!=null || string.IsNullOrEmpty(command)) return;
            CaptureDraft();
            if (command=="binder-open" || command=="binder-close") {
                open=command=="binder-open"; Publish(); return;
            }
            if (!open || restartRequired) {Refuse("Reopen Binder or Quit / restart before changing the collection.");return;}
            // Frozen intent survives Back/reopen. No new edit/review/export/cancel can replace uncertain work.
            if (pendingEdit!=null && command!="save-physical") {Refuse("Retry the SAME ownership edit before changing this collection.");return;}
            if (pendingConfirm && command!="import-confirm") {Refuse("Retry the SAME reviewed restore before changing this collection.");return;}
            if (reviewed!=null && command!="import-confirm" && command!="import-cancel") {Refuse("Confirm or cancel this retained review before browsing another set.");return;}
            if(DirtyDraft() && (command.StartsWith("mode:",StringComparison.Ordinal) || command.StartsWith("select:",StringComparison.Ordinal) || command.StartsWith("condition:",StringComparison.Ordinal) || command.StartsWith("filter:",StringComparison.Ordinal) || command.StartsWith("search:",StringComparison.Ordinal))) {
                Refuse("Save the ownership draft or restore its stored quantity before navigating away from this bucket.");return;
            }
            if (!ReferenceEquals(observedView,Game.CurrentView)) Publish();
            try {
                if(command.StartsWith("binder-set:",StringComparison.Ordinal)) {
                    if(!Acknowledged() || pendingConfirm || reviewed!=null || !string.IsNullOrEmpty(error) || DirtyDraft()) {
                        Refuse("Set browse blocked. Save the ownership draft (or restore its stored quantity), resolve errors or finish the retained review first.");return;
                    }
                    string id=command.Substring(11);
                    bool known=false; foreach(var set in registry.Sets) if(set.PrintingId==id) known=true;
                    if(!known) {Refuse("Choose an exact supported set ID.");return;}
                    if(id!=binderPrintingId) {binderPrintingId=id;selectedId=null;condition="unknown";quantity="0";page=0;search="";filter="all";}
                    StatusMessage="Binder scope changed. The active pack and next-pack selection are unchanged.";
                } else if (command=="mode:virtual" || command=="mode:physical") {
                    mode=command.Substring(5); selectedId=null; page=0; error=null;
                } else if(command.StartsWith("filter:",StringComparison.Ordinal) && KnownFilter(command.Substring(7))) {
                    filter=command.Substring(7); page=0; error=null;
                } else if(command.StartsWith("search:",StringComparison.Ordinal) && command.Length<=263) {
                    search=command.Substring(7); page=0; error=null;
                } else if(command.StartsWith("page:",StringComparison.Ordinal)) {
                    int n;if(!Index(command.Substring(5),Math.Max(1,(projection.Rows.Count+11)/12),out n)) {Refuse("Choose an available binder page.");return;}page=n;
                } else if(command.StartsWith("select:",StringComparison.Ordinal)) {
                    string id=command.Substring(7); bool found=false;
                    foreach(var row in projection.Rows) if(row.CardId==id) found=true;
                    if(!found) {Refuse("Select a printing in the current binder scope.");return;}selectedId=id;LoadQuantity();error=null;
                } else if(command.StartsWith("condition:",StringComparison.Ordinal)) {
                    if(mode!="physical" || !Selected() || !KnownCondition(command.Substring(10))) {Refuse("Select a physical printing and a supported condition first.");return;}
                    condition=command.Substring(10);LoadQuantity();error=null;
                } else if(command.StartsWith("quantity:",StringComparison.Ordinal)) {
                    if(mode!="physical" || !Selected() || command.Length>41) {Refuse("Select a physical printing before entering quantity.");return;}
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
                    pendingEdit=null;LoadQuantity();error=null;StatusMessage="Physical ownership acknowledged. No virtual holdings changed.";
                } else if(command=="backup") {
                    if(!Acknowledged() || !string.IsNullOrEmpty(error) || reviewed!=null) {
                        StatusMessage="Backup not started. Resolve the opening save or collection error before exporting.";Publish();return;
                    }
                    // The first export must not mistake deleted acknowledged default-store
                    // files for a never-used blank profile. Cached snapshots are not disk proof.
                    if(!Game.ReconcileForCollectionExport()) throw new InvalidOperationException();
                    Physical=physicalStore.Reload();
                    Profiles.CreateBackup(Game.Paths.SaveDirectory,sessionSerializer,physicalSerializer,registry);
                    RefreshBackups();error=null;StatusMessage="Validated collection backup created explicitly.";
                } else if(command.StartsWith("backup-select:",StringComparison.Ordinal)) {
                    int n;if(!Index(command.Substring(14),backupPaths.Length,out n)) {Refuse("Select an available backup in this collection.");return;}backupIndex=n;error=null;
                } else if(command=="import-preview") {
                    if(!Acknowledged() || backupIndex<0 || backupIndex>=backupPaths.Length) {
                        StatusMessage="Preview not started. Resolve the opening save and select an available backup.";Publish();return;
                    }
                    var preview=Profiles.PreviewImport(backupPaths[backupIndex],sessionSerializer,physicalSerializer,registry);
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
                } else {Refuse("Unsupported collection command. Choose a current Binder action.");return;}
            } catch(Exception) {
                error=pendingEdit!=null?"Physical save acknowledgement uncertain. Retry the SAME ownership edit; draft changes and export/import are locked.":
                    pendingConfirm?"Restore acknowledgement uncertain. Retry the SAME reviewed restore; all state changes remain locked.":
                    "Collection operation failed validation or storage access. Original data and drafts retained. Retry explicitly.";
                if(command=="backup" && Game.HasPendingStorage) openingReconciliationError=error;
            }
            Publish();
        }
        private string BinderLabel() {foreach(var choice in Game.SetChoices()) if(choice.printingId==binderPrintingId) return choice.label;return binderPrintingId;}
        private bool Acknowledged() { return pendingEdit==null && !Game.HasPendingStorage && Game.CurrentSession!=null && Physical!=null; }
        private void OnOpeningViewPublished(OpeningView view) {
            // A successful explicit Retry publication is acknowledgement, not a UI refresh.
            if(openingReconciliationError==null || Game.HasPendingStorage || Game.LastCommand!="retry") return;
            openingReconciliationError=null;error=null;
            StatusMessage="Opening storage recovered. Request Backup again explicitly to export.";
            CaptureDraft();Publish();
        }
        private void Publish() {
            if(!IsReady || closed) return;
            if(openingReconciliationError!=null && pendingEdit==null && !pendingConfirm) error=openingReconciliationError;
            observedView=Game.CurrentView;
            projection=BinderProjection.Build(registry,binderPrintingId,Game.CurrentSession,Physical,mode,filter,search);
            if(!Selected()) {selectedId=null;quantity="0";}
            page=Math.Max(0,Math.Min(page,Math.Max(0,(projection.Rows.Count-1)/12)));
            var rows=new BinderCardView[projection.Rows.Count];
            for(int i=0;i<rows.Length;i++) {
                var r=projection.Rows[i];rows[i]=new BinderCardView {id=r.CardId,name=r.Name,collectorNumber=r.CollectorNumber,rarity=r.Rarity,finish=r.Finish,kind=r.Kind,quantity=r.Quantity,owned=r.Owned};
            }
            var backups=new string[backupPaths.Length];for(int i=0;i<backups.Length;i++) backups[i]=Path.GetFileName(backupPaths[i]);
            CurrentBinderView=new BinderView {open=open,busy=false,printingId=binderPrintingId,setLabel=BinderLabel(),supportedSets=Game.SetChoices(),canSelectSet=Acknowledged() && !pendingConfirm && reviewed==null && !restartRequired && string.IsNullOrEmpty(error) && !DirtyDraft(),mode=mode,filter=filter,search=search,page=page,rows=rows,
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
            // An opening-only reconciliation error must leave its advertised Retry reachable.
            // Opening error policy still denies generation/selection; every independent host lock remains.
            bool blocked=open || fundLease!=null || shoppingLease!=null || pendingEdit!=null || pendingConfirm || reviewed!=null || restartRequired || (!string.IsNullOrEmpty(error) && openingReconciliationError==null) || DirtyDraft();
            Game.SetCollectionGate(blocked);
            table.CollectionInputBlocked=blocked;
            table.CollectionOverlayVisible=open || fundLease!=null || shoppingLease!=null;
            binder.ExternalOverlayVisible=fundLease!=null || shoppingLease!=null;
            binder.FundAvailable=fundLease==null && CanUseFund();
            RefreshShopping();
        }
        // Explicit isolated smoke mode only; production callers cannot arm faults.
        public void FailNextPhysicalCommitForSmoke() { RequireSmoke();physicalFault.Stage="after-prepare"; }
        public void FailNextImportForSmoke() { RequireSmoke();importFault.Stage="before-import-ack"; }
        private void RequireSmoke() { if(!IsReady || Game.Paths==null || !Game.Paths.IsSmokeTest) throw new InvalidOperationException("Explicit isolated smoke mode required"); }
        private void ReleaseStores() {
            var failures=new System.Collections.Generic.List<Exception>();
            try {ReleasePhysical();} catch(Exception error) {failures.Add(error);}
            try {if(Game!=null) Game.Shutdown();} catch(Exception error) {failures.Add(error);}
            // A host reentered from an earlier Game subscriber must wait for session release.
            try {if(Game==null || !Game.IsShuttingDown) ReleaseProfiles();} catch(Exception error) {failures.Add(error);}
            if(failures.Count>0) throw new AggregateException("Collection cleanup failed; all owners were attempted.",failures);
        }
        private void ReleasePhysical() {if(physicalStore!=null) {var owned=physicalStore;physicalStore=null;owned.Dispose();}}
        private void ReleaseProfiles() {if(Profiles!=null) {var owned=Profiles;Profiles=null;owned.Dispose();}}
        private void OnGameShuttingDown() {
            if(closed) return;closed=true;IsReady=false;InvalidateFund();InvalidateShopping();Detach();ReleasePhysical();
        }
        private void OnGameShutdownCompleted() {
            try {ReleaseProfiles();} finally {if(Game!=null) Game.ShutdownCompleted-=OnGameShutdownCompleted;}
        }
        private void Detach() {
            if(binder!=null) binder.Command-=DispatchBinderCommand;
            if(table!=null) {table.PhysicalOwnershipReader=null;table.CollectionAvailable=false;}
            if(Game!=null) {Game.CollectionCommand-=DispatchBinderCommand;Game.ShuttingDown-=OnGameShuttingDown;Game.ViewPublished-=OnOpeningViewPublished;}
        }
        public void Shutdown() {
            if(closed) return;closed=true;IsReady=false;InvalidateFund();InvalidateShopping();
            Detach();
            try {ReleaseStores();} finally {if(Game!=null && !Game.IsShuttingDown) Game.ShutdownCompleted-=OnGameShutdownCompleted;}
        }
        private void OnDestroy() { Shutdown(); }
        private void OnApplicationQuit() { Shutdown(); }
    }
}
