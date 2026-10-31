"""Compile only presentation appearance selection, without Domain/RNG/store dependencies."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess
from run_tilt_build import no_reparse
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')

def main():
    parser=argparse.ArgumentParser(__doc__);parser.add_argument('--label',required=True);args=parser.parse_args()
    if not re.fullmatch('[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    out=ROOT/'.artifacts/phase5-appearance'/args.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False)
    sources=[ROOT/'TestsPhase5/AppearanceChecks.cs']
    selector=ROOT/'Authoring/Phase5/Code/WrapperVariant.cs'
    if selector.exists():sources.append(selector)
    inputs=[*sources,Path(__file__).resolve(),ROOT/'tools/run_tilt_build.py']
    pins={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in inputs}
    env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out),PYTHONDONTWRITEBYTECODE='1')
    commands=[
        [str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+','-out:'+str(out/'AppearanceChecks.exe'),*[str(p) for p in sources]],
        [str(MONO/'bin/mono.exe'),str(out/'AppearanceChecks.exe')]
    ]
    runs=[];code=0
    for command in commands:
        result=subprocess.run(command,cwd=ROOT,env=env,text=True,capture_output=True,timeout=120)
        runs.append({'command':command,'exitCode':result.returncode,'stdout':result.stdout,'stderr':result.stderr})
        print(result.stdout,end='');print(result.stderr,end='')
        if result.returncode:code=result.returncode;break
    counts=re.search(r'^CHECKS (\d+) FAILED (\d+)$',runs[-1]['stdout'],re.MULTILINE)
    if code==0 and (not counts or int(counts[1])<=0 or int(counts[2])!=0):code=2
    unchanged=all(hashlib.sha256(p.read_bytes()).hexdigest()==pins[p.relative_to(ROOT).as_posix()] for p in inputs)
    (out/'results.json').write_text(json.dumps({'passed':code==0 and unchanged,'exitCode':code,'checkCount':int(counts[1]) if counts else None,'inputs':pins,'inputsUnchanged':unchanged,'compiledDomainOrRngOrStore':False,'nativeUnityPlayer':False,'runs':runs},indent=2)+'\n',encoding='utf-8')
    return code or (0 if unchanged else 3)
if __name__=='__main__':raise SystemExit(main())
