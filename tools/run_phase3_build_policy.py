"""Package-free tests for separate Phase 3 build admission and save preservation."""
from pathlib import Path
import json
import os
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
MONO = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
OUT = ROOT / '.artifacts/phase3-build-policy'


def main():
    label = sys.argv[1] if len(sys.argv) > 1 else 'latest'
    if not re.fullmatch(r'[A-Za-z0-9_-]+', label):
        raise ValueError('Unsafe label')
    OUT.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, TMP=str(OUT), TEMP=str(OUT), TMPDIR=str(OUT))
    sources = [ROOT/'TestsPhase3/BuildPolicyTests.cs', ROOT/'Assets/VintageCollector/Runtime/GamePaths.cs',
               ROOT/'Assets/VintageCollector/Editor/Phase3BuildPolicy.cs']
    exe = OUT/'BuildPolicyTests.exe'
    log = []
    result = subprocess.run([str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+',
                             '-out:'+str(exe),*map(str,sources)],cwd=ROOT,env=env,capture_output=True,text=True)
    log.extend([result.stdout,result.stderr,'COMPILE_EXIT '+str(result.returncode)])
    checks, passed = [], False
    if result.returncode == 0:
        result = subprocess.run([str(MONO/'bin/mono.exe'),str(exe),str(OUT/'fixtures')],cwd=ROOT,env=env,capture_output=True,text=True)
        log.extend([result.stdout,result.stderr,'TEST_EXIT '+str(result.returncode)])
        checks = [{'name':name,'passed':True} for name in re.findall(r'^PASS (.+)$',result.stdout,re.M)]
        count = re.search(r'^TEST_COUNT (\d+)$',result.stdout,re.M)
        passed = result.returncode == 0 and count is not None and int(count[1]) == len(checks) and len(checks) > 0
    report = {'passed':passed,'testCount':len(checks),'checks':checks,'processExit':result.returncode}
    (OUT/(label+'.log')).write_text('\n'.join(log),encoding='utf-8')
    (OUT/(label+'.json')).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print('\n'.join(log))
    print(json.dumps(report))
    return 0 if passed else 1


if __name__ == '__main__':
    sys.exit(main())
