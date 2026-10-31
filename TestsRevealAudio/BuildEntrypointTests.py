from pathlib import Path
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
sys.path.insert(0,str(ROOT/'tools'))
class BuildEntrypoints(unittest.TestCase):
    def test_both_mutators_require_stage_before_scene_or_build(self):
        path=ROOT/'Assets/VintageCollector/Editor/RevealAudioBuild.cs'
        self.assertTrue(path.is_file(),'RevealAudioBuild missing')
        text=path.read_text()
        self.assertIn('RevealAudioBuildPolicy.RequireOwnedStage(Path.GetDirectoryName(Application.dataPath),ApprovedRoot)',text)
        for name in ['CreateBootstrap','BuildWindows']:
            self.assertTrue(text.split('public static void '+name+'() {',1)[1].lstrip().startswith('Admission();'))
        self.assertIn('BootstrapTilt.unity',text)
        self.assertIn('VintageCollector.Audio.RevealAudioHost',text)
        self.assertIn('VintageCollector.Verification.RevealAudioSmokeProbe',text)
        self.assertNotIn('EditorBuildSettings.scenes=',text)
    def test_native_check_requires_fourteen_imported_clips(self):
        path=ROOT/'Assets/VintageCollector/Editor/RevealAudioNativeChecks.cs'
        self.assertTrue(path.is_file(),'Imported native profile validation missing')
        text=path.read_text()
        self.assertIn('GetData(',text)
        self.assertIn('clips.Count!=14',text)
        self.assertIn('AssetDatabase.GetAssetPath',text)
    def test_actual_new_scene_closure(self):
        import run_reveal_audio_build as m
        self.assertTrue(m.check_scene()['guidClosure'])
if __name__=='__main__':unittest.main(verbosity=2)
