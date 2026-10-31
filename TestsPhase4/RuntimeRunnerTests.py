"""Real project-local runner provenance and stale Editor-script refusal tests."""
import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('phase4_runtime_runner', ROOT/'tools/run_phase4_runtime_native.py')
assert SPEC is not None and SPEC.loader is not None
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)

class RuntimeRunnerTests(unittest.TestCase):
    def test_all_source_keys_use_one_platform_independent_format(self):
        self.assertTrue(callable(getattr(RUNNER, 'source_key', None)), 'Native source verification uses inconsistent path keys')
        for relative in ('tools/run_phase4_runtime_native.py', 'ProjectSettings/ProjectVersion.txt', 'Assets/VintageCollector/Runtime/OpeningGame.cs'):
            self.assertEqual(RUNNER.source_key(ROOT/relative), relative)

    def test_provenance_pins_runner_and_effective_project_version(self):
        self.assertTrue(callable(getattr(RUNNER, 'provenance_pins', None)), 'Runner configuration provenance is not implemented')
        pins = RUNNER.provenance_pins()
        for relative in ('tools/run_phase4_runtime_native.py', 'ProjectSettings/ProjectVersion.txt'):
            self.assertEqual(pins[relative], hashlib.sha256((ROOT/relative).read_bytes()).hexdigest())

    def test_stale_editor_script_refusal_is_readonly(self):
        self.assertTrue(callable(getattr(RUNNER, 'require_exact_editor_scripts', None)), 'Stale Editor-script admission is not implemented')
        base = ROOT/'.artifacts/phase4-runtime-runner-tests'
        base.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=base) as directory:
            editor = Path(directory)
            keep, stale = editor/'Expected.cs', editor/'Unexpected.cs'
            keep.write_text('// expected', encoding='utf-8')
            RUNNER.require_exact_editor_scripts(editor, {keep})
            stale.write_text('// retained stale script', encoding='utf-8')
            with self.assertRaises(RuntimeError):
                RUNNER.require_exact_editor_scripts(editor, {keep})
            self.assertEqual(stale.read_text(encoding='utf-8'), '// retained stale script')
            self.assertEqual(keep.read_text(encoding='utf-8'), '// expected')

if __name__ == '__main__':
    unittest.main(verbosity=2)
