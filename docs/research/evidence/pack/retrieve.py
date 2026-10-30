"""Scoped text-only research retrieval. Never downloads image/audio assets."""
from pathlib import Path
from datetime import datetime
import argparse,json,re,requests
from html.parser import HTMLParser
from html import unescape
class TextParser(HTMLParser):
    def __init__(self):super().__init__();self.parts=[];self.links=[];self.skip=0;self.in_title=False;self.title=[];self.link=None
    def handle_starttag(self,tag,attrs):
        attrs=dict(attrs)
        if tag in ('script','style'):self.skip+=1
        if tag=='title':self.in_title=True
        if tag=='img':self.links.append({'text':attrs.get('alt',''),'url':attrs.get('src',''),'type':'image-reference-only','preceding_text':''.join(self.parts)[-250:]})
        if tag=='a' and attrs.get('href'):self.link={'text':'','url':attrs['href']};self.links.append(self.link)
        if tag in ('p','div','br','li','h1','h2','h3','tr'):self.parts.append('\n')
    def handle_endtag(self,tag):
        if tag in ('script','style') and self.skip:self.skip-=1
        if tag=='title':self.in_title=False
        if tag=='a':self.link=None
    def handle_data(self,data):
        if self.skip:return
        self.parts.append(data+' ')
        if self.in_title:self.title.append(data)
        if self.link is not None:self.link['text']+=data
ROOT=Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG/docs/research/evidence/pack')
p=argparse.ArgumentParser();p.add_argument('label');p.add_argument('url');a=p.parse_args()
assert re.fullmatch(r'[a-zA-Z0-9_-]+',a.label)
r=requests.get(a.url,timeout=60,headers={'User-Agent':'Mozilla/5.0 (research; text-only)'})
ct=r.headers.get('content-type','')
if 'text' not in ct and 'json' not in ct: raise RuntimeError('Not text: '+ct)
parser=TextParser();parser.feed(r.text)
title=''.join(parser.title)
text='\n'.join(re.sub(r'\s+',' ',line).strip() for line in ''.join(parser.parts).splitlines() if line.strip()) if 'html' in ct else r.text
links=parser.links if 'html' in ct else []
meta={'requested_url':a.url,'final_url':r.url,'status':r.status_code,'title':title,'accessed':datetime.now().astimezone().isoformat(),'content_type':ct}
ROOT.mkdir(parents=True,exist_ok=True)
(ROOT/(a.label+'.txt')).write_text(json.dumps(meta,ensure_ascii=False,indent=2)+'\n\n'+text,encoding='utf-8')
(ROOT/(a.label+'.links.json')).write_text(json.dumps(links,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(meta,ensure_ascii=False));print(text[:28000])
