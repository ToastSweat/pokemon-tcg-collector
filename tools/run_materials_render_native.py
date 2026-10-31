"""Exact-source isolated native material renderer/host checks; no main Editor or live saves."""
import argparse, hashlib, json, os, shutil, struct, subprocess, sys, zlib
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT/'.artifacts/materials-render-project'
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def validate_png(p):
    b=p.read_bytes(); assert b[:8]==b'\x89PNG\r\n\x1a\n'; at=8; dims=None; end=False
    while at<len(b):
        assert len(b)-at>=12
        n=struct.unpack('>I',b[at:at+4])[0]; k=b[at+4:at+8]; d=b[at+8:at+8+n]
        assert len(d)==n and at+12+n<=len(b)
        assert zlib.crc32(k+d)&0xffffffff==struct.unpack('>I',b[at+8+n:at+12+n])[0]
        if k==b'IHDR': dims=struct.unpack('>II',d[:8])
        at+=12+n
        if k==b'IEND': end=True; break
    assert end and at==len(b) and dims and min(dims)>0
    return {'dimensions':dims,'sha256':digest(p)}
def main():
    ap=argparse.ArgumentParser(__doc__); ap.add_argument('--label',required=True)
    ap.add_argument('--mode',choices=['native','graphics','legacy-native','legacy-phase2','legacy-host','legacy-host-play','legacy-graphics'],default='native')
    ap.add_argument('--mutant-without-wrapper',action='store_true',help='Isolated staged defect control only; never edits production source')
    a=ap.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): ap.error('unsafe label')
    out=ROOT/'.artifacts/materials-render'/a.label; out.mkdir(parents=True,exist_ok=False)
    for d in ['Assets/VintageCollector/Editor','Assets/Supplied','ProjectSettings','Packages','Temp','.artifacts/phase3-host']:(PROJECT/d).mkdir(parents=True,exist_ok=True)
    sources=[]
    for part in ['Domain','Persistence','Presentation','Media','Collections']:
        sources+=sorted((ROOT/'Assets/VintageCollector'/part).glob('*.cs'))
    sources += [ROOT/'Assets/VintageCollector/Runtime'/n for n in ['OpeningGame.cs','GamePaths.cs','UnityStateSerializer.cs','Phase3MediaHost.cs']]
    sources += sorted((ROOT/'Assets/VintageCollector/Data').glob('*.json'))
    sources += sorted((ROOT/'Assets/VintageCollector/Resources/Presentation').glob('*.shader'))
    sources += [ROOT/'Assets/Supplied/AssetManifest.json']
    editor=['MaterialsPresentationChecks.cs','Phase3PresentationChecks.cs','Phase2PresentationChecks.cs','Phase3MediaHostChecks.cs']
    sources += [ROOT/'Assets/VintageCollector/Editor'/n for n in editor]
    target_files={}
    for s in sources: target_files[PROJECT/s.relative_to(ROOT)]=s
    for s in [ROOT/'TestsMaterials/RenderFixture.cs',ROOT/'TestsMaterials/RenderFixtureBuild.cs']:
        if s.exists(): target_files[PROJECT/('Assets/VintageCollector/Editor' if 'Build' in s.name else 'Assets')/s.name]=s
    if a.mode=='legacy-graphics':
        s=ROOT/'TestsPhase3/PresentationRenderFixture.cs'; target_files[PROJECT/'Assets/PresentationRenderFixture.cs']=s
    for f in (PROJECT/'Assets').rglob('*.cs'):
        if f not in target_files: f.unlink()
    for dest,src in target_files.items(): dest.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(src,dest)
    mutant_key='Assets/VintageCollector/Presentation/TabletopPresentation.cs'
    if a.mutant_without_wrapper:
        assert a.mode=='graphics'
        p=PROJECT/mutant_key; text=p.read_text(encoding='utf-8')
        call='if (metallic!=null) DrawWrapperMetallic(r,supplied,front!=null?metallic.WrapperFrontMask:metallic.WrapperBackMask);'
        assert text.count(call)==1
        p.write_text(text.replace(call,'// ISOLATED DEFECT CONTROL: missing wrapper reflection call.'),encoding='utf-8')
        (out/'defect-injection.json').write_text(json.dumps({'target':mutant_key,'removedCall':call,'scope':'Isolated stage only; original production source unchanged'},indent=2),encoding='utf-8')
    for stale in [PROJECT/'.artifacts/presentation-engine-report.json',PROJECT/'.artifacts/phase3-host/results.json']:
        if stale.exists(): stale.unlink()
    if a.mode=='legacy-graphics':
        captures=PROJECT/'.artifacts/phase3-presentation/render'
        if captures.exists(): shutil.rmtree(captures)
    # Optional real private supplied materials; only this dedicated approved subtree.
    supplied=ROOT/'Assets/Supplied/BaseSetMaterials'
    if (supplied/'Manifest.json').exists(): shutil.copytree(supplied,PROJECT/'Assets/Supplied/BaseSetMaterials',dirs_exist_ok=True)
    (PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.2.14f1\n',encoding='utf-8')
    modules=['audio','imageconversion','imgui','jsonserialize','screencapture','ui']
    (PROJECT/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.'+m:'1.0.0' for m in modules}}),encoding='utf-8')
    before=digest(ROOT/'Assets/Supplied/AssetManifest.json')
    assert json.loads((ROOT/'Assets/Supplied/AssetManifest.json').read_text(encoding='utf-8'))['assets']==[]
    supplied_pins={}
    if (supplied/'Manifest.json').exists():
        for src in sorted(supplied.rglob('*')):
            if src.is_file() and src.suffix.lower() in ['.png','.json']:
                dest=PROJECT/src.relative_to(ROOT)
                supplied_pins[src.relative_to(ROOT).as_posix()]={'source':digest(src),'staged':digest(dest)}
        assert all(v['source']==v['staged'] for v in supplied_pins.values())
    (out/'supplied-pins.json').write_text(json.dumps(supplied_pins,indent=2),encoding='utf-8')
    pins={str(src.relative_to(ROOT)).replace('\\','/'):{'source':digest(src),'staged':digest(dest)} for dest,src in target_files.items()}
    assert pins and all(v['source']==v['staged'] or (a.mutant_without_wrapper and k==mutant_key) for k,v in pins.items())
    (out/'source-pins.json').write_text(json.dumps(pins,indent=2),encoding='utf-8')
    env=dict(os.environ,TEMP=str(PROJECT/'Temp'),TMP=str(PROJECT/'Temp'),TMPDIR=str(PROJECT/'Temp'),MATERIALS_RENDER_OUT=str(out),MATERIALS_RENDER_ROOT=str(PROJECT),VCC_HOST_REPORT=str(PROJECT/'.artifacts/phase3-host/results.json'),VCC_HOST_SLICE='play' if a.mode=='legacy-host-play' else 'all')
    entries={'native':'MaterialsPresentationChecks.Run','graphics':'RenderFixtureBuild.Run','legacy-native':'VintageCollector.EditorChecks.Phase3PresentationChecks.Run','legacy-phase2':'VintageCollector.EditorChecks.Phase2PresentationChecks.Run','legacy-host':'VintageCollector.EditorChecks.Phase3MediaHostChecks.Run','legacy-host-play':'VintageCollector.EditorChecks.Phase3MediaHostChecks.Run','legacy-graphics':'RenderFixtureBuild.RunLegacy'}
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(PROJECT),'-logFile',str(out/'unity.log'),'-executeMethod',entries[a.mode]]
    (out/'command.json').write_text(json.dumps(cmd,indent=2),encoding='utf-8')
    def run(cmd,log):
        r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=600)
        (out/log).write_text(r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8'); return r.returncode
    code=run(cmd,'execution.log')
    report=out/'results.json'
    if a.mode in ['graphics','legacy-graphics'] and not code:
        exe=PROJECT/'.artifacts/player/RenderFixture.exe'
        code=run([str(exe),'-screen-fullscreen','0','-screen-width','1000','-screen-height','800','-logFile',str(out/'player.log')],'player-execution.log')
        if a.mode=='legacy-graphics':
            captures=PROJECT/'.artifacts/phase3-presentation/render'; shutil.copytree(captures,out/'captures'); shutil.copy2(captures/'render-report.json',report)
    elif a.mode in ['legacy-native','legacy-phase2']:
        p=PROJECT/'.artifacts/presentation-engine-report.json'
        if p.exists(): shutil.copy2(p,report)
    elif a.mode in ['legacy-host','legacy-host-play']:
        p=PROJECT/'.artifacts/phase3-host/results.json'
        if p.exists(): shutil.copy2(p,report)
    assert before==digest(ROOT/'Assets/Supplied/AssetManifest.json')
    (out/'protected-manifest.json').write_text(json.dumps({'unchanged':True,'sha256':before}),encoding='utf-8')
    if not report.exists(): print('No native report; '+str(out/'unity.log')); return code or 2
    data=json.loads(report.read_text(encoding='utf-8')); print(json.dumps(data))
    assert data['checkCount']>0 and data['checkCount']==len(data['checks'])
    pngs={p.relative_to(out).as_posix():validate_png(p) for p in out.rglob('*.png')}
    (out/'png-evidence.json').write_text(json.dumps(pngs,indent=2),encoding='utf-8')
    if 'screenshots' in data: assert len(data['screenshots'])==data['screenshotCount']==len(pngs)
    return code or (0 if data['passed'] else 1)
if __name__=='__main__': sys.exit(main())
