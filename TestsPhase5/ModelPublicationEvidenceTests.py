"""MODEL-REPRO-001: real disposable bytes, never repin historical receipts."""
from contextlib import ExitStack
from pathlib import Path
import hashlib
import copy
import importlib.util
import json
import shutil
import sys
import unittest
import uuid
from unittest.mock import patch
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
import prepare_phase5_models as authoring  # pyright: ignore[reportMissingImports]

PUBLICATION = 'docs/technical/Phase_5_Data_Preparation.md'
HISTORICAL = 'fc4685b96b932cc805ceacd81a71b61fa19657c78647a13aec637f4f70f05f5e'


class ModelPublicationEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.fixture = ROOT / '.artifacts/phase5-model-repro-fix/fixtures' / uuid.uuid4().hex
        self.fixture.mkdir(parents=True)
        provenance = json.loads((ROOT / 'Authoring/Phase5/ApprovedData/Provenance.json').read_text())
        review = json.loads((ROOT / '.artifacts/phase5-review/pins.json').read_text())
        names = set(provenance['inputs']) | set(review)
        names.update(p.relative_to(ROOT).as_posix() for p in (ROOT / 'Authoring/Phase5/ApprovedData').iterdir())
        for name in names:
            target = self.fixture / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / name, target)
        self.stack = ExitStack()
        self.addCleanup(self.stack.close)
        for name, value in dict(ROOT=self.fixture, APPROVED_ROOT=self.fixture,
                                APPROVED_DIR=self.fixture / 'Authoring/Phase5/ApprovedData',
                                ARTIFACT_DIR=self.fixture / 'output').items():
            self.stack.enter_context(patch.object(authoring, name, value))

    def approved_snapshot(self):
        return {p.name: (p.read_bytes(), p.stat().st_mtime_ns) for p in authoring.APPROVED_DIR.iterdir()}

    def test_publication_only_updates_preserve_original_provenance_and_current_audit(self):
        before = self.approved_snapshot()
        path = self.fixture / PUBLICATION
        path.write_bytes(path.read_bytes() + b'\nPublication status fixture: complete.\n')
        docs = authoring.build_documents()
        self.assertEqual(HISTORICAL, docs['Provenance.json']['inputs'][PUBLICATION])
        self.assertEqual(before['Provenance.json'][0], authoring.document_bytes(docs)['Provenance.json'])
        observation = authoring.publication_observations()
        self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), observation[PUBLICATION])
        self.assertNotEqual(HISTORICAL, observation[PUBLICATION])
        self.assertNotIn(PUBLICATION, authoring.effective_source_pins())
        self.assertEqual(HISTORICAL, authoring.source_pins()[PUBLICATION])
        self.assertNotIn('.gitattributes', authoring.source_pins())
        self.assertEqual('unchanged', authoring.publish(docs, authoring.APPROVED_DIR))
        self.assertEqual(docs, authoring.read_approved(authoring.APPROVED_DIR))
        self.assertEqual(observation, authoring.publication_observations())
        self.assertEqual(before, self.approved_snapshot())

    def test_cli_detects_publication_observation_drift_without_repinning(self):
        before = self.approved_snapshot()
        real_read = authoring.read_approved
        def read_then_update():
            docs = real_read(authoring.APPROVED_DIR)
            path = self.fixture / PUBLICATION
            path.write_bytes(path.read_bytes() + b'\nPublication changed during check.\n')
            return docs
        with patch.object(sys, 'argv', ['prepare_phase5_models.py', '--check']), patch.object(authoring, 'read_approved', side_effect=read_then_update):
            with self.assertRaisesRegex(ValueError, 'Publication observation drift'):
                authoring.main()
        self.assertEqual(before, self.approved_snapshot())


    def test_generation_authority_drift_is_refused_before_any_publication_write(self):
        docs = authoring.build_documents()
        approved = self.approved_snapshot()
        guarded = [authoring.APPROVAL, authoring.WEIGHTS,
                   'Authoring/Phase5/Data/jungle-en-unlimited1999.catalog.json',
                   'Authoring/Phase5/Data/fossil-en-unlimited1999.catalog.json',
                   'docs/research/phase5/jungle/claim-register.md',
                   'Assets/VintageCollector/Domain/PackGenerator.cs']
        for index, name in enumerate(guarded):
            with self.subTest(path=name):
                path = self.fixture / name
                original = path.read_bytes()
                target = authoring.ARTIFACT_DIR / ('refused-' + str(index))
                try:
                    if path.suffix == '.json':
                        changed = json.loads(original)
                        if name == authoring.WEIGHTS:
                            changed['tables'][0]['cards'][0]['multiplicity'] += 1
                        elif name == authoring.APPROVAL:
                            changed['modelStatus'] = 'unapproved-fixture'
                        else:
                            changed['cards'][0]['name'] = 'Changed candidate fixture'
                        path.write_text(json.dumps(changed), encoding='utf-8')
                    else:
                        path.write_bytes(original + b'\nNumeric/source drift fixture.\n')
                    with self.assertRaisesRegex(ValueError, 'Frozen input drift|Approved configuration drift'):
                        authoring.publish(docs, target)
                    with self.assertRaisesRegex(ValueError, 'Frozen input drift|Approved configuration drift'):
                        authoring.read_approved(authoring.APPROVED_DIR)
                    self.assertFalse(target.exists())
                    self.assertEqual(approved, self.approved_snapshot())
                finally:
                    path.write_bytes(original)

    def test_historical_membership_cannot_be_removed_or_repinned(self):
        docs = authoring.build_documents()
        before = self.approved_snapshot()
        for mode in ['remove', 'repin']:
            with self.subTest(mode=mode):
                changed = copy.deepcopy(docs)
                inputs = changed['Provenance.json']['inputs']
                if mode == 'remove':
                    del inputs[PUBLICATION]
                else:
                    inputs[PUBLICATION] = authoring.sha(self.fixture / PUBLICATION)
                target = authoring.ARTIFACT_DIR / mode
                with self.assertRaisesRegex(ValueError, 'Approved configuration drift'):
                    authoring.publish(changed, target)
                self.assertFalse(target.exists())
                self.assertEqual(before, self.approved_snapshot())

    def fixture_runner(self):
        for name in ['tools/prepare_phase5_models.py', 'tools/run_phase5_models.py', 'TestsPhase5/ModelProof.cs']:
            target = self.fixture / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / name, target)
        spec = importlib.util.spec_from_file_location('publication_fixture_runner', self.fixture / 'tools/run_phase5_models.py')
        assert spec is not None and spec.loader is not None
        m = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(m)
        # The fixture has no live players/material. Feed the retained baseline only
        # for this observation-boundary test, not as a new live preservation claim.
        baseline = json.loads((self.fixture / '.artifacts/phase5-preservation/before.json').read_text())
        self.stack.enter_context(patch.object(m, 'preservation', return_value=baseline))
        self.stack.enter_context(patch.object(m, 'read_approved', side_effect=lambda: authoring.read_approved(authoring.APPROVED_DIR)))
        return m

    def test_real_csharp_runner_detects_publication_observation_drift_separately(self):
        m = self.fixture_runner()
        original_run = m.checked_run
        def run_then_update(command, out, stage, runs):
            result = original_run(command, out, stage, runs)
            if stage == 'generation':
                path = self.fixture / PUBLICATION
                path.write_bytes(path.read_bytes() + b'\nPublication changed during CSharp run.\n')
            return result
        before = self.approved_snapshot()
        with patch.object(sys, 'argv', ['run_phase5_models.py', '--label', 'observation-drift', '--packs', '0']), patch.object(m, 'checked_run', side_effect=run_then_update):
            self.assertEqual(1, m.main())
        report_path = authoring.ARTIFACT_DIR / 'observation-drift/report.json'
        report = json.loads(report_path.read_text())
        self.assertEqual(12, report['csharpChecks'])
        self.assertTrue(report['deterministicRerunMatched'])
        self.assertFalse(report['passed'])
        self.assertFalse(report['publicationObservationsUnchanged'])
        self.assertIn('Publication observation drift', report['failure'])
        self.assertTrue(report['inputsUnchanged'])
        self.assertFalse(report['reviewLiveUnchangedDuringRun'])
        inputs = json.loads((report_path.parent / 'inputs-before.json').read_text())
        self.assertNotIn(PUBLICATION, inputs)
        self.assertEqual(before, self.approved_snapshot())


if __name__ == '__main__':
    unittest.main(verbosity=2)
