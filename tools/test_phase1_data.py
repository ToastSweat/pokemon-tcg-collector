"""Regression checks for the source-to-runtime authoring boundary; project-local fixtures only."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / 'Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json'
MODEL = ROOT / 'Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json'
VALIDATOR = ROOT / 'tools/verify_phase1_data.py'


class DataAuthoringTests(unittest.TestCase):
    def mutated_report(self, mutate):
        catalog = json.loads(CATALOG.read_text(encoding='utf-8'))
        model = json.loads(MODEL.read_text(encoding='utf-8'))
        mutate(catalog, model)
        fixtures = ROOT / '.artifacts/data-authoring-fixtures'
        fixtures.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=fixtures) as folder:
            cpath, mpath = Path(folder) / 'catalog.json', Path(folder) / 'model.json'
            cpath.write_text(json.dumps(catalog), encoding='utf-8')
            mpath.write_text(json.dumps(model), encoding='utf-8')
            result = subprocess.run([sys.executable, '-B', str(VALIDATOR), '--catalog', str(cpath),
                                     '--model', str(mpath)], cwd=ROOT, capture_output=True,
                                    text=True, encoding='utf-8')
            report = json.loads(result.stdout)
            self.assertEqual(result.returncode, 0 if report['passed'] else 1, result.stderr)
            return report

    def test_printing_specific_name_cannot_drift_back_to_source_typo(self):
        def mutate(catalog, model):
            card = next(c for c in catalog['cards'] if c['collectorNumber'] == '73')
            card['displayName'] = card['sourceName']
        report = self.mutated_report(mutate)
        self.assertFalse(report['passed'])
        self.assertIn('catalog_source_mapping', [c['name'] for c in report['checks'] if not c['passed']])

    def test_rule_mutations_cannot_silently_change_the_approved_model(self):
        def changed_weights(catalog, model):
            entries = next(p for p in model['pools'] if p['id'] == 'holo')['entries']
            entries[0]['weight'] += 1
            entries[1]['weight'] -= 1
        def changed_order(catalog, model):
            model['slots'][0], model['slots'][5] = model['slots'][5], model['slots'][0]
        def changed_holo(catalog, model):
            model['slots'][7]['alternativeDenominator'] = 2
        def changed_duplicates(catalog, model):
            next(p for p in model['pools'] if p['id'] == 'common')['uniqueWithinPack'] = False
        def false_certainty(catalog, model):
            model['status'] = 'historically-exact'
        def unsupported_schema(catalog, model):
            model['schemaVersion'] = 2
        for mutate in (changed_weights, changed_order, changed_holo, changed_duplicates,
                       false_certainty, unsupported_schema):
            with self.subTest(mutation=mutate.__name__):
                self.assertFalse(self.mutated_report(mutate)['passed'])

    def test_unanswered_completion_target_cannot_be_implicitly_approved(self):
        report = self.mutated_report(lambda catalog, model: catalog.update(
            completionTargetStatus='approved-102-including-machamp'))
        self.assertFalse(report['passed'])

    def test_unsupported_catalog_schema_is_rejected(self):
        report = self.mutated_report(lambda catalog, model: catalog.update(schemaVersion=2))
        self.assertFalse(report['passed'])

    def test_real_authored_data_matches_approved_source_mapping(self):
        result = subprocess.run([sys.executable, '-B', str(VALIDATOR)], cwd=ROOT,
                                capture_output=True, text=True, encoding='utf-8')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        report = json.loads(result.stdout)
        self.assertTrue(report['passed'])
        self.assertEqual(report['cardCount'], 101)
        self.assertEqual(report['sourceChecklistCount'], 102)
        self.assertEqual(report['completionTargetStatus'], 'unresolved-machamp-exception')
        self.assertEqual(report['modelVersion'], '0.1.0')
        self.assertEqual(report['poolSizes'], {'holo': 15, 'rare': 16, 'uncommon': 32,
                                               'common': 32, 'energy': 6})
        self.assertEqual(report['poolWeights'], {'holo': 110, 'rare': 121, 'uncommon': 121,
                                                 'common': 121, 'energy': 121})


if __name__ == '__main__':
    unittest.main(verbosity=2)
