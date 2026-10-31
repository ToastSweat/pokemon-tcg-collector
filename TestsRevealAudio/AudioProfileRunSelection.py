"""Compile only this suite's explicitly owned sources; preserve actual exits/logs."""
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor')
OUT = ROOT / '.artifacts/reveal-audio-profile'

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    label = sys.argv[1] if len(sys.argv) > 1 else 'selection'
    if not label.replace('-', '').replace('_', '').isalnum():
        raise ValueError('simple evidence label required')
    exe = OUT / 'AudioProfileSelectionTests.exe'
    sources = [ROOT / 'TestsRevealAudio/AudioProfileUnityDoubles.cs',
               ROOT / 'Assets/VintageCollector/Media/PresentationMediaContract.cs',
               ROOT / 'TestsRevealAudio/AudioProfileSelectionTests.cs']
    sources += [p for p in (ROOT / 'Assets/VintageCollector/Audio/RevealAudioProfile.cs',
                           ROOT / 'Assets/VintageCollector/Audio/RevealAudioHost.cs') if p.exists()]
    mono = str(EDITOR / 'Data/MonoBleedingEdge/bin/mono.exe')
    command = [mono, str(EDITOR / 'Data/MonoBleedingEdge/lib/mono/4.5/mcs.exe'),
               '-warnaserror+', '-out:' + str(exe), *map(str, sources)]
    env = dict(os.environ, TMP=str(OUT), TEMP=str(OUT), TMPDIR=str(OUT))
    result = subprocess.run(command, capture_output=True, text=True, env=env)
    text = 'COMPILE exit=' + str(result.returncode) + '\n' + result.stdout + result.stderr
    code = result.returncode
    if code == 0:
        result = subprocess.run([mono, str(exe)], capture_output=True, text=True, env=env)
        text += 'RUN exit=' + str(result.returncode) + '\n' + result.stdout + result.stderr
        code = result.returncode
    log = OUT / (label + '.log')
    if log.exists():
        raise FileExistsError(log)
    log.write_text(text, encoding='utf-8')
    print(text, end='')
    print('Evidence: ' + str(log))
    return code

if __name__ == '__main__':
    sys.exit(main())
