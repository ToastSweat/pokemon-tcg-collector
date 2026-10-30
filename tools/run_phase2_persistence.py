"""Package-free, opt-in Mono persistence verification; all generated files stay project-local."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
MONO = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')

def main():
    p = argparse.ArgumentParser(__doc__)
    p.add_argument('--tests', action='store_true', required=True)
    p.add_argument('--label', default='latest')
    p.add_argument('--filter', default='')
    a = p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):
        p.error('Unsafe label')
    out = ROOT / '.artifacts/phase2-persistence' / a.label
    out.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, TMP=str(out), TEMP=str(out), TMPDIR=str(out))
    log = []
    protected = sorted((ROOT / 'Assets/VintageCollector/Domain').glob('*.cs')) + sorted((ROOT / 'Assets/VintageCollector/Data').glob('*.json'))
    before = {str(f.relative_to(ROOT)): hashlib.sha256(f.read_bytes()).hexdigest() for f in protected}
    def invoke(args):
        cmd = [str(x) for x in args]
        log.append('COMMAND ' + json.dumps(cmd))
        r = subprocess.run(cmd, cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
        log.extend([r.stdout, r.stderr, 'EXIT ' + str(r.returncode)])
        print(r.stdout, end='')
        print(r.stderr, end='', file=sys.stderr)
        return r
    try:
        library = out / 'VintageCollector.Domain.dll'
        compiler = [MONO / 'bin/mono.exe', MONO / 'lib/mono/4.5/mcs.exe', '-warnaserror+']
        r = invoke([*compiler, '-target:library', '-out:' + str(library), *sorted((ROOT / 'Assets/VintageCollector/Domain').glob('*.cs'))])
        if r.returncode:
            return r.returncode
        exe = out / 'PersistenceTests.exe'
        sources = sorted((ROOT / 'Assets/VintageCollector/Persistence').glob('*.cs')) + [ROOT / 'TestsPhase2/PersistenceTests.cs']
        (out / 'compiled-inputs.json').write_text(json.dumps({str(f.relative_to(ROOT)): hashlib.sha256(f.read_bytes()).hexdigest() for f in [*protected, *sources]}, indent=2) + '\n', encoding='utf-8')
        r = invoke([*compiler, '-r:System.Runtime.Serialization', '-r:' + str(library), '-out:' + str(exe), *sources])
        if r.returncode:
            return r.returncode
        r = invoke([MONO / 'bin/mono.exe', exe, str(ROOT), str(out), a.filter])
        report = json.loads(r.stdout)
        (out / 'results.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
        if report['testCount'] != len(report['checks']) or report['testCount'] <= 0:
            raise RuntimeError('Invalid or zero test count')
        if r.returncode or not report['passed'] or not all(c['passed'] for c in report['checks']):
            return 1
        return 0
    finally:
        after = {str(f.relative_to(ROOT)): hashlib.sha256(f.read_bytes()).hexdigest() for f in protected}
        (out / 'protected-inputs.json').write_text(json.dumps({'before': before, 'after': after, 'unchanged': before == after}, indent=2) + '\n', encoding='utf-8')
        (out / 'execution.log').write_text('\n'.join(log), encoding='utf-8')
        if before != after:
            raise RuntimeError('Protected input changed')

if __name__ == '__main__':
    sys.exit(main())
