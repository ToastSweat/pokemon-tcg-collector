"""Offline Jungle research authoring; never imports or alters the game runtime."""
import html
import json
import re
from collections import Counter

RARITIES = {'Rare Holo': 'holo', 'Rare': 'rare', 'Uncommon': 'uncommon', 'Common': 'common'}
POKEMON_TYPES = {'Grass', 'Fire', 'Water', 'Lightning', 'Fighting', 'Psychic', 'Colorless'}


def parse_bulbapedia(raw):
    """Read only the English numbered table; image alt labels are text, not artwork."""
    section = raw.split('id="Set_lists"', 1)[1].split('id="Gallery"', 1)[0]
    result = []
    for match in re.finditer(r'<tr\b[^>]*>(.*?)</tr>', section, re.S):
        row = match.group(1)
        number = re.search(r'<td\b[^>]*>\s*(\d+)/64\s*</td>', row)
        if not number:
            continue
        name = re.search(r'<a\b[^>]*title="[^"]*\(Jungle \d+\)"[^>]*>(.*?)</a>', row)
        rarity = re.search(r'<a href="/wiki/Rarity" title="([^"]+)">', row)
        if not name or not rarity or rarity.group(1) not in RARITIES:
            raise ValueError('Incomplete numbered Bulbapedia row')
        type_cell = re.search(r'<th\b[^>]*>(.*?)</th>', row, re.S)
        if not type_cell:
            raise ValueError('Missing kind evidence')
        type_html = type_cell.group(1)
        if re.sub('<[^>]*>', '', type_html).strip() == 'T':
            kind = 'trainer'
        elif any('alt="' + t + '"' in type_html for t in POKEMON_TYPES):
            kind = 'pokemon'
        else:
            raise ValueError('Unrecognized type; do not infer kind from card name')
        normalized_rarity = RARITIES[rarity.group(1)]
        result.append(dict(number=int(number.group(1)), name=html.unescape(name.group(1)),
                           rarity=normalized_rarity, kind=kind,
                           finish='holo' if normalized_rarity == 'holo' else 'nonholo'))
    if [r['number'] for r in result] != list(range(1, 65)):
        raise ValueError('Expected exactly ordered English 1..64 membership')
    return result


def crosscheck(evidence):
    """Fail closed on any disagreement across the two complete metadata sources."""
    independent = parse_bulbapedia((evidence / 'bulbapedia.raw.html').read_text(encoding='utf-8'))
    api_cards = json.loads((evidence / 'tcg-data.raw.json').read_text(encoding='utf-8'))
    if len(api_cards) != 64:
        raise ValueError('JSON source does not have exactly 64 cards')
    aliases = {'Nidoran ♀': 'Nidoran♀', 'Nidoran F': 'Nidoran♀', 'Poke Ball': 'Poké Ball'}
    normalized = []
    for c in api_cards:
        number = c['number']
        if not re.fullmatch(r'[1-9]\d*', number) or c['id'] != 'base2-' + number:
            raise ValueError('Unexpected source identity or noncanonical number')
        rarity = RARITIES[c['rarity']]
        normalized.append(dict(number=int(number), name=aliases.get(c['name'], c['name']),
                               rarity=rarity, kind={'Pokémon': 'pokemon', 'Trainer': 'trainer'}[c['supertype']],
                               finish='holo' if rarity == 'holo' else 'nonholo'))
    normalized.sort(key=lambda r: r['number'])
    if normalized != independent:
        raise ValueError('Complete JSON/Bulbapedia metadata mismatch')
    third = (evidence / 'pokellector.text.txt').read_text(encoding='utf-8')
    names = [(int(n), aliases.get(name.strip(), name.strip()))
             for n, name in re.findall(r'#(\d+) - ([^\r\n]+)', third)]
    if names != [(r['number'], r['name']) for r in independent]:
        raise ValueError('Pokellector number/name mismatch')
    return [dict(r, sourceId='base2-' + str(r['number'])) for r in independent]


def build_catalog(evidence):
    """Apply explicit proposed standard-printing policy, not purported source edition data."""
    rows = crosscheck(evidence)
    originals = {c['id']: c for c in json.loads((evidence / 'tcg-data.raw.json').read_text(encoding='utf-8'))}
    printing = 'jungle-en-unlimited1999'
    cards = []
    for r in rows:
        number = r['number']
        cards.append(dict(id=f"pkmn-{printing}-{number:03d}-{r['finish']}-standard",
                          sourceId=r['sourceId'], displayName=r['name'],
                          sourceName=originals[r['sourceId']]['name'], setId='jungle',
                          collectorNumber=str(number), language='en', edition='unlimited',
                          printingId=printing, variant='standard', rarity=r['rarity'],
                          kind=r['kind'], finish=r['finish'], artReference='placeholder:card-face',
                          boosterEligible=True))
    return dict(schemaVersion=1, catalogVersion='jungle-en-unlimited1999-candidate-v0.1',
                printingId=printing, completionTargetStatus='proposed-not-approved',
                sourceResearch='docs/research/phase5/jungle/claim-register.md', cards=cards)


def validate_catalog(catalog, evidence):
    """Validate the entire candidate contract against retained source reconciliation."""
    expected = build_catalog(evidence)
    if (type(catalog.get('schemaVersion')) is not int or
            any(type(c.get('boosterEligible')) is not bool for c in catalog.get('cards', []))):
        raise ValueError('JSON schemaVersion must be integer and boosterEligible must be boolean')
    if catalog != expected:
        raise ValueError('Candidate differs from crosschecked membership or explicit proposed identity/eligibility policy')
    cards = catalog['cards']
    ids = {c['id'] for c in cards}
    if len(ids) != len(cards):
        raise ValueError('Duplicate candidate IDs')
    return dict(passed=True, cardCount=len(cards), uniqueIds=len(ids),
                collectorNumbers=[c['collectorNumber'] for c in cards],
                rarityCounts=dict(Counter(c['rarity'] for c in cards)),
                kindCounts=dict(Counter(c['kind'] for c in cards)),
                finishCounts=dict(Counter(c['finish'] for c in cards)),
                proposedBoosterEligible=sum(c['boosterEligible'] for c in cards),
                completionTargetStatus=catalog['completionTargetStatus'])
