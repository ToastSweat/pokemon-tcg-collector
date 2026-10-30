"""Opt-in, package-free Phase 1 Mono verification. No game startup hooks."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
MONO_ROOT = Path("C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge")

def inclusion_probabilities(weights, draws):
    """Exact finite-state exclusion DP over equal-weight groups, not RNG simulation."""
    from collections import Counter, defaultdict
    if not weights or any(w <= 0 for w in weights) or draws < 0 or draws > len(weights):
        raise ValueError("Invalid weighted-without-replacement population/demand")
    groups = Counter(weights)
    group_weights = sorted(groups)
    total = sum(weights)
    by_weight = {}
    for target in group_weights:
        others = [groups[w] - (w == target) for w in group_weights]
        states = {(0,) * len(group_weights): 1.0}
        for _ in range(draws):
            next_states = defaultdict(float)
            for removed, mass in states.items():
                remaining_total = total - sum(k * w for k, w in zip(removed, group_weights))
                for j, w in enumerate(group_weights):
                    left = others[j] - removed[j]
                    if left:
                        key = list(removed)
                        key[j] += 1
                        next_states[tuple(key)] += mass * left * w / remaining_total
            states = next_states
        by_weight[target] = 1.0 - sum(states.values())
    return [by_weight[w] for w in weights]

def analyze_batch(report, rule):
    if report.get("sampleSize", 0) <= 0:
        raise ValueError("Nonpositive batch sampleSize")
    if report.get("countCount") != len(report.get("counts", [])) or report.get("energyPairCount") != len(report.get("energyPairs", [])):
        raise ValueError("Declared batch counts do not match arrays")
    import math
    n = report["sampleSize"]
    pools = {p["id"]: p for p in rule["pools"]}
    expected_ids = {e["cardId"] for p in rule["pools"] for e in p["entries"]}
    counts = {c["id"]: c for c in report["counts"]}
    if set(counts) != expected_ids or len(counts) != len(report["counts"]):
        raise ValueError("Missing/extra/duplicate batch card counters")
    if report["cardCount"] != 11*n or sum(c["count"] for c in counts.values()) != 11*n:
        raise ValueError("Card counts do not reconcile")
    if any(type(c["count"]) is not int or c["count"] < 0 for c in counts.values()):
        raise ValueError("Invalid card counters")
    for name in ("modelId", "modelVersion", "printingId", "status", "rngAlgorithm"):
        if report[name] != rule[name]:
            raise ValueError("Batch provenance mismatch: " + name)
    energy = pools["energy"]["entries"]
    energy_ids = {e["cardId"] for e in energy}
    first = {c["id"]: c["count"] for c in report["firstEnergyCounts"]}
    second = {c["id"]: c["count"] for c in report["secondEnergyCounts"]}
    pairs = {(c["firstId"], c["secondId"]): c["count"] for c in report["energyPairs"]}
    if set(first) != energy_ids or set(second) != energy_ids or set(pairs) != {(a,b) for a in energy_ids for b in energy_ids}:
        raise ValueError("Incomplete Energy counters")
    if len(report["firstEnergyCounts"]) != len(first) or len(report["secondEnergyCounts"]) != len(second) or len(report["energyPairs"]) != len(pairs):
        raise ValueError("Duplicate Energy counters")
    if sum(first.values()) != n or sum(second.values()) != n or sum(pairs.values()) != n:
        raise ValueError("Energy counters do not reconcile")
    if any(type(v) is not int or v < 0 for values in (first, second, pairs) for v in values.values()):
        raise ValueError("Invalid Energy counter")
    for id in energy_ids:
        if first[id] + second[id] != counts[id]["count"] or sum(v for (a,b),v in pairs.items() if a == id) != first[id] or sum(v for (a,b),v in pairs.items() if b == id) != second[id]:
            raise ValueError("Energy pair marginals do not reconcile")
    if sum(v for (a,b),v in pairs.items() if a == b) != report["energyRepeatCount"]:
        raise ValueError("Energy repeat count does not reconcile")
    for pool_id, expected_total in {"common":5*n, "uncommon":3*n, "energy":2*n, "holo":report["holoCount"], "rare":n-report["holoCount"]}.items():
        if sum(counts[e["cardId"]]["count"] for e in pools[pool_id]["entries"]) != expected_total:
            raise ValueError("Pool count does not reconcile: " + pool_id)
    checks, inclusion = [], []
    def check(name, actual, trials, probability):
        expected = trials * probability
        tolerance = 6 * math.sqrt(trials * probability * (1-probability)) + 2
        checks.append({"name":name, "actualCount":actual, "trials":trials,
                       "expectedProbability":probability, "expectedCount":expected,
                       "actualRate":actual/trials if trials else None,
                       "toleranceCount":tolerance, "toleranceRate":tolerance/trials if trials else None,
                       "passed":abs(actual-expected) <= tolerance})
    checks.append({"name":"all-pack-invariants", "actualCount":report["invariantViolations"], "expectedCount":0,
                   "passed":report["invariantViolations"] == 0})
    check("holo-rate", report["holoCount"], n, 1/3)
    e_total = sum(e["weight"] for e in energy)
    ep = {e["cardId"]: e["weight"]/e_total for e in energy}
    check("energy-repeat", report["energyRepeatCount"], n, sum(p*p for p in ep.values()))
    for pool_id, pool in pools.items():
        entries = pool["entries"]
        draws = 0
        total = sum(e["weight"] for e in entries)
        if pool_id in ("common", "uncommon"):
            draws = 5 if pool_id == "common" else 3
            probabilities = inclusion_probabilities([e["weight"] for e in entries], draws)
            if abs(sum(probabilities)-draws) > 1e-10:
                raise ValueError("Oracle inclusion probabilities fail demand sum")
        else:
            probabilities = [e["weight"]/total for e in entries]
        for e, probability in zip(entries, probabilities):
            id = e["cardId"]
            if counts[id]["poolId"] != pool_id:
                raise ValueError("Counter pool mismatch")
            if pool_id in ("common", "uncommon"):
                inclusion.append({"cardId":id, "poolId":pool_id, "weight":e["weight"], "draws":draws,
                                  "expectedInclusionProbability":probability,
                                  "naiveDrawsTimesSheetShare":draws*e["weight"]/total})
                check("inclusion:"+id, counts[id]["count"], n, probability)
            elif pool_id == "energy":
                check("energy-occurrences:"+id, counts[id]["count"], 2*n, probability)
                check("energy-first:"+id, first[id], n, probability)
                check("energy-second:"+id, second[id], n, probability)
            else:
                branch = 1/3 if pool_id == "holo" else 2/3
                check("unconditional:"+id, counts[id]["count"], n, branch*probability)
                trials = report["holoCount"] if pool_id == "holo" else n-report["holoCount"]
                check("conditional:"+id, counts[id]["count"], trials, probability)
    for (a,b), actual in sorted(pairs.items()):
        check("energy-pair:"+a+":"+b, actual, n, ep[a]*ep[b])
    return {"sampleSize":n, "seed":report["seed"], "modelId":report["modelId"], "modelVersion":report["modelVersion"],
            "rngAlgorithm":report["rngAlgorithm"], "warning":report["warning"],
            "tolerance":"Each count: abs(actual-N*p) <= 6*sqrt(N*p*(1-p))+2; per-card C/U trials are packs, not slots. Deterministic PRNG regression, not a historical confidence interval.",
            "oracle":"Independent Python equal-weight-group exclusion-state DP; sum inclusion probabilities equals draws; tested against exhaustive small-population ordered enumeration.",
            "checkCount":len(checks), "checks":checks, "inclusionProbabilityCount":len(inclusion),
            "inclusionProbabilities":inclusion, "passed":all(c["passed"] for c in checks)}

def main():
    p = argparse.ArgumentParser(__doc__)
    p.add_argument("--tests", action="store_true")
    p.add_argument("--diagnostics", action="store_true", help="Emit three ordered provisional packs")
    p.add_argument("--batch", type=int, default=0, help="Opt-in batch sample size, at least 100000")
    p.add_argument("--seed", type=int, default=2463534242)
    p.add_argument("--filter", default="")
    p.add_argument("--label", default="latest")
    a = p.parse_args()
    if not a.tests:
        p.error("Explicit --tests required (generation is opt-in)")
    if a.seed < 1 or a.seed > 0xffffffff:
        p.error("--seed must be a nonzero UInt32")
    if a.batch and (a.batch < 100000 or a.batch > 2147483647 // 11):
        p.error("--batch must be between 100000 and Int32.MaxValue/11")
    if not a.label or any(c not in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_" for c in a.label):
        p.error("Unsafe artifact label")
    out = ROOT / ".artifacts" / "domain"
    out.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, TMP=str(out), TEMP=str(out), TMPDIR=str(out))
    log = []
    def invoke(cmd):
        log.append("COMMAND " + json.dumps([str(x) for x in cmd]))
        r = subprocess.run([str(x) for x in cmd], cwd=ROOT, env=env, capture_output=True, text=True)
        log.extend([r.stdout, r.stderr, "EXIT " + str(r.returncode)])
        print(r.stdout, end="")
        print(r.stderr, end="", file=sys.stderr)
        return r
    try:
        sources = sorted((ROOT / "Assets/VintageCollector/Domain").rglob("*.cs"))
        tests = sorted((ROOT / "Tests").rglob("*.cs"))
        if not sources or not tests:
            raise RuntimeError("Missing domain or test sources")
        exe = out / "Phase1.exe"
        library = out / "VintageCollector.Domain.dll"
        r = invoke([MONO_ROOT / "bin/mono.exe", MONO_ROOT / "lib/mono/4.5/mcs.exe", "-warnaserror+", "-target:library", "-out:"+str(library), *sources])
        if r.returncode:
            return r.returncode
        r = invoke([MONO_ROOT / "bin/mono.exe", MONO_ROOT / "lib/mono/4.5/mcs.exe", "-warnaserror+", "-r:System.Runtime.Serialization", "-r:"+str(library), "-out:"+str(exe), *tests])
        if r.returncode:
            return r.returncode
        r = invoke([MONO_ROOT / "bin/mono.exe", exe, "tests", a.filter])
        if r.returncode:
            return r.returncode
        report = json.loads(r.stdout)
        (out / (a.label + "-tests.json")).write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
        if report["testCount"] <= 0 or report["testCount"] != len(report["checks"]) or not report["passed"] or not all(c["passed"] for c in report["checks"]):
            raise RuntimeError("Invalid/zero/failed test report")
        if a.diagnostics:
            packs = []
            for seed in (1, 4, a.seed):
                r = invoke([MONO_ROOT / "bin/mono.exe", exe, "diagnostic", seed])
                if r.returncode:
                    return r.returncode
                pack = json.loads(r.stdout)
                if pack["cardCount"] != len(pack["cards"]) or pack["cardCount"] != 11:
                    raise RuntimeError("Diagnostic count mismatch")
                packs.append(pack)
            (out / (a.label + "-diagnostics.json")).write_text(json.dumps({"packCount":len(packs), "packs":packs}, indent=2)+"\n", encoding="utf-8")
        if a.batch:
            r = invoke([MONO_ROOT / "bin/mono.exe", exe, "batch", a.seed, a.batch])
            if r.returncode:
                return r.returncode
            batch = json.loads(r.stdout)
            (out / (a.label + "-batch.json")).write_text(json.dumps(batch, indent=2)+"\n", encoding="utf-8")
            rule = json.loads((ROOT / "Assets/VintageCollector/Data/base-en-unlimited1999.model-a-v0.1.json").read_text(encoding="utf-8"))
            analysis = analyze_batch(batch, rule)
            (out / (a.label + "-distribution.json")).write_text(json.dumps(analysis, indent=2, allow_nan=False)+"\n", encoding="utf-8")
            print(json.dumps({"sampleSize":batch["sampleSize"], "distributionCheckCount":analysis["checkCount"], "passed":analysis["passed"]}))
            if not analysis["passed"]:
                return 1
        return 0
    except Exception as e:
        log.append(str(e))
        print(str(e), file=sys.stderr)
        return 1
    finally:
        (out / (a.label + ".log")).write_text("\n".join(log), encoding="utf-8")

if __name__ == "__main__":
    sys.exit(main())
