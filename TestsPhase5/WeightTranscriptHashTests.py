"""Byte-level WEIGHTS-001 regression; extract only finalizer metadata, never its main.

All fixture writes stay under .artifacts/phase5-weight-hash-fix. The checked-in
transcripts are read-only inputs; quote text may use LF, but transcriptSha256
must identify the saved file bytes without newline normalization.
"""
import ast
import hashlib
import json
import os
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'docs/research/phase5/weight-supplement'
HELPER = Path(os.environ.get('WEIGHT_HASH_HELPER', str(SOURCE / 'finalize_evidence.py')))
FIXTURES = ROOT / '.artifacts/phase5-weight-hash-fix/fixtures'


def extracted_metadata(path):
    """Exercise the exact evidence.append expression, with no imports or writes."""
    tree = ast.parse(HELPER.read_text(encoding='utf-8'))
    matches = [node for node in ast.walk(tree) if isinstance(node, ast.Dict)
               and any(isinstance(key, ast.Constant) and key.value == 'transcriptSha256'
                       for key in node.keys)]
    if len(matches) != 1:
        raise AssertionError('expected exactly one transcript metadata expression')
    text = path.read_text(encoding='utf-8')
    return eval(compile(ast.Expression(matches[0]), str(HELPER), 'eval'),
                {'hashlib': hashlib, 'HERE': path.parent, 'filename': path.name,
                 'content': text, 'q': text.rstrip('\n'),
                 'table': {'citationId': 2, 'imageSourceId': 'fixture-image'}})


class WeightTranscriptHashTests(unittest.TestCase):
    def fixture(self, name, payload):
        FIXTURES.mkdir(parents=True, exist_ok=True)
        path = FIXTURES / name
        path.write_bytes(payload)
        return path

    def assert_raw_hash_and_unchanged_bytes(self, path):
        before = path.read_bytes()
        metadata = extracted_metadata(path)
        self.assertEqual(before, path.read_bytes(), 'metadata extraction changed source bytes')
        self.assertEqual(hashlib.sha256(before).hexdigest(), metadata['transcriptSha256'],
                         'transcriptSha256 must hash actual saved bytes, not normalized text')
        return metadata

    def test_lf_saved_bytes(self):
        self.assert_raw_hash_and_unchanged_bytes(
            self.fixture('lf.txt', 'Pokémon table\nH n 14 m 7\n'.encode('utf-8')))

    def test_crlf_saved_bytes(self):
        self.assert_raw_hash_and_unchanged_bytes(
            self.fixture('crlf.txt', 'Pokémon table\r\nH n 14 m 7\r\n'.encode('utf-8')))

    def test_newline_only_input_hash_mismatch_is_detected(self):
        path = self.fixture('input-change.txt', b'table\nH n 14 m 7\n')
        original = self.assert_raw_hash_and_unchanged_bytes(path)['transcriptSha256']
        changed = b'table\r\nH n 14 m 7\r\n'
        path.write_bytes(changed)
        actual = extracted_metadata(path)['transcriptSha256']
        self.assertEqual(changed, path.read_bytes(), 'hashing must preserve changed input bytes')
        self.assertNotEqual(original, actual, 'newline-only byte change must invalidate input hash')
        self.assertEqual(hashlib.sha256(changed).hexdigest(), actual)

    def test_four_saved_transcripts_and_literal_metadata(self):
        evidence = json.loads((SOURCE / 'literal-evidence-quotes.json').read_text(encoding='utf-8'))
        records = [q for q in evidence if 'transcriptSha256' in q]
        self.assertEqual(4, len(records))
        for record in records:
            with self.subTest(transcript=record['textFile']):
                path = SOURCE / record['textFile']
                metadata = self.assert_raw_hash_and_unchanged_bytes(path)
                self.assertEqual(record['transcriptSha256'], metadata['transcriptSha256'])
                self.assertIn(record['quote'], path.read_text(encoding='utf-8'))


if __name__ == '__main__':
    unittest.main(verbosity=2)
