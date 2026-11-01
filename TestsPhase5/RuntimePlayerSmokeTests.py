from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
class SmokeContracts(unittest.TestCase):
 def test_opt_in_and_real_flows_and_complete_capture(self):
  p=ROOT/'.artifacts/phase5-build-tools/Assets/VintageCollector/Runtime/Phase5SmokeProbe.cs';self.assertTrue(p.is_file(),'Phase5 native probe required');s=p.read_text()
  for token in ('DefaultExecutionOrder(-90)','phase5-player-fixture','phase5-fixture.json','IsSmokeTest','BaseSaveDirectory','ReadExactFixture','256','binder-set:','set:','backup','import-preview','import-confirm','FailNextCommitForSmoke','GeneratedResultCount','SessionJson','CompletePng','Time.realtimeSinceStartup','ScreenCapture.CaptureScreenshot','GetComponentsInChildren<AudioSource>(true)'):
   self.assertTrue(token in s,token)
  self.assertNotIn('--vcc-phase5',s)
 def test_actual_native_audio_observation_not_only_profile_metadata(self):
  s=(ROOT/'.artifacts/phase5-build-tools/Assets/VintageCollector/Runtime/Phase5SmokeProbe.cs').read_text()
  for token in ('RunAudio','ObservePlayback','GetOutputData','AudioListener.GetOutputData','fourteen-assigned-clips','all-three-rare-categories','skip-no-music-catchup','focus-return-no-replay','selection-keeps-current-music'):
   self.assertTrue(token in s,token)
 def test_opted_fixture_waits_bounded_readiness_not_permanent_early_exit(self):
  s=(ROOT/'.artifacts/phase5-build-tools/Assets/VintageCollector/Runtime/Phase5SmokeProbe.cs').read_text()
  self.assertTrue('readinessDeadline' in s,'bounded startup readiness missing')
  self.assertNotIn('game.Paths==null || !game.Paths.IsSmokeTest)yield break',s)
 def test_selector_witness_uses_actual_view_bound_current_repaint(self):
  s=(ROOT/'.artifacts/phase5-build-tools/Assets/VintageCollector/Runtime/Phase5SmokeProbe.cs').read_text()
  self.assertTrue('ControlObservations' in s and 'selector-repaint' in s and 'selectorControlsObserved' in s,'Use completed selector current-render API, not guessed bounds')
if __name__=='__main__':unittest.main(verbosity=2)
