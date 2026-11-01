"""Fail-closed structural contracts; managed compilation is run separately, not native evidence."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
STAGED=ROOT/'.artifacts/phase5-build-tools/Assets/VintageCollector'
class NativeAuthoringContracts(unittest.TestCase):
 def test_builder_guards_before_saving_or_building(self):
  p=STAGED/'Editor/Phase5Build.cs';self.assertTrue(p.is_file(),'Owned native builder required');s=p.read_text()
  for token in ('Phase5BuildPolicy.Admit','SaveCurrentModifiedScenesIfUserWantsTo','CreateAssets','CreateBootstrap','BuildWindows','BuildOptions.None','ScriptingImplementation.Mono2x','GraphicsDeviceType.Direct3D11','totalWarnings!=0','totalErrors!=0','BootstrapPhase5.unity'):
   self.assertIn(token,s)
 def test_native_checks_actual_textures_clips_scene_and_execution_order(self):
  p=STAGED/'Editor/Phase5NativeChecks.cs';self.assertTrue(p.is_file(),'Native scene/profile readback required');s=p.read_text()
  for token in ('GetData','GetPixels32','AssetDatabase.GetDependencies','MonoImporter.GetExecutionOrder','-200','-100','-90','rareRevealPosition','pullSounds','phase5Sets','pointerTiltEnabled'):
   self.assertIn(token,s)
 def test_approved_bytes_and_native_textasset_parity(self):
  s=(STAGED/'Editor/Phase5NativeChecks.cs').read_text()
  for token in ('TextAsset.bytes','60fc385e944778008e284c96d0038411238c0b44607b370537c825b9c88a0355','0d58b66ee91099bf6ad47845d62751d523404bd0dc866a62f1897041972bff12','e724f63927dd66a819c02ab4dd14408ccd0affd2bee6d5a8caa7908ed5f69860','eff5082e6f11efbe638d62cfbd314bda1e2fb8d54b5c7534d3a77956cd10353e'):
   self.assertIn(token,s)
 def test_native_texture_roles_match_approved_authoring_bytes(self):
  s=(STAGED/'Editor/Phase5NativeChecks.cs').read_text()
  self.assertTrue('PlaceholderManifest.json' in s and 'authoring-role-native-byte-parity' in s,'Native A/B/C/template role byte witness required')
 def test_native_scene_exercises_real_admission_without_stores(self):
  s=(STAGED/'Editor/Phase5NativeChecks.cs').read_text()
  self.assertTrue('PreparePhase5Configuration' in s and 'native-configuration-no-store-side-effects' in s,'Native full registry/TextAsset admission before store action required')
if __name__=='__main__':unittest.main(verbosity=2)
