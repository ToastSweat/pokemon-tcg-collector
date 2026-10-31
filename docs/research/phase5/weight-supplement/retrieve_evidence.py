"""Research-only retrieval: exactly four user-supplied numeric PNGs; no other media."""
from pathlib import Path
from datetime import datetime, timezone
import concurrent.futures
import hashlib
import json
import struct
import subprocess
import sys
import urllib.request

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
CITATIONS = Path('C:/Users/pada1/AppData/Local/hermes/profiles/chatgpt/skills/research/grounded-citations/scripts/sources.py')
ARTICLE = 'https://www.elitefourum.com/t/the-english-pokemon-card-rarity-guide/39762'
IMAGE_IDS = [
    'c10b7cdcae9c7f888eea6b72108aca993c11c6bc',
    '3a4f6ceeac70e776f92552a6b0097d6c62e3ac4e',
    '412a4015bff1ef1eb12453acbc355cf7448ab776',
    '18d87beedd4eb026e3af510c22670f09e3d2ce30',
]

def now():
    return datetime.now(timezone.utc).isoformat()

def ledger(*args):
    p = subprocess.run([sys.executable, '-B', str(CITATIONS), '--ledger', str(HERE/'citations-ledger.json'), *args], capture_output=True, text=True, encoding='utf-8')
    print(p.stdout, end='')
    if p.returncode:
        raise RuntimeError(p.stderr)
    return p.stdout

def fetch(image_id):
    suffix = f'/uploads/default/original/3X/{image_id[0]}/{image_id[1]}/{image_id}.png'
    attempts = []
    for host in ['https://efour.b-cdn.net', 'https://www.elitefourum.com']:
        url = host+suffix
        entry: dict = {'requestedUrl': url, 'startedAtUtc': now()}
        try:
            req = urllib.request.Request(url, headers={'User-Agent':'Mozilla/5.0 (research numeric table only)'})
            with urllib.request.urlopen(req, timeout=60) as response:
                data = response.read()
                entry.update(status=response.status, finalUrl=response.url, contentType=response.headers.get('Content-Type'))
            assert data[:8] == b'\x89PNG\r\n\x1a\n', 'not PNG'
            width, height = struct.unpack('>II', data[16:24])
            (HERE/f'{image_id}.png').write_bytes(data)
            entry.update(success=True, bytes=len(data), sha256=hashlib.sha256(data).hexdigest(), sha1=hashlib.sha1(data).hexdigest(), width=width, height=height, file=f'{image_id}.png')
        except Exception as exc:
            entry.update(success=False, error=f'{type(exc).__name__}: {exc}')
        entry['finishedAtUtc'] = now()
        attempts.append(entry)
        if entry['success']:
            break
    return {'imageSourceId':image_id, 'suppliedUrl':'https://efour.b-cdn.net'+suffix, 'attempts':attempts}

if __name__ == '__main__':
    HERE.mkdir(parents=True, exist_ok=True)
    manifest = {'startedAtUtc':now(), 'scope':'Numeric research charts only. No runtime/art permission.', 'inputs':[]}
    paths = [
        'docs/research/phase5/jungle/rarity-guide.raw.html',
        'docs/research/phase5/jungle/rarity-guide.text.txt',
        'docs/research/phase5/jungle/rarity-guide.retrieval.json',
        'docs/research/phase5/jungle/tcg-data.raw.json',
        'docs/research/phase5/fossil/evidence/api.json',
        'Authoring/Phase5/Data/jungle-en-unlimited1999.catalog.json',
        'Authoring/Phase5/Data/fossil-en-unlimited1999.catalog.json',
    ]
    for rel in paths:
        p = ROOT/rel
        data = p.read_bytes()
        manifest['inputs'].append({'path':rel, 'bytes':len(data), 'sha256':hashlib.sha256(data).hexdigest(), 'observedAtUtc':now()})
    ledger('reset')
    ledger('add',ARTICLE,'--title','The English Pokémon card rarity guide — existing retrieved article snapshot')
    # Preserve the source text as retrieved by the earlier worker; no image URLs are followed.
    text = (ROOT/paths[1]).read_bytes()
    (HERE/'article-context.text.txt').write_bytes(text)
    raw = (ROOT/paths[0]).read_text(encoding='utf-8')
    for image_id in IMAGE_IDS:
        url = f'https://efour.b-cdn.net/uploads/default/original/3X/{image_id[0]}/{image_id[1]}/{image_id}.png'
        assert url in raw, f'supplied source ID not literally in retrieved article: {image_id}'
        ledger('add',url,'--title',f'Numeric rarity table {image_id}')
    results = list(concurrent.futures.ThreadPoolExecutor(max_workers=4).map(fetch,IMAGE_IDS))
    manifest.update(charts=results, finishedAtUtc=now())
    (HERE/'retrieval-manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps(manifest,indent=2,ensure_ascii=False))
    assert all(any(a['success'] for a in r['attempts']) for r in results)
