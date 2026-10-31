"""Offline explicit-source core Phase4 real-file harness; synthetic directories only."""
import argparse, hashlib, json, os, re, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MONO=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
SOURCES=['TestsPhase4/CoreTests.cs','Assets/VintageCollector/Domain/Data.cs','Assets/VintageCollector/Domain/Catalog.cs','Assets/VintageCollector/Domain/Random.cs','Assets/VintageCollector/Domain/PackGenerator.cs','Assets/VintageCollector/Persistence/SessionData.cs','Assets/VintageCollector/Persistence/StateValidation.cs','Assets/VintageCollector/Persistence/SaveProtocol.cs','Assets/VintageCollector/Persistence/SessionStore.cs','Assets/VintageCollector/Collections/CollectionContract.cs','Assets/VintageCollector/Collections/PhysicalStore.cs','Assets/VintageCollector/Collections/PhysicalValidation.cs','Assets/VintageCollector/Collections/PhysicalProtocol.cs','Assets/VintageCollector/Collections/BinderProjection.cs','Assets/VintageCollector/Collections/CollectionProfiles.cs','Assets/VintageCollector/Collections/CollectionFiles.cs','Assets/VintageCollector/Persistence/SessionArchiveValidation.cs','Assets/VintageCollector/Collections/CollectionEnvelope.cs','Assets/VintageCollector/Collections/CollectionArchive.cs','Assets/VintageCollector/Collections/CollectionZipStructure.cs','Assets/VintageCollector/Collections/CollectionJsonShape.cs']
def main():
 p=argparse.ArgumentParser(); p.add_argument('--label',required=True); a=p.parse_args()
 if not re.fullmatch(r'[a-zA-Z0-9_-]+',a.label): p.error('unsafe label')
 out=ROOT/'.artifacts/phase4-core'/a.label
 if (out/'execution.log').exists() or (out/'CoreTests.exe').exists(): p.error('Evidence label already used; choose a fresh --label')
 out.mkdir(parents=True,exist_ok=True)
 env=dict(os.environ,TMP=str(out),TEMP=str(out),TMPDIR=str(out),PHASE4_CATALOG=str(ROOT/"Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json"))
 sources=[ROOT/s for s in SOURCES]; inputs=SOURCES+['Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json','Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json']; pins={s:hashlib.sha256((ROOT/s).read_bytes()).hexdigest() for s in inputs}
 (out/'compiled-inputs.json').write_text(json.dumps(pins,indent=2))
 commands=[[MONO/'bin/mono.exe',MONO/'lib/mono/4.5/mcs.exe','-warnaserror+','-r:System.Web.Extensions','-r:System.IO.Compression','-r:System.IO.Compression.FileSystem','-out:'+str(out/'CoreTests.exe'),*sources],[MONO/'bin/mono.exe',out/'CoreTests.exe',ROOT,out]]
 log=[]; result=1
 for name,target in [('reparse','junction-target'),('reparse-export/Backups','junction-target'),('reparse-files/physical.save','junction-target')]:
  link=out/name; dest=out/target; dest.mkdir(parents=True,exist_ok=True); link.parent.mkdir(parents=True,exist_ok=True)
  if not link.exists():
   j=subprocess.run(['cmd.exe','/c','mklink','/J',str(link),str(dest)],env=env,capture_output=True,text=True)
   log+=['JUNCTION '+str(link),j.stdout,j.stderr,'EXIT '+str(j.returncode)]
   if j.returncode: raise RuntimeError('Synthetic junction creation failed')
 try:
  for i,cmd in enumerate(commands):
   r=subprocess.run([str(x) for x in cmd],cwd=ROOT,env=env,capture_output=True,text=True,timeout=180)
   log += ['COMMAND '+json.dumps([str(x) for x in cmd]),r.stdout,r.stderr,'EXIT '+str(r.returncode)]
   print(r.stdout,end=''); print(r.stderr,end='',file=sys.stderr)
   result=r.returncode
   if i==1:
    report=json.loads(r.stdout); (out/'results.json').write_text(json.dumps(report,indent=2))
    if report['testCount']<1 or report['testCount']!=len(report['checks']) or report['passed']!=all(x['passed'] for x in report['checks']): raise RuntimeError('Invalid counts')
    if not report['passed']: result=result or 1
   if result: break
  return result
 finally:
  after={s:hashlib.sha256((ROOT/s).read_bytes()).hexdigest() for s in inputs}
  (out/'source-pins.json').write_text(json.dumps({'before':pins,'after':after,'unchanged':pins==after},indent=2))
  (out/'execution.log').write_text('\n'.join(log))
  if pins!=after: raise RuntimeError('Test inputs changed while executing')
if __name__=='__main__': sys.exit(main())
