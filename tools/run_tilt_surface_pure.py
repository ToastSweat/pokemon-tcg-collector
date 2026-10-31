"""Pure frozen-API tests. Explicit sources, bundled compiler, warnings as errors."""
import argparse, hashlib, json, os, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
def main():
    p=argparse.ArgumentParser(__doc__);p.add_argument('--label',required=True);p.add_argument('--slice',default='all');a=p.parse_args()
    assert a.label and all(c.isalnum() or c in '-_' for c in a.label)
    out=ROOT/'.artifacts/tilt-surface-project/evidence'/a.label;out.mkdir(parents=True,exist_ok=False)
    sources=[ROOT/'Assets/VintageCollector/Presentation/HoverTiltMotion.cs',ROOT/'TestsTilt/SurfaceMotionTests.cs']
    (out/'source-pins.json').write_text(json.dumps({s.relative_to(ROOT).as_posix():hashlib.sha256(s.read_bytes()).hexdigest() for s in sources},indent=2))
    env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out));exe=out/'SurfaceMotionTests.exe';logs=[]
    for cmd in [[MONO/'bin/mono.exe',MONO/'lib/mono/4.5/mcs.exe','-warnaserror+','-out:'+str(exe),*sources],[MONO/'bin/mono.exe',exe,a.slice]]:
        r=subprocess.run(list(map(str,cmd)),cwd=ROOT,env=env,capture_output=True,text=True,timeout=120);logs.append({'command':list(map(str,cmd)),'exit':r.returncode,'stdout':r.stdout,'stderr':r.stderr});(out/'execution.json').write_text(json.dumps(logs,indent=2));print(r.stdout+r.stderr)
        if r.returncode:return r.returncode
    data=json.loads(r.stdout);assert data['checkCount']==len(data['checks'])>0 and data['passed'];(out/'results.json').write_text(json.dumps(data,indent=2));return 0
if __name__=='__main__':sys.exit(main())
