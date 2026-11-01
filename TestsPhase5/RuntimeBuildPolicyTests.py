"""Read-only admission tracer bullets; all fixtures stay in the owned tool workspace."""
import importlib.util, json, pathlib, shutil, sys, unittest, uuid
sys.dont_write_bytecode=True
ROOT=pathlib.Path(__file__).resolve().parents[1]
POLICY=ROOT/'tools/run_phase5_build_policy.py'
class AdmissionTests(unittest.TestCase):
 def setUp(self):
  self.assertTrue(POLICY.is_file(), 'Phase5 pure admission policy must exist')
  spec=importlib.util.spec_from_file_location('phase5policy',POLICY); self.p=importlib.util.module_from_spec(spec);spec.loader.exec_module(self.p)
  self.root=ROOT/'.artifacts/phase5-build-tools/fixtures'/uuid.uuid4().hex;self.root.mkdir(parents=True)
  for folder in ('Assets','Packages','ProjectSettings'):(self.root/folder).mkdir()
 def tearDown(self):
  if hasattr(self,'root'):shutil.rmtree(self.root)
 def test_exact_stage_refuses_root_and_cousin_before_mutation(self):
  stage=self.root/'.artifacts/phase5-build-project'
  self.assertEqual(stage,self.p.require_stage(stage,self.root))
  for bad in (self.root,stage.with_name('phase5-build-project-cousin')):
   with self.assertRaises(ValueError):self.p.require_stage(bad,self.root)
  self.assertTrue(hasattr(self.p,'originals_manifest'))
  self.assertEqual({'Pokemon-Base-Set':{},'Pokemon Cries':{}},self.p.originals_manifest(self.root))
  self.assertFalse(stage.exists())
 def test_parent_authorization_required_before_any_stage_mutation(self):
  self.assertTrue(hasattr(self.p,'validate_authorization'),'Parent-go-ahead receipt admission required')
  with self.assertRaises(ValueError):self.p.validate_authorization({},self.root,'BuildWindows')
 def test_actual_four_reference_folder_exceptions_only(self):
  self.assertEqual({'Assets/Supplied/BaseSetMaterials.meta','Assets/Supplied/BaseSetMaterials/generations.meta','Assets/Supplied/BaseSetMaterials/generations/ac0a72cb65e509f252c73d7692d84c691f9b71718127a5ecf45563db4ab75bbb.meta','Assets/VintageCollector/Collections.meta'},self.p.FOLDER_META_EXCEPTIONS)
 def test_stale_package_code_cannot_be_deleted_and_no_marker_written(self):
  stage=self.root/'.artifacts/phase5-build-project';(stage/'Packages/evil').mkdir(parents=True)
  for suffix in ('.cs','.asmdef','.asmref','.dll','.rsp'):(stage/('Packages/evil/payload'+suffix)).write_bytes(b'foreign')
  before={p.relative_to(stage).as_posix():p.read_bytes() for p in stage.rglob('*') if p.is_file()}
  with self.assertRaises(ValueError):self.p.stage_exact(self.root,{},restage=True)
  after={p.relative_to(stage).as_posix():p.read_bytes() for p in stage.rglob('*') if p.is_file()}
  self.assertEqual(before,after)
 def test_populated_saves_case_insensitive_refused_before_marker(self):
  folder=self.root/'Builds/Phase5Windows/sAvEs';folder.mkdir(parents=True);(folder/'canary').write_bytes(b'progress')
  with self.assertRaises(ValueError):self.p.stage_exact(self.root,{},restage=True)
  self.assertEqual(b'progress',(folder/'canary').read_bytes());self.assertFalse((self.root/'.artifacts/phase5-build-project').exists())
 def test_foreign_stage_marker_refused_without_mutation(self):
  stage=self.root/'.artifacts/phase5-build-project';stage.mkdir(parents=True)
  (stage/self.p.MARKER).write_text('{}')
  with self.assertRaises(ValueError):self.p.stage_exact(self.root,{},restage=True)
  self.assertEqual('{}',(stage/self.p.MARKER).read_text())
 def test_source_logs_and_saves_are_not_compiler_prunes(self):
  for folder in ('Assets/Logs/Editor','Packages/Saves'):
   p=self.root/folder/'hidden.cs';p.parent.mkdir(parents=True);p.write_bytes(b'compiler')
  pins=self.p.compiler_manifest(self.root)
  self.assertIn('Assets/Logs/Editor/hidden.cs',pins);self.assertIn('Packages/Saves/hidden.cs',pins)
  self.p.stage_exact(self.root,pins)
  self.assertEqual(pins,self.p.compiler_manifest(self.root/'.artifacts/phase5-build-project'))
 def test_changed_existing_code_is_not_silently_replaced(self):
  p=self.root/'Assets/owned.cs';p.write_bytes(b'original');pins=self.p.compiler_manifest(self.root);self.p.stage_exact(self.root,pins)
  q=self.root/'.artifacts/phase5-build-project/Assets/owned.cs';q.write_bytes(b'stale executable')
  with self.assertRaises(ValueError):self.p.stage_exact(self.root,pins,restage=True)
  self.assertEqual(b'stale executable',q.read_bytes())
 def test_player_audit_prunes_mutable_names_before_read_or_attrs(self):
  player=self.root/'player';(player/'SaVeS').mkdir(parents=True);(player/'LoGs').mkdir();(player/'immutable.dll').write_bytes(b'player')
  # Junctions here must never be inspected, followed or hashed by preservation.
  (player/'SaVeS/secret').write_bytes(b'do not read')
  self.assertEqual({'immutable.dll':self.p.sha(player/'immutable.dll')},self.p.player_manifest(player))
 def test_compiler_closure_exact_and_four_folder_metas_use_native_bytes(self):
  source=self.p.compiler_manifest(self.root);stage=self.p.stage_exact(self.root,source)
  name='Assets/VintageCollector/Collections.meta';p=stage/name;p.parent.mkdir(parents=True);p.with_suffix('').mkdir();p.write_text('fileFormatVersion: 2\nguid: '+('a'*32)+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
  actual=self.p.compiler_manifest(stage)
  self.assertEqual(actual,self.p.verify_closure(source,actual,stage))
  with self.assertRaises(ValueError):self.p.verify_closure(source,{},stage)
  (stage/'Packages/extra.rsp').parent.mkdir(exist_ok=True);(stage/'Packages/extra.rsp').write_text('-unsafe')
  with self.assertRaises(ValueError):self.p.verify_closure(source,actual,stage)
 def test_native_adoption_never_overwrites_existing_meta(self):
  p=self.root/'Assets/new.cs';p.write_text('class New {}');pins=self.p.compiler_manifest(self.root);stage=self.p.stage_exact(self.root,pins)
  native=stage/'Assets/new.cs.meta';native.write_text('fileFormatVersion: 2\nguid: '+('b'*32)+'\nMonoImporter:\n  externalObjects: {}\n')
  plan=self.p.native_meta_plan(self.root,stage,pins,{'Assets/new.cs'})
  self.assertEqual(self.p.sha(native),plan['Assets/new.cs.meta']['sha256'])
  original=native.read_text();native.write_text(original+'guid: '+('c'*32)+'\n')
  with self.assertRaises(ValueError):self.p.native_meta_plan(self.root,stage,pins,{'Assets/new.cs'})
  native.write_text(original)
  (self.root/'Assets/new.cs.meta').write_text('existing')
  with self.assertRaises(ValueError):self.p.native_meta_plan(self.root,stage,pins,{'Assets/new.cs'})
 def test_windows_junction_refused_and_mutable_player_junction_pruned(self):
  import subprocess
  stage=self.root/'.artifacts/phase5-build-project';stage.parent.mkdir(parents=True);target=self.root/'target';target.mkdir()
  r=subprocess.run(['cmd','/c','mklink','/J',str(stage),str(target)],capture_output=True,text=True)
  self.assertEqual(0,r.returncode,r.stdout+r.stderr)
  try:
   with self.assertRaises(ValueError):self.p.require_stage(stage,self.root)
   player=self.root/'player';player.mkdir();j=player/'sAvEs'
   r=subprocess.run(['cmd','/c','mklink','/J',str(j),str(target)],capture_output=True,text=True);self.assertEqual(0,r.returncode)
   try:self.assertEqual({},self.p.player_manifest(player))
   finally:j.rmdir()
  finally:stage.rmdir()
 def test_foreign_candidate_output_refused_without_write(self):
  output=self.root/'Builds/Phase5Windows';output.mkdir(parents=True);(output/'foreign.exe').write_bytes(b'not ours')
  with self.assertRaises(ValueError):self.p.candidate_admission(self.root)
  self.assertEqual(b'not ours',(output/'foreign.exe').read_bytes());self.assertEqual(['foreign.exe'],[p.name for p in output.iterdir()])
 def test_native_authoring_refuses_any_borrowed_base_mutation(self):
  self.assertTrue(hasattr(self.p,'verify_prepare_closure'),'Native authoring full before/after closure required')
  p=self.root/'Assets/Base.asset';p.write_bytes(b'borrowed');pins=self.p.compiler_manifest(self.root);stage=self.p.stage_exact(self.root,pins);(stage/'Assets/Base.asset').write_bytes(b'changed')
  with self.assertRaises(ValueError):self.p.verify_prepare_closure(pins,self.p.compiler_manifest(stage),stage,'CreateAssets',[])
 def test_native_admission_requires_assembled_phase5_source(self):
  self.assertTrue(hasattr(self.p,'require_assembled_sources'),'No Unity startup on incomplete Root source')
  with self.assertRaises(ValueError):self.p.require_assembled_sources(self.root,'CreateAssets')
if __name__=='__main__':unittest.main(verbosity=2)
