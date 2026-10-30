"""Explicit package-free presentation checks; writes only .artifacts/presentation."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor')

def main():
    p = argparse.ArgumentParser(__doc__)
    p.add_argument('--label', required=True)
    p.add_argument('--filter', default='')
    a = p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):
        p.error('Unsafe label')
    out = ROOT / '.artifacts/presentation'
    out.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, TEMP=str(out), TMP=str(out), TMPDIR=str(out))
    mono = EDITOR / 'Data/MonoBleedingEdge'
    exe = out / 'PresentationTests.exe'
    sources = sorted((ROOT / 'Assets/VintageCollector/Presentation').glob('Opening*.cs'))
    sources += sorted((ROOT / 'Assets/VintageCollector/Presentation').glob('PresentationRules.cs'))
    sources += [ROOT / 'TestsPhase2/PresentationTests.cs']
    log = []
    def run(cmd):
        r = subprocess.run([str(x) for x in cmd], cwd=ROOT, env=env, capture_output=True, text=True)
        log.extend(['COMMAND '+json.dumps([str(x) for x in cmd]), r.stdout, r.stderr, 'EXIT '+str(r.returncode)])
        print(r.stdout, end='')
        print(r.stderr, end='', file=sys.stderr)
        return r
    try:
        r = run([mono / 'bin/mono.exe', mono / 'lib/mono/4.5/mcs.exe', '-warnaserror+', '-out:'+str(exe), *sources])
        if r.returncode:
            return r.returncode
        r = run([mono / 'bin/mono.exe', exe, a.filter])
        if not r.returncode:
            report = json.loads(r.stdout)
            if report['checkCount'] != len(report['checks']) or report['checkCount'] < 1 or not report['passed']:
                raise RuntimeError('Invalid assertion report')
            (out / (a.label+'.json')).write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
        return r.returncode
    finally:
        (out / (a.label+'.log')).write_text('\n'.join(log)+'\n', encoding='utf-8')

if __name__ == '__main__':
    sys.exit(main())
