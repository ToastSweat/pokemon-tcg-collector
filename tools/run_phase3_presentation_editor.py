"""Package-free native checks in an exclusively staged Phase 3 presentation project."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
PROJECT = ROOT / '.artifacts/phase3-presentation-project'
OUT = ROOT / '.artifacts/phase3-presentation'

def stage():
    OUT.mkdir(parents=True, exist_ok=True)
    for name in ['Assets/VintageCollector/Presentation', 'Assets/VintageCollector/Media', 'Assets/VintageCollector/Editor', 'ProjectSettings', 'Packages', '.artifacts', 'Temp']:
        (PROJECT/name).mkdir(parents=True, exist_ok=True)
    if not (PROJECT/'ProjectSettings/ProjectVersion.txt').exists():
        for f in (ROOT/'ProjectSettings').glob('*'):
            if f.is_file(): shutil.copy2(f, PROJECT/'ProjectSettings'/f.name)
        shutil.copy2(ROOT/'Packages/manifest.json', PROJECT/'Packages/manifest.json')
    for f in (ROOT/'Assets/VintageCollector/Presentation').glob('*.cs'):
        shutil.copy2(f, PROJECT/'Assets/VintageCollector/Presentation'/f.name)
    shutil.copy2(ROOT/'Assets/VintageCollector/Media/PresentationMediaContract.cs', PROJECT/'Assets/VintageCollector/Media/PresentationMediaContract.cs')
    shutil.copy2(ROOT/'Assets/VintageCollector/Editor/Phase3PresentationChecks.cs', PROJECT/'Assets/VintageCollector/Editor/Phase3PresentationChecks.cs')
    shutil.copy2(ROOT/'Assets/VintageCollector/Editor/Phase2PresentationChecks.cs', PROJECT/'Assets/VintageCollector/Editor/Phase2PresentationChecks.cs')
    resources=ROOT/'Assets/VintageCollector/Resources/Presentation'
    if resources.exists(): shutil.copytree(resources, PROJECT/'Assets/VintageCollector/Resources/Presentation', dirs_exist_ok=True)
    return dict(os.environ, TEMP=str(PROJECT/'Temp'), TMP=str(PROJECT/'Temp'), TMPDIR=str(PROJECT/'Temp'))

def main():
    p=argparse.ArgumentParser(__doc__)
    p.add_argument('--label', required=True)
    p.add_argument('--legacy', action='store_true', help='Run untouched Phase 2 native checks with current renderer')
    a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): p.error('Unsafe label')
    env=stage()
    entry='VintageCollector.EditorChecks.'+('Phase2PresentationChecks' if a.legacy else 'Phase3PresentationChecks')+'.Run'
    cmd=[str(EDITOR), '-batchmode', '-nographics', '-projectPath', str(PROJECT), '-logFile', str(OUT/(a.label+'-unity.log')), '-executeMethod', entry]
    report=PROJECT/'.artifacts/presentation-engine-report.json'
    if report.exists(): report.unlink()
    r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300)
    (OUT/(a.label+'-process.log')).write_text(json.dumps(cmd)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    if not report.exists():
        print('No assertion report; inspect '+str(OUT/(a.label+'-unity.log')))
        return r.returncode or 2
    data=json.loads(report.read_text(encoding='utf-8'))
    shutil.copy2(report,OUT/(a.label+'-engine.json'))
    print(json.dumps(data))
    if data['checkCount']!=len(data['checks']) or not data['passed'] or data['checkCount']<1: return r.returncode or 1
    return r.returncode

if __name__=='__main__': sys.exit(main())
