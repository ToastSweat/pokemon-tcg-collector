"""Fail-closed runner tests with real Mono negative subprocesses, never alternate pack evidence."""
from pathlib import Path
import copy
import importlib.util
import json
import subprocess
import sys
import unittest
import uuid
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
import prepare_phase5_models as authoring
import run_phase5_models as runner


class ModelGuardTests(unittest.TestCase):
    def test_failed_compiler_process_cannot_be_accepted(self):
        self.assertTrue(hasattr(runner, 'checked_run'), 'Missing fail-closed execution/command-log boundary')
        out = ROOT / '.artifacts/phase5-models' / ('negative-compile-' + uuid.uuid4().hex)
        out.mkdir()
        runs = []
        command = [str(runner.MONO / 'bin/mono.exe'), str(runner.MONO / 'lib/mono/4.5/mcs.exe'), '-warnaserror+', '-out:' + str(out / 'never.exe'), str(out / 'missing-source.cs')]
        with self.assertRaisesRegex(ValueError, 'compiler'):
            runner.checked_run(command, out, 'compiler', runs)
        self.assertEqual(1, len(runs))
        self.assertNotEqual(0, runs[0]['exitCode'])
        self.assertIn('CS2001', runs[0]['stdout'] + runs[0]['stderr'])
        self.assertFalse((out / 'never.exe').exists())

    def test_real_child_runtime_zero_malformed_and_declared_count_mismatch_are_refused(self):
        self.assertTrue(hasattr(runner, 'accept_execution'), 'Missing reconciliation of real process stdout and proof file')
        docs = authoring.read_approved()
        out = ROOT / '.artifacts/phase5-models' / ('negative-child-' + uuid.uuid4().hex)
        out.mkdir()
        # First obtain an actual passing production-generator smoke proof.
        label = 'guard-baseline-' + uuid.uuid4().hex
        result = subprocess.run([sys.executable, '-B', str(ROOT / 'tools/run_phase5_models.py'), '--label', label, '--packs', '0'], capture_output=True, text=True, timeout=180)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        valid = json.loads((ROOT / '.artifacts/phase5-models' / label / 'csharp-proof.json').read_text())
        source = out / 'GuardChild.cs'
        source.write_text('using System; using System.IO; public static class GuardChild { public static int Main(string[] a) { File.Copy(a[0],a[1],false); Console.WriteLine(a[2]); return int.Parse(a[3]); } }', encoding='utf-8')
        runner.checked_run([str(runner.MONO / 'bin/mono.exe'), str(runner.MONO / 'lib/mono/4.5/mcs.exe'), '-warnaserror+', '-out:' + str(out / 'GuardChild.exe'), str(source)], out, 'fixture-compiler', [])
        modes = ['runtime-failure', 'zero-checks', 'malformed-json', 'declared-mismatch', 'stdout-zero', 'seed-duplication']
        for mode in modes:
            with self.subTest(mode=mode):
                proof = copy.deepcopy(valid)
                stdout = 'CHECKS ' + str(valid['checkCount']) + ' FAILED 0'
                if mode == 'zero-checks':
                    proof['checkCount'] = 0
                    proof['checks'] = []
                    stdout = 'CHECKS 0 FAILED 0'
                if mode == 'declared-mismatch':
                    proof['checkCount'] += 1
                if mode == 'stdout-zero':
                    stdout = 'CHECKS 0 FAILED 0'
                if mode == 'seed-duplication':
                    proof['sets'][0]['fixedDiagnostics'] = [proof['sets'][0]['fixedDiagnostics'][0]] * 7
                source_json = out / (mode + '.input.json')
                source_json.write_text('not JSON' if mode == 'malformed-json' else json.dumps(proof), encoding='utf-8')
                proof_path = out / (mode + '.output.json')
                runs = []
                command = [str(runner.MONO / 'bin/mono.exe'), str(out / 'GuardChild.exe'), str(source_json), str(proof_path), stdout, '7' if mode == 'runtime-failure' else '0']
                if mode == 'runtime-failure':
                    with self.assertRaisesRegex(ValueError, 'runtime-failure'):
                        runner.checked_run(command, out, mode, runs)
                    self.assertEqual(7, runs[0]['exitCode'])
                else:
                    actual = runner.checked_run(command, out, mode, runs)
                    self.assertEqual(0, actual['exitCode'])
                    with self.assertRaises(ValueError):
                        runner.accept_execution(actual, proof_path, docs)

    def test_exact_seed_and_json_count_types_are_required(self):
        label = 'type-guard-baseline-' + uuid.uuid4().hex
        result = subprocess.run([sys.executable, '-B', str(ROOT / 'tools/run_phase5_models.py'), '--label', label, '--packs', '0'], capture_output=True, text=True, timeout=180)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        valid = json.loads((ROOT / '.artifacts/phase5-models' / label / 'csharp-proof.json').read_text())
        docs = authoring.read_approved()
        self.assertGreater(runner.validate_proof(valid, docs), 0)
        for key, value in [('checkCount', float(valid['checkCount'])), ('failedCount', False)]:
            changed = copy.deepcopy(valid)
            changed[key] = value
            with self.subTest(key=key):
                with self.assertRaises(ValueError):
                    runner.validate_proof(changed, docs)
    def test_review_snapshot_separates_external_metadata_drift_from_generator_inputs(self):
        self.assertTrue(hasattr(runner, 'review_snapshot'), 'Missing independent review-pin audit')
        snapshot = runner.review_snapshot()
        pins = json.loads((ROOT / '.artifacts/phase5-review/pins.json').read_text())
        self.assertEqual(set(pins), set(snapshot))
        self.assertNotIn('.gitattributes', authoring.source_pins())
        for name, record in snapshot.items():
            self.assertEqual(pins[name]['live'], record['reviewedLiveSha256'])
            self.assertEqual(authoring.sha(ROOT / name), record['currentLiveSha256'])
        for forbidden in ['finalize_evidence.py', 'literal-evidence-quotes.json', 'verification-proof.json']:
            self.assertNotIn('docs/research/phase5/weight-supplement/' + forbidden, authoring.source_pins())


if __name__ == '__main__':
    unittest.main(verbosity=2)
