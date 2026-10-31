from pathlib import Path
import importlib.util
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
sys.path.insert(0,str(ROOT/'tools'))
def verifier():
    path=ROOT/'tools/verify_reveal_audio_native.py'
    if not path.is_file():raise AssertionError('Native audio report gate missing')
    spec=importlib.util.spec_from_file_location('audio_verifier_tests',path);assert spec is not None and spec.loader is not None
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
class PlayerVerifier(unittest.TestCase):
    def test_declared_count_mismatch_is_refused(self):
        v=verifier()
        with self.assertRaisesRegex(ValueError,'count'):
            v.validate_audio_report({'passed':True,'checkCount':2,'checks':[{'passed':True}]},[])
    def test_silent_native_output_is_not_playback_proof(self):
        v=verifier()
        with self.assertRaisesRegex(ValueError,'signal'):
            v.validate_playback({'expectedClip':'a','actualClip':'a','childSourcePlaying':True,'sourcePeak':0,'listenerPeak':0,'samples':1,'frequency':44100})
    def test_wrong_clip_refused_despite_playing_source(self):
        v=verifier()
        with self.assertRaisesRegex(ValueError,'clip'):
            v.validate_playback({'expectedClip':'a','actualClip':'b','childSourcePlaying':True,'sourcePeak':.1,'listenerPeak':.1,'samples':1,'frequency':44100})
    def test_owned_window_input_reused_not_copied(self):
        path=ROOT/'tools/verify_reveal_audio_player.py';self.assertTrue(path.is_file(),'Assignable-audio gameplay driver missing')
        text=path.read_text();self.assertIn('OwnedWindowInput',text);self.assertNotIn('class OwnedWindowInput',text)
        self.assertIn('Phase3RevealAudioWindows',text);self.assertIn('reveal-audio-build',text)
if __name__=='__main__':unittest.main(verbosity=2)
