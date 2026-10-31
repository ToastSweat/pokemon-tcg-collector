"""Explicit source-only pure presentation regression; outputs exclusively in owned artifacts."""
import argparse,hashlib,json,os,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
def main():
    p=argparse.ArgumentParser(__doc__);p.add_argument('--label',required=True);a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):p.error('unsafe label')
    out=ROOT/'.artifacts/materials-render-project/.artifacts/pure'/a.label;out.mkdir(parents=True,exist_ok=False)
    env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out))
    sources=[ROOT/'Assets/VintageCollector/Presentation'/n for n in ['OpeningView.cs','PresentationRules.cs','Phase3PresentationMotion.cs']]+[ROOT/'TestsPhase3/PresentationMotionTests.cs']
    (out/'source-pins.json').write_text(json.dumps({s.relative_to(ROOT).as_posix():hashlib.sha256(s.read_bytes()).hexdigest() for s in sources},indent=2),encoding='utf-8')
    exe=out/'PresentationMotionTests.exe';log=[]
    for cmd in [[MONO/'bin/mono.exe',MONO/'lib/mono/4.5/mcs.exe','-warnaserror+','-out:'+str(exe),*sources],[MONO/'bin/mono.exe',exe]]:
        r=subprocess.run([str(x) for x in cmd],cwd=ROOT,env=env,capture_output=True,text=True,timeout=120)
        log.append(json.dumps([str(x) for x in cmd])+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode))
        (out/'execution.log').write_text('\n'.join(log),encoding='utf-8');print(r.stdout+r.stderr)
        if r.returncode:return r.returncode
    d=json.loads(r.stdout);assert d['passed'] and d['checkCount']>0 and d['checkCount']==len(d['checks'])
    (out/'results.json').write_text(json.dumps(d,indent=2),encoding='utf-8');return 0
if __name__=='__main__':sys.exit(main())
