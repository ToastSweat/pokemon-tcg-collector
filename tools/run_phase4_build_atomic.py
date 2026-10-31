"""Native Mono regression for the EXACT probe atomic publication method under Windows reader locks."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True);args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    out=ROOT/'.artifacts/phase4-player-tools'/args.label;out.mkdir(parents=True,exist_ok=False)
    tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
    probe=ROOT/'Assets/VintageCollector/Runtime/Phase4SmokeProbe.cs';template=ROOT/'TestsPhase4/PlayerAtomicTests.cs'
    text=probe.read_text();start=text.index('private static void AtomicText(');end=text.index('private void Finish(',start)
    source=out/'ExactProbeAtomicTests.cs';source.write_text(template.read_text().replace('/*ATOMIC_SOURCE*/',text[start:end]))
    pins={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [probe,template,Path(__file__)]}
    (out/'source-pins.json').write_text(json.dumps(pins,indent=2))
    commands=[[str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+','-out:'+str(out/'tests.exe'),str(source)],
              [str(MONO/'bin/mono.exe'),str(out/'tests.exe'),str(out/'fixtures')]]
    log=[];passed=False;checks=[];exit_code=-1
    for command in commands:
        result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=30);exit_code=result.returncode
        log.extend([json.dumps(command),result.stdout,result.stderr,'EXIT '+str(result.returncode)])
        checks=re.findall(r'^PASS (.+)$',result.stdout,re.M);count=re.search(r'^TEST_COUNT (\d+)$',result.stdout,re.M)
        passed=result.returncode==0 and count is not None and int(count[1])==len(checks)>0
        if result.returncode:break
    (out/'execution.log').write_text('\n'.join(log));report={'passed':passed,'checkCount':len(checks),'checks':checks,'exit':exit_code}
    (out/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps(report));return 0 if passed else 1
if __name__=='__main__':sys.exit(main())
