"""Offline isolated native materials checks; never opens the main project or accepted builds."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import zlib
sys.dont_write_bytecode = True
from run_phase3_media import ROOT, fixtures
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')

def pins(files):
    return {f.relative_to(ROOT).as_posix(): hashlib.sha256(f.read_bytes()).hexdigest() for f in files}

def png(path, width, height, color):
    def chunk(tag, data):
        return struct.pack('>I', len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
    raw=(b'\0'+bytes(color)*width)*height
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',width,height,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))

def main():
    p=argparse.ArgumentParser(__doc__)
    p.add_argument('--label',required=True)
    p.add_argument('--filter',default='')
    p.add_argument('--existing',action='store_true')
    p.add_argument('--full-set',action='store_true')
    p.add_argument('--synthetic-full-set',action='store_true')
    p.add_argument('--mutation',choices=['cache-before-face'],default=None)
    a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): p.error('Unsafe label')
    project=ROOT/'.artifacts/materials-media-project'
    out=ROOT/'.artifacts/materials-media'/a.label
    out.mkdir(parents=True,exist_ok=True)
    if (out/'execution.log').exists(): p.error('Use a fresh label to retain evidence')
    for d in ['Assets/Media','Assets/Domain','Assets/Editor','ProjectSettings','Packages','Temp']:
        (project/d).mkdir(parents=True,exist_ok=True)
    fixtures(out)
    extra_inputs=[ROOT/'tools/run_phase3_media.py']
    if a.full_set or a.synthetic_full_set:
        if a.full_set and a.synthetic_full_set: p.error('Choose actual OR synthetic full set')
        from run_materials_media_fullset import full_set_inputs
        extra_inputs += full_set_inputs(out,a.synthetic_full_set,png)
        extra_inputs += [ROOT/'tools/run_materials_media_fullset.py',out/'full-set-oracle.json']
    for i in range(12): png(out/('unique-large-'+str(i)+'.png'),2016,2816,(30+i,80,130,255))
    png(out/'wrapper-mask.png',128,128,(128,128,128,255))
    png(out/'wrapper-wrong-mask.png',64,128,(128,128,128,255))
    (out/'junction-target').mkdir(exist_ok=True)
    shutil.copy2(out/'original-synthetic-card.png',out/'junction-target/original-synthetic-card.png')
    (out/'junction-target/original.bin').write_bytes(b'original synthetic test bytes')
    env=dict(os.environ,TEMP=str(project/'Temp'),TMP=str(project/'Temp'),TMPDIR=str(project/'Temp'),PHASE3_MEDIA_ROOT=str(ROOT),PHASE3_MEDIA_OUTPUT=str(out),MATERIALS_MEDIA_FILTER=a.filter,MATERIALS_MEDIA_FULL_SET='1' if a.full_set or a.synthetic_full_set else '0')
    if not (out/'reparse').exists():
        j=subprocess.run(['cmd.exe','/c','mklink','/J',str(out/'reparse'),str(out/'junction-target')],env=env,capture_output=True,text=True)
        (out/'junction.log').write_text(j.stdout+j.stderr,encoding='utf-8')
        if j.returncode: raise RuntimeError('Native junction setup failed')
    (project/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.2.14f1\nm_EditorVersionWithRevision: 6000.2.14f1 (589824c1fc31)\n',encoding='utf-8',newline='\n')
    (project/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.audio':'1.0.0','com.unity.modules.imageconversion':'1.0.0','com.unity.modules.jsonserialize':'1.0.0'}}),encoding='utf-8')
    sources=sorted((ROOT/'Assets/VintageCollector/Media').glob('*.cs'))+sorted((ROOT/'Assets/VintageCollector/Domain').glob('*.cs'))
    test=ROOT/('TestsPhase3/MediaNativeChecks.cs' if a.existing else 'TestsMaterials/MediaNativeChecks.cs')
    sources.append(test)
    allowed={('Editor' if s==test else s.parent.name,s.name) for s in sources}
    for f in (project/'Assets').rglob('*.cs'):
        if (f.parent.name,f.name) not in allowed: raise RuntimeError('Unexpected staged source: '+str(f))
    for s in sources:
        target=project/'Assets'/('Editor' if s==test else s.parent.name)/s.name
        text=s.read_text(encoding='utf-8')
        if a.mutation and s.name=='PresentationMediaLibrary.cs':
            # Mutation testing only: deliberately violate the security order in the
            # isolated stage. Production bytes and their pins are never modified.
            start=text.index('                    if(IsMask(entry.Role)) {')
            cache=text.index('                    // Validate each corresponding face BEFORE',start)
            end=text.index('                    long pixels=',cache)
            text=text[:start]+text[cache:end]+text[start:cache]+text[end:]
        if a.existing and s==test:
            # The historic budget test used identical bytes four times. Preserve its
            # assertions and exercise four UNIQUE resources under the new dedup contract.
            text=text.replace('Asset("card-face","original-synthetic-large-card.png",catalog.Cards[i].Id)', 'Asset("card-face","unique-large-"+i+".png",catalog.Cards[i].Id)')
        target.write_text(text,encoding='utf-8')
    protected=sorted((ROOT/'Assets/VintageCollector/Domain').glob('*.cs'))+[ROOT/'Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json',ROOT/'Assets/Supplied/AssetManifest.json']
    before=pins(protected)
    input_files=sources+[Path(__file__)]+extra_inputs
    inputs=pins(input_files)
    (out/'compiled-inputs.json').write_text(json.dumps(inputs,indent=2),encoding='utf-8')
    def staged_pins():
        files=list((project/'Assets').rglob('*.cs'))+[project/'Packages/manifest.json',project/'ProjectSettings/ProjectVersion.txt']
        return {f.relative_to(project).as_posix():hashlib.sha256(f.read_bytes()).hexdigest() for f in files}
    staged=staged_pins()
    (out/'staged-inputs.json').write_text(json.dumps(staged,indent=2),encoding='utf-8')
    installed=[EDITOR,EDITOR.parent/'Unity.dll']+[EDITOR.parent/('Data/Managed/UnityEngine/UnityEngine.'+name+'Module.dll') for name in ['Core','ImageConversion','Audio','JSONSerialize']]
    def installed_pins():
        return {f.relative_to(EDITOR.parent).as_posix():hashlib.sha256(f.read_bytes()).hexdigest() for f in installed}
    native=installed_pins()
    (out/'native-library-pins.json').write_text(json.dumps({'editorVersion':'6000.2.14f1','files':native},indent=2),encoding='utf-8')
    command=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(project),'-logFile',str(out/'unity.log'),'-executeMethod','MediaNativeChecks.Run' if a.existing else 'MaterialsMediaNativeChecks.Run']
    report=out/'native-results.json'
    r=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=480)
    (out/'execution.log').write_text(json.dumps(command)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    unchanged=before==pins(protected) and inputs==pins(input_files) and staged==staged_pins() and native==installed_pins()
    (out/'protected-inputs.json').write_text(json.dumps({'before':before,'after':pins(protected),'protectedUnchanged':before==pins(protected),'sourceInputsUnchanged':inputs==pins(input_files),'stageUnchanged':staged==staged_pins(),'nativeLibrariesUnchanged':native==installed_pins(),'unchanged':unchanged},indent=2),encoding='utf-8')
    if not unchanged: raise RuntimeError('Inputs changed during verification')
    if not report.exists(): print('No native report: '+str(out/'unity.log')); return r.returncode or 2
    data=json.loads(report.read_text(encoding='utf-8'))
    print(json.dumps(data))
    if data['testCount']<1 or data['testCount']!=len(data['checks']): raise RuntimeError('Zero/mismatched count')
    return r.returncode or (0 if data['passed'] and all(c['passed'] for c in data['checks']) else 1)

if __name__=='__main__': sys.exit(main())
