"""Real file/PNG tests. Every fixture and log stays in .artifacts/materials-authoring."""
import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile
import unittest
from typing import Any
from unittest.mock import patch
from PIL import Image

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / '.artifacts/materials-authoring'
ART.mkdir(parents=True, exist_ok=True)
CAT = Path('Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json')
INTAKE = Path('docs/verification/Supplied_Base_Set_Intake.json')
DEST = Path('Assets/Supplied/BaseSetMaterials')
TOOL = ROOT / 'tools/prepare_base_set_materials.py'
tool: Any = None
if TOOL.exists():
    spec = importlib.util.spec_from_file_location('materials_authoring', TOOL)
    assert spec is not None and spec.loader is not None
    tool = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(tool)
else:
    tool = None


def sha(data):
    return hashlib.sha256(data).hexdigest()


def dump(value):
    return (json.dumps(value, indent=2, ensure_ascii=False) + '\n').encode('utf-8')


class AuthoringTests(unittest.TestCase):
    def setUp(self):
        self.work = Path(tempfile.mkdtemp(prefix='case-', dir=ART))
        self.addCleanup(lambda: shutil.rmtree(self.work))

    def fixture(self):
        source = self.work / 'Pokemon-Base-Set'
        source.mkdir()
        intake = json.loads((ROOT / INTAKE).read_text(encoding='utf-8'))
        for index, row in enumerate(intake['files']):
            path = self.work / row['relativePath']
            if path.suffix == '.png':
                im = Image.new('P', (row['width'], row['height']), 1)
                im.putpalette([index, 31, 47, 79, index, 113] + [0] * 762)
                im.putpixel((index, index), 0)
                im.save(path, transparency=bytes([40, 255]))
            else:
                path.write_bytes(b'ignored fixture original ' + str(index).encode())
            row['sha256'] = sha(path.read_bytes())
            row['bytes'] = path.stat().st_size
        (self.work / INTAKE).parent.mkdir(parents=True)
        (self.work / INTAKE).write_bytes(dump(intake))
        (self.work / CAT).parent.mkdir(parents=True)
        (self.work / CAT).write_bytes((ROOT / CAT).read_bytes())
        return intake

    def pinned_fixture(self):
        """Synthetic sidecars only; actual native metadata adoption is parent-owned."""
        self.fixture()
        report = tool.prepare(self.work)
        generation = DEST / 'generations' / report['generation']
        assets = [DEST / 'Owner.json', DEST / 'Manifest.json', DEST / 'generations',
                  generation, generation / 'Inventory.json', generation / 'base1-001.png']
        rows = []
        for index, asset in enumerate(assets, 1):
            guid = f'{index:032x}'
            data = f'fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  userData: \n'.encode('utf-8')
            path = self.work / (asset.as_posix() + '.meta')
            path.write_bytes(data)
            rows.append({'path': path.relative_to(self.work).as_posix(), 'sha256': sha(data),
                         'bytes': len(data), 'guid': guid})
        ledger = self.work / 'docs/verification/Base_Set_Materials_Unity_Meta_Pins.json'
        ledger.write_bytes(dump({'schemaVersion': 1, 'originStage': 'synthetic-fixture', 'files': rows}))
        return report, ledger, rows

    def destination_snapshot(self):
        return {p.relative_to(self.work).as_posix(): (sha(p.read_bytes()), p.stat().st_mtime_ns)
                for p in (self.work / DEST).rglob('*') if p.is_file()}

    def test_explicit_pinned_native_metas_coexist_with_idempotent_preparation(self):
        first, ledger, rows = self.pinned_fixture()
        before = self.destination_snapshot()
        ledger_before = ledger.read_bytes(), ledger.stat().st_mtime_ns
        old = self.work / 'Assets/Supplied/AssetManifest.json'
        old.write_bytes((ROOT / 'Assets/Supplied/AssetManifest.json').read_bytes())
        old_before = old.read_bytes(), old.stat().st_mtime_ns
        self.assertEqual(tool.prepare(self.work), first)
        self.assertEqual(tool.prepare(self.work), first)
        self.assertEqual(self.destination_snapshot(), before)
        self.assertEqual((ledger.read_bytes(), ledger.stat().st_mtime_ns), ledger_before)
        self.assertEqual((old.read_bytes(), old.stat().st_mtime_ns), old_before)
        self.assertEqual(len(rows), 6)

    def test_missing_ledger_retains_strict_sidecar_refusal(self):
        _, ledger, _ = self.pinned_fixture()
        ledger.unlink()
        before = self.destination_snapshot()
        with self.assertRaises(tool.PreparationError):
            tool.prepare(self.work)
        self.assertEqual(self.destination_snapshot(), before)

    def test_unknown_and_tampered_sidecars_fail_without_repair(self):
        _, ledger, rows = self.pinned_fixture()
        original_ledger = ledger.read_bytes()
        sidecar = self.work / rows[-1]['path']
        good = sidecar.read_bytes()
        for label in ['unknown', 'tampered', 'missing-pin', 'missing-sidecar']:
            with self.subTest(case=label):
                extra = sidecar.with_name('base1-002.png.meta')
                try:
                    if label == 'unknown':
                        extra.write_bytes(good)
                    elif label == 'tampered':
                        sidecar.write_bytes(good + b'changed')
                    elif label == 'missing-pin':
                        value = json.loads(original_ledger)
                        value['files'].pop()
                        ledger.write_bytes(dump(value))
                    else:
                        sidecar.unlink()
                    before = self.destination_snapshot()
                    with self.assertRaises(tool.PreparationError):
                        tool.prepare(self.work)
                    self.assertEqual(self.destination_snapshot(), before)
                finally:
                    extra.unlink(missing_ok=True)
                    sidecar.write_bytes(good)
                    ledger.write_bytes(original_ledger)

    def test_pins_cannot_authorize_foreign_files_or_unsafe_paths(self):
        report, ledger, rows = self.pinned_fixture()
        baseline = json.loads(ledger.read_bytes())
        generation = DEST / 'generations' / report['generation']
        foreign = self.work / generation / 'foreign.cs'
        foreign.write_bytes(b'foreign sentinel')
        unsafe = ['Assets/Supplied/BaseSetMaterials/../escape.png.meta',
                  'Assets/Supplied/BaseSetMaterials/generations/../Owner.json.meta',
                  rows[-1]['path'].replace('/', '\\'), '/' + rows[-1]['path'],
                  'C:/' + rows[-1]['path'], '//server/share/a.meta',
                  (generation / 'foreign.cs.meta').as_posix(),
                  (generation / 'foreign.yaml.meta').as_posix(),
                  (generation / 'base1-001.png.meta.meta').as_posix(),
                  (generation / 'foreign.png.meta').as_posix(),
                  (DEST / 'Owner.json').as_posix(),
                  'Assets/Supplied/BaseSetMaterials.meta']
        # No unlisted metas may mask the actual path-grammar rejection.
        for row in rows:
            (self.work / row['path']).unlink()
        before = self.destination_snapshot()
        for path in unsafe:
            with self.subTest(path=path):
                value = copy.deepcopy(baseline)
                value['files'] = [dict(rows[-1], path=path)]
                ledger.write_bytes(dump(value))
                with self.assertRaisesRegex(tool.PreparationError, 'metadata pin (path|is not an exact owned sidecar)'):
                    tool.prepare(self.work)
                self.assertEqual(self.destination_snapshot(), before)
        self.assertEqual(foreign.read_bytes(), b'foreign sentinel')

    def test_pinned_metas_do_not_weaken_asset_or_receipt_validation(self):
        report, ledger, rows = self.pinned_fixture()
        generation = self.work / DEST / 'generations' / report['generation']
        for asset in [generation / 'base1-001.png', generation / 'Inventory.json',
                      self.work / DEST / 'Owner.json', self.work / DEST / 'Manifest.json']:
            good = asset.read_bytes()
            for label in ['missing', 'directory', 'mutated']:
                with self.subTest(asset=asset.name, case=label):
                    try:
                        asset.unlink()
                        if label == 'directory':
                            asset.mkdir()
                        elif label == 'mutated':
                            asset.write_bytes(good + b'changed')
                        before = self.destination_snapshot()
                        with self.assertRaises(tool.PreparationError):
                            tool.prepare(self.work)
                        self.assertEqual(self.destination_snapshot(), before)
                    finally:
                        if asset.is_dir():
                            asset.rmdir()
                        asset.write_bytes(good)

    def test_invalid_meta_ledgers_fail_even_without_sidecars(self):
        _, ledger, rows = self.pinned_fixture()
        baseline = json.loads(ledger.read_bytes())
        for row in rows:
            (self.work / row['path']).unlink()
        def field(key, value):
            result = copy.deepcopy(baseline)
            result[key] = value
            return result
        values = [None, [], {}, field('schemaVersion', True), field('schemaVersion', 2),
                  field('originStage', ''), field('originStage', 'bad\norigin'),
                  field('originStage', 1), field('originStage', 'x' * 257),
                  field('files', {}), field('files', [None]), field('foreign', True),
                  field('files', [rows[0]] * 4097)]
        for key, bad in [('sha256', 'A' * 64), ('sha256', '0' * 63),
                         ('bytes', True), ('bytes', 0), ('bytes', 65537),
                         ('guid', 'A' * 32), ('guid', '0' * 32), ('guid', 'not-a-guid'),
                         ('path', 123), ('extra', 'foreign')]:
            value = copy.deepcopy(baseline)
            value['files'] = [dict(rows[0], **{key: bad})]
            values.append(value)
        raw = dump(baseline)
        invalid = [dump(v) for v in values] + [b'\xef\xbb\xbf' + raw, b'\xff', b'{broken',
                   b'{"schemaVersion":1,"schemaVersion":1}', b'{"n":NaN}', b' ' * (1024 * 1024 + 1)]
        before = self.destination_snapshot()
        for index, data in enumerate(invalid):
            with self.subTest(case=index):
                ledger.write_bytes(data)
                with self.assertRaises(tool.PreparationError):
                    tool.prepare(self.work)
                self.assertEqual(self.destination_snapshot(), before)
                self.assertEqual(ledger.read_bytes(), data)

    def test_meta_guid_content_and_exact_pins_are_validated(self):
        _, ledger, rows = self.pinned_fixture()
        baseline = json.loads(ledger.read_bytes())
        sidecar = self.work / rows[-1]['path']
        good = sidecar.read_bytes()
        guid = rows[-1]['guid'].encode()
        malformed = [good.replace(guid, b'g' * 32), good.replace(guid, b'a' * 31),
                     good.replace(guid, b'a' * 32), good + b'guid: ' + guid + b'\n',
                     good.replace(b'guid:', b' guid:'), b'\xef\xbb\xbf' + good,
                     good + b'\xff', good + b'\x00', good.replace(b'fileFormatVersion: 2', b'fileFormatVersion: 1')]
        for data in malformed:
            with self.subTest(data=data[:70]):
                value = copy.deepcopy(baseline)
                value['files'][-1].update(bytes=len(data), sha256=sha(data))
                ledger.write_bytes(dump(value))
                sidecar.write_bytes(data)
                before = self.destination_snapshot()
                with self.assertRaises(tool.PreparationError):
                    tool.prepare(self.work)
                self.assertEqual(self.destination_snapshot(), before)
        sidecar.write_bytes(good)
        for key, bad in [('bytes', len(good) + 1), ('sha256', '0' * 64)]:
            value = copy.deepcopy(baseline)
            value['files'][-1][key] = bad
            ledger.write_bytes(dump(value))
            with self.assertRaises(tool.PreparationError):
                tool.prepare(self.work)
        for duplicated in [baseline['files'] + [baseline['files'][0]],
                           [dict(row, guid=rows[0]['guid']) for row in rows]]:
            ledger.write_bytes(dump(dict(baseline, files=duplicated)))
            with self.assertRaises(tool.PreparationError):
                tool.prepare(self.work)

    def test_metadata_hardlinks_are_rejected_before_leaf_read(self):
        _, ledger, rows = self.pinned_fixture()
        for target in [ledger, self.work / rows[-1]['path']]:
            with self.subTest(path=target.name):
                link = self.work / 'metadata-hardlink'
                os.link(target, link)
                actual_open = Path.open
                reads = []
                def guarded_open(path, *args, **kwargs):
                    if path == target:
                        reads.append(path)
                    return actual_open(path, *args, **kwargs)
                try:
                    with patch.object(Path, 'open', guarded_open):
                        with self.assertRaises(tool.PreparationError):
                            tool.prepare(self.work)
                    self.assertEqual(reads, [], 'linked leaf must never be opened')
                    self.assertEqual(target.read_bytes(), link.read_bytes())
                finally:
                    link.unlink()

    def test_metadata_symlinks_are_rejected_before_leaf_read(self):
        _, ledger, rows = self.pinned_fixture()
        for target in [ledger, self.work / rows[-1]['path']]:
            with self.subTest(path=target.name):
                saved = self.work / 'metadata-symlink-target'
                target.rename(saved)
                try:
                    try:
                        os.symlink(saved, target)
                    except OSError as error:
                        self.skipTest('file symlink privilege unavailable: ' + str(error))
                    actual_open = Path.open
                    reads = []
                    def guarded_open(path, *args, **kwargs):
                        if path == target:
                            reads.append(path)
                        return actual_open(path, *args, **kwargs)
                    with patch.object(Path, 'open', guarded_open):
                        with self.assertRaises(tool.PreparationError):
                            tool.prepare(self.work)
                    self.assertEqual(reads, [], 'symlink leaf must never be opened')
                finally:
                    if target.is_symlink():
                        target.unlink()
                    saved.rename(target)

    def test_ledger_ancestor_junction_is_rejected_before_leaf_read(self):
        _, ledger, _ = self.pinned_fixture()
        parent = ledger.parent
        saved = self.work / 'verification-junction-target'
        parent.rename(saved)
        result = subprocess.run(['cmd.exe', '/c', 'mklink', '/J', str(parent), str(saved)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        try:
            actual_open = Path.open
            reads = []
            def guarded_open(path, *args, **kwargs):
                if path == ledger:
                    reads.append(path)
                return actual_open(path, *args, **kwargs)
            with patch.object(Path, 'open', guarded_open):
                with self.assertRaises(tool.PreparationError):
                    tool.prepare(self.work)
            self.assertEqual(reads, [])
        finally:
            parent.rmdir()
            saved.rename(parent)

    def test_deeply_nested_meta_ledger_is_a_controlled_refusal(self):
        self.fixture()
        ledger = self.work / 'docs/verification/Base_Set_Materials_Unity_Meta_Pins.json'
        data = b'[' * 2000 + b'0' + b']' * 2000
        ledger.write_bytes(data)
        with self.assertRaises(tool.PreparationError):
            tool.prepare(self.work)
        self.assertEqual(ledger.read_bytes(), data)
        self.assertFalse((self.work / DEST).exists())

    def test_complete_preparation_identity_roles_pixels_and_private_schema(self):
        intake = self.fixture()
        self.assertTrue(callable(getattr(tool, 'prepare', None)), 'complete preparation API missing')
        report = tool.prepare(self.work)
        manifest_path = self.work / DEST / 'Manifest.json'
        raw = manifest_path.read_bytes()
        self.assertFalse(raw.startswith(b'\xef\xbb\xbf'))
        manifest = json.loads(raw)
        self.assertEqual(set(manifest), {'schemaVersion', 'status', 'intendedUse', 'redistributionPermission', 'rightsDefault', 'notes', 'assets'})
        self.assertEqual(manifest['schemaVersion'], 1)
        self.assertEqual(manifest['status'], 'user-supplied-private-materials')
        self.assertEqual(manifest['rightsDefault'], 'unknown')
        self.assertEqual(manifest['redistributionPermission'], 'not-established')
        cards = json.loads((ROOT / CAT).read_text())['cards']
        entries = manifest['assets']
        self.assertTrue(all(x['relativePath'].startswith('Assets/Supplied/BaseSetMaterials/') for x in entries), 'runtime MediaFiles.Resolve requires project-relative Assets/Supplied paths')
        faces = [x for x in entries if x['role'] == 'card-face']
        masks = [x for x in entries if x['role'] == 'holo-mask']
        self.assertEqual([x['cardId'] for x in faces], [x['id'] for x in cards])
        self.assertEqual([x['cardId'] for x in masks], [x['id'] for x in cards if x['finish'] == 'holo'])
        self.assertEqual((len(faces), len(masks), len(entries)), (101, 15, 121))
        self.assertFalse(any('-008-' in x.get('cardId', '') for x in entries))
        self.assertEqual(len({x['relativePath'] for x in masks}), 1)
        self.assertEqual(len({(x['role'], x.get('cardId', '')) for x in entries}), len(entries))
        self.assertEqual({x['role'] for x in entries} - {'card-face', 'holo-mask'}, {'card-back', 'wrapper-front', 'wrapper-back', 'wrapper-front-mask', 'wrapper-back-mask'})
        front = next(x for x in entries if x['role'] == 'wrapper-front')
        self.assertTrue(front['relativePath'].endswith('base1-base-set-booster-charizard-unlimited.png'))
        for entry in entries:
            self.assertEqual(set(entry), {'role', 'cardId', 'relativePath', 'source', 'rights', 'restrictions', 'sha256'})
            self.assertEqual(entry['rights'], 'unknown')
            self.assertIn('no redistribution', entry['restrictions'])
            self.assertRegex(entry['sha256'], r'^[0-9a-f]{64}$')
            self.assertEqual(entry['sha256'], sha((self.work / entry['relativePath']).read_bytes()))
        self.assertTrue(all('user-approved' in x['source'] and 'first-edition' in x['source'] and 'Unlimited' in x['source'] for x in faces))
        generation = (self.work / front['relativePath']).parent
        self.assertEqual(len(list(generation.glob('*.png'))), 110)
        for row in intake['files']:
            original = self.work / row['relativePath']
            self.assertEqual(sha(original.read_bytes()), row['sha256'])
            if original.suffix != '.png':
                continue
            derived = generation / original.name
            data = derived.read_bytes()
            self.assertEqual(struct.unpack('>IIBBBBB', data[16:29]), (row['width'], row['height'], 8, 6, 0, 0, 0))
            with Image.open(original) as a, Image.open(derived) as b:
                a.load(); b.load()
                self.assertEqual(a.convert('RGBA').tobytes(), b.convert('RGBA').tobytes(), original.name)
        self.assertEqual(report['bindingCounts'], {'card-face': 101, 'holo-mask': 15, 'card-back': 1, 'wrapper-front': 1, 'wrapper-back': 1, 'wrapper-front-mask': 1, 'wrapper-back-mask': 1})
        self.assertEqual(report['uniqueRuntimeImageCount'], 107)
        self.assertEqual(report['runtimeTexturePixels'], 51479000)
        self.assertEqual(report['derivedImageCount'], 110)

    def test_runtime_accounting_deduplicates_content_across_different_filenames(self):
        intake = self.fixture()
        first = self.work / 'Pokemon-Base-Set/base1-001.png'
        second = self.work / 'Pokemon-Base-Set/base1-002.png'
        second.write_bytes(first.read_bytes())
        row = next(x for x in intake['files'] if x['relativePath'].endswith('/base1-002.png'))
        row['bytes'], row['sha256'] = second.stat().st_size, sha(second.read_bytes())
        (self.work / INTAKE).write_bytes(dump(intake))
        report = tool.prepare(self.work)
        manifest = json.loads((self.work / DEST / 'Manifest.json').read_bytes())
        resources = set()
        for entry in manifest['assets']:
            with Image.open(self.work / entry['relativePath']) as image:
                resources.add((entry['sha256'], entry['role'] in ('holo-mask', 'wrapper-front-mask', 'wrapper-back-mask'), image.width, image.height))
        pixels = sum(width * height for _, _, width, height in resources)
        self.assertEqual(len(resources), 106)
        self.assertEqual(report['uniqueRuntimeImageCount'], len(resources))
        self.assertEqual(report['runtimeTexturePixels'], pixels)
        self.assertEqual(report['runtimeRgbaBytes'], pixels * 4)
        self.assertEqual(report['bindingCount'], 121)

    def test_preflight_rejects_invalid_identity_inventory_and_path_without_manifest(self):
        baseline = self.fixture()
        catalog = json.loads((self.work / CAT).read_text())
        mutations = {
            'duplicate-source': lambda i, c: i['files'].__setitem__(1, copy.deepcopy(i['files'][0])),
            'missing-source-row': lambda i, c: i['files'].pop(),
            'traversal': lambda i, c: i['files'][0].__setitem__('relativePath', 'Pokemon-Base-Set/../outside.png'),
            'backslash': lambda i, c: i['files'][0].__setitem__('relativePath', i['files'][0]['relativePath'].replace('/', chr(92))),
            'absolute': lambda i, c: i['files'][0].__setitem__('relativePath', 'C:/outside.png'),
            'upper-hash': lambda i, c: i['files'][0].__setitem__('sha256', i['files'][0]['sha256'].upper()),
            'wrong-dimension': lambda i, c: next(x for x in i['files'] if x['relativePath'].endswith('base1-001.png')).__setitem__('width', 599),
            'wrong-count': lambda i, c: i.__setitem__('pngCount', 109),
            'duplicate-card': lambda i, c: c['cards'].__setitem__(1, copy.deepcopy(c['cards'][0])),
            'missing-card': lambda i, c: c['cards'].pop(),
            'wrong-variant': lambda i, c: c['cards'][0].__setitem__('variant', 'yellow-cheeks'),
            'wrong-id': lambda i, c: c['cards'][0].__setitem__('id', c['cards'][0]['id'] + '-bad'),
            'wrong-name': lambda i, c: c['cards'][0].__setitem__('displayName', 'Invented'),
            'nonholo-mask': lambda i, c: c['cards'][17].__setitem__('finish', 'holo'),
            'machamp': lambda i, c: c['cards'][7].__setitem__('collectorNumber', '8'),
        }
        for label, mutate in mutations.items():
            with self.subTest(case=label):
                i, c = copy.deepcopy(baseline), copy.deepcopy(catalog)
                mutate(i, c)
                (self.work / INTAKE).write_bytes(dump(i))
                (self.work / CAT).write_bytes(dump(c))
                try:
                    with self.assertRaises(tool.PreparationError):
                        tool.prepare(self.work)
                    self.assertFalse((self.work / DEST / 'Manifest.json').exists())
                finally:
                    if (self.work / DEST).exists():
                        shutil.rmtree(self.work / DEST)

    def test_bad_pngs_and_source_pin_changes_fail_without_manifest(self):
        baseline = self.fixture()
        row = next(x for x in baseline['files'] if x['relativePath'].endswith('base1-001.png'))
        path = self.work / row['relativePath']
        good = path.read_bytes()
        for label, data, repin in [('truncated', good[:-20], True), ('malformed', b'not a PNG', True), ('changed-source', good + b'changed', False)]:
            with self.subTest(case=label):
                intake = copy.deepcopy(baseline)
                current = next(x for x in intake['files'] if x['relativePath'] == row['relativePath'])
                path.write_bytes(data)
                if repin:
                    current['sha256'], current['bytes'] = sha(data), len(data)
                (self.work / INTAKE).write_bytes(dump(intake))
                with self.assertRaises(tool.PreparationError):
                    tool.prepare(self.work)
                self.assertFalse((self.work / DEST / 'Manifest.json').exists())
        path.write_bytes(good)

    def test_idempotence_and_semantic_input_order_determinism(self):
        self.fixture()
        first = tool.prepare(self.work)
        def snapshot():
            return {str(p.relative_to(self.work / DEST)): (sha(p.read_bytes()), p.stat().st_mtime_ns) for p in (self.work / DEST).rglob('*') if p.is_file()}
        before = snapshot()
        second = tool.prepare(self.work)
        self.assertEqual(first, second)
        self.assertEqual(before, snapshot(), 'idempotent run must not rewrite files')
        intake = json.loads((self.work / INTAKE).read_text())
        intake['files'].reverse()
        (self.work / INTAKE).write_bytes(dump(intake))
        catalog = json.loads((self.work / CAT).read_text())
        catalog['cards'].reverse()
        (self.work / CAT).write_bytes(dump(catalog))
        third = tool.prepare(self.work)
        self.assertEqual(first['manifestSha256'], third['manifestSha256'])
        self.assertEqual(first['generation'], third['generation'])
        self.assertEqual(before, snapshot())

    def test_foreign_destination_refusal_without_modification(self):
        self.fixture()
        dest = self.work / DEST
        dest.mkdir(parents=True)
        foreign = dest / 'Manifest.json'
        foreign.write_bytes(b'user-owned do not replace')
        with self.assertRaises(tool.PreparationError):
            tool.prepare(self.work)
        self.assertEqual(foreign.read_bytes(), b'user-owned do not replace')
        self.assertEqual(sorted(x.name for x in dest.iterdir()), ['Manifest.json'])

    def test_manifest_last_fault_preserves_previous_generation(self):
        intake = self.fixture()
        first = tool.prepare(self.work)
        manifest = self.work / DEST / 'Manifest.json'
        before = manifest.read_bytes()
        row = next(x for x in intake['files'] if x['relativePath'].endswith('base1-001.png'))
        source = self.work / row['relativePath']
        with Image.open(source) as image:
            altered = image.convert('RGBA')
        altered.putpixel((50, 60), (7, 9, 11, 13))
        altered.save(source)
        row['bytes'], row['sha256'] = source.stat().st_size, sha(source.read_bytes())
        (self.work / INTAKE).write_bytes(dump(intake))
        invoked = []
        def refuse_replace(src, dst, expected=None):
            self.assertEqual(Path(dst), manifest)
            new = json.loads(Path(src).read_bytes())
            for entry in new['assets']:
                p = self.work / entry['relativePath']
                self.assertTrue(p.is_file(), 'all generation files must precede manifest')
                self.assertEqual(sha(p.read_bytes()), entry['sha256'])
            invoked.append(True)
            raise OSError('injected last-manifest publication failure')
        with patch.object(tool, 'publish_manifest', side_effect=refuse_replace):
            with self.assertRaises(OSError):
                tool.prepare(self.work)
        self.assertEqual(invoked, [True])
        self.assertEqual(manifest.read_bytes(), before)
        self.assertTrue((self.work / DEST / 'generations' / first['generation']).is_dir())
        recovered = tool.prepare(self.work)
        self.assertNotEqual(first['manifestSha256'], recovered['manifestSha256'])

    def test_first_publication_failure_never_exposes_half_manifest_and_can_retry(self):
        self.fixture()
        with patch.object(tool, 'publish_manifest', side_effect=OSError('injected first manifest failure')):
            with self.assertRaises(OSError):
                tool.prepare(self.work)
        self.assertFalse((self.work / DEST / 'Manifest.json').exists())
        report = tool.prepare(self.work)
        self.assertEqual(report['bindingCount'], 121)

    def test_png_trailing_data_and_animation_are_rejected(self):
        im = Image.new('RGBA', (600, 825), (13, 17, 19, 23))
        buf = io.BytesIO(); im.save(buf, format='PNG')
        with self.assertRaises(tool.PreparationError):
            tool.normalize_png(buf.getvalue() + b'foreign trailing payload', (600, 825))
        second = Image.new('RGBA', (600, 825), (31, 37, 41, 43))
        animation = io.BytesIO()
        im.save(animation, format='PNG', save_all=True, append_images=[second], duration=50)
        with self.assertRaises(tool.PreparationError):
            tool.normalize_png(animation.getvalue(), (600, 825))

    def test_destination_derived_tampering_is_refused_not_repaired(self):
        self.fixture()
        report = tool.prepare(self.work)
        manifest = (self.work / DEST / 'Manifest.json').read_bytes()
        derived = self.work / DEST / 'generations' / report['generation'] / 'base1-001.png'
        changed = derived.read_bytes() + b'foreign modification'
        derived.write_bytes(changed)
        with self.assertRaises(tool.PreparationError):
            tool.prepare(self.work)
        self.assertEqual(derived.read_bytes(), changed)
        self.assertEqual((self.work / DEST / 'Manifest.json').read_bytes(), manifest)

    def test_source_change_during_encoding_rejected_before_publication(self):
        self.fixture()
        original = tool.normalize_png
        changed = []
        def mutate_after_decode(data, dimensions):
            result = original(data, dimensions)
            if not changed:
                path = self.work / 'Pokemon-Base-Set/base1-001.png'
                path.write_bytes(path.read_bytes() + b'late source modification')
                changed.append(True)
            return result
        with patch.object(tool, 'normalize_png', side_effect=mutate_after_decode):
            with self.assertRaises(tool.PreparationError):
                tool.prepare(self.work)
        self.assertFalse((self.work / DEST / 'Manifest.json').exists())

    def test_concurrent_authoring_lock_refusal_never_removes_foreign_lock(self):
        self.fixture()
        lock = self.work / '.artifacts/materials-authoring/prepare.lock'
        lock.parent.mkdir(parents=True)
        lock.write_bytes(b'foreign active authoring operation')
        with self.assertRaises(tool.PreparationError):
            tool.prepare(self.work)
        self.assertEqual(lock.read_bytes(), b'foreign active authoring operation')
        self.assertFalse((self.work / DEST).exists())

    def test_late_foreign_manifest_collision_is_not_overwritten(self):
        self.fixture()
        rename = tool.os.rename
        sentinel = b'late foreign manifest'
        manifest = self.work / DEST / 'Manifest.json'
        def collide(src, dst):
            rename(src, dst)
            manifest.write_bytes(sentinel)
        with patch.object(tool.os, 'rename', side_effect=collide):
            with self.assertRaises(tool.PreparationError):
                tool.prepare(self.work)
        self.assertEqual(manifest.read_bytes(), sentinel)

    def test_readonly_sources_and_old_production_manifest_remain_byte_exact(self):
        intake = self.fixture()
        old = self.work / 'Assets/Supplied/AssetManifest.json'
        old.parent.mkdir(parents=True, exist_ok=True)
        old.write_bytes((ROOT / 'Assets/Supplied/AssetManifest.json').read_bytes())
        before = old.read_bytes()
        sources = [self.work / row['relativePath'] for row in intake['files']]
        for source in sources:
            source.chmod(0o444)
        try:
            tool.prepare(self.work)
            self.assertEqual(old.read_bytes(), before)
            for row, source in zip(intake['files'], sources):
                self.assertEqual(sha(source.read_bytes()), row['sha256'])
        finally:
            for source in sources:
                source.chmod(0o666)

    def test_real_junction_and_hardlink_paths_are_refused(self):
        self.fixture()
        source = self.work / 'Pokemon-Base-Set'
        renamed = self.work / 'original-source'
        source.rename(renamed)
        result = subprocess.run(['cmd.exe', '/c', 'mklink', '/J', str(source), str(renamed)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        try:
            with self.assertRaises(tool.PreparationError):
                tool.prepare(self.work)
            self.assertFalse((self.work / DEST).exists())
        finally:
            source.rmdir()
            renamed.rename(source)
        card = source / 'base1-001.png'
        hardlink = self.work / 'hardlink.png'
        os.link(card, hardlink)
        try:
            with self.assertRaises(tool.PreparationError):
                tool.prepare(self.work)
        finally:
            hardlink.unlink()
        dest = self.work / DEST
        dest.parent.mkdir(parents=True, exist_ok=True)
        outside = self.work / 'foreign-destination'
        outside.mkdir()
        sentinel = outside / 'never-touch.txt'
        sentinel.write_bytes(b'foreign')
        result = subprocess.run(['cmd.exe', '/c', 'mklink', '/J', str(dest), str(outside)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        try:
            with self.assertRaises(tool.PreparationError):
                tool.prepare(self.work)
            self.assertEqual([p.name for p in outside.iterdir()], ['never-touch.txt'])
            self.assertEqual(sentinel.read_bytes(), b'foreign')
        finally:
            dest.rmdir()

    def test_strict_json_rejects_bom_duplicate_keys_and_nonfinite_values(self):
        self.fixture()
        good = (self.work / INTAKE).read_bytes()
        for bad in [b'\xef\xbb\xbf' + good, good.replace(b'"fileCount": 114', b'"fileCount": 114, "fileCount": 114'), good.replace(b'114', b'NaN', 1), b'{truncated']:
            with self.subTest(prefix=bad[:30]):
                (self.work / INTAKE).write_bytes(bad)
                with self.assertRaises(tool.PreparationError):
                    tool.prepare(self.work)
                self.assertFalse((self.work / DEST / 'Manifest.json').exists())

    def test_cli_actually_prepares_and_retains_reports_and_hashpins(self):
        self.fixture()
        result = subprocess.run([sys.executable, '-B', str(TOOL), '--root', str(self.work)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue(result.stdout.strip(), 'CLI must print its completed preparation report')
        report = json.loads(result.stdout)
        saved = json.loads((self.work / '.artifacts/materials-authoring/Prepared_Report.json').read_bytes())
        self.assertEqual(saved, report)
        self.assertEqual(report['bindingCount'], 121)
        pins = json.loads((self.work / '.artifacts/materials-authoring/Source_Hash_Pins.json').read_bytes())
        self.assertEqual(len(pins['files']), 114)
        self.assertEqual(pins['sourcePinsSha256'], report['sourcePinsSha256'])
        self.assertEqual(sha((self.work / DEST / 'Manifest.json').read_bytes()), report['manifestSha256'])

    def test_publication_rechecks_prior_manifest_identity(self):
        self.fixture()
        tool.prepare(self.work)
        manifest = self.work / DEST / 'Manifest.json'
        expected = manifest.read_bytes()
        candidate = self.work / 'candidate.json'
        candidate.write_bytes(b'new complete manifest')
        foreign = b'foreign manifest substituted immediately before publication'
        manifest.write_bytes(foreign)
        self.assertIn('expected', tool.publish_manifest.__code__.co_varnames, 'publication must pin its prior manifest identity')
        with self.assertRaises(tool.PreparationError):
            tool.publish_manifest(candidate, manifest, expected)
        self.assertEqual(manifest.read_bytes(), foreign)
        self.assertTrue(candidate.is_file())

    def test_lossless_palette_transparency_original_dimensions_and_codec(self):
        self.assertIsNotNone(tool, 'asset preparation implementation is missing')
        source = Image.new('P', (600, 825), 1)
        source.putpalette([19, 23, 71, 47, 89, 101] + [0] * 762)
        source.putpixel((12, 20), 0)
        buf = io.BytesIO()
        source.save(buf, format='PNG', transparency=bytes([0, 175]))
        before = buf.getvalue()
        derived = tool.normalize_png(before, (600, 825))
        self.assertEqual(before, buf.getvalue())
        self.assertEqual(struct.unpack('>IIBBBBB', derived[16:29]), (600, 825, 8, 6, 0, 0, 0))
        with Image.open(io.BytesIO(before)) as original, Image.open(io.BytesIO(derived)) as result:
            result.load()
            self.assertEqual(original.convert('RGBA').tobytes(), result.convert('RGBA').tobytes())
        self.assertEqual(derived, tool.normalize_png(before, (600, 825)))


if __name__ == '__main__':
    unittest.main(verbosity=2)
