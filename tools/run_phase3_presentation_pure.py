"""Compile explicit real pure presentation sources, no Unity doubles or packages."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')

def main():
    p=argparse.ArgumentParser(__doc__); p.add_argument('--label',required=True); a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): p.error('Unsafe label')
    out=ROOT/'.artifacts/phase3-presentation'; out.mkdir(parents=True,exist_ok=True)
    env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out))
    sources=[ROOT/'Assets/VintageCollector/Presentation'/n for n in ['OpeningView.cs','PresentationRules.cs']]
    sources+=list((ROOT/'Assets/VintageCollector/Presentation').glob('Phase3*Motion.cs'))
    sources+=[ROOT/'TestsPhase3/PresentationMotionTests.cs']
    exe=out/'PresentationMotionTests.exe'; log=[]
    result=1
    try:
        for cmd in [[MONO/'bin/mono.exe',MONO/'lib/mono/4.5/mcs.exe','-warnaserror+','-out:'+str(exe),*sources],[MONO/'bin/mono.exe',exe]]:
            r=subprocess.run([str(x) for x in cmd],cwd=ROOT,env=env,capture_output=True,text=True,timeout=120)
            log.append(json.dumps([str(x) for x in cmd])+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode))
            print(r.stdout+r.stderr); result=r.returncode
            if cmd[-1]==exe:
                data=json.loads(r.stdout)
                (out/(a.label+'.json')).write_text(json.dumps(data,indent=2),encoding='utf-8')
                if data['checkCount']!=len(data['checks']) or not data['passed'] or data['checkCount']<1: return r.returncode or 1
            if r.returncode: return r.returncode
        return result
    finally: (out/(a.label+'.log')).write_text('\n'.join(log),encoding='utf-8')
if __name__=='__main__': sys.exit(main())
