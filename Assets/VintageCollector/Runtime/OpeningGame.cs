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
        public string allowedProjectRoot;
        public GamePaths Paths { get; private set; }
        public OpeningView CurrentView { get; private set; }
        // Last acknowledged snapshot only; not authoritative while CurrentView.stage == "error".
        public SessionSnapshot CurrentSession { get; private set; }
        public int GeneratedResultCount { get; private set; }
        public string LastCommand { get; private set; }

        private bool initialized;
        private Catalog catalog;
        private PackGenerator generator;
        private SessionStore store;
        private TabletopPresentation presentation;
        private bool closed;
        private PendingAction pending;
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
            public bool armed;
            public void At(string stage)
            {
                if (armed && stage == "after-prepare") {
                    armed = false;
                    throw new IOException("Explicit smoke fault after durable preparation; Retry preserves this exact action.");
                }
            }
        }

        private sealed class PendingAction
        {
            public string command, transactionId, createdUtc;
            public uint seed;
            public PackResult result;
            public int target;
        }

        private void Start()
        {
            if (!initialized) Initialize(Environment.GetCommandLineArgs());
        }

        public void Initialize(string[] args)
        {
            if (initialized) return;
            initialized = true;
            presentation = GetComponent<TabletopPresentation>();
            if (presentation == null) presentation = gameObject.AddComponent<TabletopPresentation>();
            presentation.Command += DispatchCommand;
            initialRoot = allowedProjectRoot;
            initialArgs = args == null ? null : (string[])args.Clone();
            catalogSource = catalogJson == null ? null : catalogJson.text;
            modelSource = modelJson == null ? null : modelJson.text;
            RunStorage(delegate {
                OpenSession();
                CurrentSession = store.Snapshot;
                PublishView();
            });
        }

        private void OpenSession()
        {
            if (Paths == null) Paths = GamePaths.Resolve(initialRoot, Application.dataPath, Application.isEditor, initialArgs);
            if (catalogSource == null || modelSource == null) throw new ArgumentException("Catalog and model TextAssets must be configured");
            if (catalog == null) catalog = Catalog.FromData(JsonUtility.FromJson<CatalogData>(catalogSource));
            if (generator == null) generator = new PackGenerator(catalog, JsonUtility.FromJson<RuleData>(modelSource));
            if (Paths.IsSmokeTest && smokeFaults == null) smokeFaults = new SmokeFaults();
            if (store == null) store = new SessionStore(Paths.SaveDirectory, new UnityStateSerializer(), catalog, smokeFaults);
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
            if (closed || !PresentationRules.CanEmit(CurrentView, command)) return;
            if (command == "quit") {
                LastCommand = command;
                Shutdown();
                if (!Application.isEditor) Application.Quit();
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
                RunStorage(delegate {
                    OpenSession();
                    store.Reload(); // Recovery may throw too; never expose its candidate early.
                    if (pending != null) ExecutePending();
                    else { CurrentSession = store.Snapshot; PublishView(); }
                });
                return;
            }
            if (pending != null) return;
            if (command == "generate") { RunStorage(delegate { Generate(CryptoSeed()); }); return; }
            if (command != "open" && command != "next" && command != "skip") return;
            var pack = CurrentSession.CurrentPack;
            LastCommand = command;
            pending = new PendingAction {
                command=command, transactionId=pack.TransactionId,
                target=command == "skip" ? pack.OrderedCardIds.Count : pack.RevealedCount+1
            };
            RunStorage(ExecutePending);
        }

        public void GenerateForSmoke(uint seed)
        {
            if (Paths == null || !Paths.IsSmokeTest) throw new InvalidOperationException("Explicit smoke mode required");
            if (seed == 0) throw new ArgumentOutOfRangeException("seed", "Seed must be nonzero");
            if (closed || pending != null || !PresentationRules.CanEmit(CurrentView,"generate")) return;
            RunStorage(delegate { Generate(seed); });
        }

        public void FailNextCommitForSmoke()
        {
            if (closed || Paths == null || !Paths.IsSmokeTest || smokeFaults == null)
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

        private void Generate(uint seed)
        {
            LastCommand = "generate";
            var action = new PendingAction {
                command="generate", transactionId=Guid.NewGuid().ToString("N"),
                createdUtc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture), seed=seed
            };
            GeneratedResultCount++;
            action.result = generator.Generate(new SeededRandom(seed));
            // Freeze the result and every call argument before entering the durable service.
            pending = action;
            ExecutePending();
        }

        private void ExecutePending()
        {
            var action = pending;
            if (action.command == "generate") store.OpenPack(action.transactionId,action.createdUtc,action.seed,action.result);
            else if (action.command == "open") store.OpenWrapper(action.transactionId);
            else store.RevealTo(action.transactionId,action.target);
            var acknowledged = store.Snapshot;
            CurrentSession = acknowledged;
            pending = null;
            // Keep the final ordinary reveal on the same table as every earlier card.
            // This is presentation-only: the eleven-card transaction is already durable.
            if (action.command == "next" && acknowledged.CurrentPack != null && acknowledged.CurrentPack.IsComplete)
                finalTableTransaction = acknowledged.CurrentPack.TransactionId;
            else if (action.command == "generate" || action.command == "skip")
                finalTableTransaction = null;
            PublishView();
            presentation.NotifyCommitted(action.command);
        }

        private void PublishError(Exception error)
        {
            // Never query the uncertain store here: Snapshot itself may require failing recovery.
            lastError = error;
            SetView(new OpeningView {
                stage="error", errorMessage=error.Message,
                transactionId=pending != null ? pending.transactionId : CurrentSession == null || CurrentSession.CurrentPack == null ? null : CurrentSession.CurrentPack.TransactionId,
                cards=new OpeningCardView[0],
                revealedCount=CurrentSession == null || CurrentSession.CurrentPack == null ? 0 : CurrentSession.CurrentPack.RevealedCount,
                totalVirtualCards=CurrentSession == null ? 0 : CurrentSession.TotalVirtualCards,
                recoveryNotice=CurrentSession == null ? null : CurrentSession.RecoveryNotice,
                saveLocation=Paths == null ? null : Paths.SaveDirectory,
                modelLabel="Model A 0.1.0 · approved provisional",
                statusLine="Save blocked · Last acknowledged snapshot only · Physical tracking: not configured"
            });
        }

        private void SetView(OpeningView view)
        {
            view.paused = paused;
            view.volume = volume;
            view.inspectIndex = inspectIndex;
            CurrentView = view;
            presentation.View = view;
        }

        private void PublishView()
        {
            lastError = null;
            var pack = CurrentSession.CurrentPack;
            var cards = new OpeningCardView[pack == null ? 0 : pack.OrderedCardIds.Count];
            for (int i=0; i<cards.Length; i++) {
                var card = catalog.Find(pack.OrderedCardIds[i]);
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
                modelLabel=pack == null ? "Model A 0.1.0 · approved provisional" : pack.ModelId+" "+pack.ModelVersion+" · "+pack.Status,
                statusLine="Placeholder visuals · Physical tracking: not configured"
            });
        }

        public void Shutdown()
        {
            closed = true;
            if (presentation != null) presentation.Command -= DispatchCommand;
            if (store != null) { store.Dispose(); store = null; }
        }

        private void OnDestroy() { Shutdown(); }
        private void OnApplicationQuit() { Shutdown(); }
    }
}
