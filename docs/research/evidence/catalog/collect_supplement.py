"""Collect source metadata for gaps and printing cautions; no assets requested."""
import concurrent.futures
import datetime
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT=Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/docs/research/evidence/catalog')
NUMBERS=[8,31,58,73,96,97,98,99,100,101,102]
REMOVED={'image','logo','symbol','pricing'}

def clean(value):
    if isinstance(value,dict): return {k:clean(v) for k,v in value.items() if k not in REMOVED}
    if isinstance(value,list): return [clean(v) for v in value]
    return value

def fetch(n):
    url=f'https://api.tcgdex.net/v2/en/cards/base1-{n}'
    with urllib.request.urlopen(url,timeout=45) as r: raw=r.read()
    return {'source_url':url,'accessed_at':datetime.datetime.now().astimezone().isoformat(),'raw_response_sha256':hashlib.sha256(raw).hexdigest(),'card':clean(json.loads(raw))}

if __name__=='__main__':
    records=list(concurrent.futures.ThreadPoolExecutor(max_workers=4).map(fetch,NUMBERS))
    result={'provenance':{'source_title':'TCGdex English Base Set card-object metadata','record_count':len(records),'policy':'Original source fields and values, with media URL and pricing fields removed recursively. No media requested. This is supplementary research, not authoritative physical-print certification.','removed_field_names':sorted(REMOVED)},'records':records}
    out=ROOT/'tcgdex_supplement.json'
    out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    for rec in records:
        c=rec['card']; print(rec['source_url'],c['name'],'rarity=',c.get('rarity'),'stage=',c.get('stage'),'energyType=',c.get('energyType'),'variants=',c.get('variants'))
        if c['localId'] in ('8','31','58'): print('VARIANTS',json.dumps(c.get('variants_detailed'),ensure_ascii=False))
    print('SAVED',out,len(records))
