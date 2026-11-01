"""Execute only build-tool-owned pure suites and managed real-Unity-assembly compilation. Never launches Unity.exe."""
from pathlib import Path
import argparse,json,os,re,subprocess,sys,zlib,struct
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1]
E=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data')
from run_phase5_build_policy import sha,no_reparse,compiler_manifest,preserved_players,originals_manifest,shared_base_pins
TESTS=('RuntimeBuildPolicyTests.py','RuntimeBuildNativeTests.py','RuntimePlayerSmokeTests.py','RuntimePlayerRunnerTests.py','RuntimePlayerInputSafetyTests.py')

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--label',required=True);a=p.parse_args()
 if not re.fullmatch('[A-Za-z0-9_-]+',a.label):p.error('Unsafe label')
 stage=ROOT/'.artifacts/phase5-build-tools';out=stage/'evidence'/a.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False);tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TEMP=str(tmp),TMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
 before={'source':compiler_manifest(),'players':preserved_players(),'originals':originals_manifest(),'sharedBase':shared_base_pins()};records=[];failure=None
 owned=sorted((stage/'Assets/VintageCollector').rglob('*.cs'));sources=[q for q in (ROOT/'Assets/VintageCollector').rglob('*.cs') if 'Editor' not in q.relative_to(ROOT).parts and q.name!='Phase5SmokeProbe.cs']
 refs={q.name:q for q in list((E/'Managed').glob('*.dll'))+list((E/'Managed/UnityEngine').glob('*.dll'))};refs['netstandard.dll']=E/'MonoBleedingEdge/lib/mono/4.5/Facades/netstandard.dll'
 mono=E/'MonoBleedingEdge/bin/mono.exe';mcs=E/'MonoBleedingEdge/lib/mono/4.5/mcs.exe'
 inputs=owned+sources+[ROOT/'TestsPhase5'/n for n in TESTS]+[ROOT/n for n in ('TestsPhase5/RuntimeBuildPolicyChecks.cs','TestsPhase5/RuntimePlayerPureChecks.cs','tools/run_phase5_build_tools_tests.py','tools/run_phase5_build.py','tools/run_phase5_build_policy.py','tools/verify_phase5_player.py','tools/phase5_owned_window_input.py','tools/verify_tilt_player.py','tools/verify_phase3_player.py','tools/verify_reveal_audio_native.py','tools/run_reveal_audio_build.py','tools/run_tilt_build.py','docs/technical/Phase_5_Runtime_Contract.md','docs/technical/Phase_5_Runtime_Discovery.md','docs/technical/Phase_5_Runtime_API_Additions.md')]
 pins={q.relative_to(ROOT).as_posix():sha(q) for q in inputs};(out/'inputs.json').write_text(json.dumps(pins,indent=2))
 def run(name,command,expected=None,expected_checks=None):
  r=subprocess.run([str(x) for x in command],cwd=ROOT,env=env,capture_output=True,text=True,timeout=180);record={'name':name,'command':[str(x) for x in command],'exitCode':r.returncode,'stdout':r.stdout,'stderr':r.stderr};records.append(record);(out/(name+'.json')).write_text(json.dumps(record,indent=2))
  if r.returncode!=0:raise RuntimeError(name+' real execution failed: '+r.stdout+r.stderr)
  if expected is not None and not re.search(r'Ran '+str(expected)+r' tests?\b',r.stdout+r.stderr):raise ValueError(name+' test count mismatch')
  if expected_checks is not None:
   names=re.findall(r'^PASS (.+)$',r.stdout,re.M)
   if not re.search(r'^CHECKS '+str(expected_checks)+r'\s*$',r.stdout,re.M) or len(names)!=expected_checks or len(set(names))!=expected_checks:raise ValueError(name+' C# check count mismatch')
 counts=(15,5,4,7,10)
 try:
  for n,count in zip(TESTS,counts):run(n,[sys.executable,'-B',ROOT/'TestsPhase5'/n],expected=count)
  lib=out/'Phase5RuntimeProbe.dll';run('runtime-managed-compile',[mono,mcs,'-warnaserror+','-target:library','-r:System.IO.Compression.dll','-r:System.IO.Compression.FileSystem.dll','-out:'+str(lib)]+['-r:'+str(q) for q in refs.values()]+sources+[stage/'Assets/VintageCollector/Runtime/Phase5SmokeProbe.cs'])
  run('editor-managed-compile',[mono,mcs,'-warnaserror+','-target:library','-out:'+str(out/'Phase5EditorTools.dll')]+['-r:'+str(q) for q in refs.values()]+[q for q in owned if 'Editor' in q.parts])
  policy=out/'PolicyChecks.exe';run('policy-compile',[mono,mcs,'-warnaserror+','-out:'+str(policy),ROOT/'TestsPhase5/RuntimeBuildPolicyChecks.cs',stage/'Assets/VintageCollector/Editor/Phase5BuildPolicy.cs']);run('policy-execute',[mono,policy,out/'policy-fixture'],expected_checks=6)
  pure=out/'PlayerPureChecks.exe';run('player-pure-compile',[mono,mcs,'-warnaserror+','-r:'+str(lib),'-r:'+str(refs['UnityEngine.CoreModule.dll']),'-r:'+str(refs['netstandard.dll']),'-out:'+str(pure),ROOT/'TestsPhase5/RuntimePlayerPureChecks.cs'])
  def chunk(kind,b):return struct.pack('>I',len(b))+kind+b+struct.pack('>I',zlib.crc32(kind+b)&0xffffffff)
  png=out/'synthetic-unit-not-player.png';png.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',2,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\x00\xff\x00\x00\x00\xff\x00'))+chunk(b'IEND',b''));env['MONO_PATH']=';'.join([str(out),str(E/'Managed'),str(E/'Managed/UnityEngine')]);run('player-pure-execute',[mono,pure,ROOT,png],expected_checks=10)
 except Exception as e:failure=repr(e)
 after={'source':compiler_manifest(),'players':preserved_players(),'originals':originals_manifest(),'sharedBase':shared_base_pins()};unchanged=before==after and all(sha(ROOT/n)==d for n,d in pins.items())
 report={'passed':failure is None and unchanged,'evidenceKind':'pure policies + source contracts + managed real-assembly compile, NOT Unity native execution','failure':failure,'records':records,'recordCount':len(records),'pythonTestCount':sum(counts) if failure is None else None,'csharpCheckCount':sum((6,10)) if failure is None else None,'sourceUnchanged':before['source']==after['source'],'preservationUnchanged':before==after,'effectiveInputsUnchanged':unchanged,'baseline':after};(out/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:report[k] for k in ('passed','recordCount','pythonTestCount','csharpCheckCount','failure')},indent=2));return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())
