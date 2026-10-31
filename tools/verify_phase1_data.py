"""Verify Phase 1 authored metadata against evidence and the approved provisional model.

This is an offline authoring guard, not a historical-factory validator or pack generator.
"""
import argparse
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CATALOG = ROOT / 'Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json'
DEFAULT_MODEL = ROOT / 'Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json'
SOURCE = ROOT / 'docs/research/evidence/catalog/card_metadata.json'


def validate(catalog, model, source):
    checks = []

    def check(name, passed):
        checks.append({'name': name, 'passed': bool(passed)})

    cards = catalog['cards']
    pools = model['pools']
    sizes = {p['id']: len(p['entries']) for p in pools}
    totals = {p['id']: sum(e['weight'] for e in p['entries']) for p in pools}
    expected = []
    for entry in source['cards']:
        number = int(entry['number'])
        if number == 8:
            continue  # Source checklist retains it; no completion exception was approved.
        rarity = {'Rare Holo': 'holo', 'Rare': 'rare', 'Uncommon': 'uncommon',
                  'Common': 'common', None: 'common'}[entry.get('rarity')]
        kind = {'Pokémon': 'pokemon', 'Trainer': 'trainer',
                'Energy': 'special-energy' if number == 96 else 'basic-energy'}[entry['supertype']]
        finish = 'holo' if rarity == 'holo' else 'nonholo'
        variant = 'yellow-cheeks' if number == 58 else 'standard'
        expected.append({'id': f'pkmn-base-en-unlimited1999-{number:03d}-{finish}-{variant}',
                         'sourceId': entry['id'], 'displayName': 'Imposter Professor Oak' if number == 73 else entry['name'],
                         'sourceName': entry['name'], 'setId': 'base', 'collectorNumber': str(number),
                         'language': 'en', 'edition': 'unlimited', 'printingId': 'base-en-unlimited1999',
                         'variant': variant, 'rarity': rarity, 'kind': kind, 'finish': finish,
                         'artReference': 'placeholder:card-face', 'boosterEligible': True})
    check('catalog_source_mapping', cards == expected)
    check('booster_catalog_size', len(cards) == 101)
    check('source_checklist_size', len(source['cards']) == 102)
    check('pool_sizes', sizes == {'holo': 15, 'rare': 16, 'uncommon': 32, 'common': 32, 'energy': 6})
    check('pool_weight_totals', totals == {'holo': 110, 'rare': 121, 'uncommon': 121, 'common': 121, 'energy': 121})
    def exact(left, right):
        return json.dumps(left, sort_keys=True, ensure_ascii=True) == json.dumps(right, sort_keys=True, ensure_ascii=True)
    by_number = {int(c['collectorNumber']): c for c in expected}
    weight_exceptions = {'holo': ({3, 9, 11, 13, 16}, 8, 7),
                         'rare': ({70, 72, 73, 75, 77, 78, 79}, 7, 8),
                         'uncommon': ({80, 82, 83, 85, 86, 88, 89}, 3, 4),
                         'common': ({67, 69, 91, 92, 93, 94, 95}, 3, 4)}
    energy_weights = {97: 9, 98: 9, 99: 17, 100: 35, 101: 34, 102: 17}
    approved_pools = {}
    for pool_id in ('holo', 'rare', 'uncommon', 'common', 'energy'):
        entries = []
        for number, card in by_number.items():
            eligible = card['kind'] == 'basic-energy' if pool_id == 'energy' else (
                card['rarity'] == pool_id and card['kind'] != 'basic-energy')
            if eligible:
                if pool_id == 'energy':
                    weight = energy_weights[number]
                else:
                    exceptions, exceptional, ordinary = weight_exceptions[pool_id]
                    weight = exceptional if number in exceptions else ordinary
                entries.append({'cardId': card['id'], 'weight': weight})
        approved_pools[pool_id] = {'id': pool_id, 'uniqueWithinPack': pool_id in ('common', 'uncommon'),
                                   'entries': entries}
    check('approved_pool_entries_and_policies', len(pools) == 5 and exact({p['id']: p for p in pools}, approved_pools))
    order = ['common'] * 5 + ['energy'] * 2 + ['rare'] + ['uncommon'] * 3
    approved_slots = [{'poolId': pool_id, 'alternativePoolId': 'holo' if i == 7 else '',
                       'alternativeNumerator': 1 if i == 7 else 0,
                       'alternativeDenominator': 3 if i == 7 else 1} for i, pool_id in enumerate(order)]
    check('approved_order_and_holo_fraction', exact(model['slots'], approved_slots))
    approved_metadata = {'schemaVersion': 1, 'modelId': 'base-en-unlimited1999-sheet-informed',
                         'modelVersion': '0.1.0', 'status': 'approved-provisional',
                         'printingId': 'base-en-unlimited1999', 'rngAlgorithm': 'xorshift32-v1'}
    check('approved_model_metadata', exact({k: model.get(k) for k in approved_metadata}, approved_metadata))
    catalog_metadata = {'schemaVersion': 1, 'catalogVersion': 'base-en-unlimited1999-0.1.0',
                        'printingId': 'base-en-unlimited1999',
                        'completionTargetStatus': 'approved-strict-unstamped-101-excluding-machamp'}
    check('catalog_schema_and_selected_target', exact({k: catalog.get(k) for k in catalog_metadata}, catalog_metadata))
    return {'passed': all(c['passed'] for c in checks), 'checkCount': len(checks), 'checks': checks,
            'cardCount': len(cards), 'sourceChecklistCount': len(source['cards']),
            'completionTargetStatus': catalog['completionTargetStatus'],
            'modelVersion': model['modelVersion'], 'poolSizes': sizes, 'poolWeights': totals}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--catalog', type=Path, default=DEFAULT_CATALOG)
    parser.add_argument('--model', type=Path, default=DEFAULT_MODEL)
    args = parser.parse_args(argv)
    try:
        report = validate(json.loads(args.catalog.read_text(encoding='utf-8')),
                          json.loads(args.model.read_text(encoding='utf-8')),
                          json.loads(SOURCE.read_text(encoding='utf-8')))
    except (OSError, ValueError, KeyError, TypeError) as error:
        report = {'passed': False, 'error': str(error), 'checkCount': 0, 'checks': []}
    print(json.dumps(report, ensure_ascii=True, indent=2))
    return 0 if report['passed'] else 1


if __name__ == '__main__':
    sys.exit(main())
