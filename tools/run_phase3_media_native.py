"""Real Unity media checks in the isolated .artifacts/phase3-media-project only (not player/core-connected)."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
sys.dont_write_bytecode = True
from run_phase3_media import ROOT, fixtures, protected
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')

def main():
    p=argparse.ArgumentParser(__doc__)
    p.add_argument('--label', required=True)
    a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):
        p.error('Unsafe label')
    project=ROOT/'.artifacts/phase3-media-project'
    out=ROOT/'.artifacts/phase3-media'/a.label
    out.mkdir(parents=True,exist_ok=True)
    for directory in ['Assets/Media','Assets/Domain','Assets/Editor','ProjectSettings','Packages','Temp']:
        (project/directory).mkdir(parents=True,exist_ok=True)
    before=protected()
    fixtures(out)
    (out/'junction-target').mkdir(exist_ok=True)
    shutil.copy2(out/'original-synthetic-card.png',out/'junction-target/original-synthetic-card.png')
    if not (out/'reparse').exists():
        junction=subprocess.run(['cmd.exe','/c','mklink','/J',str(out/'reparse'),str(out/'junction-target')],capture_output=True,text=True)
        (out/'junction-fixture.log').write_text(junction.stdout+'\n'+junction.stderr+'\nEXIT '+str(junction.returncode),encoding='utf-8')
        if junction.returncode: raise RuntimeError('Cannot create isolated native junction fixture')
    (project/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.2.14f1\n',encoding='utf-8')
    (project/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.audio':'1.0.0','com.unity.modules.imageconversion':'1.0.0','com.unity.modules.jsonserialize':'1.0.0'}}),encoding='utf-8')
    sources=list((ROOT/'Assets/VintageCollector/Media').glob('*.cs'))+list((ROOT/'Assets/VintageCollector/Domain').glob('*.cs'))+[ROOT/'TestsPhase3/MediaNativeChecks.cs']
    for source in sources:
        target='Editor' if source.name=='MediaNativeChecks.cs' else ('Domain' if source.parent.name=='Domain' else 'Media')
        shutil.copy2(source,project/'Assets'/target/source.name)
    (out/'compiled-inputs.json').write_text(json.dumps({str(f.relative_to(ROOT)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sources},indent=2),encoding='utf-8')
    env=dict(os.environ,TEMP=str(project/'Temp'),TMP=str(project/'Temp'),TMPDIR=str(project/'Temp'),PHASE3_MEDIA_ROOT=str(ROOT),PHASE3_MEDIA_OUTPUT=str(out))
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(project),'-logFile',str(out/'unity.log'),'-executeMethod','MediaNativeChecks.Run']
    report=out/'native-results.json'
    if report.exists(): report.unlink()
    try:
        r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=360)
        (out/'execution.log').write_text(json.dumps(cmd)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
        if not report.exists():
            print('No native report. Inspect '+str(out/'unity.log')); return r.returncode or 2
        data=json.loads(report.read_text(encoding='utf-8'))
        print(json.dumps(data))
        if data['testCount']<1 or data['testCount']!=len(data['checks']):
            raise RuntimeError('Invalid/zero test count')
        return r.returncode or (0 if data['passed'] and all(c['passed'] for c in data['checks']) else 1)
    finally:
        after=protected()
        (out/'protected-inputs.json').write_text(json.dumps({'before':before,'after':after,'unchanged':before==after},indent=2),encoding='utf-8')
        if before!=after: raise RuntimeError('Protected source/data/save/baseline changed')

if __name__=='__main__': sys.exit(main())
