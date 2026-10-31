"""Pure PNG evidence boundary tests; synthetic bytes are NOT framebuffer evidence."""
from pathlib import Path
import importlib.util
import json
import struct
import sys
import unittest
import zlib
ROOT=Path(__file__).resolve().parents[1]
class PlayerToolsTests(unittest.TestCase):
    def test_png_requires_crc_dimensions_iend_and_full_membership(self):
        path=ROOT/'tools/verify_phase4_player.py';self.assertTrue(path.is_file(),'Phase 4 player verifier missing')
        spec=importlib.util.spec_from_file_location('phase4player',path);assert spec and spec.loader
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
        raw=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',1,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b'\0\0\0\0'))+chunk(b'IEND',b'')
        file=OUT/'synthetic-parser-fixture.png';file.write_bytes(raw)
        self.assertEqual(module.validate_png(file),(1,1))
        for invalid in [raw[:-1],raw+b'junk',raw[:44]+b'x'+raw[45:]]:
            file.write_bytes(invalid)
            with self.assertRaises(ValueError):module.validate_png(file)
    def test_current_control_guard_rejects_retained_real_ghost_snapshot(self):
        spec=importlib.util.spec_from_file_location('phase4player',ROOT/'tools/verify_phase4_player.py');assert spec and spec.loader
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        self.assertTrue(callable(getattr(module,'validate_binder_controls',None)),'current-render guard missing')
        records=json.loads((ROOT/'.artifacts/phase4-player/finish-input-03/native-acknowledgements.json').read_text())
        state=next(r['state'] for r in records if r['state']['binder']['pendingImportConfirm'])
        state['controlsSource']='binder-repaint';state['controlsViewJson']=json.dumps(state['binder'])
        with self.assertRaisesRegex(ValueError,'Ghost/missing current-render controls'):module.validate_binder_controls(state)
    def test_actual_current_render_snapshots_and_ghost_mutations(self):
        import copy
        spec=importlib.util.spec_from_file_location('phase4player',ROOT/'tools/verify_phase4_player.py');assert spec and spec.loader
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        records=json.loads((ROOT/'.artifacts/phase4-player/current-controls-input-01/native-acknowledgements.json').read_text())
        for record in records:
            state=record['state']
            self.assertTrue(module.validate_binder_controls(state),record['action'])
            if not state['binder']['open']:continue
            ghost=copy.deepcopy(state);ghost['controls'].append(dict(name='select:not-in-current-render',x=20,y=20,width=30,height=30,enabled=False))
            with self.assertRaisesRegex(ValueError,'Ghost/missing current-render controls'):module.validate_binder_controls(ghost)
            empty=copy.deepcopy(state);empty['controls']=[]
            self.assertFalse(module.validate_binder_controls(empty))
            stale=copy.deepcopy(state);stale['binder']['quantityText']='different-unrendered-draft'
            with self.assertRaisesRegex(ValueError,'current observed binder DTO'):module.validate_binder_controls(stale)
    def test_png_rejects_invalid_scanline_size_and_filter(self):
        spec=importlib.util.spec_from_file_location('phase4player',ROOT/'tools/verify_phase4_player.py');assert spec and spec.loader
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
        for pixels in [b'\0',b'\x05\0\0\0',b'\0\0\0\0\0']:
            raw=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',1,1,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(pixels))+chunk(b'IEND',b'')
            file=OUT/'synthetic-row-fixture.png';file.write_bytes(raw)
            with self.assertRaises(ValueError):module.validate_png(file)
if __name__=='__main__':
    label=sys.argv.pop(1)
    if not label.replace('-','').isalnum():raise ValueError('Unsafe label')
    OUT=ROOT/'.artifacts/phase4-player-tools'/label;OUT.mkdir(parents=True,exist_ok=False)
    stream=(OUT/'execution.log').open('w');result=unittest.TextTestRunner(stream=stream,verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(PlayerToolsTests));stream.close()
    report={'passed':result.wasSuccessful(),'testCount':result.testsRun,'failures':len(result.failures),'errors':len(result.errors)}
    (OUT/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps(report));sys.exit(not result.wasSuccessful())
