"""Run unchanged earlier-phase native checks on current frozen source, in a fresh owned project."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')

def main():
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('--label', required=True)
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+', args.label):
        parser.error('Unsafe label')
    output = ROOT/'.artifacts/phase4-legacy-native'/args.label
    output.mkdir(parents=True, exist_ok=False)
    project = output/'project'
    sources = []
    base = ROOT/'Assets/VintageCollector'
    for folder in ('Domain','Persistence','Collections','Media','Presentation'):
        sources.extend(sorted((base/folder).glob('*.cs')))
    sources.extend(base/'Runtime'/(name+'.cs') for name in ('GamePaths','OpeningGame','Phase3MediaHost','Phase4CollectionHost','UnityPhysicalSerializer','UnityStateSerializer'))
    sources.extend(base/'Editor'/(name+'.cs') for name in ('Phase1Diagnostics','Phase1EditorVerification','Phase2RuntimeChecks'))
    sources.extend(sorted((base/'Data').glob('*.json')))
    sources.extend([ROOT/'ProjectSettings/ProjectVersion.txt'])
    pins = {}
    for source in sources:
        relative = source.relative_to(ROOT)
        destination = project/relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
        meta = Path(str(source)+'.meta')
        if meta.is_file():
            shutil.copy2(meta, str(destination)+'.meta')
        pins[relative.as_posix()] = hashlib.sha256(destination.read_bytes()).hexdigest()
    manifest = project/'Packages/manifest.json'
    manifest.parent.mkdir(parents=True, exist_ok=True)
    manifest.write_text(json.dumps({'dependencies':{name:'1.0.0' for name in ('com.unity.modules.imgui','com.unity.modules.audio','com.unity.modules.imageconversion','com.unity.modules.jsonserialize','com.unity.modules.screencapture')}}), encoding='utf-8')
    pins['tools/run_phase4_legacy_native.py'] = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    (output/'source-pins.json').write_text(json.dumps(pins, indent=2), encoding='utf-8')
    temporary = output/'tmp'
    temporary.mkdir()
    env = dict(os.environ, TMP=str(temporary), TEMP=str(temporary), TMPDIR=str(temporary), VCC_RUNTIME_LABEL='legacy-regression')
    # The historical export check removes prior files before calling the menu;
    # establish its owned output-directory fixture on a genuinely fresh project.
    (project/'.artifacts/editor').mkdir(parents=True, exist_ok=True)
    results = []
    for name, method, report in (
        ('phase1','VintageCollector.EditorTools.Phase1EditorVerification.Run','.artifacts/editor/phase1-editor-checks.json'),
        ('phase2','Phase2RuntimeChecks.Run','.artifacts/phase2-runtime/legacy-regression.json')):
        command = [str(EDITOR),'-batchmode','-nographics','-projectPath',str(project),'-executeMethod',method,'-logFile',str(output/(name+'.log'))]
        run = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=600)
        (output/(name+'-execution.json')).write_text(json.dumps({'command':command,'exit':run.returncode,'stdout':run.stdout,'stderr':run.stderr}, indent=2), encoding='utf-8')
        path = project/report
        if not path.is_file():
            raise RuntimeError('No actual native '+name+' report; inspect retained log')
        data = json.loads(path.read_text(encoding='utf-8'))
        assert data['checkCount'] == len(data['checks']) and data['checkCount'] > 0
        assert data['passed'] == all(check['passed'] for check in data['checks'])
        if 'assertionCount' in data:
            assert data['assertionCount'] == len(data['assertions'])
        result = {'suite':name,'exit':run.returncode,'passed':data['passed'],'groups':data['checkCount'],'assertions':data.get('assertionCount'),'failures':[c for c in data['checks'] if not c['passed']]}
        results.append(result)
        shutil.copy2(path, output/(name+'-results.json'))
    current = {relative:hashlib.sha256((ROOT/relative).read_bytes()).hexdigest() for relative in pins}
    report = {'passed':current==pins and all(r['passed'] and r['exit']==0 for r in results),'currentSourceMatches':current==pins,'suites':results}
    (output/'results.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report))
    return 0 if report['passed'] else 1

if __name__ == '__main__':
    sys.exit(main())
