"""Independent real-art readback + separate-process rerun audit. All output stays under Root."""
import hashlib
import io
import json
from pathlib import Path
import subprocess
import sys
import unittest
from PIL import Image

sys.dont_write_bytecode = True
from PlaceholderAuthoringTests import ROOT, TOOL, decode_png


class PlaceholderActualReadbackTests(unittest.TestCase):
    def test_actual_pngs_full_decode_native_sheet_crops_protected_sources_and_process_idempotence(self):
        output = ROOT / 'Authoring/Phase5/Placeholders'
        manifest_path = output / 'PlaceholderManifest.json'
        self.assertTrue(manifest_path.exists(), 'Run the authoring CLI before actual readback')
        manifest = json.loads(manifest_path.read_text('utf-8'))
        protected = sorted(x for x in (ROOT / 'Pokemon-Base-Set').rglob('*') if x.is_file())
        protected += [ROOT / 'Assets/Supplied/AssetManifest.json',
                      ROOT / 'Assets/Supplied/BaseSetMaterials/Manifest.json']
        before = {x.relative_to(ROOT).as_posix(): (hashlib.sha256(x.read_bytes()).hexdigest(), x.stat().st_mtime_ns)
                  for x in protected}
        before_outputs = {x.name: (hashlib.sha256(x.read_bytes()).hexdigest(), x.stat().st_mtime_ns)
                          for x in output.iterdir()}
        subprocess_runs = []
        for _ in range(2):
            run = subprocess.run([sys.executable, '-B', str(TOOL)], cwd=ROOT,
                                 capture_output=True, text=True, timeout=30)
            self.assertEqual(run.returncode, 0, run.stderr)
            subprocess_runs.append(json.loads(run.stdout))
        self.assertEqual(before_outputs, {x.name: (hashlib.sha256(x.read_bytes()).hexdigest(), x.stat().st_mtime_ns)
                                         for x in output.iterdir()})
        self.assertEqual(before, {x.relative_to(ROOT).as_posix(): (hashlib.sha256(x.read_bytes()).hexdigest(), x.stat().st_mtime_ns)
                                  for x in protected})
        self.assertEqual(before['Assets/Supplied/AssetManifest.json'][0],
                         'f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8')
        self.assertEqual(before['Assets/Supplied/BaseSetMaterials/Manifest.json'][0],
                         '7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be')
        decoded, image_report = {}, []
        for row in manifest['assets'] + manifest['previews']:
            path = ROOT / row['relativePath']
            data = path.read_bytes()
            width, height, pixels = decode_png(data)
            digest = hashlib.sha256(data).hexdigest()
            self.assertEqual((width, height, digest), (row['width'], row['height'], row['sha256']))
            with Image.open(io.BytesIO(data)) as image:
                image.verify()
            with Image.open(io.BytesIO(data)) as image:
                image.load()
                self.assertEqual(image.mode, 'RGBA')
                self.assertEqual(image.tobytes(), pixels)
            decoded[path.name] = (width, height, pixels)
            image_report.append({'relativePath': row['relativePath'], 'width': width, 'height': height,
                                 'sha256': digest, 'bytes': len(data), 'crcValid': True,
                                 'fullPillowDecodeEqualsIndependentFilter0Decoder': True})
        width, _, sheet = decoded['ContactSheet.png']
        for index, set_id in enumerate(('jungle', 'fossil')):
            y = 96 + index * 889
            positions = [(f'{set_id}-wrapper-{v}.png', 32 + col * 284) for col, v in enumerate('ABC')]
            positions.append((f'{set_id}-card-face-template.png', 908))
            for name, x in positions:
                w, h, original = decoded[name]
                crop = b''.join(sheet[((y+dy)*width+x)*4:((y+dy)*width+x+w)*4] for dy in range(h))
                self.assertEqual(original, crop, name)
        self.assertEqual(len(manifest['assets']), 8)
        self.assertEqual(len({x['sha256'] for x in manifest['assets']}), 8)
        wrappers = [x for x in manifest['assets'] if x['role'] == 'wrapper-front']
        self.assertEqual(len(wrappers), 6)
        self.assertEqual(len({x['sha256'] for x in wrappers}), 6)
        self.assertFalse(manifest['runtimeBound'])
        report = {'status': 'PASS', 'runtimeBound': False, 'artImages': 8, 'wrapperImages': 6,
                  'distinctArtSha256Count': 8, 'wrapperSha256Count': 6, 'decodedPNGsIncludingPreview': len(image_report),
                  'nativePixelSheetCropsEqualOriginals': 8, 'separateProcessIdempotentReruns': 2,
                  'unchangedOutputBytesAndMtimes': len(before_outputs),
                  'unchangedProtectedFileBytesAndMtimes': len(protected),
                  'protectedPins': before, 'images': image_report, 'rerunResults': subprocess_runs,
                  'manifestSha256': hashlib.sha256(manifest_path.read_bytes()).hexdigest()}
        report_path = ROOT / '.artifacts/phase5-placeholders/Actual_Readback.json'
        report_path.write_text(json.dumps(report, indent=2, sort_keys=True) + '\n', encoding='utf-8')
        print(json.dumps({k: v for k, v in report.items() if k not in ('protectedPins', 'images', 'rerunResults')}, sort_keys=True))


if __name__ == '__main__':
    unittest.main(verbosity=2)
