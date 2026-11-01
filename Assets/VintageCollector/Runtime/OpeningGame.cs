using System;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using UnityEngine;
using VintageCollector.Domain;
using VintageCollector.Persistence;
using VintageCollector.Presentation;

namespace VintageCollector.Runtime
{
    public sealed class OpeningGame : MonoBehaviour
    {
        public TextAsset catalogJson, modelJson;
        public bool backgroundOpeningStorage = false;
        public bool IsStorageBusy { get; private set; }
        public bool IsAcceptingInput { get { return !closed && !quitRequested && !terminalInitialization; } }
        private SerialOpeningStorage storageWorker;
        public Phase5SetConfig[] phase5Sets;
        public ApprovedSetRegistry ApprovedSets { get; private set; }
        public string SelectedPrintingId { get; private set; }
        public event Action<OpeningView> ViewPublished;
        private Phase5SetConfig[] phase5ConfigSnapshot = null;
        private System.Collections.Generic.Dictionary<string,PackGenerator> approvedGenerators = null;
        public void SetCollectionGate(bool blocked) {
            if (collectionGate == blocked) return;
            collectionGate = blocked;
            if (ApprovedSets != null && CurrentView != null && !closed) SetView(CopyView(CurrentView));
        }
        public bool TrySelectSet(string printingId) {
            if (ApprovedSets == null || string.IsNullOrEmpty(printingId) || !CanSelectSet()) return false;
            try { ApprovedSets.FindSet(printingId); }
            catch (System.Collections.Generic.KeyNotFoundException) { return false; }
            catch (ArgumentException) { return false; }
            if (SelectedPrintingId == printingId) return true;
            SelectedPrintingId = printingId;
            PublishView();
            return true;
        }
        private string OpeningStage() {
            if (lastError != null) return "error";
            var pack = CurrentSession == null ? null : CurrentSession.CurrentPack;
            return pack == null ? "ready" : pack.IsComplete && finalTableTransaction != pack.TransactionId ? "results" : pack.WrapperOpened ? "revealing" : "sealed";
        }
        private bool CanSelectSet() {
            return !quitRequested && store != null && CurrentSession != null && Phase5SelectionPolicy.CanSelectOpening(OpeningStage(),paused,inspectIndex>=0,pending!=null || IsStorageBusy,collectionGate,terminalInitialization,closed,lastError!=null);
        }
        public SetChoiceView[] SetChoices() {
            if (ApprovedSets == null) return new SetChoiceView[0];
            var choices = new SetChoiceView[ApprovedSets.Sets.Count];
            for (int i=0;i<choices.Length;i++) {
                string id=ApprovedSets.Sets[i].PrintingId;
                choices[i]=new SetChoiceView {printingId=id,label=SetLabel(id)};
            }
            return choices;
        }
        private string SetLabel(string id) {
            if (phase5ConfigSnapshot != null) foreach (var config in phase5ConfigSnapshot) if (config.printingId==id) return config.displayLabel;
            return id;
        }
        private static OpeningView CopyView(OpeningView v) {
            return new OpeningView {storageBusy=v.storageBusy,stage=v.stage,transactionId=v.transactionId,errorMessage=v.errorMessage,recoveryNotice=v.recoveryNotice,modelLabel=v.modelLabel,statusLine=v.statusLine,saveLocation=v.saveLocation,revealedCount=v.revealedCount,totalVirtualCards=v.totalVirtualCards,cards=v.cards==null?null:(OpeningCardView[])v.cards.Clone()};
        }
        public string allowedProjectRoot;
        public GamePaths Paths { get; private set; }
        public OpeningView CurrentView { get; private set; }
        // Last acknowledged snapshot only; not authoritative while CurrentView.stage == "error".
        public SessionSnapshot CurrentSession { get; private set; }
        public bool PhysicalTrackingConfigured { get; set; }
        public int GeneratedResultCount { get; private set; }
        public string LastCommand { get; private set; }

        public event Action<string> CollectionCommand;
        public event Action ShuttingDown;
        public event Action ShutdownCompleted;
        public bool HasPendingStorage { get { return IsStorageBusy || pending != null || lastError != null; } }
        private bool collectionGate, terminalInitialization;
        private string initialProfileId = "";
        public void BlockInitialization(string error) {
            if (initialized && store != null) Shutdown();
            initialized = true; terminalInitialization = true;
            presentation = GetComponent<TabletopPresentation>();
            if (presentation == null) presentation = gameObject.AddComponent<TabletopPresentation>();
            closed = false;
            presentation.Command -= DispatchCommand;
            presentation.Command += DispatchCommand;
            PublishError(new InvalidOperationException(error));
        }
        private bool initialized;
        private Catalog catalog;
        private PackGenerator generator;
        private SessionStore store;
        private TabletopPresentation presentation;
        private bool closed;
        private OpeningStorageRequest pending;
        private SmokeFaults smokeFaults;
        private string initialRoot, catalogSource, modelSource;
        private string[] initialArgs;
        private bool paused;
        private float volume = 0.5f;
        private int inspectIndex = -1;
        private string finalTableTransaction;
        private Exception lastError;

        private sealed class SmokeFaults : ISaveFaultInjector
        {
            public volatile bool armed;
            public void At(string stage)
            {
                if (armed && stage == "after-prepare") {
                    armed = false;
                    throw new IOException("Explicit smoke fault after durable preparation; Retry preserves this exact action.");
                }
            }
        }

        private void Start()
        {
            if (!initialized) Initialize(Environment.GetCommandLineArgs());
        }

        private bool phase5ConfigurationAttempted;
        private Exception phase5ConfigurationFailure;
        public ApprovedSetRegistry PreparePhase5Configuration() {
            if(ApprovedSets!=null) return ApprovedSets;
            if(phase5ConfigurationFailure!=null) throw new InvalidOperationException("Phase5 configuration was rejected. Correct the scene and restart; no legacy fallback is allowed.",phase5ConfigurationFailure);
            if(phase5ConfigurationAttempted) return null;
            phase5ConfigurationAttempted=true;
            try {return PreparePhase5ConfigurationCore();}
            catch(Exception error) {phase5ConfigurationFailure=error;throw;}
        }
        private ApprovedSetRegistry PreparePhase5ConfigurationCore() {
            if (phase5Sets == null || phase5Sets.Length == 0) return null;
            if (phase5Sets.Length != 3) throw new ArgumentException("Exactly three approved Phase5 configurations are required");
            var configs=new Phase5SetConfig[3];
            var catalogs=new Catalog[3]; var models=new RuleData[3];
            var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var catalogBytes=new byte[3][]; var modelBytes=new byte[3][];
            // Capture every mutable scene field and source before validation or any storage activity.
            for(int i=0;i<configs.Length;i++) {
                var c=phase5Sets[i];
                if(c==null || c.catalogJson==null || c.modelJson==null || string.IsNullOrEmpty(c.printingId) || !ids.Add(c.printingId)) throw new ArgumentException("Missing or duplicate Phase5 configuration");
                configs[i]=CopyConfig(c);
                catalogBytes[i]=c.catalogJson.bytes; modelBytes[i]=c.modelJson.bytes;
            }
            if(!ids.Contains(Phase5InputPolicy.BasePrintingId) || !ids.Contains(Phase5InputPolicy.JunglePrintingId) || !ids.Contains(Phase5InputPolicy.FossilPrintingId)) throw new ArgumentException("Unknown Phase5 configuration");
            // All raw source pins precede any deserialization.
            var cs=new string[3]; var ms=new string[3];
            for(int i=0;i<3;i++) {
                cs[i]=Phase5InputPolicy.VerifySource(configs[i].printingId,false,catalogBytes[i]);
                ms[i]=Phase5InputPolicy.VerifySource(configs[i].printingId,true,modelBytes[i]);
            }
            for(int i=0;i<3;i++) {
                catalogs[i]=Catalog.FromData(JsonUtility.FromJson<CatalogData>(cs[i]));
                models[i]=JsonUtility.FromJson<RuleData>(ms[i]);
                if(catalogs[i].PrintingId!=configs[i].printingId || models[i].printingId!=configs[i].printingId) throw new ArgumentException("Phase5 source identity mismatch");
            }
            var registry=ApprovedSetRegistry.FromApprovedData(catalogs,models);
            var generators=new System.Collections.Generic.Dictionary<string,PackGenerator>(StringComparer.Ordinal);
            foreach(var set in registry.Sets) generators.Add(set.PrintingId,registry.CreateGenerator(set.PrintingId));
            phase5ConfigSnapshot=configs; approvedGenerators=generators;
            SelectedPrintingId=Phase5InputPolicy.BasePrintingId;
            ApprovedSets=registry;
            return registry;
        }
        private static Phase5SetConfig CopyConfig(Phase5SetConfig c) {
            return new Phase5SetConfig {printingId=c.printingId,displayLabel=string.IsNullOrEmpty(c.displayLabel)?c.printingId:c.displayLabel,catalogJson=c.catalogJson,modelJson=c.modelJson,revealAudioProfile=c.revealAudioProfile,cardFaceTemplate=c.cardFaceTemplate,wrapperFronts=c.wrapperFronts==null?null:(Texture2D[])c.wrapperFronts.Clone(),cardBack=c.cardBack,wrapperBack=c.wrapperBack};
        }
        public Phase5SetConfig[] GetPhase5PresentationConfiguration() {
            if(ApprovedSets==null || phase5ConfigSnapshot==null) throw new InvalidOperationException("Phase5 configuration has not been admitted");
            var copy=new Phase5SetConfig[phase5ConfigSnapshot.Length];
            for(int i=0;i<copy.Length;i++) copy[i]=CopyConfig(phase5ConfigSnapshot[i]);
            return copy;
        }
        public void Initialize(string[] args) { InitializeForProfile(args, ""); }

        public void InitializeForProfile(string[] args, string profileId)
        {
            if (initialized) return;
            initialProfileId = profileId;
            initialized = true;
            presentation = GetComponent<TabletopPresentation>();
            if (presentation == null) presentation = gameObject.AddComponent<TabletopPresentation>();
            presentation.Command += DispatchCommand;
            initialRoot = allowedProjectRoot;
            initialArgs = args == null ? null : (string[])args.Clone();
            catalogSource = catalogJson == null ? null : catalogJson.text;
            modelSource = modelJson == null ? null : modelJson.text;
            RunStorage(delegate {
                PreparePhase5Configuration();
                OpenSession();
                CurrentSession = store.Snapshot;
                if(ApprovedSets!=null) SelectedPrintingId=CurrentSession.CurrentPack==null?Phase5InputPolicy.BasePrintingId:CurrentSession.CurrentPack.PrintingId;
                PublishView();
            });
        }

        private void OpenSession()
        {
            PreparePhase5Configuration();
            if (Paths == null) Paths = (ApprovedSets==null
                ? GamePaths.Resolve(initialRoot, Application.dataPath, Application.isEditor, initialArgs)
                : GamePaths.ResolvePhase5(initialRoot, Application.dataPath, Application.isEditor, initialArgs)).WithProfile(initialProfileId);
            if(ApprovedSets==null) {
                if (catalogSource == null || modelSource == null) throw new ArgumentException("Catalog and model TextAssets must be configured");
                if (catalog == null) catalog = Catalog.FromData(JsonUtility.FromJson<CatalogData>(catalogSource));
                if (generator == null) generator = new PackGenerator(catalog, JsonUtility.FromJson<RuleData>(modelSource));
            }
            if (Paths.IsSmokeTest && smokeFaults == null) smokeFaults = new SmokeFaults();
            if (store == null) store = ApprovedSets==null ? new SessionStore(Paths.SaveDirectory, new UnityStateSerializer(), catalog, smokeFaults) : new SessionStore(Paths.SaveDirectory, new UnityStateSerializer(), ApprovedSets, smokeFaults);
        }

        private void RunStorage(Action action)
        {
            // On Unity's Mono InvalidDataException is NOT an IOException.
            try { action(); }
            catch (InvalidDataException e) { PublishError(e); }
            catch (IOException e) { PublishError(e); }
            catch (UnauthorizedAccessException e) { PublishError(e); }
            catch (System.Security.SecurityException e) { PublishError(e); }
            catch (ArgumentException e) { PublishError(e); }
            catch (InvalidOperationException e) { PublishError(e); }
        }

        public void DispatchCommand(string command)
        {
            // Closing gameplay input must not discard reentrant process-exit intent.
            if (closed && command == "quit") { RequestQuit(); return; }
            if (closed || string.IsNullOrEmpty(command)) return;
            if (quitRequested && command != "quit") return;
            if(command.StartsWith("set:",StringComparison.Ordinal)) { TrySelectSet(command.Substring(4)); return; }
            if (command == "binder-open" || command == "binder-close") {
                var handler = CollectionCommand; if (handler != null) handler(command);
                return;
            }
            if ((collectionGate || terminalInitialization) && command != "quit") return;
            if (!PresentationRules.CanEmit(CurrentView, command)) return;
            if (command == "quit") {
                RequestQuit();
                return;
            }
            if (command == "results") {
                LastCommand = command;
                finalTableTransaction = null;
                PublishView();
                return;
            }
            if (command == "pause" || command == "resume" || command == "close-inspect" ||
                command.StartsWith("inspect:",StringComparison.Ordinal) || command.StartsWith("volume:",StringComparison.Ordinal)) {
                LastCommand = command;
                if (command == "pause") paused = true;
                else if (command == "resume") paused = false;
                else if (command == "close-inspect") inspectIndex = -1;
                else if (command.StartsWith("inspect:",StringComparison.Ordinal))
                    inspectIndex = int.Parse(command.Substring(8),NumberStyles.None,CultureInfo.InvariantCulture);
                else volume = float.Parse(command.Substring(7),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture);
                if (lastError != null) PublishError(lastError); else PublishView();
                return;
            }
            if (command == "retry") {
                LastCommand = command;
                if (backgroundOpeningStorage && store != null) { QueueStorage(pending ?? new OpeningStorageRequest("reload"),true); return; }
                RunStorage(delegate {
                    OpenSession();
                    store.Reload(); // Recovery may throw too; never expose its candidate early.
                    if (pending != null) ExecutePending();
                    else {
                        bool firstAcknowledgement=CurrentSession==null;
                        CurrentSession = store.Snapshot;
                        if(firstAcknowledgement && ApprovedSets!=null) SelectedPrintingId=CurrentSession.CurrentPack==null?Phase5InputPolicy.BasePrintingId:CurrentSession.CurrentPack.PrintingId;
                        PublishView();
                    }
                });
                return;
            }
            if (pending != null) return;
            if (command == "generate") { RunStorage(delegate { Generate(CryptoSeed()); }); return; }
            if (command != "open" && command != "next" && command != "skip") return;
            var pack = CurrentSession.CurrentPack;
            LastCommand = command;
            pending = new OpeningStorageRequest(command,pack.TransactionId,target:command == "skip" ? pack.OrderedCardIds.Count : pack.RevealedCount+1);
            RunStorage(ExecutePending);
        }

        public void GenerateForSmoke(uint seed)
        {
            if (Paths == null || !Paths.IsSmokeTest) throw new InvalidOperationException("Explicit smoke mode required");
            if (seed == 0) throw new ArgumentOutOfRangeException("seed", "Seed must be nonzero");
            if (closed || quitRequested || collectionGate || terminalInitialization || pending != null || !PresentationRules.CanEmit(CurrentView,"generate")) return;
            RunStorage(delegate { Generate(seed); });
        }

        // Test-only deterministic transaction seam. No normal command, picker or persisted field.
        public void GenerateForSmoke(uint seed,string transactionId)
        {
            if (Paths == null || !Paths.IsSmokeTest) throw new InvalidOperationException("Explicit smoke mode required");
            if (seed == 0) throw new ArgumentOutOfRangeException("seed", "Seed must be nonzero");
            // Mirror the existing schema1 transaction identity exactly, without normalization.
            if(string.IsNullOrWhiteSpace(transactionId) || transactionId.Length>256 || transactionId.Trim()!=transactionId) throw new ArgumentException("Invalid transactionId");
            foreach(char c in transactionId) if(char.IsControl(c)) throw new ArgumentException("Control in transactionId");
            if (closed || quitRequested || collectionGate || terminalInitialization || pending != null || lastError != null || !PresentationRules.CanEmit(CurrentView,"generate")) return;
            foreach(var pack in CurrentSession.Packs) if(string.Equals(pack.TransactionId,transactionId,StringComparison.Ordinal)) throw new ArgumentException("Smoke transaction identity already exists");
            RunStorage(delegate { Generate(seed,transactionId); });
        }

        // Explicit export barrier only: reconcile the actual leased store against its live
        // acknowledgement. This never executes pending work, generates, reveals or cues audio.
        public bool ReconcileForCollectionExport()
        {
            if (closed || quitRequested || terminalInitialization || store == null || IsStorageBusy || pending != null || lastError != null) return false;
            bool acknowledged = false;
            RunStorage(delegate {
                store.Reload();
                CurrentSession = store.Snapshot;
                PublishView();
                acknowledged = true;
            });
            return acknowledged;
        }

        public void FailNextCommitForSmoke()
        {
            if (closed || IsStorageBusy || Paths == null || !Paths.IsSmokeTest || smokeFaults == null)
                throw new InvalidOperationException("An initialized explicit smoke session is required");
            smokeFaults.armed = true;
        }

        private static uint CryptoSeed()
        {
            using (var random = RandomNumberGenerator.Create()) {
                var bytes = new byte[4]; uint seed;
                do { random.GetBytes(bytes); seed = BitConverter.ToUInt32(bytes,0); } while (seed == 0);
                return seed;
            }
        }

        private void Generate(uint seed) { Generate(seed,Guid.NewGuid().ToString("N")); }
        private void Generate(uint seed,string transactionId)
        {
            LastCommand = "generate";
            var createdUtc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture);
            GeneratedResultCount++;
            var result=(ApprovedSets==null ? generator : approvedGenerators[SelectedPrintingId]).Generate(new SeededRandom(seed));
            var action=new OpeningStorageRequest("generate",transactionId,createdUtc,seed,result);
            // Freeze the result and every call argument before entering the durable service.
            pending = action;
            ExecutePending();
        }

        private void ExecutePending()
        {
            if(backgroundOpeningStorage) { QueueStorage(pending,false); return; }
            var action = pending;
            var acknowledged=action.Execute(store,false);
            Acknowledge(action,acknowledged);
        }

        private void QueueStorage(OpeningStorageRequest action,bool reload)
        {
            if(IsStorageBusy) return;
            if(storageWorker==null) storageWorker=new SerialOpeningStorage(store);
            IsStorageBusy=true;
            try {storageWorker.Submit(action,reload);}
            catch {IsStorageBusy=false;throw;}
            SetView(CopyView(CurrentView));
        }
        private void Update()
        {
            if(closed || !IsStorageBusy) return;
            SerialOpeningStorage.Completion answer;
            if(!storageWorker.TryTake(out answer)) return;
            IsStorageBusy=false;
            Exception completionFailure=null;
            try {
                if(answer.Error!=null) PublishError(answer.Error);
                else if(answer.Request.command=="reload") {CurrentSession=answer.Snapshot;PublishView();}
                else Acknowledge(answer.Request,answer.Snapshot);
            }
            catch(Exception error) {completionFailure=error;}
            try {if(quitRequested && !closed) Shutdown();}
            catch(Exception cleanupError) {
                if(completionFailure!=null) throw new AggregateException(completionFailure,cleanupError);
                throw;
            }
            if(completionFailure!=null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(completionFailure).Throw();
        }
        private void Acknowledge(OpeningStorageRequest action,SessionSnapshot acknowledged)
        {
            CurrentSession = acknowledged;
            pending = null;
            // Keep the final ordinary reveal on the same table as every earlier card.
            // This is presentation-only: the eleven-card transaction is already durable.
            if (action.command == "next" && acknowledged.CurrentPack != null && acknowledged.CurrentPack.IsComplete)
                finalTableTransaction = acknowledged.CurrentPack.TransactionId;
            else if (action.command == "generate" || action.command == "skip")
                finalTableTransaction = null;
            PublishView();
            if(!backgroundOpeningStorage || !closed) presentation.NotifyCommitted(action.command);
        }

        private void PublishError(Exception error)
        {
            // Never query the uncertain store here: Snapshot itself may require failing recovery.
            lastError = error;
            SetView(new OpeningView {
                stage="error", errorMessage=terminalInitialization?error.Message:PhysicalTrackingConfigured?"Opening storage is blocked. Retry the same pending action or Quit; original data is retained.":error.Message,
                transactionId=pending != null ? pending.transactionId : CurrentSession == null || CurrentSession.CurrentPack == null ? null : CurrentSession.CurrentPack.TransactionId,
                cards=new OpeningCardView[0],
                revealedCount=CurrentSession == null || CurrentSession.CurrentPack == null ? 0 : CurrentSession.CurrentPack.RevealedCount,
                totalVirtualCards=CurrentSession == null ? 0 : CurrentSession.TotalVirtualCards,
                recoveryNotice=CurrentSession == null ? null : CurrentSession.RecoveryNotice,
                saveLocation=Paths == null ? null : Paths.SaveDirectory,
                modelLabel="Model A 0.1.0 · approved provisional",
                statusLine="Save blocked · Last acknowledged snapshot only · Physical tracking: "+(PhysicalTrackingConfigured?"manual ownership in Binder":"not configured")
            });
        }

        private void SetView(OpeningView view)
        {
            view.storageBusy = IsStorageBusy;
            if(IsStorageBusy) view.statusLine = "Saving... · Waiting for durable acknowledgement";
            view.paused = paused;
            view.volume = volume;
            view.inspectIndex = inspectIndex;
            if (ApprovedSets != null) {
                var pack=CurrentSession==null?null:CurrentSession.CurrentPack;
                view.selectedPrintingId=SelectedPrintingId;
                view.activePackPrintingId=pack==null?null:pack.PrintingId;
                view.setLabel=SetLabel(pack==null?SelectedPrintingId:pack.PrintingId);
                view.canSelectSet=CanSelectSet();
                view.supportedSets=SetChoices();
            }
            CurrentView = view;
            var handler=ViewPublished; if(handler!=null) handler(view);
            // A synchronous authority subscriber may have republished a stricter gate.
            // Never restore the superseded DTO after that nested publication.
            if (!closed) presentation.View = CurrentView;
        }

        private void PublishView()
        {
            lastError = null;
            var pack = CurrentSession.CurrentPack;
            var cards = new OpeningCardView[pack == null ? 0 : pack.OrderedCardIds.Count];
            for (int i=0; i<cards.Length; i++) {
                var card = ApprovedSets==null ? catalog.Find(pack.OrderedCardIds[i]) : ApprovedSets.FindCard(pack.OrderedCardIds[i]);
                cards[i] = new OpeningCardView {
                    id=card.Id, name=card.DisplayName, collectorNumber=card.CollectorNumber,
                    rarity=card.Rarity, kind=card.Kind, finish=card.Finish, isNew=pack.NewToVirtual[i]
                };
            }
            SetView(new OpeningView {
                stage=pack == null ? "ready" : pack.IsComplete && finalTableTransaction != pack.TransactionId ? "results" : pack.WrapperOpened ? "revealing" : "sealed",
                transactionId=pack == null ? null : pack.TransactionId, cards=cards,
                revealedCount=pack == null ? 0 : pack.RevealedCount,
                totalVirtualCards=CurrentSession.TotalVirtualCards, recoveryNotice=CurrentSession.RecoveryNotice,
                saveLocation=Paths.SaveDirectory, volume=0.5f,
                modelLabel=pack == null ? ApprovedSets==null ? "Model A 0.1.0 · approved provisional" : ApprovedSets.FindSet(SelectedPrintingId).ModelId+" "+ApprovedSets.FindSet(SelectedPrintingId).ModelVersion+" · "+ApprovedSets.FindSet(SelectedPrintingId).Status : pack.ModelId+" "+pack.ModelVersion+" · "+pack.Status,
                statusLine="Private collection · Physical tracking: "+(PhysicalTrackingConfigured?"manual ownership in Binder":"not configured")
            });
        }

        // All presentation entrypoints forward process-exit intent to this owner.
        public void RequestQuit()
        {
            quitRequested = true;
            LastCommand = "quit";
            if (IsStorageBusy && !closed) return;
            if (!closed) Shutdown();
            else AttemptQuitAfterCleanup();
        }
        private bool quitRequested, quitAttempted, shutdownComplete;
        private void AttemptQuitAfterCleanup()
        {
            // A nested Shutdown return is not global completion. Completion includes every
            // ShutdownCompleted subscriber, even ones registered after the lease owners.
            if (!shutdownComplete || !quitRequested || quitAttempted || Application.isEditor) return;
            quitAttempted = true; // A reentrant or throwing exit request is never retried.
            Application.Quit();
        }
        private bool shutdownNotifying;
        internal bool IsShuttingDown { get { return shutdownNotifying; } }
        private static void NotifyCleanup(Action handlers,System.Collections.Generic.List<Exception> failures) {
            if(handlers==null) return;
            // Snapshot each independent owner: a throwing subscriber cannot suppress later cleanup.
            foreach(Action handler in handlers.GetInvocationList()) {
                try { handler(); } catch(Exception error) { failures.Add(error); }
            }
        }
        private void ReleaseSession() {
            if (store != null) {var owned=store;store=null;owned.Dispose();}
        }
        public void Shutdown()
        {
            if(closed) {
                // The unchanged legacy collection host releases physical, calls back into
                // Shutdown to release session, then releases its container. Do not re-emit.
                if(shutdownNotifying && ApprovedSets==null) ReleaseSession();
                return;
            }
            closed = true;
            shutdownComplete = false;
            var failures=new System.Collections.Generic.List<Exception>();
            // Close input before joining, and finish the owned worker before any lease owner callback.
            if(storageWorker!=null) {
                try {
                    storageWorker.Dispose();
                    SerialOpeningStorage.Completion answer;
                    if(storageWorker.TryTake(out answer)) {
                        if(answer.Error!=null) failures.Add(answer.Error);
                        else {CurrentSession=answer.Snapshot;pending=null;}
                    }
                } catch(Exception error) {failures.Add(error);}
                finally {storageWorker=null;IsStorageBusy=false;}
            }
            shutdownNotifying=true;
            try { NotifyCleanup(ShuttingDown,failures); }
            finally { shutdownNotifying=false; }
            if (presentation != null) presentation.Command -= DispatchCommand;
            try { ReleaseSession(); } catch(Exception error) { failures.Add(error); }
            NotifyCleanup(ShutdownCompleted,failures);
            shutdownComplete = true;
            Exception shutdownFailure = failures.Count==0 ? null : new AggregateException("Shutdown cleanup failed; all owners were attempted.",failures);
            try { AttemptQuitAfterCleanup(); }
            catch(Exception exitError) {
                if(shutdownFailure!=null) throw new AggregateException(shutdownFailure,exitError);
                throw;
            }
            if(shutdownFailure!=null) throw shutdownFailure;
        }

        private void OnDestroy() { Shutdown(); }
        private void OnApplicationQuit() { Shutdown(); }
    }
}
