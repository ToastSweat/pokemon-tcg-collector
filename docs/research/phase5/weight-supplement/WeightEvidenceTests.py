"""Offline research proof tests. Never imports game code or writes game data."""
from pathlib import Path
import importlib.util
import json
import unittest

HERE = Path(__file__).resolve().parent

class WeightEvidenceTests(unittest.TestCase):
    def test_complete_sheet_vectors_match_catalog_and_exclude_ivy(self):
        helper = HERE/'build_supplement.py'
        self.assertTrue(helper.exists(), 'research expansion/validation helper not implemented')
        spec = importlib.util.spec_from_file_location('weight_builder', helper)
        assert spec is not None and spec.loader is not None
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        result = module.build()
        tables = {t['tableId']:t for t in result['tables']}
        expected = {
            'jungle-1-removed': (64, {'H':110,'R':121,'U':120,'C':121}),
            'jungle-2-ivy-error': (64, {'H':110,'R':121,'U':121,'C':121}),
            'jungle-3-exeggutor': (64, {'H':110,'R':121,'U':121,'C':121}),
            'fossil-1': (62, {'H':110,'R':121,'U':121,'C':121}),
        }
        for table_id, (count, sums) in expected.items():
            t = tables[table_id]
            self.assertEqual(count, len(t['cards']))
            self.assertEqual(sums, t['checks']['allOutcomePositionSums'])
            self.assertTrue(t['checks']['exactCatalogAndSourceMap'])
            self.assertTrue(t['checks']['imageSourceIdAndArticlePlacement'])
        exegg = lambda tid: next(c for c in tables[tid]['cards'] if c['collectorNumber']=='35')['multiplicity']
        self.assertEqual(7, exegg('jungle-1-removed'))
        self.assertEqual(7, exegg('jungle-2-ivy-error'))
        self.assertEqual(8, exegg('jungle-3-exeggutor'))
        self.assertEqual([{'label':'Pikachu (Promo)','pool':'U','multiplicity':1,'catalogId':None,'eligibleForCandidate':False,'reason':'First Edition error-variant promo only; no Jungle collector-number mapping asserted'}], tables['jungle-2-ivy-error']['nonCatalogOutcomes'])
        for t in tables.values():
            common_pika = [c for c in t['cards'] if c['displayName']=='Pikachu']
            if common_pika:
                self.assertEqual([('60','C',7)], [(c['collectorNumber'],c['pool'],c['multiplicity']) for c in common_pika])

    def test_rejects_caption_variant_mixing(self):
        helper = HERE/'build_supplement.py'
        spec = importlib.util.spec_from_file_location('weight_builder', helper)
        assert spec is not None and spec.loader is not None
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        data = json.loads((HERE/'manual-transcription.json').read_text(encoding='utf-8'))
        data['tables'][0]['titleVerbatim'] = data['tables'][2]['titleVerbatim']
        with self.assertRaisesRegex(AssertionError, 'caption/variant'):
            module.build(transcription=data)

    def test_rejects_wrong_names_numbers_totals_and_source_ids(self):
        import copy
        spec = importlib.util.spec_from_file_location('weight_builder', HERE/'build_supplement.py')
        assert spec is not None and spec.loader is not None
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        original = json.loads((HERE/'manual-transcription.json').read_text(encoding='utf-8'))
        mutations = [
            ('wrong uncommon denominator', lambda d:d['tables'][0]['pools'][2].update(emittedPositionTotal=121)),
            ('wrong holo group magnitude', lambda d:d['tables'][3]['pools'][0]['groups'][0].update(m=9)),
            ('promo conflated with common Pikachu', lambda d:d['tables'][1]['pools'][2]['exceptionLinesVerbatim'].__setitem__(0,'U1    Pikachu')),
            ('unmapped exception name', lambda d:d['tables'][0]['pools'][0]['exceptionLinesVerbatim'].__setitem__(0,'H6    Victreebel, Wigglypuff')),
            ('source image swap', lambda d:d['tables'][0].update(imageSourceId=d['tables'][3]['imageSourceId'])),
        ]
        for label, mutate in mutations:
            with self.subTest(label=label):
                d = copy.deepcopy(original)
                mutate(d)
                with self.assertRaises(AssertionError):
                    module.build(transcription=d)
        original_cats = {s:json.loads((module.ROOT/f'Authoring/Phase5/Data/{s}-en-unlimited1999.catalog.json').read_text(encoding='utf-8')) for s in ['jungle','fossil']}
        for field,value in [('sourceId','base3-1'),('sourceName','Wigglytuff'),('collectorNumber','01'),('displayName','Clefablé'),('rarity','rare')]:
            with self.subTest(catalogField=field):
                cats = copy.deepcopy(original_cats)
                cats['jungle']['cards'][0][field]=value
                with self.assertRaises((AssertionError,KeyError)):
                    module.build(catalogs=cats)

if __name__ == '__main__':
    unittest.main(verbosity=2)
