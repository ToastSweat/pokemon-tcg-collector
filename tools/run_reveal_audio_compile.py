"""Compile current runtime plus owned reveal-audio builder against real installed Unity references; not native playback."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
ROOT=Path(__file__).resolve().parents[1]
DATA=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data')
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True);args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    out=ROOT/'.artifacts/reveal-audio-compile'/args.label;out.mkdir(parents=True,exist_ok=False);tmp=out/'tmp';tmp.mkdir()
    env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1',MONO_PATH=str(DATA/'Managed/UnityEngine'))
    runtime=[p for folder in ['Domain','Persistence','Collections','Media','Presentation','Runtime','Audio'] for p in sorted((ROOT/'Assets/VintageCollector'/folder).glob('*.cs'))]
    editor=[ROOT/'Assets/VintageCollector/Editor'/n for n in ['TiltBuildPolicy.cs','RevealAudioBuildPolicy.cs','RevealAudioBuild.cs','RevealAudioNativeChecks.cs']]
    modules=['UnityEngine','UnityEngine.CoreModule','UnityEngine.IMGUIModule','UnityEngine.AudioModule','UnityEngine.ImageConversionModule','UnityEngine.ScreenCaptureModule','UnityEngine.JSONSerializeModule','UnityEngine.InputLegacyModule','UnityEngine.PhysicsModule','UnityEngine.TextRenderingModule']
    refs=['-r:'+str(DATA/'Managed/UnityEngine'/(n+'.dll')) for n in modules]+['-r:'+str(DATA/'MonoBleedingEdge/lib/mono/4.5'/n) for n in ['System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','Facades/netstandard.dll']]
    mono=DATA/'MonoBleedingEdge/bin/mono.exe';base=[str(mono),str(DATA/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'-nologo','-warnaserror+','-langversion:latest']
    test=ROOT/'TestsRevealAudio/PlayerOptInTests.cs'
    inputs=runtime+editor+[test,Path(__file__).resolve()];pins={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in inputs}
    commands=[base+['-target:library']+refs+['-out:'+str(out/'Assembly-CSharp.dll')]+list(map(str,runtime)),base+['-target:library']+refs+['-r:'+str(DATA/'Managed/UnityEditor.dll'),'-out:'+str(out/'RevealAudioBuild.dll')]+list(map(str,editor)),base+['-target:exe']+refs+['-r:'+str(out/'Assembly-CSharp.dll'),'-out:'+str(out/'tests.exe'),str(test)],[str(mono),str(out/'tests.exe')]]
    log=[];exits=[];checks=[];declared=0
    for command in commands:
        result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=120);log.extend([json.dumps(command),result.stdout,result.stderr,'EXIT '+str(result.returncode)]);exits.append(result.returncode)
        if result.returncode:break
        checks=re.findall(r'^PASS (.+)$',result.stdout,re.M);count=re.search(r'^TEST_COUNT (\d+)$',result.stdout,re.M);declared=int(count[1]) if count else 0
    passed=exits==[0,0,0,0] and len(checks)==declared>0 and all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==digest for p,digest in pins.items())
    (out/'compiled-inputs.json').write_text(json.dumps(pins,indent=2));(out/'execution.log').write_text('\n'.join(log));report={'passed':passed,'exits':exits,'nativePlayback':False,'checkCount':len(checks),'checks':checks}
    (out/'results.json').write_text(json.dumps(report,indent=2));print('\n'.join(log));print(json.dumps(report));return 0 if passed else 1
if __name__=='__main__':sys.exit(main())
