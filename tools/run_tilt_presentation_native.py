"""Owned isolated native integration checks; no accepted build/main Editor/live saves."""
import argparse, hashlib, json, os, shutil, struct, subprocess, sys, zlib
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT/'.artifacts/tilt-presentation-project'
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def validate_png(p):
    b=p.read_bytes(); assert b[:8]==b'\x89PNG\r\n\x1a\n'; at=8; dims=None; end=False
    while at<len(b):
        n=struct.unpack('>I',b[at:at+4])[0]; k=b[at+4:at+8]; d=b[at+8:at+8+n]
        assert len(d)==n and at+12+n<=len(b)
        assert zlib.crc32(k+d)&0xffffffff==struct.unpack('>I',b[at+8+n:at+12+n])[0]
        if k==b'IHDR': dims=struct.unpack('>II',d[:8])
        at+=12+n
        if k==b'IEND': end=True; break
    assert end and at==len(b) and dims and min(dims)>0
    return {'dimensions':dims,'sha256':digest(p)}
def stage(out,mode='contract',without_backend=False):
    for d in ['Assets/VintageCollector/Editor','Assets/VintageCollector/Presentation','Assets/VintageCollector/Media','ProjectSettings','Packages','Temp']:(PROJECT/d).mkdir(parents=True,exist_ok=True)
    sources=[]
    for part in ['Presentation','Media','Domain']:
        sources+=sorted((ROOT/'Assets/VintageCollector'/part).glob('*.cs'))
    sources += sorted((ROOT/'Assets/VintageCollector/Resources/Presentation').glob('*.shader'))
    sources += [ROOT/'Assets/VintageCollector/Editor/TiltPresentationChecks.cs']
    sources += [ROOT/'Assets/VintageCollector/Editor'/n for n in ['Phase2PresentationChecks.cs','Phase3PresentationChecks.cs']]
    if mode=='legacy-render':
        sources += [ROOT/'TestsMaterials/RenderFixture.cs',ROOT/'TestsMaterials/RenderFixtureBuild.cs']
    sources += sorted((ROOT/'Assets/VintageCollector/Data').glob('*.json'))
    manifest=ROOT/'Assets/Supplied/BaseSetMaterials/Manifest.json'
    sources += [manifest]
    sources += sorted({ROOT/e['relativePath'] for e in json.loads(manifest.read_text(encoding='utf-8'))['assets']})
    targets={PROJECT/s.relative_to(ROOT):s for s in sources}
    for name in ['RenderFixture.cs','RenderFixtureBuild.cs']:
        src=ROOT/'TestsMaterials'/name
        if src in sources:
            del targets[PROJECT/src.relative_to(ROOT)]
            targets[PROJECT/('Assets/Editor' if 'Build' in name else 'Assets')/name]=src
    for name in ['IntegrationFixture.cs','IntegrationFixtureBuild.cs']:
        src=ROOT/'TestsTilt'/name
        if src.exists(): targets[PROJECT/('Assets/Editor' if 'Build' in name else 'Assets')/name]=src
    for f in (PROJECT/'Assets').rglob('*.cs'):
        if f not in targets:f.unlink()
    for dest,src in targets.items():dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dest)
    if without_backend:
        shader=PROJECT/'Assets/VintageCollector/Resources/Presentation/TiltedSurface.shader'
        if shader.exists():shader.unlink()
    (PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.2.14f1\n',encoding='utf-8')
    (PROJECT/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.'+m:'1.0.0' for m in ['audio','imageconversion','imgui','jsonserialize','screencapture','ui']}}),encoding='utf-8')
    pins={s.relative_to(ROOT).as_posix():{'source':digest(s),'staged':digest(d) if d.exists() else None} for d,s in targets.items()}
    assert pins and all(v['source']==v['staged'] or (without_backend and k.endswith('/TiltedSurface.shader') and v['staged'] is None) for k,v in pins.items())
    (out/'source-pins.json').write_text(json.dumps(pins,indent=2),encoding='utf-8')
    return dict(os.environ,TEMP=str(PROJECT/'Temp'),TMP=str(PROJECT/'Temp'),TMPDIR=str(PROJECT/'Temp'),TILT_PRESENTATION_OUT=str(out),MATERIALS_RENDER_OUT=str(out),MATERIALS_RENDER_ROOT=str(PROJECT))
def main():
    ap=argparse.ArgumentParser(__doc__);ap.add_argument('--label',required=True);ap.add_argument('--mode',choices=['contract','build','legacy2','legacy3','legacy-render'],default='contract');ap.add_argument('--without-backend',action='store_true');a=ap.parse_args()
    assert a.label and all(c.isalnum() or c in '-_' for c in a.label)
    out=ROOT/'.artifacts/tilt-presentation'/a.label;out.mkdir(parents=True,exist_ok=False)
    env=stage(out,a.mode,a.without_backend)
    entry={'contract':'TiltPresentationChecks.Run','build':'IntegrationFixtureBuild.Run','legacy2':'VintageCollector.EditorChecks.Phase2PresentationChecks.Run','legacy3':'VintageCollector.EditorChecks.Phase3PresentationChecks.Run','legacy-render':'RenderFixtureBuild.Run'}[a.mode]
    legacy_report=PROJECT/'.artifacts/presentation-engine-report.json'
    if legacy_report.exists():legacy_report.unlink()
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(PROJECT),'-logFile',str(out/'unity.log'),'-executeMethod',entry]
    (out/'command.json').write_text(json.dumps(cmd,indent=2),encoding='utf-8')
    r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=600)
    (out/'execution.json').write_text(json.dumps({'exit':r.returncode,'stdout':r.stdout,'stderr':r.stderr},indent=2),encoding='utf-8')
    report=out/'results.json'
    if a.mode in ['legacy2','legacy3'] and legacy_report.exists():shutil.copy2(legacy_report,report)
    if a.mode=='legacy-render' and r.returncode==0:
        player=[str(PROJECT/'.artifacts/player/RenderFixture.exe'),'-screen-fullscreen','0','-screen-width','1000','-screen-height','800','-logFile',str(out/'player.log')]
        r=subprocess.run(player,cwd=PROJECT,env=env,capture_output=True,text=True,timeout=180)
        (out/'player-execution.json').write_text(json.dumps({'exit':r.returncode,'stdout':r.stdout,'stderr':r.stderr,'command':player},indent=2),encoding='utf-8')
    pins=json.loads((out/'source-pins.json').read_text(encoding='utf-8'))
    after={k:digest(ROOT/k) for k in pins}
    unchanged=all(v['source']==after[k] for k,v in pins.items())
    (out/'source-after.json').write_text(json.dumps({'unchanged':unchanged,'pins':after},indent=2),encoding='utf-8')
    assert unchanged,'Sources changed during native execution; label cannot be admitted'
    if a.mode=='build' and r.returncode==0:
        exe=PROJECT/'.artifacts/player/Integration.exe'
        assert exe.is_file()
        (out/'build.json').write_text(json.dumps({'built':True,'nativeExit':r.returncode,'exeSha256':digest(exe),'withoutBackend':a.without_backend,'sourcePins':pins},indent=2),encoding='utf-8')
    if report.exists():
        data=json.loads(report.read_text(encoding='utf-8'));print(json.dumps(data));assert data['checkCount']==len(data['checks'])
        pngs={p.name:validate_png(p) for p in out.glob('*.png')}
        (out/'png-evidence.json').write_text(json.dumps(pngs,indent=2),encoding='utf-8')
        if 'screenshotCount' in data:assert len(pngs)==data['screenshotCount']==len(data['screenshots'])
        return r.returncode or (0 if data['passed'] and data['checkCount'] else 1)
    print('Native exit',r.returncode,'log',out/'unity.log');return r.returncode or (0 if a.mode=='build' else 2)
if __name__=='__main__':sys.exit(main())
