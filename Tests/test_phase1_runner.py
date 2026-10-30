"""Independent oracle and runner fail-closed checks; opt-in unittest script."""
import importlib.util
import itertools
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("phase1_runner", ROOT / "tools/run_phase1.py")
assert spec is not None and spec.loader is not None
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)

class OracleTests(unittest.TestCase):
    def test_grouped_marginals_match_exhaustive_ordered_enumeration(self):
        weights = [1, 2, 2, 4]
        for draws in (1, 2, 3, 4):
            expected = [0.0] * len(weights)
            for order in itertools.permutations(range(len(weights)), draws):
                probability, remaining = 1.0, sum(weights)
                for index in order:
                    probability *= weights[index] / remaining
                    remaining -= weights[index]
                for index in order:
                    expected[index] += probability
            actual = runner.inclusion_probabilities(weights, draws)
            for a, e in zip(actual, expected):
                self.assertAlmostEqual(a, e, places=12)
            self.assertAlmostEqual(sum(actual), draws, places=12)

class IndependentPackOracleTests(unittest.TestCase):
    def test_python_reference_matches_actual_seeded_ordered_packs(self):
        import subprocess
        rule = json.loads((ROOT / "Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json").read_text(encoding="utf-8"))
        for seed in (1, 4, 12345, 2463534242):
            state = seed
            def next_int(bound):
                nonlocal state
                while True:
                    state ^= (state << 13) & 0xffffffff
                    state ^= state >> 17
                    state ^= (state << 5) & 0xffffffff
                    state &= 0xffffffff
                    value = state - 1
                    if value < 0xffffffff - 0xffffffff % bound:
                        return value % bound
            pools = {p["id"]: p for p in rule["pools"]}
            removed = {id:set() for id in pools}
            ids = []
            for slot in rule["slots"]:
                id = slot["poolId"]
                if slot["alternativePoolId"] and next_int(slot["alternativeDenominator"]) < slot["alternativeNumerator"]:
                    id = slot["alternativePoolId"]
                pool = pools[id]
                eligible = [e for e in pool["entries"] if e["cardId"] not in removed[id]]
                ticket = next_int(sum(e["weight"] for e in eligible))
                # Independent cumulative interval selection, not subtractive C# loop.
                cumulative = list(itertools.accumulate(e["weight"] for e in eligible))
                index = next(i for i, upper in enumerate(cumulative) if ticket < upper)
                selected = eligible[index]["cardId"]
                ids.append(selected)
                if pool["uniqueWithinPack"]:
                    removed[id].add(selected)
            result = subprocess.run([str(runner.MONO_ROOT / "bin/mono.exe"), str(ROOT / ".artifacts/domain/Phase1.exe"), "diagnostic", str(seed)], cwd=ROOT, capture_output=True, text=True, check=True)
            actual = json.loads(result.stdout)
            self.assertEqual(ids, [c["id"] for c in actual["cards"]])

class BatchAnalysisTests(unittest.TestCase):
    def test_real_batch_probabilities_and_all_checks(self):
        import subprocess
        import copy
        result = subprocess.run([str(runner.MONO_ROOT / "bin/mono.exe"), str(ROOT / ".artifacts/domain/Phase1.exe"), "batch", "2463534242", "100000"], cwd=ROOT, capture_output=True, text=True, check=True)
        actual = json.loads(result.stdout)
        rule = json.loads((ROOT / "Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json").read_text(encoding="utf-8"))
        analysis = runner.analyze_batch(actual, rule)
        self.assertIsInstance(analysis, dict)
        self.assertTrue(analysis["passed"])
        self.assertEqual(analysis["checkCount"], len(analysis["checks"]))
        self.assertEqual(analysis["sampleSize"], 100000)
        self.assertEqual(len(analysis["inclusionProbabilities"]), 64)
        checks = {c["name"]: c for c in analysis["checks"]}
        self.assertAlmostEqual(checks["holo-rate"]["expectedProbability"], 1/3)
        self.assertAlmostEqual(checks["energy-repeat"]["expectedProbability"], sum(w*w for w in (9,9,17,35,34,17))/121**2)
        self.assertTrue(all(c["passed"] for c in analysis["checks"]))
        # MUTATED TEST fixture: conserve counts but concentrate holos in one identity.
        bad = copy.deepcopy(actual)
        holos = [c for c in bad["counts"] if c["poolId"] == "holo"]
        total = sum(c["count"] for c in holos)
        for c in holos: c["count"] = 0
        holos[0]["count"] = total
        self.assertFalse(runner.analyze_batch(bad, rule)["passed"])

    def test_analysis_rejects_biased_actual_counter_fixture(self):
        rule = json.loads((ROOT / "Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json").read_text(encoding="utf-8"))
        with self.assertRaises(ValueError):
            runner.analyze_batch({"sampleSize": 0}, rule)
        # TEST fixture intentionally inconsistent and zeroed, not generated evidence.
        with self.assertRaises(ValueError):
            runner.analyze_batch({"sampleSize": 100000, "countCount": 101, "counts": [], "energyPairCount": 36, "energyPairs": []}, rule)

class RunnerIntegrationTests(unittest.TestCase):
    def test_opt_in_full_run_writes_parseable_evidence(self):
        import subprocess
        result = subprocess.run([__import__("sys").executable, "-B", str(ROOT / "tools/run_phase1.py"), "--tests", "--diagnostics", "--batch", "100000", "--label", "integration"], cwd=ROOT, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        out = ROOT / ".artifacts/domain"
        analysis = json.loads((out / "integration-distribution.json").read_text(encoding="utf-8"))
        raw = json.loads((out / "integration-batch.json").read_text(encoding="utf-8"))
        diagnostic = json.loads((out / "integration-diagnostics.json").read_text(encoding="utf-8"))
        self.assertTrue(analysis["passed"])
        self.assertEqual(raw["sampleSize"], 100000)
        self.assertEqual(diagnostic["packCount"], len(diagnostic["packs"]))
        self.assertEqual(diagnostic["packCount"], 3)
        self.assertTrue(any(p["cards"][7]["rarity"] == "holo" for p in diagnostic["packs"]), "diagnostics should demonstrate holo replacement")
        self.assertTrue(any(p["cards"][5]["id"] == p["cards"][6]["id"] for p in diagnostic["packs"]), "diagnostics should demonstrate allowed Energy repeats")
        for pack in diagnostic["packs"]:
            self.assertEqual(pack["cardCount"], len(pack["cards"]))

class RunnerFailureTests(unittest.TestCase):
    def test_missing_zero_compile_runtime_and_count_mismatch_fail_closed(self):
        from unittest.mock import patch
        import contextlib
        import io
        fixture_base = ROOT / ".artifacts/domain/runner-failure-fixtures"
        cases = {
            "missing": None,
            "compile": "public static class Bad { this is invalid C sharp; }",
            "runtime": "public static class Bad { public static int Main() { return 7; } }",
            "zero": 'public static class Bad { public static int Main() { System.Console.WriteLine("{\\\"testCount\\\":0,\\\"passed\\\":true,\\\"checks\\\":[]}"); return 0; } }',
            "mismatch": 'public static class Bad { public static int Main() { System.Console.WriteLine("{\\\"testCount\\\":2,\\\"passed\\\":true,\\\"checks\\\":[{\\\"passed\\\":true}]}"); return 0; } }',
        }
        outcomes = []
        for name, source in cases.items():
            fixture = fixture_base / name
            fixture.mkdir(parents=True, exist_ok=True)
            if source is not None:
                domain = fixture / "Assets/VintageCollector/Domain"
                tests = fixture / "Tests"
                domain.mkdir(parents=True, exist_ok=True)
                tests.mkdir(parents=True, exist_ok=True)
                (domain / "Bad.cs").write_text("public sealed class DeliberateNegativeFixture { }", encoding="utf-8")
                (tests / "Empty.cs").write_text(source, encoding="utf-8")
            with patch.object(runner, "ROOT", fixture), patch("sys.argv", ["run_phase1.py", "--tests", "--label", name]), contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                code = runner.main()
            self.assertNotEqual(code, 0, name)
            log = (fixture / (".artifacts/domain/" + name + ".log")).read_text(encoding="utf-8")
            if name in ("zero", "mismatch"):
                self.assertIn("Invalid/zero/failed test report", log, "must reach report validation, not fail compilation")
            outcomes.append({"name":name, "exitCode":code, "passed":code != 0})
        # A missing filter selection exercises the real harness, not a report fixture.
        import subprocess
        result = subprocess.run([__import__("sys").executable, "-B", str(ROOT / "tools/run_phase1.py"), "--tests", "--filter", "NO_SUCH_TEST", "--label", "zero-selection"], cwd=ROOT, capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        outcomes.append({"name":"real-zero-selection", "exitCode":result.returncode, "passed":result.returncode != 0})
        (ROOT / ".artifacts/domain/runner-failure-checks.json").write_text(json.dumps({"checkCount":len(outcomes), "checks":outcomes, "passed":all(x["passed"] for x in outcomes)}, indent=2)+"\n", encoding="utf-8")

class JsonResult(unittest.TextTestResult):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.checks = []
    def addSuccess(self, test):
        super().addSuccess(test)
        self.checks.append({"name":test.id(), "passed":True, "error":""})
    def addFailure(self, test, err):
        super().addFailure(test, err)
        self.checks.append({"name":test.id(), "passed":False, "error":"".join(__import__("traceback").format_exception(*err))})
    def addError(self, test, err):
        super().addError(test, err)
        self.checks.append({"name":test.id(), "passed":False, "error":"".join(__import__("traceback").format_exception(*err))})

if __name__ == "__main__":
    program = unittest.main(testRunner=unittest.TextTestRunner(resultclass=JsonResult), exit=False)
    result = program.result
    assert isinstance(result, JsonResult)
    checks = result.checks
    passed = result.wasSuccessful() and result.testsRun > 0 and result.testsRun == len(checks)
    report = {"testCount":result.testsRun, "checks":checks, "passed":passed}
    output = ROOT / ".artifacts/domain/python-tests.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(report))
    raise SystemExit(0 if passed else 1)
