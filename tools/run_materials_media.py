"""Existing thirteen pure media regressions, with no accepted-build traversal."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
sys.dont_write_bytecode=True
from run_phase3_media import ROOT, MONO, fixtures
from run_materials_media_native import pins

def main():
    parser=argparse.ArgumentParser(__doc__)
    parser.add_argument('--label',required=True)
    args=parser.parse_args()
    if not args.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in args.label): parser.error('Unsafe label')
    out=ROOT/'.artifacts/materials-media'/args.label
    out.mkdir(parents=True,exist_ok=True)
    if (out/'execution.log').exists(): parser.error('Use fresh label')
    fixtures(out)
    env=dict(os.environ,TMP=str(out),TEMP=str(out),TMPDIR=str(out))
    (out/'junction-target').mkdir(exist_ok=True)
    (out/'junction-target/original.bin').write_bytes(b'original synthetic test bytes')
    if not (out/'reparse').exists():
        r=subprocess.run(['cmd.exe','/c','mklink','/J',str(out/'reparse'),str(out/'junction-target')],env=env,capture_output=True,text=True)
        if r.returncode: raise RuntimeError(r.stdout+r.stderr)
    sources=[ROOT/'TestsPhase3/MediaPureTests.cs']+[ROOT/('Assets/VintageCollector/Media/'+n+'.cs') for n in ['MediaSamples','MediaManifest','MediaPng','MediaFiles','MediaWave']]+sorted((ROOT/'Assets/VintageCollector/Domain').glob('*.cs'))
    protected=sorted((ROOT/'Assets/VintageCollector/Domain').glob('*.cs'))+[ROOT/'Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json',ROOT/'Assets/Supplied/AssetManifest.json']
    before=pins(protected)
    inputs=pins(sources+[Path(__file__)])
    (out/'compiled-inputs.json').write_text(json.dumps(inputs,indent=2),encoding='utf-8')
    commands=[[MONO/'bin/mono.exe',MONO/'lib/mono/4.5/mcs.exe','-warnaserror+','-out:'+str(out/'MediaPureTests.exe'),*sources],[MONO/'bin/mono.exe',out/'MediaPureTests.exe',ROOT,out]]
    logs=[]
    try:
        for i,command in enumerate(commands):
            r=subprocess.run([str(c) for c in command],cwd=ROOT,env=env,capture_output=True,text=True,timeout=180)
            logs.extend([json.dumps([str(c) for c in command]),r.stdout,r.stderr,'EXIT '+str(r.returncode)])
            print(r.stdout,end=''); print(r.stderr,end='',file=sys.stderr)
            if r.returncode: return r.returncode
            if i==1:
                data=json.loads(r.stdout)
                (out/'results.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
                assert data['testCount']==len(data['checks']) and data['testCount']>0
                if not data['passed'] or not all(c['passed'] for c in data['checks']): return 1
        return 0
    finally:
        (out/'execution.log').write_text('\n'.join(logs),encoding='utf-8')
        (out/'protected-inputs.json').write_text(json.dumps({'before':before,'after':pins(protected),'unchanged':before==pins(protected)},indent=2),encoding='utf-8')
        if before!=pins(protected) or inputs!=pins(sources+[Path(__file__)]): raise RuntimeError('Input changed')

if __name__=='__main__': sys.exit(main())
