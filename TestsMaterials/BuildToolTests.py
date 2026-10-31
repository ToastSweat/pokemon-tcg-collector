"""Owned material builder admission tests. Fixtures never leave .artifacts."""
import importlib.util
import json
from pathlib import Path
import sys
import unittest
import uuid
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
class BuildToolTests(unittest.TestCase):
    def test_folder_meta_exception_requires_actual_bytes_and_exact_closure(self):
        path=ROOT/'tools/run_materials_build.py'
        self.assertTrue(path.is_file(),'separate materials build driver missing')
        spec=importlib.util.spec_from_file_location('materials_build',path);assert spec is not None and spec.loader is not None
        m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
        stage=ROOT/'.artifacts/materials-build-tool-tests'/uuid.uuid4().hex
        stage.mkdir(parents=True,exist_ok=False)
        (stage/'Assets/Folder').mkdir(parents=True)
        raw=b'fileFormatVersion: 2\nguid: 12345678901234567890123456789012\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
        meta=stage/'Assets/Folder.meta';meta.write_bytes(raw)
        digest=m.sha(meta)
        compiled={'fileCount':2,'files':[{'path':'Assets/source.cs','sha256':'a'*64},{'path':'Assets/Folder.meta','sha256':digest}]}
        closure=m.verify_compiler_closure({'Assets/source.cs':'a'*64},compiled,stage)
        self.assertEqual(closure['nativeGeneratedFolderMetas'],{'Assets/Folder.meta':digest})
        meta.write_bytes(raw+b'# changed')
        with self.assertRaises(ValueError):m.verify_compiler_closure({'Assets/source.cs':'a'*64},compiled,stage)
        with self.assertRaises(ValueError):m.verify_compiler_closure({'Assets/source.cs':'a'*64},{'fileCount':1,'files':compiled['files']},stage)
        with self.assertRaises(ValueError):m.verify_compiler_closure({'Assets/source.cs':'b'*64},compiled,stage)
    def test_missing_source_asset_meta_is_a_prebuild_gate(self):
        spec=importlib.util.spec_from_file_location('materials_build',ROOT/'tools/run_materials_build.py');assert spec is not None and spec.loader is not None
        m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
        self.assertTrue(callable(getattr(m,'require_source_asset_metas',None)),'source asset meta admission missing')
        with self.assertRaises(ValueError):m.require_source_asset_metas({'Assets/Supplied/private.png':'a'*64})
        m.require_source_asset_metas({'Assets/Supplied/private.png':'a'*64,'Assets/Supplied/private.png.meta':'b'*64})
    def test_preservation_includes_phase4_and_prunes_mutable_before_stat(self):
        path=ROOT/'tools/run_materials_build.py'
        spec=importlib.util.spec_from_file_location('materials_build',path);assert spec is not None and spec.loader is not None
        m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
        fixture=ROOT/'.artifacts/materials-build-tool-tests'/uuid.uuid4().hex
        accepted=fixture/'Builds/Phase4Windows';accepted.mkdir(parents=True)
        (accepted/'game.exe').write_bytes(b'accepted')
        (accepted/'SaVeS').mkdir();(accepted/'SaVeS/private').write_bytes(b'never traverse')
        (accepted/'LoGs').mkdir();(accepted/'LoGs/private').write_bytes(b'never traverse')
        candidate=fixture/'Builds/Phase3MaterialsWindows';candidate.mkdir();(candidate/'game.exe').write_bytes(b'candidate')
        m.ROOT=fixture
        self.assertEqual(m.preserved_players(),{'Phase4Windows':{'game.exe':m.sha(accepted/'game.exe')}})
        canary=candidate/'sAvEs';canary.mkdir();(canary/'state.save').write_bytes(b'untouched')
        m.APPROVED=fixture
        # Admission must refuse before hashing manifest0 or doing unrelated work.
        with self.assertRaises(ValueError):m.admission()
        self.assertEqual((canary/'state.save').read_bytes(),b'untouched')
if __name__=='__main__':unittest.main(verbosity=2)
