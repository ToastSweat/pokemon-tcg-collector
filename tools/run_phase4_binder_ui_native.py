"""No-package isolated Unity binder native checks/build. Does not touch the main Editor."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import struct
import zlib

ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT/'.artifacts/phase4-ui-project'
OUT=ROOT/'.artifacts/phase4-ui'
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')

def stage(render=False):
    for folder in ['Assets/Presentation','Assets/Editor','ProjectSettings','Packages','.artifacts','Temp']:
        (PROJECT/folder).mkdir(parents=True,exist_ok=True)
    shutil.copy2(ROOT/'ProjectSettings/ProjectVersion.txt',PROJECT/'ProjectSettings/ProjectVersion.txt')
    # Built-in engine modules only; no registry or third-party packages/downloads.
    (PROJECT/'Packages/manifest.json').write_text(json.dumps({'dependencies':{name:'1.0.0' for name in ['com.unity.modules.imgui','com.unity.modules.audio','com.unity.modules.imageconversion','com.unity.modules.screencapture','com.unity.modules.jsonserialize']}}),encoding='utf-8')
    sources=[ROOT/'Assets/VintageCollector/Presentation/BinderView.cs',ROOT/'Assets/VintageCollector/Presentation/BinderLayoutRules.cs',ROOT/'Assets/VintageCollector/Editor/Phase4BinderPresentationChecks.cs']
    presentation=ROOT/'Assets/VintageCollector/Presentation/BinderPresentation.cs'
    if presentation.exists():sources.append(presentation)
    if render:sources.extend([ROOT/'TestsPhase4/UiRenderFixture.cs',ROOT/'TestsPhase4/UiFixtureBuild.cs'])
    else:
        # Remove only this worker's stale fixture scripts, not the project/cache or another Editor.
        for target in [PROJECT/'Assets/Presentation/UiRenderFixture.cs',PROJECT/'Assets/Editor/UiFixtureBuild.cs']:
            if target.exists():target.unlink()
    for f in sources:
        target=PROJECT/('Assets/Editor' if f.parent.name=='Editor' or f.name=='UiFixtureBuild.cs' else 'Assets/Presentation')/f.name
        shutil.copy2(f,target)
    return dict(os.environ,TMP=str(PROJECT/'Temp'),TEMP=str(PROJECT/'Temp'),TMPDIR=str(PROJECT/'Temp')),sources

def png_dimensions(path):
    raw=path.read_bytes()
    if raw[:8]!=b'\x89PNG\r\n\x1a\n':raise ValueError('PNG signature: '+str(path))
    at=8;size=None;ended=False;compressed=[]
    while at<len(raw):
        length=struct.unpack('>I',raw[at:at+4])[0];kind=raw[at+4:at+8];data=raw[at+8:at+8+length]
        crc=struct.unpack('>I',raw[at+8+length:at+12+length])[0]
        if zlib.crc32(kind+data)&0xffffffff!=crc:raise ValueError('PNG CRC: '+str(path))
        if kind==b'IHDR':size=struct.unpack('>II',data[:8])
        if kind==b'IDAT':compressed.append(data)
        at+=12+length
        if kind==b'IEND':ended=True;break
    if not size or min(size)<1 or not ended or not zlib.decompress(b''.join(compressed)):raise ValueError('Incomplete PNG '+str(path))
    return size

def main():
    p=argparse.ArgumentParser(__doc__);p.add_argument('--label',required=True);p.add_argument('--render',action='store_true');a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):p.error('Unsafe label')
    out=OUT/a.label;out.mkdir(parents=True,exist_ok=False)
    env,sources=stage(a.render)
    (out/'sources.json').write_text(json.dumps({str(f.relative_to(ROOT)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sources},indent=2),encoding='utf-8')
    report=PROJECT/'.artifacts/binder-engine-report.json'
    if report.exists():report.unlink()
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(PROJECT),'-logFile',str(out/'unity.log'),'-executeMethod','UiFixtureBuild.Run' if a.render else 'VintageCollector.EditorChecks.Phase4BinderPresentationChecks.Run']
    r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=600)
    (out/'process.log').write_text(json.dumps(cmd)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    if not a.render:
        if not report.exists():print('Missing report: '+str(out/'unity.log'));return r.returncode or 2
        data=json.loads(report.read_text(encoding='utf-8'));shutil.copy2(report,out/'results.json')
    elif not r.returncode:
        player=PROJECT/'.artifacts/player/BinderFixture.exe'
        capture=out/'captures';capture.mkdir()
        command=[str(player),'-screen-fullscreen','0','-logFile',str(out/'player.log'),'--binder-fixture-output='+str(capture)]
        r=subprocess.run(command,cwd=PROJECT,env=env,capture_output=True,text=True,timeout=240)
        (out/'player-process.log').write_text(json.dumps(command)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
        data=json.loads((capture/'results.json').read_text(encoding='utf-8'))
        shutil.copy2(capture/'results.json',out/'results.json')
        dimensions={name:png_dimensions(capture/name) for name in data['screenshots']}
        assert dimensions and len(dimensions)==len(data['screenshots'])
        for name,size in dimensions.items():
            expected=tuple(int(n) for n in name.split('-')[0].split('x'))
            assert tuple(size)==expected,(name,size,expected)
        (out/'png-verification.json').write_text(json.dumps({'passed':True,'screenshotCount':len(dimensions),'dimensions':dimensions},indent=2),encoding='utf-8')
    else: print('Build failed '+str(out/'unity.log'));return r.returncode
    assert data['checkCount']>0 and data['checkCount']==len(data['checks'])
    assert data['passed']==all(c['passed'] for c in data['checks'])
    print(json.dumps({'passed':data['passed'],'checkCount':data['checkCount'],'failures':[c for c in data['checks'] if not c['passed']],'failure':data.get('failure'),'report':str(out/'results.json')}))
    return r.returncode or (0 if data['passed'] else 1)

if __name__=='__main__':sys.exit(main())
