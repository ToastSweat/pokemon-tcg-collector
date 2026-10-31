"""Run directly: python TestsRevealAudio/BuildAdmissionTests.py. All fixtures project-local."""
from pathlib import Path
import importlib.util
import json
import shutil
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
sys.path.insert(0,str(ROOT/'tools'))

def runner():
    path=ROOT/'tools/run_reveal_audio_build.py'
    if not path.is_file():raise AssertionError('Missing guarded reveal-audio runner; main-project mutation admission is not implemented')
    spec=importlib.util.spec_from_file_location('reveal_build_under_test',path)
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    return module

class BuildAdmission(unittest.TestCase):
    def test_main_project_refused_without_mutation(self):
        m=runner();before=m.source_manifest()
        with self.assertRaisesRegex(ValueError,'owned stage'):
            m.require_owned_stage(ROOT)
        self.assertEqual(before,m.source_manifest())
        self.assertEqual(m.require_owned_stage(m.STAGE),m.STAGE)

    def test_current_profile_component_closure_is_required(self):
        m=runner()
        with self.assertRaisesRegex(ValueError,'RevealAudioHost|RevealAudioProfile|RevealAudioSmokeProbe'):
            m.check_scene(ROOT/'Assets/VintageCollector/Scenes/BootstrapTilt.unity')

    def test_stale_extra_editor_script_refused_before_restage(self):
        m=runner();fixture=ROOT/'.artifacts/reveal-audio-tests/stale-stage-fixture'
        if fixture.exists():shutil.rmtree(fixture)
        stage=fixture/'stage';extra=stage/'Assets/Editor/Stale.cs';extra.parent.mkdir(parents=True)
        extra.write_text('class Stale {}')
        (stage/'.reveal-audio-owned-stage.json').write_text(json.dumps({'purpose':'reveal-audio-exact-source','source':str(fixture)}))
        m.STAGE=stage;m.ROOT=fixture
        try:
            with self.assertRaisesRegex(ValueError,'Unexpected staged Editor'):
                m.stage_exact({},fixture,restage=True)
            self.assertEqual(extra.read_text(),'class Stale {}')
        finally:shutil.rmtree(fixture)

    def test_compiler_closure_rejects_omitted_extra_script_in_stage(self):
        m=runner();fixture=ROOT/'.artifacts/reveal-audio-tests/compiler-fixture'
        if fixture.exists():shutil.rmtree(fixture)
        path=fixture/'Assets/Editor/Surprise.cs';path.parent.mkdir(parents=True);path.write_text('class Surprise {}')
        try:
            with self.assertRaisesRegex(ValueError,'complete compiler'):
                m.verify_compiler_closure({}, {'fileCount':0,'files':[]},fixture)
        finally:shutil.rmtree(fixture)

    def test_compiler_closure_cannot_prune_compilable_assets_named_logs(self):
        m=runner();fixture=ROOT/'.artifacts/reveal-audio-tests/compiler-logs-fixture'
        if fixture.exists():shutil.rmtree(fixture)
        path=fixture/'Assets/Logs/Editor/Surprise.cs';path.parent.mkdir(parents=True);path.write_text('class Surprise {}')
        try:
            with self.assertRaisesRegex(ValueError,'complete compiler'):
                m.verify_compiler_closure({}, {'fileCount':0,'files':[]},fixture)
        finally:shutil.rmtree(fixture)

if __name__=='__main__':unittest.main(verbosity=2)
