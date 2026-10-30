"""Compile/exercise scoped runtime paths with Unity Mono; all fixtures stay project-local."""
from pathlib import Path
import json
import os
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.artifacts/phase2-paths'
MONO = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')


def main():
    label = sys.argv[1] if len(sys.argv) > 1 else 'latest'
    if not re.fullmatch(r'[A-Za-z0-9_-]+', label):
        raise ValueError('Unsafe artifact label')
    OUT.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, TMP=str(OUT), TEMP=str(OUT), TMPDIR=str(OUT))
    target = OUT / 'junction-target'
    target.mkdir(exist_ok=True)
    default_root = OUT / 'default-root'
    default_root.mkdir(exist_ok=True)
    for junction in (OUT / 'junction-fixture', default_root / 'Saves'):
        if not junction.exists():
            subprocess.run(['cmd.exe', '/c', 'mklink', '/J', str(junction), str(target)],
                           cwd=ROOT, env=env, check=True, capture_output=True)
        if not junction.is_junction():
            raise RuntimeError('Owned directory-junction fixture missing: ' + str(junction))
    sources = [ROOT / 'TestsPhase2/RuntimePathTests.cs', ROOT / 'Assets/VintageCollector/Runtime/GamePaths.cs']
    exe = OUT / 'RuntimePathTests.exe'
    log = []
    result = subprocess.run([str(MONO/'bin/mono.exe'), str(MONO/'lib/mono/4.5/mcs.exe'),
                             '-warnaserror+', '-out:'+str(exe), *map(str, sources)],
                            cwd=ROOT, env=env, capture_output=True, text=True)
    log.extend([result.stdout, result.stderr, 'COMPILE_EXIT '+str(result.returncode)])
    checks = []
    passed = False
    if result.returncode == 0:
        result = subprocess.run([str(MONO/'bin/mono.exe'), str(exe)], cwd=ROOT, env=env,
                                capture_output=True, text=True)
        log.extend([result.stdout, result.stderr, 'TEST_EXIT '+str(result.returncode)])
        checks = [{'name': name, 'passed': True} for name in re.findall(r'^PASS (.+)$', result.stdout, re.M)]
        declared = re.search(r'^TEST_COUNT (\d+)$', result.stdout, re.M)
        passed = result.returncode == 0 and declared is not None and int(declared[1]) == len(checks) and len(checks) > 0
    report = {'passed': passed, 'testCount': len(checks), 'checks': checks, 'processExit': result.returncode}
    (OUT/(label+'.log')).write_text('\n'.join(log), encoding='utf-8')
    (OUT/(label+'.json')).write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print('\n'.join(log))
    print(json.dumps(report))
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
