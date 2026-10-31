"""Pure read-only authoring index tests; never launches Unity, generates packs or opens saves."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess

ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')

def main():
    parser=argparse.ArgumentParser(__doc__)
    parser.add_argument('--label',required=True)
    parser.add_argument('--catalog-file',action='append',default=[])
    args=parser.parse_args()
    if not re.fullmatch('[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    from run_tilt_build import no_reparse
    catalogs=[]
    for name in args.catalog_file:
        path=Path(name).resolve()
        if not path.is_relative_to(ROOT.resolve()) or path.suffix!='.json':parser.error('Only project-local catalog JSON may be read')
        no_reparse(path)
        catalogs.append(path)
    out=ROOT/'.artifacts/phase5-catalog-index'/args.label
    no_reparse(out)
    out.mkdir(parents=True,exist_ok=False)
    sources=[ROOT/'Assets/VintageCollector/Domain'/name for name in ('Data.cs','Catalog.cs')]
    sources.append(ROOT/'TestsPhase5/CatalogIndexChecks.cs')
    module=ROOT/'Authoring/Phase5/Code/PrintingCatalogIndex.cs'
    if module.exists():sources.append(module)
    pinned=[*sources,Path(__file__).resolve(),ROOT/'tools/run_tilt_build.py',*catalogs]
    pins={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in pinned}
    commands=[
        [str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+','-r:System.Runtime.Serialization','-out:'+str(out/'CatalogIndexChecks.exe'),*[str(s) for s in sources]],
        [str(MONO/'bin/mono.exe'),str(out/'CatalogIndexChecks.exe'),*[str(p) for p in catalogs]]
    ]
    env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out),PYTHONDONTWRITEBYTECODE='1')
    runs=[];code=0
    for command in commands:
        result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=120)
        runs.append({'command':command,'exitCode':result.returncode,'stdout':result.stdout,'stderr':result.stderr})
        print(result.stdout,end='');print(result.stderr,end='')
        if result.returncode:
            code=result.returncode
            break
    after={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in pinned}
    counts=re.search(r'^CHECKS (\d+) FAILED (\d+)$',runs[-1]['stdout'],re.MULTILINE)
    if code==0 and (not counts or int(counts[1])<=0 or int(counts[2])!=0):code=2
    (out/'results.json').write_text(json.dumps({'passed':code==0 and pins==after,'exitCode':code,'testCount':int(counts[1]) if counts else None,'inputs':pins,'inputsUnchanged':pins==after,'runs':runs,'scope':'pure-authoring-tests-not-native-player'},indent=2)+'\n',encoding='utf-8')
    return code or (0 if pins==after else 3)

if __name__=='__main__':raise SystemExit(main())
