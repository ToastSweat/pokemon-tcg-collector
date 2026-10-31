"""Actual prepared-art oracle and fail-closed player-driver tests; never launches a player."""
from pathlib import Path
import importlib.util
import hashlib
import subprocess
import sys
import unittest
import uuid
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
sys.path.insert(0,str(ROOT/'tools'))
class PlayerVerifierTests(unittest.TestCase):
    def module(self):
        spec=importlib.util.spec_from_file_location('materials_player',ROOT/'tools/verify_materials_player.py');assert spec is not None and spec.loader is not None
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
    def test_demo_requires_its_own_report_not_an_unstarted_regression_probe(self):
        module=self.module()
        self.assertTrue(callable(getattr(module,'regression_report',None)), 'Material-only demo incorrectly requires legacy probe report')
        fixture=ROOT/'.artifacts/materials-player-tool-tests'/uuid.uuid4().hex;fixture.mkdir(parents=True)
        self.assertIsNone(module.regression_report(fixture,'demo'))
        for mode in ('services','input'):
            with self.assertRaises(FileNotFoundError):module.regression_report(fixture,mode)

    def test_actual_full_quality_oracle_121_never_synthetic(self):
        oracle=self.module().actual_oracle()
        self.assertFalse(oracle['synthetic']);self.assertEqual(len(oracle['bindings']),121)
        self.assertEqual(oracle['manifestSha256'],'7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be')
        self.assertEqual(sum(b['role']=='card-face' for b in oracle['bindings']),101)
        self.assertEqual(sum(b['role']=='holo-mask' for b in oracle['bindings']),15)
        for binding in oracle['bindings']:
            self.assertEqual((binding['width'],binding['height']),(260,475) if binding['role'].startswith('wrapper') else (600,825))
            self.assertRegex(binding['rgbaSha256'],r'^[a-f0-9]{64}$')
    def test_unadmitted_native_build_never_launches_or_creates_session(self):
        label='blocked-'+uuid.uuid4().hex;path=ROOT/'.artifacts/phase4-player'/label
        result=subprocess.run([sys.executable,'-B',str(ROOT/'tools/verify_materials_player.py'),'--label',label,'--build-label','08-native-build','--mode','demo'],cwd=ROOT,capture_output=True,text=True,timeout=30)
        self.assertEqual(result.returncode,2);self.assertIn('successful admitted real native materials build',result.stderr)
        self.assertFalse(path.exists())
    def test_real_png_crc_iend_and_dimensions(self):
        import json
        module=self.module();manifest=json.loads((ROOT/'Assets/Supplied/BaseSetMaterials/Manifest.json').read_text())
        path=ROOT/manifest['assets'][0]['relativePath'];raw=path.read_bytes()
        self.assertEqual(module.validate_png(path),(600,825))
        fixture=ROOT/'.artifacts/materials-player-tool-tests'/uuid.uuid4().hex;fixture.mkdir(parents=True)
        broken=fixture/'truncated.png';broken.write_bytes(raw[:-12])
        with self.assertRaises(ValueError):module.validate_png(broken)
        altered=bytearray(raw);altered[30]^=1;broken.write_bytes(altered)
        with self.assertRaises(ValueError):module.validate_png(broken)
if __name__=='__main__':unittest.main(verbosity=2)
