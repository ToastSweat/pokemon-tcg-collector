"""Package-free regression tests for Phase 4 exact-source staging."""
from pathlib import Path
import importlib.util
import json
import os
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]

class BuildToolsTests(unittest.TestCase):
    def test_source_walker_prunes_case_insensitive_saves_before_open(self):
        p=ROOT/'tools/run_phase4_build.py'
        self.assertTrue(p.is_file(),'Phase 4 staging tool is missing')
        spec=importlib.util.spec_from_file_location('phase4build',p);assert spec and spec.loader
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        fixture=OUT/'tree';fixture.mkdir();(fixture/'keep.cs.meta').write_text('real-guid')
        (fixture/'sAvEs').mkdir();(fixture/'sAvEs'/'canary').write_text('never-read')
        (fixture/'Library').mkdir();(fixture/'Library'/'cache').write_text('no')
        from unittest.mock import patch
        original=Path.iterdir
        def guarded(directory):
            if directory.name.casefold()=='saves':raise AssertionError('Save directory traversed')
            return original(directory)
        with patch.object(Path,'iterdir',guarded):files=module.tree_manifest(fixture)
        self.assertEqual(list(files),['keep.cs.meta'])
        self.assertEqual((fixture/'sAvEs'/'canary').read_bytes(),b'never-read')

    def test_exact_stage_preserves_metas_untracked_settings_and_detects_source_drift(self):
        p=ROOT/'tools/run_phase4_build.py';self.assertTrue(p.is_file())
        spec=importlib.util.spec_from_file_location('phase4stage',p);assert spec and spec.loader
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        fixture=OUT/'source';fixture.mkdir()
        for folder in ['Assets','Packages','ProjectSettings']:(fixture/folder).mkdir()
        (fixture/'Assets/component.cs').write_text('class Component {}');(fixture/'Assets/component.cs.meta').write_text('guid: delivered-guid')
        (fixture/'Packages/manifest.json').write_text('{"dependencies":{}}');(fixture/'ProjectSettings/PackageManagerSettings.asset').write_text('user-owned-untracked-settings')
        setattr(module,'ROOT',fixture);setattr(module,'STAGE',fixture/'.artifacts/phase4-build-project');pins=module.source_manifest()
        module.stage_exact(pins,OUT)
        self.assertEqual(module.tree_manifest(module.STAGE/'Assets'),module.tree_manifest(fixture/'Assets'))
        self.assertEqual((module.STAGE/'ProjectSettings/PackageManagerSettings.asset').read_bytes(),b'user-owned-untracked-settings')
        self.assertFalse((module.STAGE/'Library').exists())
        with self.assertRaises(ValueError):module.stage_exact(pins,OUT)
        (fixture/'Assets/component.cs').write_text('class Changed {}')
        with self.assertRaisesRegex(ValueError,'Source changed'):module.stage_exact(pins,OUT,restage=True)

    def test_compiler_closure_only_admits_native_folder_metas(self):
        spec=importlib.util.spec_from_file_location('phase4closure',ROOT/'tools/run_phase4_build.py');assert spec and spec.loader
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        fixture=OUT/'compiler-stage';(fixture/'Assets/Collection').mkdir(parents=True);setattr(module,'STAGE',fixture)
        (fixture/'Assets/Collection.meta').write_text('folderAsset: yes\nguid: native-generated\n')
        pins={'Assets/Component.cs':'source-digest'}
        compiled={'fileCount':2,'files':[{'path':'Assets/Component.cs','sha256':'source-digest'},{'path':'Assets/Collection.meta','sha256':'folder-digest'}]}
        closure=module.verify_compiled_inputs(pins,compiled)
        self.assertTrue(closure['allDeliveredInputsExact']);self.assertEqual(closure['compilerCount'],2)
        compiled['files'][0]['sha256']='changed'
        with self.assertRaisesRegex(ValueError,'changed delivered'):module.verify_compiled_inputs(pins,compiled)
        compiled['files'][0]['sha256']='source-digest';compiled['files'][1]['path']='Assets/Unexpected.cs'
        with self.assertRaisesRegex(ValueError,'Unexpected compiler'):module.verify_compiled_inputs(pins,compiled)
        compiled['files'][1]=dict(compiled['files'][0])
        with self.assertRaisesRegex(ValueError,'count/duplicates'):module.verify_compiled_inputs(pins,compiled)

    def test_standalone_audit_derives_and_verifies_complete_compiler_closure(self):
        import hashlib
        spec=importlib.util.spec_from_file_location('phase4audit',ROOT/'tools/run_phase4_build_audit.py');assert spec and spec.loader
        audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
        self.assertTrue(callable(getattr(audit,'verify_compiler_closure',None)), 'Standalone audit trusts unverified compiler extras')
        stage=OUT/'audit-stage';folder=stage/'Assets/Collection';folder.mkdir(parents=True)
        meta=stage/'Assets/Collection.meta';meta.write_text('fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\nfolderAsset: yes\n')
        digest=hashlib.sha256(meta.read_bytes()).hexdigest();pins={'Assets/Component.cs':'source-digest'}
        compiled={'fileCount':2,'files':[{'path':'Assets/Component.cs','sha256':'source-digest'},{'path':'Assets/Collection.meta','sha256':digest}]}
        closure={'sourceCount':1,'compilerCount':2,'allDeliveredInputsExact':True,'nativeGeneratedFolderMetas':{'Assets/Collection.meta':digest}}
        self.assertEqual(audit.verify_compiler_closure(pins,compiled,closure,stage),closure)
        extra={'fileCount':3,'files':compiled['files']+[{'path':'Assets/Unexpected.cs','sha256':'foreign'}]}
        with self.assertRaises(ValueError):audit.verify_compiler_closure(pins,extra,closure,stage)
        forged=dict(closure,compilerCount=999)
        with self.assertRaises(ValueError):audit.verify_compiler_closure(pins,compiled,forged,stage)
        changed={'fileCount':2,'files':[compiled['files'][0],{'path':'Assets/Collection.meta','sha256':'wrong'}]}
        with self.assertRaises(ValueError):audit.verify_compiler_closure(pins,changed,closure,stage)
        for name in ('../outside.meta','Assets/../outside.meta','Assets\\Collection.meta'):
            bad={'fileCount':2,'files':[compiled['files'][0],{'path':name,'sha256':digest}]}
            with self.assertRaises(ValueError):audit.verify_compiler_closure(pins,bad,closure,stage)

if __name__=='__main__':
    label=sys.argv.pop(1)
    if not label.replace('-','').isalnum():raise ValueError('Unsafe label')
    OUT=ROOT/'.artifacts/phase4-build-tools'/label;OUT.mkdir(parents=True,exist_ok=False)
    stream=(OUT/'execution.log').open('w',encoding='utf-8')
    result=unittest.TextTestRunner(stream=stream,verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(BuildToolsTests));stream.close()
    report={'passed':result.wasSuccessful(),'testCount':result.testsRun,'failures':len(result.failures),'errors':len(result.errors)}
    (OUT/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps(report));sys.exit(not result.wasSuccessful())
