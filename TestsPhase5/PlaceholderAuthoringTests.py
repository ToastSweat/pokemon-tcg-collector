"""Root-contained, offline tests of original Phase 5 art authoring."""
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from typing import Any
import zlib

ROOT = Path(__file__).resolve().parents[1]
sys.dont_write_bytecode = True
TOOL = ROOT / 'tools/prepare_phase5_placeholders.py'
art: Any = None
if TOOL.exists():
    spec = importlib.util.spec_from_file_location('phase5_placeholders', TOOL)
    assert spec is not None and spec.loader is not None
    art = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(art)
else:
    art = None


def decode_png(data):
    """Independent strict PNG chunk/CRC/filter oracle, no generator helpers."""
    assert data[:8] == b'\x89PNG\r\n\x1a\n'
    offset, chunks, payload = 8, [], b''
    width = height = 0
    while offset < len(data):
        length = struct.unpack('>I', data[offset:offset + 4])[0]
        kind = data[offset + 4:offset + 8]
        body = data[offset + 8:offset + 8 + length]
        crc = struct.unpack('>I', data[offset + 8 + length:offset + 12 + length])[0]
        assert zlib.crc32(kind + body) & 0xffffffff == crc
        chunks.append(kind)
        if kind == b'IHDR':
            width, height, depth, color, compression, filtering, interlace = struct.unpack('>IIBBBBB', body)
            assert (depth, color, compression, filtering, interlace) == (8, 6, 0, 0, 0)
        if kind == b'IDAT':
            payload += body
        offset += length + 12
        if kind == b'IEND':
            assert length == 0 and offset == len(data)
            break
    assert chunks[0] == b'IHDR' and chunks[-1] == b'IEND'
    assert chunks.count(b'IHDR') == chunks.count(b'IEND') == 1
    raw = zlib.decompress(payload)
    stride = 4 * width + 1
    assert len(raw) == stride * height
    assert all(raw[y * stride] == 0 for y in range(height))
    pixels = b''.join(raw[y * stride + 1:(y + 1) * stride] for y in range(height))
    return width, height, pixels


class PlaceholderCodecTests(unittest.TestCase):
    def test_rgba8_png_roundtrip_is_deterministic_and_crc_valid(self):
        self.assertTrue(art is not None and callable(getattr(art, 'encode_png', None)),
                        'original deterministic RGBA8 encoder is not implemented')
        pixels = bytes([12, 23, 34, 255, 45, 56, 67, 0] * 3)
        data = art.encode_png(2, 3, pixels)
        self.assertEqual(decode_png(data), (2, 3, pixels))
        self.assertEqual(data, art.encode_png(2, 3, pixels))
        for width, height, bad in [(0, 3, b''), (2, -1, b''), (2, 3, pixels[:-1])]:
            with self.assertRaises(ValueError):
                art.encode_png(width, height, bad)


class PlaceholderArtTests(unittest.TestCase):
    def test_eight_original_images_have_exact_dimensions_and_distinct_motif_pixels(self):
        self.assertTrue(callable(getattr(art, 'build_art', None)), 'eight-image art renderer is missing')
        images = art.build_art((260, 475), (600, 825))
        expected = {f'{s}-wrapper-{v}.png' for s in ('jungle', 'fossil') for v in 'ABC'}
        expected |= {f'{s}-card-face-template.png' for s in ('jungle', 'fossil')}
        self.assertEqual(set(images), expected)
        self.assertEqual(len({hashlib.sha256(b).hexdigest() for b in images.values()}), 8)
        decoded = {}
        for name, data in images.items():
            w, h, pixels = decode_png(data)
            self.assertEqual((w, h), (260, 475) if '-wrapper-' in name else (600, 825))
            self.assertTrue(all(pixels[i] == 255 for i in range(3, len(pixels), 4)))
            decoded[name] = pixels
        # Compare motif area only: changing a label is not a distinct wrapper design.
        for set_id in ('jungle', 'fossil'):
            for left, right in [('A', 'B'), ('A', 'C'), ('B', 'C')]:
                a, b = [decoded[f'{set_id}-wrapper-{v}.png'] for v in (left, right)]
                different = sum(a[(y*260+x)*4:(y*260+x+1)*4] != b[(y*260+x)*4:(y*260+x+1)*4]
                                for y in range(140, 350) for x in range(25, 235))
                self.assertGreater(different, 260 * 475 * 0.08)
        self.assertEqual(images, art.build_art((260, 475), (600, 825)))


class PlaceholderPublicationTests(unittest.TestCase):
    def setUp(self):
        fixtures = ROOT / '.artifacts/phase5-placeholders/fixtures'
        fixtures.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=fixtures)
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.sources = self.root / 'Pokemon-Base-Set'
        self.sources.mkdir()
        for name in ('base1-001.png', 'base1-base-set-booster-charizard-unlimited.png'):
            (self.sources / name).write_bytes((ROOT / 'Pokemon-Base-Set' / name).read_bytes())

    def test_prepare_derives_headers_and_publishes_authoring_only_bindings_with_native_contact_sheet(self):
        self.assertTrue(callable(getattr(art, 'prepare', None)), 'authoring publication is missing')
        before = {x.name: x.read_bytes() for x in self.sources.iterdir()}
        manifest = art.prepare(self.root)
        output = self.root / 'Authoring/Phase5/Placeholders'
        self.assertEqual(manifest, json.loads((output / 'PlaceholderManifest.json').read_text('utf-8')))
        self.assertEqual(manifest['status'], 'authoring-only-not-runtime-bound')
        self.assertIs(manifest['runtimeBound'], False)
        self.assertEqual(manifest['supportedPlayableSetsAdded'], [])
        self.assertEqual(len(manifest['assets']), 8)
        self.assertEqual(len({x['bindingId'] for x in manifest['assets']}), 8)
        self.assertEqual(manifest['distinctArtSha256Count'], 8)
        self.assertEqual(manifest['wrapperSha256Count'], 6)
        refs = {x['role']: x for x in manifest['dimensionReferences']}
        self.assertEqual((refs['wrapper-front']['width'], refs['wrapper-front']['height']), (260, 475))
        self.assertEqual((refs['card-face-template']['width'], refs['card-face-template']['height']), (600, 825))
        for row in manifest['assets']:
            path = self.root / row['relativePath']
            data = path.read_bytes()
            w, h, _ = decode_png(data)
            self.assertEqual((row['width'], row['height']), (w, h))
            self.assertEqual(hashlib.sha256(data).hexdigest(), row['sha256'])
            self.assertEqual(row['bindingId'], f"{row['setId']}.{row['role']}.{row['variant']}")
            self.assertNotIn('cardId', row)
            self.assertIs(row['provenance']['sourcePixelsReused'], False)
            self.assertIs(row['provenance']['historicalArtSubjectClaim'], False)
        preview = manifest['previews'][0]
        sheet = (self.root / preview['relativePath']).read_bytes()
        w, h, pixels = decode_png(sheet)
        self.assertEqual((w, h), (1540, 1842))
        self.assertEqual(preview['sha256'], hashlib.sha256(sheet).hexdigest())
        # Native-size crop readback proves previews include original pixels, not resized approximations.
        first = next(x for x in manifest['assets'] if x['bindingId'] == 'jungle.wrapper-front.A')
        _, _, original = decode_png((self.root / first['relativePath']).read_bytes())
        crop = b''.join(pixels[((96+y)*w+32)*4:((96+y)*w+32+260)*4] for y in range(475))
        self.assertEqual(crop, original)
        self.assertEqual(before, {x.name: x.read_bytes() for x in self.sources.iterdir()})
        snapshot = {x.name: (x.read_bytes(), x.stat().st_mtime_ns) for x in output.iterdir()}
        self.assertEqual(art.prepare(self.root), manifest)
        self.assertEqual(snapshot, {x.name: (x.read_bytes(), x.stat().st_mtime_ns) for x in output.iterdir()})
        self.assertFalse((self.root / 'Assets').exists())


    def test_source_change_during_render_refuses_publication(self):
        from unittest.mock import patch
        original_render = art.build_art
        source = self.sources / 'base1-001.png'
        def render_then_change_source(*args):
            images = original_render(*args)
            source.write_bytes(source.read_bytes() + b'changed-during-render')
            return images
        with patch.object(art, 'build_art', side_effect=render_then_change_source):
            with self.assertRaisesRegex(ValueError, 'source.*changed'):
                art.prepare(self.root)
        self.assertFalse((self.root / 'Authoring/Phase5/Placeholders').exists())


    def test_hardlinked_source_is_refused_without_creating_output(self):
        import os
        source = self.sources / 'base1-001.png'
        link = self.root / 'other-owner.png'
        os.link(source, link)
        before = source.read_bytes()
        with self.assertRaisesRegex(ValueError, 'hardlink'):
            art.prepare(self.root)
        self.assertEqual(before, source.read_bytes())
        self.assertFalse((self.root / 'Authoring').exists())


    def test_cli_publishes_real_images_and_machine_readable_summary(self):
        import subprocess
        run = subprocess.run([sys.executable, '-B', str(TOOL), '--root', str(self.root)],
                             capture_output=True, text=True, timeout=30)
        self.assertEqual(run.returncode, 0, run.stderr)
        self.assertTrue((self.root / 'Authoring/Phase5/Placeholders/PlaceholderManifest.json').exists(),
                        'CLI exited without publishing requested actual art')
        result = json.loads(run.stdout)
        self.assertEqual((result['artImages'], result['distinctArtSha256Count'], result['wrapperSha256Count']), (8, 8, 6))
        self.assertEqual(result['runtimeBound'], False)

    def test_foreign_output_is_not_adopted_or_overwritten(self):
        output = self.root / 'Authoring/Phase5/Placeholders'
        output.mkdir(parents=True)
        sentinel = output / 'user-content.txt'
        sentinel.write_bytes(b'not generator owned')
        with self.assertRaisesRegex(ValueError, 'foreign'):
            art.prepare(self.root)
        self.assertEqual(sentinel.read_bytes(), b'not generator owned')
        self.assertEqual(list(output.iterdir()), [sentinel])

    def test_changed_owned_image_is_not_silently_repaired(self):
        art.prepare(self.root)
        output = self.root / 'Authoring/Phase5/Placeholders'
        image = output / 'jungle-wrapper-A.png'
        image.write_bytes(b'user replacement')
        before = {x.name: x.read_bytes() for x in output.iterdir()}
        with self.assertRaisesRegex(ValueError, 'refusing overwrite'):
            art.prepare(self.root)
        self.assertEqual(before, {x.name: x.read_bytes() for x in output.iterdir()})

    def test_foreign_owner_receipt_is_refused(self):
        art.prepare(self.root)
        owner = self.root / 'Authoring/Phase5/Placeholders/Owner.json'
        owner.write_text('{"owner":"someone-else"}', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'refusing overwrite'):
            art.prepare(self.root)
        self.assertEqual(owner.read_text(), '{"owner":"someone-else"}')

    def test_source_hash_change_is_refused_even_with_identical_dimensions(self):
        art.prepare(self.root)
        source = self.sources / 'base1-001.png'
        source.write_bytes(source.read_bytes() + b'new-source-bytes')
        manifest = self.root / 'Authoring/Phase5/Placeholders/PlaceholderManifest.json'
        before = manifest.read_bytes()
        with self.assertRaisesRegex(ValueError, 'source receipt'):
            art.prepare(self.root)
        self.assertEqual(manifest.read_bytes(), before)

    def test_bad_source_ihdr_crc_is_refused(self):
        source = self.sources / 'base1-001.png'
        data = bytearray(source.read_bytes())
        data[29] ^= 1
        source.write_bytes(data)
        with self.assertRaisesRegex(ValueError, 'CRC'):
            art.prepare(self.root)
        self.assertFalse((self.root / 'Authoring').exists())

    def test_changed_aspect_ratio_requires_layout_review_not_resize(self):
        source = self.sources / 'base1-001.png'
        data = bytearray(source.read_bytes())
        data[16:24] = struct.pack('>II', 601, 825)
        data[29:33] = struct.pack('>I', zlib.crc32(data[12:29]) & 0xffffffff)
        source.write_bytes(data)
        with self.assertRaisesRegex(ValueError, 'no automatic resize'):
            art.prepare(self.root)
        self.assertFalse((self.root / 'Authoring').exists())

    def test_hardlinked_output_is_refused(self):
        import os
        art.prepare(self.root)
        image = self.root / 'Authoring/Phase5/Placeholders/jungle-wrapper-A.png'
        os.link(image, self.root / 'other-owner-output.png')
        with self.assertRaisesRegex(ValueError, 'hardlink'):
            art.prepare(self.root)

    def test_out_of_scope_root_is_refused_before_any_lookup_or_write(self):
        with self.assertRaisesRegex(ValueError, 'root scope'):
            art.prepare(ROOT.parent)

    def test_real_source_ancestor_junction_is_refused(self):
        import subprocess
        original = self.root / 'source-original'
        self.sources.rename(original)
        result = subprocess.run(['cmd.exe', '/c', 'mklink', '/J', str(self.sources), str(original)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.addCleanup(self.sources.rmdir)
        with self.assertRaisesRegex(ValueError, 'junction/reparse'):
            art.prepare(self.root)
        self.assertFalse((self.root / 'Authoring').exists())


if __name__ == '__main__':
    unittest.main(verbosity=2)
