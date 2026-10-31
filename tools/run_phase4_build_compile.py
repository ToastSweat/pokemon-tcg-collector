"""Compile real Phase 4 runtime and owned builder against installed Unity reference assemblies; not a native execution claim."""
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
    out=ROOT/'.artifacts/phase4-build-compile'/args.label;out.mkdir(parents=True,exist_ok=False);tmp=out/'tmp';tmp.mkdir()
    env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
    runtime=[p for folder in ['Domain','Persistence','Collections','Media','Presentation','Runtime'] for p in sorted((ROOT/'Assets/VintageCollector'/folder).glob('*.cs'))]
    editor=[ROOT/'Assets/VintageCollector/Editor'/name for name in ['Phase4BuildPolicy.cs','Phase4Build.cs']]
    modules=['UnityEngine','UnityEngine.CoreModule','UnityEngine.IMGUIModule','UnityEngine.AudioModule','UnityEngine.ImageConversionModule','UnityEngine.ScreenCaptureModule','UnityEngine.JSONSerializeModule','UnityEngine.InputLegacyModule','UnityEngine.PhysicsModule','UnityEngine.TextRenderingModule']
    references=['-r:'+str(DATA/'Managed/UnityEngine'/(name+'.dll')) for name in modules]
    references+=['-r:'+str(DATA/'MonoBleedingEdge/lib/mono/4.5/System.IO.Compression.dll'),'-r:'+str(DATA/'MonoBleedingEdge/lib/mono/4.5/System.IO.Compression.FileSystem.dll'),'-r:'+str(DATA/'MonoBleedingEdge/lib/mono/4.5/Facades/netstandard.dll')]
    base=[str(DATA/'MonoBleedingEdge/bin/mono.exe'),str(DATA/'MonoBleedingEdge/lib/mono/4.5/csc.exe'),'-nologo','-target:library','-warnaserror+','-langversion:latest']
    pins={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in runtime+editor};(out/'compiled-inputs.json').write_text(json.dumps(pins,indent=2))
    log=[];exits=[]
    for command in [base+references+['-out:'+str(out/'Assembly-CSharp.dll')]+list(map(str,runtime)),base+references+['-r:'+str(DATA/'Managed/UnityEditor.dll'),'-out:'+str(out/'Phase4Build.dll')]+list(map(str,editor))]:
        result=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True);log.extend([json.dumps(command),result.stdout,result.stderr,'EXIT '+str(result.returncode)]);exits.append(result.returncode)
    passed=exits==[0,0] and all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==value for p,value in pins.items())
    (out/'execution.log').write_text('\n'.join(log));report={'passed':passed,'compilerExits':exits,'sourceCount':len(pins),'nativeExecution':False}
    (out/'results.json').write_text(json.dumps(report,indent=2));print('\n'.join(log));print(json.dumps(report));return 0 if passed else 1
if __name__=='__main__':sys.exit(main())
