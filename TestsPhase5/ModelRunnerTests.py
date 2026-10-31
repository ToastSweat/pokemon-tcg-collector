"""CLI proof runner vertical tests. No Unity/editor/player invocation."""
from pathlib import Path
import importlib.util
import json
import subprocess
import sys
import unittest
import uuid
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]


class ModelRunnerTests(unittest.TestCase):
    def test_real_domain_compiles_separately_and_generates_approved_diagnostics(self):
        runner = ROOT / 'tools/run_phase5_models.py'
        self.assertTrue(runner.is_file(), 'Missing standalone approved-model proof runner')
        label = 'smoke-' + uuid.uuid4().hex
        p = subprocess.run([sys.executable, '-B', str(runner), '--label', label, '--packs', '0'], capture_output=True, text=True, timeout=180)
        self.assertEqual(0, p.returncode, p.stdout + p.stderr)
        report = json.loads((ROOT / '.artifacts/phase5-models' / label / 'report.json').read_text())
        self.assertTrue(report['passed'])
        self.assertTrue(report['domainCompiledSeparately'])
        self.assertTrue(report['inputsUnchanged'])
        self.assertTrue(report['preservationUnchanged'])
        self.assertGreater(report['csharpChecks'], 0)
        self.assertEqual(2, len(report['sets']))
        for result in report['sets']:
            self.assertEqual(11, len(result['fixedDiagnostics'][0]['orderedCardIds']))
            self.assertTrue(result['passed'])
    def test_all_real_weighted_boundaries_branches_and_immutable_ids_are_exercised(self):
        runner = ROOT / 'tools/run_phase5_models.py'
        self.assertTrue(runner.is_file())
        label = 'boundaries-' + uuid.uuid4().hex
        p = subprocess.run([sys.executable, '-B', str(runner), '--label', label, '--packs', '0'], capture_output=True, text=True, timeout=180)
        self.assertEqual(0, p.returncode, p.stdout + p.stderr)
        proof = json.loads((ROOT / '.artifacts/phase5-models' / label / 'csharp-proof.json').read_text())
        for set_id in ['jungle', 'fossil']:
            prefix = set_id + '-en-unlimited1999:'
            for name in ['every-weighted-ticket-after-prefix-removal', 'both-alternative-branches-all-thresholds', 'immutable-detached-results-and-inputs', 'foreign-slot-eligibility-and-collector-eight', 'hostile-rng-no-generator-state-leak']:
                self.assertIn(prefix + name, proof['checks'], 'Missing executed production-generator proof: ' + name)
        for result in proof['sets']:
            self.assertGreater(result['boundaryCases'], 1000)
    def test_full_diagnostic_sampling_uses_weighted_wor_expectations_and_separate_replay(self):
        runner = ROOT / 'tools/run_phase5_models.py'
        self.assertTrue(runner.is_file())
        label = 'full-' + uuid.uuid4().hex
        p = subprocess.run([sys.executable, '-B', str(runner), '--label', label, '--packs', '100000'], capture_output=True, text=True, timeout=300)
        self.assertEqual(0, p.returncode, p.stdout + p.stderr)
        report = json.loads((ROOT / '.artifacts/phase5-models' / label / 'report.json').read_text())
        self.assertIn('samplingPolicy', report, 'Missing declared statistical experiment and actual 100000-pack diagnostics')
        self.assertEqual(100000, report['samplingPolicy']['statisticalTrialsPerSet'])
        self.assertTrue(report['deterministicRerunMatched'])
        for result in report['sets']:
            self.assertEqual(100000, result['sample']['packs'])
            self.assertEqual(1100000, result['sample']['cardCount'])
            self.assertTrue(result['statistics']['passed'])
            self.assertGreater(result['statistics']['checkCount'], 100)
            self.assertEqual(result['statistics']['checkCount'], len(result['statistics']['checks']))
            self.assertEqual(64 if result['printingId'].startswith('jungle') else 62, len(result['sample']['counts']))
            self.assertEqual(64, len(result['sample']['completeOrderedDiagnosticSha256']))
        (ROOT / '.artifacts/phase5-models/latest-full-test.json').write_text(json.dumps({'label': label, 'report': str((ROOT / '.artifacts/phase5-models' / label / 'report.json').relative_to(ROOT))}, indent=2), encoding='utf-8')


if __name__ == '__main__':
    unittest.main(verbosity=2)
