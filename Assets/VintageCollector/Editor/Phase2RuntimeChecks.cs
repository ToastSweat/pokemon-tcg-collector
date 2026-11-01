using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VintageCollector.Persistence;

// Explicit -executeMethod only; never runs on startup or enters Play mode.
public static class Phase2RuntimeChecks
{
    [Serializable] private sealed class Check { public string name; public bool passed; public string detail; }
    [Serializable] private sealed class Report { public bool passed; public int checkCount, assertionCount; public Check[] checks, assertions; }
    private static readonly List<Check> checks = new List<Check>();
    private static readonly List<Check> assertions = new List<Check>();
    private static string currentTest;
    private static void Test(string name, Action test)
    {
        currentTest=name;
        try { test(); checks.Add(new Check { name=name, passed=true, detail="passed" }); }
        catch(Exception e) { checks.Add(new Check { name=name, passed=false, detail=e.ToString() }); }
    }
    private static void Require(bool condition, string message)
    {
        assertions.Add(new Check { name=currentTest+"/"+(assertions.Count+1)+": "+message, passed=condition, detail=condition?"passed":"failed" });
        if(!condition) throw new Exception(message);
    }
    private sealed class Fixture : IDisposable
    {
        public readonly GameObject go;
        public readonly VintageCollector.Runtime.OpeningGame game;
        public readonly string directory;
        public Fixture(string existing=null)
        {
            directory=existing ?? Path.Combine(Path.GetDirectoryName(Application.dataPath),".artifacts","phase2-runtime","fixtures",Guid.NewGuid().ToString("N"));
            go=new GameObject("Runtime durable integration fixture");
            game=go.AddComponent<VintageCollector.Runtime.OpeningGame>();
            game.allowedProjectRoot=Path.GetDirectoryName(Application.dataPath);
            game.catalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
            game.modelJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
            game.Initialize(new[]{"--vcc-smoke=ready","--vcc-save-root="+directory,"--vcc-artifacts="+directory});
        }
        public void Invoke(string method, params object[] args)
        {
            // This legacy fixture's Seed API deliberately selects the original uint overload.
            var m=method=="GenerateForSmoke"
                ? game.GetType().GetMethod(method,new[]{typeof(uint)})
                : game.GetType().GetMethod(method);
            Require(m!=null,"Required runtime API missing: "+method);
            try { m.Invoke(game,args); } catch(System.Reflection.TargetInvocationException e) { throw e.InnerException; }
        }
        public void Command(string command) { Invoke("DispatchCommand",command); }
        public void Seed(uint seed) { Invoke("GenerateForSmoke",seed); }
        public void Dispose() { game.Shutdown(); UnityEngine.Object.DestroyImmediate(go); }
    }
    public static void Run()
    {
        checks.Clear(); assertions.Clear();
        Test("real_unity_serializer_roundtrip", delegate {
            var type=typeof(SessionStore).Assembly.GetType("VintageCollector.Runtime.UnityStateSerializer");
            Require(type!=null,"UnityStateSerializer must exist");
            var adapter=(IStateSerializer)Activator.CreateInstance(type);
            var source=new SessionData { schemaVersion=1, revision=1234567890123L, packs=new[] {
                new PackRecordData { transactionId="tx-🂡", createdUtc="2026-10-30T12:00:00.0000000Z", seed=uint.MaxValue,
                    modelId="model", modelVersion="0.1.0", catalogVersion="catalog", printingId="printing", rngAlgorithm="rng", status="approved-provisional",
                    orderedCardIds=new[]{"one","two","two"}, newToVirtual=new[]{true,true,false}, wrapperOpened=true, revealedCount=3 }
            }};
            string json=adapter.Serialize(source);
            Require(json==JsonUtility.ToJson(source),"Adapter must use actual JsonUtility unchanged fields");
            var copy=adapter.Deserialize(json);
            Require(JsonUtility.ToJson(copy)==json,"All frozen DTO values roundtrip including uint/long/flags/Unicode");
            copy.packs[0].orderedCardIds[0]="changed";
            Require(source.packs[0].orderedCardIds[0]=="one","Roundtrip detached");
        });
        Test("startup_is_passive_and_lifecycle_is_idempotent", delegate {
            var type=typeof(SessionStore).Assembly.GetType("VintageCollector.Runtime.OpeningGame");
            Require(type!=null,"OpeningGame must exist");
            string root=Path.GetDirectoryName(Application.dataPath);
            string fixture=Path.Combine(root,".artifacts","phase2-runtime","fixtures",Guid.NewGuid().ToString("N"));
            var go=new GameObject("Runtime startup fixture");
            var c=go.AddComponent(type);
            try {
                type.GetField("catalogJson").SetValue(c,AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"));
                type.GetField("modelJson").SetValue(c,AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json"));
                type.GetField("allowedProjectRoot").SetValue(c,root);
                var args=new[]{"--vcc-smoke=ready","--vcc-save-root="+fixture,"--vcc-artifacts="+fixture};
                type.GetMethod("Initialize").Invoke(c,new object[]{args});
                var view=(VintageCollector.Presentation.OpeningView)type.GetProperty("CurrentView").GetValue(c);
                var session=(SessionSnapshot)type.GetProperty("CurrentSession").GetValue(c);
                Require(view.stage=="ready" && session.Packs.Count==0 && session.TotalVirtualCards==0,"Startup has no generated pack/awards");
                Require((int)type.GetProperty("GeneratedResultCount").GetValue(c)==0,"Startup never invokes generator");
                Require(!File.Exists(Path.Combine(fixture,"state.save")),"Startup writes no blank save");
                var presentation=go.GetComponent<VintageCollector.Presentation.TabletopPresentation>();
                Require(presentation!=null && ReferenceEquals(presentation.View,view),"Controller attaches and supplies real passive presentation");
                type.GetMethod("Initialize").Invoke(c,new object[]{args});
                type.GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(c,null);
                Require(ReferenceEquals(session,type.GetProperty("CurrentSession").GetValue(c)),"Manual initialization and Start cannot reopen/reset session");
                type.GetMethod("Shutdown").Invoke(c,null); type.GetMethod("Shutdown").Invoke(c,null);
                var catalog=VintageCollector.Domain.Catalog.FromData(JsonUtility.FromJson<VintageCollector.Domain.CatalogData>(((TextAsset)type.GetField("catalogJson").GetValue(c)).text));
                using(var store=new SessionStore(fixture,new VintageCollector.Runtime.UnityStateSerializer(),catalog)) Require(store.Snapshot.Packs.Count==0,"Shutdown releases real writer lock");
            } finally { type.GetMethod("Shutdown")?.Invoke(c,null); UnityEngine.Object.DestroyImmediate(go); }
        });
        Test("durable_generate_wrapper_reveals_restart_results_and_new_flags", delegate {
            string directory, tx, stamp; string[] ids; bool[] flags;
            using(var f=new Fixture()) {
                f.Seed(4u); var g=f.game; var p=g.CurrentSession.CurrentPack;
                Require(g.GeneratedResultCount==1 && p.Seed==4 && g.CurrentSession.TotalVirtualCards==11,"One actual generation awards exactly eleven before sealed");
                Require(g.CurrentView.stage=="sealed" && !p.WrapperOpened && File.Exists(Path.Combine(f.directory,"state.save")),"Sealed exposed only after real save");
                tx=p.TransactionId; stamp=p.CreatedUtc; directory=f.directory;
                Require(Guid.TryParseExact(tx,"N",out _) && DateTime.TryParse(stamp,out _),"Single GUID and UTC creation timestamp");
                ids=new List<string>(p.OrderedCardIds).ToArray(); flags=new List<bool>(p.NewToVirtual).ToArray();
                Require(ids[5]==ids[6] && flags[5] && !flags[6],"Real seed4 repeated Energy flags are persisted per-position");
                Require(p.ModelVersion=="0.1.0" && p.RngAlgorithm=="xorshift32-v1","Unchanged frozen model metadata");
                f.Command("generate"); f.Seed(17); f.Command("next"); f.Command("skip"); f.Command("nonsense");
                Require(g.GeneratedResultCount==1 && g.CurrentSession.Revision==1 && g.LastCommand=="generate","Duplicate/rejected commands never regenerate/advance/change accepted command");
                // Real passive component event is subscribed to the controller.
                var ui=f.go.GetComponent<VintageCollector.Presentation.TabletopPresentation>();
                ui.GetType().GetMethod("RequestCommand",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(ui,new object[]{"open"});
                Require(g.CurrentView.stage=="revealing" && g.CurrentSession.CurrentPack.WrapperOpened && g.CurrentSession.Revision==2,"Presentation open command commits before reveal screen");
                f.Command("open");
                for(int i=1;i<=3;i++) { f.Command("next"); Require(g.CurrentSession.CurrentPack.RevealedCount==i && g.CurrentView.revealedCount==i,"Absolute reveal durable "+i); }
                Require(g.CurrentSession.TotalVirtualCards==11,"Reveal never awards again");
                // Replacing the serialized asset cannot mutate the frozen generator or saved record.
                var altered=new TextAsset(g.modelJson.text.Replace("0.1.0","99.0.0"));
                g.modelJson=altered; Require(g.CurrentSession.CurrentPack.ModelVersion=="0.1.0","Serialized asset mutation cannot rewrite history");
                UnityEngine.Object.DestroyImmediate(altered);
            }
            using(var f=new Fixture(directory)) {
                var g=f.game; var p=g.CurrentSession.CurrentPack;
                Require(g.GeneratedResultCount==0 && p.TransactionId==tx && p.CreatedUtc==stamp && p.RevealedCount==3,"New controller resumes exact saved count3 without generator");
                Require(string.Join("|",p.OrderedCardIds)==string.Join("|",ids),"Restart retains original order");
                for(int i=0;i<11;i++) Require(p.NewToVirtual[i]==flags[i] && g.CurrentView.cards[i].isNew==flags[i],"Saved flag projected "+i);
                f.Command("skip");
                Require(g.CurrentView.stage=="results" && g.CurrentSession.CurrentPack.RevealedCount==11 && g.CurrentSession.TotalVirtualCards==11,"Skip completes exact saved pack without awards");
                long revision=g.CurrentSession.Revision; f.Command("skip"); f.Command("next");
                Require(g.CurrentSession.Revision==revision,"Completed progress duplicate inputs rejected");
                Require(g.CurrentView.statusLine.ToLowerInvariant().Contains("physical tracking: not configured"),"Physical ownership is explicitly not configured");
                f.Seed(4);
                Require(g.GeneratedResultCount==1 && g.CurrentSession.Packs.Count==2 && g.CurrentSession.TotalVirtualCards==22,"New pack only after completion");
                foreach(bool isNew in g.CurrentSession.CurrentPack.NewToVirtual) Require(!isNew,"Second identical pack all duplicate flags");
                Require(g.CurrentSession.Packs[0].ModelVersion=="0.1.0","Historical model immutable");
            }
        });
        Test("normal_generate_uses_nonzero_crypto_identity_once", delegate {
            using(var f=new Fixture()) {
                f.Command("generate"); var p=f.game.CurrentSession.CurrentPack;
                Require(p.Seed!=0 && Guid.TryParseExact(p.TransactionId,"N",out _) && p.CreatedUtc.EndsWith("Z",StringComparison.Ordinal),"Normal generate creates nonzero seed GUID UTC");
                Require(f.game.GeneratedResultCount==1 && f.game.CurrentView.stage=="sealed","Normal dispatch invokes generator exactly once");
            }
        });
        Test("prepared_generation_failure_retries_exact_identity_and_eleven_awards", delegate {
            using(var f=new Fixture()) {
                var g=f.game; var before=g.CurrentSession;
                f.Invoke("FailNextCommitForSmoke"); f.Seed(4);
                Require(g.CurrentView.stage=="error" && ReferenceEquals(before,g.CurrentSession) && before.TotalVirtualCards==0,"Prepared failure exposes error not candidate success");
                Require(g.GeneratedResultCount==1 && g.LastCommand=="generate","Failed accepted generation counted once");
                string path=Path.Combine(f.directory,"state.pending"), prepared=File.ReadAllText(path);
                var data=new VintageCollector.Runtime.UnityStateSerializer().Deserialize(prepared.Substring(prepared.IndexOf('\n',prepared.IndexOf('\n')+1)+1));
                var p=data.packs[0];
                Require(p.seed==4 && p.orderedCardIds.Length==11 && g.CurrentView.transactionId==p.transactionId,"Exact generated arguments prepared before error");
                f.Command("generate"); f.Seed(9); f.Command("open"); f.Command("next"); f.Command("skip");
                Require(g.GeneratedResultCount==1 && ReferenceEquals(before,g.CurrentSession),"Error blocks all new generation/advance");
                using(var deny=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None)) {
                    f.Command("retry"); Require(g.CurrentView.stage=="error" && ReferenceEquals(before,g.CurrentSession),"Retry catches actual recovery sharing failure");
                }
                File.WriteAllText(path,"corrupt prepared fixture");
                f.Command("retry"); Require(g.CurrentView.stage=="error" && ReferenceEquals(before,g.CurrentSession),"Retry explicitly catches Mono InvalidDataException");
                File.WriteAllText(path,prepared);
                f.Command("retry"); var saved=g.CurrentSession.CurrentPack;
                Require(g.CurrentView.stage=="sealed" && saved.TransactionId==p.transactionId && saved.CreatedUtc==p.createdUtc && saved.Seed==p.seed,"Retry acknowledges exact original transaction/timestamp/seed");
                Require(string.Join("|",saved.OrderedCardIds)==string.Join("|",p.orderedCardIds),"Retry never rerolls ordered IDs");
                Require(g.GeneratedResultCount==1 && g.CurrentSession.TotalVirtualCards==11 && g.CurrentSession.Packs.Count==1 && g.CurrentSession.Revision==1,"Recovered retry awards exactly eleven once");
                Require(!File.Exists(path) && !string.IsNullOrEmpty(g.CurrentView.recoveryNotice),"Real recovery consumed prepared state with notice");
                f.Command("open"); Require(g.CurrentView.stage=="revealing","Fault disarmed after one actual hook");
            }
        });
        Test("prepared_wrapper_next_skip_retry_same_absolute_target", delegate {
            using(var f=new Fixture()) {
                f.Seed(4); var g=f.game; string tx=g.CurrentSession.CurrentPack.TransactionId;
                foreach(string command in new[]{"open","next","skip"}) {
                    var before=g.CurrentSession;
                    int target=command=="open"?0:command=="next"?1:11;
                    f.Invoke("FailNextCommitForSmoke"); f.Command(command);
                    Require(g.CurrentView.stage=="error" && ReferenceEquals(before,g.CurrentSession),command+" failure retains last acknowledged snapshot");
                    var ui=f.go.GetComponent<VintageCollector.Presentation.TabletopPresentation>();
                    var tear=ui.GetType().GetField("tearRemaining",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    if(command=="open") Require((float)tear.GetValue(ui)==0,"Unacknowledged wrapper never starts positive tear cue");
                    f.Command(command); f.Command("next"); f.Command("generate");
                    f.Command("retry"); var p=g.CurrentSession.CurrentPack;
                    if(command=="open") Require((float)tear.GetValue(ui)>0,"Tear cue starts only after successful exact wrapper Retry");
                    Require(p.TransactionId==tx && p.WrapperOpened && p.RevealedCount==target,command+" retries original tx and absolute target without extra reveal");
                    Require(g.GeneratedResultCount==1 && g.CurrentSession.TotalVirtualCards==11,command+" never regenerates or awards");
                }
                Require(g.CurrentView.stage=="results" && g.CurrentSession.Revision==4,"Exactly generation wrapper next skip revisions");
            }
        });
        Test("initial_corruption_blocks_without_blank_and_retry_reopens_same_directory", delegate {
            string directory,original,tx;
            using(var saved=new Fixture()) { saved.Seed(4); directory=saved.directory; original=File.ReadAllText(Path.Combine(directory,"state.save")); tx=saved.game.CurrentSession.CurrentPack.TransactionId; }
            string file=Path.Combine(directory,"state.save"), bad="VTCG-SAVE-999\nunsupported bytes retained";
            File.WriteAllText(file,bad);
            using(var f=new Fixture(directory)) {
                Require(f.game.CurrentView.stage=="error" && f.game.CurrentSession==null && f.game.GeneratedResultCount==0,"Init InvalidDataException produces blocking error not empty success");
                f.Command("retry"); f.Command("generate");
                Require(File.ReadAllText(file)==bad && f.game.CurrentView.stage=="error","Unresolved retry preserves original bad bytes and blocks generation");
                File.WriteAllText(file,original); f.Command("retry");
                Require(f.game.CurrentView.stage=="sealed" && f.game.CurrentSession.CurrentPack.TransactionId==tx && f.game.CurrentSession.TotalVirtualCards==11,"Init-only retry reopens exact save rather than blank fallback");
                Require(f.game.GeneratedResultCount==0,"Recovery never invokes generator");
            }
        });
        Test("second_writer_blocks_then_retry_after_shutdown_releases_lock", delegate {
            using(var first=new Fixture()) {
                first.Seed(4);
                using(var second=new Fixture(first.directory)) {
                    Require(second.game.CurrentView.stage=="error" && second.game.CurrentSession==null,"Second controller cannot acquire actual exclusive store");
                    second.Command("retry"); Require(second.game.CurrentView.stage=="error","Retry while owner alive remains blocked");
                    string tx=first.game.CurrentSession.CurrentPack.TransactionId; first.game.Shutdown();
                    second.Command("retry"); Require(second.game.CurrentView.stage=="sealed" && second.game.CurrentSession.CurrentPack.TransactionId==tx,"Shutdown releases lock and init-only Retry resumes saved pack");
                }
            }
        });
        Test("presentation_state_validated_without_save_or_awards", delegate {
            using(var f=new Fixture()) {
                var g=f.game;
                foreach(string command in new[]{"inspect:-1","inspect:0","inspect:999","inspect:1x","volume:-0.1","volume:1.1","volume:NaN","volume:Infinity","volume:0,5","volume:","volume:1e-1","resume","close-inspect"}) f.Command(command);
                Require(g.LastCommand==null && g.CurrentView.inspectIndex==-1 && g.CurrentView.volume==0.5f,"Invalid inspection/volume/state commands rejected by shared rules");
                f.Command("pause"); Require(g.CurrentView.paused && g.LastCommand=="pause","Pause is controller-owned presentation state");
                f.Command("generate"); Require(g.GeneratedResultCount==0,"Pause blocks generation");
                f.Command("volume:0"); Require(g.CurrentView.volume==0 && g.CurrentView.paused,"Mute while paused");
                f.Command("volume:1"); f.Command("volume:0.25");
                Require(g.CurrentView.volume==0.25f && g.CurrentSession.Revision==0,"Volume bounds accepted without save");
                f.Command("resume"); f.Seed(4); f.Command("open"); f.Command("next");
                var session=g.CurrentSession; long revision=session.Revision; string saved=File.ReadAllText(Path.Combine(f.directory,"state.save"));
                f.Command("inspect:1"); Require(g.CurrentView.inspectIndex==-1,"Unrevealed index rejected");
                f.Command("inspect:0"); Require(g.CurrentView.inspectIndex==0,"Revealed card inspectable");
                f.Command("next"); Require(g.CurrentSession.CurrentPack.RevealedCount==1,"Inspection modal blocks reveal");
                f.Command("close-inspect"); f.Command("pause"); f.Command("pause");
                var ui=f.go.GetComponent<VintageCollector.Presentation.TabletopPresentation>();
                ui.GetType().GetMethod("OnApplicationFocus",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(ui,new object[]{true});
                Require(g.CurrentView.paused,"Focus gain does not auto-resume");
                f.Command("resume");
                ui.GetType().GetMethod("OnApplicationFocus",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(ui,new object[]{false});
                Require(g.CurrentView.paused,"Real focus-loss callback requests controller pause");
                f.Command("resume");
                Require(g.CurrentSession.Revision==revision && ReferenceEquals(session,g.CurrentSession) && File.ReadAllText(Path.Combine(f.directory,"state.save"))==saved,"Settings/inspection/focus never save or award");
                Require(g.CurrentView.volume==0.25f && g.CurrentView.inspectIndex==-1,"Presentation settings survive durable updates");
                Require(f.go.GetComponentsInChildren<AudioSource>(true).Length==0,"Controller attaches no audio");
            }
        });
        Test("quit_in_editor_closes_lock_without_exit_or_final_save", delegate {
            using(var f=new Fixture()) {
                f.Seed(4); string saved=File.ReadAllText(Path.Combine(f.directory,"state.save"));
                f.Command("quit"); Require(f.game.LastCommand=="quit","Quit accepted safely without exiting test editor");
                f.Command("open"); f.Command("generate"); Require(f.game.GeneratedResultCount==1,"Closed controller rejects commands");
                var catalog=VintageCollector.Domain.Catalog.FromData(JsonUtility.FromJson<VintageCollector.Domain.CatalogData>(f.game.catalogJson.text));
                using(var store=new SessionStore(f.directory,new VintageCollector.Runtime.UnityStateSerializer(),catalog)) Require(store.Snapshot.TotalVirtualCards==11,"Quit releases exclusive lock");
                Require(File.ReadAllText(Path.Combine(f.directory,"state.save"))==saved,"Quit has no final best-effort save");
            }
            using(var f=new Fixture()) {
                f.Invoke("FailNextCommitForSmoke"); f.Seed(4); Require(f.game.CurrentView.stage=="error","Quit error fixture prepared");
                f.Command("quit"); Require(f.game.LastCommand=="quit","Pending failure never blocks Quit");
                using(var resumed=new Fixture(f.directory)) Require(resumed.game.CurrentSession.TotalVirtualCards==11,"Quit closes error-session lock without losing prepared recovery");
            }
        });
        Test("normal_path_resolution_and_init_guard_refuse_smoke_apis_without_live_save_writes", delegate {
            string root=Path.GetDirectoryName(Application.dataPath);
            var normal=VintageCollector.Runtime.GamePaths.Resolve(root,Application.dataPath,true,new string[0]);
            Require(!normal.IsSmokeTest && normal.SaveDirectory==Path.Combine(root,"Saves","Phase2"),"Normal editor save default remains project-local");
            bool existed=Directory.Exists(normal.SaveDirectory);
            var go=new GameObject("Normal startup guard fixture"); var g=go.AddComponent<VintageCollector.Runtime.OpeningGame>(); var invalid=new TextAsset("{}");
            try {
                g.allowedProjectRoot=root;
                g.catalogJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
                // Deliberately invalid model stops before SessionStore touches normal savedata.
                g.modelJson=invalid;
                typeof(VintageCollector.Runtime.OpeningGame).GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(g,null);
                Require(g.Paths!=null && !g.Paths.IsSmokeTest && g.CurrentView.stage=="error" && g.CurrentSession==null,"Actual normal Start uses process arguments and resolves default but invalid fixture blocks storage");
                bool seedRejected=false,faultRejected=false;
                try { g.GenerateForSmoke(4); } catch(InvalidOperationException) { seedRejected=true; }
                try { g.FailNextCommitForSmoke(); } catch(InvalidOperationException) { faultRejected=true; }
                Require(seedRejected && faultRejected && g.GeneratedResultCount==0,"Both smoke APIs fail closed in normal mode");
                Require(Directory.Exists(normal.SaveDirectory)==existed,"Guarded normal fixture creates no live save directory");
                Require(typeof(VintageCollector.Runtime.OpeningGame).GetField("smokeFaults",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g)==null,"No optional fault injector constructed outside smoke");
            } finally { g.Shutdown(); UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(invalid); }
            using(var f=new Fixture()) {
                bool rejected=false; try { f.Seed(0); } catch(ArgumentOutOfRangeException) { rejected=true; }
                Require(rejected && f.game.GeneratedResultCount==0 && f.game.CurrentSession.TotalVirtualCards==0,"Zero smoke seed rejected before actual generator invocation");
            }
        });
        Test("all_next_boundaries_and_completed_restart_project_saved_order", delegate {
            string directory,tx;
            using(var f=new Fixture()) {
                f.Seed(4); f.Command("open"); var g=f.game; directory=f.directory; tx=g.CurrentSession.CurrentPack.TransactionId;
                for(int target=1;target<=11;target++) {
                    var before=g.CurrentSession; f.Command("next"); var p=g.CurrentSession.CurrentPack;
                    Require(p.RevealedCount==target && g.CurrentView.revealedCount==target && p.TransactionId==tx,"Next absolute boundary "+target);
                    Require(before.CurrentPack.RevealedCount==target-1 && g.CurrentSession.TotalVirtualCards==11,"Immutable prior snapshot and eleven awards at "+target);
                    Require(g.CurrentView.stage=="revealing" && g.CurrentView.inspectIndex==-1,"Ordinary reveal stays on the opening table at "+target);
                    Require(VintageCollector.Presentation.PresentationRules.VisibleCount(g.CurrentView)==target,"Only saved reveal count visible at "+target);
                }
                f.Command("next"); Require(g.GeneratedResultCount==1 && g.CurrentSession.Revision==13,"No next beyond eleventh card");
            }
            using(var f=new Fixture(directory)) {
                Require(f.game.CurrentView.stage=="results" && f.game.CurrentSession.CurrentPack.TransactionId==tx && f.game.GeneratedResultCount==0,"Completed restart does not reroll or award");
                for(int i=0;i<11;i++) Require(f.game.CurrentView.cards[i].id==f.game.CurrentSession.CurrentPack.OrderedCardIds[i],"Results saved order "+i);
            }
        });
        Test("preparation_denied_retains_frozen_call_arguments_and_source_version", delegate {
            using(var f=new Fixture()) {
                var g=f.game;
                string writing=Path.Combine(f.directory,"state.writing");
                using(var deny=new FileStream(writing,FileMode.Create,FileAccess.ReadWrite,FileShare.None)) f.Seed(4);
                Require(g.CurrentView.stage=="error" && g.GeneratedResultCount==1 && g.CurrentSession.TotalVirtualCards==0,"Real pre-prepare disk failure blocks success");
                Require(!File.Exists(Path.Combine(f.directory,"state.pending")),"Denied write did not prepare candidate");
                var pendingField=typeof(VintageCollector.Runtime.OpeningGame).GetField("pending",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var action=pendingField.GetValue(g); var type=action.GetType();
                string tx=(string)type.GetField("transactionId").GetValue(action),stamp=(string)type.GetField("createdUtc").GetValue(action);
                var result=(VintageCollector.Domain.PackResult)type.GetField("result").GetValue(action);
                f.Command("generate"); f.Seed(9); Require(ReferenceEquals(action,pendingField.GetValue(g)),"Repeated input retains same frozen PendingAction object");
                f.Command("retry"); var p=g.CurrentSession.CurrentPack;
                Require(p.TransactionId==tx && p.CreatedUtc==stamp && p.Seed==4 && string.Join("|",p.OrderedCardIds)==string.Join("|",result.OrderedCardIds),"Pre-prepare retry reuses exact generation call arguments");
                f.Command("open"); f.Command("skip");
                var changed=new TextAsset(g.modelJson.text.Replace("0.1.0","99.0.0")); g.modelJson=changed;
                try { f.Seed(4); Require(g.CurrentSession.CurrentPack.ModelVersion=="0.1.0" && g.CurrentSession.TotalVirtualCards==22,"Next generation retains initially parsed immutable model version"); }
                finally { UnityEngine.Object.DestroyImmediate(changed); }
                f.Command("open"); f.Command("skip");
                g.Shutdown();
                using(var reopened=new Fixture(f.directory)) {
                    Require(reopened.game.CurrentSession.Packs.Count==2 && reopened.game.CurrentSession.TotalVirtualCards==22,"Two-pack archive survives real close/reopen");
                    foreach(bool isNew in reopened.game.CurrentSession.CurrentPack.NewToVirtual) Require(!isNew,"Duplicate flags stay persisted after second-pack restart");
                }
            }
        });
        bool passed=checks.Count>0 && checks.TrueForAll(c=>c.passed);
        string root=Path.GetDirectoryName(Application.dataPath);
        string output=Path.Combine(root,".artifacts","phase2-runtime"); Directory.CreateDirectory(output);
        string label=Environment.GetEnvironmentVariable("VCC_RUNTIME_LABEL") ?? "checks";
        if(label.IndexOfAny(Path.GetInvalidFileNameChars())>=0) throw new Exception("Invalid report label");
        File.WriteAllText(Path.Combine(output,label+".json"),JsonUtility.ToJson(new Report { passed=passed,checkCount=checks.Count,assertionCount=assertions.Count,checks=checks.ToArray(),assertions=assertions.ToArray() },true));
        Debug.Log("PHASE2_RUNTIME_CHECKS "+(passed?"PASS":"FAIL")+" count="+checks.Count);
        if(Application.isBatchMode) EditorApplication.Exit(passed?0:1);
        else if(!passed) throw new Exception("Runtime checks failed; see .artifacts/phase2-runtime/"+label+".json");
    }
}
