"""Three-blocker regressions. Only synthetic Saves; no live storage or player launch."""
import argparse
import hashlib
import importlib
import json
import os
from pathlib import Path
import re
import sys
import unittest
from unittest.mock import patch
import uuid
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'tools'))

class ReviewFixChecks(unittest.TestCase):
    def test_saves_pruned_before_traversal_and_read_by_both_callers(self):
        pure = importlib.import_module('run_phase3_media')
        native = importlib.import_module('run_phase3_media_native')
        base = OUT/'synthetic-root'
        base.mkdir(parents=True, exist_ok=True)
        expected = base/'Builds/Phase2Windows/player.bin'
        expected.parent.mkdir(parents=True, exist_ok=True)
        expected.write_bytes(b'original synthetic baseline')
        # Windows case-insensitivity means variants must have distinct parent directories.
        for i, spelling in enumerate(['Saves','saves','sAvEs','SAVES']):
            p = expected.parent/str(i)/spelling/'nested/canary.save'
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(b'original synthetic save - must not be opened')
        original_read, original_scan = Path.read_bytes, os.scandir
        for caller in [pure, native]:
            reads, scans = [], []
            def read(p):
                reads.append(str(p))
                return original_read(p)
            def scan(p):
                scans.append(str(p))
                return original_scan(p)
            with patch.object(pure, 'ROOT', base), patch.object(Path,'read_bytes',read), patch.object(os,'scandir',scan):
                result = caller.protected()
            unsafe_reads = [p for p in reads if 'saves' in [s.lower() for s in Path(p).parts]]
            unsafe_scans = [p for p in scans if 'saves' in [s.lower() for s in Path(p).parts]]
            evidence = {'caller':caller.__name__, 'keys':list(result), 'reads':reads, 'scans':scans, 'unsafeReads':unsafe_reads, 'unsafeScans':unsafe_scans}
            (OUT/(caller.__name__+'-spy.json')).write_text(json.dumps(evidence,indent=2),encoding='utf-8')
            with self.subTest(caller=caller.__name__):
                self.assertEqual([],unsafe_scans,'Saves traversed before pruning')
                self.assertEqual([],unsafe_reads,'Saves contents read before pruning')
                self.assertEqual({str(expected.relative_to(base)):hashlib.sha256(b'original synthetic baseline').hexdigest()},result)

    def test_scene_script_guids_resolve_uniquely_to_source(self):
        scene = ROOT/'Assets/VintageCollector/Scenes/BootstrapPhase3.unity'
        guids = re.findall(r'm_Script:.*guid: ([0-9a-f]{32})',scene.read_text(encoding='utf-8'))
        mapping = {}
        for p in (ROOT/'Assets').rglob('*.cs.meta'):
            match = re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(encoding='utf-8'),re.M)
            if match:
                mapping.setdefault(match[1],[]).append(p.relative_to(ROOT).as_posix())
                self.assertTrue(p.with_suffix('').is_file(),'MonoScript metadata must have its source file: '+str(p))
        resolved = {g:mapping.get(g,[]) for g in guids}
        (OUT/'scene-guids.json').write_text(json.dumps(resolved,indent=2),encoding='utf-8')
        self.assertGreater(len(guids),0)
        self.assertTrue(all(len(v)==1 for v in resolved.values()),str(resolved))
        for name in ['Phase3MediaHost.cs.meta','Phase3MediaProbe.cs.meta']:
            source=ROOT/'Assets/VintageCollector/Runtime'/name
            stage=ROOT/'.artifacts/phase3-build-project/Assets/VintageCollector/Runtime'/name
            self.assertEqual(stage.read_bytes(),source.read_bytes())

if __name__ == '__main__':
    ap=argparse.ArgumentParser(__doc__)
    ap.add_argument('--test',choices=['saves','meta'],required=True)
    ap.add_argument('--label',required=True)
    args=ap.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label): ap.error('unsafe label')
    OUT=ROOT/'.artifacts/phase3-review-fixes'/args.label
    OUT.mkdir(parents=True,exist_ok=False)
    pins = ['TestsPhase3/ReviewFixChecks.py','tools/run_phase3_media.py','tools/run_phase3_media_native.py',
            'Assets/VintageCollector/Scenes/BootstrapPhase3.unity',
            'Assets/VintageCollector/Runtime/Phase3MediaHost.cs.meta',
            'Assets/VintageCollector/Runtime/Phase3MediaProbe.cs.meta']
    (OUT/'source-pins.json').write_text(json.dumps({p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
        for p in pins if (ROOT/p).is_file()},indent=2),encoding='utf-8')
    method='test_saves_pruned_before_traversal_and_read_by_both_callers' if args.test=='saves' else 'test_scene_script_guids_resolve_uniquely_to_source'
    with (OUT/'execution.log').open('w',encoding='utf-8') as log:
        result=unittest.TextTestRunner(stream=log,verbosity=2).run(unittest.TestSuite([ReviewFixChecks(method)]))
        code=0 if result.wasSuccessful() else 1
        log.write('\nEXIT '+str(code)+'\n')
    report={'passed':result.wasSuccessful(),'testCount':result.testsRun,'failures':len(result.failures),'errors':len(result.errors),'exit':code}
    (OUT/'results.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report))
    sys.exit(code)
