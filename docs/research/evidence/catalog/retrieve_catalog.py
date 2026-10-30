"""Phase-1 text/metadata retrieval only. No media, Unity, git, or runtime code."""
import concurrent.futures
import datetime
import hashlib
import json
from pathlib import Path
import urllib.parse
import urllib.request
from html.parser import HTMLParser

ROOT = Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/docs/research/evidence/catalog')
ROOT.mkdir(parents=True, exist_ok=True)

class Text(HTMLParser):
    def __init__(self):
        super().__init__(); self.parts=[]; self.links=[]; self.skip=0
    def handle_starttag(self, tag, attrs):
        a=dict(attrs)
        if tag in ('script','style'): self.skip+=1
        if tag=='a' and a.get('href'): self.links.append(a['href'])
        if tag in ('p','div','tr','li','h1','h2','h3','br'): self.parts.append('\n')
    def handle_endtag(self, tag):
        if tag in ('script','style'): self.skip=max(0,self.skip-1)
    def handle_data(self, data):
        if not self.skip: self.parts.append(data)

def fetch(label, url):
    try:
        req=urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'})
        with urllib.request.urlopen(req,timeout=45) as r:
            body=r.read(); final=r.url
        raw=body.decode('utf-8',errors='replace')
        p=Text(); p.feed(raw)
        text='\n'.join(line.strip() for line in ''.join(p.parts).splitlines() if line.strip())
        (ROOT/(label+'.txt')).write_text('URL: '+url+'\nRetrieved: '+datetime.datetime.now().astimezone().isoformat()+'\n\n'+text,encoding='utf-8')
        (ROOT/(label+'.links.json')).write_text(json.dumps(p.links,indent=2,ensure_ascii=False),encoding='utf-8')
        return label, text, p.links
    except Exception as e: return label, 'ERROR: '+str(e), []

if __name__=='__main__':
    queries={
      'search-printing':'https://www.bing.com/search?q='+urllib.parse.quote('Pokemon Base Set 1999 2000 unlimited copyright differences Machamp PSA'),
      'search-variants':'https://www.bing.com/search?q='+urllib.parse.quote('Base Set unlimited Pikachu yellow cheeks Machamp cosmos holo variants'),
      'search-guide':'https://www.bing.com/search?q='+urllib.parse.quote('Base set print run 1999 2000 shadowless unlimited guide'),
      'pokellector':'https://www.pokellector.com/Base-Set-Expansion/',
      'api-readme':'https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/README.md',
      'api-card-docs':'https://docs.pokemontcg.io/api-reference/cards/card-object',
    }
    for label,text,links in concurrent.futures.ThreadPoolExecutor(max_workers=6).map(lambda a:fetch(*a),queries.items()):
        print('\n===',label,'===\n',text[:18000])
        if label.startswith('search'): print('\nLINKS:',json.dumps([u for u in links if 'http' in u and 'bing.com' not in u][:30]))
    url='https://raw.githubusercontent.com/PokemonTCG/pokemon-tcg-data/master/cards/en/base1.json'
    with urllib.request.urlopen(url,timeout=45) as r: raw=r.read()
    cards=json.loads(raw)
    for card in cards: card.pop('images',None)
    output={
      'provenance':{'source_url':url,'source_title':'Pokémon TCG Developers — pokemon-tcg-data/cards/en/base1.json','accessed_at':datetime.datetime.now().astimezone().isoformat(),'raw_response_sha256':hashlib.sha256(raw).hexdigest(),'source_field_policy':'Original card fields and values preserved except images removed (no media fetched). Source ids are provenance, not chosen runtime ids. This set-level catalog does not identify printing.','excluded_source_fields':['images'],'card_count':len(cards)},
      'cards':cards,
    }
    (ROOT/'card_metadata.json').write_text(json.dumps(output,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print('\nMETADATA',len(cards),'CARDS; fields:',sorted({k for c in cards for k in c}))
