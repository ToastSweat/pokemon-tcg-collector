"""New entrypoint safety + actual scene admission, independent of existing builders."""
import importlib.util
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
class BuildEntrypointTests(unittest.TestCase):
    def test_every_mutating_editor_entrypoint_uses_canonical_stage_guard_first(self):
        path=ROOT/'Assets/VintageCollector/Editor/TiltBuild.cs'
        self.assertTrue(path.is_file(),'separate TiltBuild editor entrypoints missing')
        text=path.read_text()
        self.assertIn('TiltBuildPolicy.RequireOwnedStage(Path.GetDirectoryName(Application.dataPath),ApprovedRoot)',text)
        for name in ('CreateBootstrap','BuildWindows'):
            body=text.split('public static void '+name+'() {',1)[1].lstrip()
            self.assertTrue(body.startswith('Admission();'),name+' must admit before mutation')
        self.assertIn('FindProperty("pointerTiltEnabled").boolValue=true',text)
        self.assertIn('VintageCollector.Verification.MaterialsSmokeProbe',text)
        self.assertIn('VintageCollector.Verification.TiltSmokeProbe',text)
        self.assertNotIn('EditorBuildSettings.scenes=',text)
    def test_actual_source_scene_has_unique_real_guid_closure(self):
        spec=importlib.util.spec_from_file_location('tilt_build',ROOT/'tools/run_tilt_build.py')
        assert spec is not None and spec.loader is not None
        m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
        self.assertTrue(m.check_scene()['guidClosure'])
if __name__=='__main__':unittest.main(verbosity=2)
