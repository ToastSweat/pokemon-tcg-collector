using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VintageCollector.Domain;
using VintageCollector.Media;
using VintageCollector.Presentation;
using VintageCollector.Runtime;

namespace VintageCollector.EditorTools
{
    public static class FinalCardPreviewChecks
    {
        [Serializable] private sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] private sealed class Report { public bool passed; public int checkCount; public Check[] checks; }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject root;
            public readonly OpeningGame game;
            public readonly TabletopPresentation presentation;
            public readonly PresentationMediaLibrary media;
            public readonly string folder;
            public Fixture(string reuse = null)
            {
                string project = Path.GetDirectoryName(Application.dataPath);
                folder = reuse ?? Path.Combine(project, ".artifacts", "final-card-fix", "fixtures", Guid.NewGuid().ToString("N"));
                root = new GameObject("Final card preview fixture");
                presentation = root.AddComponent<TabletopPresentation>();
                game = root.AddComponent<OpeningGame>();
                game.catalogJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json");
                game.modelJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json");
                game.allowedProjectRoot = project;
                game.Initialize(new[] { "--vcc-smoke=ready", "--vcc-save-root=" + Path.Combine(folder,"saves"), "--vcc-artifacts=" + Path.Combine(folder,"reports") });
                var catalog = Catalog.FromData(JsonUtility.FromJson<CatalogData>(game.catalogJson.text));
                media = PresentationMediaLibrary.Load(project,catalog);
                presentation.Media = media;
            }
            public void Dispose()
            {
                game.Shutdown(); presentation.Media = null; media.Dispose(); UnityEngine.Object.DestroyImmediate(root);
            }
        }
        private static readonly List<Check> checks = new List<Check>();
        private static void Require(string name, bool condition)
        {
            checks.Add(new Check { name=name,passed=condition,detail=condition?"PASS":"FAIL" });
            if (!condition) throw new InvalidOperationException(name);
        }
        private static void Test(string name, Action action)
        {
            try { action(); }
            catch (Exception error) { checks.Add(new Check { name=name+":failure",passed=false,detail=error.ToString() }); }
        }
        public static void Run()
        {
            checks.Clear();
            string completed = null;
            Test("last-next", delegate {
                using (var f = new Fixture()) {
                    f.game.GenerateForSmoke(4); f.game.DispatchCommand("open");
                    for (int i=0;i<10;i++) f.game.DispatchCommand("next");
                    Require("ten-cards-still-large-reveal",f.game.CurrentView.stage=="revealing" && f.game.CurrentView.revealedCount==10 && f.game.CurrentView.inspectIndex==-1);
                    int before = f.presentation.CommittedCueCount;
                    string id = f.game.CurrentSession.CurrentPack.OrderedCardIds[10];
                    f.game.DispatchCommand("next");
                    Require("eleventh-next-shows-large-preview-before-grid",f.game.CurrentView.stage=="results" && f.game.CurrentView.inspectIndex==10 && PresentationRules.CanInspect(f.game.CurrentView,10));
                    Require("large-preview-is-exact-saved-eleventh-card",f.game.CurrentView.cards[10].id==id && f.game.CurrentSession.CurrentPack.RevealedCount==11);
                    Require("final-next-retains-one-committed-sound",f.presentation.CommittedCueCount==before+1 && f.presentation.LastCommittedCue=="next");
                    long revision=f.game.CurrentSession.Revision;
                    f.game.DispatchCommand("next"); f.game.DispatchCommand("skip");
                    Require("duplicate-final-input-has-no-award-or-save",f.game.CurrentSession.Revision==revision && f.game.CurrentSession.TotalVirtualCards==11 && f.game.GeneratedResultCount==1);
                    f.game.DispatchCommand("close-inspect");
                    Require("back-shows-grid-without-new-save-award",f.game.CurrentView.stage=="results" && f.game.CurrentView.inspectIndex==-1 && f.game.CurrentSession.Revision==revision && f.game.CurrentSession.TotalVirtualCards==11);
                    completed=f.folder;
                }
            });
            Test("skip", delegate {
                using (var f = new Fixture()) {
                    f.game.GenerateForSmoke(4); f.game.DispatchCommand("open"); f.game.DispatchCommand("next"); f.game.DispatchCommand("skip");
                    Require("skip-still-goes-directly-to-grid",f.game.CurrentView.stage=="results" && f.game.CurrentView.inspectIndex==-1 && f.game.CurrentSession.TotalVirtualCards==11);
                }
            });
            Test("final-retry", delegate {
                using (var f = new Fixture()) {
                    f.game.GenerateForSmoke(4); f.game.DispatchCommand("open"); for(int i=0;i<10;i++)f.game.DispatchCommand("next");
                    int cues=f.presentation.CommittedCueCount;
                    f.game.FailNextCommitForSmoke(); f.game.DispatchCommand("next");
                    Require("failed-final-save-exposes-no-preview",f.game.CurrentView.stage=="error" && f.game.CurrentView.inspectIndex==-1 && f.presentation.CommittedCueCount==cues);
                    f.game.DispatchCommand("retry");
                    Require("successful-final-retry-opens-same-large-preview",f.game.CurrentView.inspectIndex==10 && f.game.CurrentSession.CurrentPack.RevealedCount==11 && f.game.GeneratedResultCount==1 && f.game.CurrentSession.TotalVirtualCards==11);
                    Require("successful-final-retry-cues-once",f.presentation.CommittedCueCount==cues+1);
                }
            });
            if(completed!=null)Test("completed-resume", delegate {
                using(var f=new Fixture(completed)) Require("completed-resume-does-not-replay-preview-or-sound",f.game.CurrentView.stage=="results" && f.game.CurrentView.inspectIndex==-1 && f.presentation.CommittedCueCount==0 && f.game.GeneratedResultCount==0 && f.game.CurrentSession.TotalVirtualCards==11);
            });
            bool passed=checks.Count>0 && checks.TrueForAll(c=>c.passed);
            string output=Path.Combine(Path.GetDirectoryName(Application.dataPath),".artifacts","final-card-fix"); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(new Report {passed=passed,checkCount=checks.Count,checks=checks.ToArray()},true));
            if(Application.isBatchMode)EditorApplication.Exit(passed?0:1);
            else if(!passed)throw new Exception("Final-card preview regression failed.");
        }
    }
}
