"""Exercise exact extracted renderer acknowledgement members; NOT native Unity playback proof."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess

ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
SOURCE=ROOT/'Assets/VintageCollector/Presentation/TabletopPresentation.cs'
HARNESS=ROOT/'TestsRevealAudio/RendererBoundaryChecks.cs'

def member(text,signature):
    start=text.index(signature)
    brace=text.index('{',start)
    depth=1
    end=brace+1
    while depth:
        if text[end]=='{':depth+=1
        elif text[end]=='}':depth-=1
        end+=1
    return text[start:end]

def main():
    parser=argparse.ArgumentParser(__doc__)
    parser.add_argument('--label',required=True)
    parser.add_argument('--filter',default='')
    args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    out=ROOT/'.artifacts/reveal-audio-renderer'/args.label
    out.mkdir(parents=True,exist_ok=False)
    paths=[SOURCE,HARNESS,Path(__file__).resolve()]
    pins={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    text=SOURCE.read_text(encoding='utf-8')
    members=[member(text,s) for s in ('private void StopFoley()', 'private float Volume', 'private void SyncAudio()', 'private bool IsCommittedShape(string command)', 'public void NotifyCommitted(string command)')]
    optional='public IRevealSoundMedia RevealSounds'
    members.append(member(text,optional) if optional in text else 'public IRevealSoundMedia RevealSounds {get {return revealSounds;}set {revealSounds=value;}}')
    generated=out/'ExtractedRendererChecks.cs'
    generated.write_text(HARNESS.read_text(encoding='utf-8').replace('// EXTRACTED_MEMBERS','\n'.join(members)),encoding='utf-8')
    env=dict(os.environ,TEMP=str(out),TMP=str(out),TMPDIR=str(out))
    commands=[
        [str(MONO/'bin/mono.exe'),str(MONO/'lib/mono/4.5/mcs.exe'),'-warnaserror+','-nowarn:0414','-out:'+str(out/'Checks.exe'),str(generated)],
        [str(MONO/'bin/mono.exe'),str(out/'Checks.exe'),args.filter]
    ]
    logs=[];exit_code=0
    for command in commands:
        run=subprocess.run(command,cwd=ROOT,env=env,text=True,capture_output=True,timeout=120)
        logs.append({'command':command,'exitCode':run.returncode,'stdout':run.stdout,'stderr':run.stderr})
        print(run.stdout,end='');print(run.stderr,end='')
        if run.returncode:
            exit_code=run.returncode
            break
    after={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    result={'evidence':'extracted-production-managed-boundary-only','sourcePins':pins,'afterPins':after,'unchanged':pins==after,'exitCode':exit_code,'runs':logs}
    (out/'results.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    if pins!=after:return 3
    return exit_code

if __name__=='__main__':raise SystemExit(main())
