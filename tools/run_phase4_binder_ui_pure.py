"""Explicit passive binder tests. Synthetic DTOs only; all evidence stays inside this project."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
MONO = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
OUT = ROOT / '.artifacts/phase4-ui'

def main():
    p=argparse.ArgumentParser(__doc__); p.add_argument('--label',required=True); a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): p.error('Unsafe label')
    out=OUT/a.label; out.mkdir(parents=True,exist_ok=False)
    env=dict(os.environ,TMP=str(out),TEMP=str(out),TMPDIR=str(out))
    sources=[ROOT/'TestsPhase4/UiPureTests.cs',ROOT/'Assets/VintageCollector/Presentation/BinderView.cs',ROOT/'Assets/VintageCollector/Presentation/BinderLayoutRules.cs']
    (out/'sources.json').write_text(json.dumps({str(f.relative_to(ROOT)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sources},indent=2),encoding='utf-8')
    log=[]
    for cmd in [[MONO/'bin/mono.exe',MONO/'lib/mono/4.5/mcs.exe','-warnaserror+','-out:'+str(out/'UiPureTests.exe'),*sources],[MONO/'bin/mono.exe',out/'UiPureTests.exe']]:
        r=subprocess.run([str(x) for x in cmd],cwd=ROOT,env=env,capture_output=True,text=True,timeout=180)
        log.extend([json.dumps([str(x) for x in cmd]),r.stdout,r.stderr,'EXIT '+str(r.returncode)])
        (out/'execution.log').write_text('\n'.join(log),encoding='utf-8')
        if cmd[1]==out/'UiPureTests.exe':
            report=json.loads(r.stdout); (out/'results.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
            assert report['testCount']>0 and report['testCount']==len(report['checks'])
            print(json.dumps({'label':a.label,'passed':report['passed'],'testCount':report['testCount'],'failures':[c['name'] for c in report['checks'] if not c['passed']]}))
        if r.returncode: print(r.stderr); return r.returncode
    return 0

if __name__=='__main__': sys.exit(main())
