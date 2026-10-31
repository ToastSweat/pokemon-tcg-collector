"""Behavior tests for locally composed PCM, with no third-party packages."""
import array
import hashlib
import importlib.util
import io
import math
import json
import os
import subprocess
from pathlib import Path
import sys
import unittest
import wave

ROOT = Path(__file__).resolve().parents[1]
GENERATOR = ROOT / 'tools/prepare_reveal_audio.py'

def load_generator(test):
    test.assertTrue(GENERATOR.is_file(), 'original reveal-audio generator exists')
    spec = importlib.util.spec_from_file_location('prepare_reveal_audio', GENERATOR)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def decode(test, content):
    with wave.open(io.BytesIO(content), 'rb') as wav:
        test.assertEqual(wav.getnchannels(), 1)
        test.assertEqual(wav.getsampwidth(), 2)
        test.assertEqual(wav.getframerate(), 44100)
        test.assertEqual(wav.getcomptype(), 'NONE')
        frames = wav.getnframes()
        samples = array.array('h', wav.readframes(frames))
        if sys.byteorder != 'little':
            samples.byteswap()
        test.assertEqual(len(samples), frames)
    return [x / 32768.0 for x in samples]

def rms(samples):
    return math.sqrt(sum(x * x for x in samples) / len(samples))

def valid_pcm(test, samples, minimum, maximum):
    duration = len(samples) / 44100
    test.assertGreaterEqual(duration, minimum)
    test.assertLessEqual(duration, maximum)
    test.assertTrue(all(math.isfinite(x) for x in samples))
    test.assertGreater(rms(samples), .025, 'audible signal, not silence')
    test.assertLess(max(abs(x) for x in samples), .8, 'headroom, no hard clipping')
    test.assertEqual(samples[0], 0)
    test.assertEqual(samples[-1], 0)
    test.assertLess(rms(samples[:132]), .025, 'short attack fade')
    test.assertLess(rms(samples[-132:]), .015, 'short release fade')
    test.assertLess(max(abs(b-a) for a,b in zip(samples, samples[1:])), .2, 'no discontinuity clicks')

class AudioProfileSynthesisTests(unittest.TestCase):
    def test_authoring_cli_creates_all_fourteen_without_overwriting_assets_or_metas(self):
        fixture = ROOT / '.artifacts/reveal-audio-profile' / ('authoring-' + str(os.getpid()))
        self.assertFalse(fixture.exists(), 'fresh owned fixture')
        fixture.mkdir(parents=True)
        sentinel = fixture / 'Pull_01.wav.meta'
        sentinel.write_text('untouched-user-meta', encoding='utf-8')
        def run(*flags):
            return subprocess.run([sys.executable, str(GENERATOR), *flags, '--output', str(fixture)], capture_output=True, text=True)
        result = run('--write')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        paths = sorted(fixture.glob('*.wav'))
        self.assertEqual(len(paths), 14, 'CLI generated all playable assets')
        manifest = json.loads((fixture / 'OriginalRevealAudio.manifest.json').read_text(encoding='utf-8'))
        self.assertEqual(len(manifest['clips']), 14)
        self.assertEqual(manifest['provenance'], 'Original locally composed additive synthesis; no supplied recordings or external services.')
        snapshots = {p.name: p.read_bytes() for p in fixture.iterdir()}
        result = run('--check')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        result = run('--write')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(snapshots, {p.name: p.read_bytes() for p in fixture.iterdir()})
        self.assertEqual(sentinel.read_text(encoding='utf-8'), 'untouched-user-meta')
        paths[0].write_bytes(b'artist replacement, do not overwrite')
        changed = {p.name: p.read_bytes() for p in fixture.iterdir()}
        result = run('--write')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('refusing to overwrite', result.stderr)
        self.assertEqual(changed, {p.name: p.read_bytes() for p in fixture.iterdir()})
        result = run('--check')
        self.assertNotEqual(result.returncode, 0)

    def test_three_rare_stingers_are_distinct_musical_escalations(self):
        module = load_generator(self)
        self.assertTrue(callable(getattr(module, 'rare_clips', None)), 'three original rare compositions exist')
        clips = module.rare_clips()
        self.assertEqual(list(clips), ['Rare_NonPokemon.wav', 'Rare_Pokemon.wav', 'Rare_Holo.wav'])
        self.assertEqual(clips, module.rare_clips())
        all_bytes = list(module.position_clips().values()) + list(clips.values())
        self.assertEqual(len({hashlib.sha256(data).digest() for data in all_bytes}), 14)
        durations, peaks = [], []
        for name, data in clips.items():
            with self.subTest(name=name):
                samples = decode(self, data)
                valid_pcm(self, samples, .6, 1.8)
                durations.append(len(samples))
                peaks.append(max(abs(x) for x in samples))
                # A real pitched D major foundation, not noise or an empty WAV container.
                frequency = 440 * 2 ** ((74 - 69) / 12)
                section = samples[882:5292]
                real = sum(x * math.cos(math.tau * frequency * i / 44100) for i,x in enumerate(section))
                imag = sum(x * math.sin(math.tau * frequency * i / 44100) for i,x in enumerate(section))
                self.assertGreater(2 * math.hypot(real, imag) / len(section), .03)
        self.assertTrue(durations[0] < durations[1] < durations[2])
        self.assertTrue(peaks[0] < peaks[1] < peaks[2])

    def test_eleven_original_position_wavs_grow_toward_eight_then_relax(self):
        module = load_generator(self)
        clips = module.position_clips()
        self.assertEqual(list(clips), [f'Pull_{p:02d}.wav' for p in range(1,12)])
        self.assertEqual(clips, module.position_clips(), 'deterministic composition')
        self.assertEqual(len({hashlib.sha256(data).digest() for data in clips.values()}), 11)
        levels = []
        for name, data in clips.items():
            with self.subTest(name=name):
                samples = decode(self, data)
                valid_pcm(self, samples, .18, .65)
                levels.append(rms(samples))
        self.assertTrue(all(a < b for a,b in zip(levels[:7], levels[1:8])), levels)
        self.assertTrue(all(level < levels[6] for level in levels[8:]), levels)

if __name__ == '__main__':
    unittest.main(verbosity=2)
