"""Offline authoring-only verification. Run python -B TestsPhase5/FossilCatalogTests.py."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / 'docs/research/phase5/fossil/evidence'


class FossilCatalogTests(unittest.TestCase):
    def tool(self):
        path = ROOT / 'tools/prepare_fossil_catalog.py'
        self.assertTrue(path.exists(), 'Fossil authoring implementation is missing')
        spec = importlib.util.spec_from_file_location('fossil_authoring', path)
        assert spec is not None and spec.loader is not None
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module

    def inputs(self):
        return (json.loads((EVIDENCE / 'api.json').read_text(encoding='utf-8')),
                (EVIDENCE / 'bulbapedia.html').read_text(encoding='utf-8'),
                (EVIDENCE / 'pokellector.html').read_text(encoding='utf-8'))

    def test_complete_real_retrieved_checklist_builds_proposed_candidate(self):
        catalog = self.tool().build_catalog(*self.inputs())
        self.assertEqual(1, catalog['schemaVersion'])
        self.assertEqual('proposed-not-approved', catalog['completionTargetStatus'])
        self.assertEqual('fossil-en-unlimited1999', catalog['printingId'])
        self.assertEqual([str(n) for n in range(1, 63)],
                         [c['collectorNumber'] for c in catalog['cards']])
        self.assertEqual('pkmn-fossil-en-unlimited1999-001-holo-standard', catalog['cards'][0]['id'])
        self.assertEqual('Mysterious Fossil', catalog['cards'][-1]['displayName'])
        self.assertEqual('trainer', catalog['cards'][-1]['kind'])


    def test_every_number_name_rarity_kind_finish_matches_independent_bulbapedia(self):
        tool = self.tool()
        api, bulb, pokel = self.inputs()
        damaged = copy.deepcopy(api)
        for field, value in [('number', '63'), ('name', 'Wrong name'),
                             ('rarity', 'Rare'), ('supertype', 'Trainer')]:
            with self.subTest(field=field):
                damaged = copy.deepcopy(api)
                damaged[0][field] = value
                with self.assertRaisesRegex(ValueError, 'crosscheck'):
                    tool.build_catalog(damaged, bulb, pokel)
        self.assertEqual(62, len(tool.build_catalog(api, bulb, pokel)['cards']))


    def test_third_source_and_complete_canonical_coverage_fail_closed(self):
        tool = self.tool()
        api, bulb, pokel = self.inputs()
        with self.assertRaisesRegex(ValueError, 'coverage'):
            tool.build_catalog([], '', '')
        with self.assertRaisesRegex(ValueError, 'Pokellector'):
            tool.build_catalog(api, bulb, pokel.replace('#62 - Mysterious Fossil', '#62 - Wrong'))
        with self.assertRaisesRegex(ValueError, 'Pokellector'):
            tool.build_catalog(api, bulb, '')
        for damaged in [api[:-1], api + [api[0]], [dict(api[0], number='01')] + api[1:]]:
            with self.assertRaisesRegex(ValueError, 'coverage'):
                tool.build_catalog(damaged, bulb, pokel)


    def test_candidate_verification_rejects_mutations_and_reports_groups(self):
        tool = self.tool()
        self.assertTrue(callable(getattr(tool, 'verify_catalog', None)), 'candidate verification missing')
        inputs = self.inputs()
        good = tool.build_catalog(*inputs)
        proof = tool.verify_catalog(good, *inputs)
        self.assertEqual({'holo': 15, 'rare': 15, 'uncommon': 16, 'common': 16}, proof['rarityCounts'])
        self.assertEqual({'pokemon': 57, 'trainer': 5}, proof['kindCounts'])
        self.assertEqual({'holo': 15, 'nonholo': 47}, proof['finishCounts'])
        self.assertEqual(62, proof['uniqueIds'])
        self.assertEqual(62, proof['proposedBoosterEligible'])
        self.assertEqual(0, proof['energyCards'])
        self.assertEqual(62, proof['independentNumberNameRarityKindFinishMatches'])
        for field, value in [('collectorNumber', '01'), ('id', good['cards'][1]['id']),
                             ('sourceId', 'base3-999'), ('boosterEligible', False),
                             ('rarity', 'rare'), ('kind', 'pokemon'), ('finish', 'holo'),
                             ('edition', '1stEdition'), ('variant', 'cosmos'),
                             ('artReference', 'https://not-downloaded.invalid/art')]:
            with self.subTest(field=field):
                damaged = copy.deepcopy(good)
                damaged['cards'][-1][field] = value
                with self.assertRaisesRegex(ValueError, 'candidate'):
                    tool.verify_catalog(damaged, *inputs)
        for field, value in [('completionTargetStatus', 'approved'), ('schemaVersion', 2), ('extra', True)]:
            with self.subTest(field=field):
                damaged = copy.deepcopy(good)
                damaged[field] = value
                with self.assertRaisesRegex(ValueError, 'candidate'):
                    tool.verify_catalog(damaged, *inputs)
        domain = (ROOT / 'Assets/VintageCollector/Domain/Data.cs').read_text(encoding='utf-8')
        import re
        for cls, record in [('CatalogData', good), ('CardData', good['cards'][0])]:
            body = domain.split('class ' + cls, 1)[1].split('}', 1)[0]
            fields = []
            for declaration in re.findall(r'public (?:int|string|bool|CardData\[\]) ([^;]+);', body):
                fields.extend(name.strip() for name in declaration.split(','))
            self.assertEqual(set(fields), set(record), cls)


    def test_cli_publishes_only_authoring_candidate_and_rechecks_it(self):
        import subprocess
        import sys
        script = ROOT / 'tools/prepare_fossil_catalog.py'
        result = subprocess.run([sys.executable, '-B', str(script)], cwd=ROOT,
                                capture_output=True, text=True, check=True)
        self.assertTrue(result.stdout.strip(), 'authoring CLI did not execute')
        proof = json.loads(result.stdout)
        self.assertEqual(62, proof['cards'])
        target = ROOT / 'Authoring/Phase5/Data/fossil-en-unlimited1999.catalog.json'
        self.assertTrue(target.exists())
        before = target.read_bytes()
        check = subprocess.run([sys.executable, '-B', str(script), '--check'], cwd=ROOT,
                               capture_output=True, text=True, check=True)
        self.assertEqual(proof, json.loads(check.stdout))
        self.assertEqual(before, target.read_bytes())


    def test_schema_types_do_not_coerce_booleans_or_floats(self):
        tool = self.tool()
        inputs = self.inputs()
        good = tool.build_catalog(*inputs)
        for field, value in [('schemaVersion', True), ('schemaVersion', 1.0)]:
            damaged = copy.deepcopy(good)
            damaged[field] = value
            with self.assertRaisesRegex(ValueError, 'candidate'):
                tool.verify_catalog(damaged, *inputs)
        damaged = copy.deepcopy(good)
        damaged['cards'][0]['boosterEligible'] = 1
        with self.assertRaisesRegex(ValueError, 'candidate'):
            tool.verify_catalog(damaged, *inputs)


if __name__ == '__main__':
    unittest.main(verbosity=2)
