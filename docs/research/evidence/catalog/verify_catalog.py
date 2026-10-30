"""Validate research evidence, checklist coverage and quoted-source substrings."""
import collections
import datetime
import html
import json
from pathlib import Path
import re
import unicodedata

ROOT=Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG')
E=ROOT/'docs/research/evidence/catalog'

def norm(text):
    text=re.sub(r'\[([^\]]+)\]\(\S+(?:\s+"[^"]*")?\)',r'\1',text)
    text=re.sub(r'<[^>]+>','',text)
    text=html.unescape(text).replace('*','').replace('_','').replace('`','')
    return re.sub(r'\s+',' ',unicodedata.normalize('NFKC',text)).strip()

def no_forbidden_keys(value):
    if isinstance(value,dict):
        assert not (set(value)&{'images','image','logo','symbol','pricing'})
        for v in value.values(): no_forbidden_keys(v)
    elif isinstance(value,list):
        for v in value: no_forbidden_keys(v)

primary=json.loads((E/'card_metadata.json').read_text(encoding='utf-8'))
supplement=json.loads((E/'tcgdex_supplement.json').read_text(encoding='utf-8'))
cards=primary['cards']
assert len(cards)==102==primary['provenance']['card_count']
assert sorted(int(c['number']) for c in cards)==list(range(1,103))
assert len(set(c['id'] for c in cards))==102
assert all(all(k in c for k in ('id','number','name','supertype')) for c in cards)
no_forbidden_keys(cards)
no_forbidden_keys(supplement['records'])
rarities=dict(collections.Counter(c.get('rarity','OMITTED') for c in cards))
kinds=dict(collections.Counter(c['supertype'] for c in cards))
assert rarities=={'Rare Holo':16,'Rare':16,'Uncommon':32,'Common':32,'OMITTED':6}
assert kinds=={'Pokémon':69,'Trainer':26,'Energy':7}
assert [int(c['number']) for c in cards if c.get('rarity')=='Rare Holo']==list(range(1,17))
independent=dict((int(n),name) for n,name in re.findall(r'^#(\d+) - (.+)$',(E/'pokellector.txt').read_text(encoding='utf-8'),re.M))
assert sorted(independent)==list(range(1,103))
differences=[{'number':int(c['number']),'primary':c['name'],'independent':independent[int(c['number'])]} for c in cards if independent[int(c['number'])]!=c['name']]
assert [d['number'] for d in differences]==[55,73,76,77,85,86,87]
records={int(r['card']['localId']):r for r in supplement['records']}
assert sorted(records)==[8,31,58,73,96,97,98,99,100,101,102]
for n in range(97,103):
    rec=records[n]; c=rec['card']
    assert c['rarity']=='Common' and c['variants']['holo'] is False
    assert c['variants']['normal'] is True and c['energyType']=='Normal'
    assert c['name']==cards[n-1]['name']
    assert rec['source_url']==f'https://api.tcgdex.net/v2/en/cards/base1-{n}'
assert records[8]['card']['variants']['holo'] is True
assert records[96]['card']['energyType']=='Special'
report=(ROOT/'docs/research/Catalog_Findings.md').read_text(encoding='utf-8')
rows=re.findall(r'^\| (\d+)/102 \| (.+)$',report,re.M)
assert sorted(int(n) for n,_ in rows)==list(range(1,103))
assert sum('| Holo |' in line for _,line in rows)==16
assert sum('| Non-holo |' in line for _,line in rows)==86
assert sum('Common ‡' in line for _,line in rows)==6
assert 'Imposter Professor Oak †' in report
assert cards[72]['name']=='Impostor Professor Oak'
source_files={
    'Catalog README':'api-readme.txt',
    'API card schema':'api-card-docs.txt',
    'Pokellector checklist':'pokellector.txt',
    'Bulbapedia Base Set':'bulbapedia-base-extracted.md',
    'Relentless Dragon identification':'relentlessdragon-early.md',
    'Bulbapedia Machamp':'bulbapedia-machamp.md',
    'Relentless Dragon Machamp':'relentlessdragon-machamp.md',
    'Bulbapedia Pikachu':'bulbapedia-pikachu-release.md',
    'Bulbapedia Jynx':'bulbapedia-jynx.md',
    'Bulbapedia Oak':'bulbapedia-oak.md',
    'Bulbapedia Energy':'bulbapedia-energy.md',
}
source_sections=dict(re.findall(r'^### ([^\n]+)\n(.*?)(?=^### |^## |\Z)',report,re.M|re.S))
quote_results=[]
for label,filename in source_files.items():
    evidence=norm((E/filename).read_text(encoding='utf-8'))
    section=source_sections[label]
    for line in section.splitlines():
        if line.startswith(('- **Quote:**','- **Exact checklist excerpts:**','- **Table excerpts:**')):
            for quote in re.findall('“([^”]+)”',line):
                passed=norm(quote) in evidence
                quote_results.append({'label':label,'quote':quote,'verified':passed,'evidence':filename})
for quote in [
 '© 1995, 96, 98, 99 Nintendo, Creatures, GAMEFREAK. © 1999 Wizards.',
 '© 1995, 96, 98 Nintendo, Creatures, GAMEFREAK. ©1999 Wizards.',
 '©1995, 96, 98 Nintendo, Creatures, GAMEFREAK. ©1999-2000 Wizards.',
]:
    passed=norm(quote) in norm((E/'relentlessdragon-early.md').read_text(encoding='utf-8'))
    quote_results.append({'label':'Relentless Dragon identification','quote':quote,'verified':passed,'evidence':'relentlessdragon-early.md'})
failures=[q for q in quote_results if not q['verified']]
result={'verified_at':datetime.datetime.now().astimezone().isoformat(),'primary_record_count':len(cards),'independent_checklist_count':len(independent),'number_coverage':'1-102, unique, no missing or extras','kind_counts':kinds,'primary_rarity_counts':rarities,'primary_missing_rarity_numbers':[int(c['number']) for c in cards if 'rarity' not in c],'supplementary_record_count':len(records),'primary_independent_name_differences':differences,'table_holo_count':16,'table_non_holo_count':86,'metadata_media_pricing_keys_absent':True,'quote_checks':quote_results,'quote_failures':failures}
(E/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='quote_checks'},ensure_ascii=False,indent=2))
print('VERIFIED QUOTE CHECKS',len(quote_results)-len(failures),'/',len(quote_results))
assert not failures, 'Some report quotes are not verbatim substrings of local source extracts'
print('PASS: catalog coverage, source fields, supplemental rarities/finishes, independent differences, complete report table and verbatim evidence quotes')
