"""BUILD-01/PLAYER-01 retained exact-source regressions; managed scheduling doubles are not native evidence."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
import unittest
from typing import Any

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
OUT: Path
LABEL: str
EVIDENCE: list[dict[str, Any]] = []


def digest(data):
    return hashlib.sha256(data).hexdigest()


def files(root):
    return {p.relative_to(root).as_posix(): digest(p.read_bytes())
            for p in root.rglob('*') if p.is_file()}


def runner() -> Any:
    spec = importlib.util.spec_from_file_location('review_build', ROOT / 'tools/run_reveal_audio_build.py')
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class BuildRefusal(unittest.TestCase):
    def test_all_compiler_roots_refuse_undeclared_code_without_any_mutation(self):
        variants = [('Packages/com.example.stale/Editor/Surprise.cs', '.cs'),
                    ('Packages/com.example.stale/Surprise.cs', '.cs')]
        variants += [(root + '/Logs/Saves/Surprise' + ext, ext)
                     for root in ('Assets', 'Packages', 'ProjectSettings')
                     for ext in ('.cs', '.asmdef', '.asmref', '.dll', '.rsp')]
        for index, (relative, ext) in enumerate(variants):
            with self.subTest(path=relative):
                fixture = OUT / ('stale-' + str(index))
                stage = fixture / 'stage'
                content = b'class Surprise {}' if ext == '.cs' else b'retained stale compiler input'
                extra = stage / relative
                extra.parent.mkdir(parents=True)
                extra.write_bytes(content)
                package = stage / 'Packages/com.example.stale/package.json'
                package.parent.mkdir(parents=True, exist_ok=True)
                package.write_text('{"name":"com.example.stale","version":"1.0.0"}')
                (stage / '.reveal-audio-owned-stage.json').write_text(json.dumps({
                    'purpose': 'reveal-audio-exact-source', 'source': str(fixture)}))
                (fixture / 'staged-inputs.json').write_bytes(b'retained previous declaration')
                before = files(fixture)
                # Keep the actual pre-call bytes even on historical defective-code RED runs.
                retained = OUT / ('before-bytes-' + str(index))
                for path in tuple(fixture.rglob('*')):
                    if path.is_file():
                        target = retained / path.relative_to(fixture)
                        target.parent.mkdir(parents=True, exist_ok=True)
                        target.write_bytes(path.read_bytes())
                m = runner()
                m.ROOT = fixture
                m.STAGE = stage
                rejection = None
                try:
                    m.stage_exact({}, fixture, restage=True)
                except ValueError as error:
                    rejection = str(error)
                after = files(fixture)
                EVIDENCE.append({'path': relative, 'before': before, 'after': after,
                                 'rejection': rejection, 'allBytesPreserved': before == after})
                self.assertIsNotNone(rejection, 'undeclared compiler code must refuse before restage: ' + relative)
                self.assertEqual(before, after, 'refusal must preserve every fixture byte')

    def test_native_generated_folder_meta_remains_admissible(self):
        fixture = OUT / 'folder-meta'
        stage = fixture / 'stage'
        (stage / 'Assets/GeneratedFolder').mkdir(parents=True)
        (stage / 'Assets/GeneratedFolder.meta').write_text('fileFormatVersion: 2\nguid: 12345678901234567890123456789012\nfolderAsset: yes\n')
        (stage / '.reveal-audio-owned-stage.json').write_text(json.dumps({
            'purpose': 'reveal-audio-exact-source', 'source': str(fixture)}))
        m = runner()
        m.ROOT = fixture
        m.STAGE = stage
        m.stage_exact({}, fixture, restage=True)
        self.assertEqual(json.loads((fixture / 'staged-inputs.json').read_text())['files'], {})


class PlayerOrder(unittest.TestCase):
    def test_exact_start_prefix_initializes_then_fences_and_normal_startup_is_inert(self):
        runtime = ROOT / 'Assets/VintageCollector/Runtime'
        names = ['Phase4CollectionHost', 'RevealAudioSmokeProbe', 'TiltSmokeProbe', 'MaterialsSmokeProbe', 'Phase4SmokeProbe']
        sources = {name: (runtime / (name + '.cs')).read_text() for name in names}
        orders = {name: int(m[1]) if (m := re.search(r'DefaultExecutionOrder\((-?\d+)\)', text)) else 0
                  for name, text in sources.items()}
        host = sources['Phase4CollectionHost']
        host_match = re.search(r'private void Start\(\) \{ Initialize\(Environment.GetCommandLineArgs\(\)\); \}', host)
        assert host_match is not None
        host_start = host_match[0]
        # Pin the real synchronous initialization dependency, without changing or substituting host source.
        init = host.split('public void Initialize(string[] args) {', 1)[1].split('private void RefreshBackups()', 1)[0]
        self.assertNotIn('yield ', init)
        self.assertIn('Game.InitializeForProfile(args,Profiles.ActiveProfileId);', init)
        game = (runtime / 'OpeningGame.cs').read_text()
        game_init = game.split('public void InitializeForProfile(string[] args, string profileId)', 1)[1].split('private void OpenSession()', 1)[0]
        self.assertNotIn('yield ', game_init)
        self.assertIn('RunStorage(delegate {', game_init)
        self.assertIn('OpenSession();', game_init)
        storage = game.split('private void RunStorage(Action action)', 1)[1].split('public void DispatchCommand', 1)[0]
        self.assertIn('try { action(); }', storage)
        session = game.split('private void OpenSession()', 1)[1].split('private void RunStorage', 1)[0]
        self.assertIn('if (Paths == null) Paths = GamePaths.Resolve', session)
        for name in names:
            meta = (runtime / (name + '.cs.meta')).read_text()
            override = re.search(r'^executionOrder: (-?\d+)', meta, re.M)
            self.assertTrue(override is None or int(override[1]) == 0, 'unexpected script meta order override')
        text = sources['RevealAudioSmokeProbe']
        admission = text.split('  public static string ValidateOptIn', 1)[1].split('  OpeningGame game;', 1)[0]
        prefix = text.split('  IEnumerator Start(){', 1)[1].split('   directory=game.Paths.ArtifactDirectory;', 1)[0]
        constant_match = re.search(r'  const string Root=.*?;', text)
        assert constant_match is not None
        constant = constant_match[0]
        audio = ROOT / '.artifacts/reveal-audio-player' / LABEL / 'audio'
        audio.mkdir(parents=True)
        (audio / 'reveal-audio-fixture.json').write_text('{"schemaVersion":1,"owner":"reveal-audio-player-fixture","mode":"audio"}')
        def cs(s):
            return json.dumps(str(s))
        harness = '''using System;using System.IO;using System.Collections;using System.Collections.Generic;
public class GamePaths {public bool IsSmokeTest; public string ArtifactDirectory,BaseSaveDirectory,SmokeScenario;}
public class OpeningGame {public GamePaths Paths;}
public class MonoBehaviour {public bool enabled=true;}
public class Phase4SmokeProbe:MonoBehaviour {} public class TiltSmokeProbe:MonoBehaviour {} public class MaterialsSmokeProbe:MonoBehaviour {}
public static class Debug {public static void LogError(Exception e){throw e;}}
public static class Application {public static void Quit(int n){throw new Exception("Unexpected quit");}}
// Explicit scheduling/component doubles. Probe admission, early-exit and fencing are exact extracted source.
public class Probe:MonoBehaviour {
public OpeningGame supplied; OpeningGame game; string mode;
public MonoBehaviour[] components;
public T GetComponent<T>() where T:class {return supplied as T;}
public T[] GetComponents<T>() where T:MonoBehaviour {return Array.ConvertAll(components, x=>(T)x);}
''' + constant + '\n public static string ValidateOptIn' + admission + '\n public IEnumerator Start(){' + prefix + '\n yield return "admitted"; }}\n' + '''
public class Host {public OpeningGame Game;public GamePaths Supplied; public void Initialize(string[] args){Game.Paths=Supplied;}
''' + host_start + ''' public void Run(){Start();}}
public static class Harness {
static void Check(string n,bool value){Console.WriteLine((value?"PASS ":"FAIL ")+n);if(!value)throw new Exception(n);}
static Probe Make(OpeningGame game){var p=new Probe{supplied=game};p.components=new MonoBehaviour[]{p,new Phase4SmokeProbe(),new TiltSmokeProbe(),new MaterialsSmokeProbe()};return p;}
public static int Main(){try{
var paths=new GamePaths {IsSmokeTest=true,ArtifactDirectory=AUDIO,BaseSaveDirectory=SAVE,SmokeScenario="input"};
var g=new OpeningGame();var p=Make(g);var h=new Host{Game=g,Supplied=paths};
var actions=new List<Tuple<int,int,Action>>();bool admitted=false;int competingStarts=0;
// Deliberately adversarial registration order: probe precedes host if execution orders tie.
actions.Add(Tuple.Create(PROBE,0,(Action)(()=>{admitted=p.Start().MoveNext();})));
actions.Add(Tuple.Create(HOST,1,(Action)(()=>h.Run())));
for(int i=1;i<p.components.Length;i++){var c=p.components[i];int o=c is TiltSmokeProbe?TILT:c is MaterialsSmokeProbe?MATERIALS:PHASE4;actions.Add(Tuple.Create(o,i+1,(Action)(()=>{if(c.enabled)competingStarts++;})));}
actions.Sort((a,b)=>a.Item1!=b.Item1?a.Item1.CompareTo(b.Item1):a.Item2.CompareTo(b.Item2));foreach(var a in actions)a.Item3();
Check("strict-host-audio-other-probes-order",HOST<PROBE&&PROBE<TILT&&PROBE<MATERIALS&&PROBE<PHASE4);
Check("paths-initialized-before-probe",g.Paths==paths&&admitted);
Check("all-competing-probes-fenced-before-start",competingStarts==0);
var ordinary=Make(new OpeningGame{Paths=new GamePaths{IsSmokeTest=false}});Check("ordinary-startup-inert",!ordinary.Start().MoveNext());
Check("ordinary-startup-does-not-fence",Array.TrueForAll(ordinary.components,c=>c.enabled));
var missing=Make(new OpeningGame());Check("missing-host-does-not-enable-automation",!missing.Start().MoveNext());
Check("ordinary-startup-no-marker-required",Probe.ValidateOptIn(new GamePaths{IsSmokeTest=false})==null);
Console.WriteLine("TEST_COUNT 7");return 0;
}catch(Exception e){Console.WriteLine(e);return 1;}}}
'''
        for key, value in {'AUDIO': cs(audio), 'SAVE': cs(audio.parent / 'session'),
                           'HOST': str(orders['Phase4CollectionHost']), 'PROBE': str(orders['RevealAudioSmokeProbe']),
                           'TILT': str(orders['TiltSmokeProbe']), 'MATERIALS': str(orders['MaterialsSmokeProbe']),
                           'PHASE4': str(orders['Phase4SmokeProbe'])}.items():
            harness = re.sub(r'\b' + key + r'\b', lambda _: value, harness)
        (OUT / 'StartOrderHarness.cs').write_text(harness)
        data = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
        commands = [[str(data / 'bin/mono.exe'), str(data / 'lib/mono/4.5/csc.exe'), '-nologo', '-warnaserror+',
                     '-out:' + str(OUT / 'start-order.exe'), str(OUT / 'StartOrderHarness.cs')],
                    [str(data / 'bin/mono.exe'), str(OUT / 'start-order.exe')]]
        evidence = {'kind': 'exact-source-prefix-and-admission-managed-scheduling-doubles-not-native',
                    'orders': orders, 'sourcePins': {name: digest(source.encode()) for name, source in sources.items()}, 'commands': []}
        EVIDENCE.append(evidence)
        for command in commands:
            result = subprocess.run(command, capture_output=True, text=True, timeout=60)
            evidence['commands'].append({'command': command, 'exit': result.returncode, 'stdout': result.stdout, 'stderr': result.stderr})
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        stdout = evidence['commands'][-1]['stdout']
        self.assertEqual(len(re.findall(r'^PASS ', stdout, re.M)), 7)
        self.assertIn('TEST_COUNT 7', stdout)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--label', required=True)
    parser.add_argument('--suite', choices=['build', 'player'], required=True)
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+', args.label):
        parser.error('Unsafe label')
    LABEL = args.label
    OUT = ROOT / '.artifacts/reveal-audio-fixes' / LABEL
    OUT.mkdir(parents=True, exist_ok=False)
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(BuildRefusal if args.suite == 'build' else PlayerOrder)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    (OUT / 'results.json').write_text(json.dumps({'passed': result.wasSuccessful(), 'testCount': result.testsRun,
        'failureCount': len(result.failures), 'errorCount': len(result.errors), 'evidence': EVIDENCE}, indent=2))
    sys.exit(0 if result.wasSuccessful() else 1)
