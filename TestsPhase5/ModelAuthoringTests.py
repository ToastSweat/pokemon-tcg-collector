"""Approved-model contract tests; fixtures/artifacts stay inside the approved project."""
from pathlib import Path
import copy
import importlib.util
import json
import sys
import unittest
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]


def module():
    path = ROOT / 'tools/prepare_phase5_models.py'
    if not path.is_file():
        raise AssertionError('Missing approved-model authoring implementation')
    spec = importlib.util.spec_from_file_location('phase5_models', path)
    assert spec is not None and spec.loader is not None
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


class ModelAuthoringTests(unittest.TestCase):
    def test_promoted_catalogs_preserve_every_card_and_source_identity(self):
        m = module()
        docs = m.build_documents()
        for set_id, target in [('jungle', 64), ('fossil', 62)]:
            printing = set_id + '-en-unlimited1999'
            candidate = json.loads((ROOT / ('Authoring/Phase5/Data/' + printing + '.catalog.json')).read_text())
            approved = docs[printing + '.catalog.json']
            expected = copy.deepcopy(candidate)
            expected['catalogVersion'] = printing + '-0.1.0'
            expected['completionTargetStatus'] = 'approved-standard-checklist-' + str(target)
            self.assertEqual(expected, approved)
            self.assertEqual(target, len(approved['cards']))
            self.assertEqual(list(map(str, range(1, target + 1))), [c['collectorNumber'] for c in approved['cards']])

    def test_exact_approved_rule_schema_full_weight_vectors_and_slot_order(self):
        docs = module().build_documents()
        weights = json.loads((ROOT / 'docs/research/phase5/weight-supplement/sheet-weight-supplement.json').read_text())
        for set_id, table_id in [('jungle', 'jungle-1-removed'), ('fossil', 'fossil-1')]:
            printing = set_id + '-en-unlimited1999'
            name = printing + '.rules.json'
            self.assertIn(name, docs, 'Missing approved RuleData')
            r = docs[name]
            self.assertEqual({'schemaVersion', 'modelId', 'modelVersion', 'status', 'printingId', 'rngAlgorithm', 'researchRegister', 'approvalRecord', 'approximations', 'pools', 'slots'}, set(r))
            self.assertEqual(1, r['schemaVersion'])
            self.assertEqual(printing + '-sheet-informed', r['modelId'])
            self.assertEqual('0.1.0', r['modelVersion'])
            self.assertEqual('approved-provisional', r['status'])
            self.assertEqual(printing, r['printingId'])
            self.assertEqual('xorshift32-v1', r['rngAlgorithm'])
            self.assertEqual('docs/verification/Phase_5_Model_Approval.json', r['approvalRecord'])
            self.assertTrue((ROOT / r['researchRegister']).is_file())
            table = next(t for t in weights['tables'] if t['tableId'] == table_id)
            self.assertEqual(['C', 'U', 'R', 'H'], [p['id'] for p in r['pools']])
            totals = {}
            for p in r['pools']:
                self.assertEqual({'id', 'uniqueWithinPack', 'entries'}, set(p))
                self.assertIs(p['uniqueWithinPack'], p['id'] in ['C', 'U'])
                expected = [{'cardId': c['id'], 'weight': c['multiplicity']} for c in table['cards'] if c['pool'] == p['id']]
                self.assertEqual(expected, p['entries'])
                totals[p['id']] = sum(e['weight'] for e in p['entries'])
            self.assertEqual({'H': 110, 'R': 121, 'U': 120 if set_id == 'jungle' else 121, 'C': 121}, totals)
            single = lambda p: {'poolId': p, 'alternativePoolId': '', 'alternativeNumerator': 0, 'alternativeDenominator': 1}
            self.assertEqual([single('C') for _ in range(7)] + [{'poolId': 'R', 'alternativePoolId': 'H', 'alternativeNumerator': 1, 'alternativeDenominator': 3}] + [single('U') for _ in range(3)], r['slots'])
            self.assertTrue(any(table_id in a for a in r['approximations']))

    def test_approved_configuration_mutations_are_rejected_not_just_pool_totals(self):
        m = module()
        self.assertTrue(hasattr(m, 'validate_documents'), 'Missing exact approved-configuration mutation guard')
        docs = m.build_documents()
        self.assertIn('Provenance.json', docs)
        self.assertTrue(m.validate_documents(docs))
        mutations = []
        for set_id in ['jungle', 'fossil']:
            rule = set_id + '-en-unlimited1999.rules.json'
            cat = set_id + '-en-unlimited1999.catalog.json'
            def add(name, change, key=rule):
                mutated = copy.deepcopy(docs)
                change(mutated[key])
                mutations.append((set_id + '-' + name, mutated))
            def swap_weights(r):
                entries = r['pools'][0]['entries']
                a = next(i for i, e in enumerate(entries) if e['weight'] != entries[0]['weight'])
                entries[0]['weight'], entries[a]['weight'] = entries[a]['weight'], entries[0]['weight']
            add('preserve-total-weight-swap', swap_weights)
            add('entry-reorder', lambda r: r['pools'][0]['entries'].reverse())
            add('slot-reorder', lambda r: r['slots'].reverse())
            add('probability-change', lambda r: r['slots'][7].update(alternativeDenominator=4))
            add('duplicate-policy-change', lambda r: r['pools'][0].update(uniqueWithinPack=False))
            add('foreign-card', lambda r: r['pools'][0]['entries'][0].update(cardId='pkmn-base-en-unlimited1999-008-holo-standard'))
            add('status-drift', lambda r: r.update(status='exact-factory'))
            add('approval-drift', lambda r: r.update(approvalRecord='invented.json'))
            add('schema-bool', lambda r: r.update(schemaVersion=True))
            add('integer-float', lambda r: r['pools'][0]['entries'][0].update(weight=float(r['pools'][0]['entries'][0]['weight'])))
            add('boolean-integer', lambda r: r['pools'][0].update(uniqueWithinPack=1))
            add('scope-drift', lambda c: c.update(completionTargetStatus='all-error-promos'), cat)
            add('card-identity-drift', lambda c: c['cards'][0].update(sourceId='wrong-source'), cat)
            add('research-drift', lambda c: c.update(sourceResearch='wrong.md'), cat)
        wrong = copy.deepcopy(docs)
        wrong['Provenance.json']['sets'][0]['sourceTableId'] = 'jungle-3-exeggutor'
        mutations.append(('wrong-source-table', wrong))
        wrong = copy.deepcopy(docs)
        wrong['Provenance.json']['approvalRecord'] = 'invented.json'
        mutations.append(('manifest-approval-drift', wrong))
        for name, mutated in mutations:
            with self.subTest(name=name):
                with self.assertRaises(ValueError):
                    m.validate_documents(mutated)
        self.assertTrue(m.validate_documents(docs))

    def test_authoring_is_owned_idempotent_and_refuses_unrelated_or_escaped_targets(self):
        m = module()
        self.assertTrue(hasattr(m, 'publish'), 'Missing root-contained owned authoring publication')
        import uuid
        fixture = ROOT / '.artifacts/phase5-models' / ('publication-' + uuid.uuid4().hex)
        docs = m.build_documents()
        self.assertEqual('created', m.publish(docs, fixture))
        before = {p.name: (p.read_bytes(), p.stat().st_mtime_ns) for p in fixture.iterdir()}
        self.assertEqual('unchanged', m.publish(docs, fixture))
        self.assertEqual(before, {p.name: (p.read_bytes(), p.stat().st_mtime_ns) for p in fixture.iterdir()})
        canary = fixture / 'unrelated.txt'
        canary.write_text('preserve me', encoding='utf-8')
        with self.assertRaises(ValueError):
            m.publish(docs, fixture)
        self.assertEqual('preserve me', canary.read_text())
        self.assertEqual(before, {p.name: (p.read_bytes(), p.stat().st_mtime_ns) for p in fixture.iterdir() if p.name != 'unrelated.txt'})
        canary.unlink()
        target = fixture / 'jungle-en-unlimited1999.rules.json'
        target.write_text('unowned content', encoding='utf-8')
        with self.assertRaises(ValueError):
            m.publish(docs, fixture)
        self.assertEqual('unowned content', target.read_text())
        for invalid in [ROOT / 'Assets/forbidden-model-test', ROOT.parent / 'outside-model-test', ROOT / '.artifacts/phase5-models/../outside-model-test']:
            with self.subTest(path=str(invalid)):
                with self.assertRaises(ValueError):
                    m.publish(docs, invalid)
                self.assertFalse(invalid.exists())

    def test_frozen_source_hash_drift_fails_closed(self):
        from unittest.mock import patch
        m = module()
        with patch.object(m, 'sha', return_value='wrong-digest'):
            with self.assertRaisesRegex(ValueError, 'Frozen input drift'):
                m.build_documents()

    def test_owned_publication_rejects_root_identity_drift_and_real_junctions(self):
        from unittest.mock import patch
        import subprocess
        import uuid
        m = module()
        with patch.object(m, 'ROOT', ROOT.parent / 'unapproved-copy'):
            with self.assertRaises(ValueError):
                m.safe_output(m.APPROVED_DIR)
        fixture = ROOT / '.artifacts/phase5-models' / ('junction-' + uuid.uuid4().hex)
        fixture.mkdir()
        target = fixture / 'target'
        target.mkdir()
        link = fixture / 'link'
        created = subprocess.run(['cmd.exe', '/c', 'mklink', '/J', str(link), str(target)], capture_output=True, text=True)
        self.assertEqual(0, created.returncode, created.stdout + created.stderr)
        try:
            with self.assertRaisesRegex(ValueError, 'Reparse'):
                m.publish(m.build_documents(), link / 'forbidden')
            self.assertEqual([], list(target.iterdir()))
        finally:
            link.rmdir()

    def test_duplicate_json_keys_cannot_be_normalized_into_approved_configuration(self):
        import uuid
        m = module()
        fixture = ROOT / '.artifacts/phase5-models' / ('duplicate-json-' + uuid.uuid4().hex)
        m.publish(m.build_documents(), fixture)
        path = fixture / 'jungle-en-unlimited1999.rules.json'
        text = path.read_text(encoding='utf-8')
        path.write_text(text.replace('"schemaVersion": 1,', '"schemaVersion": 1, "schemaVersion": 1,', 1), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'Duplicate'):
            m.read_approved(fixture)


if __name__ == '__main__':
    unittest.main(verbosity=2)
