"""Package-free media pure tests. All outputs, fixtures and retained logs are under .artifacts."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import struct
import zlib

def fixtures(out):
    # Original algorithmic checker swatches, not supplied/user/branded images.
    def png(name, width, height, rgba=(38, 161, 133, 255), broken=False, rgb=False):
        def chunk(tag, data):
            return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
        row=b''.join(bytes((rgba if (x//8)%2==0 else (210,140,55,255))[:3 if rgb else 4]) for x in range(width))
        raw=(b'\0'+row)*height
        data=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',width,height,8,2 if rgb else 6,0,0,0))+chunk(b'IDAT',b'not zlib image data' if broken else zlib.compress(raw))+chunk(b'IEND',b'')
        (out/name).write_bytes(data)
    png('original-synthetic-bad-zlib.png',126,176,broken=True)
    png('original-synthetic-rgb-card.png',126,176,rgb=True)
    png('original-synthetic-card.png',126,176)
    png('original-synthetic-mask.png',126,176,(255,255,255,255))
    png('original-synthetic-other-mask.png',63,88,(255,255,255,255))
    png('original-synthetic-large-card.png',2016,2816)
    png('original-synthetic-wrapper.png',128,128)
    png('original-synthetic-too-wide.png',4097,1)
    png('original-synthetic-too-many-pixels.png',3000,3000)
    import wave
    for bits, channels in [(16,1),(24,2)]:
        data=bytearray()
        for i in range(2205*channels):
            value=[0,1<<(bits-3),0,-(1<<(bits-3))][i%4]
            data.extend(value.to_bytes(bits//8,'little',signed=True))
        with wave.open(str(out/f'original-synthetic-pcm{bits}.wav'),'wb') as w:
            w.setnchannels(channels); w.setsampwidth(bits//8); w.setframerate(22050); w.writeframes(data)
    (out/'original-synthetic-not-png.png').write_bytes(b'not a PNG image')
    (out/'FIXTURES.txt').write_text('Original synthetic algorithmic PNG/WAV test fixtures only. Not supplied materials, not branded reproductions. No redistribution permission inferred.\n',encoding='utf-8')

ROOT = Path(__file__).resolve().parents[1]
MONO = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')

def protected():
    files = []
    for directory in ['Assets/VintageCollector/Domain', 'Assets/VintageCollector/Data', 'Assets/VintageCollector/Persistence', 'Builds/Phase2Windows']:
        # Prune before descent: even listing/hashing an accepted build's Saves is forbidden.
        # Shared by the pure and native callers; never inspect save contents to prove nonmutation.
        for current, dirs, names in os.walk(ROOT / directory, topdown=True, followlinks=False):
            dirs[:] = [d for d in dirs if d.casefold() != 'saves']
            files.extend(Path(current) / name for name in names if name.casefold() != 'saves')
    files.extend([ROOT/'Assets/VintageCollector/Runtime/OpeningGame.cs', ROOT/'Assets/VintageCollector/Runtime/GamePaths.cs', ROOT/'Assets/Supplied/AssetManifest.json'])
    return {str(f.relative_to(ROOT)): hashlib.sha256(f.read_bytes()).hexdigest() for f in files if f.is_file()}

def main():
    p = argparse.ArgumentParser(__doc__)
    p.add_argument('--label', required=True)
    a = p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label):
        p.error('Unsafe label')
    out = ROOT/'.artifacts/phase3-media'/a.label
    out.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, TMP=str(out), TEMP=str(out), TMPDIR=str(out))
    before = protected()
    fixtures(out)
    log = []
    (out/'junction-target').mkdir(exist_ok=True)
    (out/'junction-target/original.bin').write_bytes(b'original synthetic test bytes')
    if not (out/'reparse').exists():
        junction = subprocess.run(['cmd.exe','/c','mklink','/J',str(out/'reparse'),str(out/'junction-target')],env=env,capture_output=True,text=True)
        log.extend([junction.stdout,junction.stderr,'JUNCTION EXIT '+str(junction.returncode)])
        if junction.returncode:
            raise RuntimeError('Cannot create isolated reparse fixture: '+junction.stderr)
    sources = [ROOT/'TestsPhase3/MediaPureTests.cs'] + sorted((ROOT/'Assets/VintageCollector/Media').glob('Media*.cs')) + sorted((ROOT/'Assets/VintageCollector/Domain').glob('*.cs'))
    try:
        (out/'compiled-inputs.json').write_text(json.dumps({str(f.relative_to(ROOT)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sources}, indent=2), encoding='utf-8')
        commands = [[MONO/'bin/mono.exe', MONO/'lib/mono/4.5/mcs.exe', '-warnaserror+', '-out:'+str(out/'MediaPureTests.exe'), *sources], [MONO/'bin/mono.exe', out/'MediaPureTests.exe', ROOT, out]]
        for i, cmd in enumerate(commands):
            r = subprocess.run([str(x) for x in cmd], cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
            log.extend(['COMMAND '+json.dumps([str(x) for x in cmd]),r.stdout,r.stderr,'EXIT '+str(r.returncode)])
            print(r.stdout, end=''); print(r.stderr, end='', file=sys.stderr)
            if i == 1:
                report = json.loads(r.stdout)
                (out/'results.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
                if report['testCount'] < 1 or report['testCount'] != len(report['checks']):
                    raise RuntimeError('Zero/mismatched test count')
                if not report['passed'] or not all(c['passed'] for c in report['checks']):
                    return r.returncode or 1
            if r.returncode:
                return r.returncode
        return 0
    finally:
        after = protected()
        (out/'protected-inputs.json').write_text(json.dumps({'before':before,'after':after,'unchanged':before==after},indent=2),encoding='utf-8')
        (out/'execution.log').write_text('\n'.join(log),encoding='utf-8')
        if before != after:
            raise RuntimeError('Protected source/data/save/baseline changed')

if __name__ == '__main__':
    sys.exit(main())
