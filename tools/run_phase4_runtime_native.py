"""Exact-source isolated native Unity runtime checks; never opens the original project."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / '.artifacts/phase4-runtime'
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')

def source_key(path):
    return path.relative_to(ROOT).as_posix()

def provenance_pins():
    return {relative:hashlib.sha256((ROOT/relative).read_bytes()).hexdigest()
            for relative in ('tools/run_phase4_runtime_native.py','ProjectSettings/ProjectVersion.txt')}

def require_exact_editor_scripts(directory, expected):
    unexpected = set(directory.glob('*.cs')) - set(expected)
    if unexpected:
        raise RuntimeError('Unexpected staged Editor scripts; retained without executing: '+', '.join(sorted(p.name for p in unexpected)))

def main():
    p = argparse.ArgumentParser(__doc__)
    p.add_argument('--label', required=True)
    a = p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):
        p.error('Unsafe label')
    out = BASE / a.label
    out.mkdir(parents=True, exist_ok=False)
    project = BASE / 'project'
    for folder in ['Assets/Scripts', 'Assets/Editor', 'Assets/Data', 'Packages', 'ProjectSettings', 'Temp']:
        (project/folder).mkdir(parents=True, exist_ok=True)
    modules = {
        'Domain': ['Catalog', 'Data', 'PackGenerator', 'Random'],
        'Persistence': ['SaveProtocol', 'SessionArchiveValidation', 'SessionData', 'SessionStore', 'StateValidation'],
        'Collections': ['BinderProjection', 'CollectionArchive', 'CollectionContract', 'CollectionEnvelope', 'CollectionFiles', 'CollectionJsonShape', 'CollectionProfiles', 'CollectionZipStructure', 'PhysicalProtocol', 'PhysicalStore', 'PhysicalValidation'],
        'Media': ['MediaFiles', 'MediaManifest', 'MediaPng', 'MediaSamples', 'MediaWave', 'PresentationMediaContract', 'PresentationMediaLibrary'],
        'Presentation': ['BinderLayoutRules', 'BinderPresentation', 'BinderView', 'OpeningView', 'Phase3PresentationMotion', 'PresentationRules', 'TabletopPresentation'],
        'Runtime': ['GamePaths', 'OpeningGame', 'Phase3MediaHost', 'Phase4CollectionHost', 'UnityPhysicalSerializer', 'UnityStateSerializer'],
    }
    sources = [ROOT/'Assets/VintageCollector'/folder/(name+'.cs') for folder, names in modules.items() for name in names]
    sources.append(ROOT/'Assets/VintageCollector/Editor/Phase4RuntimeChecks.cs')
    sources.append(ROOT/'TestsPhase4/RuntimeScenarios.cs')
    sources.append(ROOT/'TestsPhase4/RuntimeNativeNumbers.cs')
    expected = set()
    pins = {}
    for f in sources:
        target = project/('Assets/Editor' if f.name in ['Phase4RuntimeChecks.cs', 'RuntimeScenarios.cs', 'RuntimeNativeNumbers.cs'] else 'Assets/Scripts')/f.name
        expected.add(target)
        shutil.copy2(f, target)
        pin = hashlib.sha256(target.read_bytes()).hexdigest()
        assert hashlib.sha256(f.read_bytes()).hexdigest() == pin, 'Source changed while staging: '+str(f)
        pins[source_key(f)] = pin
    for f in (project/'Assets/Scripts').glob('*.cs'):
        if f not in expected:
            f.unlink()
    require_exact_editor_scripts(project/'Assets/Editor',expected)
    pins.update(provenance_pins())
    sources.extend([ROOT/'tools/run_phase4_runtime_native.py',ROOT/'ProjectSettings/ProjectVersion.txt'])
    for f in (ROOT/'Assets/VintageCollector/Data').glob('*.json'):
        target = project/'Assets/Data'/f.name
        shutil.copy2(f, target)
        pins[source_key(f)] = hashlib.sha256(target.read_bytes()).hexdigest()
        sources.append(f)
    shutil.copy2(ROOT/'ProjectSettings/ProjectVersion.txt', project/'ProjectSettings/ProjectVersion.txt')
    (project/'Packages/manifest.json').write_text(json.dumps({'dependencies':{name:'1.0.0' for name in ['com.unity.modules.imgui', 'com.unity.modules.audio', 'com.unity.modules.imageconversion', 'com.unity.modules.jsonserialize', 'com.unity.modules.screencapture']}}), encoding='utf-8')
    (out/'source-pins.json').write_text(json.dumps(pins, indent=2), encoding='utf-8')
    env = dict(os.environ, TMP=str(project/'Temp'), TEMP=str(project/'Temp'), TMPDIR=str(project/'Temp'), VCC_RUNTIME_OUTPUT=str(out))
    cmd = [str(EDITOR), '-batchmode', '-nographics', '-projectPath', str(project), '-logFile', str(out/'unity.log'), '-executeMethod', 'VintageCollector.EditorChecks.Phase4RuntimeChecks.Run']
    r = subprocess.run(cmd, cwd=ROOT, env=env, capture_output=True, text=True, timeout=600)
    (out/'execution.log').write_text(json.dumps(cmd)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode), encoding='utf-8')
    current = {source_key(f):hashlib.sha256(f.read_bytes()).hexdigest() for f in sources}
    (out/'source-verification.json').write_text(json.dumps({'currentMatchesStaged':current == pins, 'current':current}, indent=2), encoding='utf-8')
    report = out/'results.json'
    if not report.exists():
        print('No report; inspect '+str(out/'unity.log'))
        return r.returncode or 2
    data = json.loads(report.read_text(encoding='utf-8'))
    assert data['checkCount'] == len(data['checks']) and data['checkCount'] > 0
    assert data['passed'] == all(c['passed'] for c in data['checks'])
    print(json.dumps({'passed':data['passed'], 'checkCount':data['checkCount'], 'failures':[c for c in data['checks'] if not c['passed']], 'report':str(report)}))
    return r.returncode or (3 if current != pins else 0 if data['passed'] else 1)
if __name__ == '__main__':
    sys.exit(main())
