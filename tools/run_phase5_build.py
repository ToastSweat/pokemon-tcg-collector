"""Phase5 exact-owned native build runner. Audit is read-only; native actions require a parent-frozen receipt.
Never promotes Root assets/metas/scenes. Unity is not invoked by policy/tests or import.
"""
from pathlib import Path
import argparse,json,os,re,subprocess,sys,time
sys.dont_write_bytecode=True
from run_phase5_build_policy import *
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
SCENE='Assets/VintageCollector/Scenes/BootstrapPhase5.unity'
TOOLS=('tools/run_phase5_build.py','tools/run_phase5_build_policy.py')

def count_checked(report):
 checks=report.get('checks',[])
 if report.get('passed') is not True or type(report.get('checkCount')) is not int or report['checkCount']!=len(checks) or not checks or any(c.get('passed') is not True for c in checks) or len({c['name'] for c in checks})!=len(checks):raise ValueError('Native check count/pass/unique-name mismatch')
 if report.get('mediaCount') is not None and (type(report['mediaCount']) is not int or report['mediaCount']!=len(report.get('media',[]))):raise ValueError('Native media row count mismatch')
 return report

def native_command(action,out):
 return [str(EDITOR),'-batchmode','-nographics','-quit','-projectPath',str(STAGE),'-executeMethod','VintageCollector.EditorTools.Phase5Build.'+action,'-logFile',str(out/'unity.log')]

def tsv(pins):return ''.join(d+'\t'+n+'\n' for n,d in sorted(pins.items()))

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--label',required=True);p.add_argument('--action',choices=['audit','CreateAssets','CreateBootstrap','BuildWindows'],default='audit');p.add_argument('--authorization',type=Path);p.add_argument('--parent-go-ahead',action='store_true');p.add_argument('--restage',action='store_true');p.add_argument('--reuse-stage',action='store_true');a=p.parse_args()
 if not re.fullmatch('[A-Za-z0-9_-]+',a.label):p.error('Unsafe label')
 if absolute(ROOT)!=absolute(APPROVED):p.error('Exact canonical Root only')
 candidate_admission();pins=compiler_manifest();protected=preserved_players();base=shared_base_pins();originals=originals_manifest()
 reference_file=ROOT/'.artifacts/phase5-preservation/before.json';no_reparse(reference_file);reference=json.loads(reference_file.read_text(encoding='utf-8'))
 if originals!=reference['originals'] or protected!=reference['players']:p.error('Original frozen preservation baseline differs; never re-pin changed originals/old players')
 receipt=None
 if a.action!='audit':
  if not a.parent_go_ahead or a.authorization is None:p.error('BLOCKED parent explicit source-promotion/go-ahead and frozen authorization receipt missing; Unity not invoked')
  no_reparse(a.authorization)
  if not absolute(a.authorization).is_relative_to(ROOT/'.artifacts'):p.error('Project-local parent receipt required')
  receipt=validate_authorization(json.loads(a.authorization.read_text(encoding='utf-8')),ROOT,a.action);require_assembled_sources(ROOT,a.action)
  # Preflight ALL compiler roots before an evidence directory, ownership marker, or deletion.
  if a.reuse_stage:
   require_regular_tree(require_stage(STAGE));marker=STAGE/MARKER;no_reparse(marker)
   if not marker.is_file() or json.loads(marker.read_text())!={'schemaVersion':1,'owner':'phase5-exact-source','source':str(absolute(ROOT))}:p.error('Foreign stage')
   staged=compiler_manifest(STAGE)
   if staged!=receipt['stagePins']:p.error('Prepared stage differs from exact frozen stage pins')
   if any(Path(n).suffix.casefold() in CODE_SUFFIXES and pins.get(n)!=d for n,d in staged.items()):p.error('Unknown/stale staged compiler-capable code')
  else:stage_preflight(ROOT,pins,a.restage)
 out=ROOT/'.artifacts/phase5-build'/a.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False)
 def save(name,value):(out/name).write_text(json.dumps(value,indent=2),encoding='utf-8')
 tools={n:sha(ROOT/n) for n in TOOLS};save('source-pins.json',pins);save('tool-pins.json',tools);save('protected-before.json',protected);save('shared-base-before.json',base)
 report={'passed':False,'built':False,'action':a.action,'failure':None,'sourcePins':pins,'toolPins':tools,'protectedPlayers':protected,'sharedBasePins':base,'originals':originals}
 try:
  if receipt is not None:
   if not a.reuse_stage:stage_exact(ROOT,pins,a.restage)
   for name in (MARKER,'.phase5-stage-owner.txt','.phase5-authorization.txt','.phase5-root-pins.tsv','.phase5-stage-pins.tsv','.phase5-bindings.json','.phase5-native-meta-allowlist.txt','.phase5-native-reports'):no_reparse(STAGE/name)
   stagepins=compiler_manifest(STAGE);save('staged-inputs.json',stagepins)
   # These noncompiler receipts are written only after canonical and stale-code preflight.
   (STAGE/'.phase5-stage-owner.txt').write_text('phase5-exact-source-v1\n'+ROOT.as_posix()+'\n',encoding='utf-8',newline='')
   (STAGE/'.phase5-authorization.txt').write_text('phase5-parent-authorized-v1\n'+a.action+'\n',encoding='utf-8',newline='')
   (STAGE/'.phase5-root-pins.tsv').write_text(tsv(pins),encoding='utf-8',newline='');(STAGE/'.phase5-stage-pins.tsv').write_text(tsv(stagepins),encoding='utf-8',newline='')
   (STAGE/'.phase5-bindings.json').write_text(json.dumps(receipt['bindings'],separators=(',',':')),encoding='utf-8')
   allows={n+'.meta' for n in receipt['nativeMetaAssets']}|(FOLDER_META_EXCEPTIONS if a.action=='BuildWindows' else PREPARE_FOLDER_METAS)
   (STAGE/'.phase5-native-meta-allowlist.txt').write_text('\n'.join(sorted(allows))+'\n',encoding='utf-8')
   for q in (EDITOR,EDITOR.parent/'Data/Resources/PackageManager/Server/UnityPackageManager.exe',EDITOR.parent/'Data/PlaybackEngines/windowsstandalonesupport'):
    if not q.exists():raise ValueError('Installed native editor prerequisite missing: '+str(q))
   tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1');cmd=native_command(a.action,out);save('command.json',cmd);started=time.time()
   r=subprocess.run(cmd,cwd=STAGE,env=env,timeout=900);report['editorExit']=r.returncode
   if r.returncode!=0:raise ValueError('Actual native Editor exit nonzero: '+str(r.returncode))
   native=STAGE/'.phase5-native-reports'
   check=native/'native-checks.json'
   if not check.is_file() or check.stat().st_mtime<started:raise ValueError('Fresh actual native checks required')
   report['nativeChecks']=count_checked(json.loads(check.read_text(encoding='utf-8')))
   save('native-checks.json',report['nativeChecks']);after=compiler_manifest(STAGE);save('native-stage-pins.json',after)
   if a.action=='BuildWindows':
    buildfile=native/'build-report.json';compiledfile=native/'compiled-inputs.json'
    if any(not f.is_file() or f.stat().st_mtime<started for f in (buildfile,compiledfile)):raise ValueError('Fresh actual build/compiler reports required')
    build=json.loads(buildfile.read_text());compiled=json.loads(compiledfile.read_text());declared={x['path']:x['sha256'] for x in compiled['files']}
    if compiled['fileCount']!=len(compiled['files']) or len(declared)!=len(compiled['files']):raise ValueError('Compiler declared count/duplicate mismatch')
    verify_closure(stagepins,declared,STAGE)
    if build['result']!='Succeeded' or build['errors']!=0 or build['warnings']!=0 or build['development'] is not False or build['target']!='StandaloneWindows64' or build['backend']!='Mono' or build['graphicsApi']!='Direct3D11':raise ValueError('Release Mono D3D11 zero-errors/warnings report required')
    if Path(build['outputPath'])!=CANDIDATE/'VintageCardCollector.exe':raise ValueError('Wrong native output target')
    report['buildReport']=build;report['compilerPins']=declared;report['candidateManifest']=player_manifest(CANDIDATE);report['candidateMemberCount']=len(report['candidateManifest']);report['built']=True
   else:
    # Adoption plan only. Parent explicitly promotes generated native bytes; this runner never overwrites Root.
    report['preparationClosure']=verify_prepare_closure(stagepins,after,STAGE,a.action,receipt['nativeMetaAssets'])
    report['nativeMetaPlan']=native_meta_plan(ROOT,STAGE,pins,receipt['nativeMetaAssets'])
    report['createdNativeFiles']={n:d for n,d in after.items() if n not in stagepins}
   if compiler_manifest()!=pins:raise ValueError('Current Root drifted during native operation')
  report['passed']=True
 except Exception as e:report['failure']=repr(e)
 finally:
  unchanged=originals_manifest()==originals and preserved_players()==protected and shared_base_pins()==base and compiler_manifest()==pins and all(sha(ROOT/n)==d for n,d in tools.items());report['preservationUnchanged']=unchanged;report['passed']=report['passed'] and unchanged;save('results.json',report)
 print(json.dumps({k:report[k] for k in ('passed','built','action','failure')},indent=2));return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())
