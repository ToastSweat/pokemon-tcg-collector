"""Original reveal cues composed locally from additive oscillators, stdlib only.

No recordings, external services, trademark themes or random/gameplay state are used.
Generation is an authoring operation; the game only loads replaceable Unity AudioClips.
"""
from array import array
import argparse
import hashlib
import json
from pathlib import Path
import io
import math
import sys
import wave

SAMPLE_RATE = 44100


def _wav(notes, duration, peak):
    """Render (start seconds, MIDI pitch, note seconds, relative gain) events."""
    count = round(duration * SAMPLE_RATE)
    samples = [0.0] * count
    for start, midi, span, gain in notes:
        frequency = 440.0 * 2.0 ** ((midi - 69) / 12.0)
        offset = round(start * SAMPLE_RATE)
        length = min(round(span * SAMPLE_RATE), count - offset)
        for i in range(length):
            t = i / SAMPLE_RATE
            phase = math.tau * frequency * t
            attack = min(t / .016, 1.0)
            release = min((length - 1 - i) / (SAMPLE_RATE * .045), 1.0)
            envelope = attack * release * math.exp(-2.2 * t / span)
            voice = (math.sin(phase) + .22 * math.sin(2 * phase)
                     + .10 * math.sin(3 * phase) + .04 * math.sin(4 * phase))
            samples[offset + i] += gain * envelope * voice
    for i in range(count):
        samples[i] *= min(i / (SAMPLE_RATE * .012), 1.0,
                          (count - 1 - i) / (SAMPLE_RATE * .035))
    scale = peak / max(abs(x) for x in samples)
    pcm = array('h', (round(x * scale * 32767) for x in samples))
    if sys.byteorder != 'little':
        pcm.byteswap()
    output = io.BytesIO()
    with wave.open(output, 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())
    return output.getvalue()


def position_clips():
    """Eleven ascending mallet/dyad pulses; 9–11 relax after the rare slot."""
    roots = (62, 64, 66, 69, 71, 74, 76, 78, 74, 69, 62)
    clips = {}
    for position, root in enumerate(roots, 1):
        duration = .22 + .032 * (position - 1) if position <= 8 else .30
        peak = .16 + .032 * (position - 1) if position <= 8 else .20 - .012 * (position - 9)
        notes = [(0, root, duration, 1.0), (.045, root + 7, duration - .045, .28)]
        clips[f'Pull_{position:02d}.wav'] = _wav(notes, duration, peak)
    return clips


def rare_clips():
    """Three independently composed short mallet/chime gestures in D major.

    Non-Pokemon: compact D/A answer. Pokemon: rising D/F#/A/B gesture.
    Holo: wider D/A/B/F#/E/high-D flourish over a luminous sustained dyad.
    """
    return {
        'Rare_NonPokemon.wav': _wav([
            (0, 74, .52, 1.0), (.18, 81, .56, .65),
            (.18, 69, .56, .23)], .74, .43),
        'Rare_Pokemon.wav': _wav([
            (0, 74, .40, 1.0), (.14, 78, .42, .80),
            (.28, 81, .45, .80), (.43, 83, .63, .70),
            (.43, 74, .63, .30)], 1.06, .53),
        'Rare_Holo.wav': _wav([
            (0, 74, .46, 1.0), (.11, 81, .46, .78),
            (.22, 83, .50, .78), (.35, 90, .50, .72),
            (.48, 88, .55, .68), (.64, 86, .90, .56),
            (.48, 74, 1.06, .34), (.48, 78, 1.06, .22),
            (.48, 81, 1.06, .20)], 1.54, .64),
    }


def main(argv=None):
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--write', action='store_true', help='create missing originals only; never replace differing files')
    mode.add_argument('--check', action='store_true', help='verify originals without modifying anything')
    parser.add_argument('--output', type=Path, default=root / 'Assets/VintageCollector/Audio/Clips')
    args = parser.parse_args(argv)
    output = args.output.resolve()
    # No authoring action can target user assets or any path outside the two owned roots.
    allowed = [root / 'Assets/VintageCollector/Audio/Clips', root / '.artifacts/reveal-audio-profile']
    if not any(output == p or output.is_relative_to(p) for p in allowed):
        parser.error('output must stay inside owned reveal-audio source/fixture roots')
    clips = {**position_clips(), **rare_clips()}
    entries = []
    for name, data in clips.items():
        with wave.open(io.BytesIO(data), 'rb') as wav:
            frames = wav.getnframes()
        entries.append({'file': name, 'sha256': hashlib.sha256(data).hexdigest(),
                        'sampleRate': SAMPLE_RATE, 'channels': 1, 'bitsPerSample': 16,
                        'frames': frames, 'durationSeconds': frames / SAMPLE_RATE})
    manifest = {'schema': 'original-reveal-audio-v1',
                'provenance': 'Original locally composed additive synthesis; no supplied recordings or external services.',
                'composition': 'D-major mallet/dyad pulses, rising through slot 8 then relaxing; three original rare gestures.',
                'clips': entries}
    files = {**clips, 'OriginalRevealAudio.manifest.json':
             (json.dumps(manifest, indent=2, ensure_ascii=False) + '\n').encode('utf-8')}
    # Batch preflight: protect artist replacements and every existing .meta before writes.
    for name, data in files.items():
        target = output / name
        if target.exists() and target.read_bytes() != data:
            parser.error('refusing to overwrite differing asset: ' + str(target))
        if args.check and not target.is_file():
            parser.error('missing original asset: ' + str(target))
    if args.write:
        output.mkdir(parents=True, exist_ok=True)
        for name, data in files.items():
            target = output / name
            if not target.exists():
                with target.open('xb') as stream:
                    stream.write(data)
    print(json.dumps({'passed': True, 'mode': 'write' if args.write else 'check',
                      'clips': len(clips), 'output': str(output)}, indent=2))
    return 0


if __name__ == '__main__':
    sys.exit(main())
