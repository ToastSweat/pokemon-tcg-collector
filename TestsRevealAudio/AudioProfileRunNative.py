"""Isolated native profile import/authoring; never launch the main project Editor."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.artifacts/reveal-audio-profile'
PROJECT = OUT / 'native-project'
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--label', required=True)
    parser.add_argument('--author', action='store_true')
    parser.add_argument('--adopt', action='store_true')
    args = parser.parse_args()
    if not args.label.replace('-', '').replace('_', '').isalnum():
        parser.error('simple label required')
    out = OUT / args.label
    out.mkdir(parents=True, exist_ok=False)
    sources = []
    for part in ('Presentation', 'Media', 'Domain'):
        sources += sorted((ROOT / 'Assets/VintageCollector' / part).glob('*.cs'))
    sources += sorted((ROOT / 'Assets/VintageCollector/Audio').rglob('*'))
    folder_meta = ROOT / 'Assets/VintageCollector/Audio.meta'
    if folder_meta.is_file():
        sources.append(folder_meta)
    sources = [p for p in sources if p.is_file()]
    targets = {PROJECT / p.relative_to(ROOT): p for p in sources}
    native = ROOT / 'TestsRevealAudio/AudioProfileNativeChecks.cs'
    targets[PROJECT / 'Assets/Editor/AudioProfileNativeChecks.cs'] = native
    for dest, src in list(targets.items()):
        meta = src.with_name(src.name + '.meta')
        if meta.is_file() and not src.name.endswith('.meta'):
            targets[dest.with_name(dest.name + '.meta')] = meta
    expected = {p for p in targets if p.suffix == '.cs'}
    existing = set((PROJECT / 'Assets').rglob('*.cs')) if (PROJECT / 'Assets').exists() else set()
    if existing - expected:
        raise RuntimeError('Unexpected staged scripts: ' + str(existing - expected))
    for dest, src in targets.items():
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dest)
    for folder in ('Packages', 'ProjectSettings', 'Temp'):
        (PROJECT / folder).mkdir(parents=True, exist_ok=True)
    configs = {
        'Packages/manifest.json': json.dumps({'dependencies': {'com.unity.modules.' + m: '1.0.0' for m in ('audio', 'imageconversion', 'imgui', 'jsonserialize', 'ui')}}),
        'ProjectSettings/ProjectVersion.txt': 'm_EditorVersion: 6000.2.14f1\n',
    }
    for key, content in configs.items():
        (PROJECT / key).write_text(content, encoding='utf-8')
    pins = {src.relative_to(ROOT).as_posix(): {'source': digest(src), 'stagedPath': dest.relative_to(PROJECT).as_posix(), 'staged': digest(dest)} for dest, src in targets.items()}
    pins[Path(__file__).relative_to(ROOT).as_posix()] = {'source': digest(Path(__file__)), 'stagedPath': None}
    effective = {k: digest(PROJECT / k) for k in configs}
    (out / 'source-pins.json').write_text(json.dumps({'sources': pins, 'configuration': effective}, indent=2), encoding='utf-8')
    entry = 'AudioProfileNativeChecks.AuthorAndVerify' if args.author else 'AudioProfileNativeChecks.BeginVerify'
    command = [str(EDITOR), '-batchmode', '-nographics', '-projectPath', str(PROJECT), '-logFile', str(out / 'unity.log'), '-executeMethod', entry]
    env = dict(os.environ, TMP=str(PROJECT / 'Temp'), TEMP=str(PROJECT / 'Temp'), TMPDIR=str(PROJECT / 'Temp'), AUDIO_PROFILE_OUT=str(out))
    (out / 'command.json').write_text(json.dumps(command, indent=2), encoding='utf-8')
    result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=600)
    (out / 'execution.json').write_text(json.dumps({'exit': result.returncode, 'stdout': result.stdout, 'stderr': result.stderr}, indent=2), encoding='utf-8')
    drift = [k for k,v in pins.items() if digest(ROOT / k) != v['source']]
    staged_drift = [k for k,v in pins.items() if v.get('stagedPath') and digest(PROJECT / v['stagedPath']) != v['staged']]
    (out / 'source-after.json').write_text(json.dumps({'unchanged': not drift and not staged_drift, 'sourceDrift': drift, 'stagedDrift': staged_drift}, indent=2), encoding='utf-8')
    if drift or staged_drift:
        raise RuntimeError('Source drift prevents admission: ' + str((drift, staged_drift)))
    report_path = out / 'results.json'
    if not report_path.exists():
        print('Native exit:', result.returncode, 'No results; log:', out / 'unity.log')
        return result.returncode or 2
    report = json.loads(report_path.read_text(encoding='utf-8'))
    assert report['checkCount'] == len(report['checks'])
    print(json.dumps({'passed': report['passed'], 'failure': report['failure'],
                      'checkCount': report['checkCount'], 'importedClipCount': len(report['clips']),
                      'maxPcmError': max((c['maxPcmError'] for c in report['clips']), default=None)}, indent=2))
    passed = result.returncode == 0 and report['passed'] and report['checkCount'] > 0
    if args.adopt and passed:
        # Adopt only actual native-produced asset/metas, never overwrite existing source identities.
        stage_audio = PROJECT / 'Assets/VintageCollector/Audio'
        candidates = [p for p in stage_audio.rglob('*') if p.is_file() and (p.suffix == '.meta' or p.name == 'DefaultRevealAudioProfile.asset')]
        stage_folder_meta = stage_audio.with_name('Audio.meta')
        if stage_folder_meta.exists():
            candidates.append(stage_folder_meta)
        for source in candidates:
            dest = ROOT / source.relative_to(PROJECT)
            if dest.exists() and dest.read_bytes() != source.read_bytes():
                raise RuntimeError('Refusing to overwrite source asset/meta: ' + str(dest))
        adopted = []
        for source in candidates:
            dest = ROOT / source.relative_to(PROJECT)
            if not dest.exists():
                dest.parent.mkdir(parents=True, exist_ok=True)
                with dest.open('xb') as stream:
                    stream.write(source.read_bytes())
            adopted.append({'source': source.relative_to(PROJECT).as_posix(), 'target': dest.relative_to(ROOT).as_posix(), 'sha256': digest(dest)})
        (out / 'native-asset-adoption.json').write_text(json.dumps(adopted, indent=2), encoding='utf-8')
        print('Native asset/meta identities adopted:', len(adopted))
    print('Evidence:', out)
    return 0 if passed else result.returncode or 1

if __name__ == '__main__':
    sys.exit(main())
