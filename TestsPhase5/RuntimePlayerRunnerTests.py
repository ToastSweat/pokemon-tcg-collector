"""Synthetic unit fixtures only: these reports are never native evidence."""
from pathlib import Path
import copy,importlib.util,json,sys,unittest
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
class PlayerReportTests(unittest.TestCase):
 def setUp(self):
  path=ROOT/'tools/verify_phase5_player.py';self.assertTrue(path.is_file(),'Phase5 actual-player runner required');spec=importlib.util.spec_from_file_location('phase5player',path);self.m=importlib.util.module_from_spec(spec);spec.loader.exec_module(self.m)
 def report(self):
  s={'generatedResultCount':0,'totalVirtualCards':0,'mediaBound':True,'sessionJson':json.dumps({'schemaVersion':1,'revision':0,'packs':[]})}
  return {'passed':True,'mode':'reopen','checkCount':1,'checks':[{'name':'reload-no-generation','passed':True}], 'initial':s,'final':s,'snapshotCount':2,'snapshots':[s,s],'seedCount':0,'seeds':[],'wrapperCount':0,'wrappers':[], 'screenshotCount':0,'screenshots':[]}
 def test_reconciles_counts_without_native_execution(self):
  self.m.validate_report(self.report(),'reopen')
  for key in ('checkCount','snapshotCount','seedCount','screenshotCount'):
   r=copy.deepcopy(self.report());r[key]=True
   with self.assertRaises(ValueError):self.m.validate_report(r,'reopen')
   r[key]=42
   with self.assertRaises(ValueError):self.m.validate_report(r,'reopen')
 def test_missing_actual_build_is_refused(self):
  with self.assertRaises((ValueError,FileNotFoundError)):self.m.admitted_build('never-built-phase5-unit-fixture')
 def test_duplicate_checks_and_spurious_generation_refused(self):
  r=self.report();r['checks']*=2;r['checkCount']=2
  with self.assertRaises(ValueError):self.m.validate_report(r,'reopen')
  r=self.report();r['final']=dict(r['final'],generatedResultCount=1)
  with self.assertRaises(ValueError):self.m.validate_report(r,'reopen')
 def test_every_native_message_rechecks_live_pid_then_live(self):
  from verify_tilt_player import OwnedWindowInput
  events=[]
  class Process:
   pid=123
   def poll(self):events.append('live');return None
  class Backend:
   def owner(self,h):events.append('pid');return 123
   def post(self,*a):events.append('post');return True
  native=OwnedWindowInput(Process(),Backend(),wait=lambda _:None);native.key(50,13)
  self.assertEqual(['live','pid','live','post']*2,events)
 def test_audio_metadata_alone_cannot_claim_native_playback(self):
  self.assertTrue(hasattr(self.m,'validate_audio'),'Native playback oracle required')
  with self.assertRaises(ValueError):self.m.validate_audio({'rareBranches':['non-pokemon','pokemon','holo'],'checks':[],'playback':[]},['clip'+str(i) for i in range(14)])
 def test_selector_targets_require_pinned_source_and_bounded_rectangles(self):
  self.assertTrue(hasattr(self.m,'validate_selector_targets'),'Independent pinned selector-layout admission required')
  with self.assertRaises(ValueError):self.m.validate_selector_targets({}, {}, (1600,900))
 def test_binder_selector_extends_not_weakens_legacy_current_render_oracle(self):
  import os
  ids=['base-en-unlimited1999','jungle-en-unlimited1999','fossil-en-unlimited1999']
  v=dict(open=True,rows=[],page=0,selectedCardId='',importSummary='',mode='virtual',backups=[],busy=False,restartRequired=False,pendingPhysicalEdit=False,pendingImportConfirm=False,error='',selectedBackupIndex=-1,canSelectSet=True,supportedSets=[{'printingId':p,'label':p} for p in ids])
  names={'mode:virtual','mode:physical','binder-search','page:-1','page:1','backup','import-preview','binder-close','quit'}|{'filter:'+f for f in ['all','owned','missing','duplicates','holo']}
  controls=[dict(name=n,x=1,y=1,width=2,height=2,enabled=n not in {'page:-1','page:1','import-preview'}) for n in names]
  controls += [dict(name='binder-set:'+p,x=1,y=1,width=2,height=2,enabled=True) for p in ids]
  state={'binder':v,'controls':controls,'controlsSource':'binder-repaint','controlsViewJson':json.dumps(v)}
  oracle=self.m.validate_binder_controls if os.environ.get('PHASE5_TEST_LEGACY_BINDER_ORACLE')=='1' else self.m.validate_phase5_binder_controls
  self.assertTrue(oracle(state))
  state['controls']=controls+[dict(controls[-1])]
  with self.assertRaises(ValueError):self.m.validate_phase5_binder_controls(state)
if __name__=='__main__':unittest.main(verbosity=2)
