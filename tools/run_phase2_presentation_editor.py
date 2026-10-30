"""Run real engine assertions only in an isolated project under .artifacts."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[1]
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')

def main():
    p=argparse.ArgumentParser(__doc__)
    p.add_argument('--label', required=True)
    a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):
        p.error('Unsafe label')
    project=ROOT/'.artifacts/phase2-presentation-project'
    out=ROOT/'.artifacts/presentation'
    out.mkdir(parents=True,exist_ok=True)
    for directory in ['Assets/VintageCollector/Presentation','Assets/VintageCollector/Editor','ProjectSettings','Packages','.artifacts','Temp']:
        (project/directory).mkdir(parents=True,exist_ok=True)
    # Copy baseline settings only on creation; never open or alter main project.
    if not (project/'ProjectSettings/ProjectVersion.txt').exists():
        for f in (ROOT/'ProjectSettings').glob('*'):
            if f.is_file(): shutil.copy2(f,project/'ProjectSettings'/f.name)
        shutil.copy2(ROOT/'Packages/manifest.json',project/'Packages/manifest.json')
    for f in (ROOT/'Assets/VintageCollector/Presentation').glob('*.cs'):
        shutil.copy2(f,project/'Assets/VintageCollector/Presentation'/f.name)
    shutil.copy2(ROOT/'Assets/VintageCollector/Editor/Phase2PresentationChecks.cs',project/'Assets/VintageCollector/Editor/Phase2PresentationChecks.cs')
    env=dict(os.environ,TEMP=str(project/'Temp'),TMP=str(project/'Temp'),TMPDIR=str(project/'Temp'))
    log=out/(a.label+'-unity.log')
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(project),'-logFile',str(log),'-executeMethod','VintageCollector.EditorChecks.Phase2PresentationChecks.Run']
    report=project/'.artifacts/presentation-engine-report.json'
    if report.exists(): report.unlink()
    r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300)
    (out/(a.label+'-process.log')).write_text(json.dumps(cmd)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode)+'\n',encoding='utf-8')
    if report.exists():
        shutil.copy2(report,out/(a.label+'-engine.json'))
        data=json.loads(report.read_text(encoding='utf-8'))
        print(json.dumps(data))
        if r.returncode==0 and (not data['passed'] or data['checkCount']<1 or data['checkCount']!=len(data['checks'])):
            return 2
    else:
        print('No assertion report; inspect '+str(log))
        return r.returncode or 2
    print('Unity exit '+str(r.returncode))
    return r.returncode

if __name__=='__main__': sys.exit(main())
