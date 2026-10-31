"""Explicit main-Editor host integration checks; controlled outputs only in .artifacts."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
ROOT = Path(__file__).resolve().parents[1]
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
def pins():
    files=[]
    for part in ['Assets/VintageCollector/Domain','Assets/VintageCollector/Data','Assets/VintageCollector/Persistence','Assets/Supplied']:
        files += [p for p in (ROOT/part).rglob('*') if p.is_file()]
    files += [ROOT/'Assets/VintageCollector/Runtime'/p for p in ['GamePaths.cs','OpeningGame.cs','UnityStateSerializer.cs']]
    package_settings=ROOT/'ProjectSettings/PackageManagerSettings.asset'
    if package_settings.exists(): files.append(package_settings)
    # Never read live saves, including the accepted build's Saves subtree.
    for directory, dirs, names in os.walk(ROOT/'Builds/Phase2Windows'):
        dirs[:] = [d for d in dirs if d.lower() != 'saves']
        files += [Path(directory)/n for n in names]
    return {p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(files)}
def main():
    ap=argparse.ArgumentParser(__doc__)
    ap.add_argument('--label', required=True)
    ap.add_argument('--slice', default='binding')
    a=ap.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): ap.error('unsafe label')
    out=ROOT/'.artifacts/phase3-host'/a.label
    out.mkdir(parents=True,exist_ok=False)
    before=pins()
    env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out),VCC_HOST_REPORT=str(out/'results.json'),VCC_HOST_SLICE=a.slice)
    # The user-owned interactive Editor may hold the main project. Never close it.
    # Stage exact real runtime sources + data (not mock/controller substitutes).
    import shutil
    project=ROOT/'.artifacts/phase3-host/native-project'
    project.mkdir(parents=True,exist_ok=True)
    for part in ['Domain','Data','Persistence','Runtime','Media','Presentation','Resources']:
        shutil.copytree(ROOT/'Assets/VintageCollector'/part,project/'Assets/VintageCollector'/part,dirs_exist_ok=True)
    (project/'Assets/VintageCollector/Editor').mkdir(parents=True,exist_ok=True)
    shutil.copy2(ROOT/'Assets/VintageCollector/Editor/Phase3MediaHostChecks.cs',project/'Assets/VintageCollector/Editor/Phase3MediaHostChecks.cs')
    shutil.copytree(ROOT/'Assets/Supplied',project/'Assets/Supplied',dirs_exist_ok=True)
    if not (project/'ProjectSettings/ProjectVersion.txt').exists():
        shutil.copytree(ROOT/'ProjectSettings',project/'ProjectSettings',dirs_exist_ok=True)
        (project/'Packages').mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/'Packages/manifest.json',project/'Packages/manifest.json')
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(project),'-logFile',str(out/'unity.log'),'-executeMethod','VintageCollector.EditorChecks.Phase3MediaHostChecks.Run']
    (out/'command.json').write_text(json.dumps(cmd,indent=2),encoding='utf-8')
    sources=[p for p in (ROOT/'Assets/VintageCollector').rglob('*') if p.is_file() and p.suffix in ('.cs','.shader')]
    (out/'source-pins.json').write_text(json.dumps({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sources},indent=2),encoding='utf-8')
    copied=[p for p in (project/'Assets').rglob('*') if p.is_file() and p.suffix in ('.cs','.shader','.json')]
    staged={p.relative_to(project).as_posix():{'source':hashlib.sha256((ROOT/p.relative_to(project)).read_bytes()).hexdigest(),'staged':hashlib.sha256(p.read_bytes()).hexdigest()} for p in copied}
    stage_exact=bool(staged) and all(v['source']==v['staged'] for v in staged.values())
    (out/'staged-inputs.json').write_text(json.dumps({'exact':stage_exact,'files':staged},indent=2),encoding='utf-8')
    if not stage_exact: raise RuntimeError('Staged native inputs differ from real source')
    native_report=project/'.artifacts/phase3-host'/a.label/'results.json'
    native_report.parent.mkdir(parents=True,exist_ok=True)
    env['VCC_HOST_REPORT']=str(native_report)
    r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=360)
    if native_report.exists(): shutil.copy2(native_report,out/'results.json')
    (out/'execution.log').write_text(r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    after=pins()
    (out/'protected-inputs.json').write_text(json.dumps({'unchanged':before==after,'before':before,'after':after,'liveSaves':'not read or written'},indent=2),encoding='utf-8')
    report=out/'results.json'
    if not report.exists(): print('Missing report; inspect '+str(out/'unity.log')); return r.returncode or 2
    d=json.loads(report.read_text(encoding='utf-8'))
    valid=d['checkCount']==len(d['checks']) and d['checkCount']>0
    print(json.dumps({'exit':r.returncode,'countMatched':valid,'protectedUnchanged':before==after,'passed':d['passed'],'checkCount':d['checkCount'],'failure':d.get('failure'),'report':str(report)},indent=2))
    return r.returncode or (0 if valid and d['passed'] and before==after else 1)
if __name__=='__main__': sys.exit(main())
