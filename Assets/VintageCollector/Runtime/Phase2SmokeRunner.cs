using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VintageCollector.Runtime;
using VintageCollector.Presentation;

namespace VintageCollector.Verification
{
    // Copied into the main Assets tree only for this approved local verification build.
    // Normal launch returns before any automation, screenshot, report or generated pack.
    public sealed class Phase2SmokeRunner : MonoBehaviour
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Control { public string name; public float x, y, width, height; }
        [Serializable] public sealed class Probe
        {
            public string stage, transactionId, lastCommand, modelVersion;
            public int revealedCount, totalVirtualCards, generatedResultCount, inspectIndex, screenWidth, screenHeight;
            public long revision;
            public bool paused;
            public string[] cardIds;
            public bool[] newToVirtual;
            public Control[] controls;
        }
        [Serializable] public sealed class Report
        {
            public bool passed;
            public int checkCount;
            public string scenario;
            public Check[] checks;
            public Probe initial, final;
            public string[] screenshots;
        }
        private OpeningGame game;
        private GamePaths paths;
        private readonly List<Check> checks = new List<Check>();
        private readonly List<string> screenshots = new List<string>();
        private Probe initial;
        private bool active, finished;
        private string lastProbe = "";

        private IEnumerator Start()
        {
            yield return null;
            game = GetComponent<OpeningGame>();
            if (game == null || game.Paths == null || !game.Paths.IsSmokeTest) yield break;
            paths = game.Paths;
            Directory.CreateDirectory(paths.ArtifactDirectory);
            Application.runInBackground = true;
            active = true;
            initial = Snapshot();
            if (paths.SmokeScenario == "input")
            {
                CheckThat("input-mode-does-not-autogenerate", game.GeneratedResultCount == 0, "Native-input observation mode only.");
                yield break;
            }
            yield return Guard(RunScenario());
            Finish(true);
        }

        private IEnumerator Guard(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                object current = null;
                bool more = false;
                Exception failure = null;
                try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null)
                {
                    checks.Add(new Check { name = "unhandled-smoke-failure", passed = false, detail = failure.ToString() });
                    yield break;
                }
                if (!more) { stack.Pop(); continue; }
                var nested = current as IEnumerator;
                if (nested != null) stack.Push(nested);
                else yield return current;
            }
        }

        private IEnumerator RunScenario()
        {
            if (game.CurrentView.paused) game.DispatchCommand("resume");
            string scenario = paths.SmokeScenario;
            if (scenario == "resume")
            {
                CheckThat("restart-resumes-three-reveals", game.CurrentView.stage == "revealing" && game.CurrentView.revealedCount == 3, "Persisted progress loaded in another player process.");
                CheckThat("restart-never-regenerates", game.GeneratedResultCount == 0 && game.CurrentSession.TotalVirtualCards == 11, "Existing transaction and awards loaded, not rolled.");
                yield return Capture("resumed-three");
                game.DispatchCommand("next");
                CheckThat("resume-next-advances-exactly-one", game.CurrentView.revealedCount == 4, "Absolute persisted target 4.");
                game.DispatchCommand("skip");
                CheckThat("skip-results-award-once", game.CurrentView.stage == "results" && game.CurrentSession.TotalVirtualCards == 11, "Completion does not award again.");
                string tx = game.CurrentView.transactionId;
                for (int i = 0; i < 8; i++) { game.DispatchCommand("skip"); game.DispatchCommand("next"); game.DispatchCommand("open"); }
                CheckThat("repeated-completion-input-is-inert", game.CurrentSession.TotalVirtualCards == 11 && game.CurrentView.transactionId == tx && game.GeneratedResultCount == 0, "Repeated already-complete UI input cannot reroll or award.");
                yield return Capture("results-first");
                game.DispatchCommand("inspect:7");
                CheckThat("holo-inspection-opens", game.CurrentView.inspectIndex == 7, "Inspect saved card in original position 8.");
                yield return Capture("inspection-holo");
                game.DispatchCommand("close-inspect");
                game.GenerateForSmoke(4u);
                CheckThat("next-pack-is-a-new-transaction", game.CurrentView.transactionId != tx && game.CurrentSession.TotalVirtualCards == 22 && game.GeneratedResultCount == 1, "One new generated pack only.");
                foreach (bool isNew in game.CurrentSession.CurrentPack.NewToVirtual)
                    CheckThat("repeat-pack-keeps-duplicate-status", !isNew, "Same deliberate seed is unboosted; all printings were already awarded.");
                game.DispatchCommand("generate");
                CheckThat("sealed-pack-blocks-double-generation", game.GeneratedResultCount == 1 && game.CurrentSession.TotalVirtualCards == 22, "Repeated Generate while a pack is incomplete is ignored.");
                game.DispatchCommand("open"); game.DispatchCommand("skip");
                yield return Capture("results-duplicates");
                yield break;
            }
            CheckThat("startup-does-not-generate", game.GeneratedResultCount == 0 && game.CurrentSession.Packs.Count == 0 && game.CurrentView.stage == "ready", "Fresh isolated directory starts ready.");
            yield return Capture("ready");
            if (scenario == "ready") yield break;
            if (scenario == "error")
            {
                game.FailNextCommitForSmoke();
                game.GenerateForSmoke(4u);
                CheckThat("prepared-write-failure-blocks-presentation", game.CurrentView.stage == "error" && game.GeneratedResultCount == 1, "Injected actual persistence after-prepare failure.");
                yield return Capture("save-error");
                game.DispatchCommand("generate");
                CheckThat("error-blocks-fresh-generation", game.GeneratedResultCount == 1, "No reroll while outcome is uncertain.");
                game.DispatchCommand("retry");
                CheckThat("retry-recovers-original-generation-once", game.CurrentView.stage == "sealed" && game.CurrentSession.TotalVirtualCards == 11 && game.GeneratedResultCount == 1 && game.CurrentSession.CurrentPack.Seed == 4u, "Same prepared transaction recovered and idempotently acknowledged.");
                yield return Capture("save-recovered");
                game.DispatchCommand("open"); game.DispatchCommand("next");
                CheckThat("recovered-pack-continues", game.CurrentView.revealedCount == 1 && game.CurrentSession.TotalVirtualCards == 11, "Further actions persist normally.");
                yield break;
            }
            game.GenerateForSmoke(4u);
            CheckThat("generation-durable-before-wrapper", game.CurrentView.stage == "sealed" && game.CurrentSession.TotalVirtualCards == 11 && game.CurrentSession.CurrentPack.RevealedCount == 0, "Full pack and virtual transaction committed.");
            yield return Capture("sealed");
            game.DispatchCommand("open");
            CheckThat("wrapper-open-is-durable", game.CurrentSession.CurrentPack.WrapperOpened && game.CurrentView.stage == "revealing", "Open committed before cue.");
            yield return new WaitForSecondsRealtime(0.65f);
            for (int i = 0; i < 3; i++) { game.DispatchCommand("next"); yield return null; }
            CheckThat("three-ordered-reveals", game.CurrentView.revealedCount == 3 && game.CurrentSession.TotalVirtualCards == 11, "Reveal does not generate or re-award.");
            yield return Capture("revealing-three");
            game.DispatchCommand("inspect:1");
            CheckThat("revealed-card-inspection", game.CurrentView.inspectIndex == 1, "Saved second card only.");
            yield return Capture("inspection");
            game.DispatchCommand("close-inspect");
            game.DispatchCommand("pause");
            CheckThat("pause-is-explicit", game.CurrentView.paused, "Pause does not alter persistent progress.");
            yield return Capture("pause");
            game.DispatchCommand("resume");
            if (scenario == "layout")
            {
                game.DispatchCommand("skip");
                yield return Capture("results");
                game.DispatchCommand("inspect:7");
                yield return Capture("inspection-holo");
                game.DispatchCommand("close-inspect");
            }
            CheckThat("only-one-result-generated", game.GeneratedResultCount == 1 && game.CurrentSession.TotalVirtualCards == 11, "No animation/inspection/pause generation.");
        }

        private IEnumerator Capture(string name)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(paths.ArtifactDirectory, name + ".png");
            ScreenCapture.CaptureScreenshot(file);
            for (int i = 0; i < 4; i++) yield return null;
            CheckThat("screenshot:" + name, File.Exists(file) && new FileInfo(file).Length > 0, "Graphics-enabled player capture.");
            screenshots.Add(name + ".png");
        }

        private void Update()
        {
            if (!active || finished || paths.SmokeScenario != "input") return;
            Probe current = Snapshot();
            string json = JsonUtility.ToJson(current, true);
            if (json == lastProbe) return;
            try
            {
                AtomicText(Path.Combine(paths.ArtifactDirectory, "probe.json"), json);
                lastProbe = json;
                string image = "input-" + screenshots.Count.ToString("D3") + ".png";
                ScreenCapture.CaptureScreenshot(Path.Combine(paths.ArtifactDirectory, image));
                screenshots.Add(image);
            }
            catch (IOException) { /* A test observer may briefly hold the report; retry the same observation next frame. */ }
        }

        private Probe Snapshot()
        {
            var view = game.CurrentView;
            var session = game.CurrentSession;
            var pack = session == null ? null : session.CurrentPack;
            var l = PresentationRules.Layout(Screen.width, Screen.height);
            var controls = new List<Control>();
            AddControl(controls, "primary", l.primary, l.scale);
            AddControl(controls, "secondary", l.secondary, l.scale);
            AddControl(controls, "pause", l.pause, l.scale);
            AddControl(controls, "quit", l.quit, l.scale);
            AddControl(controls, "resume", l.resume, l.scale);
            AddControl(controls, "back", l.back, l.scale);
            AddControl(controls, "hero", l.heroCard, l.scale);
            for (int i = 0; i < l.resultsCards.Length; i++) AddControl(controls, "result-" + i, l.resultsCards[i], l.scale);
            var ids = pack == null ? new string[0] : new List<string>(pack.OrderedCardIds).ToArray();
            var flags = pack == null ? new bool[0] : new List<bool>(pack.NewToVirtual).ToArray();
            return new Probe {
                stage = view == null ? "uninitialized" : view.stage,
                transactionId = view == null ? "" : view.transactionId,
                revealedCount = view == null ? 0 : view.revealedCount,
                totalVirtualCards = view == null ? 0 : view.totalVirtualCards,
                paused = view != null && view.paused, inspectIndex = view == null ? -1 : view.inspectIndex,
                generatedResultCount = game.GeneratedResultCount, lastCommand = game.LastCommand,
                revision = session == null ? 0 : session.Revision, modelVersion = pack == null ? "" : pack.ModelVersion,
                screenWidth = Screen.width, screenHeight = Screen.height, cardIds = ids, newToVirtual = flags,
                controls = controls.ToArray()
            };
        }
        private static void AddControl(List<Control> controls, string name, PresentationBox box, float scale)
        {
            controls.Add(new Control { name = name, x = box.x * scale, y = box.y * scale, width = box.width * scale, height = box.height * scale });
        }
        private void CheckThat(string name, bool passed, string detail)
        {
            checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (!passed) throw new InvalidOperationException(name + ": " + detail);
        }
        private static void AtomicText(string path, string text)
        {
            string temporary = path + ".writing";
            File.WriteAllText(temporary, text);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        private void Finish(bool quit)
        {
            if (finished || !active) return;
            finished = true;
            bool passed = checks.Count > 0 && checks.TrueForAll(check => check.passed);
            var report = new Report { passed = passed, checkCount = checks.Count, checks = checks.ToArray(),
                scenario = paths.SmokeScenario, initial = initial, final = Snapshot(), screenshots = screenshots.ToArray() };
            AtomicText(Path.Combine(paths.ArtifactDirectory, "report.json"), JsonUtility.ToJson(report, true));
            if (quit) Application.Quit(passed ? 0 : 1);
        }
        private void OnApplicationQuit()
        {
            if (active && paths.SmokeScenario == "input") Finish(false);
        }
    }
}
