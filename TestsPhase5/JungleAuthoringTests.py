"""Tests use retained real source snapshots, not fictional card fixtures."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / 'docs/research/phase5/jungle'
spec = importlib.util.spec_from_file_location('jungle_authoring', ROOT / 'tools/prepare_jungle_catalog.py')
assert spec is not None and spec.loader is not None
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

class JungleAuthoringTests(unittest.TestCase):
    def test_parse_independent_bulbapedia_complete_membership(self):
        self.assertTrue(callable(getattr(module, 'parse_bulbapedia', None)), 'missing independent HTML membership parser')
        rows = module.parse_bulbapedia((EVIDENCE / 'bulbapedia.raw.html').read_text(encoding='utf-8'))
        self.assertEqual(list(range(1, 65)), [r['number'] for r in rows])
        self.assertEqual({'number': 1, 'name': 'Clefable', 'rarity': 'holo', 'kind': 'pokemon', 'finish': 'holo'}, rows[0])
        self.assertEqual({'number': 64, 'name': 'Poké Ball', 'rarity': 'common', 'kind': 'trainer', 'finish': 'nonholo'}, rows[-1])
        self.assertEqual('rare', rows[16]['rarity'])
        self.assertEqual('uncommon', rows[32]['rarity'])
        self.assertEqual('Nidoran♀', rows[56]['name'])

    def test_crosscheck_all_fields_against_independent_json_and_names_against_third_source(self):
        self.assertTrue(callable(getattr(module, 'crosscheck', None)), 'missing complete cross-source reconciliation')
        rows = module.crosscheck(EVIDENCE)
        self.assertEqual(64, len(rows))
        self.assertEqual('base2-64', rows[-1]['sourceId'])
        self.assertEqual({'number': 57, 'name': 'Nidoran♀', 'rarity': 'common', 'kind': 'pokemon', 'finish': 'nonholo', 'sourceId': 'base2-57'}, rows[56])

    def test_build_candidate_only_separates_policy_from_membership(self):
        self.assertTrue(callable(getattr(module, 'build_catalog', None)), 'missing candidate catalog builder')
        catalog = module.build_catalog(EVIDENCE)
        self.assertEqual(1, catalog['schemaVersion'])
        self.assertEqual('proposed-not-approved', catalog['completionTargetStatus'])
        self.assertEqual('jungle-en-unlimited1999', catalog['printingId'])
        self.assertEqual(64, len(catalog['cards']))
        row = catalog['cards'][56]
        self.assertEqual('Nidoran ♀', row['sourceName'])
        self.assertEqual('Nidoran♀', row['displayName'])
        self.assertEqual('pkmn-jungle-en-unlimited1999-057-nonholo-standard', row['id'])
        self.assertTrue(all(c['boosterEligible'] is True for c in catalog['cards']))
        self.assertEqual({'id','sourceId','displayName','sourceName','setId','collectorNumber','language','edition','printingId','variant','rarity','kind','finish','artReference','boosterEligible'}, set(row))
        self.assertTrue(all(c['artReference'] == 'placeholder:card-face' for c in catalog['cards']))

    def test_validate_rejects_identity_membership_and_approval_drift(self):
        self.assertTrue(callable(getattr(module, 'validate_catalog', None)), 'missing fail-closed candidate validator')
        real = module.build_catalog(EVIDENCE)
        report = module.validate_catalog(real, EVIDENCE)
        self.assertEqual({'holo': 16, 'rare': 16, 'uncommon': 16, 'common': 16}, report['rarityCounts'])
        self.assertEqual({'pokemon': 63, 'trainer': 1}, report['kindCounts'])
        self.assertEqual({'holo': 16, 'nonholo': 48}, report['finishCounts'])
        self.assertEqual(64, report['uniqueIds'])
        mutations = [
            ('duplicate-id', lambda c: c['cards'][1].update(id=c['cards'][0]['id'])),
            ('missing-member', lambda c: c['cards'].pop()),
            ('noncanonical-number', lambda c: c['cards'][0].update(collectorNumber='01')),
            ('wrong-finish', lambda c: c['cards'][0].update(finish='nonholo')),
            ('wrong-kind', lambda c: c['cards'][63].update(kind='basic-energy')),
            ('wrong-name', lambda c: c['cards'][0].update(displayName='Clefairy')),
            ('unsupported-edition', lambda c: c['cards'][0].update(edition='first')),
            ('unsupported-variant', lambda c: c['cards'][0].update(variant='no-symbol')),
            ('unsupported-eligibility', lambda c: c['cards'][0].update(boosterEligible=False)),
            ('integer-booster-flag', lambda c: c['cards'][0].update(boosterEligible=1)),
            ('boolean-schema-version', lambda c: c.update(schemaVersion=True)),
            ('pretend-approved', lambda c: c.update(completionTargetStatus='approved')),
        ]
        for name, mutate in mutations:
            with self.subTest(mutation=name):
                changed = copy.deepcopy(real)
                mutate(changed)
                with self.assertRaises(ValueError):
                    module.validate_catalog(changed, EVIDENCE)

    def test_crosscheck_rejects_changed_real_source(self):
        with tempfile.TemporaryDirectory(prefix='jungle-source-mutation-', dir=EVIDENCE) as d:
            target = Path(d)
            for filename in ['bulbapedia.raw.html', 'tcg-data.raw.json', 'pokellector.text.txt']:
                text = (EVIDENCE / filename).read_text(encoding='utf-8')
                if filename == 'tcg-data.raw.json':
                    text = text.replace('"rarity": "Rare Holo"', '"rarity": "Rare"', 1)
                (target / filename).write_text(text, encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'metadata mismatch'):
                module.crosscheck(target)

if __name__ == '__main__':
    unittest.main(verbosity=2)
