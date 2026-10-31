"""Offline, data-only Fossil candidate authoring; never reads/writes runtime assets."""
from collections import Counter
import re
from html.parser import HTMLParser


class _Cell(HTMLParser):
    def __init__(self):
        super().__init__()
        self.parts = []

    def handle_data(self, data):
        self.parts.append(data)

    def handle_starttag(self, tag, attrs):
        if tag == 'img':
            self.parts.append(dict(attrs).get('alt', ''))


def _text(markup):
    parser = _Cell()
    parser.feed(markup)
    return ' '.join(''.join(parser.parts).split())


def bulbapedia_rows(markup):
    section = markup.split('id="Set_lists"', 1)[-1].split('id="Gallery"', 1)[0]
    rows = []
    for row in re.findall(r'<tr\b[^>]*>((?:(?!<tr\b).)*?)</tr>', section, re.I | re.S):
        cells = [_text(c) for c in re.findall(r'<t[dh]\b[^>]*>(.*?)</t[dh]>', row, re.I | re.S)]
        if len(cells) >= 5 and re.fullmatch(r'[0-9]+/62', cells[0]):
            rows.append(dict(number=cells[0].split('/')[0], name=cells[2],
                             rarity=cells[4], supertype='Trainer' if cells[3] == 'T' else 'Pokémon'))
    return rows


PRINTING = 'fossil-en-unlimited1999'
RESEARCH = 'docs/research/phase5/fossil/Fossil_Research_Register.md'
RARITIES = {'Rare Holo': 'holo', 'Rare': 'rare', 'Uncommon': 'uncommon', 'Common': 'common'}
KINDS = {'Pokémon': 'pokemon', 'Trainer': 'trainer'}


def build_catalog(api, bulbapedia_html, pokellector_html):
    numbers = [row['number'] for row in api]
    if len(numbers) != 62 or set(numbers) != {str(n) for n in range(1, 63)}:
        raise ValueError('crosscheck coverage: require exactly canonical collector strings 1 through 62')
    plaques = [_text(p) for p in re.findall(r'<div class="plaque">(.*?)</div>', pokellector_html, re.S)]
    expected = [f"#{row['number']} - {row['name']}" for row in api]
    if plaques != expected:
        raise ValueError('crosscheck: Pokellector collector number/name mismatch')
    independent = bulbapedia_rows(bulbapedia_html)
    comparison = [{key: row[key] for key in ('number', 'name', 'rarity', 'supertype')} for row in api]
    if comparison != independent:
        raise ValueError('crosscheck: API differs from independent English Bulbapedia checklist')
    cards = []
    for row in sorted(api, key=lambda c: int(c['number'])):
        number = row['number']
        rarity = RARITIES[row['rarity']]
        finish = 'holo' if rarity == 'holo' else 'nonholo'
        cards.append(dict(id=f'pkmn-{PRINTING}-{int(number):03d}-{finish}-standard',
                          sourceId=row['id'], displayName=row['name'], sourceName=row['name'],
                          setId='fossil', collectorNumber=number, language='en', edition='unlimited',
                          printingId=PRINTING, variant='standard', rarity=rarity,
                          kind=KINDS[row['supertype']], finish=finish,
                          artReference='placeholder:card-face', boosterEligible=True))
    return dict(schemaVersion=1, catalogVersion='phase5-fossil-candidate-2026-10-31-v1',
                printingId=PRINTING, completionTargetStatus='proposed-not-approved',
                sourceResearch=RESEARCH, cards=cards)


def verify_catalog(catalog, api, bulbapedia_html, pokellector_html) -> dict[str, object]:
    import json
    expected = build_catalog(api, bulbapedia_html, pokellector_html)
    if json.dumps(catalog, sort_keys=True) != json.dumps(expected, sort_keys=True):
        raise ValueError('candidate differs from source-crosschecked proposed schema')
    cards = catalog['cards']
    return dict(cards=len(cards), uniqueIds=len({c['id'] for c in cards}),
                rarityCounts=dict(Counter(c['rarity'] for c in cards)),
                kindCounts=dict(Counter(c['kind'] for c in cards)),
                finishCounts=dict(Counter(c['finish'] for c in cards)),
                proposedBoosterEligible=sum(c['boosterEligible'] for c in cards),
                energyCards=sum(c['kind'] in ('basic-energy', 'special-energy') for c in cards),
                independentNumberNameRarityKindFinishMatches=len(cards),
                thirdSourceNumberNameMatches=len(cards),
                physicalPrintingVerified=False, runtimeAdmitted=False)


def main():
    import argparse
    import hashlib
    import json
    from pathlib import Path
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='check without writing any files')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    research = root / 'docs/research/phase5/fossil'
    evidence = research / 'evidence'
    paths = [evidence / name for name in ('api.json', 'bulbapedia.html', 'pokellector.html')]
    api = json.loads(paths[0].read_text(encoding='utf-8'))
    bulb, pokel = [p.read_text(encoding='utf-8') for p in paths[1:]]
    target = root / 'Authoring/Phase5/Data/fossil-en-unlimited1999.catalog.json'
    catalog = json.loads(target.read_text(encoding='utf-8')) if args.check else build_catalog(api, bulb, pokel)
    proof = verify_catalog(catalog, api, bulb, pokel)
    proof['sourceSha256'] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    encoded = json.dumps(catalog, indent=2, ensure_ascii=False) + '\n'
    proof['catalogSha256'] = hashlib.sha256(encoded.encode('utf-8')).hexdigest()
    if not args.check:
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(encoded, encoding='utf-8', newline='\n')
        (research / 'catalog-verification.json').write_text(json.dumps(proof, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(proof, sort_keys=True))


if __name__ == '__main__':
    main()
