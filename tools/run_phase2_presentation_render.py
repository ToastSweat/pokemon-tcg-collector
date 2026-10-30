"""Build/run graphical synthetic presentation fixture, never the parent player."""
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
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): p.error('Unsafe label')
    project=ROOT/'.artifacts/phase2-presentation-project'
    out=ROOT/'.artifacts/presentation'
    if not (project/'ProjectSettings/ProjectVersion.txt').exists(): p.error('Run isolated editor assertions first')
    for f in (ROOT/'Assets/VintageCollector/Presentation').glob('*.cs'): shutil.copy2(f,project/'Assets/VintageCollector/Presentation'/f.name)
    shutil.copy2(ROOT/'TestsPhase2/PresentationRenderFixture.cs',project/'Assets/PresentationRenderFixture.cs')
    shutil.copy2(ROOT/'TestsPhase2/PresentationFixtureBuild.cs',project/'Assets/VintageCollector/Editor/PresentationFixtureBuild.cs')
    env=dict(os.environ,TEMP=str(project/'Temp'),TMP=str(project/'Temp'),TMPDIR=str(project/'Temp'))
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(project),'-logFile',str(out/(a.label+'-build-unity.log')),'-executeMethod','PresentationFixtureBuild.Run']
    r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300)
    (out/(a.label+'-build-process.log')).write_text(json.dumps(cmd)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    print('Isolated build exit '+str(r.returncode))
    if r.returncode: return r.returncode
    player=ROOT/'.artifacts/presentation-fixture-player/PresentationFixture.exe'
    report=out/'render/render-report.json'
    if report.exists(): report.unlink()
    r=subprocess.run([str(player),'-screen-fullscreen','0','-logFile',str(out/(a.label+'-player.log'))],cwd=player.parent,env=env,capture_output=True,text=True,timeout=180)
    (out/(a.label+'-player-process.log')).write_text(r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    print('Graphical isolated player exit '+str(r.returncode))
    if not report.exists(): print('No render report'); return 2
    data=json.loads(report.read_text(encoding='utf-8'))
    shutil.copy2(report,out/(a.label+'-render.json'))
    print(json.dumps({k:v for k,v in data.items() if k!='checks'}))
    if data['checkCount']!=len(data['checks']) or not data['passed'] or data['checkCount']<1: return 1
    return r.returncode

if __name__=='__main__': sys.exit(main())
