"""Private native full-set oracle preparation. Never publishes or modifies supplied art."""
import hashlib
import json
from pathlib import Path, PurePosixPath
import PIL
from typing import cast
from PIL import Image
from run_phase3_media import ROOT

def full_set_inputs(out, synthetic, make_png):
    catalog=json.loads((ROOT/'Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json').read_text(encoding='utf-8'))['cards']
    if synthetic:
        directory=out/'full-set-synthetic'
        directory.mkdir(exist_ok=True)
        assets=[]
        def add(role, name, width, height, color, card_id=''):
            file=directory/name
            if not file.exists(): make_png(file,width,height,color)
            assets.append(dict(role=role,cardId=card_id,relativePath=file.relative_to(ROOT).as_posix(),source='original synthetic full-quality fixture',rights='unknown',restrictions='test only',sha256=hashlib.sha256(file.read_bytes()).hexdigest()))
        for i,card in enumerate(catalog): add('card-face','face-'+str(i)+'.png',600,825,(i+1,40,110,255),card['id'])
        for card in catalog:
            if card['finish']=='holo': add('holo-mask','shared-holo.png',600,825,(128,128,128,255),card['id'])
        add('card-back','back.png',600,825,(60,50,140,255))
        for role,color in [('wrapper-front',(45,50,55,255)),('wrapper-back',(65,70,75,255)),('wrapper-front-mask',(90,90,90,255)),('wrapper-back-mask',(140,140,140,255))]: add(role,role+'.png',260,475,color)
        manifest=directory/'Manifest.json'
        manifest.write_text(json.dumps(dict(schemaVersion=1,status='synthetic-full-quality-not-supplied',intendedUse='test',redistributionPermission='none',rightsDefault='unknown',notes='',assets=assets)),encoding='utf-8')
    else:
        manifest=ROOT/'Assets/Supplied/BaseSetMaterials/Manifest.json'
        if not manifest.is_file(): raise RuntimeError('Prepared private manifest not ready; no waiting. Run again with --full-set after authoring completes.')
        assets=json.loads(manifest.read_text(encoding='utf-8'))['assets']
    bindings=[]
    files=[manifest]
    unique={}
    for entry in assets:
        relative=entry['relativePath']
        path=PurePosixPath(relative)
        if '\\' in relative or ':' in relative or path.is_absolute() or '..' in path.parts or not relative.startswith('.artifacts/' if synthetic else 'Assets/Supplied/'):
            raise RuntimeError('Unsafe oracle input')
        file=ROOT.joinpath(*path.parts)
        for ancestor in [file,*file.parents]:
            if ancestor.is_symlink() or ancestor.is_junction(): raise RuntimeError('Reparse oracle input')
            if ancestor==ROOT: break
        data=file.read_bytes()
        if hashlib.sha256(data).hexdigest()!=entry['sha256']: raise RuntimeError('Hash mismatch before independent oracle decode')
        with Image.open(file) as image:
            rgba=image.convert('RGBA')
            width,height=rgba.size
            points=[(0,0),(width-1,height-1),(width//2,height//2),(width//3,height//3),(width//2,height//4)]
            samples=[dict(x=x,y=height-1-y,r=p[0],g=p[1],b=p[2],a=p[3]) for x,y in points for p in [cast(tuple[int, ...], rgba.getpixel((x,y)))]]
            rgba_hash=hashlib.sha256(rgba.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes()).hexdigest()
        mask=entry['role'].endswith('-mask') or entry['role']=='holo-mask'
        unique[(entry['sha256'],mask,width,height)]=width*height
        bindings.append(dict(role=entry['role'],cardId=entry.get('cardId') or '',relativePath=relative,sha256=entry['sha256'],width=width,height=height,mask=mask,rgbaSha256=rgba_hash,samples=samples))
        files.append(file)
    oracle=dict(synthetic=synthetic,manifestRelativePath=manifest.relative_to(ROOT).as_posix(),expectedBindings=len(bindings),expectedUnique=len(unique),expectedPixels=sum(unique.values()),pillowVersion=PIL.__version__,bindings=bindings)
    (out/'full-set-oracle.json').write_text(json.dumps(oracle,indent=2),encoding='utf-8')
    return list(dict.fromkeys(files))
