"""SEC-01/02 regression fixtures. Never starts Unity/player; all children are owned Mono checks."""
import contextlib, hashlib, importlib.util, json, os, pathlib, shutil, stat, subprocess, sys, unittest, uuid
from unittest.mock import patch
from typing import Any
from types import SimpleNamespace
sys.dont_write_bytecode=True
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'.artifacts/phase5-build-boundary-fix'
DATA=pathlib.Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data')
MONO=DATA/'MonoBleedingEdge/bin/mono.exe'
MCS=DATA/'MonoBleedingEdge/lib/mono/4.5/mcs.exe'

def load(name,relative)->Any:
 spec=importlib.util.spec_from_file_location(name,ROOT/relative)
 assert spec is not None and spec.loader is not None
 module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def snapshot(root):
 """Byte+mtime snapshot of whole fixture, checking links before any traversal/read."""
 result={}
 def walk(p):
  a=p.lstat();key=p.relative_to(root).as_posix()
  result[key]={'mode':a.st_mode,'attrs':getattr(a,'st_file_attributes',0),'mtimeNs':a.st_mtime_ns}
  if stat.S_ISLNK(a.st_mode) or getattr(a,'st_file_attributes',0)&0x400:return
  if stat.S_ISDIR(a.st_mode):
   for q in sorted(p.iterdir()):walk(q)
  elif stat.S_ISREG(a.st_mode):result[key]['sha256']=hashlib.sha256(p.read_bytes()).hexdigest()
 walk(root);return result

class BoundaryTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  cls.artifacts=OUT/'runs'/uuid.uuid4().hex;cls.artifacts.mkdir(parents=True)
  cls.exe=cls.artifacts/'BoundaryChecks.exe'
  cmd=[str(MONO),str(MCS),'-warnaserror+','-out:'+str(cls.exe),str(ROOT/'TestsPhase5/RuntimeBuildBoundaryChecks.cs'),str(ROOT/'.artifacts/phase5-build-tools/Assets/VintageCollector/Editor/Phase5BuildPolicy.cs')]
  r=subprocess.run(cmd,capture_output=True,text=True,timeout=60)
  (cls.artifacts/'compile.json').write_text(json.dumps({'command':cmd,'exitCode':r.returncode,'stdout':r.stdout,'stderr':r.stderr},indent=2))
  if r.returncode:raise AssertionError(r.stdout+r.stderr)
 def setUp(self):
  self.p=load('boundary_policy','tools/run_phase5_build_policy.py')
  self.root=self.artifacts/self._testMethodName;self.root.mkdir()
  self.links=[];self.targets=[];self.observations=[]
  for name in ('Assets','Packages','ProjectSettings'):(self.root/name).mkdir()
  (self.root/'Assets/owned.cs').write_bytes(b'class Owned {}')
  self.pins=self.p.compiler_manifest(self.root)
 def tearDown(self):
  for j in reversed(self.links):j.rmdir()
  (self.artifacts/(self._testMethodName+'.json')).write_text(json.dumps(self.observations,indent=2))
 def candidate(self):
  c=self.root/'Builds/Phase5Windows';c.mkdir(parents=True)
  (c/'.phase5-candidate-owner.txt').write_text('phase5-candidate-v1\n'+self.root.as_posix()+'\n',newline='')
  return c
 def stage(self):
  s=self.p.stage_exact(self.root,self.pins)
  (s/'.phase5-stage-owner.txt').write_text('phase5-exact-source-v1\n'+self.root.as_posix()+'\n',newline='')
  return s
 def junction(self,path):
  path.parent.mkdir(parents=True,exist_ok=True)
  target=self.root/('target-'+uuid.uuid4().hex);target.mkdir();(target/'canary').write_bytes(b'owned target must not change')
  cmd=['cmd','/c','mklink','/J',str(path),str(target)]
  r=subprocess.run(cmd,capture_output=True,text=True,timeout=15)
  self.assertEqual(0,r.returncode,r.stdout+r.stderr)
  self.assertTrue(path.lstat().st_file_attributes&0x400)
  self.links.append(path);self.targets.append(target);return target
 def csharp(self,mode,refuse=True,reason='Reparse',locked=None):
  cmd=[str(MONO),str(self.exe),str(self.root),mode,'refuse' if refuse else 'admit']
  if locked is not None:cmd.append(str(locked))
  before=snapshot(self.root);r=subprocess.run(cmd,capture_output=True,text=True,timeout=30);after=snapshot(self.root)
  self.observations.append({'mode':mode,'language':'C#','command':cmd,'exitCode':r.returncode,'stdout':r.stdout,'stderr':r.stderr,'before':before,'after':after,'wholeFixtureUnchanged':before==after})
  self.assertEqual(before,after);self.assertEqual(0,r.returncode,r.stdout+r.stderr);self.assertIn('CHECKS 1',r.stdout)
  self.assertIn('WRITE_CALLS 0 LAUNCH_CALLS 0',r.stdout)
  if refuse:self.assertIn(reason,r.stdout)
 def refuses_without_writes(self,call,reason='Reparse|Non-regular'):
  before=snapshot(self.root)
  target_calls=[]
  with contextlib.ExitStack() as stack:
   for name in ('iterdir','open','lstat'):
    original=getattr(pathlib.Path,name)
    def guarded(path,*args,_original=original,_name=name,**kwargs):
     if any(path.is_relative_to(t) for t in self.targets) or any(path.is_relative_to(j) and (path!=j or _name!='lstat') for j in self.links):
      target_calls.append((_name,str(path)));raise AssertionError('Junction target access before rejection')
     return _original(path,*args,**kwargs)
    stack.enter_context(patch.object(pathlib.Path,name,guarded))
   mocks={}
   for obj,name in ((pathlib.Path,'write_text'),(pathlib.Path,'write_bytes'),(pathlib.Path,'mkdir'),(shutil,'rmtree'),(shutil,'copy2'),(subprocess,'run')):
    mocks[name]=stack.enter_context(patch.object(obj,name,side_effect=AssertionError('Mutation/launch before refusal: '+name)))
   try:
    with self.assertRaisesRegex(ValueError,reason):call()
   finally:
    calls={n:m.call_count for n,m in mocks.items()}
  after=snapshot(self.root)
  self.observations.append({'language':'Python','before':before,'after':after,'wholeFixtureUnchanged':before==after,'mutationAndLaunchCalls':calls,'targetAccessCalls':target_calls})
  self.assertEqual(before,after);self.assertTrue(all(n==0 for n in calls.values()),calls);self.assertEqual([],target_calls)
 def test_candidate_nested_managed_junction_python(self):
  c=self.candidate();self.junction(c/'VintageCardCollector_Data/Managed')
  self.refuses_without_writes(lambda:self.p.stage_exact(self.root,self.pins,restage=True))
 def test_candidate_nested_managed_junction_csharp(self):
  c=self.candidate();self.junction(c/'VintageCardCollector_Data/Managed');self.csharp('candidate')
 def test_candidate_ordinary_tree_positive(self):
  c=self.candidate();(c/'VintageCardCollector_Data/Managed/deep').mkdir(parents=True);(c/'VintageCardCollector_Data/Managed/deep/owned.dll').write_bytes(b'ordinary')
  (c/'Saves').mkdir();before=snapshot(self.root)
  self.assertEqual(c,self.p.candidate_admission(self.root));self.csharp('candidate',False);self.assertEqual(before,snapshot(self.root))

 def test_stage_write_tree_junctions_python(self):
  s=self.stage()
  for relative in ('Library','Temp','Logs','UserSettings','Library/deep/cache','Temp/deep','Logs/deep','UserSettings/deep','Unlisted/deep','Saves/deep'):
   with self.subTest(relative=relative):
    j=s/relative;self.junction(j)
    try:self.refuses_without_writes(lambda:self.p.stage_exact(self.root,self.pins,restage=True))
    finally:j.rmdir();self.links.remove(j)
 def test_stage_write_tree_junctions_csharp(self):
  s=self.stage()
  for relative in ('Library','Temp','Logs','UserSettings','Library/deep/cache','Temp/deep','Logs/deep','UserSettings/deep','Unlisted/deep','Saves/deep'):
   with self.subTest(relative=relative):
    j=s/relative;self.junction(j)
    try:self.csharp('stage')
    finally:j.rmdir();self.links.remove(j)
 def test_stage_ordinary_tree_positive(self):
  s=self.stage()
  for relative in ('Library/cache/deep','Temp/deep','Logs/deep','UserSettings/deep','Unlisted/deep','Saves/deep'):
   p=s/relative;p.mkdir(parents=True);(p/'ordinary.bin').write_bytes(b'cache is not compiler source')
  before=snapshot(self.root)
  self.assertEqual(s,self.p.stage_preflight(self.root,self.pins,restage=True)[0]);self.csharp('stage',False)
  self.assertEqual(before,snapshot(self.root))
  self.assertEqual(s,self.p.stage_exact(self.root,self.pins,restage=True))
  self.assertEqual(self.pins,self.p.compiler_manifest(s))

 def runner(self,reuse):
  # Isolate only unrelated authorization/preservation reads. Boundary policies stay real.
  # This is a mocked-launch unit seam, never an operational authorization bypass.
  sys.path.insert(0,str(ROOT/'tools'))
  try:b=load('boundary_runner','tools/run_phase5_build.py')
  finally:sys.path.pop(0)
  s=self.root/'.artifacts/phase5-build-project'
  reference=self.root/'.artifacts/phase5-preservation/before.json';reference.parent.mkdir(parents=True);reference.write_text('{"originals":{},"players":{}}')
  auth=self.root/'.artifacts/auth.json';auth.write_text('{}')
  b.ROOT=b.APPROVED=self.root;b.STAGE=s;b.CANDIDATE=self.root/'Builds/Phase5Windows'
  b.candidate_admission=lambda:self.p.candidate_admission(self.root)
  b.compiler_manifest=lambda directory=self.root:self.p.compiler_manifest(directory)
  b.require_stage=lambda current:self.p.require_stage(current,self.root)
  b.preserved_players=lambda:{};b.shared_base_pins=lambda:{};b.originals_manifest=lambda:{}
  receipt={'stagePins':self.p.compiler_manifest(s),'bindings':{},'nativeMetaAssets':[]}
  b.validate_authorization=lambda *_:receipt;b.require_assembled_sources=lambda *_:True
  args=['build','--label','refusal','--action','CreateAssets','--authorization',str(auth),'--parent-go-ahead','--reuse-stage' if reuse else '--restage']
  def main():
   with patch.object(sys,'argv',args):return b.main()
  return main
 def test_reuse_stage_runner_rejects_cache_junction_before_evidence(self):
  s=self.stage();main=self.runner(True);self.junction(s/'Library/deep/redirect')
  self.refuses_without_writes(main)
 def test_restage_runner_rejects_cache_junction_before_evidence(self):
  s=self.stage();main=self.runner(False);self.junction(s/'Library/deep/redirect')
  self.refuses_without_writes(main)
 def test_reuse_stage_runner_ordinary_positive_reaches_evidence_boundary(self):
  s=self.stage();(s/'Library/deep').mkdir(parents=True);(s/'Library/deep/cache').write_bytes(b'ordinary')
  main=self.runner(True);before=snapshot(self.root)
  class EvidenceBoundary(Exception):pass
  with patch.object(pathlib.Path,'mkdir',side_effect=EvidenceBoundary) as writes,patch.object(subprocess,'run') as launch:
   with self.assertRaises(EvidenceBoundary):main()
   self.assertEqual(1,writes.call_count);launch.assert_not_called()
  self.assertEqual(before,snapshot(self.root))

 def test_populated_candidate_saves_refuses_without_descendant_stat_or_read(self):
  c=self.candidate();saves=c/'sAvEs';saves.mkdir();hidden=saves/'secret';hidden.mkdir();(hidden/'progress').write_bytes(b'owned synthetic save')
  before=snapshot(self.root)
  with contextlib.ExitStack() as stack:
   for name in ('lstat','stat','open','iterdir'):
    original=getattr(pathlib.Path,name)
    def guarded(path,*args,_original=original,_name=name,**kwargs):
     if path!=saves and path.is_relative_to(saves):raise AssertionError('Save descendant inspected: '+_name)
     return _original(path,*args,**kwargs)
    stack.enter_context(patch.object(pathlib.Path,name,guarded))
   with patch.object(self.p,'require_regular_tree',side_effect=AssertionError('Must refuse Saves before recursive scan')):
    with self.assertRaisesRegex(ValueError,'Populated candidate Saves'):self.p.candidate_admission(self.root)
  self.assertEqual(before,snapshot(self.root));self.csharp('candidate',reason='Populated candidate Saves')
 def test_old_player_saves_logs_junctions_pruned_before_attributes(self):
  player=self.root/'old-player';player.mkdir();(player/'immutable.dll').write_bytes(b'immutable')
  for name in ('sAvEs','LoGs'):self.junction(player/name)
  before=snapshot(self.root)
  with contextlib.ExitStack() as stack:
   for name in ('lstat','stat','iterdir','open'):
    original=getattr(pathlib.Path,name)
    def guarded(path,*args,_original=original,**kwargs):
     if any(path.is_relative_to(j) for j in self.links+self.targets):raise AssertionError('Preservation prune inspected')
     return _original(path,*args,**kwargs)
    stack.enter_context(patch.object(pathlib.Path,name,guarded))
   self.assertEqual({'immutable.dll':hashlib.sha256(b'immutable').hexdigest()},self.p.player_manifest(player))
  self.assertEqual(before,snapshot(self.root))
 def test_stat_only_write_scans_never_open_regular_cache_or_candidate_files(self):
  c=self.candidate();s=self.stage()
  for folder in (c/'VintageCardCollector_Data/Managed/deep',s/'Library/cache/deep',s/'Temp/deep',s/'Logs/deep',s/'UserSettings/deep'):
   folder.mkdir(parents=True);(folder/'ordinary.bin').write_bytes(b'not hashed by boundary scanner')
  before=snapshot(self.root)
  with patch.object(pathlib.Path,'open',side_effect=AssertionError('Boundary scan must be stat only')),patch.object(self.p,'sha',side_effect=AssertionError('No cache hashes')):
   self.assertEqual(c,self.p.require_regular_tree(c));self.assertEqual(s,self.p.require_regular_tree(s))
  self.assertEqual(before,snapshot(self.root))
 def test_nonregular_descendants_refused_by_python_at_candidate_and_stage(self):
  c=self.candidate();s=self.stage();original=pathlib.Path.lstat
  for folder in (c/'VintageCardCollector_Data/Managed',s/'Library/deep'):
   folder.mkdir(parents=True);special=folder/'special';special.write_bytes(b'stat seam, Windows cannot create a POSIX FIFO')
   before=snapshot(self.root)
   def attrs(path,*args,**kwargs):
    return SimpleNamespace(st_mode=stat.S_IFIFO,st_file_attributes=0,st_mtime_ns=original(path).st_mtime_ns) if path==special else original(path,*args,**kwargs)
   with patch.object(pathlib.Path,'lstat',attrs):
    self.refuses_without_writes(lambda:self.p.stage_exact(self.root,self.pins,restage=True))
   self.assertEqual(before,snapshot(self.root))
   special.unlink()

 def test_csharp_admission_never_reads_locked_cache_or_save_content(self):
  c=self.candidate();s=self.stage()
  for mode,folder in (('candidate',c/'VintageCardCollector_Data/Managed'),('stage',s/'Library/deep')):
   folder.mkdir(parents=True);cache=folder/'cache';cache.write_bytes(b'locked against content reads')
   self.csharp(mode,False,locked=cache)
  saves=c/'sAvEs';saves.mkdir();secret=saves/'secret';secret.write_bytes(b'no save reads')
  self.csharp('candidate',reason='Populated candidate Saves',locked=secret)

 def test_nonregular_device_attributes_refused_by_csharp(self):
  # Exact policy logic with only File.GetAttributes redirected to an explicit test shim.
  # Real junction cases above compile unmodified production bytes, not this seam copy.
  text=(ROOT/'.artifacts/phase5-build-tools/Assets/VintageCollector/Editor/Phase5BuildPolicy.cs').read_text()
  copied=self.root/'PolicyAttributeSeam.cs';copied.write_text(text.replace('File.GetAttributes(', 'BoundaryAttributes.GetAttributes('))
  exe=self.root/'AttributeChecks.exe'
  cmd=[str(MONO),str(MCS),'-warnaserror+','-define:BOUNDARY_ATTRIBUTES','-out:'+str(exe),str(ROOT/'TestsPhase5/RuntimeBuildBoundaryChecks.cs'),str(copied)]
  r=subprocess.run(cmd,capture_output=True,text=True,timeout=60)
  self.observations.append({'kind':'test-only-OS-attribute-seam-compile','command':cmd,'exitCode':r.returncode,'stdout':r.stdout,'stderr':r.stderr})
  self.assertEqual(0,r.returncode,r.stdout+r.stderr);self.exe=exe
  c=self.candidate();s=self.stage()
  for mode,folder in (('candidate',c/'VintageCardCollector_Data/Managed'),('stage',s/'Library/deep')):
   folder.mkdir(parents=True);special=folder/'special';special.write_bytes(b'OS attribute seam')
   with patch.dict(os.environ,{'PHASE5_TEST_DEVICE':str(special)}):self.csharp(mode,reason='Non-regular')
 def test_candidate_other_descendants_junctions_never_pruned(self):
  c=self.candidate()
  for relative in ('Logs/deep','Other/deep','VintageCardCollector_Data/Plugins/deep'):
   with self.subTest(relative=relative):
    j=c/relative;self.junction(j)
    try:
     self.refuses_without_writes(lambda:self.p.candidate_admission(self.root));self.csharp('candidate')
    finally:j.rmdir();self.links.remove(j)

if __name__=='__main__':unittest.main(verbosity=2)
