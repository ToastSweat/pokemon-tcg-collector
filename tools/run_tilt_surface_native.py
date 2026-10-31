"""Exact-source isolated native D3D11 player proof. No live project, saves or installs."""
import argparse,hashlib,json,os,shutil,struct,subprocess,sys,zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];PROJECT=ROOT/'.artifacts/tilt-surface-project'
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def png(p):
    b=p.read_bytes();assert b[:8]==b'\x89PNG\r\n\x1a\n';at=8;dims=None;end=False
    while at<len(b):
        assert len(b)-at>=12;n=struct.unpack('>I',b[at:at+4])[0];k=b[at+4:at+8];d=b[at+8:at+8+n];assert len(d)==n and at+n+12<=len(b)
        assert zlib.crc32(k+d)&0xffffffff==struct.unpack('>I',b[at+8+n:at+12+n])[0]
        if k==b'IHDR':dims=struct.unpack('>II',d[:8])
        at+=n+12
        if k==b'IEND':end=True;break
    assert end and at==len(b) and dims== (720,720)
    return {'dimensions':dims,'sha256':digest(p),'completeCRC':True}
def main():
    ap=argparse.ArgumentParser(__doc__);ap.add_argument('--label',required=True);ap.add_argument('--slice',default='full');ap.add_argument('--missing-shader',action='store_true');ap.add_argument('--unsupported-shader',action='store_true');ap.add_argument('--mutant-leak-material',action='store_true');ap.add_argument('--compile-only',action='store_true');a=ap.parse_args()
    assert a.label and all(c.isalnum() or c in '-_' for c in a.label)
    out=PROJECT/'evidence'/a.label;out.mkdir(parents=True,exist_ok=False)
    for d in ['Assets/VintageCollector/Editor','Packages','ProjectSettings','Temp','player']:(PROJECT/d).mkdir(parents=True,exist_ok=True)
    pairs={PROJECT/p:ROOT/p for p in ['Assets/VintageCollector/Presentation/HoverTiltMotion.cs','Assets/VintageCollector/Presentation/TiltedSurfaceRenderer.cs','Assets/VintageCollector/Editor/TiltSurfaceChecks.cs']}
    if not a.compile_only:pairs[PROJECT/'Assets/SurfaceGpuFixture.cs']=ROOT/'TestsTilt/SurfaceGpuFixture.cs'
    shader=ROOT/'Assets/VintageCollector/Resources/Presentation/TiltedSurface.shader'
    if shader.exists() and not a.missing_shader:pairs[PROJECT/shader.relative_to(ROOT)]=shader
    allowed=set(pairs)
    for f in (PROJECT/'Assets').rglob('*'):
        if f.suffix in ['.cs','.shader'] and f not in allowed:f.unlink()
    for dest,src in pairs.items():dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dest)
    mutations=[]
    if a.unsupported_shader:
        p=PROJECT/shader.relative_to(ROOT);text=p.read_text();p.write_text(text.replace('#pragma target 3.0','#pragma target 3.0\n            #pragma only_renderers glcore'));mutations.append(p)
    if a.mutant_leak_material:
        p=PROJECT/'Assets/VintageCollector/Presentation/TiltedSurfaceRenderer.cs';text=p.read_text();old='if(Application.isPlaying)UnityEngine.Object.Destroy(material);else UnityEngine.Object.DestroyImmediate(material);material=null;';assert old in text;p.write_text(text.replace(old,'material=null;'));mutations.append(p)
    if mutations:(out/'defect-injection.json').write_text(json.dumps([str(p.relative_to(PROJECT)) for p in mutations]))
    manifest=ROOT/'Assets/Supplied/BaseSetMaterials/Manifest.json';data=json.loads(manifest.read_text());assets=data['assets']
    selected=[x for x in assets if x['role'] not in ['card-face','holo-mask'] or x['cardId'] in ['pkmn-base-en-unlimited1999-001-holo-standard','pkmn-base-en-unlimited1999-017-nonholo-standard']]
    supplied={ROOT/x['relativePath'] for x in selected}|{manifest,ROOT/'Assets/Supplied/AssetManifest.json'}
    for src in supplied:
        dest=PROJECT/src.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dest)
    pins={str(s.relative_to(ROOT)).replace('\\','/'):{'source':digest(s),'staged':digest(d)} for d,s in pairs.items()}
    allprotected={ROOT/x['relativePath'] for x in assets}|{manifest,ROOT/'Assets/Supplied/AssetManifest.json'}
    protected={str(p.relative_to(ROOT)):digest(p) for p in allprotected}
    (out/'source-pins.json').write_text(json.dumps(pins,indent=2));assert all(x['source']==x['staged'] or PROJECT/k in mutations for k,x in pins.items())
    (PROJECT/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.2.14f1\n')
    (PROJECT/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.'+m:'1.0.0' for m in ['imageconversion','imgui','jsonserialize','screencapture','ui']}}))
    env=dict(os.environ,TEMP=str(PROJECT/'Temp'),TMP=str(PROJECT/'Temp'),TMPDIR=str(PROJECT/'Temp'),TILT_SURFACE_OUT=str(out),TILT_SURFACE_ROOT=str(PROJECT),TILT_SURFACE_SLICE='faults' if a.missing_shader or a.unsupported_shader else a.slice)
    logs=[];code=2
    commands=[[str(EDITOR),'-batchmode','-nographics','-projectPath',str(PROJECT),'-logFile',str(out/'unity.log'),'-executeMethod','TiltSurfaceChecks.CompileOnly' if a.compile_only else 'TiltSurfaceChecks.Build']]
    if not a.compile_only:commands.append([str(PROJECT/'player/SurfaceFixture.exe'),'-screen-fullscreen','0','-screen-width','720','-screen-height','720','-logFile',str(out/'player.log')])
    for cmd in commands:
        r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=600);logs.append({'command':cmd,'exit':r.returncode,'stdout':r.stdout,'stderr':r.stderr});(out/'execution.json').write_text(json.dumps(logs,indent=2))
        code=r.returncode
        if code:break
    assert all(digest(ROOT/p)==h for p,h in protected.items());(out/'protected-pins.json').write_text(json.dumps({'unchanged':True,'files':protected},indent=2))
    after={str(s.relative_to(ROOT)).replace('\\','/'):digest(s) for s in pairs.values()};assert all(after[k]==v['source'] for k,v in pins.items());(out/'source-after-pins.json').write_text(json.dumps(after,indent=2))
    pngs={p.name:png(p) for p in out.glob('*.png')};(out/'png-evidence.json').write_text(json.dumps(pngs,indent=2))
    report=out/'results.json'
    if report.exists():
        d=json.loads(report.read_text());assert d['checkCount']==len(d['checks'])>0 and d['screenshotCount']==len(d['screenshots'])==len(pngs);print(json.dumps({k:v for k,v in d.items() if k not in ['checks','screenshots']},indent=2));return code or (0 if d['passed'] else 1)
    print('No GPU report: '+str(out/'unity.log'));return code or 2
if __name__=='__main__':sys.exit(main())
