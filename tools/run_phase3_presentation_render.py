"""Build and exercise labelled original synthetic graphics; never the core player."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import struct
import zlib
sys.dont_write_bytecode = True
from run_phase3_presentation_editor import ROOT, EDITOR, PROJECT, OUT, stage

def validate_png(path):
    raw=path.read_bytes()
    if raw[:8]!=b'\x89PNG\r\n\x1a\n': raise ValueError('PNG signature: '+str(path))
    at=8; dimensions=None; ended=False
    while at<len(raw):
        n=struct.unpack('>I',raw[at:at+4])[0]; kind=raw[at+4:at+8]; data=raw[at+8:at+8+n]
        crc=struct.unpack('>I',raw[at+8+n:at+12+n])[0]
        if zlib.crc32(kind+data)&0xffffffff!=crc: raise ValueError('PNG CRC: '+str(path))
        if kind==b'IHDR': dimensions=struct.unpack('>II',data[:8])
        at+=12+n
        if kind==b'IEND': ended=True; break
    if not ended or not dimensions or min(dimensions)<1: raise ValueError('PNG incomplete')
    return dimensions

def main():
    p=argparse.ArgumentParser(__doc__); p.add_argument('--label',required=True); a=p.parse_args()
    if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_' for c in a.label): p.error('Unsafe label')
    env=stage()
    for name in ['PresentationRenderFixture.cs','PresentationFixtureBuild.cs']:
        shutil.copy2(ROOT/'TestsPhase3'/name,PROJECT/('Assets/VintageCollector/Editor' if 'Build' in name else 'Assets')/name)
    capture=OUT/'render'
    if capture.exists(): shutil.rmtree(capture)
    cmd=[str(EDITOR),'-batchmode','-nographics','-projectPath',str(PROJECT),'-logFile',str(OUT/(a.label+'-build.log')),'-executeMethod','PresentationFixtureBuild.Run']
    r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300)
    (OUT/(a.label+'-build-process.log')).write_text(json.dumps(cmd)+'\n'+r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    print('Isolated build exit',r.returncode)
    if r.returncode: return r.returncode
    player=ROOT/'.artifacts/phase3-presentation-player/PresentationFixture.exe'
    r=subprocess.run([str(player),'-screen-fullscreen','0','-logFile',str(OUT/(a.label+'-player.log'))],cwd=player.parent,env=env,capture_output=True,text=True,timeout=180)
    (OUT/(a.label+'-player-process.log')).write_text(r.stdout+'\n'+r.stderr+'\nEXIT '+str(r.returncode),encoding='utf-8')
    report=capture/'render-report.json'
    if not report.exists(): print('No graphics report'); return r.returncode or 2
    data=json.loads(report.read_text(encoding='utf-8'))
    shutil.copy2(report,OUT/(a.label+'-render.json'))
    archive=OUT/(a.label+'-captures')
    if archive.exists(): raise RuntimeError('Refusing to overwrite retained capture evidence: '+str(archive))
    shutil.copytree(capture,archive)
    print(json.dumps(data))
    for name in data['screenshots']:
        print('Verified PNG',name,validate_png(capture/name))
    if data['checkCount']!=len(data['checks']) or not data['passed'] or data['checkCount']<1 or data['screenshotCount']!=len(data['screenshots']): return r.returncode or 1
    return r.returncode

if __name__=='__main__': sys.exit(main())
