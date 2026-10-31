"""Run a targeted Python audio suite, retaining its real exit and full output."""
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.artifacts/reveal-audio-profile'

def main():
    label = sys.argv[1]
    if not label.replace('-', '').replace('_', '').isalnum():
        raise ValueError('simple evidence label required')
    OUT.mkdir(parents=True, exist_ok=True)
    log = OUT / (label + '.log')
    if log.exists():
        raise FileExistsError(log)
    env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', TMP=str(OUT), TEMP=str(OUT), TMPDIR=str(OUT))
    result = subprocess.run([sys.executable, str(ROOT / 'TestsRevealAudio/AudioProfileSynthesisTests.py'), *sys.argv[2:]], capture_output=True, text=True, env=env)
    text = 'RUN exit=' + str(result.returncode) + '\n' + result.stdout + result.stderr
    log.write_text(text, encoding='utf-8')
    print(text, end='')
    print('Evidence: ' + str(log))
    return result.returncode

if __name__ == '__main__':
    sys.exit(main())
